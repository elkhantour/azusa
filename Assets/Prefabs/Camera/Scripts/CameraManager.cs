using UnityEngine;

public enum CameraModeType
{
    Orbit,
    TopDown
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
                currentMode = orbitMode;
                break;

            case CameraModeType.TopDown:
                currentMode = topDownMode;
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
