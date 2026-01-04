using UnityEngine;
using System.Collections;

public class StartLevel : MonoBehaviour
{
    [Header("Post-Start Box")]
    [Tooltip("גררי לכאן את ה-Panel שיצרת עם הרקע וההוראות")]
    [SerializeField] private GameObject welcomePanelObject;

    [Tooltip("כמה שניות ההודעה תופיע על המסך")]
    [SerializeField] private float welcomeDuration = 5f;

    void Start()
    {
        // התיקון: מפעילים את ה-Coroutine פעם אחת בלבד כשהמשחק מתחיל
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