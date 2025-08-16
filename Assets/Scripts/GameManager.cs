using UnityEngine;
using System;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using System.Collections;

public class GameManager : MonoBehaviour
{
    // Singleton
    public static GameManager Instance { get; private set; }

    [SerializeField] private GameInputActions gameInputActions;
    [SerializeField] private PlayerShipController playerShipController;
    [SerializeField] private HUDUIController hudUiController;
    [SerializeField] private float reloadDelay = 5.0f;


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
    private bool isPlayerDead = false;

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

        // Start the game in an unpaused state.
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
        hudUiController = FindFirstObjectByType<HUDUIController>();

        // Reset game state and counters for a new game.
        SetPauseState(false);
        ResetGameCounters();
        isPlayerDead = false;
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
            OnGamePaused?.Invoke();
            Debug.Log("Game state changed: PAUSED");
        }
        else
        {
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

    public void ReloadCurrentScene()
    {
        string currentSceneName = SceneManager.GetActiveScene().name;
        SceneManager.LoadScene(currentSceneName);
    }

    public void StartDeathSequence(GameObject playerShip)
    {

        // Check if the death sequence is already active to prevent re-entry.
        if (isPlayerDead)
        {
            Debug.Log("Death sequence is already active.");
            return;
        }

        // Set the flag to true to prevent further calls.
        isPlayerDead = true;

        // Get the player controller and handle the death state
        if (playerShip.TryGetComponent<PlayerShipController>(out var playerController))
        {
            playerController.HandleDeath(); // Call the new method to stop all movement
        }

        // Start the death sequence coroutine on this GameManager object.
        // Run it here in case the player ship might be destroyed.
        StartCoroutine(DeathSequence(playerShip));
    }

    /// <summary>
    /// Coroutine that handles the death animation, UI display, and scene reload.
    /// </summary>
    private IEnumerator DeathSequence(GameObject playerShip)
    {
        float animationDuration = 2.0f;
        float timer = 0f;

        // Animate player ships destruction
        while (timer < animationDuration)
        {
            if (playerShip != null)
            {
                // Spin the ship
                playerShip.transform.Rotate(Vector3.forward, 360f * Time.deltaTime);

                // Move the ship downwards and away
                playerShip.transform.position += Vector3.down * 5f * Time.deltaTime;
                playerShip.transform.position += Vector3.forward * 5f * Time.deltaTime;
            }
            timer += Time.deltaTime;
            yield return null; // Wait for the next frame
        }

        // Show the game over UI
        if (hudUiController != null)
        {
            hudUiController.ShowGameOverScreen();
        }

        // Wait for a delay before reloading the scene
        yield return new WaitForSeconds(reloadDelay);


        // Deactivate the game object after the scene reload is triggered.
        if (playerShip != null)
        {
            playerShip.SetActive(false);
            Destroy(playerShip);
        }

        // Reload the current scene to restart the game
        ReloadCurrentScene();
    }

}
