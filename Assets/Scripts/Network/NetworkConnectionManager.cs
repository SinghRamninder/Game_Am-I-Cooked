using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.SceneManagement;

public class NetworkConnectionManager : MonoBehaviour
{
    public static NetworkConnectionManager Instance;

    [SerializeField] private int maxPlayers = 4;
    [SerializeField] private ushort port = 7777;
    [SerializeField] private int maxNameLength = 14;
    [SerializeField] private string menuSceneName = "MainMenu";
    [SerializeField] private GameObject sessionManagerPrefab;

    private NetworkManager networkManager;
    private UnityTransport transport;
    private Dictionary<ulong, string> playerNames = new Dictionary<ulong, string>();
    private string disconnectMessage = "";
    private bool isLeaving = false;

    private void Awake()
    {
        Instance = this;
        networkManager = GetComponent<NetworkManager>();
        transport = GetComponent<UnityTransport>();

        networkManager.ConnectionApprovalCallback = ApproveConnection;
        networkManager.OnClientDisconnectCallback += OnClientDisconnected;
    }

    private void OnDestroy()
    {
        if (networkManager != null)
        {
            networkManager.ConnectionApprovalCallback = null;
            networkManager.OnClientDisconnectCallback -= OnClientDisconnected;
        }

        if (Instance == this)
        {
            Instance = null;
        }
    }

    public bool StartHost(string playerName)
    {
        if (networkManager.IsListening) return false;

        isLeaving = false;
        playerNames.Clear();
        transport.SetConnectionData("127.0.0.1", port, "0.0.0.0");
        networkManager.NetworkConfig.ConnectionData = Encoding.UTF8.GetBytes(playerName);

        if (!networkManager.StartHost())
        {
            return false;
        }

        GameObject sessionManager = Instantiate(sessionManagerPrefab);
        sessionManager.GetComponent<NetworkObject>().Spawn();
        return true;
    }

    public bool StartClient(string ipAddress, string playerName)
    {
        if (networkManager.IsListening) return false;

        isLeaving = false;
        transport.SetConnectionData(ipAddress, port);
        networkManager.NetworkConfig.ConnectionData = Encoding.UTF8.GetBytes(playerName);

        return networkManager.StartClient();
    }

    public void StartGame(string sceneName)
    {
        if (!networkManager.IsServer) return;

        SessionManager.Instance.isGameStarted.Value = true;
        networkManager.SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
    }

    public void LeaveGame()
    {
        isLeaving = true;
        networkManager.Shutdown();

        if (SceneManager.GetActiveScene().name != menuSceneName)
        {
            SceneManager.LoadScene(menuSceneName);
        }
    }

    public string GetPlayerName(ulong clientId)
    {
        string playerName;
        if (playerNames.TryGetValue(clientId, out playerName))
        {
            return playerName;
        }

        return "Player";
    }

    public string ConsumeDisconnectMessage()
    {
        string message = disconnectMessage;
        disconnectMessage = "";
        return message;
    }

    public string GetLocalIPAddress()
    {
        try
        {
            using (Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp))
            {
                socket.Connect("8.8.8.8", 65530);
                IPEndPoint endPoint = (IPEndPoint)socket.LocalEndPoint;
                return endPoint.Address.ToString();
            }
        }
        catch (SocketException)
        {
            foreach (IPAddress address in Dns.GetHostEntry(Dns.GetHostName()).AddressList)
            {
                if (address.AddressFamily == AddressFamily.InterNetwork)
                {
                    return address.ToString();
                }
            }
        }

        return "127.0.0.1";
    }

    private void ApproveConnection(NetworkManager.ConnectionApprovalRequest request, NetworkManager.ConnectionApprovalResponse response)
    {
        response.CreatePlayerObject = false;

        if (playerNames.Count >= maxPlayers)
        {
            response.Approved = false;
            response.Reason = "The kitchen is full (" + maxPlayers + "/" + maxPlayers + " players).";
            return;
        }

        if (SessionManager.Instance != null && SessionManager.Instance.isGameStarted.Value)
        {
            response.Approved = false;
            response.Reason = "The game has already started.";
            return;
        }

        string playerName = Encoding.UTF8.GetString(request.Payload).Trim();
        if (playerName.Length == 0)
        {
            playerName = "Player " + (playerNames.Count + 1);
        }
        if (playerName.Length > maxNameLength)
        {
            playerName = playerName.Substring(0, maxNameLength);
        }

        playerNames[request.ClientNetworkId] = playerName;
        response.Approved = true;
    }

    private void OnClientDisconnected(ulong clientId)
    {
        if (networkManager.IsServer)
        {
            playerNames.Remove(clientId);
            return;
        }

        if (isLeaving) return;

        disconnectMessage = networkManager.DisconnectReason;
        if (string.IsNullOrEmpty(disconnectMessage) || disconnectMessage.StartsWith("["))
        {
            disconnectMessage = "The host left or the connection was lost.";
        }

        Debug.Log("Disconnected: " + disconnectMessage);
        LeaveGame();
    }
}
