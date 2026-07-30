using UnityEngine;

[DisallowMultipleComponent]
public sealed class PlayerBallPickup : MonoBehaviour
{
    [Header("Required Player References")]
    [SerializeField] private PlayerMovementController playerMovement;
    [SerializeField] private PlayerAnimationController animationController;
    [SerializeField] private PlayerAudioController playerAudio;
    [SerializeField] private Transform ballHoldPoint;

    [Header("Pickup Trigger")]
    [Tooltip("Assign the SphereCollider on this BallPickupTrigger object.")]
    [SerializeField] private SphereCollider pickupCollider;

    [Header("Ball References")]
    [Tooltip("Assign the scene BallController.")]
    [SerializeField] private BallController pickupBall;

    [Tooltip("Assign the main Collider from the same ball.")]
    [SerializeField] private Collider pickupBallCollider;

    [Header("UI References")]
    [SerializeField] private GameObject fixedJoystick;
    [SerializeField] private GameObject aimUIRoot;

    [Header("Aim References")]
    [SerializeField] private GameObject trajectoryPreview;

    public BallController HeldBall { get; private set; }
    public bool HasBall => HeldBall != null;
    public bool IsPickingUp => pendingBall != null;

    private BallController pendingBall;

    private void Awake()
    {
        if (pickupCollider == null)
        {
            Debug.LogError("PlayerBallPickup: Pickup Collider is not assigned.", this);
            enabled = false;
            return;
        }

        if (pickupBall == null)
        {
            Debug.LogError("PlayerBallPickup: Pickup Ball is not assigned.", this);
            enabled = false;
            return;
        }

        if (pickupBallCollider == null)
        {
            Debug.LogError("PlayerBallPickup: Pickup Ball Collider is not assigned.", this);
            enabled = false;
            return;
        }

        pickupCollider.isTrigger = true;

        if (fixedJoystick != null)
        {
            fixedJoystick.SetActive(true);
        }

        SetAimVisuals(false);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (HasBall || IsPickingUp || other != pickupBallCollider)
        {
            return;
        }

        if (!pickupBall.TryBeginPickup())
        {
            return;
        }

        pendingBall = pickupBall;

        animationController?.PlayPickup();
        BeginPickupSequence();
    }

    private void BeginPickupSequence()
    {
        if (playerMovement != null)
        {
            playerMovement.SetAimMovementMode(false);
            playerMovement.SetMovementEnabled(false);
        }

        if (fixedJoystick != null)
        {
            fixedJoystick.SetActive(true);
        }

        SetAimVisuals(false);
        pickupCollider.enabled = false;
    }

    // Called by AE_AttachBall.
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

        playerAudio?.PlayPickup();

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

        if (pickupCollider != null)
        {
            pickupCollider.enabled = true;
        }

        SetAimVisuals(false);

        if (fixedJoystick != null)
        {
            fixedJoystick.SetActive(true);
        }

        if (playerMovement != null)
        {
            playerMovement.SetAimMovementMode(false);
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
}