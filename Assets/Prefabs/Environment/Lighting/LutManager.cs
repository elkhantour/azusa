using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class LutManager : MonoBehaviour
{
    [Header("LUT Textures")]
    public Texture2D[] lutTextures = new Texture2D[] { };

    [Header("Volume Reference")]
    [Tooltip("Leave empty to use the global volume found at runtime.")]
    public Volume targetVolume;

    private int _currentIndex = 0;
    private ColorLookup _colorLookup;

    // Override stack: pushed when entering an interior, popped on exit
    private Texture2D _overrideLut = null;

    // -------------------------------------------------------------------------
    // Lifecycle
    // -------------------------------------------------------------------------

    private void OnEnable()
    {
        ResolveVolume();
        TimeManager.OnUpdateMoment += OnUpdateMoment;

        // Apply initial LUT immediately
        if (lutTextures.Length > 0 && lutTextures[_currentIndex] != null)
            ApplyLut(_overrideLut != null ? _overrideLut : lutTextures[_currentIndex]);
    }

    private void OnDisable()
    {
        TimeManager.OnUpdateMoment -= OnUpdateMoment;
    }

    // -------------------------------------------------------------------------
    // TimeManager callback
    // -------------------------------------------------------------------------

    private void OnUpdateMoment(int index, DayMoment dayMoment)
    {
        // Ignore time-of-day transitions while an override is active
        if (_overrideLut != null) return;

        Texture2D current = lutTextures[_currentIndex];
        Texture2D next = lutTextures[index];

        if (next != null && current != null && index != _currentIndex)
        {
            _currentIndex = index;
            StopCoroutine("TransitionLut");
            StartCoroutine(TransitionLut(current, next, dayMoment.transitionTime));
        }
    }

    // -------------------------------------------------------------------------
    // Transition coroutine
    // -------------------------------------------------------------------------

    private IEnumerator TransitionLut(Texture2D from, Texture2D to, float duration)
    {
        if (_colorLookup == null) yield break;

        float time = 0f;

        // Fade out current LUT
        while (time < duration * 0.5f)
        {
            _colorLookup.contribution.value = Mathf.Lerp(1f, 0f, time / (duration * 0.5f));
            time += Time.deltaTime;
            yield return null;
        }

        // Swap texture at the midpoint (invisible since contribution is 0)
        _colorLookup.texture.value = to;
        _colorLookup.texture.overrideState = true;

        // Fade back in
        time = 0f;
        while (time < duration * 0.5f)
        {
            _colorLookup.contribution.value = Mathf.Lerp(0f, 1f, time / (duration * 0.5f));
            time += Time.deltaTime;
            yield return null;
        }

        _colorLookup.contribution.value = 1f;
    }


    // -------------------------------------------------------------------------
    // Override API  (call these when entering / leaving an interior)
    // -------------------------------------------------------------------------

    /// <summary>
    /// Push an interior (or any situational) LUT, interrupting the time-of-day cycle.
    /// </summary>
    public void PushOverrideLut(Texture2D lut, float transitionTime = 0f)
    {
        if (lut == null) return;

        _overrideLut = lut;
        StopCoroutine("TransitionLut");

        Texture2D current = _colorLookup?.texture.value as Texture2D;

        if (transitionTime > 0f && current != null)
            StartCoroutine(TransitionLut(current, lut, transitionTime));
        else
            ApplyLut(lut);
    }

    /// <summary>
    /// Remove the override and return to the current time-of-day LUT.
    /// </summary>
    public void PopOverrideLut(float transitionTime = 0f)
    {
        _overrideLut = null;

        Texture2D returnTo = lutTextures.Length > 0 ? lutTextures[_currentIndex] : null;
        if (returnTo == null) return;

        StopCoroutine("TransitionLut");

        Texture2D current = _colorLookup?.texture.value as Texture2D;

        if (transitionTime > 0f && current != null)
            StartCoroutine(TransitionLut(current, returnTo, transitionTime));
        else
            ApplyLut(returnTo);
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    /// <summary>
    /// Instantly set the LUT with no blending.
    /// </summary>
    private void ApplyLut(Texture2D lut)
    {
        if (_colorLookup == null) return;

        _colorLookup.texture.value = lut;
        _colorLookup.texture.overrideState = true;
        _colorLookup.contribution.value = 1f;
        _colorLookup.contribution.overrideState = true;
    }

    private void ResolveVolume()
    {
        if (targetVolume == null)
            targetVolume = FindFirstObjectByType<Volume>();

        if (targetVolume == null)
        {
            GameObject volumeGO = new GameObject("LutManager_GlobalVolume");
	    volumeGO.transform.SetParent(transform);
            targetVolume = volumeGO.AddComponent<Volume>();
            targetVolume.isGlobal = true;
            targetVolume.priority = 1;
            targetVolume.profile = ScriptableObject.CreateInstance<VolumeProfile>();
        }

        // Always ensure ColorLookup exists — add it if missing
        if (!targetVolume.profile.TryGet(out _colorLookup))
            _colorLookup = targetVolume.profile.Add<ColorLookup>(true);

        if (_colorLookup == null)
            Debug.LogWarning("[LutManager] Failed to create ColorLookup override.");
    }
}
