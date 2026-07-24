using UnityEngine;

[DisallowMultipleComponent]
public sealed class PlayerThrowController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerBallPickup ballPickup;
    [SerializeField] private PlayerAimController aimController;
    [SerializeField] private TrajectoryPreviewController trajectoryPreview;
    [SerializeField] private Transform throwOrigin;

    [Header("Ball Spin")]
    [SerializeField, Min(0f)] private float backspinSpeed = 8f;

    private bool isThrowing;

    public void ThrowBall()
    {
        if (isThrowing ||
            ballPickup == null ||
            aimController == null ||
            trajectoryPreview == null ||
            throwOrigin == null ||
            !ballPickup.HasBall ||
            !aimController.IsAiming)
        {
            return;
        }

        isThrowing = true;

        BallController ball = ballPickup.HeldBall;

        // Start the real ball from the same point as the trajectory.
        ball.transform.position = throwOrigin.position;

        Vector3 launchVelocity =
            aimController.AimDirection.normalized *
            trajectoryPreview.LaunchSpeed;

        Vector3 backspin =
            -transform.right * backspinSpeed;

        ball.Throw(launchVelocity, backspin);

        ballPickup.CompleteThrow();

        isThrowing = false;
    }
}