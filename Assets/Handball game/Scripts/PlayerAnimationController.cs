using UnityEngine;

[DisallowMultipleComponent]
public sealed class PlayerAnimationController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Animator animator;
    [SerializeField] private PlayerMovement playerMovement;

    [Header("Locomotion Thresholds")]
    [SerializeField, Range(0f, 1f)]
    private float idleToWalkThreshold = 0.08f;

    [SerializeField, Range(0f, 1f)]
    private float walkToRunThreshold = 0.55f;

    [SerializeField, Range(0f, 1f)]
    private float runningPickupThreshold = 0.55f;

    [SerializeField, Min(0f)]
    private float locomotionDampTime = 0.1f;

    [Header("Walk Playback")]
    [SerializeField, Range(0.1f, 1.5f)]
    private float minimumWalkPlayback = 0.3f;

    [SerializeField, Range(0.1f, 1.5f)]
    private float maximumWalkPlayback = 1f;

    [Header("Run Playback")]
    [SerializeField, Range(0.1f, 2f)]
    private float minimumRunPlayback = 0.9f;

    [SerializeField, Range(0.1f, 2f)]
    private float maximumRunPlayback = 1.15f;

    private static readonly int SpeedHash =
        Animator.StringToHash("Speed");

    private static readonly int WalkPlaybackHash =
        Animator.StringToHash("WalkPlayback");

    private static readonly int RunPlaybackHash =
        Animator.StringToHash("RunPlayback");

    private static readonly int PickUpHash =
        Animator.StringToHash("PickUp");

    private static readonly int ThrowHash =
        Animator.StringToHash("Throw");

    private static readonly int DanceHash =
        Animator.StringToHash("Dance");

    public bool IsRunning =>
        playerMovement != null &&
        playerMovement.NormalizedSpeed >= runningPickupThreshold;

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

        float normalizedSpeed = playerMovement.NormalizedSpeed;
        float deltaTime = Time.deltaTime;

        animator.SetFloat(
            SpeedHash,
            normalizedSpeed,
            locomotionDampTime,
            deltaTime
        );

        UpdateWalkPlayback(normalizedSpeed);
        UpdateRunPlayback(normalizedSpeed);
    }

    private void UpdateWalkPlayback(float normalizedSpeed)
    {
        float walkRange = Mathf.InverseLerp(
            idleToWalkThreshold,
            walkToRunThreshold,
            normalizedSpeed
        );

        float walkPlayback = Mathf.Lerp(
            minimumWalkPlayback,
            maximumWalkPlayback,
            walkRange
        );

        animator.SetFloat(
            WalkPlaybackHash,
            walkPlayback
        );
    }

    private void UpdateRunPlayback(float normalizedSpeed)
    {
        float runRange = Mathf.InverseLerp(
            walkToRunThreshold,
            1f,
            normalizedSpeed
        );

        float runPlayback = Mathf.Lerp(
            minimumRunPlayback,
            maximumRunPlayback,
            runRange
        );

        animator.SetFloat(
            RunPlaybackHash,
            runPlayback
        );
    }

    public void PlayPickup()
    {
        if (animator == null)
        {
            return;
        }

        ResetActionTriggers();
        animator.SetTrigger(PickUpHash);
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
        animator.ResetTrigger(ThrowHash);
        animator.ResetTrigger(DanceHash);
    }
}