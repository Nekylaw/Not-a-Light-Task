using System;
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

    #region Serialize attributes
    [SerializeField] private GameObject StartGamePanel;
    [SerializeField] private GameObject EndGamePanel;
    [SerializeField] private GameObject OptionsPanel;

    [SerializeField] private GameObject ImmersiveCanvas;
    [SerializeField] private GameObject Player;
    #endregion
    
    #region PRIVATE ATTRIBUTES
    
    private GameObject _panelClosed;
    
    #endregion
    
    #region  PUBLIC METHODS

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            ShowUI();
            UIPlacement();
        }
    }

    public void UIStartGame()
    {
        StartGamePanel.SetActive(false);
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
        ImmersiveCanvas.transform.position = Player.transform.position;
        ImmersiveCanvas.transform.rotation = Player.transform.rotation * Quaternion.Euler(new Vector3(0, 90, 0));
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
}
