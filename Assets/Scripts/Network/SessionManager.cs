using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

public class SessionManager : NetworkBehaviour
{
    public static SessionManager Instance;

    [SerializeField] private int maxPlayers = 4;

    public NetworkList<PlayerSessionData> players;
    public NetworkVariable<bool> isGameStarted = new NetworkVariable<bool>(false);

    private void Awake()
    {
        players = new NetworkList<PlayerSessionData>();
    }

    public override void OnNetworkSpawn()
    {
        Instance = this;

        if (IsServer)
        {
            NetworkManager.OnClientConnectedCallback += OnClientConnected;
            NetworkManager.OnClientDisconnectCallback += OnClientDisconnected;

            foreach (ulong clientId in NetworkManager.ConnectedClientsIds)
            {
                AddPlayer(clientId);
            }
        }
    }

    public override void OnNetworkDespawn()
    {
        if (Instance == this)
        {
            Instance = null;
        }

        if (IsServer && NetworkManager != null)
        {
            NetworkManager.OnClientConnectedCallback -= OnClientConnected;
            NetworkManager.OnClientDisconnectCallback -= OnClientDisconnected;
        }
    }

    private void OnClientConnected(ulong clientId)
    {
        AddPlayer(clientId);
    }

    private void OnClientDisconnected(ulong clientId)
    {
        for (int i = players.Count - 1; i >= 0; i--)
        {
            if (players[i].clientId == clientId)
            {
                players.RemoveAt(i);
            }
        }
    }

    private void AddPlayer(ulong clientId)
    {
        PlayerSessionData existingData;
        if (TryGetPlayerData(clientId, out existingData)) return;

        PlayerSessionData data = new PlayerSessionData();
        data.clientId = clientId;
        data.playerIndex = GetFreePlayerIndex();
        data.playerName = new FixedString32Bytes();
        data.playerName.CopyFromTruncated(NetworkConnectionManager.Instance.GetPlayerName(clientId));

        players.Add(data);
        Debug.Log(data.playerName + " joined as player " + (data.playerIndex + 1));
    }

    private int GetFreePlayerIndex()
    {
        for (int index = 0; index < maxPlayers; index++)
        {
            bool isTaken = false;
            for (int i = 0; i < players.Count; i++)
            {
                if (players[i].playerIndex == index)
                {
                    isTaken = true;
                }
            }

            if (!isTaken) return index;
        }

        return players.Count;
    }

    public bool TryGetPlayerData(ulong clientId, out PlayerSessionData data)
    {
        for (int i = 0; i < players.Count; i++)
        {
            if (players[i].clientId == clientId)
            {
                data = players[i];
                return true;
            }
        }

        data = new PlayerSessionData();
        return false;
    }

    public bool TryGetPlayerDataByIndex(int playerIndex, out PlayerSessionData data)
    {
        for (int i = 0; i < players.Count; i++)
        {
            if (players[i].playerIndex == playerIndex)
            {
                data = players[i];
                return true;
            }
        }

        data = new PlayerSessionData();
        return false;
    }
}
