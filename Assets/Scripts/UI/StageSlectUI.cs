using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class StageSelectUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TextMeshProUGUI selectedStageText;

    // Holds the scene name of the selected stage
    private string selectedSceneName = null;

    private void Start()
    {
        UpdateSelectedStageText();
    }

    /// <summary>
    /// Called when a stage button is clicked.
    /// Stores the selected scene name for later.
    /// </summary>
    /// <param name="sceneName">Scene name to load later</param>
    public void SelectStage(string sceneName)
    {
        selectedSceneName = sceneName;
        UpdateSelectedStageText();
    }

    /// <summary>
    /// Called when the PLAY button is pressed.
    /// Rule:
    /// - Starting from the menu means a NEW RUN -> money should reset to 0.
    /// - Stage-to-stage progression should NOT reset money (handled elsewhere).
    /// </summary>
    public void PlaySelectedStage()
    {
        if (string.IsNullOrEmpty(selectedSceneName))
        {
            Debug.Log("StageSelectUI: No stage selected!");
            return;
        }

        // Make sure time scale is normal
        Time.timeScale = 1f;

        // Mark this as a NEW RUN started from the menu (Money resets on next scene load)
        if (RunContext.Instance != null)
        {
            RunContext.Instance.StartNewRun();
        }
        else
        {
            Debug.LogWarning("StageSelectUI: RunContext.Instance is missing! Money may not reset correctly.");
        }

        // Load the selected stage scene
        SceneManager.LoadScene(selectedSceneName);
    }

    /// <summary>
    /// Updates the UI text showing the selected stage.
    /// </summary>
    private void UpdateSelectedStageText()
    {
        if (selectedStageText == null)
            return;

        if (string.IsNullOrEmpty(selectedSceneName))
            selectedStageText.text = "Select a stage";
        else
            selectedStageText.text = "Selected: " + selectedSceneName;
    }
}