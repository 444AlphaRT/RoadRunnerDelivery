using UnityEngine;
using TMPro;

public class DeliveryUI : MonoBehaviour
{
    [Header("References")]
    public PlayerController player;
    public TextMeshProUGUI hasPackageText;
    public TextMeshProUGUI deliveriesText;

    private void Update()
    {
        if (player == null) return;

        // Show carried packages count (Level 3)
        hasPackageText.text = $"Packages: {player.packagesHeld}/{player.maxPackages}";

        // Show completed deliveries
        deliveriesText.text = "Deliveries: " + player.deliveriesCompleted;
    }
}
