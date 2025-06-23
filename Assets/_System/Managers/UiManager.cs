using UnityEngine;
using UnityEngine.EventSystems;
using DG.Tweening;
using UnityEngine.InputSystem;

public class UiManager : MonoBehaviour
{
    public static UiManager Instance { get; private set; }

    [Header("UI Panels")]
    [SerializeField] private GameObject startPanel;
    [SerializeField] private GameObject optionsPanel;
    [SerializeField] private GameObject endGamePanel;

    [Header("World Space UI Settings")]
    [SerializeField] private Transform worldSpaceCanvas;
    [SerializeField] private float distanceFromCamera = 5f;
    [SerializeField] private float heightOffset = 1.5f;
    [SerializeField] private float minHeightFromGround = 1f;
    [SerializeField] private float smoothTime = 0.2f;
    [SerializeField] private LayerMask groundLayers = -1;

    [Header("Menu Boundaries")]
    [SerializeField] private float horizontalMargin = 0.1f; 
    [SerializeField] private float verticalMargin = 0.1f;

    [Header("First Selected Objects")]
    [SerializeField] private GameObject startPanelFirstSelected;
    [SerializeField] private GameObject optionsPanelFirstSelected;
    [SerializeField] private GameObject endGamePanelFirstSelected; 

    [Header("Menu Animation")]
    [SerializeField] private float menuFadeInDuration = 0.3f;
    [SerializeField] private float menuScaleInDuration = 0.2f;

    private Camera mainCamera;
    private GameObject currentActivePanel;
    private Vector3 targetPosition;
    private Vector3 currentVelocity;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }

        mainCamera = Camera.main;
        if (mainCamera == null)
        {
            Debug.LogError("Main Camera not found!");
        }
    }

    private void Start()
    {
        HideAllMenus();

        if (worldSpaceCanvas)
        {
            worldSpaceCanvas.localScale = Vector3.one; 
        }

        ShowStartMenu();
    }

    private void LateUpdate()
    {
        if (worldSpaceCanvas && worldSpaceCanvas.gameObject.activeSelf && mainCamera)
        {
            UpdateMenuPosition();

            Vector3 lookDirection = mainCamera.transform.position - worldSpaceCanvas.position;
            lookDirection.y = 0; 
            if (lookDirection != Vector3.zero)
            {
                worldSpaceCanvas.rotation = Quaternion.LookRotation(-lookDirection);
            }
        }

        MaintainUISelection();
    }

    private void UpdateMenuPosition()
    {
        Vector3 cameraForward = mainCamera.transform.forward;
        cameraForward.y = 0;
        cameraForward.Normalize();

        Vector3 basePosition = mainCamera.transform.position + cameraForward * distanceFromCamera;
        basePosition.y = mainCamera.transform.position.y + heightOffset;

        if (Physics.Raycast(basePosition + Vector3.up * 10f, Vector3.down, out RaycastHit hit, 50f, groundLayers))
        {
            float groundHeight = hit.point.y + minHeightFromGround;
            if (basePosition.y < groundHeight)
            {
                basePosition.y = groundHeight;
            }
        }

        Vector3 screenPos = mainCamera.WorldToViewportPoint(basePosition);
        screenPos.x = Mathf.Clamp(screenPos.x, horizontalMargin, 1f - horizontalMargin);
        screenPos.y = Mathf.Clamp(screenPos.y, verticalMargin, 1f - verticalMargin);

        targetPosition = mainCamera.ViewportToWorldPoint(new Vector3(screenPos.x, screenPos.y, distanceFromCamera));

        // Smooth movement
        worldSpaceCanvas.position = Vector3.SmoothDamp(
            worldSpaceCanvas.position,
            targetPosition,
            ref currentVelocity,
            smoothTime
        );
    }

    private void MaintainUISelection()
    {
        if (EventSystem.current == null) return;

        if (EventSystem.current.currentSelectedGameObject == null && currentActivePanel != null)
        {
            GameObject toSelect = null;

            if (currentActivePanel == startPanel)
                toSelect = startPanelFirstSelected;
            else if (currentActivePanel == optionsPanel)
                toSelect = optionsPanelFirstSelected;
            else if (currentActivePanel == endGamePanel)
                toSelect = endGamePanelFirstSelected;

            if (toSelect != null && toSelect.activeInHierarchy)
            {
                EventSystem.current.SetSelectedGameObject(toSelect);
            }
        }
    }

    public void ShowStartMenu()
    {
        ShowPanel(startPanel, startPanelFirstSelected);
    }

    public void ShowOptionsMenu()
    {
        ShowPanel(optionsPanel, optionsPanelFirstSelected);
    }

    public void ShowEndGameMenu()
    {
        ShowPanel(endGamePanel, endGamePanelFirstSelected);
    }

    public void HideAllMenus()
    {
        if (worldSpaceCanvas)
        {
            worldSpaceCanvas.DOScale(0f, menuScaleInDuration)
                .OnComplete(() => worldSpaceCanvas.gameObject.SetActive(false));
        }

        startPanel?.SetActive(false);
        optionsPanel?.SetActive(false);
        endGamePanel?.SetActive(false);

        currentActivePanel = null;
        EventSystem.current?.SetSelectedGameObject(null);
    }

    private void ShowPanel(GameObject panel, GameObject firstSelected)
    {
        if (panel == null) return;

        // Hide all panels first
        startPanel?.SetActive(false);
        optionsPanel?.SetActive(false);
        endGamePanel?.SetActive(false);

        // Show the canvas
        if (worldSpaceCanvas && !worldSpaceCanvas.gameObject.activeSelf)
        {
            worldSpaceCanvas.gameObject.SetActive(true);
            worldSpaceCanvas.localScale = Vector3.zero;
            worldSpaceCanvas.DOScale(0.1f, menuScaleInDuration).SetUpdate(true);
        }

        // Show the specific panel
        panel.SetActive(true);
        currentActivePanel = panel;

        // Animate panel elements
        CanvasGroup canvasGroup = panel.GetComponent<CanvasGroup>();
        if (canvasGroup)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.DOFade(1f, menuFadeInDuration).SetUpdate(true);
        }

        if (firstSelected && EventSystem.current)
        {
            EventSystem.current.SetSelectedGameObject(firstSelected);
        }
    }

    public void BackFromOptions()
    {
        ShowStartMenu();
    }

    public void OnPlayButtonClicked()
    {
        GameManager.Instance.ResumeGame(); 
    }

    public void OnResumeButtonClicked()
    {
        GameManager.Instance.ResumeGame();
    }

    public void OnRestartButtonClicked()
    {
        GameManager.Instance.RestartGame();
    }

    public void OnOptionsButtonClicked()
    {
        ShowOptionsMenu();
    }

    public void OnQuitButtonClicked()
    {
        GameManager.Instance.QuitGame();
    }
}