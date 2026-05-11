using UnityEngine;

public class CameraModeOrbit : CameraMode
{
    public GameObject CameraObject;

    [Space]
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float zoomSpeed = 5f;
    [SerializeField] private float rotationSpeed = 2.0f;
    [Tooltip("Camera damping, 0.05 to 0.1 for more responsive feel, 0.3+ for heavier feel.")]
    [SerializeField] protected float smoothTime = 0.3f;

    [SerializeField] private bool restrictBelowFloor = true;
    [Tooltip("If enabled, camera tilts based on distance to ground as you zoom.")]
    [SerializeField] private bool enableTiltOnZoom = true;



    [Space]
    [Header("Vertical Orbit")]
    [Tooltip("Clamp the vertical orbit angle to avoid flipping")]
    [SerializeField] private float minPitchAngle = -80f;
    [SerializeField] private float maxPitchAngle = 80f;

    [Space]
    [Header("Angle")]
    [Tooltip("The object from which the camera hit test to define the rotation point (i.e. grid)")]
    [SerializeField] private Transform targetObject;
    [Tooltip("Minimum distance from the object")]
    [SerializeField] private float minDistance = 5f;
    [Tooltip("Maximum distance from the object")]
    [SerializeField] private float maxDistance = 20f;
    [Tooltip("Minimum rotation angle when close to the object")]
    [SerializeField] private float minAngle = 10f;
    [Tooltip("Maximum rotation angle when far from the object")]
    [SerializeField] private float maxAngle = 45f;

    // Orbit
    private Vector3 lastHitPoint;
    private float lastAngle = 0.0f;
    private float xSmooth = 0.0f;
    private float ySmooth = 0.0f;
    private float mouseX = 0.0f;
    private float mouseY = 0.0f;
    private float xVelo = 0.0f;
    private float yVelo = 0.0f;

    public override void Activate() { }
    public override void Deactivate() { }

    public override void Tick()
    {
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");
        MoveCamera(horizontal, vertical);

        float scrollWheel = Input.GetAxis("Mouse ScrollWheel");
        ZoomCamera(scrollWheel);

        Orbit();

        if (Input.GetMouseButtonDown(1))
            state = CameraState.Rotate;
        else if (Input.GetMouseButtonUp(1))
            state = CameraState.Rest;
    }

    private void MoveCamera(float horizontal, float vertical)
    {
        // Use the rig's yaw direction so movement is camera-facing but stays flat
        Vector3 forward = transform.forward;
        forward.y = 0f;
        forward.Normalize();

        Vector3 right = transform.right;
        right.y = 0f;
        right.Normalize();

        Vector3 moveDirection = (right * horizontal + forward * vertical).normalized;
        transform.Translate(moveDirection * moveSpeed * Time.deltaTime, Space.World);
    }

    private void ZoomCamera(float scrollWheel)
    {
        // --- Zoom movement ---
        transform.Translate(CameraObject.transform.forward * scrollWheel * zoomSpeed, Space.World);

        // --- Tilt on zoom ---
        if (!enableTiltOnZoom) return;

        float t = 0.0f;
        float targetAngle = 0.0f;
        float distance = 0.0f;

        if (targetObject != null)
        {
            RaycastHit hit;
            if (Physics.Raycast(transform.position, Vector3.down, out hit))
            {
                if (hit.transform.IsChildOf(targetObject))
                {
                    distance = Vector3.Distance(transform.position, hit.point);
                    distance = Mathf.Clamp(distance, minDistance, maxDistance);
                    t = Mathf.InverseLerp(minDistance, maxDistance, distance);
                    targetAngle = Mathf.Lerp(minAngle, maxAngle, t);
                }
            }
        }
        else
        {
            distance = transform.position.y;
            distance = Mathf.Clamp(distance, minDistance, maxDistance);
            t = Mathf.InverseLerp(minDistance, maxDistance, distance);
            targetAngle = Mathf.Lerp(minAngle, maxAngle, t);
        }

        Quaternion targetRotation = Quaternion.Euler(targetAngle, transform.eulerAngles.y, transform.eulerAngles.z);
        CameraObject.transform.rotation = Quaternion.Lerp(CameraObject.transform.rotation, targetRotation, Time.deltaTime * 5f);
    }

    private void Orbit()
    {
        if (lastAngle == transform.rotation.y && state == CameraState.Rest)
        {
            mouseX = 0.0f;
            mouseY = 0.0f;

            if (xSmooth > -0.001 && xSmooth < 0.001 && ySmooth > -0.001 && ySmooth < 0.001)
            {
                lastHitPoint = Vector3.zero;
                return;
            }
        }


        if (state == CameraState.Rotate)
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            Vector3 hitPoint = Vector3.zero;
            bool gotHit = false;

            if (targetObject != null)
            {
                RaycastHit hit;
                if (Physics.Raycast(ray, out hit))
                {
                    hitPoint = hit.point;
                    gotHit = true;
                }
            }
            else
            {
                // Intersect ray with the infinite y=0 plane
                // ray.direction.y must be non-zero (i.e. ray isn't parallel to the plane)
                if (Mathf.Abs(ray.direction.y) > 0.0001f)
                {
                    float t = -ray.origin.y / ray.direction.y;
                    if (t > 0f) // intersection is in front of the camera
                    {
                        hitPoint = ray.origin + ray.direction * t;
                        gotHit = true;
                    }
                }
            }

            if (gotHit)
            {
                if (lastHitPoint == Vector3.zero)
                    lastHitPoint = hitPoint;

                mouseX += Input.GetAxis("Mouse X") * rotationSpeed * 0.2f;
                mouseY -= Input.GetAxis("Mouse Y") * rotationSpeed * 0.2f;
            }
        }

        xSmooth = Mathf.SmoothDamp(xSmooth, mouseX, ref xVelo, smoothTime);
        ySmooth = Mathf.SmoothDamp(ySmooth, mouseY, ref yVelo, smoothTime);

        // Horizontal orbit around world up — unchanged
        transform.RotateAround(lastHitPoint, Vector3.up, xSmooth);

        // Vertical orbit around the rig's local right axis, clamped to avoid flipping
        float currentPitch = transform.eulerAngles.x;
        if (currentPitch > 180f) currentPitch -= 360f; // normalize to -180/180

        float clampedY = ySmooth;
        if ((currentPitch + clampedY) > maxPitchAngle) clampedY = maxPitchAngle - currentPitch;
        if ((currentPitch + clampedY) < minPitchAngle) clampedY = minPitchAngle - currentPitch;

        transform.RotateAround(lastHitPoint, transform.right, clampedY);

        lastAngle = transform.rotation.y;
    }

    private void LateUpdate()
    {
        if (restrictBelowFloor)
            RestrictBelowFloor();
    }

    private void RestrictBelowFloor()
    {
        float yPos = Mathf.Max(0f, transform.position.y);
        transform.position = new Vector3(transform.position.x, yPos, transform.position.z);
    }

    private float ClampAngle(float angle, float min, float max)
    {
        if (angle < -360) angle += 360;
        if (angle > 360) angle -= 360;
        return Mathf.Clamp(angle, min, max);
    }
}
