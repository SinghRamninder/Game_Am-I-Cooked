using UnityEngine;
using UnityEngine.UI;

public class DangerVignette : MonoBehaviour
{
    [SerializeField] private Image vignetteImage;
    [SerializeField] private float dangerLevel = 0.75f;
    [SerializeField] private float blinkDuration = 1.6f;
    [SerializeField] private float minAlpha = 0.05f;
    [SerializeField] private float maxAlpha = 0.4f;
    [SerializeField] private float fadeSpeed = 2f;

    private float strength = 0f;
    private float blinkTime = 0f;

    private void Start()
    {
        SetAlpha(0f);
    }

    private void Update()
    {
        float targetStrength = 0f;
        if (IsLocalPlayerInDanger())
        {
            targetStrength = 1f;
        }

        strength = Mathf.MoveTowards(strength, targetStrength, fadeSpeed * Time.deltaTime);

        if (strength <= 0f)
        {
            blinkTime = 0f;
            SetAlpha(0f);
            return;
        }

        blinkTime += Time.deltaTime;
        float blink = 0.5f - 0.5f * Mathf.Cos(blinkTime / blinkDuration * Mathf.PI * 2f);
        SetAlpha(Mathf.Lerp(minAlpha, maxAlpha, blink) * strength);
    }

    private bool IsLocalPlayerInDanger()
    {
        if (GameOverManager.Instance != null && GameOverManager.Instance.IsGameOver()) return false;

        foreach (PlayerHungerManager player in PlayerHungerManager.allPlayers)
        {
            if (player != null && player.IsLocalPlayer)
            {
                return !player.isDead.Value && player.GetHungerPercentage() >= dangerLevel;
            }
        }

        return false;
    }

    private void SetAlpha(float alpha)
    {
        Color color = vignetteImage.color;
        color.a = alpha;
        vignetteImage.color = color;
    }
}
