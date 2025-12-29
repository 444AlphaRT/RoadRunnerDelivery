using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class FuelManager : MonoBehaviour
{
    public static FuelManager Instance { get; private set; }

    [Header("UI (auto found by tag each scene)")]
    [SerializeField] private TextMeshProUGUI fuelText;
    [SerializeField] private string fuelTextTag = "FuelText"; // This must be a real Unity Tag on the TMP object

    [Header("Fuel Settings")]
    [SerializeField] private int startingFuel = 5;
    [SerializeField] private int maxFuel = 10;

    [Header("Fuel Consumption")]
    [Tooltip("How many completed deliveries consume 1 fuel unit.")]
    [SerializeField] private int deliveriesPerFuelUnit = 3;

    [Header("Refuel Settings")]
    [SerializeField] private int refuelUnitsAmount = 5;
    [SerializeField] private int refuelCostCoins = 5;

    [Header("No Money Penalty")]
    [SerializeField] private float noMoneyFreezeSeconds = 5f;

    [Header("References")]
    [SerializeField] private PlayerController player;

    public int CurrentFuel { get; private set; }

    private int deliveriesSinceLastFuelDrop = 0;
    private bool initialized = false;

    private Coroutine freezeCoroutine = null;
    private bool isFrozen = false;

    private void Awake()
    {
        // Singleton + keep between scenes
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
        if (initialized) return;
        initialized = true;

        ResetToDefaults();        // start fuel for this run
        TryAutoAssignPlayer();
        RebindFuelUI();
        UpdateFuelText();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        TryAutoAssignPlayer();
        RebindFuelUI();

        // If we started a NEW run from stage select -> reset fuel ONCE
        if (RunContext.Instance != null && RunContext.Instance.ConsumeFuelResetFlag())
        {
            ResetToDefaults();
        }

        UpdateFuelText();
    }

    private void TryAutoAssignPlayer()
    {
        if (player == null)
            player = FindFirstObjectByType<PlayerController>();
    }

    private void RebindFuelUI()
    {
        // Find the scene's UI text by tag
        GameObject go = GameObject.FindGameObjectWithTag(fuelTextTag);
        if (go != null)
            fuelText = go.GetComponent<TextMeshProUGUI>();
    }

    // Call this once when a delivery is completed
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
        UpdateFuelText();
    }

    // Called by button OR by Player pressing E (your choice)
    public void TryRefuel()
    {
        if (isFrozen) return; // prevent spam while already frozen

        if (MoneyManager.Instance == null)
        {
            Debug.LogWarning("FuelManager: MoneyManager.Instance is NULL!");
            return;
        }

        if (CurrentFuel >= maxFuel)
            return;

        bool paid = MoneyManager.Instance.TrySpend(refuelCostCoins);
        if (paid)
        {
            AddFuel(refuelUnitsAmount);
            return;
        }

        // No money -> freeze player for a few seconds
        if (player != null)
        {
            if (freezeCoroutine != null) StopCoroutine(freezeCoroutine);
            freezeCoroutine = StartCoroutine(FreezePlayer(noMoneyFreezeSeconds));
        }
    }

    private IEnumerator FreezePlayer(float seconds)
    {
        isFrozen = true;

        bool prevCanMove = player.canMove;
        player.canMove = false;

        // Use realtime so it works even if Time.timeScale changes somewhere
        yield return new WaitForSecondsRealtime(seconds);

        player.canMove = prevCanMove;

        isFrozen = false;
        freezeCoroutine = null;
    }

    // Called when starting a NEW run (stage select / try again)
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