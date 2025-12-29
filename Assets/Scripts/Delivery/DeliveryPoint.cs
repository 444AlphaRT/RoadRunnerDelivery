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
    [SerializeField] private GameObject marker;           // Icon for THIS point (pickup icon or dropoff icon)
    [SerializeField] private GameObject dropoffMarker;    // Only used on the pickup point to show where to deliver

    [Header("Hint UI (optional)")]
    [SerializeField] private TextMeshProUGUI hintText;

    [Header("References (Dropoff only)")]
    [SerializeField] private Transform pickupPointForThisRoute; // Assign the PickupPoint here in the Dropoff inspector

    [Header("Reward Configuration - Base Pay")]
    [SerializeField] private float minDistanceForPay = 1f;            // Minimum distance considered for pay
    [SerializeField] private float basePayPerUnit = 1.5f;             // Coins per distance unit
    [SerializeField] private float farDistanceThreshold = 12f;        // Distance at which a delivery is considered "long"
    [SerializeField] private float farDistanceMultiplier = 1.3f;      // Multiplier for long deliveries

    [Header("Reward Configuration - Time & Speed")]
    [SerializeField] private float minTimeSafe = 0.1f;                // To avoid division by zero
    [SerializeField] private float expectedSpeedUnitsPerSecond = 4f;  // "Normal" driving speed for expected time

    [Header("Reward Configuration - Tip Thresholds (ratio = actualTime / expectedTime)")]
    [SerializeField] private float tipFastThresholdRatio = 0.8f;      // ratio <= this → top tip
    [SerializeField] private float tipNormalThresholdRatio = 1.1f;    // ratio <= this → normal tip
    [SerializeField] private float tipSlowThresholdRatio = 1.5f;      // ratio <= this → sometimes small tip

    [Header("Reward Configuration - Tip Rates")]
    [SerializeField] private float tipRateFast = 0.12f;               // 12% tip when very fast
    [SerializeField] private float tipRateNormal = 0.10f;             // 10% tip when normal speed
    [SerializeField] private float tipRateSlow = 0.05f;               // 5% tip when a bit slow (if customer is nice)

    [Header("Reward Configuration - Customer Mood")]
    [SerializeField] private float niceCustomerProbability = 0.5f;    // Chance for a slow delivery to still get a small tip

    [Header("Reward Configuration - Clamp Coins")]
    [SerializeField] private int minCoinsPerDelivery = 2;             // Never give less than this
    [SerializeField] private int maxCoinsPerDelivery = 15;            // Never give more than this

    private DeliveryTimer timer;
    private UpgradeUIController upgradeUI; // Reference to UI controller that manages the upgrade button

    private void Start()
    {
        // Find the global timer once in the scene
        timer = FindAnyObjectByType<DeliveryTimer>();

        // Find the upgrade UI controller once in the scene
        upgradeUI = FindAnyObjectByType<UpgradeUIController>();

        if (pointType == PointType.Pickup)
        {
            // Player has no package yet → show pickup icon
            if (marker != null)
                marker.SetActive(true);

            // Dropoff icon will be shown only after pickup
            if (dropoffMarker != null)
                dropoffMarker.SetActive(false);

            if (hintText != null)
                hintText.text = "Drive to the pickup icon to collect an order.";
        }
        else // Dropoff
        {
            // We always want the dropoff icon visible so the player knows where to deliver
            if (marker != null)
                marker.SetActive(true);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        PlayerController player = other.GetComponent<PlayerController>();
        if (player == null)
            return;

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
        // Already carrying a package → ignore
        if (player.HasPackage)
            return;

        // Player picks up the package
        player.PickUpPackage();

        // While an active delivery is running, hide the upgrade button (if unlocked)
        if (upgradeUI != null)
        {
            upgradeUI.OnPickupStarted(player.deliveriesCompleted);
        }

        // Start the delivery timer
        if (timer != null)
            timer.StartTimer();

        // Hide pickup marker
        if (marker != null)
            marker.SetActive(false);

        // Show dropoff icon (assigned from DropoffPoint)
        if (dropoffMarker != null)
            dropoffMarker.SetActive(true);

        if (hintText != null)
            hintText.text = "Follow the purple path and deliver the package.";
    }

    private void HandleDropoff(PlayerController player)
    {
        // Must have a package to deliver
        if (!player.HasPackage)
            return;

        // Deliver the package (updates player's delivery stats)
        player.DeliverPackage();

        // Notify the upgrade UI that a delivery was completed (button can appear between deliveries)
        if (upgradeUI != null)
        {
            upgradeUI.OnDeliveryCompleted(player.deliveriesCompleted);
        }

        float time = 0f;
        float distance = 0f;

        // Stop timer and read time
        if (timer != null)
        {
            timer.StopTimer();
            time = timer.LastDeliveryTime;
        }

        // Compute distance from pickup point to this dropoff
        if (pickupPointForThisRoute != null)
        {
            distance = Vector2.Distance(pickupPointForThisRoute.position, transform.position);
        }

        int coins = CalculateReward(distance, time);

        // Add money if MoneyManager exists
        if (MoneyManager.Instance != null)
        {
            MoneyManager.Instance.AddMoney(coins);
        }

        // Save stats (run + total) if PlayerStatsManager exists
        if (PlayerStatsManager.Instance != null)
        {
            PlayerStatsManager.Instance.RegisterDelivery(coins);
        }

        Debug.Log($"Delivery complete → distance={distance:F1}, time={time:F1}s, coins={coins}");

        // Move this DropoffPoint to a new random building
        RandomDropoffLocation randomDropoff = GetComponent<RandomDropoffLocation>();
        if (randomDropoff != null)
        {
            randomDropoff.MoveToRandomSpot();
        }

        if (hintText != null)
            hintText.text = "Great! Drive to the pickup icon for your next order.";
    }

    /// <summary>
    /// Calculates coin reward based on delivery distance and time.
    /// - Base pay scales with distance (with a bonus for long deliveries).
    /// - Tip depends on how fast the delivery was compared to an expected time.
    /// </summary>
    private int CalculateReward(float distance, float time)
    {
        // Sanity clamps
        if (distance < minDistanceForPay)
            distance = minDistanceForPay;

        if (time < minTimeSafe)
            time = minTimeSafe;

        // 1) Base pay from distance
        float basePay = distance * basePayPerUnit;

        bool isFarDelivery = distance >= farDistanceThreshold;
        if (isFarDelivery)
        {
            basePay *= farDistanceMultiplier;
        }

        // 2) Tip based on speed
        float expectedTime = distance / expectedSpeedUnitsPerSecond;
        float ratio = time / expectedTime; // < 1 = faster, > 1 = slower

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

        // 3) Clamp to a reasonable range
        int coins = Mathf.RoundToInt(totalPay);
        coins = Mathf.Clamp(coins, minCoinsPerDelivery, maxCoinsPerDelivery);

        return coins;
    }
}