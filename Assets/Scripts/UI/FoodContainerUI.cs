using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class FoodContainerUI : MonoBehaviour
{
    [SerializeField] private CanvasGroup stockLabel;
    [SerializeField] private Image labelBackground;
    [SerializeField] private TextMeshProUGUI labelText;
    [SerializeField] private Renderer[] containerRenderers;
    [SerializeField] private Color lowStockColor = new Color(1f, 0.82f, 0.2f);
    [SerializeField] private Color lowStockTextColor = new Color(0.3f, 0.2f, 0.05f);
    [SerializeField] private Color outOfStockColor = new Color(0.85f, 0.2f, 0.18f);
    [SerializeField] private Color outOfStockTextColor = Color.white;
    [SerializeField] private float darkenAmount = 0.45f;
    [SerializeField] private float popTime = 0.25f;

    private Coroutine popCoroutine;

    private void Awake()
    {
        stockLabel.alpha = 0f;
    }

    public void ShowLowStock()
    {
        ShowLabel("Low in stock", lowStockColor, lowStockTextColor);
    }

    public void ShowOutOfStock()
    {
        ShowLabel("Out of stock", outOfStockColor, outOfStockTextColor);
        DarkenContainer();
    }

    private void ShowLabel(string message, Color backgroundColor, Color textColor)
    {
        labelText.text = message;
        labelText.color = textColor;
        labelBackground.color = backgroundColor;

        if (popCoroutine != null)
        {
            StopCoroutine(popCoroutine);
        }

        popCoroutine = StartCoroutine(PopLabel());
    }

    private IEnumerator PopLabel()
    {
        float timer = 0f;
        while (timer < popTime)
        {
            timer += Time.deltaTime;
            float progress = timer / popTime;
            stockLabel.alpha = progress;
            stockLabel.transform.localScale = Vector3.one * Mathf.Lerp(1.4f, 1f, progress);
            yield return null;
        }

        stockLabel.alpha = 1f;
        stockLabel.transform.localScale = Vector3.one;
        popCoroutine = null;
    }

    private void DarkenContainer()
    {
        foreach (Renderer containerRenderer in containerRenderers)
        {
            Color darkColor = containerRenderer.material.color * darkenAmount;
            darkColor.a = 1f;
            containerRenderer.material.color = darkColor;
        }
    }
}
