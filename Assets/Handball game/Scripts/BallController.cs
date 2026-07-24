using UnityEngine;

public enum BallState
{
    Free,
    Held,
    Thrown
}

[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody))]
public sealed class BallController : MonoBehaviour
{
    [SerializeField] private Collider ballCollider;

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

    public BallState CurrentState { get; private set; } = BallState.Free;

    private Rigidbody ballRigidbody;
    private Transform cachedTransform;
    private bool isGrounded;
    private float lowSpeedTimer;
    private float groundedTimer;
    private float throwTimestamp;

    private void Awake()
    {
        ballRigidbody = GetComponent<Rigidbody>();
        cachedTransform = transform;

        if (ballCollider == null)
        {
            ballCollider = GetComponent<Collider>();
        }
    }

    public bool TryPickUp(Transform holdPoint)
    {
        if (holdPoint == null || CurrentState == BallState.Held)
        {
            return false;
        }

        if (CurrentState == BallState.Thrown)
        {
            bool cooldownElapsed =
                Time.time - throwTimestamp >= pickupCooldownAfterThrow;

            bool slowEnough =
                ballRigidbody.linearVelocity.sqrMagnitude <=
                maximumPickupSpeed * maximumPickupSpeed;

            if (!cooldownElapsed || !slowEnough)
            {
                return false;
            }
        }

        CurrentState = BallState.Held;

        ballRigidbody.linearVelocity = Vector3.zero;
        ballRigidbody.angularVelocity = Vector3.zero;

        ballRigidbody.isKinematic = true;
        ballRigidbody.useGravity = false;

        if (ballCollider != null)
        {
            ballCollider.enabled = false;
        }

        cachedTransform.SetParent(holdPoint, false);
        cachedTransform.localPosition = Vector3.zero;
        cachedTransform.localRotation = Quaternion.identity;

        isGrounded = false;
        lowSpeedTimer = 0f;
        groundedTimer = 0f;

        return true;
    }

    public void Throw(Vector3 launchVelocity, Vector3 spin)
    {
        if (CurrentState != BallState.Held)
        {
            return;
        }

        cachedTransform.SetParent(null, true);

        ballRigidbody.isKinematic = false;
        ballRigidbody.useGravity = true;

        if (ballCollider != null)
        {
            ballCollider.enabled = true;
        }

        ballRigidbody.linearVelocity = launchVelocity;
        ballRigidbody.angularVelocity = spin;

        isGrounded = false;
        lowSpeedTimer = 0f;
        groundedTimer = 0f;
        throwTimestamp = Time.time;

        CurrentState = BallState.Thrown;
    }

    public void MakeAvailable()
    {
        if (CurrentState != BallState.Held)
        {
            CurrentState = BallState.Free;
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        EvaluateGroundContact(collision);
    }

    private void OnCollisionStay(Collision collision)
    {
        EvaluateGroundContact(collision);
    }

    private void OnCollisionExit(Collision collision)
    {
        isGrounded = false;
    }

    private void EvaluateGroundContact(Collision collision)
    {
        // Layer mask left at "Everything" by default; restrict it in the
        // Inspector to only the floor if other surfaces should never count.
        if (groundLayers.value != 0 &&
            (groundLayers.value & (1 << collision.gameObject.layer)) == 0)
        {
            return;
        }

        int contactCount = collision.contactCount;

        for (int i = 0; i < contactCount; i++)
        {
            if (collision.GetContact(i).normal.y >= groundNormalThreshold)
            {
                isGrounded = true;
                return;
            }
        }
    }

    private void FixedUpdate()
    {
        if (CurrentState != BallState.Thrown)
        {
            return;
        }

        if (!isGrounded)
        {
            // Airborne: no artificial braking, timers only count grounded time.
            lowSpeedTimer = 0f;
            groundedTimer = 0f;
            return;
        }

        Vector3 velocity = ballRigidbody.linearVelocity;
        Vector3 planarVelocity = new Vector3(velocity.x, 0f, velocity.z);

        planarVelocity = Vector3.MoveTowards(
            planarVelocity,
            Vector3.zero,
            groundLinearDeceleration * Time.fixedDeltaTime
        );

        ballRigidbody.linearVelocity = new Vector3(
            planarVelocity.x,
            velocity.y,
            planarVelocity.z
        );

        ballRigidbody.angularVelocity = Vector3.MoveTowards(
            ballRigidbody.angularVelocity,
            Vector3.zero,
            groundAngularDeceleration * Time.fixedDeltaTime
        );

        groundedTimer += Time.fixedDeltaTime;

        if (planarVelocity.magnitude <= settleLinearSpeed)
        {
            lowSpeedTimer += Time.fixedDeltaTime;
        }
        else
        {
            lowSpeedTimer = 0f;
        }

        if (lowSpeedTimer >= settleDuration ||
            groundedTimer >= maximumGroundRollTime)
        {
            ballRigidbody.linearVelocity = Vector3.zero;
            ballRigidbody.angularVelocity = Vector3.zero;
            ballRigidbody.Sleep();

            isGrounded = false;
            lowSpeedTimer = 0f;
            groundedTimer = 0f;

            CurrentState = BallState.Free;
        }
    }
}
