using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(SphereCollider))]
public sealed class PlayerBallPickup : MonoBehaviour
{
    [Header("Player References")]
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private Transform ballHoldPoint;

    [Header("UI References")]
    [SerializeField] private GameObject fixedJoystick;
    [SerializeField] private GameObject aimUIRoot;

    [Header("Aim References")]
    [SerializeField] private GameObject trajectoryPreview;

    public BallController HeldBall { get; private set; }
    public bool HasBall => HeldBall != null;

    private SphereCollider pickupCollider;
    private Joystick fixedJoystickComponent;

    private void Awake()
    {
        pickupCollider = GetComponent<SphereCollider>();
        pickupCollider.isTrigger = true;

        if (fixedJoystick != null)
        {
            fixedJoystickComponent = fixedJoystick.GetComponent<Joystick>();
        }

        if (aimUIRoot != null)
        {
            aimUIRoot.SetActive(false);
        }

        if (trajectoryPreview != null)
        {
            trajectoryPreview.SetActive(false);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (HasBall)
        {
            return;
        }

        BallController ball =
            other.GetComponentInParent<BallController>();

        if (ball == null)
        {
            return;
        }

        if (!ball.TryPickUp(ballHoldPoint))
        {
            return;
        }

        HeldBall = ball;
        EnterAimMode();
    }

    private void EnterAimMode()
    {
        playerMovement.SetMovementEnabled(false);

        if (fixedJoystick != null)
        {
            if (fixedJoystickComponent != null)
            {
                fixedJoystickComponent.ResetJoystick();
            }

            fixedJoystick.SetActive(false);
        }

        if (aimUIRoot != null)
        {
            aimUIRoot.SetActive(true);
        }

        if (trajectoryPreview != null)
        {
            trajectoryPreview.SetActive(true);
        }

        // Prevent repeated pickup events while holding the ball.
        pickupCollider.enabled = false;
    }

    public void CompleteThrow()
    {
        HeldBall = null;

        pickupCollider.enabled = true;

        if (aimUIRoot != null)
        {
            aimUIRoot.SetActive(false);
        }

        if (trajectoryPreview != null)
        {
            trajectoryPreview.SetActive(false);
        }

        if (fixedJoystick != null)
        {
            fixedJoystick.SetActive(true);

            if (fixedJoystickComponent != null)
            {
                fixedJoystickComponent.ResetJoystick();
            }
        }

        playerMovement.SetMovementEnabled(true);
    }
}