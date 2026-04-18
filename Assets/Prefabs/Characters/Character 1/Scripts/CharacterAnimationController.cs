using UnityEngine;

public class CharacterAnimationController : MonoBehaviour
{
    private Animator animator;

    [Header("Movement")]
    public float walkSpeedThreshold = 0.1f;
    public float runSpeedThreshold = 3f;
    public float rotationSpeed = 10f; // How fast the character turns

    private void Start()
    {
        animator = GetComponent<Animator>();
    }

    private void Update()
    {
        HandleMovement();
        HandleActions();
    }

    private void HandleMovement()
    {
        float vertical = Input.GetAxis("Vertical");
        float horizontal = Input.GetAxis("Horizontal");

        // 1. Calculate the direction vector relative to the world
        Vector3 moveDirection = new Vector3(horizontal, 0, vertical).normalized;

        // 2. Rotate the character if there is movement input
        if (moveDirection.magnitude >= 0.1f)
        {
            // Calculate the target rotation
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
            
            // Smoothly rotate toward that target
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }

        // 3. Handle Animator Speed
        float speed = new Vector2(horizontal, vertical).magnitude;
        if (Input.GetKey(KeyCode.LeftShift))
        {
            speed *= 5f;
        }

        animator.SetFloat("Speed", speed);
    }

    private void HandleActions()
    {
        // Jump Logic (Optimized)
        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (Input.GetKey(KeyCode.LeftShift))
            {
                animator.SetTrigger("RunningJump");
            }
            else
            {
                animator.SetTrigger("Jump");
            }
        }

        // Sit toggle
        if (Input.GetKeyDown(KeyCode.C))
        {
            bool sitting = animator.GetBool("IsSitting");
            animator.SetBool("IsSitting", !sitting);
            animator.SetTrigger("Sit");
        }
    }
}
