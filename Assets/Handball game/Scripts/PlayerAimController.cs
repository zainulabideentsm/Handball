using UnityEngine;

[DisallowMultipleComponent]
public sealed class PlayerAimController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerBallPickup ballPickup;
    [SerializeField] private ThirdPersonCameraFollow followCamera;
    [SerializeField] private Transform aimPivot;

    [Header("Throw Angle")]
    [Tooltip("Must match the camera's normal starting pitch.")]
    [SerializeField] private float neutralCameraPitch = 18f;

    [SerializeField, Range(0f, 89f)]
    private float maximumUpAngle = 45f;

    [SerializeField, Range(0f, 89f)]
    private float maximumDownAngle = 35f;

    public bool IsAiming { get; private set; }

    public float ThrowAngle { get; private set; }

    public Vector3 AimDirection
    {
        get
        {
            return aimPivot != null
                ? aimPivot.forward
                : transform.forward;
        }
    }

    private void Update()
    {
        bool shouldAim =
            ballPickup != null &&
            ballPickup.HasBall;

        if (shouldAim != IsAiming)
        {
            SetAiming(shouldAim);
        }

        if (!IsAiming || followCamera == null)
        {
            return;
        }

        UpdateAimDirection();
    }

    private void SetAiming(bool enabled)
    {
        IsAiming = enabled;

        if (followCamera != null)
        {
            followCamera.SetAimMode(enabled);
        }

        if (!enabled && aimPivot != null)
        {
            aimPivot.localRotation = Quaternion.identity;
            ThrowAngle = 0f;
        }
    }

    private void UpdateAimDirection()
    {
        // Character faces the camera's horizontal direction.
        transform.rotation = Quaternion.Euler(
            0f,
            followCamera.Yaw,
            0f
        );

        // Camera looking down produces a positive downward throw angle.
        // Camera looking up produces a negative upward throw angle.
        float cameraAngleFromNeutral =
            followCamera.Pitch - neutralCameraPitch;

        ThrowAngle = Mathf.Clamp(
            cameraAngleFromNeutral,
            -maximumUpAngle,
            maximumDownAngle
        );

        if (aimPivot != null)
        {
            aimPivot.localRotation = Quaternion.Euler(
                ThrowAngle,
                0f,
                0f
            );
        }
    }
}