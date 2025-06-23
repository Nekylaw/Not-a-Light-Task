using System;
using System.Linq;
using DG.Tweening;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.Serialization;

public class UiManager : MonoBehaviour
{

    public static UiManager Instance;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }

        ImmersiveCanvas.transform.position = MenuPoper.transform.position;
    }

    public MenuFeedback scriptFb;

    private void Update()
    {
        UIPlacement();

        if (EventSystem.current.currentSelectedGameObject == null)
        {
            if (StartGamePanel.activeSelf)
            {
                EventSystem.current.SetSelectedGameObject(playButton);
            }
            else if (OptionsPanel.activeSelf)
            {
                EventSystem.current.SetSelectedGameObject(masterSlider);
            }
        }
    }

    private void LateUpdate()
    {
        if (GameManager.Instance.gameState != GameManager.GameState.Playing)
        {
            var crossHair = new Vector3(Screen.width / 2f, Screen.height / 2f, 0f);
            var aimTargetRay = Camera.main.ScreenPointToRay(crossHair);

            if (!Physics.Raycast(aimTargetRay, out RaycastHit hit)) return;
            var hitObject = hit.collider.gameObject;
            var hitFeedback = hitObject.GetComponent<MenuFeedback>();

            if (hitFeedback == null) return;
            if (scriptFb == hitFeedback) return;

            scriptFb = hitFeedback;
            EventSystem.current.SetSelectedGameObject(scriptFb.gameObject);
        }
    }

    #region Serialize attributes
    [SerializeField] private GameObject StartGamePanel;
    [SerializeField] private GameObject EndGamePanel;
    [SerializeField] private GameObject OptionsPanel;
    [SerializeField] private GameObject playButton;
    [SerializeField] private GameObject masterSlider;

    [SerializeField] private GameObject ImmersiveCanvas;
    [SerializeField] private GameObject Player;
    [SerializeField] private GameObject MenuPoper;
    [SerializeField] private Camera MainCamera;
    #endregion

    #region PRIVATE ATTRIBUTES

    private GameObject _panelClosed;

    #endregion

    #region  PUBLIC METHODS

    public void UIStartGame()
    {
        StartGamePanel.SetActive(true);
        OptionsPanel.SetActive(false);
        EndGamePanel.SetActive(false);
    }

    public void UIPauseGame(GameObject panelToSave)
    {
        _panelClosed = panelToSave;

        OptionsPanel.SetActive(false);
        StartGamePanel.SetActive(false);
        EndGamePanel.SetActive(false);
        OptionsPanel.SetActive(true);

    }
    public void UIBack()
    {
        OptionsPanel.SetActive(false);
        _panelClosed.SetActive(true);
        EventSystem.current.SetSelectedGameObject(playButton);
    }

    public void UIEndGame()
    {
        EndGamePanel.SetActive(true);
    }

    public void UIPlacement()
    {
        ImmersiveCanvas.transform.DORotate(Player.transform.rotation.eulerAngles, 0.1f);

        if (CheckMenuVisibility())
            return;
        
        var pos = new Vector3(math.clamp(MainCamera.WorldToScreenPoint(ImmersiveCanvas.transform.position).x, 0, Screen.width), MainCamera.WorldToScreenPoint(new Vector3(0, 1, 6)).y, 6);
        ImmersiveCanvas.transform.DOMove(MainCamera.ScreenToWorldPoint(pos), 0.1f);

    }

    public void HideUI()
    {
        ImmersiveCanvas.SetActive(false);
    }

    public void ShowUI()
    {
        ImmersiveCanvas.SetActive(true);
    }

    #endregion

    private bool CheckMenuVisibility()
    {
        Plane[] planes = GeometryUtility.CalculateFrustumPlanes(MainCamera);
        return planes.All(plane => plane.GetDistanceToPoint(ImmersiveCanvas.transform.position) >= 0);
    }

}
