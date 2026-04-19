using UnityEngine;

/**
 * Handles the business logic for the player animation state.
 * THe key and movement part is handled by CharacterMovement
 */
public class CharacterAnimationController : MonoBehaviour
{
    private Animator animator;

    private void Start()
    {
        animator = GetComponent<Animator>();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.C))
        {
            ToggleSit();
        }
    }

    public void UpdateSpeed(float speed)
    {
        animator.SetFloat("Speed", speed);
    }

    public void TriggerJump(bool isRunning)
    {
        if (isRunning)
            animator.SetTrigger("RunningJump");
        else
            animator.SetTrigger("Jump");
    }

    private void ToggleSit()
    {
        bool isSitting = !animator.GetBool("IsSitting");
        animator.SetBool("IsSitting", isSitting);
        animator.SetTrigger("Sit");
    }

    public bool IsSitting()
    {
        return animator.GetBool("IsSitting");
    }
}
