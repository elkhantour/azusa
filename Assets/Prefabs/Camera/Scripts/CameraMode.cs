using UnityEngine;


public enum CameraState
{
    REST,
    ROTATE,
    MOVE
}


public abstract class CameraMode : MonoBehaviour
{

    public CameraState state;

    public abstract void Activate();
    public abstract void Deactivate();
    public abstract void Tick();
}
