using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(LineRenderer))]
public sealed class TrajectoryPreviewController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerBallPickup ballPickup;
    [SerializeField] private PlayerAimController aimController;
    [SerializeField] private Transform throwOrigin;

    [Header("Trajectory")]
    [SerializeField, Min(2)] private int pointCount = 25;
    [SerializeField, Min(0.01f)] private float timeStep = 0.08f;
    [SerializeField, Min(0f)] private float launchSpeed = 12f;

    private LineRenderer lineRenderer;
    private Vector3[] trajectoryPoints;

    public float LaunchSpeed => launchSpeed;

    private void Awake()
    {
        lineRenderer = GetComponent<LineRenderer>();

        trajectoryPoints = new Vector3[pointCount];

        lineRenderer.useWorldSpace = true;
        lineRenderer.positionCount = 0;
    }

    private void Update()
    {
        if (ballPickup == null ||
            aimController == null ||
            throwOrigin == null ||
            !ballPickup.HasBall ||
            !aimController.IsAiming)
        {
            HideTrajectory();
            return;
        }

        DrawTrajectory();
    }

    private void DrawTrajectory()
    {
        Vector3 startPosition = throwOrigin.position;

        Vector3 initialVelocity =
            aimController.AimDirection.normalized * launchSpeed;

        Vector3 gravity = Physics.gravity;

        for (int i = 0; i < pointCount; i++)
        {
            float time = i * timeStep;

            trajectoryPoints[i] =
                startPosition +
                initialVelocity * time +
                0.5f * gravity * time * time;
        }

        lineRenderer.positionCount = pointCount;
        lineRenderer.SetPositions(trajectoryPoints);
    }

    private void HideTrajectory()
    {
        if (lineRenderer.positionCount != 0)
        {
            lineRenderer.positionCount = 0;
        }
    }

    private void OnValidate()
    {
        pointCount = Mathf.Max(2, pointCount);

        if (Application.isPlaying)
        {
            trajectoryPoints = new Vector3[pointCount];
        }
    }
}