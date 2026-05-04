using UnityEngine;


public enum CameraState
{
    Rest,
    Rotate,
    Move,
    Locked,
}


public abstract class CameraMode : MonoBehaviour
{

    public CameraState state { get; set; }

    public abstract void Activate();
    public abstract void Deactivate();
    public abstract void Tick();
}
