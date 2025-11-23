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

        string status = player.HasPackage ? "Yes" : "No";
        hasPackageText.text = "Package: " + status;

        deliveriesText.text = "Deliveries: " + player.deliveriesCompleted;
    }
}