using UnityEngine;

public enum CameraModeType
{
    Orbit,
    TopDown,
    CharacterEditor,
    Locked,
}


public class CameraManager : MonoBehaviour
{

    private static CameraManager _instance;

    public static CameraManager Instance
    {
        get
        {
            return _instance;
        }
    }

    [SerializeField] private GameModeManager _gameModeManager;

    [Header("Core")]
    [SerializeField] private CameraModeType defaultMode;

    [SerializeField] private CameraModeOrbit _orbitMode;
    [SerializeField] private CameraModeTopDown _topDownMode;
    [SerializeField] private CameraModeCharacterEditor _characterEditorMode;
    [SerializeField] private CameraModeLocked _lockedMode;

    private CameraMode currentMode;

    private void Awake()
    {
        _instance = this;
        SetMode(defaultMode);

        _gameModeManager.OnModeChanged += OnGameModeChanged;
        OnGameModeChanged(_gameModeManager.CurrentMode);

    }

    public void SetMode(CameraModeType mode)
    {
        currentMode = mode switch
        {
            CameraModeType.Orbit => _orbitMode,
            CameraModeType.TopDown => _topDownMode,
            CameraModeType.CharacterEditor => _characterEditorMode,
            CameraModeType.Locked => _lockedMode,
            _ => null
        };
    }

    public void FreezeZoom()
    {
        _orbitMode.FreezeZoom();
    }

    public void UnfreezeZoom()
    {
        _orbitMode.UnfreezeZoom();
    }


    private void LateUpdate()
    {
        currentMode?.Tick();
    }

    public CameraState GetState()
    {
        return currentMode.state;
    }

    public void OnGameModeChanged(GameMode mode)
    {

        switch (mode)
        {

            case GameMode.Play:
                SetMode(CameraModeType.TopDown);
                break;


            case GameMode.Build:
                SetMode(CameraModeType.Orbit);
                break;

            case GameMode.CharacterEditor:
                SetMode(CameraModeType.CharacterEditor);
                break;

            default:
                break;

        }

    }

}
