using System.Collections.Generic;
using UnityEngine;

public class WayPointNetwork : MonoBehaviour
{
    [Header("All waypoints in the scene")]
    [SerializeField]
    private List<Waypoint> waypoints = new List<Waypoint>();

    [Header("Connection settings")]
    [SerializeField]
    private float maxNeighborDistance = 5f;   // set this in the Inspector

    [Header("Obstacle settings")]
    [SerializeField]
    private LayerMask obstacleMask;           // Mapcolliders layer

    public List<Waypoint> Waypoints => waypoints;

    private void Awake()
    {
        BuildNetwork();
    }

    public void BuildNetwork()
    {
        // clear old neighbors
        foreach (Waypoint waypoint in waypoints)
        {
            if (waypoint != null)
            {
                waypoint.neighbors.Clear();
            }
        }

        int count = waypoints.Count;

        for (int i = 0; i < count; i++)
        {
            Waypoint a = waypoints[i];
            if (a == null)
            {
                continue;
            }

            for (int j = i + 1; j < count; j++)
            {
                Waypoint b = waypoints[j];
                if (b == null)
                {
                    continue;
                }

                float distance = Vector2.Distance(
                    a.transform.position,
                    b.transform.position
                );

                if (distance > maxNeighborDistance)
                {
                    continue;
                }

                // do not connect if there is an obstacle between a and b
                bool blocked = Physics2D.Linecast(
                    a.transform.position,
                    b.transform.position,
                    obstacleMask
                );

                if (blocked)
                {
                    continue;
                }

                a.neighbors.Add(b);
                b.neighbors.Add(a);
            }
        }

        Debug.Log("WayPointNetwork: built network for " + count + " waypoints.");
    }

    public Waypoint FindClosest(Vector3 position)
    {
        Waypoint closest = null;
        float bestDistance = float.MaxValue;

        foreach (Waypoint waypoint in waypoints)
        {
            if (waypoint == null)
            {
                continue;
            }

            float distance = Vector2.Distance(
                position,
                waypoint.transform.position
            );

            if (distance < bestDistance)
            {
                bestDistance = distance;
                closest = waypoint;
            }
        }

        return closest;
    }
}