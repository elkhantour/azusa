using UnityEngine;

public class BikeController : MonoBehaviour
{

    [Header("Movement Settings")]
    public float moveSpeed = 10f;
    public float turnSpeed = 100f;
    public float acceleration = 5f;

    [Header("Leaning (The Juice)")]
    public Transform bikeVisuals; // Drag the mesh child here
    public float maxRoll = 20f;   // How much it leans into turns
    public float maxYaw = 5f;    // Slight "wobble" at high speed
    public float leanSmoothing = 5f;

    private float _currentSpeed;
    private float _targetRotation;
    private float _currentRoll;

    public Transform SeatAnchor;
    public bool IsOccupied { get; set; }

    void Update()
    {
        if (!IsOccupied) return;

        HandleMovement();
        HandleVisuals();
    }

    void HandleMovement()
    {
        float moveInput = Input.GetAxis("Vertical"); // W/S
        float turnInput = Input.GetAxis("Horizontal"); // A/D

        // 1. Acceleration/Deceleration
        float targetSpeed = moveInput * moveSpeed;
        _currentSpeed = Mathf.Lerp(_currentSpeed, targetSpeed, Time.deltaTime * acceleration);

        // 2. Translation
        transform.Translate(Vector3.forward * _currentSpeed * Time.deltaTime);

        // 3. Turning (Yaw) - Only turn if moving
        if (Mathf.Abs(_currentSpeed) > 0.1f)
        {
            // Reverse steering if moving backward
            float directionSign = Mathf.Sign(_currentSpeed);
            float turnAmount = turnInput * turnSpeed * Time.deltaTime * directionSign;
            transform.Rotate(Vector3.up, turnAmount);
        }
    }

    void HandleVisuals()
    {
        if (bikeVisuals == null) return;

        float turnInput = Input.GetAxis("Horizontal");
        float moveInput = Input.GetAxis("Vertical");

        // --- ROLL (Z-axis) ---
        // Lean into the turn based on horizontal input and speed
        float targetRoll = -turnInput * maxRoll * Mathf.Clamp01(Mathf.Abs(_currentSpeed) / 2);
        _currentRoll = Mathf.Lerp(_currentRoll, targetRoll, Time.deltaTime * leanSmoothing);

        // --- YAW WOBBLE (Y-axis) ---
        // Slight organic yaw offset when moving fast to simulate wind/road feel
        float speedFactor = Mathf.Clamp01(_currentSpeed / moveSpeed);
        float yawWobble = Mathf.Sin(Time.time * 10f) * speedFactor * maxYaw;

        // Apply to the visuals only (so the actual transform stays upright for collisions)
        bikeVisuals.localRotation = Quaternion.Euler(0, yawWobble, _currentRoll);
    }

    public void SetPhysicsEnabled(bool state)
    {
        IsOccupied = state;
        if (!state) _currentSpeed = 0; // Stop instantly on dismount
    }

}
