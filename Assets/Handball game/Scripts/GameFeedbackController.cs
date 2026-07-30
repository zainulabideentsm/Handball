using UnityEngine;

[DisallowMultipleComponent]
public sealed class GameFeedbackController : MonoBehaviour
{
    [Header("Required References")]
    [SerializeField] private PlayerMovementController playerMovement;
    [SerializeField] private ThirdPersonCameraFollow followCamera;
    [SerializeField] private Camera gameplayCamera;

    [Header("Run Speed Lines")]
    [SerializeField] private CanvasGroup speedLinesCanvasGroup;
    [SerializeField] private RectTransform speedLinesTransform;

    [Header("Score Feedback")]
    [SerializeField] private RectTransform scoreTransform;

    [Header("Optional Particles")]
    [SerializeField] private ParticleSystem landingParticles;
    [SerializeField] private ParticleSystem ballImpactParticles;
    [SerializeField] private ParticleSystem hoopImpactParticles;

    [Header("Maximum-Speed Feedback")]
    [SerializeField, Range(0f, 1f)] private float runEffectStart = 0.78f;
    [SerializeField, Min(0f)] private float maximumRunFovBoost = 7f;
    [SerializeField, Range(0.01f, 0.5f)] private float runEffectSmoothTime = 0.12f;
    [SerializeField, Range(0f, 1f)] private float speedLinesMaximumAlpha = 0.65f;
    [SerializeField, Range(0.5f, 2f)] private float speedLinesMinimumScale = 1f;
    [SerializeField, Range(0.5f, 2f)] private float speedLinesMaximumScale = 1.08f;

    [SerializeField]
    private AnimationCurve runEffectCurve = new AnimationCurve(
        new Keyframe(0f, 0f),
        new Keyframe(0.4f, 0.15f),
        new Keyframe(1f, 1f)
    );

    [Header("Speed Lines Animation")]
    [SerializeField, Min(0f)] private float speedLinesAnimationSpeed = 1.5f;
    [SerializeField, Range(0f, 0.1f)] private float speedLinesPulseAmount = 0.025f;
    [SerializeField] private Vector2 speedLinesDriftAmount = new Vector2(4f, 2f);
    [SerializeField, Range(0f, 5f)] private float speedLinesRotationAmount = 0.8f;

    [Header("Jump Feedback")]
    [SerializeField, Min(0f)] private float jumpFovPulse = 0.8f;
    [SerializeField] private bool vibrateOnJump;

    [Header("Landing Feedback")]
    [SerializeField, Min(0f)] private float minimumLandingImpact = 3f;
    [SerializeField, Min(0f)] private float maximumLandingImpact = 12f;
    [SerializeField, Min(0f)] private float landingFovPulse = 0.6f;

    [Header("Throw Feedback")]
    [SerializeField, Min(0f)] private float throwFovPulse = 2.5f;
    [SerializeField] private bool vibrateOnThrow = true;

    [Header("Ball Impact Feedback")]
    [SerializeField, Min(0f)] private float minimumParticleImpactSpeed = 3f;

    [Header("Goal Feedback")]
    [SerializeField, Min(0f)] private float goalFovPulse = 4f;
    [SerializeField, Range(1f, 2f)] private float scorePulseScale = 1.3f;
    [SerializeField, Min(0.01f)] private float scorePulseDuration = 0.3f;
    [SerializeField] private bool vibrateOnGoal = true;

    [Header("FOV Pulse Recovery")]
    [SerializeField, Min(0.1f)] private float fovPulseReturnSpeed = 10f;

    private float baseFieldOfView;
    private float currentFieldOfView;
    private float fieldOfViewVelocity;
    private float fieldOfViewPulse;

    private float runIntensity;
    private float runIntensityVelocity;

    private Vector2 speedLinesBasePosition;
    private Quaternion speedLinesBaseRotation;

    private Vector3 scoreBaseScale = Vector3.one;
    private float scorePulseRemaining;

    private bool speedLinesVisible;

    private void Awake()
    {
        if (gameplayCamera != null)
        {
            baseFieldOfView = gameplayCamera.fieldOfView;
            currentFieldOfView = baseFieldOfView;
        }

        if (speedLinesTransform != null)
        {
            speedLinesBasePosition = speedLinesTransform.anchoredPosition;
            speedLinesBaseRotation = speedLinesTransform.localRotation;
        }

        if (scoreTransform != null)
        {
            scoreBaseScale = scoreTransform.localScale;
        }

        HideSpeedLinesImmediately();
    }

    private void OnEnable()
    {
        if (playerMovement == null)
        {
            return;
        }

        playerMovement.Jumped += PlayJumpFeedback;
        playerMovement.Landed += PlayLandingFeedback;
    }

    private void OnDisable()
    {
        if (playerMovement != null)
        {
            playerMovement.Jumped -= PlayJumpFeedback;
            playerMovement.Landed -= PlayLandingFeedback;
        }

        ResetFeedback();
    }

    private void Update()
    {
        float deltaTime = Time.deltaTime;

        UpdateRunFeedback(deltaTime);
        UpdateFieldOfView(deltaTime);
        UpdateScorePulse(Time.unscaledDeltaTime);
    }

    private void UpdateRunFeedback(float deltaTime)
    {
        bool canShowRunEffect =
            playerMovement != null &&
            playerMovement.MovementEnabled &&
            playerMovement.IsGrounded &&
            !playerMovement.IsAimMovementMode;

        float normalizedSpeed = canShowRunEffect
            ? playerMovement.NormalizedSpeed
            : 0f;

        float targetIntensity = Mathf.InverseLerp(
            runEffectStart,
            1f,
            normalizedSpeed
        );

        runIntensity = Mathf.SmoothDamp(
            runIntensity,
            targetIntensity,
            ref runIntensityVelocity,
            runEffectSmoothTime,
            Mathf.Infinity,
            deltaTime
        );

        float shapedIntensity = runEffectCurve != null
            ? runEffectCurve.Evaluate(runIntensity)
            : runIntensity;

        UpdateSpeedLines(shapedIntensity);
    }

    private void UpdateFieldOfView(float deltaTime)
    {
        if (gameplayCamera == null)
        {
            return;
        }

        float shapedRunIntensity = runEffectCurve != null
            ? runEffectCurve.Evaluate(runIntensity)
            : runIntensity;

        float targetFieldOfView =
            baseFieldOfView +
            maximumRunFovBoost * shapedRunIntensity +
            fieldOfViewPulse;

        currentFieldOfView = Mathf.SmoothDamp(
            currentFieldOfView,
            targetFieldOfView,
            ref fieldOfViewVelocity,
            runEffectSmoothTime,
            Mathf.Infinity,
            deltaTime
        );

        gameplayCamera.fieldOfView = currentFieldOfView;

        fieldOfViewPulse = Mathf.MoveTowards(
            fieldOfViewPulse,
            0f,
            fovPulseReturnSpeed * deltaTime
        );
    }

    private void UpdateSpeedLines(float intensity)
    {
        if (speedLinesCanvasGroup == null)
        {
            return;
        }

        bool shouldBeVisible = intensity > 0.01f;

        if (shouldBeVisible && !speedLinesVisible)
        {
            speedLinesCanvasGroup.gameObject.SetActive(true);
            speedLinesVisible = true;
        }

        speedLinesCanvasGroup.alpha = intensity * speedLinesMaximumAlpha;

        if (speedLinesTransform != null)
        {
            float animationTime = Time.unscaledTime * speedLinesAnimationSpeed;

            float pulse = Mathf.Sin(animationTime * Mathf.PI * 2f);
            float horizontalDrift = Mathf.Sin(animationTime * 0.8f);
            float verticalDrift = Mathf.Cos(animationTime * 0.65f);
            float rotationSway = Mathf.Sin(animationTime * 0.55f);

            float baseScale = Mathf.Lerp(
                speedLinesMinimumScale,
                speedLinesMaximumScale,
                intensity
            );

            float animatedScale =
                baseScale +
                pulse *
                speedLinesPulseAmount *
                intensity;

            speedLinesTransform.localScale = Vector3.one * animatedScale;

            speedLinesTransform.anchoredPosition =
                speedLinesBasePosition +
                new Vector2(
                    horizontalDrift * speedLinesDriftAmount.x,
                    verticalDrift * speedLinesDriftAmount.y
                ) * intensity;

            speedLinesTransform.localRotation =
                speedLinesBaseRotation *
                Quaternion.Euler(
                    0f,
                    0f,
                    rotationSway *
                    speedLinesRotationAmount *
                    intensity
                );
        }

        if (!shouldBeVisible &&
            speedLinesVisible &&
            speedLinesCanvasGroup.alpha <= 0.001f)
        {
            speedLinesCanvasGroup.gameObject.SetActive(false);
            speedLinesVisible = false;
            ResetSpeedLinesTransform();
        }
    }

    public void PlayJumpFeedback()
    {
        AddFieldOfViewPulse(jumpFovPulse);

        if (vibrateOnJump)
        {
            Vibrate();
        }
    }

    public void PlayLandingFeedback(float impactSpeed)
    {
        float strength = Mathf.InverseLerp(
            minimumLandingImpact,
            maximumLandingImpact,
            impactSpeed
        );

        if (strength <= 0f)
        {
            return;
        }

        AddFieldOfViewPulse(-landingFovPulse * strength);

        if (landingParticles != null && playerMovement != null)
        {
            PlayParticle(
                landingParticles,
                playerMovement.transform.position,
                Vector3.up
            );
        }
    }

    public void PlayThrowFeedback()
    {
        followCamera?.PlayThrowImpulse();
        AddFieldOfViewPulse(throwFovPulse);

        if (vibrateOnThrow)
        {
            Vibrate();
        }
    }

    public void PlayBallImpactFeedback(
        Vector3 position,
        Vector3 surfaceNormal,
        float impactSpeed,
        bool hoopImpact)
    {
        if (impactSpeed < minimumParticleImpactSpeed)
        {
            return;
        }

        ParticleSystem selectedParticles = hoopImpact
            ? hoopImpactParticles
            : ballImpactParticles;

        PlayParticle(
            selectedParticles,
            position,
            surfaceNormal
        );
    }

    public void PlayGoalFeedback()
    {
        AddFieldOfViewPulse(goalFovPulse);

        if (scoreTransform != null)
        {
            scorePulseRemaining = scorePulseDuration;
        }

        if (vibrateOnGoal)
        {
            Vibrate();
        }
    }

    private void AddFieldOfViewPulse(float amount)
    {
        if (Mathf.Abs(amount) > Mathf.Abs(fieldOfViewPulse))
        {
            fieldOfViewPulse = amount;
        }
    }

    private void UpdateScorePulse(float unscaledDeltaTime)
    {
        if (scoreTransform == null || scorePulseRemaining <= 0f)
        {
            return;
        }

        scorePulseRemaining -= unscaledDeltaTime;

        float progress = 1f - Mathf.Clamp01(
            scorePulseRemaining / scorePulseDuration
        );

        float pulse = Mathf.Sin(progress * Mathf.PI);

        scoreTransform.localScale =
            scoreBaseScale *
            Mathf.Lerp(1f, scorePulseScale, pulse);

        if (scorePulseRemaining <= 0f)
        {
            scoreTransform.localScale = scoreBaseScale;
        }
    }

    private static void PlayParticle(
        ParticleSystem particles,
        Vector3 position,
        Vector3 surfaceNormal)
    {
        if (particles == null)
        {
            return;
        }

        Vector3 normal = surfaceNormal.sqrMagnitude > 0.0001f
            ? surfaceNormal.normalized
            : Vector3.up;

        particles.transform.SetPositionAndRotation(
            position,
            Quaternion.LookRotation(normal)
        );

        particles.Stop(
            true,
            ParticleSystemStopBehavior.StopEmittingAndClear
        );

        particles.Play(true);
    }

    private void ResetSpeedLinesTransform()
    {
        if (speedLinesTransform == null)
        {
            return;
        }

        speedLinesTransform.anchoredPosition = speedLinesBasePosition;
        speedLinesTransform.localRotation = speedLinesBaseRotation;
        speedLinesTransform.localScale = Vector3.one * speedLinesMinimumScale;
    }

    private void HideSpeedLinesImmediately()
    {
        speedLinesVisible = false;

        if (speedLinesCanvasGroup == null)
        {
            return;
        }

        speedLinesCanvasGroup.alpha = 0f;
        speedLinesCanvasGroup.gameObject.SetActive(false);

        ResetSpeedLinesTransform();
    }

    public void ResetFeedback()
    {
        runIntensity = 0f;
        runIntensityVelocity = 0f;

        fieldOfViewPulse = 0f;
        fieldOfViewVelocity = 0f;

        scorePulseRemaining = 0f;

        if (gameplayCamera != null)
        {
            gameplayCamera.fieldOfView = baseFieldOfView;
            currentFieldOfView = baseFieldOfView;
        }

        if (scoreTransform != null)
        {
            scoreTransform.localScale = scoreBaseScale;
        }

        HideSpeedLinesImmediately();
    }

    private static void Vibrate()
    {
#if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR
        Handheld.Vibrate();
#endif
    }

    private void OnValidate()
    {
        maximumLandingImpact = Mathf.Max(
            minimumLandingImpact,
            maximumLandingImpact
        );

        speedLinesMinimumScale = Mathf.Max(
            0.5f,
            speedLinesMinimumScale
        );

        speedLinesMaximumScale = Mathf.Max(
            speedLinesMinimumScale,
            speedLinesMaximumScale
        );
    }
}