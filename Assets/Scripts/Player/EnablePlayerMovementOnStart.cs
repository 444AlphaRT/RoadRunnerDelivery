using UnityEngine;
using UnityEngine.SceneManagement;

public class EnablePlayerMovementOnStart : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerController player;

    [Header("Tutorial Scene Name")]
    [SerializeField] private string tutorialSceneName = "Carloop"; // change to your tutorial scene name

    private void Start()
    {
        if (player == null)
            player = FindFirstObjectByType<PlayerController>();

        if (player == null)
        {
            Debug.LogWarning("EnablePlayerMovementOnStart: PlayerController not found.");
            return;
        }

        string scene = SceneManager.GetActiveScene().name;

        // In tutorial -> keep movement disabled (intro controls it)
        if (scene == tutorialSceneName)
            return;

        // In all other stages -> enable movement immediately
        player.canMove = true;
    }
}