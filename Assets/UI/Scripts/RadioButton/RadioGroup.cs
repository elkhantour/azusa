using UnityEngine;
using System.Collections.Generic;
using System;

public class RadioGroup : MonoBehaviour
{
    [SerializeField]
    private List<RadioButton> _buttons = new();
    public event Action<RadioButton, RadioButton> OnSelectionChanged;

    [SerializeField]
    private bool _allowDeselect = true;

    private RadioButton _activeButton;

    private void Awake()
    {
        foreach (RadioButton button in _buttons)
        {
            button.Init(this);
        }
    }

    public void Add(RadioButton button)
    {
        _buttons.Add(button);
        button.Init(this);
    }

    public void Remove(RadioButton button)
    {
        _buttons.Remove(button);
    }

    public void Select(RadioButton target)
    {
        // Clicked active button
        if (_activeButton == target)
        {
            if (_allowDeselect)
            {
                _activeButton.SetSelected(false);
                _activeButton = null;
            }

            OnSelectionChanged?.Invoke(target, _activeButton);
            return;
        }

        // Disable previous
        if (_activeButton != null)
        {
            _activeButton.SetSelected(false);
        }

        // Enable new
        _activeButton = target;
        _activeButton.SetSelected(true);

        OnSelectionChanged?.Invoke(target, _activeButton);
    }

    public void DisableActive()
    {
        if (_activeButton != null)
        {
            _activeButton.SetSelected(false);
            _activeButton = null;
        }
    }
}
