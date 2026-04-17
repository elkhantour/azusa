using UnityEngine;

public class CameraModeTopDown : CameraMode
{
    [Header("Target")]
    [SerializeField] private Transform target;

    [Header("Position")]
    [SerializeField] private float height = 15f;
    [SerializeField] private Vector3 offset = Vector3.zero;

    [Header("Zoom")]
    [SerializeField] private float zoomSpeed = 10f;
    [SerializeField] private float minHeight = 5f;
    [SerializeField] private float maxHeight = 40f;

    [Header("Smoothing")]
    [SerializeField] private float smoothSpeed = 10f;

    public override void Activate() { }

    public override void Deactivate() { }

    public override void Tick()
    {
        if (target == null) return;

        HandleZoom();

        Vector3 desiredPosition = target.position 
                                + Vector3.up * height 
                                + offset;

        transform.position = Vector3.Lerp(
            transform.position,
            desiredPosition,
            smoothSpeed * Time.deltaTime
        );

        transform.rotation = Quaternion.Euler(90f, 0f, 0f);
    }

    private void HandleZoom()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");

        if (Mathf.Abs(scroll) > 0.01f)
        {
            height -= scroll * zoomSpeed;
            height = Mathf.Clamp(height, minHeight, maxHeight);
        }
    }

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
    }
}
