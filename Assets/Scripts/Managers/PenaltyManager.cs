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

    private int violationsTotal = 0;

    private int speedUnpaidStrikes = 0;
    private int redUnpaidStrikes = 0;
    private int lateUnpaidStrikes = 0;

    private Coroutine freezeCoroutine;
    private bool isGameOver = false;

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
        penaltyUI?.Hide();
    }

    private void RebindSceneReferences()
    {
        // include inactive so UI can be found even if root panel is disabled
        player = FindFirstObjectByType<PlayerController>(FindObjectsInactive.Include);
        penaltyUI = FindFirstObjectByType<PenaltyOverlayUI>(FindObjectsInactive.Include);
        gameOverUI = FindFirstObjectByType<GameOverUI>(FindObjectsInactive.Include);

        if (player == null)
            Debug.LogWarning("PenaltyManager: PlayerController not found in this scene.");
        if (penaltyUI == null)
            Debug.LogWarning("PenaltyManager: PenaltyOverlayUI not found in this scene.");
        if (gameOverUI == null)
            Debug.LogWarning("PenaltyManager: GameOverUI not found in this scene.");
    }

    public void IssueTicket(ViolationType type, int fineAmount, string reason)
    {
        if (isGameOver) return;

        // Always count the violation (paid or not)
        violationsTotal++;
        if (violationsTotal >= maxViolationsBeforeGameOver)
        {
            TriggerGameOver("Too many violations.");
            return;
        }

        // If missing refs in this scene, try to bind again right now
        if (player == null || penaltyUI == null || gameOverUI == null)
            RebindSceneReferences();

        if (MoneyManager.Instance == null)
        {
            Debug.LogWarning("PenaltyManager: MoneyManager.Instance is NULL (fine cannot be paid).");
            // still allow freeze / gameover logic if you want,
            // but right now we just stop here:
            return;
        }

        // Try to pay
        bool paid = MoneyManager.Instance.TrySpend(fineAmount);
        if (paid)
            return;

        // Unpaid -> strike per type
        int strikes = IncrementUnpaid(type);
        if (strikes >= maxUnpaidStrikes)
        {
            TriggerGameOver("Too many unpaid fines.");
            return;
        }

        // Freeze only if we actually have a player to freeze
        if (player == null)
        {
            Debug.LogWarning("PenaltyManager: Player missing, can't freeze. (But violation counted)");
            return;
        }

        float duration = (strikes == 1) ? firstTimeoutSeconds : secondTimeoutSeconds;

        if (freezeCoroutine != null)
            StopCoroutine(freezeCoroutine);

        freezeCoroutine = StartCoroutine(FreezeRoutine(duration, reason, strikes));
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

        float t = seconds;
        while (t > 0f && !isGameOver)
        {
            if (penaltyUI != null)
                penaltyUI.ShowFreeze(reason, t, strikes, maxUnpaidStrikes);

            yield return new WaitForSeconds(1f);
            t -= 1f;
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

        if (gameOverUI == null)
            gameOverUI = FindFirstObjectByType<GameOverUI>(FindObjectsInactive.Include);

        if (gameOverUI != null)
        {
            gameOverUI.Show();
        }
        else
        {
            Debug.LogWarning("PenaltyManager: GameOverUI not found, freezing timeScale.");
            Time.timeScale = 0f;
        }
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
    }
}