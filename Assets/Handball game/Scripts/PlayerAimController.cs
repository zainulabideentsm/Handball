using UnityEngine;

[DisallowMultipleComponent]
public sealed class PlayerAimController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerBallPickup ballPickup;
    [SerializeField] private ThirdPersonCameraFollow followCamera;
    [SerializeField] private Transform aimPivot;

    [Header("Throw Elevation")]
    [SerializeField, Range(0f, 89f)]
    private float minimumThrowElevation = 18f;

    [SerializeField, Range(0f, 89f)]
    private float maximumThrowElevation = 82f;

    [Tooltip("Below 1 makes the trajectory rise faster during an upward swipe.")]
    [SerializeField, Range(0.25f, 2f)]
    private float verticalAimResponse = 0.7f;

    public bool IsAiming { get; private set; }

    public float ThrowElevation { get; private set; }

    public Vector3 AimDirection =>
        aimPivot != null
            ? aimPivot.forward
            : transform.forward;

    private void Update()
    {
        bool shouldAim =
            ballPickup != null &&
            ballPickup.HasBall;

        if (shouldAim != IsAiming)
        {
            SetAiming(shouldAim);
        }

        if (!IsAiming ||
            followCamera == null ||
            aimPivot == null)
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
            if (enabled)
            {
                followCamera.AlignYawTo(
                    transform.eulerAngles.y,
                    false
                );
            }

            followCamera.SetAimMode(enabled);
        }

        if (!enabled && aimPivot != null)
        {
            aimPivot.localRotation =
                Quaternion.identity;

            ThrowElevation = 0f;
        }
    }

    private void UpdateAimDirection()
    {
        // Horizontal camera drag rotates the character and trajectory.
        transform.rotation = Quaternion.Euler(
            0f,
            followCamera.Yaw,
            0f
        );

        float response = Mathf.Pow(
            followCamera.AimVertical01,
            verticalAimResponse
        );

        ThrowElevation = Mathf.Lerp(
            minimumThrowElevation,
            maximumThrowElevation,
            response
        );

        aimPivot.localRotation = Quaternion.Euler(
            -ThrowElevation,
            0f,
            0f
        );
    }
}