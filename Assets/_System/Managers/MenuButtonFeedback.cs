using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using DG.Tweening;

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

    [Header("Audio")]
    [SerializeField] private AudioClip hoverSound;
    [SerializeField] private AudioClip clickSound;
    [SerializeField] private AudioSource audioSource;

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

    private void Awake()
    {
        selectable = GetComponent<Selectable>();
        graphics = GetComponentsInChildren<Graphic>();
        originalScale = transform.localScale;

        // Sauvegarder les couleurs originales
        originalColors = new Color[graphics.Length];
        for (int i = 0; i < graphics.Length; i++)
        {
            originalColors[i] = graphics[i].color;
        }

        // Setup outline si nécessaire
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

        // Setup audio
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null && (hoverSound != null || clickSound != null))
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;
            }
        }

        // Ajouter un listener pour le clic
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

        // Sound effect
        PlaySound(hoverSound);
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

        // Sound effect
        PlaySound(clickSound);
    }

    private void PlaySound(AudioClip clip)
    {
        if (audioSource && clip)
        {
            audioSource.PlayOneShot(clip);
        }
    }

    // Interface implementations pour EventSystem
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
        // Reset quand désactivé
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
    }
}