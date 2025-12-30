using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class SpeedZone : MonoBehaviour
{
    [Header("Speed limit")]
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
    private Transform player;
    private Rigidbody2D playerRb;

    private bool armed = true;
    private int localSpeedViolations = 0;

    private void Awake()
    {
        box = GetComponent<BoxCollider2D>();
        box.isTrigger = true;
    }

    private void Start()
    {
        TryAssignPlayer();
    }

    private void TryAssignPlayer()
    {
        if (player != null && playerRb != null) return;

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj == null) return;

        player = playerObj.transform;
        playerRb = playerObj.GetComponent<Rigidbody2D>();
    }

    private void Update()
    {
        if (box == null) return;

        if (player == null || playerRb == null)
            TryAssignPlayer();

        if (player == null || playerRb == null)
            return;

        bool isInside = box.bounds.Contains(player.position);
        if (!isInside)
        {
            armed = true;
            return;
        }

        float speed = playerRb.linearVelocity.magnitude;

        // If we want one ticket per overspeed event, re-arm only after slowing down
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

            // Local escalation counter (per speed zone)
            localSpeedViolations++;

            float fineFloat = baseSpeedFine * Mathf.Pow(fineMultiplierPerViolation, localSpeedViolations - 1);
            int fine = Mathf.RoundToInt(fineFloat);

            if (maxFineCap > 0)
                fine = Mathf.Min(fine, maxFineCap);

            // Report to PenaltyManager (PenaltyManager handles money/unpaid/timeouts/game over)
            PenaltyManager.Instance.IssueTicket(
                PenaltyManager.ViolationType.Speed,
                fine,
                "SPEEDING"
            );

            armed = false;
        }
    }

    private void OnDrawGizmosSelected()
    {
        BoxCollider2D b = GetComponent<BoxCollider2D>();
        if (b == null) return;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(b.bounds.center, b.bounds.size);
    }
}
