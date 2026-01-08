using UnityEngine;

/// <summary>
/// RunContext is a persistent runtime state holder.
/// It survives scene loads and tells managers what to do on the NEXT scene load:
/// - Fuel behavior (keep / reset / preset)
/// - Money reset or keep
/// - Player position persistence between scenes
/// 
/// IMPORTANT:
/// - All flags/policies are CONSUMED once on scene load.
/// - Managers should read them in OnSceneLoaded and then forget them.
/// </summary>
public class RunContext : MonoBehaviour
{
    public static RunContext Instance { get; private set; }

    // =========================================================
    // MONEY POLICY
    // =========================================================

    // If true -> MoneyManager should reset money on the NEXT scene load.
    private bool resetMoneyOnNextScene = false;

    // =========================================================
    // FUEL POLICY
    // =========================================================

    public enum FuelPolicy
    {
        None,              // No instruction (FuelManager fallback behavior)
        KeepCurrent,       // Keep current fuel value
        ResetToDefault,    // Reset fuel to startingFuel
        SetToPreset        // Set fuel to a specific preset value
    }

    private FuelPolicy fuelPolicyNextScene = FuelPolicy.None;
    private int presetFuelNextScene = -1;

    // =========================================================
    // PLAYER POSITION PERSISTENCE
    // =========================================================

    // Saved player world position between scenes
    public Vector3 SavedPlayerPosition { get; private set; }

    // True if a valid position was saved and should be restored once
    public bool HasSavedPlayerPosition { get; private set; } = false;

    // =========================================================
    // UNITY LIFECYCLE
    // =========================================================

    private void Awake()
    {
        // Singleton
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // =========================================================
    // CALLED FROM UI / GAME FLOW (BEFORE LOADING A SCENE)
    // =========================================================

    /// <summary>
    /// Start a completely new run from the beginning.
    /// - Money resets
    /// - Fuel resets to default
    /// - Player position is NOT restored
    /// </summary>
    public void StartNewRunResetFuel()
    {
        resetMoneyOnNextScene = true;

        fuelPolicyNextScene = FuelPolicy.ResetToDefault;
        presetFuelNextScene = -1;

        ClearPlayerPosition();
    }

    /// <summary>
    /// Start from the beginning but KEEP current fuel.
    /// Useful if you want to restart the map but not punish fuel.
    /// </summary>
    public void StartFromBeginningKeepFuel()
    {
        resetMoneyOnNextScene = true;

        fuelPolicyNextScene = FuelPolicy.KeepCurrent;
        presetFuelNextScene = -1;

        ClearPlayerPosition();
    }

    /// <summary>
    /// Selecting a specific stage from the menu.
    /// Fuel will be set to a preset value you choose.
    /// </summary>
    public void StartStageWithPresetFuel(int fuelAmount, bool resetMoney)
    {
        resetMoneyOnNextScene = resetMoney;

        fuelPolicyNextScene = FuelPolicy.SetToPreset;
        presetFuelNextScene = fuelAmount;

        ClearPlayerPosition();
    }

    /// <summary>
    /// Automatic progression to the next stage.
    /// - Money is kept
    /// - Fuel is kept
    /// - Player position SHOULD be restored
    /// </summary>
    public void ContinueRun()
    {
        resetMoneyOnNextScene = false;

        fuelPolicyNextScene = FuelPolicy.KeepCurrent;
        presetFuelNextScene = -1;
        // Player position is expected to be saved externally before scene load
    }

    /// <summary>
    /// Optional helper for debugging or hard reset.
    /// </summary>
    public void ResetAllFlags()
    {
        resetMoneyOnNextScene = false;
        fuelPolicyNextScene = FuelPolicy.None;
        presetFuelNextScene = -1;
        ClearPlayerPosition();
    }

    // =========================================================
    // PLAYER POSITION API
    // =========================================================

    /// <summary>
    /// Save the player's world position before loading the next scene.
    /// </summary>
    public void SavePlayerPosition(Vector3 position)
    {
        SavedPlayerPosition = position;
        HasSavedPlayerPosition = true;
    }

    /// <summary>
    /// Clear saved position so it won't be applied again.
    /// </summary>
    public void ClearPlayerPosition()
    {
        HasSavedPlayerPosition = false;
    }

    // =========================================================
    // CONSUMED BY MANAGERS (ONE-TIME FLAGS)
    // =========================================================

    /// <summary>
    /// Used by MoneyManager.
    /// Returns true ONCE if money should reset, then clears the flag.
    /// </summary>
    public bool ConsumeMoneyResetFlag()
    {
        if (!resetMoneyOnNextScene)
            return false;

        resetMoneyOnNextScene = false;
        return true;
    }

    /// <summary>
    /// Used by FuelManager.
    /// Returns true if a fuel policy was provided for this scene load.
    /// The policy is consumed and cleared immediately.
    /// </summary>
    public bool ConsumeFuelPolicy(out FuelPolicy policy, out int presetFuel)
    {
        policy = fuelPolicyNextScene;
        presetFuel = presetFuelNextScene;

        if (fuelPolicyNextScene == FuelPolicy.None)
            return false;

        // Consume once
        fuelPolicyNextScene = FuelPolicy.None;
        presetFuelNextScene = -1;
        return true;
    }

    // =========================================================
    // SAFETY
    // =========================================================

    /// <summary>
    /// Ensures RunContext exists.
    /// Call this before using RunContext.Instance if unsure.
    /// </summary>
    public static void EnsureExists()
    {
        if (Instance != null) return;

        GameObject go = new GameObject("RunContext");
        go.AddComponent<RunContext>();
        // Awake() will set Instance + DontDestroyOnLoad
    }
}
