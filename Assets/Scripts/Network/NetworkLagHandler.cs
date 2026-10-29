using System;
using Unity.Netcode;
using UnityEngine;

public class NetworkLagHandler : MonoBehaviour
{
    public static NetworkLagHandler Instance;

    [SerializeField] private float maxLagCompensation = 0.3f;
    [SerializeField] private float reachTolerance = 0.5f;

    private NetworkManager networkManager;

    private void Awake()
    {
        Instance = this;
        networkManager = GetComponent<NetworkManager>();
    }

    public float GetRoundTripTime(ulong clientId)
    {
        if (!networkManager.IsListening) return 0f;
        if (networkManager.IsServer && clientId == NetworkManager.ServerClientId) return 0f;

        return networkManager.NetworkConfig.NetworkTransport.GetCurrentRtt(clientId) / 1000f;
    }

    public int GetLocalPing()
    {
        if (!networkManager.IsListening || networkManager.IsServer) return 0;

        return (int)networkManager.NetworkConfig.NetworkTransport.GetCurrentRtt(NetworkManager.ServerClientId);
    }

    public double GetFairPressTime(ulong clientId, double pressTime, double receivedTime)
    {
        double compensation = Mathf.Min(GetRoundTripTime(clientId), maxLagCompensation);
        return Math.Clamp(pressTime, receivedTime - compensation, receivedTime);
    }

    public float GetAllowedReach(ulong clientId, float baseReach, float moveSpeed)
    {
        float lagTime = Mathf.Min(GetRoundTripTime(clientId), maxLagCompensation);
        return baseReach + reachTolerance + moveSpeed * lagTime;
    }
}
