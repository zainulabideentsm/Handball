using UnityEngine;

[DisallowMultipleComponent]
public sealed class HoopGoalDetector : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private ScoreManager scoreManager;
    [SerializeField] private Transform ballSpawnPoint;

    [Header("Scoring")]
    [SerializeField, Min(1)]
    private int pointsPerGoal = 1;

    [Tooltip("Ball must be moving downward.")]
    [SerializeField, Min(0f)]
    private float minimumDownwardSpeed = 0.02f;

    [Tooltip("Maximum time between entering the upper and lower triggers.")]
    [SerializeField, Min(0.1f)]
    private float entryValidityDuration = 3f;

    [Tooltip("Prevents the same goal being counted more than once.")]
    [SerializeField, Min(0f)]
    private float scoreCooldown = 0.5f;

    [Header("Ball Reset")]
    [SerializeField, Min(0f)]
    private float resetDelay = 1.25f;

    private BallController armedBall;
    private BallController scoredBall;

    private float armedUntil;
    private float resetAt;
    private float nextScoreAllowedTime;

    private bool resetPending;

    private void Update()
    {
        if (armedBall != null &&
            Time.time > armedUntil)
        {
            ClearArmedBall();
        }

        if (resetPending &&
            Time.time >= resetAt)
        {
            ResetScoredBall();
        }
    }

    public void NotifyTrigger(
        GoalTriggerType triggerType,
        BallController ball)
    {
        if (ball == null ||
            resetPending ||
            Time.time < nextScoreAllowedTime)
        {
            return;
        }

        if (ball.CurrentState != BallState.Thrown)
        {
            return;
        }

        if (!ball.TryGetComponent(
                out Rigidbody ballRigidbody))
        {
            return;
        }

        if (triggerType == GoalTriggerType.Entry)
        {
            HandleEntryTrigger(
                ball,
                ballRigidbody
            );

            return;
        }

        HandleScoreTrigger(
            ball,
            ballRigidbody
        );
    }

    private void HandleEntryTrigger(
        BallController ball,
        Rigidbody ballRigidbody)
    {
        if (!IsMovingDownward(ballRigidbody))
        {
            return;
        }

        armedBall = ball;
        armedUntil =
            Time.time + entryValidityDuration;
    }

    private void HandleScoreTrigger(
        BallController ball,
        Rigidbody ballRigidbody)
    {
        if (armedBall != ball)
        {
            return;
        }

        if (Time.time > armedUntil)
        {
            ClearArmedBall();
            return;
        }

        if (!IsMovingDownward(ballRigidbody))
        {
            return;
        }

        ConfirmGoal(ball);
    }

    private bool IsMovingDownward(
        Rigidbody ballRigidbody)
    {
        return ballRigidbody.linearVelocity.y <=
               -minimumDownwardSpeed;
    }

    private void ConfirmGoal(BallController ball)
    {
        scoredBall = ball;

        ClearArmedBall();

        nextScoreAllowedTime =
            Time.time + scoreCooldown;

        resetPending = true;
        resetAt = Time.time + resetDelay;

        if (scoreManager != null)
        {
            scoreManager.AddScore(pointsPerGoal);
        }

        Debug.Log("GOAL!");
    }

    private void ResetScoredBall()
    {
        // Clear the detector first so it cannot remain permanently locked.
        BallController ballToReset = scoredBall;

        scoredBall = null;
        resetPending = false;

        ClearArmedBall();

        if (ballToReset == null ||
            ballSpawnPoint == null)
        {
            return;
        }

        ballToReset.ResetToSpawn(
            ballSpawnPoint.position,
            ballSpawnPoint.rotation
        );

        // Updates trigger overlap state after teleporting the ball.
        Physics.SyncTransforms();
    }

    private void ClearArmedBall()
    {
        armedBall = null;
        armedUntil = 0f;
    }

    private void OnDisable()
    {
        armedBall = null;
        scoredBall = null;

        armedUntil = 0f;
        resetAt = 0f;
        nextScoreAllowedTime = 0f;

        resetPending = false;
    }
}