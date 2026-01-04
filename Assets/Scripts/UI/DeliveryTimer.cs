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
        // התחלה: מציג 0.0
        if (timerText != null)
        {
            timerText.gameObject.SetActive(true);
            timerText.text = "Time: 0.0s";
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

    // --- כאן השינוי ---
    public void StopTimer()
    {
        if (!isRunning)
            return;

        isRunning = false;

        // 1. שמירת הזמן הסופי (קריטי לחישוב הניקוד ב-DeliveryPoint)
        LastDeliveryTime = currentTime;

        // 2. איפוס הטיימר והתצוגה לאפס מייד
        currentTime = 0f;
        if (timerText != null)
        {
            timerText.text = "Time: 0.0s";
        }
    }

    public void ResetTimerDisplay()
    {
        currentTime = 0f;
        isRunning = false;

        if (timerText != null)
        {
            timerText.gameObject.SetActive(true);
            timerText.text = "Time: 0.0s";
        }
    }
}