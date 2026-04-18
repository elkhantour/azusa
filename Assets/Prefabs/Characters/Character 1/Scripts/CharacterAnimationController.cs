using UnityEngine;

public class CharacterAnimationController : MonoBehaviour
{
    private Animator animator;

    [Header("Movement")]
    public float walkSpeedThreshold = 0.1f;
    public float runSpeedThreshold = 3f;

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

        // Calculate base input magnitude (0 to 1)
        float speed = new Vector2(horizontal, vertical).magnitude;

        // If holding Shift, boost the speed value sent to the Animator
        if (Input.GetKey(KeyCode.LeftShift))
        {
            speed *= 5f; // This pushes 'speed' above your runSpeedThreshold (3)
        }

        animator.SetFloat("Speed", speed);
    }
    
    private void HandleActions()
    {
        // Jump
        if (Input.GetKeyDown(KeyCode.Space))
        {
            animator.SetTrigger("Jump");
        }

        // Running Jump (Shift + Space)
        if (Input.GetKeyDown(KeyCode.Space) && Input.GetKey(KeyCode.LeftShift))
        {
            animator.SetTrigger("RunningJump");
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
