using UnityEngine;
using TMPro;

/// <summary>
/// SpeedCamera reads the legal speed limit from the street's SpeedLimitSign.
/// When the player crosses the trigger:
/// - Shows a world-space message on the camera sign (green if legal, red if speeding)
/// - Issues a ticket ONLY if speeding
/// - If ticket is paid: shows a BLOCKING popup (like red light) with over-speed info
/// </summary>
[RequireComponent(typeof(BoxCollider2D))]
public class SpeedCamera : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Speed limit sign for this street (usually found on parent object).")]
    [SerializeField] private SpeedLimitSign streetSign;

    [Header("Camera Display (World Space)")]
    [Tooltip("Text shown on the camera's black panel.")]
    [SerializeField] private TextMeshProUGUI cameraText;

    [Tooltip("How long the camera message stays visible.")]
    [SerializeField] private float messageDuration = 1.5f;

    [Header("Calibration (must match SpeedometerUI)")]
    [Tooltip("Displayed KM/H when player reaches their CURRENT max speed (e.g. 50).")]
    [SerializeField] private float baselineKmhAtPlayerMaxSpeed = 50f;

    [Header("Ticket Formula")]
    [SerializeField] private int baseFine = 5;
    [SerializeField] private float finePerKmhOver = 1f;

    [Tooltip("Multiplier per repeated violation (local per camera).")]
    [SerializeField] private float violationMultiplier = 1.5f;

    [Tooltip("Optional fine cap. Set to 0 to disable.")]
    [SerializeField] private int maxFineCap = 0;

    [Header("Detection")]
    [Tooltip("KM/H tolerance to prevent tiny float jitter.")]
    [SerializeField] private float toleranceKmh = 0.5f;

    [Tooltip("Cooldown so the same camera won't ticket repeatedly.")]
    [SerializeField] private float cooldownSeconds = 1.0f;

    // Internal
    private float lastTicketTime = -999f;
    private int localViolations = 0;
    private float hideTextAt = -1f;

    private void Awake()
    {
        // Ensure trigger
        BoxCollider2D trigger = GetComponent<BoxCollider2D>();
        trigger.isTrigger = true;

        // Auto-find the sign on the same street (parent)
        if (streetSign == null)
            streetSign = GetComponentInParent<SpeedLimitSign>();

        if (streetSign == null)
            Debug.LogError("SpeedCamera: No SpeedLimitSign found in parent hierarchy!");

        // Optional: if text not assigned, try find in children
        if (cameraText == null)
            cameraText = GetComponentInChildren<TextMeshProUGUI>(true);

        // Hide text initially
        if (cameraText != null)
            cameraText.gameObject.SetActive(false);
    }

    private void Update()
    {
        // Auto-hide the camera text after a short time
        if (cameraText == null) return;

        if (hideTextAt > 0f && Time.time >= hideTextAt)
        {
            cameraText.gameObject.SetActive(false);
            hideTextAt = -1f;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        PlayerController player = other.GetComponent<PlayerController>();
        if (player == null) return;

        if (streetSign == null) return;

        // Cooldown check
        if (Time.time - lastTicketTime < cooldownSeconds)
            return;

        float limitKmh = streetSign.LimitKmh;

        // Convert real speed (units/sec) to displayed km/h (same scaling idea as SpeedometerUI)
        float unitsToKmh = baselineKmhAtPlayerMaxSpeed / Mathf.Max(player.maxSpeed, 0.01f);
        float speedKmh = player.CurrentSpeed * unitsToKmh;

        float delta = speedKmh - limitKmh;  // positive = over, negative = under
        int speedRounded = Mathf.RoundToInt(speedKmh);
        int limitRounded = Mathf.RoundToInt(limitKmh);
        int overKmhRounded = Mathf.RoundToInt(delta); // only meaningful if delta > 0

        // Show camera message (always)
        ShowCameraMessage(speedRounded, limitRounded, delta);

        // If legal -> no ticket
        if (delta <= toleranceKmh)
            return;

        // Speeding -> ticket
        if (PenaltyManager.Instance == null)
        {
            Debug.LogWarning("SpeedCamera: PenaltyManager.Instance is NULL.");
            return;
        }

        localViolations++;

        float fineValue = baseFine + (delta * finePerKmhOver);
        fineValue *= Mathf.Pow(violationMultiplier, localViolations - 1);

        int fine = Mathf.RoundToInt(fineValue);
        if (maxFineCap > 0) fine = Mathf.Min(fine, maxFineCap);
        fine = Mathf.Max(1, fine);

        // IMPORTANT: Use the dedicated speed camera API so it shows blocking popup + overKmh
        PenaltyManager.Instance.IssueSpeedCameraTicket(
            fine,
            Mathf.Max(1, overKmhRounded), // ensure at least 1
            $"SPEED CAMERA (+{Mathf.Max(1, overKmhRounded)})"
        );

        lastTicketTime = Time.time;
    }

    private void ShowCameraMessage(int speedKmh, int limitKmh, float delta)
    {
        if (cameraText == null) return;

        cameraText.gameObject.SetActive(true);

        // Green if legal, Red if speeding
        if (delta > toleranceKmh)
        {
            cameraText.color = Color.red;
            cameraText.text = $"{speedKmh}/{limitKmh}\n+{Mathf.RoundToInt(delta)}";
        }
        else
        {
            cameraText.color = Color.green;
            int under = Mathf.RoundToInt(Mathf.Abs(delta));
            cameraText.text = $"{speedKmh}/{limitKmh}\n-{under}";
        }

        hideTextAt = Time.time + messageDuration;
    }
}
