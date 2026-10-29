using Unity.Netcode;
using UnityEngine;

public enum FoodType
{
    Raw,
    Snack
}

public class FoodManager : NetworkBehaviour
{
    public FoodType foodType = FoodType.Raw;

    public NetworkVariable<bool> isCooked = new NetworkVariable<bool>(false);
    public NetworkVariable<bool> isBurnt = new NetworkVariable<bool>(false);
    [HideInInspector] public float cookingProgress = 0f;

    private NetworkVariable<NetworkObjectReference> holder = new NetworkVariable<NetworkObjectReference>();
    private Transform followPoint;
    private Rigidbody rb;
    private Collider[] foodColliders;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        foodColliders = GetComponentsInChildren<Collider>(true);
    }

    public override void OnNetworkSpawn()
    {
        holder.OnValueChanged += OnHolderChanged;
        UpdateFollowPoint();
    }

    public override void OnNetworkDespawn()
    {
        holder.OnValueChanged -= OnHolderChanged;
    }

    private void LateUpdate()
    {
        if (followPoint == null) return;

        transform.SetPositionAndRotation(followPoint.position, followPoint.rotation);
    }

    public void SetHolder(NetworkObject newHolder)
    {
        if (newHolder == null)
        {
            holder.Value = new NetworkObjectReference();
        }
        else
        {
            holder.Value = newHolder;
        }

        UpdateFollowPoint();
    }

    public bool HasHolder()
    {
        NetworkObject holderObject;
        return holder.Value.TryGet(out holderObject);
    }

    public void AddImpulse(Vector3 force)
    {
        if (!IsServer || HasHolder()) return;

        rb.AddForce(force, ForceMode.Impulse);
    }

    private void OnHolderChanged(NetworkObjectReference oldHolder, NetworkObjectReference newHolder)
    {
        UpdateFollowPoint();
    }

    private void UpdateFollowPoint()
    {
        followPoint = null;

        NetworkObject holderObject;
        if (holder.Value.TryGet(out holderObject))
        {
            PlayerInteraction player = holderObject.GetComponent<PlayerInteraction>();
            if (player != null)
            {
                followPoint = player.GetHoldPoint();
            }
            else
            {
                followPoint = holderObject.transform;
            }

            transform.SetPositionAndRotation(followPoint.position, followPoint.rotation);
        }

        SetPhysics(followPoint == null);
    }

    private void SetPhysics(bool isFree)
    {
        foreach (Collider col in foodColliders)
        {
            col.enabled = isFree;
        }

        if (IsServer && isFree)
        {
            rb.isKinematic = false;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
        }
        else
        {
            if (!rb.isKinematic)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
            rb.isKinematic = true;
            rb.interpolation = RigidbodyInterpolation.None;
        }

        rb.detectCollisions = isFree;
    }
}
