using System;
using _System.Game_Manager;
using Game.Services.LightSources;
using Unity.VisualScripting;
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
    }

    private void Start()
    {
        gameState = GameState.StartMenu;
        
        Cursor.lockState = CursorLockMode.Confined;
        Cursor.visible = true;
        UiManager.Instance.ShowUI();
        UiManager.Instance.UIPlacement();

        spawnTimer = spawnRate;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape) && gameState == GameState.Playing)
        {
            UiManager.Instance.ShowUI();
            UiManager.Instance.UIPlacement();
            Cursor.lockState = CursorLockMode.Confined;
            Cursor.visible = true;
            gameState = GameState.Paused;
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
        Cursor.lockState = CursorLockMode.Locked;
        gameState = GameState.Playing;
    }

    public void PauseGame(GameObject panel)
    {
        UiManager.Instance.UIPauseGame(panel);
        Cursor.visible = true;
        
        Cursor.lockState = CursorLockMode.Confined;
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
        Cursor.lockState = CursorLockMode.Confined;
        gameState = GameState.GameOver;
    }

    public void Back()
    {
        UiManager.Instance.UIBack();
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
