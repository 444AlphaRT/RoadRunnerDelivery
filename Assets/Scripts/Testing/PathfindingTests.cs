using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public class WayPointNetworkTests
{
    private GameObject _networkGO;
    private WayPointNetwork _network;

    [SetUp]
    public void SetUp()
    {
        _networkGO = new GameObject("WayPointNetworkTestObject");
        _network = _networkGO.AddComponent<WayPointNetwork>();
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(_networkGO);
    }

    [Test]
    public void BuildNetwork_ConnectsCloseWaypointsWithoutObstacles()
    {
        // Arrange: two waypoints close enough to be neighbors
        var aGO = new GameObject("A");
        var bGO = new GameObject("B");
        var a = aGO.AddComponent<Waypoint>();
        var b = bGO.AddComponent<Waypoint>();

        a.transform.position = Vector3.zero;
        b.transform.position = new Vector3(3f, 0f, 0f); // within maxNeighborDistance = 5

        _network.Waypoints.Add(a);
        _network.Waypoints.Add(b);

        // Act
        _network.BuildNetwork();

        // Assert
        Assert.IsTrue(a.neighbors.Contains(b), "A should have B as neighbor when close enough");
        Assert.IsTrue(b.neighbors.Contains(a), "B should have A as neighbor when close enough");

        Object.DestroyImmediate(aGO);
        Object.DestroyImmediate(bGO);
    }

    [Test]
    public void BuildNetwork_DoesNotConnectDistantWaypoints()
    {
        // Arrange: two waypoints too far from each other
        var aGO = new GameObject("A");
        var bGO = new GameObject("B");
        var a = aGO.AddComponent<Waypoint>();
        var b = bGO.AddComponent<Waypoint>();

        a.transform.position = Vector3.zero;
        b.transform.position = new Vector3(10f, 0f, 0f); // farther than maxNeighborDistance = 5

        _network.Waypoints.Add(a);
        _network.Waypoints.Add(b);

        // Act
        _network.BuildNetwork();

        // Assert
        Assert.IsFalse(a.neighbors.Contains(b), "A should NOT have B as neighbor when distance is too large");
        Assert.IsFalse(b.neighbors.Contains(a), "B should NOT have A as neighbor when distance is too large");

        Object.DestroyImmediate(aGO);
        Object.DestroyImmediate(bGO);
    }

    [Test]
    public void FindClosest_ReturnsNearestWaypoint()
    {
        // Arrange: three waypoints around origin
        var aGO = new GameObject("A");
        var bGO = new GameObject("B");
        var cGO = new GameObject("C");

        var a = aGO.AddComponent<Waypoint>();
        var b = bGO.AddComponent<Waypoint>();
        var c = cGO.AddComponent<Waypoint>();

        a.transform.position = new Vector3(-5f, 0f, 0f);
        b.transform.position = new Vector3(1f, 0f, 0f);   // this should be the closest
        c.transform.position = new Vector3(10f, 0f, 0f);

        _network.Waypoints.Add(a);
        _network.Waypoints.Add(b);
        _network.Waypoints.Add(c);

        Vector3 queryPosition = Vector3.zero;

        // Act
        Waypoint closest = _network.FindClosest(queryPosition);

        // Assert
        Assert.AreEqual(b, closest, "FindClosest should return the nearest waypoint to the given position");

        Object.DestroyImmediate(aGO);
        Object.DestroyImmediate(bGO);
        Object.DestroyImmediate(cGO);
    }
}

public class AStarPathfinderTests
{
    private GameObject _go;
    private WayPointNetwork _network;
    private AStarPathfinder _pathfinder;

    [SetUp]
    public void SetUp()
    {
        _go = new GameObject("PathfinderTestObject");
        _network = _go.AddComponent<WayPointNetwork>();
        _pathfinder = _go.AddComponent<AStarPathfinder>();

        // Assign the private "network" field via reflection
        FieldInfo networkField = typeof(AStarPathfinder)
            .GetField("network", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.IsNotNull(networkField, "Could not find 'network' field via reflection on AStarPathfinder");
        networkField.SetValue(_pathfinder, _network);
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(_go);
    }

    [Test]
    public void FindPath_ReturnsNull_WhenNetworkIsNull()
    {
        // Arrange
        FieldInfo networkField = typeof(AStarPathfinder)
            .GetField("network", BindingFlags.NonPublic | BindingFlags.Instance);
        networkField.SetValue(_pathfinder, null);

        var dummyStart = new GameObject("DummyStart").AddComponent<Waypoint>();
        var dummyGoal = new GameObject("DummyGoal").AddComponent<Waypoint>();

        // Act
        List<Waypoint> path = _pathfinder.FindPath(dummyStart, dummyGoal);

        // Assert
        Assert.IsNull(path, "FindPath should return null when network is not assigned");

        Object.DestroyImmediate(dummyStart.gameObject);
        Object.DestroyImmediate(dummyGoal.gameObject);
    }

    [Test]
    public void FindPath_ReturnsNull_WhenStartOrGoalIsNull()
    {
        // Arrange: network is assigned in SetUp

        // Act
        List<Waypoint> pathWithNullStart = _pathfinder.FindPath(null, null);

        // Assert
        Assert.IsNull(pathWithNullStart, "FindPath should return null when start and goal are null");
    }

    [Test]
    public void FindPath_FindsSimpleThreeNodePath()
    {
        // Arrange: create a simple chain: start -> mid -> goal
        var startGO = new GameObject("Start");
        var midGO = new GameObject("Mid");
        var goalGO = new GameObject("Goal");

        var start = startGO.AddComponent<Waypoint>();
        var mid = midGO.AddComponent<Waypoint>();
        var goal = goalGO.AddComponent<Waypoint>();

        start.transform.position = new Vector3(0f, 0f, 0f);
        mid.transform.position = new Vector3(1f, 0f, 0f);
        goal.transform.position = new Vector3(2f, 0f, 0f);

        // Manually wire neighbors
        start.neighbors.Add(mid);
        mid.neighbors.Add(start);
        mid.neighbors.Add(goal);
        goal.neighbors.Add(mid);

        // Add waypoints to network so A* can initialize gScore/fScore
        _network.Waypoints.Add(start);
        _network.Waypoints.Add(mid);
        _network.Waypoints.Add(goal);

        // Act
        List<Waypoint> path = _pathfinder.FindPath(start, goal);

        // Assert
        Assert.IsNotNull(path, "Path should not be null when a route exists");
        Assert.AreEqual(3, path.Count, "Path should contain three waypoints: start -> mid -> goal");
        Assert.AreEqual(start, path[0], "First waypoint in path should be start");
        Assert.AreEqual(mid, path[1], "Second waypoint in path should be mid");
        Assert.AreEqual(goal, path[2], "Last waypoint in path should be goal");

        Object.DestroyImmediate(startGO);
        Object.DestroyImmediate(midGO);
        Object.DestroyImmediate(goalGO);
    }

    [Test]
    public void FindPath_ReturnsNull_WhenNoConnectionBetweenStartAndGoal()
    {
        // Arrange: two separate waypoints with no neighbors between them
        var aGO = new GameObject("A");
        var bGO = new GameObject("B");

        var a = aGO.AddComponent<Waypoint>();
        var b = bGO.AddComponent<Waypoint>();

        a.transform.position = new Vector3(0f, 0f, 0f);
        b.transform.position = new Vector3(5f, 0f, 0f);

        _network.Waypoints.Add(a);
        _network.Waypoints.Add(b);

        // No neighbors wired, so there is no path

        // Act
        List<Waypoint> path = _pathfinder.FindPath(a, b);

        // Assert
        Assert.IsNull(path, "FindPath should return null when there is no possible route between start and goal");

        Object.DestroyImmediate(aGO);
        Object.DestroyImmediate(bGO);
    }
}
