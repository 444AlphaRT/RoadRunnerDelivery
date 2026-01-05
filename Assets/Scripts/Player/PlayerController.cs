using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    // =========================
    // Movement
    // =========================
    [Header("Movement Settings")]
    public float moveSpeed = 5f;   // Base acceleration speed
    public float maxSpeed = 8f;    // Absolute maximum speed

    [Header("Rotation (Motorcycle)")]
    [Tooltip("If your sprite faces UP by default, keep this at -90. If it faces RIGHT, set it to 0.")]
    public float spriteAngleOffset = -90f;

    [Tooltip("Minimum speed required before we rotate the bike (prevents jitter when nearly stopped).")]
    public float rotateMinSpeed = 0.05f;

    // =========================
    // Gameplay State
    // =========================
    [Header("Gameplay State")]
    public bool canMove = false;   // If false, player movement is completely disabled

    // =========================
    // Delivery State
    // =========================
    [Header("Delivery State")]
    public bool HasPackage = false;      // Kept for compatibility with existing scripts (Minimap, Dropoff checks, etc.)
    public int deliveriesCompleted = 0;  // Total number of successful deliveries

    [Header("Package Carrying")]
    public int maxPackages = 2;          // Maximum number of packages the player can carry at once (Level 3 feature)
    public int packagesHeld = 0;         // Current number of packages held (0..maxPackages)

    // =========================
    // Fuel System
    // =========================
    [Header("Fuel Settings")]
    public int maxDeliveriesPerTank = 2;  // How many deliveries can be done per fuel tank
    public bool outOfFuel = false;        // True when fuel is empty
    private int deliveriesOnCurrentTank = 0;

    // =========================
    // Refuel Costs
    // =========================
    [Header("Refuel Settings")]
    public int refuelCostEmpty = 10;      // Cost when fuel is completely empty
    public int refuelCostOneLeft = 5;     // Cost when one delivery remains

    // =========================
    // Speed Zone System (supports overlapping zones) - if still used elsewhere
    // =========================
    private readonly Dictionary<object, float> activeSpeedLimits = new();
    private float? cachedSpeedLimit = null; // Cached minimum speed limit from all active zones

    // =========================
    // Internal State
    // =========================
    private Vector2 inputDirection;
    private Rigidbody2D rb;

    // =========================
    // Unity Lifecycle
    // =========================
    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();

        if (rb == null)
        {
            Debug.LogError("PlayerController: Rigidbody2D is missing!");
        }

        // Sync the legacy bool with the new package counter (important on scene start)
        HasPackage = packagesHeld > 0;
    }

    private void Update()
    {
        // Read raw input (no smoothing)
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");

        inputDirection = new Vector2(horizontal, vertical).normalized;

        // Manual refuel key
        if (Input.GetKeyDown(KeyCode.E))
        {
            FuelManager.Instance?.TryRefuel();
        }
    }

    private void FixedUpdate()
    {
        if (rb == null) return;

        // Stop movement if not allowed or out of fuel
        if (!canMove || outOfFuel)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        // Determine current max speed (speed zones override global maxSpeed)
        float currentMaxSpeed = cachedSpeedLimit.HasValue
            ? Mathf.Min(maxSpeed, cachedSpeedLimit.Value)
            : maxSpeed;

        // Apply movement (top-down style: velocity directly from input)
        Vector2 desiredVelocity = inputDirection * moveSpeed;
        Vector2 clampedVelocity = Vector2.ClampMagnitude(desiredVelocity, currentMaxSpeed);
        rb.linearVelocity = clampedVelocity;

        // =========================
        // Rotation fix:
        // Rotate the bike to face the direction it's moving.
        // This prevents "fighting" / spinning when changing directions.
        // =========================
        if (rb.linearVelocity.sqrMagnitude > rotateMinSpeed * rotateMinSpeed)
        {
            // Angle from velocity vector (in degrees)
            float angle = Mathf.Atan2(rb.linearVelocity.y, rb.linearVelocity.x) * Mathf.Rad2Deg;

            // Apply offset based on how the sprite is drawn
            rb.rotation = angle + spriteAngleOffset;
        }
    }

    // =========================
    // Speed Zone API (only needed if you still use speed-limiting zones)
    // =========================

    // Called when entering a SpeedZone that limits speed (legacy system)
    public void AddSpeedLimit(object source, float limit)
    {
        if (source == null) return;

        activeSpeedLimits[source] = limit;
        RecalculateSpeedLimit();
    }

    // Called when exiting a SpeedZone that limits speed (legacy system)
    public void RemoveSpeedLimit(object source)
    {
        if (source == null) return;

        if (activeSpeedLimits.Remove(source))
        {
            RecalculateSpeedLimit();
        }
    }

    // Recalculate the lowest active speed limit
    private void RecalculateSpeedLimit()
    {
        if (activeSpeedLimits.Count == 0)
        {
            cachedSpeedLimit = null;
            return;
        }

        float min = float.MaxValue;
        foreach (float limit in activeSpeedLimits.Values)
        {
            if (limit < min) min = limit;
        }

        cachedSpeedLimit = min;
    }

    // =========================
    // Speedometer Accessors (UI)
    // =========================
    public float CurrentSpeed
    {
        get => rb == null ? 0f : rb.linearVelocity.magnitude;
    }

    public float CurrentSpeedLimit
    {
        get => cachedSpeedLimit.HasValue
            ? Mathf.Min(maxSpeed, cachedSpeedLimit.Value)
            : maxSpeed;
    }

    // =========================
    // Delivery Logic (UPDATED for multi-package)
    // =========================
    public void PickUpPackage()
    {
        if (packagesHeld >= maxPackages)
        {
            Debug.Log("Cannot pick up: already at max packages.");
            return;
        }

        packagesHeld++;
        HasPackage = packagesHeld > 0;

        Debug.Log($"Picked up 1 package. Held: {packagesHeld}/{maxPackages}");
    }

    public void DeliverPackage()
    {
        if (packagesHeld <= 0)
        {
            Debug.Log("No package to deliver.");
            return;
        }

        packagesHeld--;
        HasPackage = packagesHeld > 0;

        deliveriesCompleted++;
        deliveriesOnCurrentTank++;

        Debug.Log($"Delivered 1 package. Held: {packagesHeld}/{maxPackages}. Total deliveries: {deliveriesCompleted}");

        FuelManager.Instance?.UseFuel(1);

        if (deliveriesOnCurrentTank >= maxDeliveriesPerTank)
        {
            outOfFuel = true;
            Debug.Log("Out of fuel! Press E to refuel.");
        }
    }

    // =========================
    // Refueling
    // =========================
    public void Refuel()
    {
        deliveriesOnCurrentTank = 0;
        outOfFuel = false;

        FuelManager.Instance?.SetFuel(maxDeliveriesPerTank);

        Debug.Log("Refueled. Tank reset.");
    }

}