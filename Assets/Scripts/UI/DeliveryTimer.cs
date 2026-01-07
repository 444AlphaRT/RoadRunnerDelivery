using UnityEngine;
using TMPro;

public class DeliveryTimer : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float timeLimit = 60f;
    [SerializeField] private float warningTime = 10f; // מתי להפוך לאדום

    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI timerText1;
    [SerializeField] private TextMeshProUGUI timerText2; // optional
    [SerializeField] private GameObject lateMessage; // גררי לפה את הודעת האיחור

    private bool slot1Running = false;
    private bool slot2Running = false;

    private float slot1Time;
    private float slot2Time;

    public float LastDeliveryTime { get; private set; }

    // המשתנה החדש שבודק אם היה איחור במשלוח האחרון
    public bool IsLate { get; private set; }

    private bool HasTwoSlots => timerText2 != null;

    private void Start()
    {
        ResetAll();
    }

    private void Update()
    {
      
        if (slot1Running)
        {
            slot1Time -= Time.deltaTime;

            // בדיקת סיום זמן
            if (slot1Time <= 0f)
            {
                slot1Time = 0f;
                ShowLateMessage();
            }

            UpdateText(timerText1, 1, slot1Time);
        }

        if (HasTwoSlots && slot2Running)
        {
            slot2Time -= Time.deltaTime;

            // בדיקת סיום זמן
            if (slot2Time <= 0f)
            {
                slot2Time = 0f;
                ShowLateMessage();
            }

            UpdateText(timerText2, 2, slot2Time);
        }
    }

    // Called when a pickup is collected
    public void StartNextTimer()
    {
        // כשמתחילים טיימר חדש, מסתירים את הודעת האיחור (אם אין עוד טיימרים שמאחרים)
        if (!IsAnyTimerLate())
        {
            if (lateMessage != null) lateMessage.SetActive(false);
        }

        if (!slot1Running)
        {
            slot1Running = true;
            slot1Time = timeLimit;
            UpdateText(timerText1, 1, slot1Time);
            return;
        }

        if (HasTwoSlots && !slot2Running)
        {
            slot2Running = true;
            slot2Time = timeLimit;
            UpdateText(timerText2, 2, slot2Time);
        }
    }

    // Called when a delivery is completed
    public void StopOldestRunningTimer()
    {
        if (slot1Running && !slot2Running)
        {
            StopSlot1();
            return;
        }

        if (!slot1Running && slot2Running)
        {
            StopSlot2();
            return;
        }

        if (slot1Running && slot2Running)
        {
            if (slot1Time <= slot2Time)
                StopSlot1();
            else
                StopSlot2();
        }
    }

    private void StopSlot1()
    {
        slot1Running = false;

        // קובעים אם היה איחור לפני האיפוס
        IsLate = (slot1Time <= 0);
        LastDeliveryTime = timeLimit - slot1Time;

        slot1Time = timeLimit;
        UpdateText(timerText1, 1, slot1Time);

        CheckIfShouldHideLateMessage();
    }

    private void StopSlot2()
    {
        slot2Running = false;

        // קובעים אם היה איחור לפני האיפוס
        IsLate = (slot2Time <= 0);
        LastDeliveryTime = timeLimit - slot2Time;

        slot2Time = timeLimit;
        UpdateText(timerText2, 2, slot2Time);

        CheckIfShouldHideLateMessage();
    }

    public void ResetAll()
    {
        slot1Running = false;
        slot2Running = false;

        slot1Time = timeLimit;
        slot2Time = timeLimit;
        LastDeliveryTime = 0f;
        IsLate = false;

        if (lateMessage != null) lateMessage.SetActive(false);

        UpdateText(timerText1, 1, slot1Time);
        if (HasTwoSlots)
            UpdateText(timerText2, 2, slot2Time);
    }

    private void UpdateText(TextMeshProUGUI text, int index, float time)
    {
        if (text == null) return;

        int seconds = Mathf.CeilToInt(time);

        // שינוי צבע לאדום אם הזמן נמוך
        if (time <= warningTime && time > 0)
        {
            text.color = Color.red;
        }
        else if (time <= 0)
        {
            text.color = Color.red; // נשאר אדום גם כשנגמר
        }
        else
        {
            text.color = Color.white; // צבע רגיל
        }

        text.text = HasTwoSlots
            ? $"Delivery {index}: {seconds}s"
            : $"Time: {seconds}s";
    }

    private void ShowLateMessage()
    {
        if (lateMessage != null && !lateMessage.activeSelf)
        {
            lateMessage.SetActive(true);
        }
    }

    private bool IsAnyTimerLate()
    {
        // בודק אם יש כרגע טיימר שרץ והוא על 0
        bool t1Late = slot1Running && slot1Time <= 0;
        bool t2Late = slot2Running && slot2Time <= 0;
        return t1Late || t2Late;
    }

    private void CheckIfShouldHideLateMessage()
    {
        // אם אף טיימר לא מאחר כרגע, אפשר להחביא את ההודעה
        if (!IsAnyTimerLate())
        {
            if (lateMessage != null) lateMessage.SetActive(false);
        }
    }

    // פונקציית עזר לשימוש ידני (אם צריך)
    public void StartTimer()
    {
        StartNextTimer();
    }

    public void StopTimer()
    {
        StopOldestRunningTimer();
    }
}