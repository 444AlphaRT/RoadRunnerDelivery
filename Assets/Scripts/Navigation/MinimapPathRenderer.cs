using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class MinimapPathRenderer : MonoBehaviour
{
    // A* pathfinder and waypoint network references
    [SerializeField] private AStarPathfinder pathfinder;
    [SerializeField] private WayPointNetwork network;

    // Player position for starting point
    [SerializeField] private Transform player;

    // Targets for navigation
    [SerializeField] private Transform pickupPoint;
    [SerializeField] private Transform dropoffPoint;

    // To check if the player currently holds a package
    [SerializeField] private PlayerController playerController;

    // Optional: auto-update path every frame
    [SerializeField] private bool updateEveryFrame = true;

    private LineRenderer line;

    private void Awake()
    {
        // Cache LineRenderer and ensure it draws in world space
        line = GetComponent<LineRenderer>();
        line.useWorldSpace = true;
    }

    private void Update()
    {
        if (updateEveryFrame)
        {
            UpdatePath();
        }
    }

    // Called when delivery point changes (after a delivery)
    public void SetDropoffTarget(Transform newTarget)
    {
        dropoffPoint = newTarget;
        UpdatePath();
    }

    // Core function: builds the route line on minimap
    public void UpdatePath()
    {
        // Safety check: missing references → hide path
        if (pathfinder == null || network == null || line == null ||
            player == null || playerController == null)
        {
            line.positionCount = 0;
            return;
        }

        // Decide which target to guide to
        // If player has no package → go to pickup
        // If holding a package → go to drop-off
        Transform currentTarget = playerController.HasPackage ? dropoffPoint : pickupPoint;

        if (currentTarget == null)
        {
            line.positionCount = 0;
            return;
        }

        // Find closest waypoints to player and target
        Waypoint start = network.FindClosest(player.position);
        Waypoint goal  = network.FindClosest(currentTarget.position);

        if (start == null || goal == null)
        {
            line.positionCount = 0;
            return;
        }

        // Run A* to compute shortest route
        List<Waypoint> path = pathfinder.FindPath(start, goal);

        // If no valid path → hide line
        if (path == null || path.Count == 0)
        {
            line.positionCount = 0;
            return;
        }

        // Render the path on the minimap
        line.positionCount = path.Count;

        for (int i = 0; i < path.Count; i++)
        {
            line.SetPosition(i, path[i].transform.position);
        }
    }
}