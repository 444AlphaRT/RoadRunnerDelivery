using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UpgradeManager : MonoBehaviour
{
    private enum UpgradeType
    {
        Speed,
        TopSpeed,
        Income
    }

    [Header("References")]
    [SerializeField] private PlayerController player;
    [SerializeField] private MoneyManager moneyManager;
    [SerializeField] private GameObject upgradePanel; // The whole upgrade screen

    [Header("Speed Upgrade (Acceleration)")]
    [SerializeField] private int maxSpeedLevel = 5;
    [SerializeField] private float accelerationIncreasePerLevel = 1.0f;
    [SerializeField] private int speedFirstLevelCost = 5;
    [SerializeField] private int speedCostIncreasePerLevel = 3;
    [SerializeField] private Image speedFill;

    [Header("Top Speed Upgrade (Max Speed)")]
    [SerializeField] private int maxTopSpeedLevel = 5;
    [SerializeField] private float maxSpeedIncreasePerLevel = 0.4f;
    [SerializeField] private int topSpeedFirstLevelCost = 6;
    [SerializeField] private int topSpeedCostIncreasePerLevel = 4;
    [SerializeField] private Image topSpeedFill;

    [Header("Income Upgrade")]
    [SerializeField] private int maxIncomeLevel = 5;
    [SerializeField] private float incomeMultiplierIncreasePerLevel = 0.1f;
    [SerializeField] private int incomeFirstLevelCost = 8;
    [SerializeField] private int incomeCostIncreasePerLevel = 5;
    [SerializeField] private Image incomeFill;

    [Header("UI")]
    [SerializeField] private TextMeshProUGUI upgradeCostText; // number under the Upgrade button
    [SerializeField] private TextMeshProUGUI feedbackText;    // short messages

    private int currentSpeedLevel = 0;
    private int currentTopSpeedLevel = 0;
    private int currentIncomeLevel = 0;

    private float baseAcceleration;
    private float baseMaxSpeed;

    private UpgradeType currentSelection = UpgradeType.Speed;

    private void Start()
    {
        if (moneyManager == null)
            moneyManager = MoneyManager.Instance;

        if (player == null)
            player = FindAnyObjectByType<PlayerController>();

        if (player != null)
        {
            baseAcceleration = player.acceleration;
            baseMaxSpeed = player.maxSpeed;
        }

        SelectSpeed();

        if (upgradePanel != null)
            upgradePanel.SetActive(false);
    }

    public void OpenUpgradePanel()
    {
        if (upgradePanel != null)
            upgradePanel.SetActive(true);

        UpdateBars();
        UpdateCostText();
    }

    public void CloseUpgradePanel()
    {
        if (upgradePanel != null)
            upgradePanel.SetActive(false);
    }

    public void SelectSpeed()
    {
        currentSelection = UpgradeType.Speed;
        UpdateCostText();
        ShowFeedback("Acceleration selected");
    }

    public void SelectTopSpeed()
    {
        currentSelection = UpgradeType.TopSpeed;
        UpdateCostText();
        ShowFeedback("Top speed selected");
    }

    public void SelectIncome()
    {
        currentSelection = UpgradeType.Income;
        UpdateCostText();
        ShowFeedback("Income selected");
    }

    public void OnUpgradeButtonPressed()
    {
        if (player == null || moneyManager == null)
            return;

        int cost = GetSelectedCost();

        if (cost < 0)
        {
            ShowFeedback("Max level reached");
            return;
        }

        if (!moneyManager.TrySpend(cost))
        {
            ShowFeedback("Not enough coins");
            return;
        }

        switch (currentSelection)
        {
            case UpgradeType.Speed:
                currentSpeedLevel++;
                player.acceleration = baseAcceleration + currentSpeedLevel * accelerationIncreasePerLevel;
                ShowFeedback("Acceleration upgraded!");
                break;

            case UpgradeType.TopSpeed:
                currentTopSpeedLevel++;
                player.maxSpeed = baseMaxSpeed + currentTopSpeedLevel * maxSpeedIncreasePerLevel;
                ShowFeedback("Top speed upgraded!");
                break;

            case UpgradeType.Income:
                currentIncomeLevel++;
                moneyManager.IncreaseIncomeMultiplier(incomeMultiplierIncreasePerLevel);
                ShowFeedback("Income upgraded!");
                break;
        }

        UpdateBars();
        UpdateCostText();
    }

    private int GetSelectedCost()
    {
        switch (currentSelection)
        {
            case UpgradeType.Speed:
                if (currentSpeedLevel >= maxSpeedLevel) return -1;
                return speedFirstLevelCost + currentSpeedLevel * speedCostIncreasePerLevel;

            case UpgradeType.TopSpeed:
                if (currentTopSpeedLevel >= maxTopSpeedLevel) return -1;
                return topSpeedFirstLevelCost + currentTopSpeedLevel * topSpeedCostIncreasePerLevel;

            case UpgradeType.Income:
                if (currentIncomeLevel >= maxIncomeLevel) return -1;
                return incomeFirstLevelCost + currentIncomeLevel * incomeCostIncreasePerLevel;
        }

        return -1;
    }

    private void UpdateBars()
    {
        if (speedFill != null)
            speedFill.fillAmount = (float)currentSpeedLevel / maxSpeedLevel;

        if (topSpeedFill != null)
            topSpeedFill.fillAmount = (float)currentTopSpeedLevel / maxTopSpeedLevel;

        if (incomeFill != null)
            incomeFill.fillAmount = (float)currentIncomeLevel / maxIncomeLevel;
    }

    private void UpdateCostText()
    {
        if (upgradeCostText == null)
            return;

        int cost = GetSelectedCost();
        upgradeCostText.text = cost < 0 ? "-" : cost.ToString();
    }

    private void ShowFeedback(string msg)
    {
        if (feedbackText != null)
            feedbackText.text = msg;
    }
}
