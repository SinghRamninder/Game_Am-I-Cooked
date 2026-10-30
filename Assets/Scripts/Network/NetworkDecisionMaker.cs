using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public enum InteractionType
{
    Interact,
    Eat,
    Drop,
    Attack
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
        if (GameOverManager.Instance != null && GameOverManager.Instance.IsGameOver()) return;
        if (IntroSequence.Instance != null && IntroSequence.Instance.IsLocked()) return;

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

            PlayerCombat combat = request.player.GetComponent<PlayerCombat>();
            if (combat.IsStunned()) continue;

            NetworkObject target;
            request.target.TryGet(out target);

            bool canBeClaimed = target != null && request.type == InteractionType.Interact && target.GetComponent<Dustbin>() == null;

            int claimedIndex = -1;
            if (canBeClaimed)
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

            if (success && canBeClaimed)
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
