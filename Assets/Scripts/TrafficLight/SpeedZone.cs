using System.Collections;
using UnityEngine;

public class SpeedZone : MonoBehaviour
{
    [Header("Speed limit")]
    public float zoneMaxSpeed = 3.5f;

    [Header("Penalty Settings")]
    public int firstFine = 5;
    public int secondFine = 10;
    public float stopDuration = 2f;

    [Header("Enforcement Settings")]
    public float graceAfterStop = 0.3f; // small grace time after stop ends, to avoid instant re-trigger

    private BoxCollider2D box;
    private Transform player;
    private Rigidbody2D playerRb;

    private bool handledThisOverspeed = false; // like handledThisRed in traffic light
    private bool isStopping = false;           // prevents stop loops

    private static int speedViolations = 0;    // global violations (same style as traffic lights)

    private void Start()
    {
        box = GetComponent<BoxCollider2D>();
        if (box == null)
        {
            Debug.LogWarning("SpeedZone: BoxCollider2D not found!");
        }

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
            playerRb = playerObj.GetComponent<Rigidbody2D>();

            if (playerRb == null)
            {
                Debug.LogWarning("SpeedZone: Rigidbody2D not found on Player!");
            }
        }
        else
        {
            Debug.LogWarning("SpeedZone: Player with tag 'Player' not found!");
        }
    }

    private void Update()
    {
        if (player == null || box == null || playerRb == null)
        {
            return;
        }

        // If currently stopping the player, do not enforce new violations
        if (isStopping)
        {
            return;
        }

        // Check if player is inside this speed zone area
        bool isInside = box.bounds.Contains(player.position);
        if (!isInside)
        {
            // If not inside, rearm for next entry
            handledThisOverspeed = false;
            return;
        }

        float speed = playerRb.linearVelocity.magnitude;

        // If speeding, handle once until player becomes legal again
        if (speed > zoneMaxSpeed)
        {
            if (!handledThisOverspeed)
            {
                HandleSpeedViolation();
                handledThisOverspeed = true;
            }
        }
        else
        {
            // Player is legal again -> allow a new violation next time they speed
            handledThisOverspeed = false;
        }
    }

    private void HandleSpeedViolation()
    {
        speedViolations++;

        if (speedViolations == 1)
        {
            ApplyFine(firstFine);
        }
        else if (speedViolations == 2)
        {
            ApplyFine(secondFine);
        }
        else
        {
            StopPlayerForSeconds(stopDuration);
        }
    }

    private void ApplyFine(int amount)
    {
        if (MoneyManager.Instance == null)
        {
            Debug.LogWarning("SpeedZone: MoneyManager.Instance is NULL!");
            return;
        }

        bool success = MoneyManager.Instance.TrySpend(amount);

        if (success)
        {
            Debug.Log($"Speed fine applied! -{amount}₪ | Current money: {MoneyManager.Instance.CurrentMoney}");
        }
        else
        {
            Debug.Log("Speed fine not applied – not enough money (already 0).");
        }
    }

    private void StopPlayerForSeconds(float seconds)
    {
        if (player == null)
        {
            return;
        }

        PlayerController pc = player.GetComponent<PlayerController>();
        if (pc == null)
        {
            Debug.LogWarning("SpeedZone: PlayerController not found on Player. Can't stop movement.");
            return;
        }

        StartCoroutine(StopRoutine(pc, seconds));
    }

    private IEnumerator StopRoutine(PlayerController pc, float seconds)
    {
        isStopping = true;

        // Stop movement via the same pattern you used in TrafficLightController
        pc.canMove = false;

        yield return new WaitForSeconds(seconds);

        // IMPORTANT: Always release movement (prevents "stuck forever")
        pc.canMove = true;

        // Reset counters after stop (same idea as your traffic light behavior)
        speedViolations = 0;

        // Rearm enforcement so it doesn't instantly punish again
        handledThisOverspeed = false;

        // Small grace period (optional but makes it feel fair)
        if (graceAfterStop > 0f)
        {
            yield return new WaitForSeconds(graceAfterStop);
        }

        isStopping = false;
    }

    private void OnDrawGizmosSelected()
    {
        BoxCollider2D b = GetComponent<BoxCollider2D>();
        if (b == null)
        {
            return;
        }

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(b.bounds.center, b.bounds.size);
    }
}
