using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(LineRenderer))]
public sealed class TrajectoryPreviewController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerBallPickup ballPickup;
    [SerializeField] private PlayerAimController aimController;
    [SerializeField] private Transform throwOrigin;

    [Header("Throw")]
    [SerializeField, Min(0f)]
    private float launchSpeed = 12f;

    [Header("Prediction")]
    [SerializeField, Range(10, 100)]
    private int maximumPointCount = 60;

    [SerializeField, Range(0.01f, 0.15f)]
    private float simulationStep = 0.05f;

    [SerializeField, Range(0.5f, 6f)]
    private float maximumSimulationTime = 4f;

    [Header("Collision")]
    [Tooltip("Select Ground, Hoop and Boundaries. Exclude Player and Ball.")]
    [SerializeField]
    private LayerMask collisionMask;

    [SerializeField, Min(0.01f)]
    private float previewBallRadius = 0.12f;

    [SerializeField, Min(0f)]
    private float collisionSkin = 0.01f;

    [Header("Moving Arrow Texture")]
    [SerializeField]
    private bool animateTexture = true;

    [SerializeField]
    private float scrollSpeed = 1.5f;

    [SerializeField, Min(0.1f)]
    private float tilesPerWorldUnit = 0.8f;

    public float LaunchSpeed => launchSpeed;

    private LineRenderer lineRenderer;
    private Vector3[] trajectoryPoints;

    private Material runtimeMaterial;
    private string texturePropertyName;

    private float textureOffset;

    private void Awake()
    {
        lineRenderer = GetComponent<LineRenderer>();

        trajectoryPoints =
            new Vector3[maximumPointCount];

        lineRenderer.useWorldSpace = true;
        lineRenderer.positionCount = 0;
        lineRenderer.textureMode = LineTextureMode.Tile;
        lineRenderer.alignment = LineAlignment.View;

        if (lineRenderer.sharedMaterial != null)
        {
            runtimeMaterial = new Material(
                lineRenderer.sharedMaterial
            );

            lineRenderer.material = runtimeMaterial;

            if (runtimeMaterial.HasProperty("_BaseMap"))
            {
                texturePropertyName = "_BaseMap";
            }
            else if (runtimeMaterial.HasProperty("_MainTex"))
            {
                texturePropertyName = "_MainTex";
            }
        }
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
        AnimateArrowTexture();
    }

    private void DrawTrajectory()
    {
        Vector3 startPosition =
            throwOrigin.position;

        Vector3 initialVelocity =
            aimController.AimDirection.normalized *
            launchSpeed;

        Vector3 gravity = Physics.gravity;

        trajectoryPoints[0] = startPosition;

        Vector3 previousPosition = startPosition;

        int usedPointCount = 1;

        float simulationTime = simulationStep;
        float totalLength = 0f;

        while (usedPointCount < maximumPointCount &&
               simulationTime <= maximumSimulationTime)
        {
            Vector3 nextPosition =
                startPosition +
                initialVelocity * simulationTime +
                0.5f *
                gravity *
                simulationTime *
                simulationTime;

            Vector3 segment =
                nextPosition - previousPosition;

            float segmentDistance =
                segment.magnitude;

            if (segmentDistance > 0.0001f)
            {
                Vector3 segmentDirection =
                    segment / segmentDistance;

                if (Physics.SphereCast(
                        previousPosition,
                        previewBallRadius,
                        segmentDirection,
                        out RaycastHit hit,
                        segmentDistance,
                        collisionMask,
                        QueryTriggerInteraction.Ignore))
                {
                    Vector3 hitPosition =
                        hit.point +
                        hit.normal *
                        (previewBallRadius +
                         collisionSkin);

                    trajectoryPoints[usedPointCount] =
                        hitPosition;

                    totalLength += Vector3.Distance(
                        previousPosition,
                        hitPosition
                    );

                    usedPointCount++;
                    break;
                }
            }

            trajectoryPoints[usedPointCount] =
                nextPosition;

            totalLength += segmentDistance;

            usedPointCount++;
            previousPosition = nextPosition;
            simulationTime += simulationStep;
        }

        lineRenderer.positionCount =
            usedPointCount;

        for (int i = 0; i < usedPointCount; i++)
        {
            lineRenderer.SetPosition(
                i,
                trajectoryPoints[i]
            );
        }

        UpdateTextureTiling(totalLength);
    }

    private void UpdateTextureTiling(float pathLength)
    {
        if (runtimeMaterial == null ||
            string.IsNullOrEmpty(texturePropertyName))
        {
            return;
        }

        runtimeMaterial.SetTextureScale(
            texturePropertyName,
            new Vector2(4f, 1f)
        );
    }

    private void AnimateArrowTexture()
    {
        if (!animateTexture ||
            runtimeMaterial == null ||
            string.IsNullOrEmpty(texturePropertyName))
        {
            return;
        }

        textureOffset = Mathf.Repeat(
            textureOffset +
            scrollSpeed * Time.deltaTime,
            1f
        );

        runtimeMaterial.SetTextureOffset(
            texturePropertyName,
            new Vector2(-textureOffset, 0f)
        );
    }

    private void HideTrajectory()
    {
        if (lineRenderer.positionCount != 0)
        {
            lineRenderer.positionCount = 0;
        }
    }

    private void OnDestroy()
    {
        if (runtimeMaterial != null)
        {
            Destroy(runtimeMaterial);
        }
    }

    private void OnValidate()
    {
        maximumPointCount =
            Mathf.Max(10, maximumPointCount);

        if (Application.isPlaying)
        {
            trajectoryPoints =
                new Vector3[maximumPointCount];
        }
    }
}