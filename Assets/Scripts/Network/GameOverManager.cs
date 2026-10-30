using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class GameOverManager : NetworkBehaviour
{
    public static GameOverManager Instance;

    [SerializeField] private GameOverUI gameOverUI;
    [SerializeField] private float showDelay = 1f;

    private bool isGameOver = false;
    private bool hasMatchStarted = false;
    private List<PlayerHungerManager> lastDeadPlayers = new List<PlayerHungerManager>();

    private void Awake()
    {
        Instance = this;
    }

    public override void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }

        base.OnDestroy();
    }

    public bool IsGameOver()
    {
        return isGameOver;
    }

    private void LateUpdate()
    {
        if (!IsSpawned || !IsServer || isGameOver) return;

        if (PlayerHungerManager.allPlayers.Count >= 2)
        {
            hasMatchStarted = true;
        }

        if (!hasMatchStarted) return;

        PlayerHungerManager lastAlivePlayer = null;
        int aliveCount = 0;
        foreach (PlayerHungerManager player in PlayerHungerManager.allPlayers)
        {
            if (player != null && !player.isDead.Value)
            {
                lastAlivePlayer = player;
                aliveCount++;
            }
        }

        if (aliveCount > 1) return;

        if (aliveCount == 1)
        {
            DeclareWinner(lastAlivePlayer);
        }
        else
        {
            PlayerHungerManager winner = PickWinnerFromLastDeaths();
            if (winner != null)
            {
                winner.Revive();
            }
            DeclareWinner(winner);
        }
    }

    private PlayerHungerManager PickWinnerFromLastDeaths()
    {
        int lastDeathFrame = -1;
        foreach (PlayerHungerManager player in PlayerHungerManager.allPlayers)
        {
            if (player != null && player.deathFrame > lastDeathFrame)
            {
                lastDeathFrame = player.deathFrame;
            }
        }

        lastDeadPlayers.Clear();
        foreach (PlayerHungerManager player in PlayerHungerManager.allPlayers)
        {
            if (player != null && player.deathFrame == lastDeathFrame)
            {
                lastDeadPlayers.Add(player);
            }
        }

        if (lastDeadPlayers.Count == 0) return null;

        return lastDeadPlayers[Random.Range(0, lastDeadPlayers.Count)];
    }

    private void DeclareWinner(PlayerHungerManager winner)
    {
        isGameOver = true;

        int winnerIndex = -1;
        if (winner != null)
        {
            winnerIndex = winner.GetComponent<PlayerIdentity>().playerIndex.Value;
            Debug.Log("Game over. Winner: " + winner.name);
        }

        ShowGameOverRpc(winnerIndex);
    }

    [Rpc(SendTo.Everyone)]
    private void ShowGameOverRpc(int winnerIndex)
    {
        isGameOver = true;

        foreach (PlayerHungerManager player in PlayerHungerManager.allPlayers)
        {
            player.FreezeControls();
        }

        PlayerIdentity winner = null;
        foreach (PlayerHungerManager player in PlayerHungerManager.allPlayers)
        {
            PlayerIdentity identity = player.GetComponent<PlayerIdentity>();
            if (identity.playerIndex.Value == winnerIndex)
            {
                winner = identity;
            }
        }

        if (winner == null)
        {
            gameOverUI.Show("Nobody", Color.white, false, showDelay);
            return;
        }

        gameOverUI.Show(winner.GetPlayerName(), winner.GetColor(), winner.IsLocalPlayer, showDelay);
    }
}
