using UnityEngine;
using TMPro;

public class HintManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerController player;          // Player controller
    [SerializeField] private TextMeshProUGUI hintText;         // UI text used for hints

    [Header("Hint Settings")]
    [SerializeField] private int deliveriesWithHints = 1;      // Show hints only for this many deliveries

    private bool hintsActive = false;
    private bool lastHasPackage;
    private int baseCompletedDeliveries;

    private void Start()
    {
        // Clamp to at least 1 so hints will not instantly disable
        if (deliveriesWithHints < 1)
            deliveriesWithHints = 1;

        // Start with no text and make sure the hint object is hidden
        if (hintText != null)
        {
            hintText.text = "";
            hintText.gameObject.SetActive(false);
        }
        else
        {
            Debug.LogError("HintManager: HintText reference is missing.");
        }

        if (player == null)
        {
            Debug.LogError("HintManager: Player reference is missing.");
        }
    }

    // Called from OpeningSequence AFTER the welcome message
    public void BeginHintsForFirstDelivery()
    {
        if (player == null || hintText == null)
        {
            Debug.LogError("HintManager: Cannot begin hints, missing Player or HintText.");
            return;
        }

        hintsActive = true;
        baseCompletedDeliveries = player.deliveriesCompleted;
        lastHasPackage = player.HasPackage;

        hintText.gameObject.SetActive(true);
        UpdateHint();
    }

    // Called from OpeningSequence at Start to make sure hints are off
    public void StopHints()
    {
        hintsActive = false;

        if (hintText != null)
        {
            hintText.text = "";
            hintText.gameObject.SetActive(false);
        }
    }

    private void Update()
    {
        if (!hintsActive || player == null)
            return;

        // Stop showing hints after the first delivery (or more if configured)
        int deliveredSinceStart = player.deliveriesCompleted - baseCompletedDeliveries;
        if (deliveredSinceStart >= deliveriesWithHints)
        {
            StopHints();
            return;
        }

        // Update hints when the package state changes (pickup / dropoff)
        if (player.HasPackage != lastHasPackage)
        {
            lastHasPackage = player.HasPackage;
            UpdateHint();
        }
    }

    private void UpdateHint()
    {
        if (hintText == null || player == null)
            return;

        if (!player.HasPackage)
        {
            // Player has no package yet
            hintText.text = "Drive to the pickup point to collect your first order.";
        }
        else
        {
            // Player is carrying a package
            hintText.text = "Follow the purple path and deliver the package.";
        }
    }
}