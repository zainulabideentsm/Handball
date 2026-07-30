using UnityEngine;

[DisallowMultipleComponent]
public sealed class PlayerThrowController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerBallPickup ballPickup;
    [SerializeField] private PlayerAimController aimController;
    [SerializeField] private PlayerAnimationController animationController;
    [SerializeField] private PlayerAudioController playerAudio;
    [SerializeField] private TrajectoryPreviewController trajectoryPreview;
    [SerializeField] private Transform throwOrigin;

    [Header("Ball Spin")]
    [SerializeField, Min(0f)] private float backspinSpeed = 8f;

    private BallController pendingThrowBall;
    private Vector3 pendingLaunchVelocity;
    private Vector3 pendingSpin;
    private bool isThrowing;

    private void Awake()
    {
        if (playerAudio == null)
        {
            playerAudio =
                GetComponentInParent<PlayerAudioController>();
        }
    }

    public void ThrowBall()
    {
        if (isThrowing ||
            ballPickup == null ||
            aimController == null ||
            animationController == null ||
            trajectoryPreview == null ||
            throwOrigin == null ||
            !ballPickup.HasBall ||
            !aimController.IsAiming)
        {
            return;
        }

        pendingThrowBall = ballPickup.HeldBall;

        pendingLaunchVelocity =
            aimController.AimDirection.normalized *
            trajectoryPreview.LaunchSpeed;

        pendingSpin = -transform.right * backspinSpeed;

        isThrowing = true;
        animationController.PlayThrow();
    }

    // Called by AE_ReleaseBall.
    public void ReleasePendingBall()
    {
        if (!isThrowing || pendingThrowBall == null)
        {
            return;
        }

        pendingThrowBall.transform.position =
            throwOrigin.position;

        pendingThrowBall.Throw(
            pendingLaunchVelocity,
            pendingSpin
        );

        playerAudio?.PlayThrow();
        GameManager.Instance?.Feedback?.PlayThrowFeedback();

        ballPickup.CompleteThrow();

        pendingThrowBall = null;
        pendingLaunchVelocity = Vector3.zero;
        pendingSpin = Vector3.zero;
        isThrowing = false;
    }

    private void OnDisable()
    {
        pendingThrowBall = null;
        pendingLaunchVelocity = Vector3.zero;
        pendingSpin = Vector3.zero;
        isThrowing = false;
    }
}