using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Static camera that lerps between its initial world position (zoom = 0)
/// and the zoom target (zoom = 1) based on scroll wheel input.
/// </summary>
public class CameraModeCharacterEditor : CameraMode
{
    // ── Inspector ────────────────────────────────────────────────────

    [Header("Zoom Target")]
    [Tooltip("The point the camera moves toward at full zoom (e.g. character chest).")]
    public Transform zoomTarget;

    [Header("Zoom Settings")]
    [Tooltip("How much each scroll tick moves the zoom t value.")]
    [Range(0.01f, 0.5f)]
    public float scrollStep = 0.1f;

    [Header("Smoothing")]
    [Tooltip("Higher = snappier. Lower = floatier.")]
    [Range(1f, 30f)]
    public float zoomDamping = 8f;

    // ── Private state ────────────────────────────────────────────────

    private Vector3 _originPosition;  // camera position at Activate() → t = 0
    private float _targetT;         // where scroll wants us to be  [0, 1]
    private float _currentT;        // smoothed value driving the lerp

    // ── CameraMode contract ──────────────────────────────────────────

    private void Awake()
    {

        _originPosition = transform.position;   // anchor the "fully zoomed out" point
        _currentT = 0f;
        _targetT = 0f;

        state = CameraState.Rest;
    }

    public override void Activate() { }

    public override void Deactivate()
    {
        state = CameraState.Locked;
    }

    public override void Tick()
    {

        // Cancel if hovering UI
        if (EventSystem.current.IsPointerOverGameObject())
            return;

        HandleScrollInput();
        ApplyZoom();
    }

    // ── Input ────────────────────────────────────────────────────────

    private void HandleScrollInput()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Approximately(scroll, 0f)) return;

        // Scroll up → zoom in (t toward 1), scroll down → zoom out (t toward 0)
        _targetT = Mathf.Clamp01(_targetT + scroll * scrollStep);

        state = CameraState.Move;
    }

    // ── Zoom ─────────────────────────────────────────────────────────

    private void ApplyZoom()
    {
        _currentT = Mathf.Lerp(_currentT, _targetT, Time.deltaTime * zoomDamping);

        transform.position = Vector3.Lerp(_originPosition, zoomTarget.position, _currentT);

        // Snap and settle
        if (Mathf.Abs(_currentT - _targetT) < 0.0001f)
        {
            _currentT = _targetT;
            state = CameraState.Rest;
        }
    }

}
