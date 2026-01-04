using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class DeliveryUI : MonoBehaviour
{
    [Header("References")]
    private PlayerController player;

    [Header("UI Elements")]
    public TextMeshProUGUI deliveryText; // הטקסט של המשלוחים
    public Image packageIcon;            // תמונת הקופסה

    [Header("Settings")]
    public Color activeColor = new Color(1, 1, 1, 1);      // לבן (יש חבילה)
    public Color inactiveColor = new Color(1, 1, 1, 0.2f); // שקוף (אין חבילה)

    private void Awake()
    {
        // מוצאים את השחקן (הפקודה המעודכנת)
        player = FindFirstObjectByType<PlayerController>();
    }

    private void Update()
    {
        if (player == null) return;

        // --- תיקון 1: גישה ישירה למשתנה deliveriesCompleted ---
        deliveryText.text = "Deliveries: " + player.deliveriesCompleted;

        // --- תיקון 2: גישה ישירה למשתנה packagesHeld ---
        // (אם השם אצלך הוא שונה, למשל hasPackage, פשוט תשני את המילה packagesHeld לשם הנכון)
        if (player.packagesHeld > 0)
        {
            packageIcon.color = activeColor;
        }
        else
        {
            packageIcon.color = inactiveColor;
        }
    }
}