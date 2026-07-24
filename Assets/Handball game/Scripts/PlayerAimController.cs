using UnityEngine;

[DisallowMultipleComponent]
public sealed class PlayerAimController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerBallPickup ballPickup;
    [SerializeField] private ThirdPersonCameraFollow followCamera;
    [SerializeField] private Transform aimPivot;

    [Header("Throw Angle")]
    [SerializeField] private float throwAngleOffset = 0f;
    [SerializeField, Range(0f, 89f)] private float minimumThrowAngle = 5f;
    [SerializeField, Range(0f, 89f)] private float maximumThrowAngle = 65f;

    public bool IsAiming { get; private set; }

    public Vector3 AimDirection =>
        aimPivot != null
            ? aimPivot.forward
            : transform.forward;

    public float ThrowAngle { get; private set; }

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
    }

    private void UpdateAimDirection()
    {
        // Face the same horizontal direction as the camera.
        transform.rotation = Quaternion.Euler(
            0f,
            followCamera.Yaw,
            0f
        );

        ThrowAngle = Mathf.Clamp(
            followCamera.Pitch + throwAngleOffset,
            minimumThrowAngle,
            maximumThrowAngle
        );

        if (aimPivot != null)
        {
            aimPivot.localRotation = Quaternion.Euler(
                -ThrowAngle,
                0f,
                0f
            );
        }
    }
}