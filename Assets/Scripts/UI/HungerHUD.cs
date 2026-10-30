using TMPro;
using Unity.Netcode;
using UnityEngine;

public class HungerHUD : MonoBehaviour
{
    [SerializeField] private PlayerHungerCard[] playerCards;
    [SerializeField] private TextMeshProUGUI pingText;

    private void Update()
    {
        for (int i = 0; i < playerCards.Length; i++)
        {
            PlayerHungerManager player = FindPlayer(i);
            if (player == null)
            {
                playerCards[i].Hide();
                continue;
            }

            PlayerIdentity identity = player.GetComponent<PlayerIdentity>();
            playerCards[i].ShowPlayer(identity.GetPlayerName(), identity.GetColor(), identity.GetCharacterIcon(), player.GetHungerPercentage(), player.IsLocalPlayer, player.isDead.Value);
        }

        UpdatePingText();
    }

    private PlayerHungerManager FindPlayer(int playerIndex)
    {
        foreach (PlayerHungerManager player in PlayerHungerManager.allPlayers)
        {
            if (player != null && player.GetComponent<PlayerIdentity>().playerIndex.Value == playerIndex)
            {
                return player;
            }
        }

        return null;
    }

    private void UpdatePingText()
    {
        NetworkManager networkManager = NetworkManager.Singleton;
        if (networkManager == null || !networkManager.IsListening || NetworkLagHandler.Instance == null)
        {
            pingText.text = "";
        }
        else if (networkManager.IsHost)
        {
            pingText.text = "Host";
        }
        else
        {
            pingText.text = "Ping " + NetworkLagHandler.Instance.GetLocalPing() + " ms";
        }
    }
}
