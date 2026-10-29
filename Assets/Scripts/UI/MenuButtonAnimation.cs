using UnityEngine;
using UnityEngine.EventSystems;

public class MenuButtonAnimation : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private float hoverScale = 1.08f;
    [SerializeField] private float scaleSpeed = 12f;

    private Vector3 startScale;
    private bool isHovered = false;

    private void Awake()
    {
        startScale = transform.localScale;
    }

    private void OnDisable()
    {
        isHovered = false;
        transform.localScale = startScale;
    }

    private void Update()
    {
        Vector3 targetScale = startScale;
        if (isHovered)
        {
            targetScale = startScale * hoverScale;
        }

        transform.localScale = Vector3.Lerp(transform.localScale, targetScale, scaleSpeed * Time.unscaledDeltaTime);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        isHovered = true;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovered = false;
    }
}
