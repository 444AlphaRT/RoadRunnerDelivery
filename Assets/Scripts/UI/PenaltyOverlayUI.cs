using UnityEngine;
using TMPro;

public class PenaltyOverlayUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject rootPanel;     // Panel that we show/hide
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI bodyText;

    private void Awake()
    {
        Hide();
    }

    public void ShowFreeze(string reason, float secondsLeft, int unpaidStrike, int maxStrikes)
    {
        if (rootPanel != null) rootPanel.SetActive(true);

        if (titleText != null)
            titleText.text = "PENALTY";

        if (bodyText != null)
        {
            bodyText.text =
                $"{reason}\n" +
                $"No money to pay the fine.\n" +
                $"Unpaid strikes: {unpaidStrike}/{maxStrikes}\n" +
                $"You are stopped for: {secondsLeft:F0}s";
        }
    }

    public void ShowTextOnly(string msg)
    {
        if (rootPanel != null) rootPanel.SetActive(true);
        if (titleText != null) titleText.text = "PENALTY";
        if (bodyText != null) bodyText.text = msg;
    }

    public void Hide()
    {
        if (rootPanel != null) rootPanel.SetActive(false);
    }
}