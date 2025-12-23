using UnityEngine;
using TMPro;
using System.Globalization;

public class SpeedometerUI : MonoBehaviour
{
    [Header("References")]
    public PlayerController player;
    public TextMeshProUGUI speedText;

    [Header("Format")]
    public int decimals = 1;

    [Header("Color thresholds")]
    [Range(0.5f, 1f)]
    public float warningPercent = 0.9f;   // Yellow when >= 90% of limit

    public bool showLimit = true;

    private static readonly CultureInfo culture = CultureInfo.InvariantCulture;

    private void Update()
    {
        if (player == null || speedText == null) return;

        float speed = player.CurrentSpeed;
        float limit = player.CurrentSpeedLimit;

        string speedStr = speed.ToString("F" + decimals, culture);
        string limitStr = limit.ToString("F" + decimals, culture);

        if (showLimit)
            speedText.text = $"Speed: {speedStr} / {limitStr}";
        else
            speedText.text = $"Speed: {speedStr}";

        // Color logic: Green -> Yellow -> Red (if speed exceeds limit)
        if (limit <= 0.01f)
        {
            speedText.color = Color.white;
            return;
        }

        if (speed > limit + 0.01f)
        {
            speedText.color = Color.red;
        }
        else if (speed >= limit * warningPercent)
        {
            speedText.color = Color.yellow;
        }
        else
        {
            speedText.color = Color.green;
        }
    }
}
