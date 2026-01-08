using System;
using UnityEngine;

[RequireComponent(typeof(EdgeCollider2D))]
public class StopLineTrigger : MonoBehaviour
{
    /// <summary>
    /// Event fired when the player crosses the stop line trigger.
    /// </summary>
    public event Action<Collider2D> PlayerCrossed;

    private EdgeCollider2D edge;

    private void Awake()
    {
        edge = GetComponent<EdgeCollider2D>();
        edge.isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        PlayerCrossed?.Invoke(other);
    }
}
