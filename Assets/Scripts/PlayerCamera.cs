using UnityEngine;

public class PlayerCamera : MonoBehaviour
{
    [SerializeField] private Vector3 offset = new Vector3(0f, 12.5f, -8.6f);
    [SerializeField] private float followSmoothTime = 0.25f;
    [SerializeField] private Transform audioListener;
    [SerializeField] private float listenerHeight = 1.5f;
    [SerializeField] private float shakeFrequency = 22f;
    [SerializeField] private Vector2 kitchenMin = new Vector2(-7.7f, -5.5f);
    [SerializeField] private Vector2 kitchenMax = new Vector2(7.7f, 5.5f);
    [SerializeField] private float wallHeight = 4.5f;

    private Camera cam;
    private Transform target;
    private Vector3 followVelocity;
    private Vector3 followPosition;
    private float shakeStrength = 0f;
    private float shakeDuration = 0f;
    private float shakeTimer = 0f;
    private float zoom = 1f;

    private void Awake()
    {
        cam = GetComponent<Camera>();
    }

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
        followPosition = ClampToKitchen(target.position + offset);
        transform.position = followPosition;
    }

    public void SetZoom(float newZoom)
    {
        zoom = newZoom;
    }

    public void Shake(float strength, float duration)
    {
        if (shakeTimer > 0f && strength < shakeStrength * (shakeTimer / shakeDuration)) return;

        shakeStrength = strength;
        shakeDuration = duration;
        shakeTimer = duration;
    }

    private void LateUpdate()
    {
        if (target == null) return;

        Vector3 targetPosition = ClampToKitchen(target.position + offset * zoom);
        followPosition =Vector3.SmoothDamp(followPosition, targetPosition, ref followVelocity, followSmoothTime);
        transform.position = followPosition + GetShakeOffset();

        if (audioListener != null)
        {
            audioListener.SetPositionAndRotation(target.position + Vector3.up * listenerHeight, transform.rotation);
        }
    }

    private Vector3 ClampToKitchen(Vector3 position)
    {
        Vector3 bottomLeft = GetPointOnHeight(new Vector3(0f, 0f, 0f), 0f, position);
        Vector3 bottomRight = GetPointOnHeight(new Vector3(1f, 0f, 0f), 0f, position);
        Vector3 topLeft = GetPointOnHeight(new Vector3(0f, 1f, 0f), wallHeight, position);
        Vector3 topRight = GetPointOnHeight(new Vector3(1f, 1f, 0f), wallHeight, position);

        float viewMinX = Mathf.Min(bottomLeft.x, topLeft.x);
        float viewMaxX = Mathf.Max(bottomRight.x, topRight.x);
        float viewMinZ = Mathf.Min(bottomLeft.z, bottomRight.z);
        float viewMaxZ = Mathf.Max(topLeft.z, topRight.z);

        position.x += GetClampShift(viewMinX, viewMaxX, kitchenMin.x, kitchenMax.x);
        position.z += GetClampShift(viewMinZ, viewMaxZ, kitchenMin.y, kitchenMax.y);
        return position;
    }

    private Vector3 GetPointOnHeight(Vector3 viewportPoint, float height, Vector3 cameraPosition)
    {
        Vector3 direction = cam.ViewportPointToRay(viewportPoint).direction;
        if (direction.y > -0.01f)
        {
            direction.y = -0.01f;
        }

        float distance = (height - cameraPosition.y) / direction.y;
        return cameraPosition + direction * distance;
    }

    private float GetClampShift(float viewMin, float viewMax, float boundsMin, float boundsMax)
    {
        if (viewMax - viewMin >= boundsMax - boundsMin)
        {
            return (boundsMin + boundsMax) * 0.5f - (viewMin + viewMax) * 0.5f;
        }

        if (viewMin < boundsMin)
        {
            return boundsMin - viewMin;
        }

        if (viewMax > boundsMax)
        {
            return boundsMax - viewMax;
        }

        return 0f;
    }

    private Vector3 GetShakeOffset()
    {
        if (shakeTimer <= 0f) return Vector3.zero;

        shakeTimer -= Time.deltaTime;
        float fade = Mathf.Clamp01(shakeTimer / shakeDuration);
        float time = Time.time * shakeFrequency;
        float shakeX = Mathf.PerlinNoise(time, 0f) * 2f - 1f;
        float shakeY = Mathf.PerlinNoise(0f, time) * 2f - 1f;

        return (transform.right * shakeX + transform.up * shakeY) * shakeStrength * fade;
    }
}
