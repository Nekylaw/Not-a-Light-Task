using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using DG.Tweening;

public class WorldSpaceMenuInteraction : MonoBehaviour
{
    [Header("Raycast Settings")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private float raycastDistance = 50f;
    [SerializeField] private LayerMask uiLayerMask = -1;

    [Header("Crosshair")]
    [SerializeField] private GameObject crosshairUI;
    [SerializeField] private Image crosshairImage;
    [SerializeField] private Color defaultColor = Color.white;
    [SerializeField] private Color hoverColor = Color.green;
    [SerializeField] private float crosshairScaleOnHover = 1.2f;

    [Header("Visual Feedback")]
    [SerializeField] private bool showDebugRay = false;

    private Button currentHoveredButton;
    private Selectable currentHoveredSelectable;
    private Vector3 crosshairDefaultScale;

    private void Start()
    {
        if (playerCamera == null)
        {
            playerCamera = Camera.main;
        }

        if (crosshairImage)
        {
            crosshairDefaultScale = crosshairImage.transform.localScale;
        }
    }

    private void Update()
    {
        if (GameManager.Instance.IsPlaying())
        {
            if (crosshairUI) crosshairUI.SetActive(false);
            return;
        }

        if (crosshairUI) crosshairUI.SetActive(true);

        PerformRaycast();
        HandleInput();
    }

    private void PerformRaycast()
    {

        Ray ray = playerCamera.ScreenPointToRay(new Vector3(Screen.width / 2f, Screen.height / 2f, 0f));

        if (showDebugRay)
        {
            Debug.DrawRay(ray.origin, ray.direction * raycastDistance, Color.red);
        }

    
        if (Physics.Raycast(ray, out RaycastHit hit, raycastDistance, uiLayerMask))
        {
       
            Selectable selectable = hit.collider.GetComponent<Selectable>();

            if (selectable != null && selectable.interactable)
            {
              
                if (currentHoveredSelectable != selectable)
                {
                   
                    if (currentHoveredSelectable != null)
                    {
                        OnHoverExit(currentHoveredSelectable);
                    }

                    currentHoveredSelectable = selectable;
                    currentHoveredButton = selectable as Button;
                    OnHoverEnter(currentHoveredSelectable);
                }
            }
            else
            {
                ClearHover();
            }
        }
        else
        {
            ClearHover();
        }
    }

    private void HandleInput()
    {
        if (Mouse.current?.leftButton.wasPressedThisFrame == true ||
            Gamepad.current?.aButton.wasPressedThisFrame == true ||
            Keyboard.current?.spaceKey.wasPressedThisFrame == true)
        {
            if (currentHoveredButton != null)
            {
                currentHoveredButton.onClick.Invoke();

                if (crosshairImage)
                {
                    crosshairImage.transform.DOPunchScale(Vector3.one * 0.2f, 0.2f);
                }
            }
            else if (currentHoveredSelectable != null)
            {
                ExecuteEvents.Execute(currentHoveredSelectable.gameObject,
                    new PointerEventData(EventSystem.current),
                    ExecuteEvents.submitHandler);
            }
        }
    }

    private void OnHoverEnter(Selectable selectable)
    {
        // Sélectionner dans l'EventSystem
        EventSystem.current.SetSelectedGameObject(selectable.gameObject);

        // Feedback visuel du crosshair
        if (crosshairImage)
        {
            crosshairImage.color = hoverColor;
            crosshairImage.transform.DOScale(crosshairDefaultScale * crosshairScaleOnHover, 0.1f);
        }

        var menuFeedback = selectable.GetComponent<MenuButtonFeedback>();
        if (menuFeedback)
        {
            menuFeedback.OnHoverEnter();
        }
    }

    private void OnHoverExit(Selectable selectable)
    {
        var menuFeedback = selectable.GetComponent<MenuButtonFeedback>();
        if (menuFeedback)
        {
            menuFeedback.OnHoverExit();
        }
    }

    private void ClearHover()
    {
        if (currentHoveredSelectable != null)
        {
            OnHoverExit(currentHoveredSelectable);
            currentHoveredSelectable = null;
            currentHoveredButton = null;
        }

        // Reset crosshair
        if (crosshairImage)
        {
            crosshairImage.color = defaultColor;
            crosshairImage.transform.DOScale(crosshairDefaultScale, 0.1f);
        }
    }
}