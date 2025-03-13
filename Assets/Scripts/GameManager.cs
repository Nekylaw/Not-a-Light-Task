using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

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
        Time.timeScale = 0;
        
        Cursor.lockState = CursorLockMode.Confined;
    }
    
    #region PRIVATE ATTRIBUTES
    
    #endregion
    
    #region PUBLIC METHODS

    public void StartGame()
    {
        UiManager.Instance.UIStartGame();
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
        Time.timeScale = 1;
    }

    public void PauseGame(GameObject panel)
    {
        UiManager.Instance.UIPauseGame(panel);
        Cursor.visible = true;
        
        Cursor.lockState = CursorLockMode.Confined;
        Time.timeScale = 0;
    }

    public void QuitGame()
    {
        Application.Quit();
    }

    public void Reset()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void EndGame()
    {
        UiManager.Instance.UIEndGame();
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.Confined;
        Time.timeScale = 0;
    }

    public void Back()
    {
        UiManager.Instance.UIBack();
    }
    
    #endregion
    
}
