using UnityEngine;
using TMPro;

public class DeliveryTimersLevel4 : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerController player;

    [Header("UI References (two separate TMP texts)")]
    [SerializeField] private TextMeshProUGUI timerText1;
    [SerializeField] private TextMeshProUGUI timerText2;

    [Header("Level 4 - Deadline & Late Penalty")]
    [Tooltip("After this many seconds, the delivery becomes late.")]
    [SerializeField] private float deadlineSeconds = 20f;

    [Tooltip("How much money is charged PER SECOND after deadline.")]
    [SerializeField] private int latePenaltyPerSecond = 1;

    // Timer state
    private bool timer1Running = false;
    private bool timer2Running = false;
    private float timer1 = 0f;
    private float timer2 = 0f;

    // Late-penalty tick accumulators (so we charge exactly once per second)
    private float lateTick1 = 0f;
    private float lateTick2 = 0f;

    // Used to detect changes
    private int lastPackagesHeld = 0;
    private int lastDeliveriesCompleted = 0;

    private void Awake()
    {
        if (player == null)
            player = FindFirstObjectByType<PlayerController>();
    }

    private void Start()
    {
        // Hide at start
        if (timerText1 != null) { timerText1.text = ""; timerText1.gameObject.SetActive(false); }
        if (timerText2 != null) { timerText2.text = ""; timerText2.gameObject.SetActive(false); }

        if (player == null)
        {
            Debug.LogError("DeliveryTimersLevel4: Player reference missing!");
            enabled = false;
            return;
        }

        lastPackagesHeld = player.packagesHeld;
        lastDeliveriesCompleted = player.deliveriesCompleted;
    }

    private void Update()
    {
        if (player == null) return;

        // IMPORTANT:
        // If player is frozen (PenaltyManager stopped movement), do NOT keep charging money.
        // This prevents "money slowly decreases while frozen".
        bool allowTimeAndPenalties = player.canMove && Time.timeScale > 0f;

        // 1) Update running timers + show UI
        if (timer1Running && allowTimeAndPenalties)
        {
            timer1 += Time.deltaTime;
            UpdateTimerText(timerText1, 1, timer1);
            HandleLatePenalty(1, timer1, ref lateTick1);
        }
        else if (timer1Running)
        {
            // still show last known time even if frozen
            UpdateTimerText(timerText1, 1, timer1);
        }

        if (timer2Running && allowTimeAndPenalties)
        {
            timer2 += Time.deltaTime;
            UpdateTimerText(timerText2, 2, timer2);
            HandleLatePenalty(2, timer2, ref lateTick2);
        }
        else if (timer2Running)
        {
            UpdateTimerText(timerText2, 2, timer2);
        }

        // 2) Detect PICKUP (packagesHeld increased) -> start next timer(s)
        if (player.packagesHeld > lastPackagesHeld)
        {
            int delta = player.packagesHeld - lastPackagesHeld;
            for (int i = 0; i < delta; i++)
                StartNextTimer();

            lastPackagesHeld = player.packagesHeld;
        }
        else if (player.packagesHeld < lastPackagesHeld)
        {
            lastPackagesHeld = player.packagesHeld;
        }

        // 3) Detect DELIVERY (deliveriesCompleted increased) -> stop oldest timer(s)
        if (player.deliveriesCompleted > lastDeliveriesCompleted)
        {
            int delta = player.deliveriesCompleted - lastDeliveriesCompleted;
            for (int i = 0; i < delta; i++)
                StopOldestRunningTimer();

            lastDeliveriesCompleted = player.deliveriesCompleted;
        }
    }

    private void UpdateTimerText(TextMeshProUGUI text, int index, float t)
    {
        if (text == null) return;

        // Show deadline info
        if (t <= deadlineSeconds)
        {
            float left = Mathf.Max(0f, deadlineSeconds - t);
            text.text = $"Delivery {index}: {t:F1}s  (Deadline in {left:F0}s)";
        }
        else
        {
            float lateBy = t - deadlineSeconds;
            text.text = $"Delivery {index}: {t:F1}s  (LATE by {lateBy:F0}s)";
        }
    }

    private void HandleLatePenalty(int index, float timerValue, ref float lateTick)
    {
        if (timerValue <= deadlineSeconds) return;

        // Accumulate 1-second ticks after deadline
        lateTick += Time.deltaTime;

        // Charge once per second
        if (lateTick < 1f) return;
        lateTick -= 1f;

        if (PenaltyManager.Instance == null)
        {
            Debug.LogWarning("DeliveryTimersLevel4: PenaltyManager.Instance is NULL!");
            return;
        }

        // We treat late delivery as its own violation type
        string reason = $"Late delivery (Delivery {index})";

        PenaltyManager.Instance.IssueTicket(
            PenaltyManager.ViolationType.LateDelivery,
            latePenaltyPerSecond,
            reason
        );
    }

    // Start timer for next picked package (max 2)
    private void StartNextTimer()
    {
        if (!timer1Running)
        {
            timer1Running = true;
            timer1 = 0f;
            lateTick1 = 0f;

            if (timerText1 != null) timerText1.gameObject.SetActive(true);
            return;
        }

        if (!timer2Running)
        {
            timer2Running = true;
            timer2 = 0f;
            lateTick2 = 0f;

            if (timerText2 != null) timerText2.gameObject.SetActive(true);
            return;
        }

        Debug.Log("DeliveryTimersLevel4: Both delivery timers are already running.");
    }

    // Stop the oldest running timer (the one that started first)
    private void StopOldestRunningTimer()
    {
        if (!timer1Running && !timer2Running) return;

        if (timer1Running && !timer2Running) { StopTimer1(); return; }
        if (!timer1Running && timer2Running) { StopTimer2(); return; }

        // If both are running, stop the one with higher elapsed time
        if (timer1 >= timer2) StopTimer1();
        else StopTimer2();
    }

    private void StopTimer1()
    {
        timer1Running = false;
        lateTick1 = 0f;

        if (timerText1 != null)
            timerText1.text = $"Delivery 1 complete: {timer1:F1}s";
    }

    private void StopTimer2()
    {
        timer2Running = false;
        lateTick2 = 0f;

        if (timerText2 != null)
            timerText2.text = $"Delivery 2 complete: {timer2:F1}s";
    }
}