using System.Collections;
using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class TrafficLightController : MonoBehaviour
{
    [Header("Timing")]
    [SerializeField] private float greenDuration = 3f;
    [SerializeField] private float redDuration = 3f;

    [Header("Fines")]
    [SerializeField] private int firstRedFine = 3;
    [SerializeField] private int secondRedFine = 6;

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
        if (rb == null)
        {
            rb = other.GetComponent<Rigidbody2D>();
            if (rb == null)
            {
                Debug.LogWarning("TrafficLight: Player entered but has no Rigidbody2D.");
                return;
            }
        }

        // Must be moving enough
        float speed = rb.linearVelocity.magnitude;
        if (speed < minSpeedToCount)
            return;

        // Must have PenaltyManager in this scene
        if (PenaltyManager.Instance == null)
        {
            Debug.LogError("TrafficLight: PenaltyManager.Instance is NULL in this scene. Add PenaltyManager to the scene.");
            return;
        }

        // Escalation per-light: 1st fine, 2nd fine, then keep second fine
        redViolationsLocal++;
        int fine = (redViolationsLocal == 1) ? firstRedFine : secondRedFine;

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