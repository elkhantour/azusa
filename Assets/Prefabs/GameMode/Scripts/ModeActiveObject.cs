using System.Collections.Generic;
using UnityEngine;

public class ModeActiveObject : MonoBehaviour
{
    [SerializeField] private GameModeManager _modeManager;
    [SerializeField] private List<GameMode> _activeModes;

    private void Awake()
    {
        _modeManager.OnModeChanged += OnModeChanged;
        OnModeChanged(_modeManager.CurrentMode);
    }

    private void OnModeChanged(GameMode mode)
    {
        gameObject.SetActive(_activeModes.Contains(mode));
    }
}
