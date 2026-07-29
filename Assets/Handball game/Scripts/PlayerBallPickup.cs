using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(SphereCollider))]
public sealed class PlayerBallPickup : MonoBehaviour
{
    [Header("Player References")]
    [SerializeField] private PlayerMovementController playerMovement;
    [SerializeField] private PlayerAnimationController animationController;
    [SerializeField] private Transform ballHoldPoint;

    [Header("UI References")]
    [SerializeField] private GameObject fixedJoystick;
    [SerializeField] private GameObject aimUIRoot;

    [Header("Aim References")]
    [SerializeField] private GameObject trajectoryPreview;

    public BallController HeldBall { get; private set; }
    public bool HasBall => HeldBall != null;
    public bool IsPickingUp => pendingBall != null;

    private BallController pendingBall;
    private SphereCollider pickupCollider;

    private void Awake()
    {
        pickupCollider = GetComponent<SphereCollider>();
        pickupCollider.isTrigger = true;

        if (fixedJoystick != null)
        {
            fixedJoystick.SetActive(true);
        }

        SetAimVisuals(false);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (HasBall || IsPickingUp)
        {
            return;
        }

        BallController ball = other.GetComponentInParent<BallController>();

        if (ball == null || !ball.TryBeginPickup())
        {
            return;
        }

        pendingBall = ball;

        if (animationController != null)
        {
            animationController.PlayPickup();
        }

        BeginPickupSequence();
    }

    private void BeginPickupSequence()
    {
        // Stop only during the pickup animation.
        if (playerMovement != null)
        {
            playerMovement.SetAimMovementMode(false);
            playerMovement.SetMovementEnabled(false);
        }

        // Joystick remains visible.
        if (fixedJoystick != null)
        {
            fixedJoystick.SetActive(true);
        }

        SetAimVisuals(false);
        pickupCollider.enabled = false;
    }

    // Called by AE_AttachBall on the pickup animation.
    public void AttachPendingBall()
    {
        if (pendingBall == null || ballHoldPoint == null)
        {
            return;
        }

        if (!pendingBall.AttachToHoldPoint(ballHoldPoint))
        {
            return;
        }

        HeldBall = pendingBall;
        pendingBall = null;

        // Enter slow aim movement before re-enabling movement.
        if (playerMovement != null)
        {
            playerMovement.SetAimMovementMode(true);
            playerMovement.SetMovementEnabled(true, false);
        }

        if (fixedJoystick != null)
        {
            fixedJoystick.SetActive(true);
        }

        SetAimVisuals(true);
    }

    public void CompleteThrow()
    {
        HeldBall = null;
        pendingBall = null;

        pickupCollider.enabled = true;
        SetAimVisuals(false);

        if (fixedJoystick != null)
        {
            fixedJoystick.SetActive(true);
        }

        if (playerMovement != null)
        {
            playerMovement.SetAimMovementMode(false);

            // Return to normal movement, but require the joystick
            // to be released before running again.
            playerMovement.SetMovementEnabled(true, true);
        }
    }

    private void SetAimVisuals(bool visible)
    {
        if (aimUIRoot != null)
        {
            aimUIRoot.SetActive(visible);
        }

        if (trajectoryPreview != null)
        {
            trajectoryPreview.SetActive(visible);
        }
    }

    private void OnDisable()
    {
        pendingBall = null;

        if (playerMovement != null)
        {
            playerMovement.SetAimMovementMode(false);
        }
    }
}