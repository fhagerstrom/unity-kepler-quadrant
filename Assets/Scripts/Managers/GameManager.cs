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
    [SerializeField] private ReticleController reticleController;

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

    private int hits = 0;
    public int Hits
    {
        get => hits;
        private set
        {
            hits = value;
            OnScoreChanged?.Invoke(hits);
        }
    }

    private int finalScore = 0;
    public int FinalScore
    {
        get => finalScore;
        private set
        {
            finalScore = value;
            OnScoreChanged?.Invoke(finalScore);
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

    // Call whenever a new scene is loaded
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Find instances of objects for initialization
        hudUiController = FindFirstObjectByType<HUDUIController>();
        reticleController = FindFirstObjectByType<ReticleController>();

        // Reset game state and counters
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

    public void TogglePauseState()
    {
        SetPauseState(!IsPaused);
    }

    public void ResumeGame()
    {
        SetPauseState(false);
    }

    private void PauseGame()
    {
        SetPauseState(true);
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
        Hits += amount;
        Debug.Log($"Hits: {Hits}");
    }

    public void ResetGameCounters()
    {
        RingsPassed = 0;
        Hits = 0;
        Debug.Log("Game counters reset.");
    }

    public void ReloadCurrentScene()
    {
        string currentSceneName = SceneManager.GetActiveScene().name;
        SceneManager.LoadScene(currentSceneName);
    }

    /// <summary>
    /// Starts the mission complete sequence, which displays the win screen and then pauses the game.
    /// </summary>
    public void StartCompleteSequence()
    {
        Debug.Log("Game has been won! Starting win sequence...");
        // Start the coroutine to handle the sequence.
        StartCoroutine(CompleteSequence());
    }

    private IEnumerator CompleteSequence()
    {
        // Hide the HUD and reticle first.
        if (hudUiController != null)
        {
            hudUiController.HideHUD();
        }
        if (reticleController != null)
        {
            reticleController.HideReticles();
        }

        // Wait for a short duration to allow the fade-out animation to complete.
        // You might need to adjust this value based on your fade animation duration.
        yield return new WaitForSeconds(0.5f);

        // Show the win screen with the final stats, then pause the game.
        if (hudUiController != null)
        {
            hudUiController.ShowWinScreen(RingsPassed, Hits, Hits, PauseGame);
        }

        // We can also disable the player ship controller here to prevent any unintended movement
        // or actions after the game is won, in case the player somehow retains control.
        if (playerShipController != null)
        {
            playerShipController.DisableMovement();
        }
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
            playerController.DisableMovement(); // Stop all movement
        }

        // Immediately start the HUD fade-out.
        if (hudUiController != null)
        {
            hudUiController.HideHUD();
        }

        // Immediately hide the reticles.
        if (reticleController != null)
        {
            reticleController.HideReticles();
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

        // Deactivate player object
        if (playerShip != null)
        {
            playerShip.SetActive(false);
            Destroy(playerShip);
        }

        // Show the game over UI, and then pause the game after the UI fade in is complete
        if (hudUiController != null)
        {
            hudUiController.ShowDeathScreen(PauseGame);
        }
    }
}
