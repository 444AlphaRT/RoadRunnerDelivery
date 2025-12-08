using UnityEngine;
using TMPro;

public class DeliveryTimer : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI timerText;   // UI text to display the timer

    private bool isRunning = false;
    private float currentTime = 0f;

    public float LastDeliveryTime { get; private set; }   // Time of last completed delivery
    public bool IsRunning => isRunning;

    private void Start()
    {
        // Make sure the timer text starts hidden and empty
        if (timerText != null)
        {
            timerText.text = "";
            timerText.gameObject.SetActive(false);
        }
    }

    private void Update()
    {
        if (!isRunning)
            return;

        currentTime += Time.deltaTime;

        if (timerText != null)
        {
            timerText.text = $"Time: {currentTime:F1}s";
        }
    }

    public void StartTimer()
    {
        currentTime = 0f;
        isRunning = true;

        if (timerText != null)
        {
            timerText.gameObject.SetActive(true);
        }
    }

    public void StopTimer()
    {
        if (!isRunning)
            return;

        isRunning = false;

        LastDeliveryTime = currentTime;   // Save the final time

        if (timerText != null)
        {
            timerText.text = $"Time: {LastDeliveryTime:F1}s";
        }
    }

    public void ResetTimerDisplay()
    {
        currentTime = 0f;
        isRunning = false;

        if (timerText != null)
        {
            timerText.text = "";
            timerText.gameObject.SetActive(false);
        }
    }
}