using UnityEngine;
using TMPro;
using System.Globalization;

public class SpeedometerUI : MonoBehaviour
{
    [Header("References")]
    public PlayerController player;
    public TextMeshProUGUI speedText;

    [Header("KM/H Calibration (Display Only)")]
    [Tooltip("When the player reaches their CURRENT max speed, show this KM/H (e.g. 50).")]
    public float baselineKmhAtMaxSpeed = 50f;

    [Header("Format")]
    public int decimals = 0;

    [Header("Color thresholds")]
    [Range(0.5f, 1f)]
    [Tooltip("Yellow when reaching this percentage of the player's current max speed.")]
    public float warningPercent = 0.9f;

    [Tooltip("Show \"current speed / max speed\" text.")]
    public bool showMax = true;

    private static readonly CultureInfo culture = CultureInfo.InvariantCulture;

    private void Update()
    {
        if (player == null || speedText == null)
            return;

        // Real speed values from the player (units/sec)
        float speedUnits = player.CurrentSpeed;

        // With no zones, the 'limit' is just the player's current maxSpeed
        float maxUnits = player.maxSpeed;

        // Prevent division by zero
        float safeMax = Mathf.Max(maxUnits, 0.01f);

        // Display scaling:
        // When speedUnits == maxUnits -> display baselineKmhAtMaxSpeed (e.g. 50 km/h)
        float unitsToKmh = baselineKmhAtMaxSpeed / safeMax;

        // Convert to display km/h
        float speedKmh = speedUnits * unitsToKmh;
        float maxKmh = maxUnits * unitsToKmh; // equals baselineKmhAtMaxSpeed

        // Format
        string speedStr = speedKmh.ToString("F" + decimals, culture);
        string maxStr = maxKmh.ToString("F" + decimals, culture);

        // Update text
        if (showMax)
            speedText.text = $"Speed: {speedStr} / {maxStr} km/h";
        else
            speedText.text = $"Speed: {speedStr} km/h";

        // Color logic based on REAL speed vs REAL max speed
        if (speedUnits >= maxUnits * warningPercent)
        {
            speedText.color = Color.yellow;
        }
        else
        {
            speedText.color = Color.green;
        }
    }
}
