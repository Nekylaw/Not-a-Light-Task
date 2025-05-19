using System;
using _System.Game_Manager;
using Game.Services.LightSources;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
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
    }
    
    [Header("Event System Selecion Firts")]
    [SerializeField] private GameObject playButton;
    [SerializeField] private GameObject backButton;
    
    private void Start()
    {
        gameState = GameState.StartMenu;
        
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = true;
        UiManager.Instance.ShowUI();
        UiManager.Instance.UIPlacement();
        EventSystem.current.SetSelectedGameObject(playButton);

        spawnTimer = spawnRate;
    }


    private void OnPause()
    {
        if (gameState == GameState.Playing)
        {
            gameState = GameState.Paused;
            UiManager.Instance.ShowUI();
            UiManager.Instance.UIStartGame();
            UiManager.Instance.UIPlacement();
            EventSystem.current.SetSelectedGameObject(playButton);
        }
        else if ( gameState == GameState.Paused)
        {
            UiManager.Instance.HideUI();
            gameState = GameState.Playing;
            EventSystem.current.SetSelectedGameObject(null);
        }
    }

    private void OnPress()
    {
        if (gameState != GameState.Playing)
        {
            Vector3 crossHair = new Vector3(Screen.width / 2f, Screen.height / 2f, 0f);
            var aimTargetRay = Camera.main.ScreenPointToRay(crossHair);

            if (Physics.Raycast(aimTargetRay, out RaycastHit hit))
            {
                if (hit.collider.gameObject.layer != LayerMask.NameToLayer("UI"))
                {
                    return;
                }
               
                GameObject hitObject = hit.collider.gameObject;
                
                var button = hitObject.GetComponent<UnityEngine.UI.Button>();
                if (button != null)
                {
                    button.onClick.Invoke();
                }
            }
        }
    }

    #region PUBLIC PROPERTIES

    public GameState gameState;
    public enum GameState
    {
        StartMenu,
        Paused,
        Playing,
        GameOver
    }
    #endregion
    
    #region PUBLIC METHODS
    
    public void StartGame()
    {
        UiManager.Instance.HideUI();
        Cursor.visible = false;
        gameState = GameState.Playing;
        EventSystem.current.SetSelectedGameObject(null);
    }

    public void PauseGame(GameObject panel)
    {
        UiManager.Instance.UIPauseGame(panel);
        Cursor.visible = true;
        
        EventSystem.current.SetSelectedGameObject(playButton);
        
        gameState = GameState.Paused;
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
        gameState = GameState.GameOver;
    }

    public void Back()
    {
        UiManager.Instance.UIBack();
        EventSystem.current.SetSelectedGameObject(backButton);
    }
    
    #endregion
    
    #region Creature Spawner

    [SerializeField]
    private float spawnRate;
    private float spawnTimer;
    
    private void SpawnTimer()
    {
        if (spawnTimer <= 0)
        {
            var _lightSources = GameObject.FindGameObjectsWithTag("LightSource");

            foreach (var VARIABLE in _lightSources)
            {
                var comp = VARIABLE.GetComponent<CreatureSpawner>();

                comp.CheckIfSpawn();
            }
            spawnTimer = spawnRate;
            
        }
        else
        {
            spawnTimer -= Time.deltaTime;
        }
    }
    
    #endregion
}
