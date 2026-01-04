using UnityEngine;
using System.Collections;

public class OpeningSequence : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerController player;
    [SerializeField] private GameObject introPanel;

    [Header("UI Elements Hidden During Intro")]
    [SerializeField] private GameObject[] uiToHideDuringIntro;

    [Header("Post-Start Welcome Box")]
    [Tooltip("גררי לכאן את ה-Panel שיצרת עם הרקע וההוראות")]
    [SerializeField] private GameObject welcomePanelObject;

    [Tooltip("כמה שניות ההודעה תופיע על המסך")]
    [SerializeField] private float welcomeDuration = 5f;

    private void Awake()
    {
        if (introPanel == null)
            introPanel = gameObject;
    }

    private void Start()
    {
        // הקפאת השחקן
        if (player != null)
            player.canMove = false;

        // הצגת מסך הפתיחה
        if (introPanel != null)
            introPanel.SetActive(true);

        // הסתרת ה-HUD
        if (uiToHideDuringIntro != null)
        {
            foreach (GameObject ui in uiToHideDuringIntro)
            {
                if (ui != null) ui.SetActive(false);
            }
        }

        // וידוא שהודעת ה-Welcome מוסתרת בהתחלה
        if (welcomePanelObject != null)
            welcomePanelObject.SetActive(false);

    }

    // הפונקציה שמופעלת ע"י כפתור START GAME
    public void OnStartGamePressed()
    {
        // העלמת מסך הפתיחה
        if (introPanel != null)
            introPanel.SetActive(false);

        // הצגת ה-HUD
        if (uiToHideDuringIntro != null)
        {
            foreach (GameObject ui in uiToHideDuringIntro)
            {
                if (ui != null) ui.SetActive(true);
            }
        }

        // שחרור השחקן
        if (player != null)
            player.canMove = true;

        // הפעלת רצף ה-Welcome
        StartCoroutine(PostStartFlow());
    }

    private IEnumerator PostStartFlow()
    {
        // 1) הצגת הקופסה המעוצבת
        if (welcomePanelObject != null)
            welcomePanelObject.SetActive(true);

        // המתנה לפי הזמן שהגדרת
        yield return new WaitForSeconds(welcomeDuration);

        // 2) העלמת הקופסה
        if (welcomePanelObject != null)
            welcomePanelObject.SetActive(false);

      
    }
}