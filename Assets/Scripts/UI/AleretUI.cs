using System.Collections;
using UnityEngine;
using TMPro;

public class AlertUI : MonoBehaviour
{

    public static AlertUI Instance { get; private set; }

    [Header("UI References")]
    [SerializeField] private GameObject panel;
    [SerializeField] private TextMeshProUGUI messageText;

    [Header("Settings")]
    [SerializeField] private float duration = 2f;

    private Coroutine currentRoutine;

    private void Awake()
    {
        // Singleton setup
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (panel != null)
            panel.SetActive(false);
    }

    public void Show(string message)
    {
        if (panel == null || messageText == null)
        {
            Debug.LogError("AlertUI: panel or messageText is NOT assigned!");
            return;
        }

        panel.SetActive(true);
        messageText.text = message;

        if (currentRoutine != null)
            StopCoroutine(currentRoutine);

        currentRoutine = StartCoroutine(ShowRoutine());
    }

    private IEnumerator ShowRoutine()
    {
        yield return new WaitForSeconds(duration);

        if (panel != null)
            panel.SetActive(false);

        currentRoutine = null;
    }
}
