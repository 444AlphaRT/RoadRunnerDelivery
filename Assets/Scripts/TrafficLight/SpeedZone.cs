using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class SpeedZone : MonoBehaviour
{
    [Header("Speed limit (Unity units/sec)")]
    [SerializeField] private float zoneMaxSpeed = 3.5f;

    [Header("Crossing detection")]
    [SerializeField] private float minSpeedToCount = 0.2f;

    [Header("Anti-spam")]
    [Tooltip("If true, a new ticket is issued only after the player slows down to legal speed.")]
    [SerializeField] private bool requireSlowdownBeforeNextTicket = true;

    [Header("Fine escalation (local)")]
    [SerializeField] private int baseSpeedFine = 5;

    [Tooltip("Multiplier applied per additional violation: 1st=base, 2nd=base*mult, 3rd=base*mult^2, ...")]
    [SerializeField] private float fineMultiplierPerViolation = 1.5f;

    [Tooltip("Optional cap to prevent insane values. Set to 0 to disable cap.")]
    [SerializeField] private int maxFineCap = 0;

    private BoxCollider2D box;

    private PlayerController player;
    private Rigidbody2D playerRb;

    private bool isInside = false;

    // Ticketing state
    private bool armed = true;
    private int localSpeedViolations = 0;

    private void Awake()
    {
        box = GetComponent<BoxCollider2D>();
        box.isTrigger = true;
    }

    private void Update()
    {
        if (!isInside) return;
        if (player == null || playerRb == null) return;

        float speed = playerRb.linearVelocity.magnitude;

        // Re-arm only after slowing down (optional)
        if (requireSlowdownBeforeNextTicket && !armed)
        {
            if (speed <= zoneMaxSpeed)
                armed = true;

            return;
        }

        if (speed < minSpeedToCount)
            return;

        if (speed > zoneMaxSpeed)
        {
            if (PenaltyManager.Instance == null)
            {
                Debug.LogWarning("SpeedZone: PenaltyManager.Instance is NULL in this scene.");
                return;
            }

            localSpeedViolations++;

            float fineFloat = baseSpeedFine * Mathf.Pow(fineMultiplierPerViolation, localSpeedViolations - 1);
            int fine = Mathf.RoundToInt(fineFloat);

            if (maxFineCap > 0)
                fine = Mathf.Min(fine, maxFineCap);

            PenaltyManager.Instance.IssueTicket(
                PenaltyManager.ViolationType.Speed,
                fine,
                "SPEEDING"
            );

            armed = false;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // We only care about the player
        PlayerController pc = other.GetComponent<PlayerController>();
        if (pc == null) return;

        player = pc;
        playerRb = pc.GetComponent<Rigidbody2D>();

        isInside = true;
        armed = true;

        // IMPORTANT: This is what makes CurrentSpeedLimit work (UI will now know there is a limit)
        player.AddSpeedLimit(this, zoneMaxSpeed);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        PlayerController pc = other.GetComponent<PlayerController>();
        if (pc == null) return;

        // Remove the speed limit when leaving the zone
        pc.RemoveSpeedLimit(this);

        isInside = false;
        armed = true;
    }

    private void OnDisable()
    {
        // Safety: if the zone is disabled while player is inside, remove the limit
        if (player != null)
            player.RemoveSpeedLimit(this);

        isInside = false;
        armed = true;
    }

    private void OnDrawGizmosSelected()
    {
        BoxCollider2D b = GetComponent<BoxCollider2D>();
        if (b == null) return;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(b.bounds.center, b.bounds.size);
    }
}
