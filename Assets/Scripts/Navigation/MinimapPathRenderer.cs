using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class MinimapPathRenderer : MonoBehaviour
{
    [SerializeField] private AStarPathfinder pathfinder;
    [SerializeField] private WayPointNetwork network;
    [SerializeField] private Transform player;
    [SerializeField] private Transform pickupPoint;
    [SerializeField] private Transform dropoffPoint;
    [SerializeField] private PlayerController playerController;
    [SerializeField] private bool updateEveryFrame = true;

    [Header("Line Colors")]
    [SerializeField] private Color noPackageColor = Color.magenta;
    [SerializeField] private Color hasPackageColor = Color.cyan;

    private LineRenderer line;

    private void Awake()
    {
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

    public void SetDropoffTarget(Transform newTarget)
    {
        dropoffPoint = newTarget;
        UpdatePath();
    }

    public void UpdatePath()
    {
        if (line == null)
            return;

        if (pathfinder == null || network == null || player == null || playerController == null)
        {
            line.positionCount = 0;
            return;
        }

        Transform currentTarget = playerController.HasPackage ? dropoffPoint : pickupPoint;

        if (currentTarget == null)
        {
            line.positionCount = 0;
            return;
        }

        Color targetColor = playerController.HasPackage ? hasPackageColor : noPackageColor;
        line.startColor = targetColor;
        line.endColor = targetColor;

        Waypoint start = network.FindClosest(player.position);
        Waypoint goal = network.FindClosest(currentTarget.position);

        if (start == null || goal == null)
        {
            line.positionCount = 0;
            return;
        }

        List<Waypoint> path = pathfinder.FindPath(start, goal);

        if (path == null || path.Count == 0)
        {
            line.positionCount = 0;
            return;
        }

        line.positionCount = path.Count;
        for (int i = 0; i < path.Count; i++)
        {
            line.SetPosition(i, path[i].transform.position);
        }
    }
}