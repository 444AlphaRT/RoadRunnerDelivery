using System.Collections;
using UnityEngine;

public class TrafficLightController : MonoBehaviour
{
    [Header("Timing")]
    public float greenDuration = 3f;   
    public float redDuration = 3f;     

    [Header("Penalty Settings")]
    public int firstRedFine = 3;       
    public int secondRedFine = 6;     
    public float stopDuration = 5f;   

    [Header("Sprites")]
    public Sprite greenSprite;         
    public Sprite redSprite;           

    private bool isGreen = true;      
    private bool handledThisRed = false; 

    private SpriteRenderer sr;
    private BoxCollider2D box;
    private Transform player;
    private static int redViolations = 0;

    private void Start()
    {
        sr = GetComponent<SpriteRenderer>();
        box = GetComponent<BoxCollider2D>();
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
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
            handledThisRed = false;   
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

        if (isGreen && greenSprite != null)
            sr.sprite = greenSprite;
        else if (!isGreen && redSprite != null)
            sr.sprite = redSprite;
    }

    private void Update()
    {
        if (player == null || box == null)
            return;
        if (!isGreen && !handledThisRed)
        {
            if (box.bounds.Contains(player.position))
            {
                HandleRedViolation();
                handledThisRed = true; 
            }
        }
    }

    private void HandleRedViolation()
    {
        redViolations++;

        if (redViolations == 1)
        {
            ApplyFine(firstRedFine);
        }
        else if (redViolations == 2)
        {
            ApplyFine(secondRedFine);
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
            Debug.LogWarning("TrafficLight: MoneyManager.Instance is NULL!");
            return;
        }

        bool success = MoneyManager.Instance.TrySpend(amount);

        if (success)
        {
            Debug.Log($"Fine applied! -{amount}₪ | Current money: {MoneyManager.Instance.CurrentMoney}");
        }
        else
        {
            Debug.Log("Fine not applied – not enough money (already 0).");
        }
    }

    private void StopPlayerForSeconds(float seconds)
    {
        PlayerController pc = player.GetComponent<PlayerController>();
        if (pc == null)
        {
            Debug.LogWarning("TrafficLight: PlayerController not found on Player. Can't stop movement.");
            return;
        }

        StartCoroutine(StopRoutine(pc, seconds));
    }

    private IEnumerator StopRoutine(PlayerController pc, float seconds)
    {
        bool prevCanMove = pc.canMove;
        pc.canMove = false;
        yield return new WaitForSeconds(seconds);
        pc.canMove = prevCanMove;
    }

    private void OnDrawGizmosSelected()
    {
        BoxCollider2D b = GetComponent<BoxCollider2D>();
        if (b == null) return;
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(b.bounds.center, b.bounds.size);
    }
}
