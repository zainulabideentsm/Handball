using UnityEngine;

[DisallowMultipleComponent]
public sealed class PlayerAnimationController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Animator animator;
    [SerializeField] private PlayerMovement playerMovement;

    [Header("Settings")]
    [SerializeField, Range(0f, 1f)]
    private float runningPickupThreshold = 0.15f;

    [SerializeField, Min(0f)]
    private float locomotionDampTime = 0.1f;

    private static readonly int SpeedHash =
        Animator.StringToHash("Speed");

    private static readonly int PickUpHash =
        Animator.StringToHash("PickUp");

    private static readonly int RunningPickupHash =
        Animator.StringToHash("RunningPickup");

    private static readonly int ThrowHash =
        Animator.StringToHash("Throw");

    private static readonly int DanceHash =
        Animator.StringToHash("Dance");

    public bool IsRunning =>
        playerMovement != null &&
        playerMovement.NormalizedSpeed > runningPickupThreshold;

    private void Awake()
    {
        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }

        if (playerMovement == null)
        {
            playerMovement = GetComponent<PlayerMovement>();
        }
    }

    private void Update()
    {
        UpdateLocomotion();
    }

    private void UpdateLocomotion()
    {
        if (animator == null || playerMovement == null)
        {
            return;
        }

        animator.SetFloat( SpeedHash, playerMovement.NormalizedSpeed,locomotionDampTime,Time.deltaTime);
    }

    public void PlayPickup()
    {
        if (animator == null)
        {
            return;
        }

        ResetActionTriggers();

        if (IsRunning)
        {
            animator.SetTrigger(RunningPickupHash);
        }
        else
        {
            animator.SetTrigger(PickUpHash);
        }
    }

    public void PlayThrow()
    {
        if (animator == null)
        {
            return;
        }

        animator.ResetTrigger(DanceHash);
        animator.SetTrigger(ThrowHash);
    }

    public void PlayDance()
    {
        if (animator == null)
        {
            return;
        }

        ResetActionTriggers();
        animator.SetTrigger(DanceHash);
    }

    private void ResetActionTriggers()
    {
        animator.ResetTrigger(PickUpHash);
        animator.ResetTrigger(RunningPickupHash);
        animator.ResetTrigger(ThrowHash);
        animator.ResetTrigger(DanceHash);
    }
}