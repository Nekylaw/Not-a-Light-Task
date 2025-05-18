using System;
using System.Linq;
using DG.Tweening;
using TMPro;
using UnityEditor;
using UnityEngine;

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
    }

    private void Update()
    {
        if (!CheckMenuVisibility())
        {
            UIPlacement();
        }
    }

    #region Serialize attributes
    [SerializeField] private GameObject StartGamePanel;
    [SerializeField] private GameObject EndGamePanel;
    [SerializeField] private GameObject OptionsPanel;

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
        
        OptionsPanel.SetActive(true);
        StartGamePanel.SetActive(false);
        EndGamePanel.SetActive(false);
    }

    public void UIEndGame()
    {
        EndGamePanel.SetActive(true);
    }

    public void UIBack()
    {
        OptionsPanel.SetActive(false);
        _panelClosed.SetActive(true);
    }

    public void UIPlacement()
    {
        var pos = new Vector3(MenuPoper.transform.position.x, MenuPoper.transform.position.y + 1.8f, MenuPoper.transform.position.z);
        ImmersiveCanvas.transform.DOMove(pos, 0.5f);
        ImmersiveCanvas.transform.DORotate(Player.transform.rotation.eulerAngles, 0.5f);
        
        //ImmersiveCanvas.transform.position = MenuPoper.transform.position;
        //ImmersiveCanvas.transform.rotation = Player.transform.rotation;
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
