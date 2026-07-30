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
    [SerializeField, Min(0.1f)] private float aimDistance = 4f;

    // Shoulder-offset position + separate forward look-ahead point, so the aim camera sits
    // behind/beside the player and looks INTO the play area instead of rotating to stare
    // at the player's own centre (see UpdateCameraTransform/ComputeAimPosition/ComputeAimRotation).
    [Header("Aim Camera Composition")]
    [Tooltip("Sideways offset (camera-relative right) of the aim camera position from the player.")]
    [SerializeField] private float aimShoulderOffset = 0.9f;

    [Tooltip("Height of the aim camera position above the player's feet/pivot.")]
    [SerializeField] private float aimCameraHeight = 1.35f;

    [Tooltip("How far ahead (along aim yaw/pitch) the aim camera looks — never at the player.")]
    [SerializeField] private float aimLookAheadDistance = 6.5f;

    [Tooltip("Height of the forward aim look point above the player's feet/pivot.")]
    [SerializeField] private float aimLookHeight = 1.15f;

    [Tooltip("Sideways offset (camera-relative right) of the forward aim look point.")]
    [SerializeField] private float aimLookRightOffset = 0.45f;

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

    [Header("Throw Camera Impulse")]
    [Tooltip("Much shorter/weaker than the goal shake — reuses the same shake system, does not add a new one.")]
    [SerializeField, Min(0.01f)] private float throwImpulseDuration = 0.12f;

    [SerializeField] private Vector3 throwImpulsePositionShake = new Vector3(0.02f, 0.015f, 0.01f);
    [SerializeField] private Vector3 throwImpulseRotationShake = new Vector3(0.3f, 0.35f, 0.15f);

    [Header("Deprecated")]
    [HideInInspector]
    [SerializeField]
    [System.Obsolete("Superseded by the shoulder-offset/look-ahead Aim Camera Composition fields. Kept only so old Inspector values are not lost.")]
    private Vector3 aimPositionOffset = new Vector3(1.1f, -0.2f, 0f);

    [HideInInspector]
    [SerializeField]
    [System.Obsolete("Superseded by the shoulder-offset/look-ahead Aim Camera Composition fields. Kept only so old Inspector values are not lost.")]
    private Vector3 aimFallbackLookOffset = new Vector3(0f, 1.15f, 4f);

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

    // Normal and aim modes now use structurally different position/rotation formulas (normal
    // still orbits the player; aim uses a shoulder offset + forward look point), so they're
    // each computed fully, then blended via aimBlend — position by Lerp, rotation by Slerp.
    private void UpdateCameraTransform()
    {
        Quaternion yawRotation = Quaternion.Euler(0f, currentYaw, 0f);

        Vector3 normalPosition = ComputeNormalPosition(yawRotation);
        Quaternion normalRotation = ComputeNormalRotation(normalPosition, yawRotation);

        Vector3 aimPosition = ComputeAimPosition(yawRotation);
        Quaternion aimRotation = ComputeAimRotation(aimPosition, yawRotation);

        cachedTransform.position = Vector3.Lerp(normalPosition, aimPosition, aimBlend);
        cachedTransform.rotation = Quaternion.Slerp(normalRotation, aimRotation, aimBlend);
    }

    private Vector3 ComputeNormalPosition(Quaternion yawRotation)
    {
        Quaternion orbitRotation = Quaternion.Euler(currentPitch, currentYaw, 0f);

        return target.position +
               orbitRotation * Vector3.back * normalDistance +
               yawRotation * normalPositionOffset;
    }

    private Quaternion ComputeNormalRotation(Vector3 cameraPosition, Quaternion yawRotation)
    {
        Vector3 lookPosition = target.position + yawRotation * normalLookOffset;
        Vector3 lookDirection = lookPosition - cameraPosition;

        Quaternion lookRotation = lookDirection.sqrMagnitude > 0.0001f
            ? Quaternion.LookRotation(lookDirection, Vector3.up)
            : cachedTransform.rotation;

        return lookRotation * Quaternion.Euler(normalRotationOffset);
    }

    // Shoulder-offset aim camera position — behind and to the side of the player, at a fixed
    // height, rather than orbiting directly behind (which is what made it "look at" the player).
    private Vector3 ComputeAimPosition(Quaternion yawRotation)
    {
        return target.position +
               yawRotation * Vector3.right * aimShoulderOffset +
               Vector3.up * aimCameraHeight -
               yawRotation * Vector3.forward * aimDistance;
    }

    // Separate forward look point several metres into the play area. Vertical aim pitch tilts
    // this look point up/down (via pitchedForward) without pulling the camera back toward the
    // player, so the trajectory can arc higher than the camera's own pitch while staying visible.
    private Quaternion ComputeAimRotation(Vector3 cameraPosition, Quaternion yawRotation)
    {
        Quaternion pitchRotation = Quaternion.Euler(currentPitch, 0f, 0f);
        Vector3 pitchedForward = yawRotation * pitchRotation * Vector3.forward;

        Vector3 lookPoint =
            target.position +
            pitchedForward * aimLookAheadDistance +
            yawRotation * Vector3.right * aimLookRightOffset +
            Vector3.up * aimLookHeight;

        Vector3 lookDirection = lookPoint - cameraPosition;

        Quaternion lookRotation = lookDirection.sqrMagnitude > 0.0001f
            ? Quaternion.LookRotation(lookDirection, Vector3.up)
            : Quaternion.LookRotation(yawRotation * Vector3.forward, Vector3.up);

        return lookRotation * Quaternion.Euler(aimRotationOffset);
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

    // Much weaker/shorter than the goal shake, reuses the same shake system (no new component,
    // no accumulation — PlayCameraShake always starts a fresh, time-bounded shake).
    public void PlayThrowImpulse()
    {
        PlayCameraShake(throwImpulseDuration, throwImpulsePositionShake, throwImpulseRotationShake);
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

        // Deprecated fields kept only for Inspector value compatibility; read (discard) them
        // so they're not flagged as unused by the compiler.
#pragma warning disable CS0618
        _ = aimPositionOffset;
        _ = aimFallbackLookOffset;
#pragma warning restore CS0618
    }
}
