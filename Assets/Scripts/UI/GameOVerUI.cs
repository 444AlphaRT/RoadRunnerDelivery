using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Controls the Game Over screen (show/hide) and handles "Try Again".
/// 
/// IMPORTANT:
/// - This component object should stay active.
/// - "root" is the full-screen panel (child) that we enable/disable.
/// </summary>
public class GameOverUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject root; // GameOverRoot (panel)

    [Header("Navigation")]
    [SerializeField] private string stageSelectSceneName = "MainManu";

    private void Awake()
    {
        // This script object MUST stay active.
        // root should be a CHILD panel (GameOverRoot), not this same GameObject.
        if (root == null)
        {
            Debug.LogError("GameOverUI: Root is not assigned! Assign GameOverRoot panel.");
            return;
        }

        root.SetActive(false); // hide only the panel
    }

    public void Show()
    {
        if (root != null)
            root.SetActive(true);

        // Pause the game while Game Over is shown
        Time.timeScale = 0f;
    }

    public void Hide()
    {
        // Resume time
        Time.timeScale = 1f;

        if (root != null)
            root.SetActive(false);
    }

    /// <summary>
    /// "Try Again" means: go back to the stage select / main menu,
    /// and start a NEW run next time you enter a stage.
    /// </summary>
    public void TryAgain()
    {
        // Always restore time scale before doing anything else
        Time.timeScale = 1f;

        // Hide game over UI immediately
        if (root != null)
            root.SetActive(false);

        // Reset penalty state now (it's a DontDestroy manager so we clear it immediately)
        if (PenaltyManager.Instance != null)
            PenaltyManager.Instance.ResetRunState();

        // Tell RunContext that the NEXT stage load should start a clean run:
        // - money resets
        // - fuel resets to defaults
        // - position should not be restored
        RunContext.EnsureExists();
        RunContext.Instance.StartNewRunResetFuel();

        // IMPORTANT:
        // Do NOT manually reset MoneyManager/FuelManager here if they are persistent.
        // Let them reset on the next gameplay scene load based on RunContext policy.
        // (This prevents double reset / inconsistent behavior.)

        // Go back to stage select / main menu
        SceneManager.LoadScene(stageSelectSceneName);
    }
}
