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

    public BallState CurrentState { get; private set; } = BallState.Free;

    private Rigidbody ballRigidbody;
    private Transform cachedTransform;

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
        if (CurrentState != BallState.Free || holdPoint == null)
        {
            return false;
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

        CurrentState = BallState.Thrown;
    }

    public void MakeAvailable()
    {
        if (CurrentState != BallState.Held)
        {
            CurrentState = BallState.Free;
        }
    }
}