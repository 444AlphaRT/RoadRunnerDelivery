using UnityEngine;
using TMPro;
using System.Collections;
using UnityEngine.InputSystem;

public class FineNotification : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private TextMeshProUGUI notificationText;
    [SerializeField] private float displayDuration = 2f;

    private bool isWaitingForInput = false;

    private void Update()
    {
        // אם לא מחכים לאנטר - אין מה לעשות פה
        if (!isWaitingForInput) return;

        bool pressedEnter = false;

        // 1. בדיקה לפי Input System החדש
        if (Keyboard.current != null)
        {
            if (Keyboard.current.enterKey.wasPressedThisFrame ||
                Keyboard.current.numpadEnterKey.wasPressedThisFrame)
            {
                pressedEnter = true;
            }
        }

        // 2. בדיקה לפי המערכת הישנה (גיבוי, עובד רק אם מוגדר "Both" ב-Active Input Handling)
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            pressedEnter = true;
        }

        if (pressedEnter)
        {
            ResumeGame();
        }
    }

    public void ShowFine(int amount)
    {
        if (notificationText != null)
        {
            notificationText.text = "-" + amount + " Coins!";
            notificationText.color = Color.yellow;

            StopAllCoroutines();
            StartCoroutine(DisplayRoutine());
        }
    }

    public void ShowRedLightPenalty(int amount)
    {
        if (notificationText != null)
        {
            notificationText.text = "You ran a Red Light!\n" +
                                    "Fine: " + amount + " Coins\n" +
                                    "Press ENTER to continue";

            notificationText.color = Color.red;
            notificationText.gameObject.SetActive(true);

            // עוצר את הזמן
            Time.timeScale = 0f;
            isWaitingForInput = true;

            Debug.Log("Game Paused. Waiting for Enter..."); // הודעה לקונסול לבדיקה
        }
    }

    private void ResumeGame()
    {
        Debug.Log("Enter Pressed! Resuming Game...");
        Time.timeScale = 1f;
        isWaitingForInput = false;
        if (notificationText != null)
            notificationText.gameObject.SetActive(false);
    }

    private IEnumerator DisplayRoutine()
    {
        notificationText.gameObject.SetActive(true);
        // משתמשים ב-WaitForSecondsRealtime כדי שזה יעבוד גם אם נרצה להשתמש בזה בזמן שהזמן איטי
        yield return new WaitForSecondsRealtime(displayDuration);
        notificationText.gameObject.SetActive(false);
    }
}