using UnityEngine;
using TMPro;

public class DeliveryTimersLevel3 : MonoBehaviour
{
    [Header("Settings")]
    public float timeLimit = 60f;

    [Header("UI References")]
    public TextMeshProUGUI timerText;
    public GameObject lateMessage;

    // משתנים לקריאה ע"י DeliveryPoint
    public bool IsLate { get; private set; }

    // זה המשתנה החדש שמאפשר ל-DeliveryPoint לקרוא את הזמן לפני האיפוס
    public float CurrentTime => currentTime;
    // משתנה לתאימות לאחור אם משהו אחר מחפש את זה
    public float LastDeliveryTime { get; private set; }

    private PlayerController player;
    private float currentTime;
    private bool isTimerRunning = false;

    private void Start()
    {
        player = FindFirstObjectByType<PlayerController>();
        ResetToStart();
    }

    private void Update()
    {
        if (player == null) return;

        bool playerHasPackage = player.HasPackage;

        if (playerHasPackage)
        {
            if (!isTimerRunning) StartTimer();

            currentTime -= Time.deltaTime;

            if (currentTime <= 0)
            {
                currentTime = 0;
                IsLate = true;

                if (lateMessage != null && !lateMessage.activeSelf)
                    lateMessage.SetActive(true);
            }
        }
        else
        {
            if (isTimerRunning) StopTimer();
        }

        UpdateTextDisplay();
    }

    void StartTimer()
    {
        isTimerRunning = true;
        currentTime = timeLimit;
        IsLate = false;
        if (lateMessage != null) lateMessage.SetActive(false);
    }

    void StopTimer()
    {
        isTimerRunning = false;
        LastDeliveryTime = timeLimit - currentTime;
        ResetToStart();
    }

    void ResetToStart()
    {
        isTimerRunning = false;
        currentTime = timeLimit;
        IsLate = false;
        if (lateMessage != null) lateMessage.SetActive(false);
        UpdateTextDisplay();
    }

    void UpdateTextDisplay()
    {
        if (timerText != null)
        {
            float timeToShow = Mathf.Ceil(currentTime);
            float minutes = Mathf.FloorToInt(timeToShow / 60);
            float seconds = Mathf.FloorToInt(timeToShow % 60);

            timerText.text = string.Format("Time: {0:00}:{1:00}", minutes, seconds);

            if (currentTime <= 10 && isTimerRunning) timerText.color = Color.red;
            else timerText.color = Color.white;
        }
    }
}