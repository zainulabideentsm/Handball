using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(CharacterController))]
public sealed class PlayerMovement : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Joystick movementJoystick;
    [SerializeField] private Transform cameraTransform;

    [Header("Movement")]
    [SerializeField, Min(0f)]
    private float maximumSpeed = 4.5f;

    [SerializeField, Min(0f)]
    private float acceleration = 18f;

    [SerializeField, Min(0f)]
    private float deceleration = 24f;

    [SerializeField, Min(0f)]
    private float rotationSpeed = 720f;

    [Header("Joystick Response")]
    [SerializeField, Range(0f, 0.5f)]
    private float inputDeadZone = 0.12f;

    [Tooltip("Minimum movement amount after the joystick leaves the dead zone.")]
    [SerializeField, Range(0f, 0.5f)]
    private float minimumWalkInput = 0.22f;

    [Tooltip("Removes small sideways drift when pushing mostly straight.")]
    [SerializeField, Range(0f, 0.5f)]
    private float axisLockRatio = 0.2f;

    [Header("Gravity")]
    [SerializeField]
    private float gravity = -25f;

    [SerializeField]
    private float groundedForce = -2f;

    public bool IsMoving { get; private set; }
    public float NormalizedSpeed { get; private set; }

    private CharacterController characterController;

    private Vector3 planarVelocity;
    private float verticalVelocity;

    private bool movementEnabled = true;
    private bool waitingForJoystickNeutral;

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();

        if (cameraTransform == null && Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
        }
    }

    private void Update()
    {
        if (movementJoystick == null || cameraTransform == null)
        {
            StopMovement();
            return;
        }

        float deltaTime = Time.deltaTime;

        if (!movementEnabled)
        {
            planarVelocity = Vector3.zero;

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

        float inputMagnitude = input.magnitude;

        Vector3 desiredDirection =
            CalculateCameraRelativeDirection(input);

        UpdateHorizontalVelocity(
            desiredDirection,
            inputMagnitude,
            deltaTime
        );

        UpdateRotation(desiredDirection, deltaTime);
        UpdateGravity(deltaTime);
        MoveCharacter(deltaTime);
        UpdateMovementData();
    }

    private Vector2 ReadJoystickInput()
    {
        Vector2 input = new Vector2(
            movementJoystick.Horizontal,
            movementJoystick.Vertical
        );

        float rawMagnitude = input.magnitude;

        if (rawMagnitude <= inputDeadZone)
        {
            return Vector2.zero;
        }

        float absoluteX = Mathf.Abs(input.x);
        float absoluteY = Mathf.Abs(input.y);

        // Remove slight horizontal drift when moving mostly vertically.
        if (absoluteX < absoluteY * axisLockRatio)
        {
            input.x = 0f;
        }
        // Remove slight vertical drift when moving mostly horizontally.
        else if (absoluteY < absoluteX * axisLockRatio)
        {
            input.y = 0f;
        }

        float adjustedMagnitude =
            Mathf.Clamp01(input.magnitude);

        if (adjustedMagnitude <= inputDeadZone)
        {
            return Vector2.zero;
        }

        float correctedMagnitude = Mathf.InverseLerp(
            inputDeadZone,
            1f,
            adjustedMagnitude
        );

        // Prevent unnaturally slow creeping immediately outside the dead zone.
        float movementMagnitude = Mathf.Lerp(
            minimumWalkInput,
            1f,
            correctedMagnitude
        );

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

        // Magnitude is handled separately in UpdateHorizontalVelocity.
        return direction.sqrMagnitude > 0.0001f
            ? direction.normalized
            : Vector3.zero;
    }

    private void UpdateHorizontalVelocity(
        Vector3 desiredDirection,
        float inputMagnitude,
        float deltaTime)
    {
        Vector3 targetVelocity =
            desiredDirection *
            maximumSpeed *
            inputMagnitude;

        float changeRate =
            targetVelocity.sqrMagnitude >
            planarVelocity.sqrMagnitude
                ? acceleration
                : deceleration;

        planarVelocity = Vector3.MoveTowards(
            planarVelocity,
            targetVelocity,
            changeRate * deltaTime
        );

        if (planarVelocity.sqrMagnitude < 0.0001f)
        {
            planarVelocity = Vector3.zero;
        }
    }

    private void UpdateRotation(
        Vector3 desiredDirection,
        float deltaTime)
    {
        if (desiredDirection.sqrMagnitude <= 0.001f)
        {
            return;
        }

        Quaternion targetRotation = Quaternion.LookRotation(
            desiredDirection,
            Vector3.up
        );

        transform.rotation = Quaternion.RotateTowards(
            transform.rotation,
            targetRotation,
            rotationSpeed * deltaTime
        );
    }

    private void UpdateGravity(float deltaTime)
    {
        if (characterController.isGrounded &&
            verticalVelocity < 0f)
        {
            verticalVelocity = groundedForce;
        }
        else
        {
            verticalVelocity += gravity * deltaTime;
        }
    }

    private void MoveCharacter(float deltaTime)
    {
        Vector3 finalVelocity = planarVelocity;
        finalVelocity.y = verticalVelocity;

        characterController.Move(
            finalVelocity * deltaTime
        );
    }

    private void UpdateMovementData()
    {
        float planarSpeed = planarVelocity.magnitude;

        IsMoving = planarSpeed > 0.05f;

        NormalizedSpeed = maximumSpeed > 0f
            ? Mathf.Clamp01(planarSpeed / maximumSpeed)
            : 0f;
    }

    private void StopMovement()
    {
        planarVelocity = Vector3.zero;
        IsMoving = false;
        NormalizedSpeed = 0f;
    }

    public void SetMovementEnabled(bool enabled)
    {
        movementEnabled = enabled;
        planarVelocity = Vector3.zero;

        IsMoving = false;
        NormalizedSpeed = 0f;

        if (!enabled)
        {
            waitingForJoystickNeutral = false;
            return;
        }

        // Require the joystick to return to neutral after aiming/throwing.
        waitingForJoystickNeutral = true;
    }
}