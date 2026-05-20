using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class RadioButton : MonoBehaviour
{
    // Plug-in components (e.g. RadioButtonSpriteSwap) register here via AddListener.
    public readonly List<UnityAction> OnSelected   = new();
    public readonly List<UnityAction> OnDeselected = new();

    private RadioGroup _group;

    public void Init(RadioGroup group)
    {
        _group = group;
    }

    public void OnClick()
    {
        _group.Select(this);
    }

    /// <summary>Called by RadioGroup — invokes all registered selected/deselected callbacks.</summary>
    public void SetSelected(bool selected)
    {
        List<UnityAction> callbacks = selected ? OnSelected : OnDeselected;
        foreach (UnityAction callback in callbacks)
            callback?.Invoke();
    }
}
