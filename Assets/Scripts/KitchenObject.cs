using UnityEngine;

public class KitchenObject : HoldableItem
{
    public string objectName = "Frying Pan";
    [SerializeField] private float hitRadius = 0.6f;
    [SerializeField] private float playerHeight = 2f;
    [SerializeField] private float maxFlightTime = 3f;
    [SerializeField] private float spinSpeed = 12f;
    [SerializeField] private float bounceSpeed = 2f;
    [SerializeField] private float landingSlowdown = 0.3f;

    private PlayerCombat thrower;
    private bool isFlying = false;
    private float flightEndTime = 0f;

    public void Throw(PlayerCombat throwingPlayer, Vector3 startPosition, Vector3 velocity)
    {
        thrower = throwingPlayer;
        isFlying = true;
        flightEndTime = Time.time + maxFlightTime;

        Vector3 spinAxis = Vector3.Cross(Vector3.up, velocity).normalized;
        Launch(startPosition, velocity, spinAxis * spinSpeed);
    }

    public bool IsFlying()
    {
        return isFlying;
    }

    private void FixedUpdate()
    {
        if (!isFlying) return;

        if (!IsServer || HasHolder() || Time.time > flightEndTime)
        {
            isFlying = false;
            return;
        }

        PlayerCombat target = FindHitPlayer();
        if (target == null) return;

        isFlying = false;

        Vector3 hitDirection = GetVelocity();
        hitDirection.y = 0f;
        hitDirection.Normalize();

        target.ReceiveThrownHit(hitDirection, GetMass(), transform.position);
        SetVelocity(-hitDirection * bounceSpeed + Vector3.up * bounceSpeed);
    }

    private PlayerCombat FindHitPlayer()
    {
        foreach (PlayerHungerManager player in PlayerHungerManager.allPlayers)
        {
            if (player == null || player.isDead.Value) continue;

            PlayerCombat playerCombat = player.GetComponent<PlayerCombat>();
            if (playerCombat == thrower) continue;

            Vector3 offset = transform.position - player.transform.position;
            if (offset.y < 0f || offset.y > playerHeight) continue;

            offset.y = 0f;
            if (offset.magnitude <= hitRadius)
            {
                return playerCombat;
            }
        }

        return null;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!IsServer || !isFlying) return;

        isFlying = false;
        SetVelocity(GetVelocity() * landingSlowdown);
    }
}
