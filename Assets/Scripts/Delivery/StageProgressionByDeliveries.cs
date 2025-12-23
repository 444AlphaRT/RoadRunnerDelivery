using UnityEngine;
using UnityEngine.SceneManagement;

public class StageProgressionByDeliveries : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerController player;

    [Header("Progression")]
    [Tooltip("How many deliveries are required in THIS stage to advance.")]
    [SerializeField] private int deliveriesToAdvance = 5;

    [Tooltip("Next scene to load. Leave empty if this is the final stage.")]
    [SerializeField] private string nextSceneName;

    private bool advanced = false;

    private void Start()
    {
        if (player == null)
            player = FindFirstObjectByType<PlayerController>();

        if (player == null)
            Debug.LogError("StageProgressionByDeliveries: PlayerController not found!");
    }

    private void Update()
    {
        if (advanced) return;
        if (player == null) return;

        if (player.deliveriesCompleted >= deliveriesToAdvance)
        {
            advanced = true;

            // Reset deliveries so next stage starts clean
            player.deliveriesCompleted = 0;

            // Make sure time is running
            Time.timeScale = 1f;

            if (!string.IsNullOrEmpty(nextSceneName))
                SceneManager.LoadScene(nextSceneName);
        }
    }
}