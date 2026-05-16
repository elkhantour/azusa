using UnityEngine;

public class ModeActiveObject : MonoBehaviour
{
    [SerializeField] private GameModeManager _modeManager;
    [SerializeField] private GameMode _mode;

    private void Awake()
    {
        _modeManager.OnModeChanged += OnModeChanged;
        OnModeChanged(_modeManager.CurrentMode);
    }

    private void OnModeChanged(GameMode mode)
    {
        gameObject.SetActive(mode == _mode);
    }
}
