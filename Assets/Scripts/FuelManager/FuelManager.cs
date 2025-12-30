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
            // IMPORTANT FIX: only accept it if it actually has a TMP component
            var tmp = go.GetComponent<TextMeshProUGUI>();
            if (tmp != null)
            {
                fuelText = tmp;
                return;
            }
            // If the tagged object is NOT a TMP text (e.g. someone tagged FuelManager by mistake),
            // continue to fallback instead of returning with fuelText = null.
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

        // 3) If still not found, keep previous reference if it exists
        // (do not force null unless you really want that behavior)
        // fuelText = null;
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

    public void TryRefuel()
    {
        if (MoneyManager.Instance == null)
        {
            Debug.LogWarning("FuelManager: MoneyManager.Instance is NULL!");
            return;
        }

        if (CurrentFuel >= maxFuel)
            return;

        bool paid = MoneyManager.Instance.TrySpend(refuelCostCoins);
        if (!paid)
        {
            Debug.Log("FuelManager: Not enough money to refuel.");
            return;
        }

        AddFuel(refuelUnitsAmount);
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