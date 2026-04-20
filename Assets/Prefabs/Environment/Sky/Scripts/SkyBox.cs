using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SkyBox : MonoBehaviour
{
    public Material[] skyMaterials = new Material[] { };
    private int currentIndex = 0;

    private void OnEnable()
    {
        TimeManager.OnUpdateMoment += OnUpdateMoment;
        if (skyMaterials[currentIndex])
        {
            RenderSettings.skybox = skyMaterials[currentIndex];

            // Handle ambient mode: set the mode to Trilight (Gradient) once when starting the transition
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            UpdateEnvironmentColors(skyMaterials[currentIndex]);
        }

    }

    private void OnDisable()
    {
        TimeManager.OnUpdateMoment -= OnUpdateMoment;
    }


    private void OnUpdateMoment(int index, DayMoment dayMoment)
    {
        Material currentMaterial = skyMaterials[currentIndex];
        Material newMaterial = skyMaterials[index];

        if (newMaterial && currentMaterial && index != currentIndex)
        {
            currentIndex = index;
            StopCoroutine("SwitchSkyboxMaterial");
            StartCoroutine(SwitchSkyboxMaterial(currentMaterial, newMaterial, dayMoment.transitionTime));
        }

    }

    private IEnumerator SwitchSkyboxMaterial(Material startMaterial, Material endMaterial, float duration)
    {
        float time = 0;
        Material transitionMaterial = new Material(startMaterial);

        while (time < duration)
        {
            float lerpFactor = time / duration;
            transitionMaterial.Lerp(startMaterial, endMaterial, lerpFactor);
            RenderSettings.skybox = transitionMaterial;

            // --- AMBIENT LIGHTING UPDATE ---
            UpdateEnvironmentColors(transitionMaterial);

            time += Time.deltaTime;
            yield return null;
        }

        RenderSettings.skybox = endMaterial;
        UpdateEnvironmentColors(endMaterial);
    }

    /// <summary>
    /// Pulls gradient colors from the material and applies them to the scene's ambient lighting.
    /// </summary>
    private void UpdateEnvironmentColors(Material mat)
    {

        Debug.Log("Update render settings");

        // Use the parameter names from your shader
        Color sky = mat.GetColor("_AmbientSky");
        Color equator = mat.GetColor("_AmbientEquator");
        Color ground = mat.GetColor("_AmbientGround");
        float intensity = mat.GetFloat("_AmbientIntensity");

        RenderSettings.ambientSkyColor = sky * intensity;
        RenderSettings.ambientEquatorColor = equator * intensity;
        RenderSettings.ambientGroundColor = ground * intensity;
    }
}
