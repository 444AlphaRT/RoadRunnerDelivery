using UnityEngine;

public class RandomDropoffLocation : MonoBehaviour
{
    [Header("All possible dropoff spots")]
    public Transform[] dropoffSpots;

    private void Start()
    {
        MoveToRandomSpot();
    }

    public void MoveToRandomSpot()
    {
        if (dropoffSpots == null || dropoffSpots.Length == 0)
        {
            Debug.LogError("RandomDropoffLocation: No dropoff spots assigned!");
            return;
        }

        int index = Random.Range(0, dropoffSpots.Length);
        Transform chosenSpot = dropoffSpots[index];

        transform.position = chosenSpot.position;
    }
}