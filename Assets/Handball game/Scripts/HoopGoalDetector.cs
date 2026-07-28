using UnityEngine;

[DisallowMultipleComponent]
public sealed class HoopGoalDetector : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private ScoreManager scoreManager;
    [SerializeField] private Transform ballSpawnPoint;
    [SerializeField] private GoalCelebrationController goalCelebration;

    [Tooltip("Assign this hoop's own confetti Particle System.")]
    [SerializeField] private ParticleSystem goalParticles;

    [Header("Scoring")]
    [SerializeField, Min(1)] private int pointsPerGoal = 1;

    [Tooltip("Maximum time allowed between crossing both goal triggers.")]
    [SerializeField, Min(0.1f)] private float triggerValidityDuration = 2f;

    [Tooltip("Prevents one shot from scoring repeatedly.")]
    [SerializeField, Min(0f)] private float scoreCooldown = 0.5f;

    [Header("Ball Reset")]
    [SerializeField, Min(0f)] private float resetDelay = 1.25f;

    private BallController armedBall;
    private BallController scoredBall;
    private GoalTriggerType firstTrigger;

    private float armedUntil;
    private float resetAt;
    private float nextScoreAllowedTime;

    private bool isArmed;
    private bool resetPending;

    private void Update()
    {
        if (isArmed && Time.time > armedUntil)
        {
            ClearArmedBall();
        }

        if (resetPending && Time.time >= resetAt)
        {
            ResetScoredBall();
        }
    }

    public void NotifyTrigger(GoalTriggerType triggerType, BallController ball)
    {
        if (ball == null || resetPending || Time.time < nextScoreAllowedTime)
        {
            return;
        }

        if (ball.CurrentState == BallState.Held || ball.CurrentState == BallState.PickupPending)
        {
            return;
        }

        if (!isArmed || armedBall != ball)
        {
            ArmGoal(ball, triggerType);
            return;
        }

        if (Time.time > armedUntil)
        {
            ArmGoal(ball, triggerType);
            return;
        }

        if (triggerType == firstTrigger)
        {
            return;
        }

        ConfirmGoal(ball);
    }

    private void ArmGoal(BallController ball, GoalTriggerType triggerType)
    {
        armedBall = ball;
        firstTrigger = triggerType;
        armedUntil = Time.time + triggerValidityDuration;
        isArmed = true;
    }

    private void ConfirmGoal(BallController ball)
    {
        scoredBall = ball;

        ClearArmedBall();

        nextScoreAllowedTime = Time.time + scoreCooldown;
        resetPending = true;
        resetAt = Time.time + resetDelay;

        if (scoreManager != null)
        {
            scoreManager.AddScore(pointsPerGoal);
        }

        if (goalCelebration != null)
        {
            goalCelebration.PlayGoalCelebration(goalParticles);
        }

        Debug.Log("GOAL!");
    }

    private void ResetScoredBall()
    {
        BallController ballToReset = scoredBall;

        scoredBall = null;
        resetPending = false;

        ClearArmedBall();

        if (ballToReset == null || ballSpawnPoint == null)
        {
            return;
        }

        ballToReset.ResetToSpawn(ballSpawnPoint.position, ballSpawnPoint.rotation);
        Physics.SyncTransforms();
    }

    private void ClearArmedBall()
    {
        armedBall = null;
        armedUntil = 0f;
        isArmed = false;
    }

    private void OnDisable()
    {
        armedBall = null;
        scoredBall = null;

        armedUntil = 0f;
        resetAt = 0f;
        nextScoreAllowedTime = 0f;

        isArmed = false;
        resetPending = false;
    }
}