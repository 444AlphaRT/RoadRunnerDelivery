using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class MoneyManager : MonoBehaviour
{
    public static MoneyManager Instance { get; private set; }

    [Header("UI")]
    [SerializeField] private TextMeshProUGUI moneyText;   // will be re-found each scene by tag

    [Header("Money Settings")]
    [SerializeField] private int startingMoney = 0;

    [Header("Income Multiplier")]
    [SerializeField] private float baseIncomeMultiplier = 1f;
    [SerializeField] private float minIncomeMultiplier = 0.1f;

    public int CurrentMoney { get; private set; }
    public float IncomeMultiplier { get; private set; }

    private bool initialized = false;

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

        // Reconnect UI every time a new scene loads
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void Start()
    {
        // Initialize only once in the whole run
        if (initialized) return;
        initialized = true;

        CurrentMoney = startingMoney;
        IncomeMultiplier = baseIncomeMultiplier;
        UpdateMoneyText();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Find the new scene's money text by tag
        GameObject go = GameObject.FindGameObjectWithTag("MoneyText");
        if (go != null)
        {
            moneyText = go.GetComponent<TextMeshProUGUI>();
        }

        // If started from menu -> reset money (RunContext flag)
        if (RunContext.Instance != null && RunContext.Instance.ConsumeMoneyResetFlag())
        {
            CurrentMoney = 0;
            IncomeMultiplier = baseIncomeMultiplier;
        }

        UpdateMoneyText();
    }

    // --- Add money (affected by multiplier) ---
    public void AddMoney(int baseAmount)
    {
        Debug.Log($"[Money BEFORE] = {CurrentMoney}");
        float scaled = baseAmount * IncomeMultiplier;
        int finalAmount = Mathf.RoundToInt(scaled);

        CurrentMoney += finalAmount;
        if (CurrentMoney < 0) CurrentMoney = 0;
        Debug.Log($"[Money AFTER] = {CurrentMoney}");

        UpdateMoneyText();
    }

    // --- Spend money safely (not affected by multiplier) ---
    public bool TrySpend(int amount)
    {
        Debug.Log($"[Money BEFORE] = {CurrentMoney}");

        if (CurrentMoney < amount)
            return false;

        CurrentMoney -= amount;
        UpdateMoneyText();
        Debug.Log($"[Money AFTER] = {CurrentMoney}");

        return true;
    }

    // --- Upgrade: increase income multiplier ---
    public void IncreaseIncomeMultiplier(float delta)
    {
        IncomeMultiplier = Mathf.Max(minIncomeMultiplier, IncomeMultiplier + delta);
    }

    public void SetMoney(int amount)
    {
        CurrentMoney = Mathf.Max(0, amount);
        UpdateMoneyText();
    }

    public void ResetToDefaults()
    {
        CurrentMoney = startingMoney;
        IncomeMultiplier = baseIncomeMultiplier;
        UpdateMoneyText();
    }

    private void UpdateMoneyText()
    {
        if (moneyText != null)
        {
            moneyText.text = "x " + CurrentMoney;
        }
    }
}