using System;

[Serializable]
public class PlayerStatsData
{
    public float mouseSensitivity = 0.1f;
    public float gravity = -9.81f;
    public float jumpHeight = 1f;
    public float groundAcceleration = 30f;
    public float airAcceleration = 15f;
    public float groundDeceleration = 75f;
    public float airDeceleration = 10f;
    public float maxWalkSpeed = 4f;
    public float maxSprintSpeed = 6f;
    public float maxStamina = 100f;
    public float staminaDrainRate = 15f;
    public float staminaRegenRate = 2.5f;
    public float maxCrouchSpeed = 1.5f;
    public float standHeight = 1.8f;
    public float crouchTransitionSpeed = 10f;

    public void CopyFrom(PlayerStatsData other)
    {
        mouseSensitivity = other.mouseSensitivity;
        gravity = other.gravity;
        jumpHeight = other.jumpHeight;
        groundAcceleration = other.groundAcceleration;
        airAcceleration = other.airAcceleration;
        groundDeceleration = other.groundDeceleration;
        airDeceleration = other.airDeceleration;
        maxWalkSpeed = other.maxWalkSpeed;
        maxSprintSpeed = other.maxSprintSpeed;
        maxStamina = other.maxStamina;
        staminaDrainRate = other.staminaDrainRate;
        staminaRegenRate = other.staminaRegenRate;
        maxCrouchSpeed = other.maxCrouchSpeed;
        standHeight = other.standHeight;
        crouchTransitionSpeed = other.crouchTransitionSpeed;
    }
}
