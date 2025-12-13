using UnityEngine;
public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 5f;// Base movement speed
    public float maxSpeed = 8f;// Maximum allowed speed
    [Header("Gameplay State")]
    public bool canMove = false;// Whether the player is allowed to move
    [Header("Delivery State")]
    public bool HasPackage = false;// Whether the player is currently holding a package
    public int deliveriesCompleted = 0;// Total number of completed deliveries
    [Header("Fuel Settings")]
    public int maxDeliveriesPerTank = 2;     
    public bool outOfFuel = false;          
    private int deliveriesOnCurrentTank = 0; 
    [Header("Refuel Settings")]
    public int refuelCostEmpty = 10;// Cost when fuel is empty (0)
    public int refuelCostOneLeft = 5;// Cost when fuel is 1
    private Vector2 inputDirection;
    private Rigidbody2D rb;
    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            Debug.LogError("PlayerController: No Rigidbody2D found on the Player object.");
        }
    }
    private void Update()
    {
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");
        inputDirection = new Vector2(horizontal, vertical).normalized;
        // --- Refuel by pressing E ---
        if (Input.GetKeyDown(KeyCode.E))
        {
            TryRefuelByKey();
        }
    }
    private void FixedUpdate()
    {
        if (rb == null) return;
        if (!canMove || outOfFuel)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }
        Vector2 desiredVelocity = inputDirection * moveSpeed;
        Vector2 clampedVelocity = Vector2.ClampMagnitude(desiredVelocity, maxSpeed);
        rb.linearVelocity = clampedVelocity;
    }
    public void PickUpPackage()
    {
        if (HasPackage)
        {
            Debug.Log("Already holding a package.");
            return;
        }
        HasPackage = true;
        Debug.Log("Picked up package");
    }
    public void DeliverPackage()
    {
        if (!HasPackage)
        {
            Debug.Log("No package to deliver.");
            return;
        }
        HasPackage = false;
        deliveriesCompleted++;
        Debug.Log("Delivered package. Total deliveries: " + deliveriesCompleted);
        deliveriesOnCurrentTank++;
        if (FuelManager.Instance != null)
        {
            FuelManager.Instance.UseFuel(1);
        }
        if (deliveriesOnCurrentTank >= maxDeliveriesPerTank)
        {
            outOfFuel = true;
            Debug.Log("Out of fuel! Press E to refuel.");
        }
    }
    public void Refuel()
    {
        deliveriesOnCurrentTank = 0;
        outOfFuel = false;
        if (FuelManager.Instance != null)
        {
            FuelManager.Instance.SetFuel(maxDeliveriesPerTank);
        }
        Debug.Log("Refueled! Tank reset.");
    }
    private void TryRefuelByKey()
    {
        int fuelLeft = maxDeliveriesPerTank - deliveriesOnCurrentTank;
        if (fuelLeft >= maxDeliveriesPerTank)
            return;
        int cost;
        if (fuelLeft == 0)
            cost = refuelCostEmpty;
        else if (fuelLeft == 1)
            cost = refuelCostOneLeft;
        else
            return;
        if (MoneyManager.Instance == null)
        {
            Debug.LogWarning("MoneyManager.Instance is NULL!");
            return;
        }
        bool paid = MoneyManager.Instance.TrySpend(cost);
        if (!paid)
        {
            Debug.Log("Not enough money to refuel.");
            return;
        }
        Refuel();
        Debug.Log($"Refueled by paying {cost} coins.");
    }
}
