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
    public Transform[] spineBones;
    public PlayerStats playerStats;
    private float maxSpineBendAngle = 30f;
    private Quaternion[] _initialSpineRots;

    // Animation hashes
    private readonly int hashIsWalking = Animator.StringToHash("isWalking");
    private readonly int hashIsRunning = Animator.StringToHash("isRunning");
    private readonly int hashIsJumping = Animator.StringToHash("isJumping");
    private readonly int hashIsCrouching = Animator.StringToHash("isCrouching");
    private readonly int hashIsCrouchWalking = Animator.StringToHash("isCrouchWalking");

    private bool jumpRequested = false;
    private bool sprintRequested = false;
    private bool isSprinting = false;

    private CharacterController controller;
    private PlayerControls controls;

    private Vector2 moveInput;
    private Vector2 lookInput;
    private float verticalVelocity = 0f;
    private float xRotation = 0f;
    private float currentStamina;
    private Vector3 currentVelocity = Vector3.zero;
    private Vector3 lastPosition;
    private bool crouchRequested = false;
    private bool isCrouching = false;
    private float crouchHeight;
    private float standCameraHeight;
    private float crouchCameraHeight;
    private float currentHeight;
    private float currentCameraHeight;

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

        crouchHeight = playerStats.StandHeight * 0.5f;
        crouchCameraHeight = crouchHeight * 0.8f;
        standCameraHeight = playerStats.StandHeight * 0.9f;

        currentHeight = playerStats.StandHeight;
        currentCameraHeight = standCameraHeight;

        controller.height = playerStats.StandHeight;
        controller.center = new Vector3(0, playerStats.StandHeight / 2f, 0);

        // Set height of player model
        playerModelTransform.localPosition = Vector3.zero;
        playerModelTransform.localScale = new Vector3(playerStats.StandHeight, playerStats.StandHeight, playerStats.StandHeight);

        // Set camera height
        cameraTransform.localPosition = new Vector3(cameraTransform.localPosition.x, standCameraHeight, cameraTransform.localPosition.z);

        currentStamina = playerStats.MaxStamina;
        lastPosition = transform.position;

        _initialSpineRots = new Quaternion[spineBones.Length];
        for (int i = 0; i < spineBones.Length; i++)
            _initialSpineRots[i] = spineBones[i].localRotation;
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
        float mouseX = lookInput.x * playerStats.MouseSensitivity;
        float mouseY = lookInput.y * playerStats.MouseSensitivity;

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        cameraTransform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        transform.Rotate(Vector3.up * mouseX);

        float normalizedPitch = xRotation / 90f;
        for (int i = 0; i < spineBones.Length; i++)
        {
            // weight bones so the top ones bend more
            float weight = (float)(i + 1) / spineBones.Length;
            float bendAngle = normalizedPitch * maxSpineBendAngle * weight;
            // negative so that looking up (xRotation<0) bends spine backwards
            spineBones[i].localRotation = _initialSpineRots[i] *
                                          Quaternion.Euler(bendAngle, 0f, 0f);
        }
    }

    private void HandleMovement()
    {
        // 1) Build input and desired velocity
        Vector3 inputDirection = new Vector3(moveInput.x, 0f, moveInput.y);
        Vector3 desiredWalkVelocity = transform.TransformDirection(inputDirection.normalized) * playerStats.MaxWalkSpeed;
        Vector3 desiredSprintVelocity = transform.TransformDirection(inputDirection.normalized) * playerStats.MaxSprintSpeed;
        Vector3 desiredCrouchVelocity = transform.TransformDirection(inputDirection.normalized) * playerStats.MaxCrouchSpeed;

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
                        playerStats.GroundDeceleration * Time.deltaTime
                    );
                }
                else
                {
                    // Accelerating normally
                    currentVelocity = Vector3.MoveTowards(
                        currentVelocity,
                        desiredWalkVelocity,
                        playerStats.GroundAcceleration * Time.deltaTime
                    );
                }
            }
            else
            {
                // 2b) In air with input: same accel rate
                currentVelocity = Vector3.MoveTowards(
                    currentVelocity,
                    desiredWalkVelocity,
                    playerStats.AirAcceleration * Time.deltaTime
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
                        playerStats.GroundDeceleration * Time.deltaTime
                    );
                }
                else
                {
                    // Accelerating normally
                    currentVelocity = Vector3.MoveTowards(
                        currentVelocity,
                        desiredSprintVelocity,
                        playerStats.GroundAcceleration * Time.deltaTime
                    );
                }
            }
            else
            {
                // 2b) In air with input: same accel rate
                currentVelocity = Vector3.MoveTowards(
                    currentVelocity,
                    desiredSprintVelocity,
                    playerStats.AirAcceleration * Time.deltaTime
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
                        playerStats.GroundDeceleration * Time.deltaTime
                    );
                }
                else
                {
                    // Accelerating normally
                    currentVelocity = Vector3.MoveTowards(
                        currentVelocity,
                        desiredCrouchVelocity,
                        playerStats.GroundAcceleration * Time.deltaTime
                    );
                }
            }
            else
            {
                // 2b) In air with input: same accel rate
                currentVelocity = Vector3.MoveTowards(
                    currentVelocity,
                    desiredCrouchVelocity,
                    playerStats.AirAcceleration * Time.deltaTime
                );
            }
        }
        else
        {
            // 3) No input → decelerate toward zero
            float deceleration;
            if (controller.isGrounded)
            {
                deceleration = playerStats.GroundDeceleration;
            }
            else
            {
                deceleration = playerStats.AirDeceleration;
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
        verticalVelocity += playerStats.Gravity * Time.deltaTime;

        // Drain stamina
        if (currentVelocity.magnitude > 0f && isSprinting && lastPosition != transform.position)
        {
            currentStamina -= playerStats.StaminaDrainRate * Time.deltaTime;
            currentStamina = Mathf.Max(currentStamina, 0f);
        }
        else
        {
            currentStamina += playerStats.StaminaRegenRate * Time.deltaTime;
            currentStamina = Mathf.Min(currentStamina, playerStats.MaxStamina);
        }
    }

    private void HandleJump()
    {
        if (jumpRequested && controller.isGrounded)
        {
            jumpRequested = false;
            verticalVelocity = Mathf.Sqrt(playerStats.JumpHeight * -2f * playerStats.Gravity);
        }
        else if (GetDistanceToGround() > 0.2f)
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
        float targetHeight = isCrouching ? crouchHeight : playerStats.StandHeight;
        float targetCameraY = isCrouching ? crouchCameraHeight : standCameraHeight;

        // Smoothly lerp height and camera
        currentHeight = Mathf.Lerp(currentHeight, targetHeight, Time.deltaTime * playerStats.CrouchTransitionSpeed);
        controller.height = currentHeight;
        controller.center = new Vector3(0, currentHeight / 2f, 0);

        currentCameraHeight = Mathf.Lerp(currentCameraHeight, targetCameraY, Time.deltaTime * playerStats.CrouchTransitionSpeed);
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
        float distanceToCheck = playerStats.StandHeight - crouchHeight;

        return !Physics.SphereCast(top, controller.radius, Vector3.up, out _, distanceToCheck);
    }

    private void UpdateAnimatorBools()
    {
        bool grounded = controller.isGrounded;

        bool walking = moveInput.magnitude > 0.1f && grounded && !isSprinting && !isCrouching;

        bool running = moveInput.magnitude > 0.1f && grounded && isSprinting && !isCrouching;

        bool jumping = !grounded;
        bool crouching = isCrouching;
        bool crouchWalking = moveInput.magnitude > 0.1f && isCrouching;

        animator.SetBool(hashIsWalking, walking);
        animator.SetBool(hashIsRunning, running);
        animator.SetBool(hashIsJumping, jumping);
        animator.SetBool(hashIsCrouching, crouching);
        animator.SetBool(hashIsCrouchWalking, crouchWalking);
    }
}
