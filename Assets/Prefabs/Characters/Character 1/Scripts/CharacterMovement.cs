using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class CharacterMovement : MonoBehaviour
{
    private CharacterController controller;
    private CharacterAnimationController animationController;

    [Header("Speeds")]
    public float walkSpeed = 2f;
    public float runSpeed = 6f;
    public float rotationSpeed = 15f;
    
    [Header("Physics")]
    public float gravity = -9.81f;
    public float jumpHeight = 1.5f;

    private Vector3 velocity;
    private bool isGrounded;

    private void Start()
    {
        controller = GetComponent<CharacterController>();
        animationController = GetComponent<CharacterAnimationController>();
    }

    private void Update()
    {
        isGrounded = controller.isGrounded;
        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f;
        }

        // Check sitting status from the animator before moving
        if (animationController != null && animationController.IsSitting()) return;

        HandleMovement();
        HandleJump();

        // Apply Gravity
        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }

    private void HandleMovement()
    {
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");

        Vector3 moveDirection = new Vector3(horizontal, 0, vertical).normalized;
        bool isRunning = Input.GetKey(KeyCode.LeftShift) && moveDirection.magnitude > 0.1f;
        float currentSpeed = isRunning ? runSpeed : walkSpeed;

        if (moveDirection.magnitude >= 0.1f)
        {
            // Rotation
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);

            // Translation
            controller.Move(moveDirection * currentSpeed * Time.deltaTime);
        }

        // Tell the animation controller how fast we are going
        float animatorSpeed = moveDirection.magnitude * (isRunning ? 5f : 1f);
        animationController.UpdateSpeed(animatorSpeed);
    }

    private void HandleJump()
    {
        if (Input.GetKeyDown(KeyCode.Space) && isGrounded)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
            animationController.TriggerJump(Input.GetKey(KeyCode.LeftShift));
        }
    }
}
