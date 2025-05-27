using System;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour, PlayerControls.IPlayerActions
{
    [Header("References")]
    public Transform cameraTransform;

    [Header("Settings")]
    public float mouseSensitivity = 0.1f;
    public float gravity = -9.81f;
    public float jumpHeight = 1f;
    private bool jumpRequested = false;

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
    public float maxSpeed = 6f;
    private Vector3 currentVelocity = Vector3.zero;

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
    }

    void Update()
    {
        HandleLook();
        HandleMovement();
        HandleJump();
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
        Vector3 desiredVelocity = transform.TransformDirection(inputDirection.normalized) * maxSpeed;

        // 2) If there’s input…
        if (inputDirection.magnitude > 0.1f)
        {
            if (controller.isGrounded)
            {
                // 2a) Grounded: are we reversing?
                float dot = Vector3.Dot(currentVelocity, desiredVelocity);
                if (dot < 0f)
                {
                    // Braking (fast deceleration)
                    currentVelocity = Vector3.MoveTowards(
                        currentVelocity,
                        desiredVelocity,
                        groundDeceleration * Time.deltaTime
                    );
                }
                else
                {
                    // Accelerating normally
                    currentVelocity = Vector3.MoveTowards(
                        currentVelocity,
                        desiredVelocity,
                        groundAcceleration * Time.deltaTime
                    );
                }
            }
            else
            {
                // 2b) In air with input: same accel rate
                currentVelocity = Vector3.MoveTowards(
                    currentVelocity,
                    desiredVelocity,
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
}
