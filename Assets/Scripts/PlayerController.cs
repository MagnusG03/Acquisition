using System;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour, PlayerControls.IPlayerActions
{
    [Header("References")]
    public Transform cameraTransform;
    public Transform playerModelTransform;
    public Animator animator;

    private readonly int hashIsWalking = Animator.StringToHash("isWalking");
    private readonly int hashIsRunning = Animator.StringToHash("isRunning");
    private readonly int hashIsJumping = Animator.StringToHash("isJumping");

    [Header("Settings")]
    public float mouseSensitivity = 0.1f;
    public float gravity = -9.81f;
    public float jumpHeight = 1f;
    private bool jumpRequested = false;
    private bool sprintRequested = false;
    private bool isSprinting = false;

    private CharacterController controller;
    private PlayerControls controls;

    private Vector2 moveInput;
    private Vector2 lookInput;
    private float verticalVelocity = 0f;
    private float xRotation = 0f;
    public float groundAcceleration = 30f;
    public float airAcceleration = 15f;
    public float groundDeceleration = 75f;
    public float airDeceleration = 10f;
    public float maxWalkSpeed = 4f;
    public float maxSprintSpeed = 8f;
    public float maxStamina = 100f;
    public float staminaDrainRate = 15f;
    public float staminaRegenRate = 2.5f;
    private float currentStamina;
    private Vector3 currentVelocity = Vector3.zero;
    private Vector3 lastPosition;
    private bool crouchRequested = false;
    private bool isCrouching = false;
    public float maxCrouchSpeed = 1.5f;
    public float standHeight = 1.8f;
    private float crouchHeight;
    private float standCameraHeight;
    private float crouchCameraHeight;
    private float currentHeight;
    private float currentCameraHeight;
    public float crouchTransitionSpeed = 10f;

    void Awake()
    {
        controls = new PlayerControls();
        controls.Player.SetCallbacks(this);
    }

    void OnEnable() => controls.Player.Enable();
    void OnDisable() => controls.Player.Disable();

    void Start()
    {
        controller = GetComponent<CharacterController>();
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        crouchHeight = standHeight * 0.5f;
        crouchCameraHeight = crouchHeight * 0.8f;
        standCameraHeight = standHeight * 0.9f;

        currentHeight = standHeight;
        currentCameraHeight = standCameraHeight;

        controller.height = standHeight;
        controller.center = new Vector3(0, standHeight / 2f, 0);

        // Set height of player model
        playerModelTransform.localPosition = Vector3.zero;
        playerModelTransform.localScale = new Vector3(standHeight, standHeight, standHeight);

        // Set camera height
        cameraTransform.localPosition = new Vector3(cameraTransform.localPosition.x, standCameraHeight, cameraTransform.localPosition.z);

        currentStamina = maxStamina;
        lastPosition = transform.position;
    }

    void Update()
    {
        sprintRequested = Keyboard.current.leftShiftKey.isPressed;
        crouchRequested = Keyboard.current.leftCtrlKey.isPressed;

        HandleLook();
        HandleSprint();
        HandleJump();
        HandleCrouch();
        HandleMovement();
        UpdateAnimatorBools();
    }

    public void OnMove(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();
    }

    public void OnLook(InputAction.CallbackContext context)
    {
        lookInput = context.ReadValue<Vector2>();
    }

    public void OnJump(InputAction.CallbackContext context)
    {
        if (context.performed)
            jumpRequested = true;
    }

    public void OnSprint(InputAction.CallbackContext context)
    {
    }

    public void OnCrouch(InputAction.CallbackContext context)
    {
    }

    private void HandleLook()
    {
        float mouseX = lookInput.x * mouseSensitivity;
        float mouseY = lookInput.y * mouseSensitivity;

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        cameraTransform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        transform.Rotate(Vector3.up * mouseX);
    }

    private void HandleMovement()
    {
        // 1) Build input and desired velocity
        Vector3 inputDirection = new Vector3(moveInput.x, 0f, moveInput.y);
        Vector3 desiredWalkVelocity = transform.TransformDirection(inputDirection.normalized) * maxWalkSpeed;
        Vector3 desiredSprintVelocity = transform.TransformDirection(inputDirection.normalized) * maxSprintSpeed;
        Vector3 desiredCrouchVelocity = transform.TransformDirection(inputDirection.normalized) * maxCrouchSpeed;

        lastPosition = transform.position;

        // 2) If there’s input…
        if (inputDirection.magnitude > 0.1f && !isSprinting && !isCrouching)
        {
            if (controller.isGrounded)
            {
                // 2a) Grounded: are we reversing?
                float dot = Vector3.Dot(currentVelocity, desiredWalkVelocity);
                if (dot < 0f)
                {
                    // Braking (fast deceleration)
                    currentVelocity = Vector3.MoveTowards(
                        currentVelocity,
                        desiredWalkVelocity,
                        groundDeceleration * Time.deltaTime
                    );
                }
                else
                {
                    // Accelerating normally
                    currentVelocity = Vector3.MoveTowards(
                        currentVelocity,
                        desiredWalkVelocity,
                        groundAcceleration * Time.deltaTime
                    );
                }
            }
            else
            {
                // 2b) In air with input: same accel rate
                currentVelocity = Vector3.MoveTowards(
                    currentVelocity,
                    desiredWalkVelocity,
                    airAcceleration * Time.deltaTime
                );
            }
        }
        else if (inputDirection.magnitude > 0.1f && isSprinting && !isCrouching)
        {
            if (controller.isGrounded)
            {
                // 2a) Grounded: are we reversing?
                float dot = Vector3.Dot(currentVelocity, desiredSprintVelocity);
                if (dot < 0f)
                {
                    // Braking (fast deceleration)
                    currentVelocity = Vector3.MoveTowards(
                        currentVelocity,
                        desiredSprintVelocity,
                        groundDeceleration * Time.deltaTime
                    );
                }
                else
                {
                    // Accelerating normally
                    currentVelocity = Vector3.MoveTowards(
                        currentVelocity,
                        desiredSprintVelocity,
                        groundAcceleration * Time.deltaTime
                    );
                }
            }
            else
            {
                // 2b) In air with input: same accel rate
                currentVelocity = Vector3.MoveTowards(
                    currentVelocity,
                    desiredSprintVelocity,
                    airAcceleration * Time.deltaTime
                );
            }
        }
        else if (inputDirection.magnitude > 0.1f && isCrouching)
        {
            if (controller.isGrounded)
            {
                // 2a) Grounded: are we reversing?
                float dot = Vector3.Dot(currentVelocity, desiredCrouchVelocity);
                if (dot < 0f)
                {
                    // Braking (fast deceleration)
                    currentVelocity = Vector3.MoveTowards(
                        currentVelocity,
                        desiredCrouchVelocity,
                        groundDeceleration * Time.deltaTime
                    );
                }
                else
                {
                    // Accelerating normally
                    currentVelocity = Vector3.MoveTowards(
                        currentVelocity,
                        desiredCrouchVelocity,
                        groundAcceleration * Time.deltaTime
                    );
                }
            }
            else
            {
                // 2b) In air with input: same accel rate
                currentVelocity = Vector3.MoveTowards(
                    currentVelocity,
                    desiredCrouchVelocity,
                    airAcceleration * Time.deltaTime
                );
            }
        }
        else
        {
            // 3) No input → decelerate toward zero
            float deceleration;
            if (controller.isGrounded)
            {
                deceleration = groundDeceleration;
            }
            else
            {
                deceleration = airDeceleration;
            }

            currentVelocity = Vector3.MoveTowards(
                currentVelocity,
                Vector3.zero,
                deceleration * Time.deltaTime
            );
        }

        // 4) Apply vertical velocity & move
        Vector3 totalVelocity = currentVelocity + Vector3.up * verticalVelocity;
        controller.Move(totalVelocity * Time.deltaTime);

        // 5) Gravity reset on ground
        if (controller.isGrounded && verticalVelocity < 0f)
        {
            verticalVelocity = -2f;
        }
        verticalVelocity += gravity * Time.deltaTime;

        // Drain stamina
        if (currentVelocity.magnitude > 0f && isSprinting && lastPosition != transform.position)
        {
            currentStamina -= staminaDrainRate * Time.deltaTime;
            currentStamina = Mathf.Max(currentStamina, 0f);
        }
        else
        {
            currentStamina += staminaRegenRate * Time.deltaTime;
            currentStamina = Mathf.Min(currentStamina, maxStamina);
        }
    }

    private void HandleJump()
    {
        if (jumpRequested && controller.isGrounded)
        {
            jumpRequested = false;
            verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }
        else if (GetDistanceToGround() > 0.5f)
        {
            jumpRequested = false;
        }
    }

    private void HandleSprint()
    {
        if (!isSprinting && sprintRequested && currentStamina > 0f && controller.isGrounded && !isCrouching)
        {
            isSprinting = true;
        }

        if (!sprintRequested || currentStamina <= 0f)
        {
            isSprinting = false;
        }
    }

    private void HandleCrouch()
    {
        float targetHeight = isCrouching ? crouchHeight : standHeight;
        float targetCameraY = isCrouching ? crouchCameraHeight : standCameraHeight;

        // Smoothly lerp height and camera
        currentHeight = Mathf.Lerp(currentHeight, targetHeight, Time.deltaTime * crouchTransitionSpeed);
        controller.height = currentHeight;
        controller.center = new Vector3(0, currentHeight / 2f, 0);

        currentCameraHeight = Mathf.Lerp(currentCameraHeight, targetCameraY, Time.deltaTime * crouchTransitionSpeed);
        cameraTransform.localPosition = new Vector3(
            cameraTransform.localPosition.x,
            currentCameraHeight,
            cameraTransform.localPosition.z
        );

        // Handle crouch input state
        if (crouchRequested && !isCrouching)
        {
            isCrouching = true;
        }
        else if (!crouchRequested && isCrouching)
        {
            if (CanStandUp())
            {
                isCrouching = false;
            }
        }
    }

    float GetDistanceToGround()
    {
        Vector3 bottom = controller.transform.position + controller.center - new Vector3(0, controller.height / 2f, 0);

        RaycastHit hit;
        if (Physics.Raycast(bottom, Vector3.down, out hit, 5f))
        {
            return hit.distance;
        }

        return Mathf.Infinity;
    }

    private bool CanStandUp()
    {
        Vector3 top = controller.transform.position + controller.center + Vector3.up * (controller.height / 2f);
        float distanceToCheck = standHeight - crouchHeight;

        return !Physics.SphereCast(top, controller.radius, Vector3.up, out _, distanceToCheck);
    }

        private void UpdateAnimatorBools()
    {
        bool grounded = controller.isGrounded;

        bool walking = moveInput.magnitude > 0.1f && grounded && !isSprinting;

        bool running = moveInput.magnitude > 0.1f && grounded && isSprinting;

        bool jumping = !grounded;

        animator.SetBool(hashIsWalking, walking);
        animator.SetBool(hashIsRunning, running);
        animator.SetBool(hashIsJumping, jumping);
    }
}
