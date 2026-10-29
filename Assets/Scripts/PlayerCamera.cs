using UnityEngine;

public class PlayerCamera : MonoBehaviour
{
    [SerializeField] private Vector3 offset = new Vector3(0f, 12.5f, -8.6f);
    [SerializeField] private float followSmoothTime = 0.25f;

    private Transform target;
    private Vector3 followVelocity;

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
        transform.position = target.position + offset;
    }

    private void LateUpdate()
    {
        if (target == null) return;

        Vector3 targetPosition = target.position + offset;
        transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref followVelocity, followSmoothTime);
    }
}
