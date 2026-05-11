using UnityEngine;

public enum CameraModeType
{
    Orbit,
    TopDown,
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

    [Header("Core")]
    [SerializeField] private CameraModeType defaultMode;

    [SerializeField] private CameraModeOrbit _orbitMode;
    [SerializeField] private CameraModeTopDown _topDownMode;
    [SerializeField] private CameraModeLocked _lockedMode;

    private CameraMode currentMode;

    private void Awake()
    {
        _instance = this;
        SetMode(defaultMode);
    }

    public void SetMode(CameraModeType mode)
    {
        currentMode = mode switch
        {
            CameraModeType.Orbit => _orbitMode,
            CameraModeType.TopDown => _topDownMode,
            CameraModeType.Locked => _lockedMode,
            _ => null
        };
    }

    private void LateUpdate()
    {
	Debug.Log(currentMode);
        currentMode?.Tick();
    }

    public CameraState GetState()
    {
        return currentMode.state;
    }

}
