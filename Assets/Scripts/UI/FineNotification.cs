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
    private float previousTimeScale = 1f;

    private void Awake()
    {
        if (notificationText != null)
        {
            notificationText.text = "";
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
    /// If the game is currently paused (timeScale == 0),
    /// fallback to blocking message so it is visible and understandable.
    /// </summary>
    public void ShowTimedMessage(string message, Color color, float seconds)
    {
        if (notificationText == null) return;

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
    /// Blocking message (pause until ENTER).
    /// </summary>
    public void ShowBlockingMessage(string message, Color color, System.Action onDismissAction = null)
    {
        if (notificationText == null) return;

        StopAllCoroutines();

        ApplyText(message, color);

        onDismiss = onDismissAction;

        if (pauseGameOnBlockingMessages)
        {
            previousTimeScale = Time.timeScale;
            Time.timeScale = 0f;
        }

        isWaitingForInput = true;
    }

    // =========================
    // Internal helpers
    // =========================

    private string WithBackground(string message)
    {
        string hex = ColorUtility.ToHtmlStringRGBA(backgroundColor);
        return $"<mark=#{hex}>{message}</mark>";
    }

    private void ApplyText(string message, Color color)
    {
        notificationText.text = WithBackground(message);
        notificationText.color = color;

        notificationText.gameObject.SetActive(true);

        if (bringToFront)
            notificationText.transform.SetAsLastSibling();

        Canvas.ForceUpdateCanvases();

        if (parentCanvas != null && !parentCanvas.enabled)
            parentCanvas.enabled = true;
    }

    private IEnumerator HideAfterSeconds(float seconds)
    {
        yield return new WaitForSecondsRealtime(seconds);
        HideAndClear();
    }

    private void ResumeGameAndHide()
    {
        if (pauseGameOnBlockingMessages)
            Time.timeScale = previousTimeScale;

        isWaitingForInput = false;

        HideAndClear();

        onDismiss?.Invoke();
        onDismiss = null;
    }

    private void HideAndClear()
    {
        if (notificationText == null) return;

        notificationText.text = "";          // IMPORTANT: clear old <mark> + text
        notificationText.gameObject.SetActive(false);
    }
}
