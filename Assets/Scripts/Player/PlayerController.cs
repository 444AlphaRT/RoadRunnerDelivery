using UnityEngine;

public class PlayerController : MonoBehaviour
{
    // =========================
    // Movement
    // =========================
    [Header("Movement Settings")]
    public float moveSpeed = 5f;   // Kept for compatibility (not used as instant velocity)
    public float maxSpeed = 8f;    // Absolute maximum speed (units/sec) - upgrades can increase this

    [Header("Acceleration")]
    [Tooltip("How fast we accelerate toward the target speed (units/sec^2).")]
    public float acceleration = 12f;

    [Tooltip("How fast we decelerate to zero when no input (units/sec^2).")]
    public float deceleration = 16f;

    [Tooltip("Global multiplier to reduce acceleration (e.g. 0.5 means half acceleration).")]
    public float accelerationMultiplier = 0.5f;

    [Tooltip("Extra reduction when turning diagonally. 0.5 means half acceleration again while turning.")]
    public float turningAccelerationMultiplier = 0.5f;

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

        // Upgrades control maxSpeed (NO zone clamping)
        float currentMaxSpeed = maxSpeed;

        // Target velocity based on input and max speed
        Vector2 targetVelocity = inputDirection * currentMaxSpeed;

        // Detect diagonal movement (turning)
        bool isTurning = Mathf.Abs(inputDirection.x) > 0.001f && Mathf.Abs(inputDirection.y) > 0.001f;

        // Acceleration / deceleration rate
        float rate;
        if (inputDirection.sqrMagnitude > 0.001f)
        {
            float effectiveAccel = acceleration * accelerationMultiplier;

            if (isTurning)
                effectiveAccel *= turningAccelerationMultiplier;

            rate = effectiveAccel;
        }
        else
        {
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
    // Speedometer Accessors (UI)
    // =========================
    public float CurrentSpeed
    {
        get => rb == null ? 0f : rb.linearVelocity.magnitude;
    }

    // No zones anymore -> limit is the player's current maxSpeed
    public float CurrentSpeedLimit => maxSpeed;

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
