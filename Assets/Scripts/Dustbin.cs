using Unity.Netcode;
using UnityEngine;

public class Dustbin : NetworkBehaviour
{
    [SerializeField] private Transform lid;
    [SerializeField] private float lidOpenAngle = 75f;
    [SerializeField] private float lidOpenTime = 0.5f;

    private Quaternion lidClosedRotation;
    private float lidTimer = 0f;

    private void Awake()
    {
        lidClosedRotation = lid.localRotation;
    }

    public void PlayDiscard()
    {
        PlayDiscardRpc();
    }

    [Rpc(SendTo.Everyone)]
    private void PlayDiscardRpc()
    {
        lidTimer = lidOpenTime;
    }

    private void Update()
    {
        if (lidTimer <= 0f) return;

        lidTimer -= Time.deltaTime;
        float progress = Mathf.Clamp01(1f - lidTimer / lidOpenTime);
        float open = Mathf.Sin(progress * Mathf.PI);
        lid.localRotation = lidClosedRotation * Quaternion.Euler(-lidOpenAngle * open, 0f, 0f);
    }
}
