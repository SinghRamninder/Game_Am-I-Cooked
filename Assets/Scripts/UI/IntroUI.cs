using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class IntroUI : MonoBehaviour
{
    [SerializeField] private RectTransform canvasRect;
    [SerializeField] private Image spotlight;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private Image titleBand;
    [SerializeField] private TextMeshProUGUI countdownText;
    [SerializeField] private float dimAlpha = 0.85f;
    [SerializeField] private float titleBandAlpha = 0.7f;
    [SerializeField] private float spotlightWorldRadius = 1.2f;
    [SerializeField] private float spotlightHoleSize = 0.03f;
    [SerializeField] private float closeZoom = 0.55f;
    [SerializeField] private float zoomInTime = 0.8f;
    [SerializeField] private float zoomOutEndTime = 6f;
    [SerializeField] private float taglineTime = 6f;
    [SerializeField] private float countdownStartTime = 7f;
    [SerializeField] private float releaseTime = 10f;

    private const string FirstText = "EVERY CHEF FOR THEMSELVES!";
    private const string SecondText = "Grab food. Steal meals. Fight to survive.";

    private PlayerCamera playerCamera;
    private Camera mainCamera;

    private void Awake()
    {
        mainCamera = Camera.main;
        if (mainCamera != null)
        {
            playerCamera = mainCamera.GetComponent<PlayerCamera>();
        }

        Hide();
    }

    public void ShowIntro(float time, Transform localPlayer)
    {
        gameObject.SetActive(true);

        UpdateCamera(time);
        UpdateSpotlight(time, localPlayer);
        UpdateTitle(time);
        UpdateCountdown(time);
    }

    public void Hide()
    {
        if (playerCamera != null)
        {
            playerCamera.SetZoom(1f);
        }

        gameObject.SetActive(false);
    }

    private void UpdateCamera(float time)
    {
        if (playerCamera == null) return;

        float zoom;
        if (time < zoomInTime)
        {
            float progress = time / zoomInTime;
            zoom = Mathf.Lerp(1f, closeZoom, 1f - (1f - progress) * (1f - progress));
        }
        else
        {
            float progress = Mathf.Clamp01((time - zoomInTime) / (zoomOutEndTime - zoomInTime));
            zoom = Mathf.Lerp(closeZoom, 1f, Mathf.SmoothStep(0f, 1f, progress));
        }

        playerCamera.SetZoom(zoom);
    }

    private void UpdateSpotlight(float time, Transform localPlayer)
    {
        float alpha = dimAlpha;
        if (time < 0.4f)
        {
            alpha = dimAlpha * time / 0.4f;
        }
        else if (time > zoomOutEndTime - 1f)
        {
            alpha = dimAlpha * Mathf.Clamp01(zoomOutEndTime - time);
        }

        spotlight.color = new Color(0f, 0f, 0f, alpha);

        if (localPlayer == null || mainCamera == null) return;

        Vector3 center = localPlayer.position + Vector3.up * 0.9f;
        Vector3 edge = center + mainCamera.transform.right * spotlightWorldRadius;

        Vector2 localCenter;
        Vector2 localEdge;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, mainCamera.WorldToScreenPoint(center), null, out localCenter);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, mainCamera.WorldToScreenPoint(edge), null, out localEdge);

        float radius = Vector2.Distance(localCenter, localEdge);
        float size = radius / spotlightHoleSize;
        spotlight.rectTransform.anchoredPosition = localCenter;
        spotlight.rectTransform.sizeDelta = new Vector2(size, size);
    }

    private void UpdateTitle(float time)
    {
        float sectionStart = 0f;
        if (time < taglineTime)
        {
            titleText.text = FirstText;
        }
        else
        {
            titleText.text = SecondText;
            sectionStart = taglineTime;
        }

        float popProgress = Mathf.Clamp01((time - sectionStart) / 0.35f);
        float alpha = popProgress;
        if (time > releaseTime)
        {
            alpha = Mathf.Clamp01(1f - (time - releaseTime) / 0.6f);
        }

        titleText.alpha = alpha;

        Color bandColor = titleBand.color;
        bandColor.a = titleBandAlpha * Mathf.Clamp01(time / 0.35f);
        if (time > releaseTime)
        {
            bandColor.a = titleBandAlpha * alpha;
        }
        titleBand.color = bandColor;
        titleText.rectTransform.localScale = Vector3.one * Mathf.Lerp(1.25f, 1f, 1f - (1f - popProgress) * (1f - popProgress));
    }

    private void UpdateCountdown(float time)
    {
        if (time < countdownStartTime)
        {
            countdownText.alpha = 0f;
            return;
        }

        int step = Mathf.FloorToInt(time - countdownStartTime);
        float stepTime = time - countdownStartTime - step;
        int number = Mathf.RoundToInt(releaseTime - countdownStartTime) - step;

        if (number > 0)
        {
            countdownText.text = number.ToString();
        }
        else
        {
            countdownText.text = "GO!";
        }

        float popProgress = Mathf.Clamp01(stepTime / 0.25f);
        countdownText.rectTransform.localScale = Vector3.one * Mathf.Lerp(1.6f, 1f, 1f - (1f - popProgress) * (1f - popProgress));
        countdownText.alpha = Mathf.Clamp01(popProgress * 2f) * Mathf.Clamp01((1f - stepTime) / 0.25f);
    }
}
