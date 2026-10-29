using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public enum InteractionType
{
    Interact,
    Eat,
    Drop
}

public class InteractionRequest
{
    public PlayerInteraction player;
    public InteractionType type;
    public NetworkObjectReference target;
    public double pressTime;
}

public class NetworkDecisionMaker : MonoBehaviour
{
    public static NetworkDecisionMaker Instance;

    [SerializeField] private float decisionWindow = 0.06f;

    private List<InteractionRequest> pendingRequests = new List<InteractionRequest>();
    private List<NetworkObject> claimedTargets = new List<NetworkObject>();
    private List<PlayerInteraction> claimedByPlayers = new List<PlayerInteraction>();
    private double firstRequestTime;

    private void Awake()
    {
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public void AddRequest(PlayerInteraction player, InteractionType type, NetworkObjectReference target, double pressTime)
    {
        double receivedTime = NetworkManager.Singleton.ServerTime.Time;

        InteractionRequest request = new InteractionRequest();
        request.player = player;
        request.type = type;
        request.target = target;
        request.pressTime = NetworkLagHandler.Instance.GetFairPressTime(player.OwnerClientId, pressTime, receivedTime);

        if (pendingRequests.Count == 0)
        {
            firstRequestTime = receivedTime;
        }

        pendingRequests.Add(request);
    }

    private void Update()
    {
        if (pendingRequests.Count == 0) return;

        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsServer)
        {
            pendingRequests.Clear();
            return;
        }

        if (NetworkManager.Singleton.ServerTime.Time - firstRequestTime < decisionWindow) return;

        MakeDecisions();
    }

    private void MakeDecisions()
    {
        pendingRequests.Sort(CompareByPressTime);
        claimedTargets.Clear();
        claimedByPlayers.Clear();

        foreach (InteractionRequest request in pendingRequests)
        {
            if (request.player == null || !request.player.IsSpawned) continue;

            PlayerHungerManager hungerManager = request.player.GetComponent<PlayerHungerManager>();
            if (hungerManager.isDead.Value) continue;

            NetworkObject target;
            request.target.TryGet(out target);

            int claimedIndex = -1;
            if (target != null)
            {
                claimedIndex = claimedTargets.IndexOf(target);
            }

            if (claimedIndex >= 0 && claimedByPlayers[claimedIndex] != request.player)
            {
                string winnerName = claimedByPlayers[claimedIndex].GetPlayerName();
                request.player.ShowMessage("Too slow! " + winnerName + " got there first.");
                Debug.Log("Decision: " + winnerName + " beat " + request.player.GetPlayerName() + " to " + target.name);
                continue;
            }

            bool success = request.player.ExecuteRequest(request.type, target);

            if (success && target != null)
            {
                claimedTargets.Add(target);
                claimedByPlayers.Add(request.player);
            }
        }

        pendingRequests.Clear();
    }

    private int CompareByPressTime(InteractionRequest first, InteractionRequest second)
    {
        return first.pressTime.CompareTo(second.pressTime);
    }
}
