using UnityEngine;
using TMPro;

public class DeliveryTimer : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float timeLimit = 60f;

    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI timerText1;
    [SerializeField] private TextMeshProUGUI timerText2; // optional

    private bool slot1Running = false;
    private bool slot2Running = false;

    private float slot1Time;
    private float slot2Time;

    public float LastDeliveryTime { get; private set; }

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
            if (slot1Time < 0f) slot1Time = 0f;
            UpdateText(timerText1, 1, slot1Time);
        }

        if (HasTwoSlots && slot2Running)
        {
            slot2Time -= Time.deltaTime;
            if (slot2Time < 0f) slot2Time = 0f;
            UpdateText(timerText2, 2, slot2Time);
        }
    }

    // Called when a pickup is collected
    public void StartNextTimer()
    {
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
        LastDeliveryTime = timeLimit - slot1Time;
        slot1Time = timeLimit;
        UpdateText(timerText1, 1, slot1Time);
    }

    private void StopSlot2()
    {
        slot2Running = false;
        LastDeliveryTime = timeLimit - slot2Time;
        slot2Time = timeLimit;
        UpdateText(timerText2, 2, slot2Time);
    }

    public void ResetAll()
    {
        slot1Running = false;
        slot2Running = false;

        slot1Time = timeLimit;
        slot2Time = timeLimit;
        LastDeliveryTime = 0f;

        UpdateText(timerText1, 1, slot1Time);
        if (HasTwoSlots)
            UpdateText(timerText2, 2, slot2Time);
    }

    private void UpdateText(TextMeshProUGUI text, int index, float time)
    {
        if (text == null) return;

        int seconds = Mathf.CeilToInt(time);
        text.text = HasTwoSlots
            ? $"Delivery {index}: {seconds}s"
            : $"Time: {seconds}s";
    }
}
