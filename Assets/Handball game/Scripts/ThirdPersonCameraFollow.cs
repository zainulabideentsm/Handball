using UnityEngine;

[DisallowMultipleComponent]
public sealed class ThirdPersonCameraFollow : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform target;
    [SerializeField] private MobileLookArea lookArea;

    [Header("Normal Camera")]
    [SerializeField, Min(0.1f)]
    private float normalDistance = 4.5f;

    [Tooltip("X = right/left, Y = up/down, Z = forward/back.")]
    [SerializeField]
    private Vector3 normalPositionOffset = Vector3.zero;

    [SerializeField]
    private Vector3 normalLookOffset =
        new Vector3(0f, 0.5f, 0f);

    [Tooltip("Extra visual rotation. Does not change camera position.")]
    [SerializeField]
    private Vector3 normalRotationOffset = Vector3.zero;

    [Header("Aim Camera")]
    [SerializeField, Min(0.1f)]
    private float aimDistance = 3.5f;

    [Tooltip("X = right/left, Y = up/down, Z = forward/back.")]
    [SerializeField]
    private Vector3 aimPositionOffset =
        new Vector3(0.8f, -0.35f, 0f);

    [SerializeField]
    private Vector3 aimLookOffset =
        new Vector3(0f, 0.8f, 0.8f);

    [Tooltip("Extra visual rotation. Does not change camera position.")]
    [SerializeField]
    private Vector3 aimRotationOffset =
        new Vector3(-3f, 0f, 0f);

    [Header("Orbit")]
    [SerializeField]
    private float startingPitch = 18f;

    [SerializeField]
    private float minimumPitch = 5f;

    [SerializeField]
    private float maximumPitch = 55f;

    [Header("Touch Sensitivity")]
    [SerializeField, Min(1f)]
    private float yawDegreesPerScreen = 180f;

    [SerializeField, Min(1f)]
    private float pitchDegreesPerScreen = 80f;

    [SerializeField]
    private bool invertVertical;

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

        UpdateOrbit(deltaTime);
        UpdateCameraPosition(deltaTime);
        UpdateCameraRotation();
    }

    private void UpdateOrbit(float deltaTime)
    {
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
    }

    private void UpdateCameraPosition(float deltaTime)
    {
        float distance = isAimMode
            ? aimDistance
            : normalDistance;

        Vector3 positionOffset = isAimMode
            ? aimPositionOffset
            : normalPositionOffset;

        Quaternion orbitRotation = Quaternion.Euler(
            currentPitch,
            currentYaw,
            0f
        );

        // Position offset follows horizontal camera rotation only.
        // Looking up/down will not move the shoulder offset.
        Quaternion yawRotation = Quaternion.Euler(
            0f,
            currentYaw,
            0f
        );

        Vector3 desiredPosition =
            target.position +
            orbitRotation * Vector3.back * distance +
            yawRotation * positionOffset;

        float positionBlend =
            1f - Mathf.Exp(-positionSharpness * deltaTime);

        cachedTransform.position = Vector3.Lerp(
            cachedTransform.position,
            desiredPosition,
            positionBlend
        );
    }

    private void UpdateCameraRotation()
    {
        Vector3 lookOffset = isAimMode
            ? aimLookOffset
            : normalLookOffset;

        Vector3 rotationOffset = isAimMode
            ? aimRotationOffset
            : normalRotationOffset;

        Quaternion yawRotation = Quaternion.Euler(
            0f,
            currentYaw,
            0f
        );

        Vector3 lookPosition =
            target.position +
            yawRotation * lookOffset;

        Vector3 lookDirection =
            lookPosition - cachedTransform.position;

        if (lookDirection.sqrMagnitude <= 0.0001f)
        {
            return;
        }

        Quaternion lookRotation = Quaternion.LookRotation(
            lookDirection,
            Vector3.up
        );

        // Rotation offset changes only rotation, not camera position.
        cachedTransform.rotation =
            lookRotation * Quaternion.Euler(rotationOffset);
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

        Vector3 positionOffset = isAimMode
            ? aimPositionOffset
            : normalPositionOffset;

        Quaternion orbitRotation = Quaternion.Euler(
            currentPitch,
            currentYaw,
            0f
        );

        Quaternion yawRotation = Quaternion.Euler(
            0f,
            currentYaw,
            0f
        );

        cachedTransform.position =
            target.position +
            orbitRotation * Vector3.back * distance +
            yawRotation * positionOffset;

        UpdateCameraRotation();
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