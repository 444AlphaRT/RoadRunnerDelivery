using UnityEngine;
using TMPro;
using System.Collections;
using UnityEngine.InputSystem;

public class FineNotification : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TextMeshProUGUI notificationText;

    [Header("Timed popup")]
    [SerializeField] private float displayDuration = 2f;

    [Header("Pause behavior")]
    [Tooltip("If true, pause the game until ENTER is pressed for 'blocking' messages.")]
    [SerializeField] private bool pauseGameOnBlockingMessages = true;

    [Header("Safety")]
    [Tooltip("If true, this UI will always be brought to front so it can't be hidden behind other panels.")]
    [SerializeField] private bool bringToFront = true;

    [Header("Background (no panel)")]
    [Tooltip("Background color behind the text using TMP <mark>. Alpha controls transparency.")]
    [SerializeField] private Color backgroundColor = new Color(0f, 0f, 0f, 0.65f);

    private bool isWaitingForInput = false;
    private System.Action onDismiss;

    private Canvas parentCanvas;

    private void Awake()
    {
        if (notificationText != null)
        {
            notificationText.gameObject.SetActive(false);
            parentCanvas = notificationText.GetComponentInParent<Canvas>();
        }
    }

    private void Update()
    {
        if (!isWaitingForInput) return;

        bool pressedEnter = false;

        // New Input System
        if (Keyboard.current != null)
        {
            if (Keyboard.current.enterKey.wasPressedThisFrame ||
                Keyboard.current.numpadEnterKey.wasPressedThisFrame)
            {
                pressedEnter = true;
            }
        }

        // Old Input System backup (works if Active Input Handling = Both)
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            pressedEnter = true;
        }

        if (pressedEnter)
            ResumeGameAndHide();
    }

    // =========================
    // Public API
    // =========================

    public void ShowFine(int amount)
    {
        ShowTimedMessage($"-{amount} Coins!", Color.yellow, displayDuration);
    }

    /// <summary>
    /// Speed camera popup. If blocking=true -> pauses game and requires ENTER.
    /// </summary>
    public void ShowSpeedCameraFine(int fineAmount, int overKmh, bool blocking = false)
    {
        string msg =
            $"SPEED CAMERA\n" +
            $"+{overKmh} km/h over limit\n" +
            $"Fine: {fineAmount} Coins" +
            (blocking ? "\nPress ENTER to continue" : "");

        if (blocking) ShowBlockingMessage(msg, Color.red);
        else ShowTimedMessage(msg, Color.red, displayDuration);
    }

    public void ShowRedLightPenalty(int amount, bool blocking = true)
    {
        string msg =
            "RED LIGHT!\n" +
            $"Fine: {amount} Coins" +
            (blocking ? "\nPress ENTER to continue" : "");

        if (blocking) ShowBlockingMessage(msg, Color.red);
        else ShowTimedMessage(msg, Color.red, displayDuration);
    }

    public void ShowLateDeliveryPenalty(int amount, float secondsLate = -1f, bool blocking = false)
    {
        string latePart = secondsLate >= 0f ? $"\nLate by: {secondsLate:F0}s" : "";

        string msg =
            "DELIVERY LATE!\n" +
            $"Fine: {amount} Coins" +
            latePart +
            (blocking ? "\nPress ENTER to continue" : "");

        if (blocking) ShowBlockingMessage(msg, Color.yellow);
        else ShowTimedMessage(msg, Color.yellow, displayDuration);
    }

    /// <summary>
    /// Generic timed message (NO pause).
    /// If the game is currently paused (timeScale==0), we automatically fallback to blocking message.
    /// </summary>
    public void ShowTimedMessage(string message, Color color, float seconds)
    {
        if (notificationText == null) return;

        // If game is paused, timed popups can be confusing; fallback to blocking.
        if (Time.timeScale == 0f)
        {
            ShowBlockingMessage(message + "\nPress ENTER to continue", color);
            return;
        }

        StopAllCoroutines();
        isWaitingForInput = false;
        onDismiss = null;

        ApplyText(message, color);
        StartCoroutine(HideAfterSeconds(seconds));
    }

    /// <summary>
    /// Blocking message (pause until Enter).
    /// </summary>
    public void ShowBlockingMessage(string message, Color color, System.Action onDismissAction = null)
    {
        if (notificationText == null) return;

        StopAllCoroutines();

        ApplyText(message, color);

        onDismiss = onDismissAction;

        if (pauseGameOnBlockingMessages)
            Time.timeScale = 0f;

        isWaitingForInput = true;
    }

    // =========================
    // Internal helpers
    // =========================

    /// <summary>
    /// Wraps the message with TMP <mark> to create a background highlight.
    /// Requires: NotificationText (TextMeshProUGUI) -> Rich Text enabled in Inspector.
    /// </summary>
    private string WithBackground(string message)
    {
        string hex = ColorUtility.ToHtmlStringRGBA(backgroundColor);
        return $"<mark=#{hex}>{message}</mark>";
    }

    private void ApplyText(string message, Color color)
    {
        // Add background behind text (no extra panel / image needed)
        notificationText.text = WithBackground(message);
        notificationText.color = color;

        // Ensure it is visible
        notificationText.gameObject.SetActive(true);

        // Bring to front so it can't be hidden behind other UI panels
        if (bringToFront)
            notificationText.transform.SetAsLastSibling();

        // Force UI refresh this frame
        Canvas.ForceUpdateCanvases();

        // Extra safety: if the parent canvas exists, enable it
        if (parentCanvas != null && !parentCanvas.enabled)
            parentCanvas.enabled = true;
    }

    private IEnumerator HideAfterSeconds(float seconds)
    {
        yield return new WaitForSecondsRealtime(seconds);

        if (notificationText != null)
            notificationText.gameObject.SetActive(false);
    }

    private void ResumeGameAndHide()
    {
        if (pauseGameOnBlockingMessages)
            Time.timeScale = 1f;

        isWaitingForInput = false;

        if (notificationText != null)
            notificationText.gameObject.SetActive(false);

        onDismiss?.Invoke();
        onDismiss = null;
    }
}
