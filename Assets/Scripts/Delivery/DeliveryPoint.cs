using UnityEngine;
using TMPro;

public class DeliveryPoint : MonoBehaviour
{
    public enum PointType
    {
        Pickup,
        Dropoff
    }

    [Header("Delivery Point Type")]
    [SerializeField] private PointType pointType;

    [Header("Visual Markers")]
    [SerializeField] private GameObject marker;
    [SerializeField] private GameObject dropoffMarker;

    [Header("Hint UI (optional)")]
    [SerializeField] private TextMeshProUGUI hintText;

    [Header("References (Dropoff only)")]
    [SerializeField] private Transform pickupPointForThisRoute;

    [Header("Reward Configuration - Base Pay")]
    [SerializeField] private float minDistanceForPay = 1f;
    [SerializeField] private float basePayPerUnit = 1.5f;
    [SerializeField] private float farDistanceThreshold = 12f;
    [SerializeField] private float farDistanceMultiplier = 1.3f;

    [Header("Reward Configuration - Time & Speed")]
    [SerializeField] private float minTimeSafe = 0.1f;
    [SerializeField] private float expectedSpeedUnitsPerSecond = 4f;

    [Header("Reward Configuration - Tip Thresholds")]
    [SerializeField] private float tipFastThresholdRatio = 0.8f;
    [SerializeField] private float tipNormalThresholdRatio = 1.1f;
    [SerializeField] private float tipSlowThresholdRatio = 1.5f;

    [Header("Reward Configuration - Tip Rates")]
    [SerializeField] private float tipRateFast = 0.12f;
    [SerializeField] private float tipRateNormal = 0.10f;
    [SerializeField] private float tipRateSlow = 0.05f;

    [Header("Reward Configuration - Customer Mood")]
    [SerializeField] private float niceCustomerProbability = 0.5f;

    [Header("Reward Configuration - Clamp Coins")]
    [SerializeField] private int minCoinsPerDelivery = 2;
    [SerializeField] private int maxCoinsPerDelivery = 15;

    // --- שינוי: שומרים מקום לשני סוגי הטיימרים ---
    private DeliveryTimer oldTimer;           // לשימוש ב-V2
    private DeliveryTimersLevel3 v3Timer;     // לשימוש ב-V3
    // ----------------------------------------------

    private UpgradeUIController upgradeUI;

    private void Start()
    {
        // מנסים למצוא את שני הטיימרים. 
        // ב-V2 הוא ימצא רק את oldTimer. ב-V3 ימצא רק את v3Timer.
        oldTimer = FindAnyObjectByType<DeliveryTimer>();
        v3Timer = FindAnyObjectByType<DeliveryTimersLevel3>();

        upgradeUI = FindAnyObjectByType<UpgradeUIController>();

        if (pointType == PointType.Pickup)
        {
            if (marker != null) marker.SetActive(true);
            if (dropoffMarker != null) dropoffMarker.SetActive(false);
            if (hintText != null) hintText.text = "Drive to the pickup icon to collect an order.";
        }
        else
        {
            if (marker != null) marker.SetActive(true);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        PlayerController player = other.GetComponent<PlayerController>();
        if (player == null) return;

        if (pointType == PointType.Pickup)
        {
            HandlePickup(player);
        }
        else if (pointType == PointType.Dropoff)
        {
            HandleDropoff(player);
        }
    }

    private void HandlePickup(PlayerController player)
    {
        if (player.HasPackage) return;

        player.PickUpPackage();

        if (upgradeUI != null)
        {
            upgradeUI.OnPickupStarted(player.deliveriesCompleted);
        }

        // אם אנחנו ב-V2, מפעילים את הטיימר הישן ידנית.
        // ב-V3 הטיימר מזהה לבד שיש חבילה ולכן לא חייבים לקרוא לו, אבל זה לא מזיק.
        if (oldTimer != null) oldTimer.StartTimer();

        if (marker != null) marker.SetActive(false);
        if (dropoffMarker != null) dropoffMarker.SetActive(true);

        if (hintText != null) hintText.text = "Follow the purple path and deliver the package.";
    }

    private void HandleDropoff(PlayerController player)
    {
        if (!player.HasPackage) return;

        player.DeliverPackage();

        if (upgradeUI != null)
        {
            upgradeUI.OnDeliveryCompleted(player.deliveriesCompleted);
        }

        float time = 0f;
        float distance = 0f;
        bool isLate = false; // ברירת מחדל: לא מאחרים

        // --- בדיקה איזה טיימר פעיל ---
        if (v3Timer != null)
        {
            // אנחנו ב-V3: לוקחים נתונים מהטיימר החדש
            time = v3Timer.LastDeliveryTime;
            isLate = v3Timer.IsLate; // האם הגיע ל-0?
        }
        else if (oldTimer != null)
        {
            // אנחנו ב-V2: עובדים רגיל עם הטיימר הישן
            oldTimer.StopTimer();
            time = oldTimer.LastDeliveryTime;
            isLate = false; // ב-V2 אין מושג של "איחור" בקוד הזה
        }
        // -----------------------------

        if (pickupPointForThisRoute != null)
        {
            distance = Vector2.Distance(pickupPointForThisRoute.position, transform.position);
        }

        // שולחים את נתון האיחור לחישוב
        int coins = CalculateReward(distance, time, isLate);

        if (MoneyManager.Instance != null)
        {
            MoneyManager.Instance.AddMoney(coins);
        }

        if (PlayerStatsManager.Instance != null)
        {
            PlayerStatsManager.Instance.RegisterDelivery(coins);
        }

        Debug.Log($"Delivery complete -> distance={distance:F1}, time={time:F1}s, coins={coins}, Late={isLate}");

        RandomDropoffLocation randomDropoff = GetComponent<RandomDropoffLocation>();
        if (randomDropoff != null)
        {
            randomDropoff.MoveToRandomSpot();
        }

        if (hintText != null) hintText.text = "Great! Drive to the pickup icon for your next order.";
    }

    // הוספתי פרמטר שלישי: isLate
    private int CalculateReward(float distance, float time, bool isLate)
    {
        // --- אם איחרנו, מחזירים מייד 1 ויוצאים ---
        if (isLate)
        {
            return 1;
        }
        // ----------------------------------------

        if (distance < minDistanceForPay) distance = minDistanceForPay;
        if (time < minTimeSafe) time = minTimeSafe;

        float basePay = distance * basePayPerUnit;

        bool isFarDelivery = distance >= farDistanceThreshold;
        if (isFarDelivery)
        {
            basePay *= farDistanceMultiplier;
        }

        float expectedTime = distance / expectedSpeedUnitsPerSecond;
        float ratio = time / expectedTime;

        float tipRate = 0f;

        if (ratio <= tipFastThresholdRatio)
        {
            tipRate = tipRateFast;
        }
        else if (ratio <= tipNormalThresholdRatio)
        {
            tipRate = tipRateNormal;
        }
        else if (ratio <= tipSlowThresholdRatio)
        {
            bool customerNice = Random.value < niceCustomerProbability;
            tipRate = customerNice ? tipRateSlow : 0f;
        }
        else
        {
            tipRate = 0f;
        }

        float totalPay = basePay * (1f + tipRate);
        int coins = Mathf.RoundToInt(totalPay);
        coins = Mathf.Clamp(coins, minCoinsPerDelivery, maxCoinsPerDelivery);

        return coins;
    }
}