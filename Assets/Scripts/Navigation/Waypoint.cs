using System.Collections.Generic;
using UnityEngine;

public class Waypoint : MonoBehaviour
{
    [Header("Connected waypoints")]
    public List<Waypoint> neighbors = new List<Waypoint>();

    [SerializeField]
    private float gizmoRadius; // Set this in the Inspector

    private void OnDrawGizmos()
    {
        // Draw the waypoint as a small sphere in the editor
        Gizmos.color = Color.yellow;
        Gizmos.DrawSphere(transform.position, gizmoRadius);

        // Draw lines to neighbors (only for visualization in Scene view)
        if (neighbors == null)
        {
            return;
        }

        Gizmos.color = Color.cyan;
        foreach (Waypoint neighbor in neighbors)
        {
            if (neighbor != null)
            {
                Gizmos.DrawLine(transform.position, neighbor.transform.position);
            }
        }
    }
}