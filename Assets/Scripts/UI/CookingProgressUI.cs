using UnityEngine;
using UnityEngine.UI;

public class CookingProgressUI : MonoBehaviour
{
    [SerializeField] private GameObject progressBar;
    [SerializeField] private RectTransform fillRect;
    [SerializeField] private Image fillImage;
    [SerializeField] private Image warningIcon;
    [SerializeField] private Color cookingColor = new Color(0.45f, 0.85f, 0.3f);
    [SerializeField] private Color burningStartColor = new Color(1f, 0.65f, 0.15f);
    [SerializeField] private Color burningEndColor = new Color(0.9f, 0.2f, 0.15f);
    [SerializeField] private float warningThreshold = 0.7f;

    private void Awake()
    {
        HideProgress();
    }

    public void ShowCookingProgress(float progress)
    {
        progressBar.SetActive(true);
        warningIcon.gameObject.SetActive(false);
        fillImage.color = cookingColor;
        SetFill(progress);
    }

    public void ShowBurningProgress(float progress)
    {
        progressBar.SetActive(true);
        warningIcon.gameObject.SetActive(progress >= warningThreshold);
        fillImage.color = Color.Lerp(burningStartColor, burningEndColor, progress);
        SetFill(progress);
    }

    public void HideProgress()
    {
        progressBar.SetActive(false);
        warningIcon.gameObject.SetActive(false);
    }

    private void SetFill(float progress)
    {
        fillRect.anchorMax = new Vector2(Mathf.Clamp01(progress), 1f);
    }
}
