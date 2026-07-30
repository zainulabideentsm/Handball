using System;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(CharacterController))]
public sealed class PlayerMovementController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Joystick movementJoystick;
    [SerializeField] private Transform cameraTransform;

    [Header("Normal Movement")]
    [SerializeField, Min(0f)] private float maximumSpeed = 4.5f;
    [SerializeField, Min(0f)] private float acceleration = 18f;
    [SerializeField, Min(0f)] private float deceleration = 24f;
    [SerializeField, Min(0f)] private float movementDirectionSpeed = 720f;
    [SerializeField, Min(0f)] private float rotationSpeed = 720f;

    [Header("Aim Movement")]
    [SerializeField, Min(0f)] private float aimMovementSpeed = 1.5f;

    [Tooltip("PlayerAimController controls rotation while this mode is active.")]
    [SerializeField] private bool allowJumpWhileAiming;

    [Header("Air Movement")]
    [SerializeField, Range(0f, 1f)] private float airControlMultiplier = 0.45f;

    [Header("Joystick Response")]
    [SerializeField, Range(0f, 0.5f)] private float inputDeadZone = 0.12f;
    [SerializeField, Range(0f, 0.5f)] private float minimumWalkInput = 0.22f;
    [SerializeField, Range(0f, 0.5f)] private float axisLockRatio = 0.2f;

    [Header("Jump")]
    [SerializeField, Min(0.1f)] private float jumpHeight = 1.5f;
    [SerializeField, Range(0f, 0.3f)] private float coyoteTime = 0.12f;
    [SerializeField, Range(0f, 0.3f)] private float jumpBufferTime = 0.15f;
    [SerializeField, Min(0f)] private float jumpCooldown = 0.15f;

    [Header("Gravity")]
    [SerializeField] private float gravity = -25f;
    [SerializeField] private float groundedForce = -2f;

    public event Action Jumped;
    public event Action<float> Landed;

    public bool IsMoving { get; private set; }
    public bool IsGrounded { get; private set; }
    public bool IsAimMovementMode { get; private set; }
    public bool MovementEnabled => movementEnabled;

    public float NormalizedSpeed { get; private set; }
    public float CurrentSpeed { get; private set; }
    public float InputMagnitude { get; private set; }

    public Vector2 MovementInput { get; private set; }
    public float AimMoveX => IsAimMovementMode ? MovementInput.x : 0f;
    public float AimMoveY => IsAimMovementMode ? MovementInput.y : 0f;

    private float ActiveMaximumSpeed =>
        IsAimMovementMode ? aimMovementSpeed : maximumSpeed;

    private CharacterController characterController;

    private Vector3 currentMoveDirection;
    private Vector3 planarVelocity;

    private float verticalVelocity;
    private float mostNegativeVerticalVelocity;
    private float lastGroundedTime = float.NegativeInfinity;
    private float lastJumpRequestTime = float.NegativeInfinity;
    private float nextJumpAllowedTime;

    private bool movementEnabled = true;
    private bool waitingForJoystickNeutral;
    private bool wasGroundedLastFrame;

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
        wasGroundedLastFrame = characterController.isGrounded;

        if (cameraTransform == null && Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
        }
    }

    private void Update()
    {
        float deltaTime = Time.deltaTime;

        UpdateGroundedState();

        if (movementJoystick == null || cameraTransform == null)
        {
            StopHorizontalMovement();
            UpdateGravity(deltaTime);
            MoveCharacter(deltaTime);
            UpdateMovementData();
            return;
        }

        if (!movementEnabled)
        {
            StopHorizontalMovement();
            UpdateGravity(deltaTime);
            MoveCharacter(deltaTime);
            UpdateMovementData();
            return;
        }

        Vector2 input = ReadJoystickInput();

        if (waitingForJoystickNeutral)
        {
            if (input.sqrMagnitude <= 0f)
            {
                waitingForJoystickNeutral = false;
            }
            else
            {
                input = Vector2.zero;
            }
        }

        MovementInput = input;
        InputMagnitude = input.magnitude;

        Vector3 desiredDirection =
            CalculateCameraRelativeDirection(input);

        UpdateHorizontalMovement(
            desiredDirection,
            InputMagnitude,
            deltaTime
        );

        if (!IsAimMovementMode)
        {
            UpdateRotation(deltaTime);
        }

        TryPerformJump();
        UpdateGravity(deltaTime);
        MoveCharacter(deltaTime);
        UpdateMovementData();
    }

    private void UpdateGroundedState()
    {
        IsGrounded = characterController.isGrounded;

        if (!IsGrounded)
        {
            return;
        }

        lastGroundedTime = Time.time;

        if (verticalVelocity < 0f)
        {
            verticalVelocity = groundedForce;
        }
    }

    private Vector2 ReadJoystickInput()
    {
        Vector2 input = new Vector2(
            movementJoystick.Horizontal,
            movementJoystick.Vertical
        );

        if (input.magnitude <= inputDeadZone)
        {
            return Vector2.zero;
        }

        float absoluteX = Mathf.Abs(input.x);
        float absoluteY = Mathf.Abs(input.y);

        if (absoluteX < absoluteY * axisLockRatio)
        {
            input.x = 0f;
        }
        else if (absoluteY < absoluteX * axisLockRatio)
        {
            input.y = 0f;
        }

        float adjustedMagnitude = Mathf.Clamp01(input.magnitude);

        if (adjustedMagnitude <= inputDeadZone)
        {
            return Vector2.zero;
        }

        float correctedMagnitude =
            Mathf.InverseLerp(inputDeadZone, 1f, adjustedMagnitude);

        float movementMagnitude =
            Mathf.Lerp(minimumWalkInput, 1f, correctedMagnitude);

        return input.normalized * movementMagnitude;
    }

    private Vector3 CalculateCameraRelativeDirection(Vector2 input)
    {
        if (input.sqrMagnitude <= 0f)
        {
            return Vector3.zero;
        }

        Vector3 cameraForward = cameraTransform.forward;
        Vector3 cameraRight = cameraTransform.right;

        cameraForward.y = 0f;
        cameraRight.y = 0f;

        cameraForward.Normalize();
        cameraRight.Normalize();

        Vector3 direction =
            cameraForward * input.y +
            cameraRight * input.x;

        return direction.sqrMagnitude > 0.0001f
            ? direction.normalized
            : Vector3.zero;
    }

    private void UpdateHorizontalMovement(
        Vector3 desiredDirection,
        float inputMagnitude,
        float deltaTime)
    {
        float targetSpeed = ActiveMaximumSpeed * inputMagnitude;

        float speedChangeRate =
            targetSpeed > CurrentSpeed
                ? acceleration
                : deceleration;

        if (!IsGrounded)
        {
            speedChangeRate *= airControlMultiplier;
        }

        CurrentSpeed = Mathf.MoveTowards(
            CurrentSpeed,
            targetSpeed,
            speedChangeRate * deltaTime
        );

        if (desiredDirection.sqrMagnitude > 0.0001f)
        {
            if (currentMoveDirection.sqrMagnitude < 0.0001f)
            {
                currentMoveDirection = desiredDirection;
            }
            else
            {
                float directionControl =
                    IsGrounded ? 1f : airControlMultiplier;

                float maximumRadians =
                    movementDirectionSpeed *
                    directionControl *
                    Mathf.Deg2Rad *
                    deltaTime;

                currentMoveDirection = Vector3.RotateTowards(
                    currentMoveDirection,
                    desiredDirection,
                    maximumRadians,
                    0f
                ).normalized;
            }
        }

        if (CurrentSpeed <= 0.001f)
        {
            CurrentSpeed = 0f;
            planarVelocity = Vector3.zero;

            if (desiredDirection.sqrMagnitude <= 0.0001f)
            {
                currentMoveDirection = Vector3.zero;
            }

            return;
        }

        planarVelocity = currentMoveDirection * CurrentSpeed;
    }

    private void UpdateRotation(float deltaTime)
    {
        if (currentMoveDirection.sqrMagnitude <= 0.0001f)
        {
            return;
        }

        Quaternion targetRotation =
            Quaternion.LookRotation(currentMoveDirection, Vector3.up);

        transform.rotation = Quaternion.RotateTowards(
            transform.rotation,
            targetRotation,
            rotationSpeed * deltaTime
        );
    }

    public void RequestJump()
    {
        if (!movementEnabled ||
            IsAimMovementMode && !allowJumpWhileAiming)
        {
            return;
        }

        lastJumpRequestTime = Time.time;
    }

    private void TryPerformJump()
    {
        if (!movementEnabled ||
            IsAimMovementMode && !allowJumpWhileAiming)
        {
            return;
        }

        if (Time.time < nextJumpAllowedTime)
        {
            return;
        }

        bool hasBufferedJump =
            Time.time - lastJumpRequestTime <= jumpBufferTime;

        if (!hasBufferedJump)
        {
            return;
        }

        bool canJump =
            Time.time - lastGroundedTime <= coyoteTime;

        if (!canJump)
        {
            return;
        }

        verticalVelocity =
            Mathf.Sqrt(jumpHeight * -2f * gravity);

        IsGrounded = false;
        lastJumpRequestTime = float.NegativeInfinity;
        lastGroundedTime = float.NegativeInfinity;
        nextJumpAllowedTime = Time.time + jumpCooldown;

        Jumped?.Invoke();
    }

    private void UpdateGravity(float deltaTime)
    {
        if (IsGrounded && verticalVelocity <= 0f)
        {
            verticalVelocity = groundedForce;
            return;
        }

        verticalVelocity += gravity * deltaTime;
    }

    private void MoveCharacter(float deltaTime)
    {
        if (verticalVelocity < mostNegativeVerticalVelocity)
        {
            mostNegativeVerticalVelocity = verticalVelocity;
        }

        Vector3 finalVelocity = planarVelocity;
        finalVelocity.y = verticalVelocity;

        CollisionFlags collisionFlags =
            characterController.Move(finalVelocity * deltaTime);

        bool touchedGround =
            (collisionFlags & CollisionFlags.Below) != 0;

        bool touchedCeiling =
            (collisionFlags & CollisionFlags.Above) != 0;

        if (touchedCeiling && verticalVelocity > 0f)
        {
            verticalVelocity = 0f;
        }

        IsGrounded = touchedGround;

        bool landedThisFrame =
            touchedGround && !wasGroundedLastFrame;

        if (touchedGround)
        {
            lastGroundedTime = Time.time;

            if (landedThisFrame &&
                mostNegativeVerticalVelocity < -0.1f)
            {
                Landed?.Invoke(-mostNegativeVerticalVelocity);
            }

            mostNegativeVerticalVelocity = 0f;

            if (verticalVelocity < 0f)
            {
                verticalVelocity = groundedForce;
            }
        }

        wasGroundedLastFrame = IsGrounded;
    }

    private void UpdateMovementData()
    {
        IsMoving = CurrentSpeed > 0.05f;

        NormalizedSpeed = maximumSpeed > 0f
            ? Mathf.Clamp01(CurrentSpeed / maximumSpeed)
            : 0f;
    }

    private void StopHorizontalMovement()
    {
        CurrentSpeed = 0f;
        InputMagnitude = 0f;
        MovementInput = Vector2.zero;
        currentMoveDirection = Vector3.zero;
        planarVelocity = Vector3.zero;
    }

    public void SetAimMovementMode(bool enabled)
    {
        if (IsAimMovementMode == enabled)
        {
            return;
        }

        IsAimMovementMode = enabled;
        lastJumpRequestTime = float.NegativeInfinity;
    }

    public void SetMovementEnabled(
        bool enabled,
        bool requireJoystickNeutral = true)
    {
        movementEnabled = enabled;

        StopHorizontalMovement();
        UpdateMovementData();

        lastJumpRequestTime = float.NegativeInfinity;

        if (!enabled)
        {
            waitingForJoystickNeutral = false;
            return;
        }

        waitingForJoystickNeutral = requireJoystickNeutral;
    }

    private void OnValidate()
    {
        maximumSpeed = Mathf.Max(0f, maximumSpeed);
        aimMovementSpeed = Mathf.Clamp(
            aimMovementSpeed,
            0f,
            maximumSpeed
        );

        gravity = Mathf.Min(gravity, -0.01f);
        groundedForce = Mathf.Min(groundedForce, -0.01f);
        jumpHeight = Mathf.Max(0.1f, jumpHeight);
    }
}