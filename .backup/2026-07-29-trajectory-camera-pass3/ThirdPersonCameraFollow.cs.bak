using UnityEngine;

[DisallowMultipleComponent]
public sealed class ThirdPersonCameraFollow : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform target;
    [SerializeField] private Transform aimLookTarget;
    [SerializeField] private MobileLookArea lookArea;

    [Header("Normal Camera")]
    [SerializeField, Min(0.1f)] private float normalDistance = 4.5f;
    [SerializeField] private Vector3 normalPositionOffset = Vector3.zero;
    [SerializeField] private Vector3 normalLookOffset = new Vector3(0f, 0.5f, 0f);
    [SerializeField] private Vector3 normalRotationOffset = Vector3.zero;

    [Header("Aim Camera")]
    [SerializeField, Min(0.1f)] private float aimDistance = 3.4f;

    [Tooltip("Positive X moves the camera right, placing the player left on-screen.")]
    [SerializeField] private Vector3 aimPositionOffset = new Vector3(1.1f, -0.2f, 0f);

    [SerializeField] private Vector3 aimFallbackLookOffset = new Vector3(0f, 1.15f, 4f);
    [SerializeField] private Vector3 aimRotationOffset = Vector3.zero;

    [SerializeField, Range(0f, 1f)] private float startingAimVertical = 0.25f;

    [Tooltip("Small visual camera pitch range while aiming.")]
    [SerializeField] private float minimumAimCameraPitch = 10f;
    [SerializeField] private float maximumAimCameraPitch = 24f;

    [SerializeField, Range(0.01f, 0.5f)] private float cameraModeBlendTime = 0.15f;

    [Header("Normal Orbit")]
    [SerializeField] private float startingPitch = 18f;
    [SerializeField] private float minimumNormalPitch = 5f;
    [SerializeField] private float maximumNormalPitch = 45f;

    [Header("Touch Sensitivity")]
    [SerializeField, Min(1f)] private float yawDegreesPerScreen = 180f;
    [SerializeField, Min(1f)] private float normalPitchDegreesPerScreen = 80f;

    [Tooltip("How much one full-screen vertical swipe changes aiming.")]
    [SerializeField, Min(0.1f)] private float aimVerticalPerScreen = 1.1f;

    [SerializeField] private bool invertVertical;

    [Header("Orbit Smoothing")]
    [SerializeField, Range(0.01f, 0.3f)] private float orbitSmoothTime = 0.07f;

    [Header("Goal Camera Shake")]
    [SerializeField, Min(0.01f)] private float goalShakeDuration = 0.3f;

    [Tooltip("Camera-local positional shake.")]
    [SerializeField] private Vector3 goalPositionShake = new Vector3(0.08f, 0.06f, 0.03f);

    [Tooltip("Rotational shake in degrees.")]
    [SerializeField] private Vector3 goalRotationShake = new Vector3(0.9f, 1.1f, 0.4f);

    [SerializeField, Min(1f)] private float shakeFrequency = 25f;

    [SerializeField]
    private AnimationCurve shakeEnvelope = new AnimationCurve(
        new Keyframe(0f, 1f),
        new Keyframe(0.25f, 0.85f),
        new Keyframe(1f, 0f)
    );

    public float Yaw => currentYaw;
    public float Pitch => currentPitch;
    public float AimVertical01 { get; private set; }
    public bool IsAimMode => isAimMode;
    public bool IsShaking => shakeTimeRemaining > 0f;

    private Transform cachedTransform;

    private bool isAimMode;

    private float normalTargetPitch;
    private float targetYaw;
    private float targetPitch;
    private float currentYaw;
    private float currentPitch;

    private float yawSmoothVelocity;
    private float pitchSmoothVelocity;

    private float aimBlend;
    private float aimBlendVelocity;

    private int cachedScreenWidth;
    private int cachedScreenHeight;

    private float inverseScreenWidth;
    private float inverseScreenHeight;

    private float shakeTimeRemaining;
    private float shakeTotalDuration;

    private Vector3 shakePositionStrength;
    private Vector3 shakeRotationStrength;
    private Vector3 shakeNoiseSeed;

    private void Awake()
    {
        cachedTransform = transform;

        targetYaw = target != null ? target.eulerAngles.y : cachedTransform.eulerAngles.y;
        normalTargetPitch = startingPitch;
        targetPitch = startingPitch;
        currentYaw = targetYaw;
        currentPitch = targetPitch;
        AimVertical01 = startingAimVertical;

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

        UpdateCameraModeBlend(deltaTime);
        UpdateOrbitAngles(deltaTime);
        UpdateCameraTransform();
        ApplyCameraShake(Time.unscaledDeltaTime);
    }

    private void UpdateCameraModeBlend(float deltaTime)
    {
        float desiredBlend = isAimMode ? 1f : 0f;

        aimBlend = Mathf.SmoothDamp(
            aimBlend,
            desiredBlend,
            ref aimBlendVelocity,
            cameraModeBlendTime,
            Mathf.Infinity,
            deltaTime
        );
    }

    private void UpdateOrbitAngles(float deltaTime)
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

    private void UpdateCameraTransform()
    {
        Quaternion orbitRotation = Quaternion.Euler(currentPitch, currentYaw, 0f);
        Quaternion yawRotation = Quaternion.Euler(0f, currentYaw, 0f);

        float distance = Mathf.Lerp(normalDistance, aimDistance, aimBlend);
        Vector3 positionOffset = Vector3.Lerp(normalPositionOffset, aimPositionOffset, aimBlend);

        cachedTransform.position =
            target.position +
            orbitRotation * Vector3.back * distance +
            yawRotation * positionOffset;

        Vector3 normalLookPosition = target.position + yawRotation * normalLookOffset;

        Vector3 aimLookPosition = aimLookTarget != null
            ? aimLookTarget.position
            : target.position + yawRotation * aimFallbackLookOffset;

        Vector3 lookPosition = Vector3.Lerp(normalLookPosition, aimLookPosition, aimBlend);
        Vector3 lookDirection = lookPosition - cachedTransform.position;

        if (lookDirection.sqrMagnitude <= 0.0001f)
        {
            return;
        }

        Vector3 rotationOffset = Vector3.Lerp(normalRotationOffset, aimRotationOffset, aimBlend);
        Quaternion lookRotation = Quaternion.LookRotation(lookDirection, Vector3.up);

        cachedTransform.rotation = lookRotation * Quaternion.Euler(rotationOffset);
    }

    private void ApplyCameraShake(float unscaledDeltaTime)
    {
        if (shakeTimeRemaining <= 0f || shakeTotalDuration <= 0f)
        {
            return;
        }

        float elapsedTime = shakeTotalDuration - shakeTimeRemaining;
        float normalizedTime = Mathf.Clamp01(elapsedTime / shakeTotalDuration);

        float strength = shakeEnvelope != null
            ? shakeEnvelope.Evaluate(normalizedTime)
            : 1f - normalizedTime;

        float noiseTime = Time.unscaledTime * shakeFrequency;

        Vector3 positionNoise = new Vector3(
            SignedPerlin(shakeNoiseSeed.x, noiseTime),
            SignedPerlin(shakeNoiseSeed.y, noiseTime),
            SignedPerlin(shakeNoiseSeed.z, noiseTime)
        );

        Vector3 rotationNoise = new Vector3(
            SignedPerlin(shakeNoiseSeed.x + 31.7f, noiseTime),
            SignedPerlin(shakeNoiseSeed.y + 63.4f, noiseTime),
            SignedPerlin(shakeNoiseSeed.z + 95.1f, noiseTime)
        );

        Vector3 localPositionOffset = Vector3.Scale(positionNoise, shakePositionStrength) * strength;
        Vector3 rotationOffset = Vector3.Scale(rotationNoise, shakeRotationStrength) * strength;

        cachedTransform.position += cachedTransform.rotation * localPositionOffset;
        cachedTransform.rotation *= Quaternion.Euler(rotationOffset);

        shakeTimeRemaining -= unscaledDeltaTime;

        if (shakeTimeRemaining <= 0f)
        {
            StopCameraShake();
        }
    }

    private static float SignedPerlin(float seed, float time)
    {
        return Mathf.PerlinNoise(seed, time) * 2f - 1f;
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

        targetYaw += dragDelta.x * inverseScreenWidth * yawDegreesPerScreen;

        float verticalMultiplier = invertVertical ? -1f : 1f;

        if (isAimMode)
        {
            AimVertical01 +=
                dragDelta.y *
                inverseScreenHeight *
                aimVerticalPerScreen *
                verticalMultiplier;

            AimVertical01 = Mathf.Clamp01(AimVertical01);

            targetPitch = Mathf.Lerp(
                minimumAimCameraPitch,
                maximumAimCameraPitch,
                AimVertical01
            );
        }
        else
        {
            normalTargetPitch +=
                dragDelta.y *
                inverseScreenHeight *
                normalPitchDegreesPerScreen *
                verticalMultiplier;

            normalTargetPitch = Mathf.Clamp(
                normalTargetPitch,
                minimumNormalPitch,
                maximumNormalPitch
            );

            targetPitch = normalTargetPitch;
        }

        if (targetYaw > 360f || targetYaw < -360f)
        {
            targetYaw %= 360f;
            currentYaw %= 360f;
        }
    }

    public void SetAimMode(bool enabled)
    {
        if (isAimMode == enabled)
        {
            return;
        }

        isAimMode = enabled;

        if (enabled)
        {
            AimVertical01 = startingAimVertical;

            targetPitch = Mathf.Lerp(
                minimumAimCameraPitch,
                maximumAimCameraPitch,
                AimVertical01
            );
        }
        else
        {
            targetPitch = normalTargetPitch;
        }
    }

    public void AlignYawTo(float yaw, bool snapImmediately)
    {
        targetYaw = yaw;

        if (!snapImmediately)
        {
            return;
        }

        currentYaw = yaw;
        yawSmoothVelocity = 0f;
    }

    public void PlayGoalShake()
    {
        PlayCameraShake(goalShakeDuration, goalPositionShake, goalRotationShake);
    }

    public void PlayCameraShake(
        float duration,
        Vector3 positionStrength,
        Vector3 rotationStrength)
    {
        shakeTotalDuration = Mathf.Max(0.01f, duration);
        shakeTimeRemaining = shakeTotalDuration;

        shakePositionStrength = new Vector3(
            Mathf.Abs(positionStrength.x),
            Mathf.Abs(positionStrength.y),
            Mathf.Abs(positionStrength.z)
        );

        shakeRotationStrength = new Vector3(
            Mathf.Abs(rotationStrength.x),
            Mathf.Abs(rotationStrength.y),
            Mathf.Abs(rotationStrength.z)
        );

        shakeNoiseSeed = new Vector3(
            Random.Range(0f, 100f),
            Random.Range(100f, 200f),
            Random.Range(200f, 300f)
        );
    }

    public void StopCameraShake()
    {
        shakeTimeRemaining = 0f;
        shakeTotalDuration = 0f;
        shakePositionStrength = Vector3.zero;
        shakeRotationStrength = Vector3.zero;
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

        aimBlend = isAimMode ? 1f : 0f;
        aimBlendVelocity = 0f;

        UpdateCameraTransform();
    }

    private void CheckScreenSize()
    {
        if (Screen.width != cachedScreenWidth || Screen.height != cachedScreenHeight)
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

    private void OnDisable()
    {
        StopCameraShake();
    }

    private void OnValidate()
    {
        normalDistance = Mathf.Max(0.1f, normalDistance);
        aimDistance = Mathf.Max(0.1f, aimDistance);

        maximumNormalPitch = Mathf.Max(minimumNormalPitch, maximumNormalPitch);
        maximumAimCameraPitch = Mathf.Max(minimumAimCameraPitch, maximumAimCameraPitch);

        goalShakeDuration = Mathf.Max(0.01f, goalShakeDuration);
        shakeFrequency = Mathf.Max(1f, shakeFrequency);
    }
}