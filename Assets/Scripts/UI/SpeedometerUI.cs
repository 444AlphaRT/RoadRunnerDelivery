using UnityEngine;
using TMPro;
using System.Globalization;

public class SpeedometerUI : MonoBehaviour
{
    [Header("References")]
    public PlayerController player;
    public TextMeshProUGUI speedText;

    [Header("KM/H Calibration")]
    [Tooltip("When the player's INITIAL maxSpeed is reached, show this KM/H (e.g. 30).")]
    public float baselineKmhAtInitialMaxSpeed = 30f;

    [Tooltip("Set this to the player's initial maxSpeed value (units/sec) at the start of the level (e.g. 8).")]
    public float initialMaxSpeedUnits = 8f;

    [Header("Format")]
    public int decimals = 0;

    [Header("Color thresholds")]
    [Range(0.5f, 1f)]
    public float warningPercent = 0.9f;   // Yellow when >= 90% of limit

    public bool showLimit = true;

    private static readonly CultureInfo culture = CultureInfo.InvariantCulture;

    private float unitsToKmh = 1f;

    private void Awake()
    {
        if (initialMaxSpeedUnits <= 0.01f)
            initialMaxSpeedUnits = 1f;

        // Convert units/sec -> km/h for DISPLAY ONLY
        unitsToKmh = baselineKmhAtInitialMaxSpeed / initialMaxSpeedUnits;
    }

    private void Update()
    {
        if (player == null || speedText == null) return;

        // Actual physics values (units/sec)
        float speedUnits = player.CurrentSpeed;
        float limitUnits = player.CurrentSpeedLimit;

        // Display values (km/h)
        float speedKmh = speedUnits * unitsToKmh;
        float limitKmh = limitUnits * unitsToKmh;

        string speedStr = speedKmh.ToString("F" + decimals, culture);
        string limitStr = limitKmh.ToString("F" + decimals, culture);

        if (showLimit)
            speedText.text = $"Speed: {speedStr} / {limitStr} km/h";
        else
            speedText.text = $"Speed: {speedStr} km/h";

        // Color logic based on REAL limit (units)
        if (limitUnits <= 0.01f)
        {
            speedText.color = Color.white;
            return;
        }

        if (speedUnits > limitUnits + 0.01f)
        {
            speedText.color = Color.red;
        }
        else if (speedUnits >= limitUnits * warningPercent)
        {
            speedText.color = Color.yellow;
        }
        else
        {
            speedText.color = Color.green;
        }
    }
}
