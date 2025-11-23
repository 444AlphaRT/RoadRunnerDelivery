using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("Target to follow")]
    public Transform target;          

    [Header("Follow settings")]
    public float smoothSpeed = 5f;    

    private Vector3 offset;

    private void Start()
    {
        if (target == null)
        {
            Debug.LogError("CameraFollow: No target assigned!");
            return;
        }

        offset = transform.position - target.position;
    }

    private void LateUpdate()
    {
        if (target == null) return;

        Vector3 desiredPosition = target.position + offset;
        Vector3 smoothedPosition = Vector3.Lerp(
            transform.position,
            desiredPosition,
            smoothSpeed * Time.deltaTime
        );

        transform.position = smoothedPosition;
    }
}