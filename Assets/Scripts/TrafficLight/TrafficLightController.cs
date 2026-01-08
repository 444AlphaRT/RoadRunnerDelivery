using System.Collections;
using UnityEngine;

/// <summary>
/// Controls the traffic light visuals (green/red) and issues a fine
/// when the player crosses the STOP LINE during RED.
///
/// IMPORTANT:
/// - The STOP LINE collider is NOT on the traffic light object.
/// - The stop line is a separate GameObject with an EdgeCollider2D (Is Trigger).
/// - This controller listens to a helper script (StopLineTrigger) on that stop line.
/// </summary>
public class TrafficLightController : MonoBehaviour
{
    [Header("Timing")]
    [SerializeField] private float greenDuration = 3f;
    [SerializeField] private float redDuration = 3f;

    [Header("Fines (local escalation)")]
    [SerializeField] private int baseRedFine = 3;
    [SerializeField] private float fineMultiplierPerViolation = 1.5f;
    [SerializeField] private int maxFineCap = 0;

    [Header("Crossing detection")]
    [SerializeField] private float minSpeedToCount = 0.2f;

    [Header("Sprites")]
    [SerializeField] private Sprite greenSprite;
    [SerializeField] private Sprite redSprite;

    [Header("Stop Line Reference")]
    [Tooltip("Drag the StopLine GameObject here (the one that has EdgeCollider2D + StopLineTrigger).")]
    [SerializeField] private StopLineTrigger stopLine;

    private bool isGreen = true;
    private int redViolationsLocal = 0;

    private SpriteRenderer sr;

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();

        // Auto-find in children/parent if not assigned (optional safety)
        if (stopLine == null)
            stopLine = GetComponentInChildren<StopLineTrigger>();

        if (stopLine == null)
            Debug.LogError("TrafficLightController: StopLineTrigger reference is missing! Drag it in Inspector.");
    }

    private void OnEnable()
    {
        // Subscribe to stop line trigger events
        if (stopLine != null)
            stopLine.PlayerCrossed += OnPlayerCrossedStopLine;
    }

    private void OnDisable()
    {
        // Unsubscribe to avoid memory leaks / double subscriptions
        if (stopLine != null)
            stopLine.PlayerCrossed -= OnPlayerCrossedStopLine;
    }

    private void Start()
    {
        UpdateVisual();
        StartCoroutine(SwitchRoutine());
    }

    private IEnumerator SwitchRoutine()
    {
        while (true)
        {
            isGreen = true;
            UpdateVisual();
            yield return new WaitForSeconds(greenDuration);

            isGreen = false;
            UpdateVisual();
            yield return new WaitForSeconds(redDuration);
        }
    }

    private void UpdateVisual()
    {
        if (sr == null) return;
        sr.sprite = isGreen ? greenSprite : redSprite;
    }

    /// <summary>
    /// Called when the player crosses the stop line trigger.
    /// We fine only if it's currently RED and player speed is above threshold.
    /// </summary>
    private void OnPlayerCrossedStopLine(Collider2D other)
    {
        if (other == null) return;

        // Only care about the player
        if (!other.CompareTag("Player"))
            return;

        // If green, no violation
        if (isGreen)
            return;

        Rigidbody2D rb = other.attachedRigidbody;
        if (rb == null) return;

        float speed = rb.linearVelocity.magnitude;
        if (speed < minSpeedToCount)
            return;

        if (PenaltyManager.Instance == null)
            return;

        redViolationsLocal++;

        float fineFloat =
            baseRedFine *
            Mathf.Pow(fineMultiplierPerViolation, redViolationsLocal - 1);

        int fine = Mathf.RoundToInt(fineFloat);

        if (maxFineCap > 0)
            fine = Mathf.Min(fine, maxFineCap);

        bool paid = PenaltyManager.Instance.IssueTicket(
            PenaltyManager.ViolationType.RedLight,
            fine,
            "RED LIGHT"
        );

        if (paid && AlertUI.Instance != null)
        {
            AlertUI.Instance.Show("RED LIGHT! - Money deducted");
        }
    }
}
