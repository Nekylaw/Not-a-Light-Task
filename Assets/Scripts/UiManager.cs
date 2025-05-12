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
    [SerializeField] private GameObject InGamePanel;
    #endregion
    
    #region PRIVATE ATTRIBUTES
    
    private GameObject _panelClosed;
    
    #endregion
    
    #region  PUBLIC METHODS 
    
    public void UIStartGame()
    {
        StartGamePanel.SetActive(false);
        InGamePanel.SetActive(true);
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

    #endregion
}
