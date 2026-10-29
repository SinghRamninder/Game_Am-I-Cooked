using TMPro;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

public class PlayerIdentity : NetworkBehaviour
{
    private static Color[] playerColors =
    {
        new Color(0.95f, 0.3f, 0.25f),
        new Color(0.25f, 0.6f, 1f),
        new Color(0.35f, 0.85f, 0.3f),
        new Color(1f, 0.78f, 0.15f)
    };

    [SerializeField] private Renderer colorRing;
    [SerializeField] private TextMeshProUGUI nameText;

    public NetworkVariable<int> playerIndex = new NetworkVariable<int>(0);
    public NetworkVariable<FixedString32Bytes> playerName = new NetworkVariable<FixedString32Bytes>();

    public override void OnNetworkSpawn()
    {
        playerIndex.OnValueChanged += OnPlayerIndexChanged;
        playerName.OnValueChanged += OnPlayerNameChanged;
        ApplyIdentity();
    }

    public override void OnNetworkDespawn()
    {
        playerIndex.OnValueChanged -= OnPlayerIndexChanged;
        playerName.OnValueChanged -= OnPlayerNameChanged;
    }

    public static Color GetPlayerColor(int index)
    {
        return playerColors[Mathf.Abs(index) % playerColors.Length];
    }

    public Color GetColor()
    {
        return GetPlayerColor(playerIndex.Value);
    }

    public string GetPlayerName()
    {
        return playerName.Value.ToString();
    }

    private void OnPlayerIndexChanged(int oldIndex, int newIndex)
    {
        ApplyIdentity();
    }

    private void OnPlayerNameChanged(FixedString32Bytes oldName, FixedString32Bytes newName)
    {
        ApplyIdentity();
    }

    private void ApplyIdentity()
    {
        gameObject.name = "Player " + (playerIndex.Value + 1) + " (" + GetPlayerName() + ")";
        colorRing.material.color = GetColor();
        nameText.text = GetPlayerName();
        nameText.color = GetColor();
    }
}
