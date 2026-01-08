using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class FuelManager : MonoBehaviour
{
    public static FuelManager Instance { get; private set; }

    [Header("UI")]
    [SerializeField] private TextMeshProUGUI fuelText;
    [SerializeField] private string fuelTextTag = "FuelText";

    [Header("Fuel Settings")]
    [SerializeField] private int startingFuel = 5;
    [SerializeField] private int maxFuel = 10;

    [Header("Fuel Consumption")]
    [Tooltip("Every N completed deliveries consume 1 fuel unit.")]
    [SerializeField] private int deliveriesPerFuelUnit = 3;

    [Header("Refuel Settings")]
    [SerializeField] private int refuelUnitsAmount = 5;
    [SerializeField] private int refuelCostCoins = 5;

    [Header("Reset Policy")]
    [Tooltip("If true, entering a scene with a FuelManager will reset fuel to startingFuel.")]
    [SerializeField] private bool resetOnSceneEnter = true;

    [Header("WebGL/Build Robustness")]
    [Tooltip("How many frames to retry finding the FuelText after a scene loads.")]
    [SerializeField] private int rebindRetryFrames = 10;

    [Header("Refuel Feedback (UI)")]
    [Tooltip("Optional: show a notification when refueling succeeds/fails.")]
    [SerializeField] private FineNotification fineNotification;

    [Tooltip("Message display duration for refuel feedback.")]
    [SerializeField] private float refuelMessageSeconds = 1.5f;

    public int CurrentFuel { get; private set; }

    private int deliveriesSinceLastFuelDrop = 0;
    private Coroutine rebindCoroutine;

    private void Awake()
    {
        // If a duplicate FuelManager exists (because you placed one in every scene),
        // use THIS one as a config provider for the persistent instance, then destroy it.
        if (Instance != null && Instance != this)
        {
            Instance.ApplyInspectorConfigFrom(this);

            // IMPORTANT: reset decision should come from THIS scene's FuelManager
            if (this.resetOnSceneEnter)
                Instance.ResetToDefaults();

            // Rebind UI for the new scene (WebGL safe)
            Instance.StartRebindRetries();

            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void Start()
    {
        ResetToDefaults();
        StartRebindRetries();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        StartRebindRetries();

        if (RunContext.Instance != null && RunContext.Instance.ConsumeFuelResetFlag())
        {
            ResetToDefaults();
        }
        else if (resetOnSceneEnter)
        {
            ResetToDefaults();
        }
        else
        {
            UpdateFuelText();
        }
    }

    private void ApplyInspectorConfigFrom(FuelManager src)
    {
        fuelTextTag = src.fuelTextTag;

        startingFuel = src.startingFuel;
        maxFuel = src.maxFuel;

        deliveriesPerFuelUnit = src.deliveriesPerFuelUnit;

        refuelUnitsAmount = src.refuelUnitsAmount;
        refuelCostCoins = src.refuelCostCoins;

        resetOnSceneEnter = src.resetOnSceneEnter;
        rebindRetryFrames = src.rebindRetryFrames;

        // Copy feedback settings too
        fineNotification = src.fineNotification;
        refuelMessageSeconds = src.refuelMessageSeconds;
    }

    private void StartRebindRetries()
    {
        if (rebindCoroutine != null)
        {
            StopCoroutine(rebindCoroutine);
            rebindCoroutine = null;
        }

        rebindCoroutine = StartCoroutine(RebindWithRetries());
    }

    private IEnumerator RebindWithRetries()
    {
        for (int i = 0; i < Mathf.Max(1, rebindRetryFrames); i++)
        {
            RebindFuelUI();
            RebindRefuelNotification();

            if (fuelText != null)
            {
                UpdateFuelText();
                rebindCoroutine = null;
                yield break;
            }

            yield return null;
        }

        UpdateFuelText();
        Debug.LogWarning("FuelManager: FuelText not found after retries. Check FuelText tag and that the TMP object exists in the scene.");
        rebindCoroutine = null;
    }

    private void RebindFuelUI()
    {
        // 1) Try tag (active only)
        GameObject go = null;

        try
        {
            go = GameObject.FindGameObjectWithTag(fuelTextTag);
        }
        catch
        {
            Debug.LogWarning($"FuelManager: Tag '{fuelTextTag}' does not exist. Add it in Tags & Layers.");
        }

        if (go != null)
        {
            // Only accept it if it actually has a TMP component
            var tmp = go.GetComponent<TextMeshProUGUI>();
            if (tmp != null)
            {
                fuelText = tmp;
                return;
            }
        }

        // 2) Fallback: find TMP texts including inactive, prefer matching tag
        var allTmp = FindObjectsOfType<TextMeshProUGUI>(true);
        foreach (var t in allTmp)
        {
            if (t != null && t.gameObject.CompareTag(fuelTextTag))
            {
                fuelText = t;
                return;
            }
        }
    }

    /// <summary>
    /// Finds FineNotification in the currently loaded scene (because FuelManager persists).
    /// Safe even if you don't use it.
    /// </summary>
    private void RebindRefuelNotification()
    {
        if (fineNotification != null) return;
        fineNotification = FindFirstObjectByType<FineNotification>(FindObjectsInactive.Include);
    }

    public void RegisterDeliveryCompleted()
    {
        deliveriesSinceLastFuelDrop++;

        if (deliveriesPerFuelUnit <= 0)
            deliveriesPerFuelUnit = 1;

        if (deliveriesSinceLastFuelDrop >= deliveriesPerFuelUnit)
        {
            deliveriesSinceLastFuelDrop = 0;
            UseFuel(1);
        }
        else
        {
            UpdateFuelText();
        }
    }

    public void UseFuel(int amount)
    {
        amount = Mathf.Max(0, amount);
        CurrentFuel = Mathf.Max(0, CurrentFuel - amount);
        UpdateFuelText();
    }

    public void AddFuel(int amount)
    {
        amount = Mathf.Max(0, amount);
        CurrentFuel = Mathf.Clamp(CurrentFuel + amount, 0, maxFuel);
        UpdateFuelText();
    }

    public void SetFuel(int amount)
    {
        CurrentFuel = Mathf.Clamp(amount, 0, maxFuel);
        deliveriesSinceLastFuelDrop = 0;
        UpdateFuelText();
    }

    /// <summary>
    /// Refuel attempt:
    /// - If not enough money -> show feedback
    /// - If tank is full -> show feedback
    /// - On success -> add fuel + show "Refueled!" message
    /// </summary>
    public void TryRefuel()
    {
        RebindRefuelNotification();

        if (MoneyManager.Instance == null)
        {
            Debug.LogWarning("FuelManager: MoneyManager.Instance is NULL!");
            ShowRefuelFeedback("Refuel failed (no money system)", Color.red);
            return;
        }

        if (CurrentFuel >= maxFuel)
        {
            ShowRefuelFeedback("Tank is already full", Color.yellow);
            return;
        }

        bool paid = MoneyManager.Instance.TrySpend(refuelCostCoins);
        if (!paid)
        {
            ShowRefuelFeedback("Not enough coins to refuel", Color.red);
            return;
        }

        int before = CurrentFuel;
        AddFuel(refuelUnitsAmount);
        int gained = Mathf.Max(0, CurrentFuel - before);

        ShowRefuelFeedback($"+{gained} Fuel (Paid {refuelCostCoins})", Color.green);
    }

    private void ShowRefuelFeedback(string msg, Color color)
    {
        // If you have FineNotification in the scene, use it.
        // Otherwise just log (so it never breaks gameplay).
        if (fineNotification != null)
        {
            // Use the generic timed message API (no pause)
            fineNotification.ShowTimedMessage(msg, color, refuelMessageSeconds);
        }
        else
        {
            Debug.Log($"Refuel: {msg}");
        }
    }

    public void ResetToDefaults()
    {
        CurrentFuel = Mathf.Clamp(startingFuel, 0, maxFuel);
        deliveriesSinceLastFuelDrop = 0;
        UpdateFuelText();
    }

    private void UpdateFuelText()
    {
        if (fuelText != null)
            fuelText.text = "x " + CurrentFuel;
    }
}
