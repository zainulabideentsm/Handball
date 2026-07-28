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
    private Joystick fixedJoystickComponent;

    private void Awake()
    {
        pickupCollider = GetComponent<SphereCollider>();
        pickupCollider.isTrigger = true;

        if (fixedJoystick != null)
        {
            fixedJoystickComponent =
                fixedJoystick.GetComponent<Joystick>();
        }

        SetAimVisuals(false);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (HasBall || IsPickingUp)
        {
            return;
        }

        BallController ball =
            other.GetComponentInParent<BallController>();

        if (ball == null || !ball.TryBeginPickup())
        {
            return;
        }

        pendingBall = ball;

        // Must happen before movement is disabled so the script can
        // correctly choose PickUp or RunningPickup.
        if (animationController != null)
        {
            animationController.PlayPickup();
        }

        BeginPickupSequence();
    }

    private void BeginPickupSequence()
    {
        if (playerMovement != null)
        {
            playerMovement.SetMovementEnabled(false);
        }

        if (fixedJoystick != null)
        {
            fixedJoystickComponent?.ResetJoystick();
            fixedJoystick.SetActive(false);
        }

        SetAimVisuals(false);

        pickupCollider.enabled = false;
    }

    // Called by an Animation Event.
    public void AttachPendingBall()
    {
        if (pendingBall == null)
        {
            return;
        }

        if (!pendingBall.AttachToHoldPoint(ballHoldPoint))
        {
            return;
        }

        HeldBall = pendingBall;
        pendingBall = null;

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
            fixedJoystickComponent?.ResetJoystick();
        }

        if (playerMovement != null)
        {
            playerMovement.SetMovementEnabled(true);
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
}