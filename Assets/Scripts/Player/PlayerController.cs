using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 5f;   // Base movement speed
    public float maxSpeed = 8f;    // Maximum allowed speed

    [Header("Delivery State")]
    public bool HasPackage = false;           // האם השחקן מחזיק חבילה
    public int deliveriesCompleted = 0;       // כמה משלוחים הושלמו

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
    }

    private void FixedUpdate()
    {
        if (rb == null) return;

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
        Debug.Log("Delivered package Total: " + deliveriesCompleted);
    }
}