using UnityEngine;

public class CameraModeOrbit : CameraMode
{
    public GameObject CameraObject;

    [Space]
    [Header("Movement")]
    [SerializeField] private float _moveSpeed = 5f;
    [SerializeField] private float _zoomSpeed = 5f;
    [SerializeField] private float _rotationSpeed = 2.0f;
    [Tooltip("Camera damping, 0.05 to 0.1 for more responsive feel, 0.3+ for heavier feel.")]
    [SerializeField] protected float _smoothTime = 0.3f;

    [SerializeField] private bool _restrictBelowFloor = true;
    [Tooltip("If enabled, camera tilts based on distance to ground as you zoom.")]
    [SerializeField] private bool _enableTiltOnZoom = true;



    [Space]
    [Header("Vertical Orbit")]
    [Tooltip("Clamp the vertical orbit angle to avoid flipping")]
    [SerializeField] private float _minPitchAngle = -80f;
    [SerializeField] private float _maxPitchAngle = 80f;

    [Space]
    [Header("Angle")]
    [Tooltip("The object from which the camera hit test to define the rotation point (i.e. grid)")]
    [SerializeField] private Transform _targetObject;
    [Tooltip("Minimum distance from the object")]
    [SerializeField] private float _minDistance = 5f;
    [Tooltip("Maximum distance from the object")]
    [SerializeField] private float _maxDistance = 20f;
    [Tooltip("Minimum rotation angle when close to the object")]
    [SerializeField] private float _minAngle = 10f;
    [Tooltip("Maximum rotation angle when far from the object")]
    [SerializeField] private float _maxAngle = 45f;

    // Orbit
    private Vector3 _lastHitPoint;
    private float _lastAngle = 0.0f;
    private float _xSmooth = 0.0f;
    private float _ySmooth = 0.0f;
    private float _mouseX = 0.0f;
    private float _mouseY = 0.0f;
    private float _xVelo = 0.0f;
    private float _yVelo = 0.0f;
    private bool _freezeZoom = false;
    private bool _freezeOrbit = false;
    private bool _freezeMovement = false;

    public override void Activate() { }
    public override void Deactivate() { }

    public void FreezeMovement() { _freezeMovement = true; }
    public void UnfreezeMovement() { _freezeMovement = false; }

    public void FreezeZoom() { _freezeZoom = true; }
    public void UnfreezeZoom() { _freezeZoom = false; }

    public void FreezeOrbit() { _freezeOrbit = true; }
    public void UnfreezeOrbit() { _freezeOrbit = false; }

    public override void Tick()
    {
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");
        if (!_freezeMovement)
            MoveCamera(horizontal, vertical);

        float scrollWheel = Input.GetAxis("Mouse ScrollWheel");

        if (!_freezeZoom)
            ZoomCamera(scrollWheel);

        if (!_freezeOrbit)
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
        transform.Translate(moveDirection * _moveSpeed * Time.deltaTime, Space.World);
    }

    private void ZoomCamera(float scrollWheel)
    {
        // --- Zoom movement ---
        transform.Translate(CameraObject.transform.forward * scrollWheel * _zoomSpeed, Space.World);

        // --- Tilt on zoom ---
        if (!_enableTiltOnZoom) return;

        float t = 0.0f;
        float targetAngle = 0.0f;
        float distance = 0.0f;

        if (_targetObject != null)
        {
            RaycastHit hit;
            if (Physics.Raycast(transform.position, Vector3.down, out hit))
            {
                if (hit.transform.IsChildOf(_targetObject))
                {
                    distance = Vector3.Distance(transform.position, hit.point);
                    distance = Mathf.Clamp(distance, _minDistance, _maxDistance);
                    t = Mathf.InverseLerp(_minDistance, _maxDistance, distance);
                    targetAngle = Mathf.Lerp(_minAngle, _maxAngle, t);
                }
            }
        }
        else
        {
            distance = transform.position.y;
            distance = Mathf.Clamp(distance, _minDistance, _maxDistance);
            t = Mathf.InverseLerp(_minDistance, _maxDistance, distance);
            targetAngle = Mathf.Lerp(_minAngle, _maxAngle, t);
        }

        Quaternion targetRotation = Quaternion.Euler(targetAngle, transform.eulerAngles.y, transform.eulerAngles.z);
        CameraObject.transform.rotation = Quaternion.Lerp(CameraObject.transform.rotation, targetRotation, Time.deltaTime * 5f);
    }

    private void Orbit()
    {
        if (_lastAngle == transform.rotation.y && state == CameraState.Rest)
        {
            _mouseX = 0.0f;
            _mouseY = 0.0f;

            if (_xSmooth > -0.001 && _xSmooth < 0.001 && _ySmooth > -0.001 && _ySmooth < 0.001)
            {
                _lastHitPoint = Vector3.zero;
                return;
            }
        }


        if (state == CameraState.Rotate)
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            Vector3 hitPoint = Vector3.zero;
            bool gotHit = false;

            if (_targetObject != null)
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
                if (_lastHitPoint == Vector3.zero)
                    _lastHitPoint = hitPoint;

                _mouseX += Input.GetAxis("Mouse X") * _rotationSpeed * 0.2f;
                _mouseY -= Input.GetAxis("Mouse Y") * _rotationSpeed * 0.2f;
            }
        }

        _xSmooth = Mathf.SmoothDamp(_xSmooth, _mouseX, ref _xVelo, _smoothTime);
        _ySmooth = Mathf.SmoothDamp(_ySmooth, _mouseY, ref _yVelo, _smoothTime);

        // Horizontal orbit around world up — unchanged
        transform.RotateAround(_lastHitPoint, Vector3.up, _xSmooth);

        // Vertical orbit around the rig's local right axis, clamped to avoid flipping
        float currentPitch = transform.eulerAngles.x;
        if (currentPitch > 180f) currentPitch -= 360f; // normalize to -180/180

        float clampedY = _ySmooth;
        if ((currentPitch + clampedY) > _maxPitchAngle) clampedY = _maxPitchAngle - currentPitch;
        if ((currentPitch + clampedY) < _minPitchAngle) clampedY = _minPitchAngle - currentPitch;

        transform.RotateAround(_lastHitPoint, transform.right, clampedY);

        _lastAngle = transform.rotation.y;
    }

    private void LateUpdate()
    {
        if (_restrictBelowFloor)
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
