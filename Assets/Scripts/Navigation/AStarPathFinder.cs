using System.Collections.Generic;
using UnityEngine;

public class AStarPathfinder : MonoBehaviour
{
    [SerializeField]
    private WayPointNetwork network;  // assign in Inspector

    public List<Waypoint> FindPath(Waypoint start, Waypoint goal)
    {
        if (network == null)
        {
            Debug.LogWarning("AStarPathfinder: network is not assigned.");
            return null;
        }

        if (start == null || goal == null)
        {
            Debug.LogWarning("AStarPathfinder: start or goal is null.");
            return null;
        }

        var openSet = new List<Waypoint> { start };
        var cameFrom = new Dictionary<Waypoint, Waypoint>();
        var gScore = new Dictionary<Waypoint, float>();
        var fScore = new Dictionary<Waypoint, float>();

        foreach (Waypoint wp in network.Waypoints)
        {
            if (wp == null)
            {
                continue;
            }

            gScore[wp] = Mathf.Infinity;
            fScore[wp] = Mathf.Infinity;
        }

        gScore[start] = 0f;
        fScore[start] = Heuristic(start, goal);

        while (openSet.Count > 0)
        {
            Waypoint current = GetLowestFScore(openSet, fScore);

            if (current == goal)
            {
                return ReconstructPath(cameFrom, current);
            }

            openSet.Remove(current);

            foreach (Waypoint neighbor in current.neighbors)
            {
                if (neighbor == null)
                {
                    continue;
                }

                float stepCost = Vector2.Distance(
                    current.transform.position,
                    neighbor.transform.position
                );

                float tentativeG = gScore[current] + stepCost;

                if (!gScore.ContainsKey(neighbor) || tentativeG < gScore[neighbor])
                {
                    cameFrom[neighbor] = current;
                    gScore[neighbor] = tentativeG;
                    fScore[neighbor] = tentativeG + Heuristic(neighbor, goal);

                    if (!openSet.Contains(neighbor))
                    {
                        openSet.Add(neighbor);
                    }
                }
            }
        }

        Debug.LogWarning("AStarPathfinder: no path found.");
        return null;
    }

    private float Heuristic(Waypoint a, Waypoint b)
    {
        return Vector2.Distance(a.transform.position, b.transform.position);
    }

    private Waypoint GetLowestFScore(List<Waypoint> openSet, Dictionary<Waypoint, float> fScore)
    {
        Waypoint best = openSet[0];

        foreach (Waypoint wp in openSet)
        {
            if (!fScore.ContainsKey(wp))
            {
                continue;
            }

            if (!fScore.ContainsKey(best) || fScore[wp] < fScore[best])
            {
                best = wp;
            }
        }

        return best;
    }

    private List<Waypoint> ReconstructPath(Dictionary<Waypoint, Waypoint> cameFrom, Waypoint current)
    {
        var path = new List<Waypoint>();
        path.Add(current);

        while (cameFrom.ContainsKey(current))
        {
            current = cameFrom[current];
            path.Add(current);
        }

        path.Reverse();
        return path;
    }
}