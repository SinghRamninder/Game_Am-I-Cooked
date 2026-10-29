using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class LobbyUI : MonoBehaviour
{
    [SerializeField] private Image[] slotBackgrounds;
    [SerializeField] private TextMeshProUGUI[] slotNameTexts;
    [SerializeField] private TextMeshProUGUI playerCountText;
    [SerializeField] private TextMeshProUGUI addressText;
    [SerializeField] private GameObject startButton;
    [SerializeField] private TextMeshProUGUI waitingText;
    [SerializeField] private Color emptySlotColor = new Color(1f, 1f, 1f, 0.12f);
    [SerializeField] private Color emptySlotTextColor = new Color(1f, 1f, 1f, 0.45f);

    private string localIPAddress;

    private void OnEnable()
    {
        localIPAddress = NetworkConnectionManager.Instance.GetLocalIPAddress();
    }

    private void Update()
    {
        NetworkManager networkManager = NetworkManager.Singleton;
        if (networkManager == null) return;

        bool isHost = networkManager.IsHost;
        startButton.SetActive(isHost);
        waitingText.gameObject.SetActive(!isHost);

        if (isHost)
        {
            addressText.text = "Your IP  <color=#FFD23F>" + localIPAddress + "</color>";
        }
        else
        {
            addressText.text = "Connected to the host's kitchen";
        }

        if (SessionManager.Instance == null)
        {
            waitingText.text = "Connecting...";
        }
        else
        {
            waitingText.text = "Waiting for the host to start...";
        }

        int playerCount = 0;
        for (int i = 0; i < slotNameTexts.Length; i++)
        {
            PlayerSessionData data;
            if (SessionManager.Instance != null && SessionManager.Instance.TryGetPlayerDataByIndex(i, out data))
            {
                string playerName = data.playerName.ToString();
                if (data.clientId == networkManager.LocalClientId)
                {
                    playerName += "  (You)";
                }

                slotNameTexts[i].text = playerName;
                slotNameTexts[i].color = Color.white;
                slotBackgrounds[i].color = PlayerIdentity.GetPlayerColor(i);
                playerCount++;
            }
            else
            {
                slotNameTexts[i].text = "Waiting for a chef...";
                slotNameTexts[i].color = emptySlotTextColor;
                slotBackgrounds[i].color = emptySlotColor;
            }
        }

        playerCountText.text = playerCount + " / " + slotNameTexts.Length + " chefs";
    }
}
