using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseManager : MonoBehaviour
{
    public static PauseManager Instance { get; private set; }

    [Header("UI Root (Panel)")]
    [SerializeField] private GameObject pauseRoot;

    [Header("Optional: disable pause in these scenes")]
    [SerializeField] private string[] disablePauseInScenes = new string[] { "MainMenu", "LevelSelect" };

    private bool isPaused;

    public bool IsPaused => isPaused;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (pauseRoot != null)
            pauseRoot.SetActive(false);

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ForceResume();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.P))
        {
            if (IsPauseDisabledInThisScene())
                return;

            TogglePause();
        }
    }

    private bool IsPauseDisabledInThisScene()
    {
        string current = SceneManager.GetActiveScene().name;

        if (disablePauseInScenes == null) return false;

        for (int i = 0; i < disablePauseInScenes.Length; i++)
        {
            if (!string.IsNullOrEmpty(disablePauseInScenes[i]) && disablePauseInScenes[i] == current)
                return true;
        }

        return false;
    }

    public void TogglePause()
    {
        if (isPaused) Resume();
        else Pause();
    }

    public void Pause()
    {
        isPaused = true;
        Time.timeScale = 0f;

        if (pauseRoot != null)
            pauseRoot.SetActive(true);
    }

    public void Resume()
    {
        isPaused = false;
        Time.timeScale = 1f;

        if (pauseRoot != null)
            pauseRoot.SetActive(false);
    }

    public void ForceResume()
    {
        isPaused = false;
        Time.timeScale = 1f;

        if (pauseRoot != null)
            pauseRoot.SetActive(false);
    }

    public void RestartLevel()
    {
        ForceResume();
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void GoToMainMenu()
    {
        ForceResume();
        SceneManager.LoadScene("MainManu");
    }

    public void SetPauseRoot(GameObject newPauseRoot)
    {
        pauseRoot = newPauseRoot;

        if (pauseRoot != null)
            pauseRoot.SetActive(isPaused);
    }
}
