using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerSpawner : MonoBehaviour
{
    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private Transform[] spawnPoints;
    [SerializeField] private GameObject networkManagerPrefab;
    [SerializeField] private bool spawnTestPlayer = true;

    private void Awake()
    {
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer)
        {
            NetworkManager.Singleton.SceneManager.OnLoadEventCompleted += OnLoadEventCompleted;
        }
    }

    private void Start()
    {
        if (NetworkManager.Singleton == null)
        {
            Debug.Log("No network running, starting a test host.");
            Instantiate(networkManagerPrefab);
            NetworkConnectionManager.Instance.StartHost("Player");
            SpawnAllPlayers();
        }
    }

    private void OnDestroy()
    {
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.SceneManager != null)
        {
            NetworkManager.Singleton.SceneManager.OnLoadEventCompleted -= OnLoadEventCompleted;
        }
    }

    private void OnLoadEventCompleted(string sceneName, LoadSceneMode loadSceneMode, List<ulong> clientsCompleted, List<ulong> clientsTimedOut)
    {
        if (sceneName != gameObject.scene.name) return;

        SpawnAllPlayers();
    }

    private void SpawnAllPlayers()
    {
        foreach (ulong clientId in NetworkManager.Singleton.ConnectedClientsIds)
        {
            SpawnPlayer(clientId);
        }

        if (spawnTestPlayer)
        {
            SpawnTestPlayer(NetworkManager.Singleton.ConnectedClientsIds.Count);
        }

        foreach (FoodSpawner foodSpawner in FindObjectsByType<FoodSpawner>(FindObjectsSortMode.None))
        {
            foodSpawner.StartSpawning();
        }
    }

    private void SpawnPlayer(ulong clientId)
    {
        int playerIndex = 0;
        string playerName = "Player";

        PlayerSessionData data;
        if (SessionManager.Instance != null && SessionManager.Instance.TryGetPlayerData(clientId, out data))
        {
            playerIndex = data.playerIndex;
            playerName = data.playerName.ToString();
        }

        Transform spawnPoint = spawnPoints[playerIndex % spawnPoints.Length];
        GameObject player = Instantiate(playerPrefab, spawnPoint.position, spawnPoint.rotation);

        player.GetComponent<NetworkObject>().SpawnAsPlayerObject(clientId, true);

        PlayerIdentity identity = player.GetComponent<PlayerIdentity>();
        identity.playerIndex.Value = playerIndex;
        identity.playerName.Value = playerName;
    }

    private void SpawnTestPlayer(int playerIndex)
    {
        Transform spawnPoint = spawnPoints[playerIndex % spawnPoints.Length];
        GameObject player = Instantiate(playerPrefab, spawnPoint.position, spawnPoint.rotation);
        player.AddComponent<TestPlayerInput>();

        player.GetComponent<NetworkObject>().Spawn(true);

        PlayerIdentity identity = player.GetComponent<PlayerIdentity>();
        identity.playerIndex.Value = playerIndex;
        identity.playerName.Value = "Test (IJKL)";
    }
}
