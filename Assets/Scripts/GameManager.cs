using UnityEngine;
using System;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    // Singleton
    public static GameManager Instance { get; private set; }

    [SerializeField] public GameInputActions gameInputActions;

    public GameInputActions.FlyingActions FlyingActions { get; private set; }
    public GameInputActions.UIActions UIActions { get; private set; }

    private int ringsPassed = 0;
    public int RingsPassed
    {
        get => ringsPassed;
        private set
        {
            ringsPassed = value;
            OnRingsPassedChanged?.Invoke(ringsPassed);
        }
    }

    private int score = 0;
    public int Score
    {
        get => score;
        private set
        {
            score = value;
            OnScoreChanged?.Invoke(score);
        }
    }

    public event Action<int> OnRingsPassedChanged;
    public event Action<int> OnScoreChanged;

    public static event Action OnGamePaused;
    public static event Action OnGameResumed;

    public bool IsPaused { get; private set; }

    private void Awake()
    {
        // Singleton pattern
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Initialize the input actions once
        gameInputActions = new GameInputActions();
        FlyingActions = gameInputActions.Flying;
        UIActions = gameInputActions.UI;

        // Subscribe to the scene loaded event to reset the game state.
        SceneManager.sceneLoaded += OnSceneLoaded;

        // The game always starts unpaused, enable flying and disable UI.
        SetPauseState(false);
    }

    // Unsubscribe from event when object is destroyed.
    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    // Call whenever a new scene is loaded.
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Reset game state and counters for a new game.
        SetPauseState(false);
        ResetGameCounters();
    }

    public void SetPauseState(bool isPaused)
    {
        if (this.IsPaused == isPaused)
        {
            return;
        }

        this.IsPaused = isPaused;
        Time.timeScale = isPaused ? 0 : 1;

        if (isPaused)
        {
            FlyingActions.Disable();
            UIActions.Enable();

            OnGamePaused?.Invoke();
            Debug.Log("Game state changed: PAUSED");
        }
        else
        {
            FlyingActions.Enable();
            UIActions.Disable();

            OnGameResumed?.Invoke();
            Debug.Log("Game state changed: RESUMED");
        }
    }

    public void AddRing(int amount = 1)
    {
        if (amount < 0) return;
        RingsPassed += amount;
        Debug.Log($"Rings passed: {RingsPassed}");
    }

    public void AddScore(int amount = 1)
    {
        if (amount < 0) return;
        Score += amount;
        Debug.Log($"Hits: {Score}");
    }

    public void ResetGameCounters()
    {
        RingsPassed = 0;
        Score = 0;
        Debug.Log("Game counters reset.");
    }
}
