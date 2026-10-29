using Unity.Netcode;
using UnityEngine;

public class PlayerMessages : NetworkBehaviour
{
    [SerializeField] private FloatingMessageUI messageUI;
    [SerializeField] private FloatingMessageUI hungerPopupUI;

    public void ShowMessage(string message)
    {
        ShowMessageRpc(message);
    }

    public void ShowHungerPopup(int reducedPercentage)
    {
        ShowHungerPopupRpc(reducedPercentage);
    }

    [Rpc(SendTo.Owner)]
    private void ShowMessageRpc(string message)
    {
        messageUI.ShowMessage(message);
    }

    [Rpc(SendTo.Everyone)]
    private void ShowHungerPopupRpc(int reducedPercentage)
    {
        messageUI.HideMessage();
        hungerPopupUI.ShowMessage("-" + reducedPercentage + "%");
    }
}
