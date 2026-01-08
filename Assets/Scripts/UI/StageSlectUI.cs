using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// Stage selection screen:
/// - Selecting a stage loads it (auto-play).
/// - We configure RunContext BEFORE loading:
///   - Money reset (optional)
///   - Fuel preset per stage
///   - Player position cleared (because selecting a stage is NOT a continuation)
/// </summary>
public class StageSelectUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TextMeshProUGUI selectedStageText;

    // Holds the scene name of the selected stage
    private string selectedSceneName = null;

    [Header("Behavior")]
    [Tooltip("If true, selecting a stage immediately loads it.")]
    [SerializeField] private bool autoPlayOnSelect = true;

    private void Start()
    {
        UpdateSelectedStageText();
    }

    /// <summary>
    /// Called when a stage button is clicked.
    /// Stores the selected scene name for later.
    /// </summary>
    public void SelectStage(string sceneName)
    {
        selectedSceneName = sceneName;
        UpdateSelectedStageText();

        if (autoPlayOnSelect)
            PlaySelectedStage();
    }

    /// <summary>
    /// Loads the selected stage.
    /// Selecting a stage is treated as "new start", not continuation.
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

        // Ensure RunContext exists
        RunContext.EnsureExists();

        // Decide fuel preset for the selected stage (EDIT these values)
        int presetFuel = GetPresetFuelForScene(selectedSceneName);

        // Selecting a stage -> typically reset money (set false if you want to keep it)
        bool resetMoney = true;

        // Tell managers what to do on NEXT scene load:
        // - set fuel to preset for this stage
        // - optionally reset money
        // - clear saved position (stage select is not a continuation)
        RunContext.Instance.StartStageWithPresetFuel(presetFuel, resetMoney);

        // Load the selected stage scene
        SceneManager.LoadScene(selectedSceneName);
    }

    /// <summary>
    /// Example mapping: scene -> starting fuel amount.
    /// Change numbers and scene names to match your game.
    /// </summary>
    private int GetPresetFuelForScene(string sceneName)
    {
        switch (sceneName)
        {
            case "Stage1": return 5;
            case "Stage2": return 6;
            case "Stage3": return 7;
            case "Stage4": return 8;
            default: return 5;
        }
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
