using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using DG.Tweening;
using UnityEngine.VFX;

[RequireComponent(typeof(Selectable))]
public class MenuButtonFeedback : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler
{
    [Header("Hover Settings")]
    [SerializeField] private float hoverScale = 1.1f;
    [SerializeField] private float hoverDuration = 0.2f;
    [SerializeField] private Color hoverColor = Color.white;
    [SerializeField] private bool useHoverColor = true;

    [Header("Click Settings")]
    [SerializeField] private float clickScale = 0.95f;
    [SerializeField] private float clickDuration = 0.1f;

    [Header("Additional Effects")]
    [SerializeField] private bool useOutline = true;
    [SerializeField] private Outline outlineComponent;
    [SerializeField] private float outlineWidth = 3f;
    [SerializeField] private Color outlineColor = Color.yellow;

    private Selectable selectable;
    private Graphic[] graphics;
    private Color[] originalColors;
    private Vector3 originalScale;
    private bool isHovered = false;

    [Header("Effects")]
    [SerializeField] private VisualEffect buttonVFX;

    private void Awake()
    {
        selectable = GetComponent<Selectable>();
        graphics = GetComponentsInChildren<Graphic>();
        originalScale = transform.localScale;

        originalColors = new Color[graphics.Length];
        for (int i = 0; i < graphics.Length; i++)
        {
            originalColors[i] = graphics[i].color;
        }

        if ( buttonVFX == null)
        {
            buttonVFX = GetComponentInChildren<VisualEffect>();
        }

        if (buttonVFX != null)
        {
            buttonVFX.Stop();
        }

        if (useOutline && outlineComponent == null)
        {
            outlineComponent = GetComponent<Outline>();
            if (outlineComponent == null)
            {
                outlineComponent = gameObject.AddComponent<Outline>();
            }
        }

        if (outlineComponent)
        {
            outlineComponent.effectColor = outlineColor;
            outlineComponent.effectDistance = new Vector2(outlineWidth, outlineWidth);
            outlineComponent.enabled = false;
        }

        Button button = GetComponent<Button>();
        if (button != null)
        {
            button.onClick.AddListener(OnClick);
        }
    }

    public void OnHoverEnter()
    {
        if (!selectable.interactable) return;

        isHovered = true;

        // Scale effect
        transform.DOScale(originalScale * hoverScale, hoverDuration)
            .SetEase(Ease.OutBack)
            .SetUpdate(true);

        // Color effect
        if (useHoverColor)
        {
            for (int i = 0; i < graphics.Length; i++)
            {
                graphics[i].DOColor(hoverColor, hoverDuration).SetUpdate(true);
            }
        }

        // Outline effect
        if (outlineComponent)
        {
            outlineComponent.enabled = true;
        }

        // VFX effect
        if (buttonVFX != null)
        {
            buttonVFX.Play();
        }

    }

    public void OnHoverExit()
    {
        isHovered = false;

        // Reset scale
        transform.DOScale(originalScale, hoverDuration)
            .SetEase(Ease.OutBack)
            .SetUpdate(true);

        // Reset color
        if (useHoverColor)
        {
            for (int i = 0; i < graphics.Length; i++)
            {
                graphics[i].DOColor(originalColors[i], hoverDuration).SetUpdate(true);
            }
        }

        // Disable outline
        if (outlineComponent)
        {
            outlineComponent.enabled = false;
        }

        // Stop VFX
        if (buttonVFX != null)
        {
            buttonVFX.Stop();
        }
    }

    private void OnClick()
    {
        if (!selectable.interactable) return;

        // Click animation
        transform.DOScale(originalScale * clickScale, clickDuration)
            .SetEase(Ease.InOutQuad)
            .SetUpdate(true)
            .OnComplete(() =>
            {
                transform.DOScale(isHovered ? originalScale * hoverScale : originalScale, clickDuration)
                    .SetEase(Ease.OutBack)
                    .SetUpdate(true);
            });

    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        OnHoverEnter();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        OnHoverExit();
    }

    public void OnSelect(BaseEventData eventData)
    {
        OnHoverEnter();
    }

    public void OnDeselect(BaseEventData eventData)
    {
        OnHoverExit();
    }

    private void OnDisable()
    {
        transform.localScale = originalScale;

        if (graphics != null && originalColors != null)
        {
            for (int i = 0; i < graphics.Length && i < originalColors.Length; i++)
            {
                graphics[i].color = originalColors[i];
            }
        }

        if (outlineComponent)
        {
            outlineComponent.enabled = false;
        }

        if (buttonVFX != null)
        {
            buttonVFX.Stop();
        }
    }
}