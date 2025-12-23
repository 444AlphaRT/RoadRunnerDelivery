using UnityEngine;
using TMPro;

public class DeliveryTimersLevel3 : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerController player;

    [Header("UI References (two separate TMP texts)")]
    [SerializeField] private TextMeshProUGUI timerText1;
    [SerializeField] private TextMeshProUGUI timerText2;

    [Header("Level 4 - Deadline & Penalty")]
    [SerializeField] private float deadlineSeconds = 20f;
    [SerializeField] private int latePenaltyPerSecond = 1;

    private bool timer1Running = false;
    private bool timer2Running = false;

    private float timer1 = 0f;
    private float timer2 = 0f;

    private float penaltyClock1 = 0f;
    private float penaltyClock2 = 0f;

    private int lastPackagesHeld = 0;
    private int lastDeliveriesCompleted = 0;

    private void Awake()
    {
        if (player == null)
            player = FindObjectOfType<PlayerController>();
    }

    private void Start()
    {
        if (timerText1 != null) { timerText1.text = ""; timerText1.gameObject.SetActive(false); }
        if (timerText2 != null) { timerText2.text = ""; timerText2.gameObject.SetActive(false); }

        if (player != null)
        {
            lastPackagesHeld = player.packagesHeld;
            lastDeliveriesCompleted = player.deliveriesCompleted;
        }
        else
        {
            Debug.LogError("DeliveryTimersLevel3: Player missing");
            enabled = false;
        }
    }

    private void Update()
    {
        UpdateTimer(ref timer1, ref penaltyClock1, timer1Running, timerText1, 1);
        UpdateTimer(ref timer2, ref penaltyClock2, timer2Running, timerText2, 2);

        if (player == null) return;

        // Detect PICKUP
        if (player.packagesHeld > lastPackagesHeld)
        {
            int delta = player.packagesHeld - lastPackagesHeld;
            for (int i = 0; i < delta; i++)
                StartNextTimer();

            lastPackagesHeld = player.packagesHeld;
        }

        // Detect DELIVERY
        if (player.deliveriesCompleted > lastDeliveriesCompleted)
        {
            int delta = player.deliveriesCompleted - lastDeliveriesCompleted;
            for (int i = 0; i < delta; i++)
                StopOldestRunningTimer();

            lastDeliveriesCompleted = player.deliveriesCompleted;
        }
    }

    private void UpdateTimer(ref float timer, ref float penaltyClock, bool running, TextMeshProUGUI text, int index)
    {
        if (!running || text == null) return;

        timer += Time.deltaTime;

        bool late = timer > deadlineSeconds;

        text.text = late
            ? $"Time {index}: {timer:F1}s  (LATE)"
            : $"Time {index}: {timer:F1}s";

        if (!late || MoneyManager.Instance == null)
            return;

        penaltyClock += Time.deltaTime;
        if (penaltyClock >= 1f)
        {
            penaltyClock = 0f;
            MoneyManager.Instance.TrySpend(latePenaltyPerSecond);
        }
    }

    private void StartNextTimer()
    {
        if (!timer1Running)
        {
            timer1 = 0f;
            penaltyClock1 = 0f;
            timer1Running = true;
            timerText1.gameObject.SetActive(true);
            return;
        }

        if (!timer2Running)
        {
            timer2 = 0f;
            penaltyClock2 = 0f;
            timer2Running = true;
            timerText2.gameObject.SetActive(true);
        }
    }

    private void StopOldestRunningTimer()
    {
        if (!timer1Running && !timer2Running) return;

        if (timer1Running && !timer2Running) { timer1Running = false; return; }
        if (!timer1Running && timer2Running) { timer2Running = false; return; }

        if (timer1 >= timer2) timer1Running = false;
        else timer2Running = false;
    }
}