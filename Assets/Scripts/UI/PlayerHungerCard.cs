using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerHungerCard : MonoBehaviour
{
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private Image characterIcon;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private Image[] chickenFills;
    [SerializeField] private GameObject youTag;
    [SerializeField] private GameObject deadTag;
    [SerializeField] private float fillSmoothSpeed = 2f;

    private float shownHunger = 0f;

    public void ShowPlayer(string playerName, Color playerColor, Sprite icon, float hungerPercentage, bool isLocalPlayer, bool isDead)
    {
        if (!gameObject.activeSelf)
        {
            gameObject.SetActive(true);
            shownHunger = hungerPercentage;
        }

        canvasGroup.alpha = isDead ? 0.6f : 1f;
        characterIcon.sprite = icon;
        nameText.text = playerName;
        nameText.color = playerColor;
        youTag.SetActive(isLocalPlayer);
        deadTag.SetActive(isDead);

        shownHunger = Mathf.MoveTowards(shownHunger, hungerPercentage, fillSmoothSpeed * Time.deltaTime);
        UpdateChickens(1f - shownHunger);
    }

    public void Hide()
    {
        if (gameObject.activeSelf)
        {
            gameObject.SetActive(false);
        }
    }

    private void UpdateChickens(float food)
    {
        float halfChickens = Mathf.Ceil(food * chickenFills.Length * 2f - 0.01f) / 2f;

        for (int i = 0; i < chickenFills.Length; i++)
        {
            chickenFills[i].fillAmount = Mathf.Clamp01(halfChickens - i);
        }
    }
}
