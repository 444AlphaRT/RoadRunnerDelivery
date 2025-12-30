using System.Collections;
using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class TrafficLightController : MonoBehaviour
{
    [Header("Timing")]
    [SerializeField] private float greenDuration = 3f;
    [SerializeField] private float redDuration = 3f;

    [Header("Fines (local escalation)")]
    [SerializeField] private int baseRedFine = 3;

    [Tooltip("Multiplier applied per additional violation: 1st=base, 2nd=base*mult, 3rd=base*mult^2, ...")]
    [SerializeField] private float fineMultiplierPerViolation = 1.5f;

    [Tooltip("Optional cap to prevent insane values. Set to 0 to disable cap.")]
    [SerializeField] private int maxFineCap = 0;

    [Header("Crossing detection")]
    [SerializeField] private float minSpeedToCount = 0.2f;

    [Header("Sprites")]
    [SerializeField] private Sprite greenSprite;
    [SerializeField] private Sprite redSprite;

    private bool isGreen = true;
    private int redViolationsLocal = 0;

    private SpriteRenderer sr;
    private BoxCollider2D box;

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();

        box = GetComponent<BoxCollider2D>();
        box.isTrigger = true;
    }

    private void Start()
    {
        UpdateVisual();
        StartCoroutine(SwitchRoutine());
    }

    private IEnumerator SwitchRoutine()
    {
        while (true)
        {
            isGreen = true;
            UpdateVisual();
            yield return new WaitForSeconds(greenDuration);

            isGreen = false;
            UpdateVisual();
            yield return new WaitForSeconds(redDuration);
        }
    }

    private void UpdateVisual()
    {
        if (sr == null) return;

        if (isGreen)
        {
            if (greenSprite != null) sr.sprite = greenSprite;
        }
        else
        {
            if (redSprite != null) sr.sprite = redSprite;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Must be the player
        if (!other.CompareTag("Player"))
            return;

        // Only punish on RED
        if (isGreen)
            return;

        // Must have a Rigidbody2D to read velocity
        Rigidbody2D rb = other.attachedRigidbody;
        if (rb == null) rb = other.GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            Debug.LogWarning("TrafficLight: Player entered but has no Rigidbody2D.");
            return;
        }

        // Must be moving enough
        float speed = rb.linearVelocity.magnitude;
        if (speed < minSpeedToCount)
            return;

        // Must have PenaltyManager
        if (PenaltyManager.Instance == null)
        {
            Debug.LogError("TrafficLight: PenaltyManager.Instance is NULL in this scene. Add PenaltyManager to the scene.");
            return;
        }

        // Increase local violation count (per traffic light)
        redViolationsLocal++;

        // Compute fine: base * mult^(n-1)
        float fineFloat = baseRedFine * Mathf.Pow(fineMultiplierPerViolation, redViolationsLocal - 1);

        // Round to int (you can change to FloorToInt if you prefer)
        int fine = Mathf.RoundToInt(fineFloat);

        // Optional cap
        if (maxFineCap > 0)
            fine = Mathf.Min(fine, maxFineCap);

        PenaltyManager.Instance.IssueTicket(
            PenaltyManager.ViolationType.RedLight,
            fine,
            "RED LIGHT"
        );
    }

    private void OnDrawGizmosSelected()
    {
        BoxCollider2D b = GetComponent<BoxCollider2D>();
        if (b == null) return;

        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(b.bounds.center, b.bounds.size);
    }
}
