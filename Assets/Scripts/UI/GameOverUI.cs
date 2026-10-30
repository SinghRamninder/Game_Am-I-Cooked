using TMPro;
using UnityEngine;

public class GameOverUI : MonoBehaviour
{
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private RectTransform content;
    [SerializeField] private TextMeshProUGUI winnerText;
    [SerializeField] private TextMeshProUGUI messageText;
    [SerializeField] private float fadeTime = 0.6f;
    [SerializeField] private float musicVolumeOnGameOver = 0.25f;
    [SerializeField] private float musicFadeTime = 1.5f;

    private float delayTimer = 0f;
    private float fadeTimer = -1f;

    private void Awake()
    {
        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
    }

    public void Show(string winnerName, Color winnerColor, bool isLocalWinner, float delay)
    {
        winnerText.text = winnerName.ToUpper() + " WINS!";
        winnerText.color = winnerColor;

        if (isLocalWinner)
        {
            messageText.text = "You are the last chef standing!";
        }
        else
        {
            messageText.text = winnerName + " is the last chef standing!";
        }

        delayTimer = delay;
        fadeTimer = 0f;
    }

    private void Update()
    {
        if (fadeTimer < 0f) return;

        if (delayTimer > 0f)
        {
            delayTimer -= Time.unscaledDeltaTime;
            if (delayTimer <= 0f)
            {
                StartFade();
            }
            return;
        }

        fadeTimer += Time.unscaledDeltaTime;
        float progress = Mathf.Clamp01(fadeTimer / fadeTime);
        float easedProgress = 1f - (1f - progress) * (1f - progress);

        canvasGroup.alpha = easedProgress;
        content.localScale = Vector3.one * Mathf.Lerp(0.85f, 1f, easedProgress);

        if (progress >= 1f)
        {
            fadeTimer = -1f;
        }
    }

    private void StartFade()
    {
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;

        MusicPlayer musicPlayer = FindFirstObjectByType<MusicPlayer>();
        if (musicPlayer != null)
        {
            musicPlayer.LowerVolume(musicVolumeOnGameOver, musicFadeTime);
        }
    }

    public void GoToMainMenu()
    {
        NetworkConnectionManager.Instance.LeaveGame();
    }
}
