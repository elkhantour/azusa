using UnityEngine;

public class PlayerBikeInteractor : MonoBehaviour
{
    public float interactionRange = 2f;
    public KeyCode rideKey = KeyCode.E;

    private BikeController _currentBike;
    private bool _isRiding = false;

    // References to your existing scripts
    private PlayerMovement _movementScript;
    private PlayerAnimationController _controller;
    private Animator _animator;

    void Start()
    {
        _movementScript = GetComponent<PlayerMovement>();
        _controller = GetComponent<PlayerAnimationController>();
        _animator = GetComponent<Animator>();
    }

    void Update()
    {
        if (!_isRiding)
        {
            FindNearbyBike();
            if (_currentBike != null && Input.GetKeyDown(rideKey))
            {
                MountBike();
            }
        }
        else
        {
            if (Input.GetKeyDown(rideKey))
            {
                DismountBike();
            }
        }
    }

    void FindNearbyBike()
    {
        // Simple proximity check
        BikeController[] bikes = FindObjectsOfType<BikeController>();
        _currentBike = null;

        foreach (var bike in bikes)
        {
            if (Vector3.Distance(transform.position, bike.transform.position) < interactionRange)
            {
                _currentBike = bike;
                break;
            }
        }
    }

    void MountBike()
    {
        _isRiding = true;

        // 1. Disable human physics/movement
        _controller.enabled = false;
        _movementScript.enabled = false;

        // 2. Snap to bike
        transform.SetParent(_currentBike.SeatAnchor);
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;

        // 3. Animation
        _animator.SetTrigger("Bike"); // Match your Animator trigger name
	_animator.SetBool("IsBiking", true);
        // 4. Enable Bike
        _currentBike.IsOccupied = true;
        _currentBike.SetPhysicsEnabled(true);
    }

    void DismountBike()
    {
        _isRiding = false;

        // 1. Unparent
        transform.SetParent(null);

        // 2. Re-enable human physics
        _controller.enabled = true;
        _movementScript.enabled = true;

        // 3. Animation
	_animator.SetBool("IsBiking", false);

        // 4. Disable Bike
        _currentBike.IsOccupied = false;
        _currentBike.SetPhysicsEnabled(false);
        _currentBike = null;
    }
}
