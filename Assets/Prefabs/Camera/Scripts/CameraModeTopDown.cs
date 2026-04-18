using UnityEngine;

public class CameraModeTopDown : CameraMode
{
    [Header("Targets")]
    [SerializeField] private Transform target;
    [Tooltip("Offset the focus point (e.g., look at head instead of feet)")]
    [SerializeField] private Vector3 targetOffset = new Vector3(0f, 1.5f, 0f);
    
    [Header("Camera Positioning")]
    [Tooltip("Additional displacement from the pivot point (e.g., X=2 to move camera to the right)")]
    [SerializeField] private Vector3 cameraOffset = Vector3.zero;
    [SerializeField] private float verticalAngle = 45f;
    private float currentRotationAngle = 0f;

    [Header("Distance & Zoom")]
    [SerializeField] private float distance = 15f;
    [SerializeField] private float minDistance = 5f;
    [SerializeField] private float maxDistance = 40f;
    [SerializeField] private float zoomSpeed = 10f;

    [Header("Smoothing")]
    [SerializeField] private float smoothSpeed = 10f;

    public override void Activate() { }
    public override void Deactivate() { }

    public override void Tick()
    {
        if (target == null) return;

        HandleZoom();
        HandleRotation();

        // 1. The point the camera is 'orbiting'
        Vector3 focusPoint = target.position + targetOffset;

        // 2. The base rotation (Pitch from verticalAngle, Yaw from mouse input)
        Quaternion rotation = Quaternion.Euler(verticalAngle, currentRotationAngle, 0);

        // 3. Calculate Final Position
        // FocusPoint -> Apply Rotation -> Move back by Distance -> Apply side/up offset
        Vector3 direction = rotation * Vector3.forward;
        Vector3 sideOffset = rotation * cameraOffset; // Rotates the offset with the camera
        
        Vector3 desiredPosition = focusPoint - (direction * distance) + sideOffset;

        // 4. Smoothly move to position
        transform.position = Vector3.Lerp(
            transform.position,
            desiredPosition,
            smoothSpeed * Time.deltaTime
        );

        // 5. Look at the offset target point
        transform.LookAt(focusPoint);
    }

    private void HandleZoom()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) > 0.01f)
        {
            distance -= scroll * zoomSpeed;
            distance = Mathf.Clamp(distance, minDistance, maxDistance);
        }
    }

    private void HandleRotation()
    {
        if (Input.GetMouseButton(1)) // Hold Right Click to rotate
        {
            currentRotationAngle += Input.GetAxis("Mouse X") * 150f * Time.deltaTime;
        }
    }
}
