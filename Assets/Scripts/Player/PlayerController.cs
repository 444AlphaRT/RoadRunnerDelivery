using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    // =========================
    // Movement
    // =========================
    [Header("Movement Settings")]
    public float moveSpeed = 5f;   // Not used for instant velocity anymore (kept for compatibility)
    public float maxSpeed = 8f;    // Absolute maximum speed (units/sec)

    [Header("Acceleration")]
    [Tooltip("How fast we accelerate toward the target speed (units/sec^2).")]
    public float acceleration = 12f;

    [Tooltip("How fast we decelerate to zero when no input (units/sec^2).")]
    public float deceleration = 16f;

    [Tooltip("Global multiplier to reduce acceleration (we'll use 0.5 to divide by 2).")]
    public float accelerationMultiplier = 0.5f; // <-- divide acceleration by 2

    [Tooltip("Extra reduction when turning (diagonal input). 0.5 means half the acceleration again.")]
    public float turningAccelerationMultiplier = 0.5f; // <-- even slower while turning

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
    public bool HasPackage = false;      // Kept for compatibility with existing scripts
    public int deliveriesCompleted = 0;  // Total number of successful deliveries

    [Header("Package Carrying")]
    public int maxPackages = 2;          // Maximum packages the player can carry
    public int packagesHeld = 0;         // Current packages held (0..maxPackages)

    // =========================
    // Fuel (Compatibility Flag)
    // =========================
    [Header("Fuel (Driven by FuelManager)")]
    [Tooltip("Read-only style flag: this is updated from FuelManager.CurrentFuel. Do NOT set it manually.")]
    public bool outOfFuel = false;

    // =========================
    // Speed Zone System (supports overlapping zones)
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
            Debug.LogError("PlayerController: Rigidbody2D is missing!");

        // Sync the legacy bool with the package counter (important on scene start)
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

        // Fuel is controlled ONLY by FuelManager.
        bool noFuel = FuelManager.Instance != null && FuelManager.Instance.CurrentFuel <= 0;

        // Keep a compatibility flag in sync (for UI / other scripts if they read it)
        outOfFuel = noFuel;

        // Stop movement if not allowed or out of fuel
        if (!canMove || noFuel)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        // Determine current max speed (speed zones override global maxSpeed)
        float currentMaxSpeed = cachedSpeedLimit.HasValue
            ? Mathf.Min(maxSpeed, cachedSpeedLimit.Value)
            : maxSpeed;

        // Target velocity based on input and max speed
        Vector2 targetVelocity = inputDirection * currentMaxSpeed;

        // Detect if the player is currently "turning" (diagonal movement)
        // If both axes are pressed, we reduce acceleration more.
        bool isTurning = Mathf.Abs(inputDirection.x) > 0.001f && Mathf.Abs(inputDirection.y) > 0.001f;

        // Acceleration / deceleration rate
        float rate;
        if (inputDirection.sqrMagnitude > 0.001f)
        {
            // Base acceleration reduced by 2 (multiplier 0.5)
            float effectiveAccel = acceleration * accelerationMultiplier;

            // If turning (diagonal), reduce even more
            if (isTurning)
                effectiveAccel *= turningAccelerationMultiplier;

            rate = effectiveAccel;
        }
        else
        {
            // Keep deceleration strong (feels like braking)
            rate = deceleration;
        }

        // Smoothly move current velocity toward target velocity
        rb.linearVelocity = Vector2.MoveTowards(rb.linearVelocity, targetVelocity, rate * Time.fixedDeltaTime);

        // Rotate the bike to face the direction it's moving
        if (rb.linearVelocity.sqrMagnitude > rotateMinSpeed * rotateMinSpeed)
        {
            float angle = Mathf.Atan2(rb.linearVelocity.y, rb.linearVelocity.x) * Mathf.Rad2Deg;
            rb.rotation = angle + spriteAngleOffset;
        }
    }

    // =========================
    // Speed Zone API (legacy system)
    // =========================
    public void AddSpeedLimit(object source, float limit)
    {
        if (source == null) return;

        activeSpeedLimits[source] = limit;
        RecalculateSpeedLimit();
    }

    public void RemoveSpeedLimit(object source)
    {
        if (source == null) return;

        if (activeSpeedLimits.Remove(source))
            RecalculateSpeedLimit();
    }

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
    // Delivery Logic (multi-package)
    // =========================
    public void PickUpPackage()
    {
        if (packagesHeld >= maxPackages)
        {
            Debug.Log("Cannot pick up: already at max packages.");
            return;
        }

        // Optional: prevent pickup if no fuel (so player cannot start new deliveries while empty)
        if (FuelManager.Instance != null && FuelManager.Instance.CurrentFuel <= 0)
        {
            Debug.Log("Cannot pick up: out of fuel. Press E to refuel.");
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

        Debug.Log($"Delivered 1 package. Held: {packagesHeld}/{maxPackages}. Total deliveries: {deliveriesCompleted}");

        // Fuel consumed ONLY through FuelManager policy
        FuelManager.Instance?.RegisterDeliveryCompleted();
    }
}
