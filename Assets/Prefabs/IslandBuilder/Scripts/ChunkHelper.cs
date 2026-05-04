using UnityEngine;
using System.Linq; // Added for easy filtering

public class ChunkHelper : MonoBehaviour
{
    private static readonly int ActiveProperty = Shader.PropertyToID("_Active");
    private static readonly int OpacityProperty = Shader.PropertyToID("_Opacity");

    [SerializeField] private bool isActive = false;

    private Renderer[] _groundRenderers;
    private MaterialPropertyBlock _propBlock;

    private void Awake()
    {
        // Find all renderers in children, but only keep those whose 
        // GameObject name contains "Ground" (case-insensitive)
        _groundRenderers = GetComponentsInChildren<Renderer>(true)
            .Where(r => r.gameObject.name.IndexOf("Ground", System.StringComparison.OrdinalIgnoreCase) >= 0)
            .ToArray();

        _propBlock = new MaterialPropertyBlock();

        if (_groundRenderers == null || _groundRenderers.Length == 0){
	    Debug.LogWarning("Couldn't find the Ground children.");
	    Destroy(gameObject);
	    return;
	}
	
        UpdateVisuals(false);
    }

    public void SetActive(bool state)
    {
        isActive = state;
        UpdateVisuals(false);
    }

    private void OnMouseEnter()
    {
        //UpdateVisuals(true);
    }

    private void OnMouseExit()
    {
        UpdateVisuals(false);
    }

    private void UpdateVisuals(bool isHovering)
    {
        // Prepare values based on your requirements:
        // Active: 1 or 0
        // Opacity: 1 (Hover) or 0.5 (Default)
        float activeVal = isActive ? 1.0f : 0.0f;
        float opacityVal = isHovering ? 1.0f : 0.5f;

        _propBlock.SetFloat(ActiveProperty, activeVal);
        _propBlock.SetFloat(OpacityProperty, opacityVal);

        foreach (Renderer ren in _groundRenderers)
        {
            if (ren != null)
            {
                ren.SetPropertyBlock(_propBlock);
            }
        }
    }
}
