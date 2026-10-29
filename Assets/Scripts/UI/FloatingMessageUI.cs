using System.Collections;
using TMPro;
using UnityEngine;

public class FloatingMessageUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI messageText;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private float fadeInTime = 0.2f;
    [SerializeField] private float stayTime = 1.2f;
    [SerializeField] private float fadeOutTime = 0.5f;
    [SerializeField] private float quickFadeOutTime = 0.08f;
    [SerializeField] private float floatSpeed = 25f;
    [SerializeField] private float maxTextWidth = 420f;
    [SerializeField] private Vector2 backgroundPadding = new Vector2(18f, 8f);

    private RectTransform messageRect;
    private Vector2 startPosition;
    private Coroutine messageCoroutine;

    private void Awake()
    {
        messageRect = GetComponent<RectTransform>();
        startPosition = messageRect.anchoredPosition;
        canvasGroup.alpha = 0f;

        messageText.rectTransform.offsetMin = backgroundPadding;
        messageText.rectTransform.offsetMax = -backgroundPadding;
    }

    public void ShowMessage(string message)
    {
        if (messageCoroutine != null)
        {
            StopCoroutine(messageCoroutine);
        }

        messageCoroutine = StartCoroutine(PlayMessage(message));
    }

    public void HideMessage()
    {
        if (messageCoroutine != null)
        {
            StopCoroutine(messageCoroutine);
            messageCoroutine = null;
        }

        if (canvasGroup.alpha > 0f)
        {
            messageCoroutine = StartCoroutine(FadeOutQuickly());
        }
    }

    private IEnumerator FadeOutQuickly()
    {
        float startAlpha = canvasGroup.alpha;
        float fadeTimer = 0f;
        while (fadeTimer < quickFadeOutTime)
        {
            fadeTimer += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, fadeTimer / quickFadeOutTime);
            yield return null;
        }

        canvasGroup.alpha = 0f;
    }

    private IEnumerator PlayMessage(string message)
    {
        if (canvasGroup.alpha > 0f)
        {
            yield return FadeOutQuickly();
        }

        messageText.text = message;
        Vector2 textSize = messageText.GetPreferredValues(message, maxTextWidth, 0f);
        float width = Mathf.Min(textSize.x, maxTextWidth) + backgroundPadding.x * 2f;
        float height = textSize.y + backgroundPadding.y * 2f;
        messageRect.sizeDelta = new Vector2(width, height);

        float totalTime = fadeInTime + stayTime + fadeOutTime;
        float timer = 0f;

        while (timer < totalTime)
        {
            timer += Time.deltaTime;
            messageRect.anchoredPosition = startPosition + Vector2.up * floatSpeed * timer;

            if (timer < fadeInTime)
            {
                canvasGroup.alpha = timer / fadeInTime;
            }
            else if (timer < fadeInTime + stayTime)
            {
                canvasGroup.alpha = 1f;
            }
            else
            {
                canvasGroup.alpha = 1f - (timer - fadeInTime - stayTime) / fadeOutTime;
            }

            yield return null;
        }

        canvasGroup.alpha = 0f;
        messageCoroutine = null;
    }
}
