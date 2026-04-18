using UnityEngine;

public class CameraModeTopDown : CameraMode
{
    [Header("Target")]
    [SerializeField] private Transform target;

    [Header("Position")]
    [SerializeField] private float height = 15f;

    // Position influenced by look-at (relative to target)
    [SerializeField] private Vector3 position = new Vector3(0f, 0f, -10f);

    // Pure world-space offset (not influenced by rotation)
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

        // Always look at target first
        transform.LookAt(target);

        // Position relative to look direction
        Vector3 relativePosition = transform.rotation * position;

        Vector3 desiredPosition = 
            target.position +
            Vector3.up * height +
            relativePosition +
            offset;

        transform.position = Vector3.Lerp(
            transform.position,
            desiredPosition,
            smoothSpeed * Time.deltaTime
        );

        // Ensure we always look at target after moving
        transform.LookAt(target);
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
