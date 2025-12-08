using UnityEngine;
using System.Collections;

public class OpeningSequence : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerController player;          // Reference to the PlayerController
    [SerializeField] private GameObject introPanel;            // The intro screen panel

    [Header("UI Elements Hidden During Intro")]
    [SerializeField] private GameObject[] uiToHideDuringIntro; // HUD elements to hide before Start

    [Header("Post-Start Text")]
    [SerializeField] private GameObject welcomeTextObject;     // Big welcome text shown after Start
    [SerializeField] private HintManager hintManager;          // Hint manager for the first delivery

    private void Awake()
    {
        // Auto-assign this object as introPanel if none is set
        if (introPanel == null)
            introPanel = gameObject;
    }

    private void Start()
    {
        // Disable player movement at the beginning
        if (player != null)
            player.canMove = false;

        // Make sure intro panel is visible
        if (introPanel != null)
            introPanel.SetActive(true);

        // Hide HUD/UI elements until Start Game is pressed
        if (uiToHideDuringIntro != null)
        {
            foreach (GameObject ui in uiToHideDuringIntro)
            {
                if (ui != null)
                    ui.SetActive(false);
            }
        }

        // Hide welcome text until after Start
        if (welcomeTextObject != null)
            welcomeTextObject.SetActive(false);

        // Ensure hints are fully disabled at the beginning
        if (hintManager != null)
            hintManager.StopHints();
    }

    // Called by the START GAME button
    public void OnStartGamePressed()
    {
        // Hide the intro screen
        if (introPanel != null)
            introPanel.SetActive(false);

        // Show HUD/UI elements now that gameplay begins
        if (uiToHideDuringIntro != null)
        {
            foreach (GameObject ui in uiToHideDuringIntro)
            {
                if (ui != null)
                    ui.SetActive(true);
            }
        }

        // Allow player movement
        if (player != null)
            player.canMove = true;

        // Start sequence: welcome, then smart hints
        StartCoroutine(PostStartFlow());
    }

    private IEnumerator PostStartFlow()
    {
        // 1) Show welcome explanation
        if (welcomeTextObject != null)
            welcomeTextObject.SetActive(true);

        // Keep the welcome text on screen for a few seconds
        yield return new WaitForSeconds(4f);

        // Hide the welcome text
        if (welcomeTextObject != null)
            welcomeTextObject.SetActive(false);

        // 2) Start the hint system for the first delivery
        if (hintManager != null)
            hintManager.BeginHintsForFirstDelivery();
    }
}