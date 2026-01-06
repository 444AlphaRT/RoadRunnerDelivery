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
    [SerializeField] private GameObject marker;          // The marker shown on this point (pickup or dropoff UI icon)
    [SerializeField] private GameObject dropoffMarker;   // Optional: a different marker to show when dropoff becomes active

    [Header("Hint UI (optional)")]
    [SerializeField] private TextMeshProUGUI hintText;

    [Header("References (Dropoff only)")]
    [SerializeField] private Transform pickupPointForThisRoute; // Used for distance calculation (optional)

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

    // Existing systems in your project
    private DeliveryTimer deliveryTimer;         // Your only timer system
    private UpgradeUIController upgradeUI;
    private DualPickupManager dualPickupManager;

    private void Start()
    {
        // Find existing systems in the scene (only those you actually have)
        deliveryTimer = FindAnyObjectByType<DeliveryTimer>();
        upgradeUI = FindAnyObjectByType<UpgradeUIController>();
        dualPickupManager = FindAnyObjectByType<DualPickupManager>();

        // Initialize markers/hints
        if (pointType == PointType.Pickup)
        {
            ResetPickupVisuals();

            if (hintText != null)
                hintText.text = "Drive to a pickup icon to collect an order.";
        }
        else
        {
            if (marker != null) marker.SetActive(true);
        }

        if (deliveryTimer == null)
            Debug.LogWarning("DeliveryPoint: No DeliveryTimer found in the scene.");
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        PlayerController player = other.GetComponent<PlayerController>();
        if (player == null) return;

        if (pointType == PointType.Pickup)
            HandlePickup(player);
        else
            HandleDropoff(player);
    }

    // Useful for dual-pickup stages: the manager can call this after respawning pickups
    public void ResetPickupVisuals()
    {
        if (marker != null) marker.SetActive(true);
        if (dropoffMarker != null) dropoffMarker.SetActive(false);
    }

    private void HandlePickup(PlayerController player)
    {
        // Your player currently supports only one package at a time
        if (player.HasPackage) return;

        player.PickUpPackage();

        if (upgradeUI != null)
            upgradeUI.OnPickupStarted(player.deliveriesCompleted);

        // Start timing this delivery (one slot)
        if (deliveryTimer != null)
            deliveryTimer.StartNextTimer();

        // Disable THIS pickup marker, show dropoff marker
        if (marker != null) marker.SetActive(false);
        if (dropoffMarker != null) dropoffMarker.SetActive(true);

        if (hintText != null)
            hintText.text = "Follow the purple path and deliver the package.";

        // In stages without DualPickupManager, move this pickup away after pickup
        if (dualPickupManager == null)
        {
            RandomPickupLocation randomPickup = GetComponent<RandomPickupLocation>();
            if (randomPickup != null)
            {
                randomPickup.MoveToRandomSpot();
                Physics2D.SyncTransforms();
            }
        }
    }

    private void HandleDropoff(PlayerController player)
    {
        if (!player.HasPackage) return;

        player.DeliverPackage();

        if (upgradeUI != null)
            upgradeUI.OnDeliveryCompleted(player.deliveriesCompleted);

        // Stop timer and read elapsed time from your DeliveryTimer API
        float time = 0f;
        if (deliveryTimer != null)
        {
            deliveryTimer.StopOldestRunningTimer();
            time = deliveryTimer.LastDeliveryTime;
        }

        float distance = 0f;
        if (pickupPointForThisRoute != null)
            distance = Vector2.Distance(pickupPointForThisRoute.position, transform.position);

        // No "late" system here, so isLate is always false
        int coins = CalculateReward(distance, time, isLate: false);

        if (MoneyManager.Instance != null)
            MoneyManager.Instance.AddMoney(coins);

        if (PlayerStatsManager.Instance != null)
            PlayerStatsManager.Instance.RegisterDelivery(coins);

        Debug.Log($"Delivery complete -> distance={distance:F1}, time={time:F1}s, coins={coins}");

        // Move dropoff to a new random spot
        RandomDropoffLocation randomDropoff = GetComponent<RandomDropoffLocation>();
        if (randomDropoff != null)
        {
            randomDropoff.MoveToRandomSpot();
            Physics2D.SyncTransforms();
        }

        // If Stage 3 has two pickups, refresh both pickups for the next round
        if (dualPickupManager != null)
        {
            dualPickupManager.RespawnBothPickups();
            Physics2D.SyncTransforms();
        }

        // Reset local markers (ready for next cycle)
        if (marker != null) marker.SetActive(true);
        if (dropoffMarker != null) dropoffMarker.SetActive(false);

        if (hintText != null)
            hintText.text = "Great! Drive to a pickup icon for your next order.";
    }

    private int CalculateReward(float distance, float time, bool isLate)
    {
        if (isLate)
            return 1;

        if (distance < minDistanceForPay) distance = minDistanceForPay;
        if (time < minTimeSafe) time = minTimeSafe;

        float basePay = distance * basePayPerUnit;

        if (distance >= farDistanceThreshold)
            basePay *= farDistanceMultiplier;

        float expectedTime = distance / expectedSpeedUnitsPerSecond;
        float ratio = time / expectedTime;

        float tipRate = 0f;

        if (ratio <= tipFastThresholdRatio)
            tipRate = tipRateFast;
        else if (ratio <= tipNormalThresholdRatio)
            tipRate = tipRateNormal;
        else if (ratio <= tipSlowThresholdRatio)
        {
            bool customerNice = Random.value < niceCustomerProbability;
            tipRate = customerNice ? tipRateSlow : 0f;
        }
        else
            tipRate = 0f;

        float totalPay = basePay * (1f + tipRate);
        int coins = Mathf.RoundToInt(totalPay);

        coins = Mathf.Clamp(coins, minCoinsPerDelivery, maxCoinsPerDelivery);
        return coins;
    }
}
