using System.Collections;
using UnityEngine;

public class PenaltyManager : MonoBehaviour
{
    public static PenaltyManager Instance;

    // Added LateDelivery so it won't mix with Speed/RedLight strikes
    public enum ViolationType
    {
        Speed,
        RedLight,
        LateDelivery
    }

    [Header("References")]
    [SerializeField] private PlayerController player;
    [SerializeField] private PenaltyOverlayUI penaltyUI;

    [Header("Global rule")]
    [Tooltip("If total tickets reach this number -> game over.")]
    [SerializeField] private int maxTicketsTotal = 5;

    [Header("No-money timeouts")]
    [Tooltip("Freeze duration on 1st unpaid strike (per type).")]
    [SerializeField] private float firstTimeoutSeconds = 15f;

    [Tooltip("Freeze duration on 2nd unpaid strike (per type).")]
    [SerializeField] private float secondTimeoutSeconds = 30f;

    [Header("No-money strikes (per violation type)")]
    [Tooltip("3rd unpaid strike (per type) -> game over.")]
    [SerializeField] private int maxUnpaidStrikes = 3;

    private int ticketsTotal = 0;

    // Separate unpaid counters per type
    private int speedUnpaidStrikes = 0;
    private int redUnpaidStrikes = 0;
    private int lateUnpaidStrikes = 0;

    private Coroutine stopCoroutine;
    private bool isStopping = false;

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        if (player == null) player = FindFirstObjectByType<PlayerController>();
        if (penaltyUI == null) penaltyUI = FindFirstObjectByType<PenaltyOverlayUI>();
        penaltyUI?.Hide();
    }

    /// <summary>
    /// Use this for all penalties (speed / red light / late delivery).
    /// It will:
    /// - count a ticket (global)
    /// - attempt to pay
    /// - if cannot pay: increment unpaid strikes for THIS type and freeze with UI timer
    /// </summary>
    public void IssueTicket(ViolationType type, int fineAmount, string reason)
    {
        // Prevent stacking freezes (avoids "stuck forever" issues)
        if (isStopping) return;

        // Count total tickets
        ticketsTotal++;

        // Global rule
        if (ticketsTotal >= maxTicketsTotal)
        {
            Debug.Log("GAME OVER: Too many tickets (global).");
            // GameOverManager.Instance?.TriggerGameOver("Too many tickets!");
            return;
        }

        if (MoneyManager.Instance == null)
        {
            Debug.LogWarning("PenaltyManager: MoneyManager.Instance is NULL!");
            return;
        }

        // Try pay
        bool paid = MoneyManager.Instance.TrySpend(fineAmount);
        if (paid)
        {
            // Paid -> no freeze
            return;
        }

        // No money -> strike + freeze
        int strikes = IncrementUnpaid(type);

        // 3rd unpaid strike for THIS type -> game over
        if (strikes >= maxUnpaidStrikes)
        {
            Debug.Log($"GAME OVER: {maxUnpaidStrikes} unpaid fines for {type}.");
            // GameOverManager.Instance?.TriggerGameOver($"Too many unpaid fines: {type}");
            return;
        }

        // Freeze duration depends on strike number (1 => first, 2 => second)
        float duration = (strikes == 1) ? firstTimeoutSeconds : secondTimeoutSeconds;

        // Make sure only ONE freeze runs
        if (stopCoroutine != null) StopCoroutine(stopCoroutine);
        stopCoroutine = StartCoroutine(FreezeRoutine(duration, reason, strikes));
    }

    private int IncrementUnpaid(ViolationType type)
    {
        switch (type)
        {
            case ViolationType.Speed:
                return ++speedUnpaidStrikes;

            case ViolationType.RedLight:
                return ++redUnpaidStrikes;

            case ViolationType.LateDelivery:
                return ++lateUnpaidStrikes;

            default:
                return 0;
        }
    }

    private IEnumerator FreezeRoutine(float seconds, string reason, int strikes)
    {
        if (player == null)
        {
            Debug.LogWarning("PenaltyManager: Player reference missing!");
            yield break;
        }

        isStopping = true;

        bool prevCanMove = player.canMove;
        player.canMove = false;

        float t = seconds;
        while (t > 0f)
        {
            penaltyUI?.ShowFreeze(reason, t, strikes, maxUnpaidStrikes);

            yield return new WaitForSeconds(1f);
            t -= 1f;
        }

        penaltyUI?.Hide();

        // Restore movement safely
        player.canMove = prevCanMove;

        isStopping = false;
        stopCoroutine = null;
    }
}