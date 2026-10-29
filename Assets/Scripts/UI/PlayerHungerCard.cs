using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerHungerCard : MonoBehaviour
{
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private Image colorBadge;
    [SerializeField] private TextMeshProUGUI badgeText;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI hungerText;
    [SerializeField] private RectTransform hungerFill;
    [SerializeField] private Image hungerFillImage;
    [SerializeField] private GameObject youTag;
    [SerializeField] private GameObject deadTag;
    [SerializeField] private Color lowHungerColor = new Color(0.45f, 0.85f, 0.3f);
    [SerializeField] private Color midHungerColor = new Color(1f, 0.75f, 0.2f);
    [SerializeField] private Color highHungerColor = new Color(0.92f, 0.22f, 0.18f);
    [SerializeField] private float fillSmoothSpeed = 2f;

    private float shownHunger = 0f;

    public void ShowPlayer(int playerIndex, string playerName, Color playerColor, float hungerPercentage, bool isLocalPlayer, bool isDead)
    {
        if (!gameObject.activeSelf)
        {
            gameObject.SetActive(true);
            shownHunger = hungerPercentage;
        }

        canvasGroup.alpha = isDead ? 0.6f : 1f;
        colorBadge.color = playerColor;
        badgeText.text = "P" + (playerIndex + 1);
        nameText.text = playerName;
        youTag.SetActive(isLocalPlayer);
        deadTag.SetActive(isDead);

        shownHunger = Mathf.MoveTowards(shownHunger, hungerPercentage, fillSmoothSpeed * Time.deltaTime);
        hungerFill.anchorMax = new Vector2(shownHunger, 1f);
        hungerFillImage.color = GetHungerColor(shownHunger);
        hungerText.text = "Hunger " + Mathf.RoundToInt(hungerPercentage * 100f) + "%";
    }

    public void Hide()
    {
        if (gameObject.activeSelf)
        {
            gameObject.SetActive(false);
        }
    }

    private Color GetHungerColor(float hunger)
    {
        if (hunger < 0.5f)
        {
            return Color.Lerp(lowHungerColor, midHungerColor, hunger / 0.5f);
        }

        return Color.Lerp(midHungerColor, highHungerColor, (hunger - 0.5f) / 0.5f);
    }
}
