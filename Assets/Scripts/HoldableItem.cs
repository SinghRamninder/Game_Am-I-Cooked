using Unity.Netcode;
using UnityEngine;

public class HoldableItem : NetworkBehaviour
{
    private NetworkVariable<NetworkObjectReference> holder = new NetworkVariable<NetworkObjectReference>();
    private Transform followPoint;
    private Rigidbody rb;
    private Collider[] itemColliders;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        itemColliders = GetComponentsInChildren<Collider>(true);
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

    public float GetMass()
    {
        return rb.mass;
    }

    public void AddImpulse(Vector3 force)
    {
        if (!IsServer || HasHolder()) return;

        rb.AddForce(force, ForceMode.Impulse);
    }

    public void Launch(Vector3 startPosition, Vector3 velocity, Vector3 spin)
    {
        if (!IsServer || HasHolder()) return;

        rb.position = startPosition;
        transform.position = startPosition;
        rb.linearVelocity = velocity;
        rb.angularVelocity = spin;
    }

    public Vector3 GetVelocity()
    {
        return rb.linearVelocity;
    }

    public void SetVelocity(Vector3 velocity)
    {
        if (!IsServer || HasHolder()) return;

        rb.linearVelocity = velocity;
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
        foreach (Collider col in itemColliders)
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
