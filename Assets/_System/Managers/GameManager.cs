using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Game State")]
    [SerializeField] private GameState currentGameState = GameState.Paused;

    [Header("Cursor Settings")]
    [SerializeField] private bool hideCursorInGame = true;

    // Events
    public event System.Action OnGamePaused;
    public event System.Action OnGameResumed;
    public event System.Action OnGameEnded;

    public enum GameState
    {
        Paused, 
        Playing,
        GameOver
    }

    public GameState CurrentGameState => currentGameState;

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
    }

    private void Start()
    {
        InitializeGame();
    }

    private void Update()
    {
        if (Keyboard.current?.escapeKey.wasPressedThisFrame == true ||
            Gamepad.current?.startButton.wasPressedThisFrame == true)
        {
            TogglePause();
        }
    }

    private void InitializeGame()
    {
        SetGameState(GameState.Paused);
        Time.timeScale = 0f;
    }

    public void StartGame()
    {
        SetGameState(GameState.Playing);
        OnGameResumed?.Invoke();
    }

    public void PauseGame()
    {
        if (currentGameState == GameState.Playing)
        {
            SetGameState(GameState.Paused);
            OnGamePaused?.Invoke();
        }
    }

    public void ResumeGame()
    {
        if (currentGameState == GameState.Paused)
        {
            SetGameState(GameState.Playing);
            OnGameResumed?.Invoke();
        }
    }

    public void TogglePause()
    {
        if (currentGameState == GameState.Playing)
        {
            PauseGame();
        }
        else if (currentGameState == GameState.Paused)
        {
            ResumeGame();
        }
    }

    public void EndGame()
    {
        SetGameState(GameState.GameOver);
        OnGameEnded?.Invoke();
    }

    public void RestartGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
    }

    private void SetGameState(GameState newState)
    {
        currentGameState = newState;

        switch (newState)
        {
            case GameState.Paused:
                Time.timeScale = 0f;
                SetCursorState(true);
                UiManager.Instance?.ShowStartMenu();
                break;

            case GameState.Playing:
                Time.timeScale = 1f;
                SetCursorState(!hideCursorInGame);
                UiManager.Instance?.HideAllMenus();
                break;

            case GameState.GameOver:
                Time.timeScale = 0f;
                SetCursorState(true);
                UiManager.Instance?.ShowEndGameMenu();
                break;
        }
    }

    private void SetCursorState(bool visible)
    {
        Cursor.visible = visible;
        Cursor.lockState = visible ? CursorLockMode.None : CursorLockMode.Locked;
    }

    public bool IsPlaying() => currentGameState == GameState.Playing;
    public bool IsPaused() => currentGameState == GameState.Paused;
    public bool IsGameOver() => currentGameState == GameState.GameOver;
}