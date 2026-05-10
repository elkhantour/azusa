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

    [SerializeField] private CameraModeOrbit orbitMode;
    [SerializeField] private CameraModeTopDown topDownMode;

    private CameraMode currentMode;

    private void Awake()
    {
        _instance = this;
        SetMode(defaultMode);
    }

    public void SetMode(CameraModeType mode)
    {
        switch (mode)
        {
            case CameraModeType.Orbit:
                enabled = true;
                currentMode = orbitMode;
                break;

            case CameraModeType.TopDown:
                enabled = true;
                currentMode = topDownMode;
                break;

            case CameraModeType.Locked:
                enabled = false;
                break;
        }
    }

    private void LateUpdate()
    {
        currentMode.Tick();
    }

    public CameraState GetState()
    {
        return currentMode.state;
    }

}
