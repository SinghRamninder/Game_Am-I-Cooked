using Unity.Netcode;
using UnityEngine;

public class IntroSequence : NetworkBehaviour
{
    public static IntroSequence Instance;

    [SerializeField] private IntroUI introUI;
    [SerializeField] private MusicPlayer musicPlayer;
    [SerializeField] private float introDuration = 10f;
    [SerializeField] private float uiExtraTime = 1.2f;

    private bool isLocked = true;
    private bool hasStarted = false;
    private bool isUIDone = false;
    private double startTime;

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

    public bool IsLocked()
    {
        return isLocked;
    }

    public void StartIntro()
    {
        if (!IsServer || hasStarted) return;

        StartIntroRpc(NetworkManager.ServerTime.Time);
    }

    [Rpc(SendTo.Everyone)]
    private void StartIntroRpc(double serverStartTime)
    {
        startTime = serverStartTime;
        hasStarted = true;

        foreach (PlayerHungerManager player in PlayerHungerManager.allPlayers)
        {
            player.FreezeControls();
        }

        musicPlayer.Play(GetElapsedTime());
    }

    private void Update()
    {
        if (!hasStarted || isUIDone) return;

        float elapsed = GetElapsedTime();
        introUI.ShowIntro(elapsed, GetLocalPlayer());

        if (isLocked && elapsed >= introDuration)
        {
            isLocked = false;
            foreach (PlayerHungerManager player in PlayerHungerManager.allPlayers)
            {
                player.UnfreezeControls();
            }
        }

        if (elapsed >= introDuration + uiExtraTime)
        {
            isUIDone = true;
            introUI.Hide();
        }
    }

    private float GetElapsedTime()
    {
        return Mathf.Max(0f, (float)(NetworkManager.ServerTime.Time - startTime));
    }

    private Transform GetLocalPlayer()
    {
        foreach (PlayerHungerManager player in PlayerHungerManager.allPlayers)
        {
            if (player != null && player.IsLocalPlayer)
            {
                return player.transform;
            }
        }

        return null;
    }
}
