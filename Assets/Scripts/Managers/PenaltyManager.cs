using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PenaltyManager : MonoBehaviour
{
    public static PenaltyManager Instance { get; private set; }

    public enum ViolationType
    {
        Speed,
        RedLight,
        LateDelivery
    }

    [Header("References (auto-found each scene)")]
    [SerializeField] private PlayerController player;
    [SerializeField] private PenaltyOverlayUI penaltyUI;
    [SerializeField] private GameOverUI gameOverUI;

    [Header("GAME OVER (total violations)")]
    [SerializeField] private int maxViolationsBeforeGameOver = 3;

    [Header("No-money timeouts")]
    [SerializeField] private float firstTimeoutSeconds = 15f;
    [SerializeField] private float secondTimeoutSeconds = 30f;

    [Header("No-money strikes (per violation type)")]
    [SerializeField] private int maxUnpaidStrikes = 3;

    [Header("Freeze update")]
    [SerializeField] private float uiTickSeconds = 1f;

    private int violationsTotal = 0;

    private int speedUnpaidStrikes = 0;
    private int redUnpaidStrikes = 0;
    private int lateUnpaidStrikes = 0;

    private Coroutine freezeCoroutine;
    private bool isGameOver = false;

    [Header("Notifications")]
    [SerializeField] private FineNotification fineNotification;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void Start()
    {
        StartCoroutine(RebindNextFrame());
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        StartCoroutine(RebindNextFrame());
    }

    private IEnumerator RebindNextFrame()
    {
        // wait one frame so scene objects are fully created/enabled
        yield return null;

        RebindSceneReferences();

        // make sure UI is hidden at scene start
        penaltyUI?.Hide();
        gameOverUI?.Hide();
    }

    private void RebindSceneReferences()
    {
        // include inactive so UI can be found even if root panel is disabled
        player = FindFirstObjectByType<PlayerController>(FindObjectsInactive.Include);
        penaltyUI = FindFirstObjectByType<PenaltyOverlayUI>(FindObjectsInactive.Include);
        gameOverUI = FindFirstObjectByType<GameOverUI>(FindObjectsInactive.Include);
    }

    public bool IssueTicket(ViolationType type, int fineAmount, string reason)
    {
      
        if (isGameOver) return false;

        // Count total violations FIRST (paid or unpaid)
        violationsTotal++;
        if (violationsTotal >= maxViolationsBeforeGameOver)
        {
            TriggerGameOver("Too many violations.");
            return false;
        }

        // Try to rebind if needed
        if (player == null || penaltyUI == null || gameOverUI == null)
            RebindSceneReferences();

        if (MoneyManager.Instance == null)
        {
            Debug.LogWarning("PenaltyManager: MoneyManager.Instance is NULL (fine cannot be paid).");
            return false;
        }
        bool paid = MoneyManager.Instance.TrySpend(fineAmount);

        // Try to pay
        if (paid)
        {
            // --- התיקון 2: הפעלת ההודעה הנכונה ---
            if (fineNotification != null)
            {
                if (type == ViolationType.RedLight)
                {
                    Debug.Log("TRYING TO STOP GAME NOW!");
                    // רמזור אדום -> הודעה עוצרת משחק (באנגלית)
                    fineNotification.ShowRedLightPenalty(fineAmount);
                }
                else
                {
                    // עבירה אחרת (מהירות) -> הודעה רגילה
                    fineNotification.ShowFine(fineAmount);
                }
            }else
            {
                Debug.Log("ERROR: FineNotification is missing in Inspector!");
            }
            // -------------------------------------
            return true;
        }

        // Unpaid -> strike per type
        int strikes = IncrementUnpaid(type);
        if (strikes >= maxUnpaidStrikes)
        {
            TriggerGameOver("Too many unpaid fines.");
            return false;
        }

        // If player missing, we can't freeze, but we already counted the violation
        if (player == null)
        {
            Debug.LogWarning("PenaltyManager: Player missing, can't freeze.");
            return false;
        }

        float duration = (strikes == 1) ? firstTimeoutSeconds : secondTimeoutSeconds;

        // Restart freeze cleanly
        if (freezeCoroutine != null)
        {
            StopCoroutine(freezeCoroutine);
            freezeCoroutine = null;
        }

        freezeCoroutine = StartCoroutine(FreezeRoutine(duration, reason, strikes));
        return false;
    }

    private int IncrementUnpaid(ViolationType type)
    {
        switch (type)
        {
            case ViolationType.Speed: return ++speedUnpaidStrikes;
            case ViolationType.RedLight: return ++redUnpaidStrikes;
            case ViolationType.LateDelivery: return ++lateUnpaidStrikes;
            default: return 0;
        }
    }

    private IEnumerator FreezeRoutine(float seconds, string reason, int strikes)
    {
        bool prevCanMove = player.canMove;
        player.canMove = false;

        float remaining = seconds;

        // Use realtime so it works even if timeScale changes elsewhere
        while (remaining > 0f && !isGameOver)
        {
            penaltyUI?.ShowFreeze(reason, remaining, strikes, maxUnpaidStrikes);

            yield return new WaitForSecondsRealtime(uiTickSeconds);
            remaining -= uiTickSeconds;
        }

        penaltyUI?.Hide();

        if (!isGameOver)
            player.canMove = prevCanMove;

        freezeCoroutine = null;
    }

    private void TriggerGameOver(string debugReason)
    {
        if (isGameOver) return;
        isGameOver = true;

        if (freezeCoroutine != null)
        {
            StopCoroutine(freezeCoroutine);
            freezeCoroutine = null;
        }

        penaltyUI?.Hide();

        Debug.Log("GAME OVER: " + debugReason);

        // Show game over safely (sometimes UI isn't ready same frame)
        StartCoroutine(ShowGameOverSafe());
    }

    private IEnumerator ShowGameOverSafe()
    {
        // Try now
        RebindSceneReferences();

        // If still missing, try next frame (UI objects may enable after 1 frame)
        if (gameOverUI == null)
        {
            yield return null;
            RebindSceneReferences();
        }

        if (gameOverUI != null)
        {
            gameOverUI.Show();
            yield break;
        }

        // Fallback: freeze time so you know it triggered
        Debug.LogWarning("PenaltyManager: GameOverUI not found in this scene!");
        Time.timeScale = 0f;
    }

    public void ResetRunState()
    {
        violationsTotal = 0;
        speedUnpaidStrikes = 0;
        redUnpaidStrikes = 0;
        lateUnpaidStrikes = 0;

        isGameOver = false;

        if (freezeCoroutine != null)
        {
            StopCoroutine(freezeCoroutine);
            freezeCoroutine = null;
        }

        penaltyUI?.Hide();
        gameOverUI?.Hide();
    }
}
