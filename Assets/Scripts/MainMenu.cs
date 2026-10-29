using TMPro;
using Unity.Netcode;
using UnityEngine;

public class MainMenu : MonoBehaviour
{
    [SerializeField] private string scneneToLoad;
    [SerializeField] private GameObject mainMenuObject;
    [SerializeField] private GameObject creditsMenuObject;
    [SerializeField] private GameObject optionsMenuObject;
    [SerializeField] private GameObject playMenuObject;
    [SerializeField] private GameObject lobbyMenuObject;
    [SerializeField] private TMP_InputField playerNameInput;
    [SerializeField] private TMP_InputField ipAddressInput;
    [SerializeField] private TextMeshProUGUI statusText;
    [SerializeField] private GameObject networkManagerPrefab;

    private bool isConnecting = false;

    private void Start()
    {
        if (NetworkManager.Singleton == null)
        {
            Instantiate(networkManagerPrefab);
        }

        statusText.text = "";
        ShowMenu(mainMenuObject);
        CheckDisconnectMessage();
    }

    private void Update()
    {
        CheckDisconnectMessage();

        if (isConnecting && NetworkManager.Singleton.IsConnectedClient)
        {
            isConnecting = false;
            statusText.text = "";
            ShowMenu(lobbyMenuObject);
        }
    }

    private void CheckDisconnectMessage()
    {
        string disconnectMessage = NetworkConnectionManager.Instance.ConsumeDisconnectMessage();
        if (disconnectMessage != "")
        {
            isConnecting = false;
            statusText.text = disconnectMessage;
            ShowMenu(playMenuObject);
        }
    }

    public void Play()
    {
        statusText.text = "";
        ShowMenu(playMenuObject);
    }

    public void Host()
    {
        if (NetworkConnectionManager.Instance.StartHost(GetPlayerName()))
        {
            statusText.text = "";
            ShowMenu(lobbyMenuObject);
        }
        else
        {
            statusText.text = "Could not host the game. Is another game already running?";
        }
    }

    public void Join()
    {
        if (isConnecting) return;

        string ipAddress = ipAddressInput.text.Trim();
        if (ipAddress == "")
        {
            ipAddress = "127.0.0.1";
        }

        if (NetworkConnectionManager.Instance.StartClient(ipAddress, GetPlayerName()))
        {
            isConnecting = true;
            statusText.text = "Connecting to " + ipAddress + "...";
        }
        else
        {
            statusText.text = "Could not connect to " + ipAddress + ".";
        }
    }

    public void StartGame()
    {
        NetworkConnectionManager.Instance.StartGame(scneneToLoad);
    }

    public void LeaveLobby()
    {
        isConnecting = false;
        NetworkConnectionManager.Instance.LeaveGame();
        statusText.text = "";
        ShowMenu(playMenuObject);
    }

    public void Credits()
    {
        ShowMenu(creditsMenuObject);
    }

    public void Options()
    {
        ShowMenu(optionsMenuObject);
    }

    public void Back()
    {
        if (isConnecting)
        {
            LeaveLobby();
        }

        statusText.text = "";
        ShowMenu(mainMenuObject);
    }

    public void Quit()
    {
        Debug.Log("Quit");
        Application.Quit();
    }

    private void ShowMenu(GameObject menuToShow)
    {
        GameObject[] menus = { mainMenuObject, creditsMenuObject, optionsMenuObject, playMenuObject, lobbyMenuObject };
        foreach (GameObject menu in menus)
        {
            if (menu != null)
            {
                menu.SetActive(menu == menuToShow);
            }
        }
    }

    private string GetPlayerName()
    {
        string playerName = playerNameInput.text.Trim();
        if (playerName == "")
        {
            playerName = "Chef " + Random.Range(10, 100);
            playerNameInput.text = playerName;
        }

        return playerName;
    }
}
