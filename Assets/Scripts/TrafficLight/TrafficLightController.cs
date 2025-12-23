using System.Collections;
using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class TrafficLightController : MonoBehaviour
{
    [Header("Timing")]
    public float greenDuration = 3f;
    public float redDuration = 3f;

    [Header("Penalty Settings")]
    public int firstRedFine = 3;
    public int secondRedFine = 6;

    [Header("Crossing detection")]
    [Tooltip("Minimum speed required to count as 'crossing' (prevents tickets when barely moving).")]
    public float minSpeedToCount = 0.2f;

    [Header("Anti-spam")]
    [Tooltip("Cooldown after giving a ticket so it won't trigger again while still inside the collider.")]
    public float ticketCooldown = 0.75f;

    [Header("Sprites")]
    public Sprite greenSprite;
    public Sprite redSprite;

    private bool isGreen = true;

    private SpriteRenderer sr;
    private BoxCollider2D box;

    private Rigidbody2D playerRb;

    private bool canTicket = true;
    private Coroutine cooldownCoroutine = null;

    // Ticket escalation per traffic light (NOT freezing here)
    private int redViolationsLocal = 0;

    private void Start()
    {
        sr = GetComponent<SpriteRenderer>();

        box = GetComponent<BoxCollider2D>();
        box.isTrigger = true;

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            playerRb = playerObj.GetComponent<Rigidbody2D>();
            if (playerRb == null) Debug.LogWarning("TrafficLight: Rigidbody2D not found on Player!");
        }
        else
        {
            Debug.LogWarning("TrafficLight: Player with tag 'Player' not found!");
        }

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

        if (isGreen && greenSprite != null) sr.sprite = greenSprite;
        else if (!isGreen && redSprite != null) sr.sprite = redSprite;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        if (playerRb == null) return;

        // Only punish on RED
        if (isGreen) return;

        // Cooldown so it doesn't spam
        if (!canTicket) return;

        // Must be actually moving
        float speed = playerRb.linearVelocity.magnitude;
        if (speed < minSpeedToCount) return;

        IssueRedLightTicket();

        // Start cooldown
        if (cooldownCoroutine != null) StopCoroutine(cooldownCoroutine);
        cooldownCoroutine = StartCoroutine(TicketCooldownRoutine());
    }

    private IEnumerator TicketCooldownRoutine()
    {
        canTicket = false;
        yield return new WaitForSeconds(ticketCooldown);
        canTicket = true;
        cooldownCoroutine = null;
    }

    private void IssueRedLightTicket()
    {
        redViolationsLocal++;

        int fine =
            (redViolationsLocal == 1) ? firstRedFine :
            (redViolationsLocal == 2) ? secondRedFine :
            secondRedFine; // keep charging the 2nd fine amount (or change if you want)

        if (PenaltyManager.Instance == null)
        {
            Debug.LogWarning("TrafficLight: PenaltyManager.Instance is NULL! Can't issue ticket.");
            return;
        }

        PenaltyManager.Instance.IssueTicket(
            PenaltyManager.ViolationType.RedLight,
            fine,
            "RED LIGHT"
        );

        // Optional: if you want the escalation to reset after a successful freeze/payment,
        // you can later reset this from PenaltyManager via an event.
    }

    private void OnDrawGizmosSelected()
    {
        BoxCollider2D b = GetComponent<BoxCollider2D>();
        if (b == null) return;

        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(b.bounds.center, b.bounds.size);
    }
}