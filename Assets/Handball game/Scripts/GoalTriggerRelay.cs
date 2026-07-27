using UnityEngine;

public enum GoalTriggerType
{
    Entry,
    Score
}

[DisallowMultipleComponent]
[RequireComponent(typeof(BoxCollider))]
public sealed class GoalTriggerRelay : MonoBehaviour
{
    [SerializeField] private HoopGoalDetector goalDetector;
    [SerializeField] private GoalTriggerType triggerType;

    [Tooltip("Assign only the Ball layer.")]
    [SerializeField] private LayerMask ballLayers;

    private void Awake()
    {
        BoxCollider triggerCollider = GetComponent<BoxCollider>();
        triggerCollider.isTrigger = true;

        if (goalDetector == null)
        {
            goalDetector = GetComponentInParent<HoopGoalDetector>();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        ProcessBall(other);
    }

    private void OnTriggerStay(Collider other)
    {
        ProcessBall(other);
    }

    private void ProcessBall(Collider other)
    {
        if (goalDetector == null)
        {
            return;
        }

        BallController ball =
            other.GetComponentInParent<BallController>();

        if (ball == null)
        {
            return;
        }

        // Check the BallController object's layer rather than only
        // the collider object's layer.
        if (!IsAllowedLayer(ball.gameObject.layer))
        {
            return;
        }

        goalDetector.NotifyTrigger(
            triggerType,
            ball
        );
    }

    private bool IsAllowedLayer(int layer)
    {
        return (ballLayers.value & (1 << layer)) != 0;
    }
}