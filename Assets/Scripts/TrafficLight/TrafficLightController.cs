using System.Collections;
using UnityEngine;

[RequireComponent(typeof(EdgeCollider2D))]
public class TrafficLightController : MonoBehaviour
{
    [Header("Timing")]
    [SerializeField] private float greenDuration = 3f;
    [SerializeField] private float redDuration = 3f;

    [Header("Fines (local escalation)")]
    [SerializeField] private int baseRedFine = 3;

    [SerializeField] private float fineMultiplierPerViolation = 1.5f;
    [SerializeField] private int maxFineCap = 0;

    [Header("Crossing detection")]
    [SerializeField] private float minSpeedToCount = 0.2f;

    [Header("Sprites")]
    [SerializeField] private Sprite greenSprite;
    [SerializeField] private Sprite redSprite;

    private bool isGreen = true;
    private int redViolationsLocal = 0;

    private SpriteRenderer sr;
    private EdgeCollider2D edge;

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();

        edge = GetComponent<EdgeCollider2D>();
        edge.isTrigger = true;
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

        sr.sprite = isGreen ? greenSprite : redSprite;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
            return;

        if (isGreen)
            return;

        Rigidbody2D rb = other.attachedRigidbody;
        if (rb == null) return;

        float speed = rb.linearVelocity.magnitude;
        if (speed < minSpeedToCount)
            return;

        if (PenaltyManager.Instance == null)
            return;

        redViolationsLocal++;

        float fineFloat =
            baseRedFine *
            Mathf.Pow(fineMultiplierPerViolation, redViolationsLocal - 1);

        int fine = Mathf.RoundToInt(fineFloat);

        if (maxFineCap > 0)
            fine = Mathf.Min(fine, maxFineCap);

        bool paid = PenaltyManager.Instance.IssueTicket(
            PenaltyManager.ViolationType.RedLight,
            fine,
            "RED LIGHT"
        );
        if (paid)
        {
            AlertUI.Instance.Show("RED LIGHT! - Money deducted");
        }
    }

    private void OnDrawGizmosSelected()
    {
        EdgeCollider2D e = GetComponent<EdgeCollider2D>();
        if (e == null || e.points.Length < 2) return;

        Gizmos.color = Color.red;
        for (int i = 0; i < e.points.Length - 1; i++)
        {
            Vector3 a = transform.TransformPoint(e.points[i]);
            Vector3 b = transform.TransformPoint(e.points[i + 1]);
            Gizmos.DrawLine(a, b);
        }
    }
}