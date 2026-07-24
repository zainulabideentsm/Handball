using UnityEngine;

[DisallowMultipleComponent]
public sealed class ThirdPersonCameraFollow : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform target;
    [SerializeField] private MobileLookArea lookArea;

    [Header("Distance")]
    [SerializeField, Min(0.1f)] private float normalDistance = 4.5f;
    [SerializeField, Min(0.1f)] private float aimDistance = 4f;

    [Header("Look Target")]
    [SerializeField]
    private Vector3 lookOffset =
        new Vector3(0f, 0.5f, 0f);

    [Header("Orbit")]
    [SerializeField] private float startingPitch = 18f;
    [SerializeField] private float minimumPitch = 5f;
    [SerializeField] private float maximumPitch = 55f;

    [Header("Touch Sensitivity")]
    [SerializeField, Min(1f)] private float yawDegreesPerScreen = 180f;
    [SerializeField, Min(1f)] private float pitchDegreesPerScreen = 80f;
    [SerializeField] private bool invertVertical;

    [Header("Smoothing")]
    [SerializeField, Range(0.01f, 0.3f)]
    private float orbitSmoothTime = 0.07f;

    [SerializeField, Min(0.01f)]
    private float positionSharpness = 18f;

    public float Yaw => currentYaw;
    public float Pitch => currentPitch;

    private Transform cachedTransform;
    private bool isAimMode;

    private float targetYaw;
    private float targetPitch;

    private float currentYaw;
    private float currentPitch;

    private float yawSmoothVelocity;
    private float pitchSmoothVelocity;

    private int cachedScreenWidth;
    private int cachedScreenHeight;

    private float inverseScreenWidth;
    private float inverseScreenHeight;

    private void Awake()
    {
        cachedTransform = transform;

        targetYaw = target != null
            ? target.eulerAngles.y
            : transform.eulerAngles.y;

        targetPitch = startingPitch;

        currentYaw = targetYaw;
        currentPitch = targetPitch;

        RefreshScreenMetrics();
    }

    private void Start()
    {
        SnapToTarget();
    }

    private void Update()
    {
        ReadLookInput();
    }

    private void LateUpdate()
    {
        if (target == null)
        {
            return;
        }

        float deltaTime = Time.deltaTime;

        currentYaw = Mathf.SmoothDampAngle(
            currentYaw,
            targetYaw,
            ref yawSmoothVelocity,
            orbitSmoothTime,
            Mathf.Infinity,
            deltaTime
        );

        currentPitch = Mathf.SmoothDampAngle(
            currentPitch,
            targetPitch,
            ref pitchSmoothVelocity,
            orbitSmoothTime,
            Mathf.Infinity,
            deltaTime
        );

        float distance = isAimMode
            ? aimDistance
            : normalDistance;

        Quaternion orbitRotation = Quaternion.Euler(
            currentPitch,
            currentYaw,
            0f
        );

        Vector3 desiredPosition =
            target.position +
            orbitRotation * Vector3.back * distance;

        float positionBlend =
            1f - Mathf.Exp(-positionSharpness * deltaTime);

        cachedTransform.position = Vector3.Lerp(
            cachedTransform.position,
            desiredPosition,
            positionBlend
        );

        Vector3 lookPosition =
            target.position + lookOffset;

        Vector3 lookDirection =
            lookPosition - cachedTransform.position;

        if (lookDirection.sqrMagnitude > 0.0001f)
        {
            // No second rotation smoothing.
            // Smoothed orbit angles already provide smooth rotation.
            cachedTransform.rotation = Quaternion.LookRotation(
                lookDirection,
                Vector3.up
            );
        }
    }

    private void ReadLookInput()
    {
        if (lookArea == null)
        {
            return;
        }

        CheckScreenSize();

        Vector2 dragDelta = lookArea.ConsumeDelta();

        if (dragDelta.sqrMagnitude <= 0f)
        {
            return;
        }

        targetYaw +=
            dragDelta.x *
            inverseScreenWidth *
            yawDegreesPerScreen;

        float verticalMultiplier =
            invertVertical ? -1f : 1f;

        targetPitch +=
            dragDelta.y *
            inverseScreenHeight *
            pitchDegreesPerScreen *
            verticalMultiplier;

        targetPitch = Mathf.Clamp(
            targetPitch,
            minimumPitch,
            maximumPitch
        );

        // Prevent unnecessarily large accumulated angles.
        if (targetYaw > 360f || targetYaw < -360f)
        {
            targetYaw %= 360f;
            currentYaw %= 360f;
        }
    }

    public void SetAimMode(bool enabled)
    {
        isAimMode = enabled;
    }

    public void SnapToTarget()
    {
        if (target == null)
        {
            return;
        }

        currentYaw = targetYaw;
        currentPitch = targetPitch;

        yawSmoothVelocity = 0f;
        pitchSmoothVelocity = 0f;

        float distance = isAimMode
            ? aimDistance
            : normalDistance;

        Quaternion orbitRotation = Quaternion.Euler(
            currentPitch,
            currentYaw,
            0f
        );

        cachedTransform.position =
            target.position +
            orbitRotation * Vector3.back * distance;

        Vector3 lookDirection =
            target.position +
            lookOffset -
            cachedTransform.position;

        if (lookDirection.sqrMagnitude > 0.0001f)
        {
            cachedTransform.rotation = Quaternion.LookRotation(
                lookDirection,
                Vector3.up
            );
        }
    }

    private void CheckScreenSize()
    {
        if (Screen.width != cachedScreenWidth ||
            Screen.height != cachedScreenHeight)
        {
            RefreshScreenMetrics();
        }
    }

    private void RefreshScreenMetrics()
    {
        cachedScreenWidth = Mathf.Max(1, Screen.width);
        cachedScreenHeight = Mathf.Max(1, Screen.height);

        inverseScreenWidth = 1f / cachedScreenWidth;
        inverseScreenHeight = 1f / cachedScreenHeight;
    }
}