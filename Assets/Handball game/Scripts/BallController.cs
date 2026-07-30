using UnityEngine;

public enum BallState
{
    Free,
    PickupPending,
    Held,
    Thrown
}

[DisallowMultipleComponent]
public sealed class BallController : MonoBehaviour
{
    [Header("Required References")]
    [Tooltip("Assign the Rigidbody on the Ball root.")]
    [SerializeField] private Rigidbody ballRigidbody;

    [Tooltip("Assign the Ball collider.")]
    [SerializeField] private Collider ballCollider;

    [Tooltip("Assign the AudioSource from the BallAudio child object.")]
    [SerializeField] private AudioSource ballAudioSource;

    [Header("Ground Detection")]
    [SerializeField] private LayerMask groundLayers = ~0;
    [SerializeField, Range(0f, 1f)] private float groundNormalThreshold = 0.5f;

    [Header("Rolling")]
    [SerializeField, Min(0f)] private float groundLinearDeceleration = 2.5f;
    [SerializeField, Min(0f)] private float groundAngularDeceleration = 5f;
    [SerializeField, Min(0f)] private float settleLinearSpeed = 0.35f;
    [SerializeField, Min(0f)] private float settleDuration = 0.6f;
    [SerializeField, Min(0f)] private float maximumGroundRollTime = 3f;

    [Header("Pickup")]
    [SerializeField, Min(0f)] private float maximumPickupSpeed = 3f;
    [SerializeField, Min(0f)] private float pickupCooldownAfterThrow = 0.25f;

    [Header("Impact Audio And Feedback")]
    [SerializeField, Min(0f)] private float minimumImpactSpeed = 1.5f;
    [SerializeField, Min(0f)] private float impactSoundCooldown = 0.1f;

    [Tooltip("Collisions on these layers use the hoop impact sound and feedback.")]
    [SerializeField] private LayerMask hoopImpactLayerMask = 1 << 9;

    public BallState CurrentState { get; private set; } = BallState.Free;

    private Transform cachedTransform;
    private Transform currentHoldPoint;

    private RigidbodyInterpolation defaultInterpolation;
    private CollisionDetectionMode defaultCollisionDetection;

    private bool isGrounded;
    private float lowSpeedTimer;
    private float groundedTimer;
    private float throwTimestamp;
    private float nextImpactSoundAllowedTime;

    private void Awake()
    {
        cachedTransform = transform;

        if (ballRigidbody == null)
        {
            Debug.LogError("BallController: Ball Rigidbody is not assigned.", this);
            enabled = false;
            return;
        }

        if (ballCollider == null)
        {
            Debug.LogError("BallController: Ball Collider is not assigned.", this);
            enabled = false;
            return;
        }

        if (ballAudioSource == null)
        {
            Debug.LogWarning("BallController: BallAudio child AudioSource is not assigned.", this);
        }

        defaultInterpolation = ballRigidbody.interpolation;
        defaultCollisionDetection = ballRigidbody.collisionDetectionMode;
    }

    private void LateUpdate()
    {
        if (CurrentState != BallState.Held || currentHoldPoint == null)
        {
            return;
        }

        cachedTransform.localPosition = Vector3.zero;
        cachedTransform.localRotation = Quaternion.identity;
    }

    public bool TryBeginPickup()
    {
        if (CurrentState == BallState.Held || CurrentState == BallState.PickupPending)
        {
            return false;
        }

        if (CurrentState == BallState.Thrown)
        {
            bool cooldownElapsed = Time.time - throwTimestamp >= pickupCooldownAfterThrow;
            float maximumSpeedSquared = maximumPickupSpeed * maximumPickupSpeed;
            bool slowEnough = ballRigidbody.linearVelocity.sqrMagnitude <= maximumSpeedSquared;

            if (!cooldownElapsed || !slowEnough)
            {
                return false;
            }
        }

        CurrentState = BallState.PickupPending;

        ResetMovement();
        ResetGroundState();

        ballRigidbody.Sleep();
        ballRigidbody.useGravity = false;
        ballRigidbody.detectCollisions = false;
        ballRigidbody.isKinematic = true;
        ballRigidbody.interpolation = RigidbodyInterpolation.None;
        ballCollider.enabled = false;

        return true;
    }

    public bool AttachToHoldPoint(Transform holdPoint)
    {
        if (CurrentState != BallState.PickupPending || holdPoint == null)
        {
            return false;
        }

        currentHoldPoint = holdPoint;
        CurrentState = BallState.Held;

        cachedTransform.SetParent(currentHoldPoint, false);
        cachedTransform.localPosition = Vector3.zero;
        cachedTransform.localRotation = Quaternion.identity;

        return true;
    }

    public bool TryPickUp(Transform holdPoint)
    {
        return TryBeginPickup() && AttachToHoldPoint(holdPoint);
    }

    public void Throw(Vector3 launchVelocity, Vector3 spin)
    {
        if (CurrentState != BallState.Held)
        {
            return;
        }

        cachedTransform.SetParent(null, true);
        currentHoldPoint = null;

        ballCollider.enabled = true;

        ballRigidbody.isKinematic = false;
        ballRigidbody.useGravity = true;
        ballRigidbody.detectCollisions = true;
        ballRigidbody.interpolation = defaultInterpolation;
        ballRigidbody.collisionDetectionMode = defaultCollisionDetection;

        ResetGroundState();

        throwTimestamp = Time.time;
        CurrentState = BallState.Thrown;

        ballRigidbody.WakeUp();
        ballRigidbody.linearVelocity = launchVelocity;
        ballRigidbody.angularVelocity = spin;
    }

    public void MakeAvailable()
    {
        if (CurrentState == BallState.Held || CurrentState == BallState.PickupPending)
        {
            return;
        }

        CurrentState = BallState.Free;
        currentHoldPoint = null;
    }

    private void OnCollisionEnter(Collision collision)
    {
        EvaluateGroundContact(collision);
        TryPlayImpactFeedback(collision);
    }

    private void TryPlayImpactFeedback(Collision collision)
    {
        if (CurrentState != BallState.Thrown || Time.time < nextImpactSoundAllowedTime)
        {
            return;
        }

        float impactSpeed = collision.relativeVelocity.magnitude;

        if (impactSpeed < minimumImpactSpeed)
        {
            return;
        }

        nextImpactSoundAllowedTime = Time.time + impactSoundCooldown;

        float volume = Mathf.InverseLerp(minimumImpactSpeed, 10f, impactSpeed);
        bool isHoopLayer = (hoopImpactLayerMask.value & (1 << collision.gameObject.layer)) != 0;

        GameManager gameManager = GameManager.Instance;

        if (gameManager != null && gameManager.SoundData != null && ballAudioSource != null)
        {
            if (isHoopLayer)
            {
                gameManager.SoundData.PlayHoopImpact(ballAudioSource, volume);
            }
            else
            {
                gameManager.SoundData.PlayBallImpact(ballAudioSource, volume);
            }
        }

        Vector3 contactPosition = cachedTransform.position;
        Vector3 contactNormal = Vector3.up;

        if (collision.contactCount > 0)
        {
            ContactPoint contact = collision.GetContact(0);
            contactPosition = contact.point;
            contactNormal = contact.normal;
        }

        gameManager?.Feedback?.PlayBallImpactFeedback(
            contactPosition,
            contactNormal,
            impactSpeed,
            isHoopLayer
        );
    }

    private void OnCollisionStay(Collision collision)
    {
        EvaluateGroundContact(collision);
    }

    private void OnCollisionExit(Collision collision)
    {
        if (IsGroundLayer(collision.gameObject.layer))
        {
            isGrounded = false;
        }
    }

    private void EvaluateGroundContact(Collision collision)
    {
        if (!IsGroundLayer(collision.gameObject.layer))
        {
            return;
        }

        for (int i = 0; i < collision.contactCount; i++)
        {
            if (collision.GetContact(i).normal.y >= groundNormalThreshold)
            {
                isGrounded = true;
                return;
            }
        }
    }

    private bool IsGroundLayer(int layer)
    {
        return (groundLayers.value & (1 << layer)) != 0;
    }

    private void FixedUpdate()
    {
        if (CurrentState != BallState.Thrown)
        {
            return;
        }

        if (!isGrounded)
        {
            lowSpeedTimer = 0f;
            groundedTimer = 0f;
            return;
        }

        float fixedDeltaTime = Time.fixedDeltaTime;
        Vector3 velocity = ballRigidbody.linearVelocity;
        Vector3 planarVelocity = new Vector3(velocity.x, 0f, velocity.z);

        planarVelocity = Vector3.MoveTowards(
            planarVelocity,
            Vector3.zero,
            groundLinearDeceleration * fixedDeltaTime
        );

        ballRigidbody.linearVelocity = new Vector3(
            planarVelocity.x,
            velocity.y,
            planarVelocity.z
        );

        ballRigidbody.angularVelocity = Vector3.MoveTowards(
            ballRigidbody.angularVelocity,
            Vector3.zero,
            groundAngularDeceleration * fixedDeltaTime
        );

        groundedTimer += fixedDeltaTime;

        float settleSpeedSquared = settleLinearSpeed * settleLinearSpeed;

        if (planarVelocity.sqrMagnitude <= settleSpeedSquared)
        {
            lowSpeedTimer += fixedDeltaTime;
        }
        else
        {
            lowSpeedTimer = 0f;
        }

        if (lowSpeedTimer >= settleDuration || groundedTimer >= maximumGroundRollTime)
        {
            SettleBall();
        }
    }

    private void SettleBall()
    {
        ResetMovement();
        ballRigidbody.Sleep();
        ResetGroundState();

        CurrentState = BallState.Free;
    }

    private void ResetMovement()
    {
        ballRigidbody.linearVelocity = Vector3.zero;
        ballRigidbody.angularVelocity = Vector3.zero;
    }

    private void ResetGroundState()
    {
        isGrounded = false;
        lowSpeedTimer = 0f;
        groundedTimer = 0f;
    }

    public void ResetToSpawn(Vector3 position, Quaternion rotation)
    {
        cachedTransform.SetParent(null, true);
        currentHoldPoint = null;

        ResetMovement();
        ResetGroundState();

        ballCollider.enabled = true;

        ballRigidbody.isKinematic = true;
        ballRigidbody.useGravity = true;
        ballRigidbody.detectCollisions = true;
        ballRigidbody.interpolation = defaultInterpolation;
        ballRigidbody.collisionDetectionMode = defaultCollisionDetection;

        ballRigidbody.position = position;
        ballRigidbody.rotation = rotation;

        CurrentState = BallState.Free;

        ballRigidbody.isKinematic = false;
        ballRigidbody.WakeUp();
    }
}