using UnityEngine;

[DisallowMultipleComponent]
public sealed class PlayerAnimationController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Animator animator;
    [SerializeField] private PlayerMovementController playerMovement;
    [SerializeField] private PlayerBallPickup ballPickup;
    [SerializeField] private ParticleSystem runningDustParticle;

    [Header("Locomotion Thresholds")]
    [SerializeField, Range(0f, 1f)] private float idleToWalkThreshold = 0.08f;
    [SerializeField, Range(0f, 1f)] private float walkToRunThreshold = 0.55f;
    [SerializeField, Range(0f, 1f)] private float runningPickupThreshold = 0.55f;
    [SerializeField, Min(0f)] private float locomotionDampTime = 0.06f;

    [Header("Walk Playback")]
    [SerializeField, Range(0.1f, 1.5f)] private float minimumWalkPlayback = 0.3f;
    [SerializeField, Range(0.1f, 1.5f)] private float maximumWalkPlayback = 1f;

    [Header("Run Playback")]
    [SerializeField, Range(0.1f, 2f)] private float minimumRunPlayback = 0.9f;
    [SerializeField, Range(0.1f, 2f)] private float maximumRunPlayback = 1.15f;

    [Header("Running Dust")]
    [SerializeField, Range(0f, 1f)] private float dustStartThreshold = 0.65f;
    [SerializeField, Range(0f, 1f)] private float dustStopThreshold = 0.48f;
    [SerializeField] private bool clearDustWhenStopped;

    private static readonly int SpeedHash = Animator.StringToHash("Speed");
    private static readonly int GroundedHash = Animator.StringToHash("Grounded");
    private static readonly int HasBallHash = Animator.StringToHash("HasBall");
    private static readonly int WalkPlaybackHash = Animator.StringToHash("WalkPlayback");
    private static readonly int RunPlaybackHash = Animator.StringToHash("RunPlayback");
    private static readonly int JumpHash = Animator.StringToHash("Jump");
    private static readonly int PickUpHash = Animator.StringToHash("PickUp");
    private static readonly int ThrowHash = Animator.StringToHash("Throw");
    private static readonly int DanceHash = Animator.StringToHash("Dance");

    private bool dustEmissionActive;

    public bool IsRunning =>
        playerMovement != null &&
        playerMovement.IsGrounded &&
        playerMovement.NormalizedSpeed >= runningPickupThreshold;

    private void Awake()
    {
        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }

        if (playerMovement == null)
        {
            playerMovement = GetComponent<PlayerMovementController>();
        }

        if (ballPickup == null)
        {
            ballPickup = GetComponent<PlayerBallPickup>();
        }

        if (runningDustParticle == null)
        {
            runningDustParticle = GetComponentInChildren<ParticleSystem>();
        }

        StopDustImmediately();

        if (animator != null)
        {
            animator.SetBool(HasBallHash, false);
        }
    }

    private void OnEnable()
    {
        if (playerMovement != null)
        {
            playerMovement.Jumped += HandleJumped;
        }
    }

    private void OnDisable()
    {
        if (playerMovement != null)
        {
            playerMovement.Jumped -= HandleJumped;
        }

        StopDustImmediately();
    }

    private void Update()
    {
        UpdateLocomotion();
        UpdateJumpState();
        UpdateBallState();
        UpdateRunningDust();
    }

    private void UpdateLocomotion()
    {
        if (animator == null || playerMovement == null)
        {
            return;
        }

        float normalizedSpeed = playerMovement.NormalizedSpeed;

        animator.SetFloat(SpeedHash, normalizedSpeed, locomotionDampTime, Time.deltaTime);

        UpdateWalkPlayback(normalizedSpeed);
        UpdateRunPlayback(normalizedSpeed);
    }

    private void UpdateJumpState()
    {
        if (animator == null || playerMovement == null)
        {
            return;
        }

        animator.SetBool(GroundedHash, playerMovement.IsGrounded);
    }

    private void UpdateBallState()
    {
        if (animator == null || ballPickup == null)
        {
            return;
        }

        animator.SetBool(HasBallHash, ballPickup.HasBall);
    }

    private void HandleJumped()
    {
        if (animator == null)
        {
            return;
        }

        animator.ResetTrigger(JumpHash);
        animator.SetTrigger(JumpHash);

        StopDust();
    }

    private void UpdateWalkPlayback(float normalizedSpeed)
    {
        float walkRange = Mathf.InverseLerp(idleToWalkThreshold, walkToRunThreshold, normalizedSpeed);
        float walkPlayback = Mathf.Lerp(minimumWalkPlayback, maximumWalkPlayback, walkRange);

        animator.SetFloat(WalkPlaybackHash, walkPlayback);
    }

    private void UpdateRunPlayback(float normalizedSpeed)
    {
        float runRange = Mathf.InverseLerp(walkToRunThreshold, 1f, normalizedSpeed);
        float runPlayback = Mathf.Lerp(minimumRunPlayback, maximumRunPlayback, runRange);

        animator.SetFloat(RunPlaybackHash, runPlayback);
    }

    private void UpdateRunningDust()
    {
        if (runningDustParticle == null || playerMovement == null)
        {
            return;
        }

        float normalizedSpeed = playerMovement.NormalizedSpeed;

        if (!dustEmissionActive)
        {
            bool shouldStart =
                playerMovement.IsGrounded &&
                playerMovement.IsMoving &&
                playerMovement.MovementEnabled &&
                normalizedSpeed >= dustStartThreshold;

            if (shouldStart)
            {
                StartDust();
            }

            return;
        }

        bool shouldStop =
            !playerMovement.IsGrounded ||
            !playerMovement.IsMoving ||
            !playerMovement.MovementEnabled ||
            normalizedSpeed <= dustStopThreshold;

        if (shouldStop)
        {
            StopDust();
        }
    }

    private void StartDust()
    {
        if (runningDustParticle == null)
        {
            return;
        }

        dustEmissionActive = true;
        runningDustParticle.Play(true);
    }

    private void StopDust()
    {
        if (runningDustParticle == null)
        {
            return;
        }

        dustEmissionActive = false;

        ParticleSystemStopBehavior stopBehavior = clearDustWhenStopped
            ? ParticleSystemStopBehavior.StopEmittingAndClear
            : ParticleSystemStopBehavior.StopEmitting;

        runningDustParticle.Stop(true, stopBehavior);
    }

    private void StopDustImmediately()
    {
        dustEmissionActive = false;

        if (runningDustParticle != null)
        {
            runningDustParticle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
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

        animator.ResetTrigger(ThrowHash);
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
        animator.ResetTrigger(JumpHash);
        animator.ResetTrigger(PickUpHash);
        animator.ResetTrigger(ThrowHash);
        animator.ResetTrigger(DanceHash);
    }

    private void OnValidate()
    {
        if (dustStopThreshold > dustStartThreshold)
        {
            dustStopThreshold = dustStartThreshold;
        }
    }
}