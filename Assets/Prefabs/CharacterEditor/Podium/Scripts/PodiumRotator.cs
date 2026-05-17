using UnityEngine;

/// <summary>
/// Attach to your podium (or a parent object containing both the podium and character).
/// Rotates on the Y axis when the user clicks and drags horizontally.
/// Uses angular damping for smooth, organic deceleration.
/// </summary>
public class PodiumRotator : MonoBehaviour
{
    [Header("Rotation")]
    [Tooltip("How fast the podium rotates per pixel dragged.")]
    public float dragSensitivity = 0.4f;

    [Tooltip("How quickly the spin decelerates after releasing the mouse (0 = instant stop, 1 = never stops).")]
    [Range(0f, 0.99f)]
    public float dampingFactor = 0.92f;

    [Tooltip("Minimum angular velocity before it is snapped to zero (prevents infinite micro-spinning).")]
    public float velocityThreshold = 0.05f;

    // ── Private state ────────────────────────────────────────────────
    private float _angularVelocity;   // degrees / frame
    private float _lastMouseX;
    private bool  _isDragging;

    // ── Unity callbacks ──────────────────────────────────────────────

    private void Update()
    {
        HandleInput();
        ApplyRotation();
        ApplyDamping();
    }

    // ── Input ────────────────────────────────────────────────────────

    private void HandleInput()
    {
        if (Input.GetMouseButtonDown(0) && IsPointerOverPodium())
        {
            _isDragging   = true;
            _lastMouseX   = Input.mousePosition.x;
            _angularVelocity = 0f;          // kill existing spin on fresh grab
        }

        if (Input.GetMouseButton(0) && _isDragging)
        {
            float deltaX = Input.mousePosition.x - _lastMouseX;

            // Negative so dragging RIGHT spins the podium counter-clockwise
            // (feels natural — like pushing the near edge of a turntable).
            // Flip the sign if you prefer the opposite convention.
            _angularVelocity = -deltaX * dragSensitivity;

            _lastMouseX = Input.mousePosition.x;
        }

        if (Input.GetMouseButtonUp(0))
        {
            _isDragging = false;
            // _angularVelocity keeps its last value → coasts to a stop via damping
        }
    }

    // ── Rotation ─────────────────────────────────────────────────────

    private void ApplyRotation()
    {
        if (Mathf.Abs(_angularVelocity) < velocityThreshold && !_isDragging)
            return;

        transform.Rotate(Vector3.up, _angularVelocity, Space.World);
    }

    private void ApplyDamping()
    {
        if (_isDragging) return;                    // no damping while the user is in control

        _angularVelocity *= dampingFactor;

        if (Mathf.Abs(_angularVelocity) < velocityThreshold)
            _angularVelocity = 0f;
    }

    // ── Hit-test (optional but recommended) ─────────────────────────

    /// <summary>
    /// Returns true if the mouse cursor is currently over this GameObject
    /// (requires a Collider on the podium or its children).
    /// If you don't need hit-testing, replace with `return true`.
    /// </summary>
    private bool IsPointerOverPodium()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        return Physics.Raycast(ray, out RaycastHit hit) && hit.transform.IsChildOf(transform);
    }
}
