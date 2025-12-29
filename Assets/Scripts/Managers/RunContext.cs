using UnityEngine;

public class RunContext : MonoBehaviour
{
    public static RunContext Instance { get; private set; }

    // If true -> the manager should reset on the NEXT scene load (consumed once).
    private bool resetMoneyOnNextScene = false;
    private bool resetFuelOnNextScene = false;

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
    // Run control (what you call from UI / progression scripts)
    // =========================================================

    /// <summary>
    /// Call when starting a NEW run from stage select / main menu.
    /// This means money and fuel should reset to their starting values.
    /// </summary>
    public void StartNewRun()
    {
        resetMoneyOnNextScene = true;
        resetFuelOnNextScene = true;
    }

    /// <summary>
    /// Call when advancing automatically to the next stage.
    /// This means money and fuel should be preserved.
    /// </summary>
    public void ContinueRun()
    {
        resetMoneyOnNextScene = false;
        resetFuelOnNextScene = false;
    }

    /// <summary>
    /// Optional helper for debugging or "hard restart".
    /// </summary>
    public void ResetAllFlags()
    {
        resetMoneyOnNextScene = false;
        resetFuelOnNextScene = false;
    }

    // =========================================================
    // Consumed by MoneyManager / FuelManager (one-time flags)
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
    /// Returns true ONCE if fuel should reset, then clears the flag.
    /// </summary>
    public bool ConsumeFuelResetFlag()
    {
        if (!resetFuelOnNextScene)
            return false;

        resetFuelOnNextScene = false;
        return true;
    }

    // =========================================================
    // Safety: optional auto-create RunContext if missing
    // =========================================================

    /// <summary>
    /// If you ever call RunContext.Instance and it's null,
    /// you can call RunContext.EnsureExists() before using it.
    /// </summary>
    public static void EnsureExists()
    {
        if (Instance != null) return;

        GameObject go = new GameObject("RunContext");
        go.AddComponent<RunContext>();
        // Awake will run and set Instance + DontDestroyOnLoad
    }
}