using UnityEngine;
using TMPro;

public class MoneyManager : MonoBehaviour
{
    public static MoneyManager Instance { get; private set; }

    [Header("UI")]
    [SerializeField] private TextMeshProUGUI moneyText;   // UI: "x 12"

    [Header("Money Settings")]
    [SerializeField] private int startingMoney = 0;

    [Header("Income Multiplier")]
    [SerializeField] private float baseIncomeMultiplier = 1f;   // 1.0 = normal
    [SerializeField] private float minIncomeMultiplier = 0.1f;  // safety lower bound

    public int CurrentMoney { get; private set; }
    public float IncomeMultiplier { get; private set; }

    private void Awake()
    {
        // Simple singleton pattern
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        CurrentMoney = startingMoney;
        IncomeMultiplier = baseIncomeMultiplier;
        UpdateMoneyText();
    }

    // --- Add money (affected by multiplier) ---
    public void AddMoney(int baseAmount)
    {
        // baseAmount is the reward calculated in DeliveryPoint
        float scaled = baseAmount * IncomeMultiplier;
        int finalAmount = Mathf.RoundToInt(scaled);

        CurrentMoney += finalAmount;
        if (CurrentMoney < 0)
            CurrentMoney = 0;

        UpdateMoneyText();
    }

    // --- Spend money safely (not affected by multiplier) ---
    public bool TrySpend(int amount)
    {
        if (CurrentMoney < amount)
        {
            return false;
        }

        CurrentMoney -= amount;
        UpdateMoneyText();
        return true;
    }

    // --- Upgrade: increase income multiplier ---
    public void IncreaseIncomeMultiplier(float delta)
    {
        IncomeMultiplier = Mathf.Max(minIncomeMultiplier, IncomeMultiplier + delta);
    }

    // --- Force-set money (debug only) ---
    public void SetMoney(int amount)
    {
        CurrentMoney = Mathf.Max(0, amount);
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