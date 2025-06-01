using System.IO;
using UnityEngine;

[DisallowMultipleComponent]
public class PlayerStats : MonoBehaviour
{
    private readonly PlayerStatsData baseData = new PlayerStatsData();
    private PlayerStatsData currentData;
    public bool ignoreSavedData = false;

    // These helpers build a unique filename per player. Adjust PlayerID
    // to match however you identify each user/game‐slot.
    private string _saveFileName => $"player_{PlayerID}.json";
    private string PlayerID
    {
        get
        {
            // Replace this with your actual unique‐ID logic (account name,
            // network‐assigned ID, etc.). For now, we’ll just use the GameObject’s name:
            return gameObject.name.Replace(" ", "_");
        }
    }
    //–––––––––––––––––––––––––––––––––––––––––––––––––––––––––––––––––––

    private void Awake()
    {
        if (ignoreSavedData)
        {
            // Bypass LoadFromDisk completely—always use baseData.
            currentData = new PlayerStatsData();
            currentData.CopyFrom(baseData);
            Debug.Log("[PlayerStats] ignoreSavedData is true; using baseData.");
            return;
        }
        // On Awake, try loading saved stats. If none, copy from baseData.
        if (LoadFromDisk())
        {
            // currentData is now populated from disk
        }
        else
        {
            // no save found: copy defaults from baseData
            currentData = new PlayerStatsData();
            currentData.CopyFrom(baseData);
        }
    }

    //––––––––––––––––––––––––––––––––––––––––––––––––––––––––––––––––––
    // PUBLIC READERS (properties)
    //––––––––––––––––––––––––––––––––––––––––––––––––––––––––––––––––––
    public float MouseSensitivity => currentData.mouseSensitivity;
    public float Gravity => currentData.gravity;
    public float JumpHeight => currentData.jumpHeight;
    public float GroundAcceleration => currentData.groundAcceleration;
    public float AirAcceleration => currentData.airAcceleration;
    public float GroundDeceleration => currentData.groundDeceleration;
    public float AirDeceleration => currentData.airDeceleration;
    public float MaxWalkSpeed => currentData.maxWalkSpeed;
    public float MaxSprintSpeed => currentData.maxSprintSpeed;
    public float MaxStamina => currentData.maxStamina;
    public float StaminaDrainRate => currentData.staminaDrainRate;
    public float StaminaRegenRate => currentData.staminaRegenRate;
    public float MaxCrouchSpeed => currentData.maxCrouchSpeed;
    public float StandHeight => currentData.standHeight;
    public float CrouchTransitionSpeed => currentData.crouchTransitionSpeed;

    // If you ever need the entire data blob in one go:
    public PlayerStatsData GetAllStatsData()
    {
        return currentData;
    }

    //––––––––––––––––––––––––––––––––––––––––––––––––––––––––––––––––––
    // PUBLIC SETTERS (mutators)
    //––––––––––––––––––––––––––––––––––––––––––––––––––––––––––––––––––

    /// <summary>
    /// Set mouse sensitivity directly.
    /// </summary>
    public void SetMouseSensitivity(float newSens)
    {
        currentData.mouseSensitivity = newSens;
    }

    /// <summary>
    /// Set gravity (e.g. -9.81f for normal gravity, or change it mid‐game).
    /// </summary>
    public void SetGravity(float newGravity)
    {
        currentData.gravity = newGravity;
    }

    /// <summary>
    /// Set the jump height (units in world‐space).
    /// </summary>
    public void SetJumpHeight(float newJumpHeight)
    {
        currentData.jumpHeight = newJumpHeight;
    }

    /// <summary>
    /// Set ground‐acceleration (how fast you speed up on the ground).
    /// </summary>
    public void SetGroundAcceleration(float newGroundAccel)
    {
        currentData.groundAcceleration = newGroundAccel;
    }

    /// <summary>
    /// Set air‐acceleration (how fast you speed up while in the air).
    /// </summary>
    public void SetAirAcceleration(float newAirAccel)
    {
        currentData.airAcceleration = newAirAccel;
    }

    /// <summary>
    /// Set ground‐deceleration (how quickly you slow to a stop on the ground).
    /// </summary>
    public void SetGroundDeceleration(float newGroundDecel)
    {
        currentData.groundDeceleration = newGroundDecel;
    }

    /// <summary>
    /// Set air‐deceleration (how quickly you slow when no input in the air).
    /// </summary>
    public void SetAirDeceleration(float newAirDecel)
    {
        currentData.airDeceleration = newAirDecel;
    }

    /// <summary>
    /// Set the maximum walking speed.
    /// </summary>
    public void SetMaxWalkSpeed(float newMaxWalk)
    {
        currentData.maxWalkSpeed = newMaxWalk;
    }

    /// <summary>
    /// Set the maximum sprinting speed.
    /// </summary>
    public void SetMaxSprintSpeed(float newMaxSprint)
    {
        currentData.maxSprintSpeed = newMaxSprint;
    }

    /// <summary>
    /// Set the maximum stamina.
    /// </summary>
    public void SetMaxStamina(float newMaxStam)
    {
        currentData.maxStamina = newMaxStam;
    }

    /// <summary>
    /// Set the rate at which stamina drains while sprinting.
    /// </summary>
    public void SetStaminaDrainRate(float newDrainRate)
    {
        currentData.staminaDrainRate = newDrainRate;
    }

    /// <summary>
    /// Set the rate at which stamina regenerates when not sprinting.
    /// </summary>
    public void SetStaminaRegenRate(float newRegenRate)
    {
        currentData.staminaRegenRate = newRegenRate;
    }

    /// <summary>
    /// Set the maximum movement speed while crouched.
    /// </summary>
    public void SetMaxCrouchSpeed(float newCrouchSpeed)
    {
        currentData.maxCrouchSpeed = newCrouchSpeed;
    }

    /// <summary>
    /// Set the standing height (character controller height when standing).
    /// </summary>
    public void SetStandHeight(float newStandHeight)
    {
        currentData.standHeight = newStandHeight;
    }

    /// <summary>
    /// Set how fast the player transitions between crouch and stand.
    /// </summary>
    public void SetCrouchTransitionSpeed(float newTransitionSpeed)
    {
        currentData.crouchTransitionSpeed = newTransitionSpeed;
    }

    //––––––––––––––––––––––––––––––––––––––––––––––––––––––––––––––––––
    // SAVE / LOAD (JSON on disk)
    //––––––––––––––––––––––––––––––––––––––––––––––––––––––––––––––––––

    /// <summary>
    /// Saves the currentData to Application.persistentDataPath/<_saveFileName>.
    /// </summary>
    public bool SaveToDisk()
    {
        try
        {
            string json = JsonUtility.ToJson(currentData, prettyPrint: true);
            string path = Path.Combine(Application.persistentDataPath, _saveFileName);
            File.WriteAllText(path, json);
            Debug.Log($"[PlayerStats] Saved to {path}");
            return true;
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[PlayerStats] Failed to save: {ex}");
            return false;
        }
    }

    /// <summary>
    /// Loads currentData from Application.persistentDataPath/<_saveFileName>.
    /// Returns true if the file existed and was successfully parsed.
    /// </summary>
    public bool LoadFromDisk()
    {
        try
        {
            string path = Path.Combine(Application.persistentDataPath, _saveFileName);
            if (!File.Exists(path)) return false;

            string json = File.ReadAllText(path);
            PlayerStatsData loaded = JsonUtility.FromJson<PlayerStatsData>(json);
            if (loaded != null)
            {
                currentData = loaded;
                Debug.Log($"[PlayerStats] Loaded data from {path}");
                return true;
            }
            else
            {
                Debug.LogWarning($"[PlayerStats] JSON parse returned null (corrupt file?)");
                return false;
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[PlayerStats] Failed to load: {ex}");
            return false;
        }
    }

    // (Optional) Auto‐save on quit:
    private void OnApplicationQuit()
    {
        SaveToDisk();
    }
}
