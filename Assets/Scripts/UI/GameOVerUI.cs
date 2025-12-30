using UnityEngine;
using UnityEngine.SceneManagement;

public class GameOverUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject root; // GameOverRoot (panel)

    [Header("Navigation")]
    [SerializeField] private string stageSelectSceneName = "MainManu";

    private void Awake()
    {
        // IMPORTANT:
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

        Time.timeScale = 0f;
    }

    public void Hide()
    {
        Time.timeScale = 1f;

        if (root != null)
            root.SetActive(false);
    }

    public void TryAgain()
    {
        // Always restore time scale before doing anything else
        Time.timeScale = 1f;

        // Hide game over UI immediately
        if (root != null)
            root.SetActive(false);

        // Reset managers first (so next run starts clean)
        if (PenaltyManager.Instance != null)
            PenaltyManager.Instance.ResetRunState();

        if (MoneyManager.Instance != null)
            MoneyManager.Instance.ResetToDefaults();

        if (FuelManager.Instance != null)
            FuelManager.Instance.ResetToDefaults();

        // Reset run context AFTER resets (optional, depends on your design)
        if (RunContext.Instance != null)
            RunContext.Instance.StartNewRun();

        // Go back to stage select / main menu
        SceneManager.LoadScene(stageSelectSceneName);
    }
}
