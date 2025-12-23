using UnityEngine;
using UnityEngine.SceneManagement;

public class GameOverUI : MonoBehaviour
{
    [SerializeField] private GameObject root; // drag GameOverRoot here
    [SerializeField] private string stageSelectSceneName = "MainManu"; 

    private void Awake()
    {
        Hide();
    }

    public void Show()
    {
        if (root != null) root.SetActive(true);
        Time.timeScale = 0f; // freeze the game
    }

    public void Hide()
    {
        Time.timeScale = 1f;
        if (root != null) root.SetActive(false);
    }

    // Button: TRY AGAIN
    public void TryAgain()
    {
        Time.timeScale = 1f;

        // Reset money ONLY here
        if (MoneyManager.Instance != null)
            MoneyManager.Instance.ResetToDefaults();

        SceneManager.LoadScene(stageSelectSceneName);
    }
}