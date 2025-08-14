using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using UnityEngine.InputSystem;

public class PauseMenuController : MonoBehaviour
{
    [SerializeField] private UIDocument uiDocument;
    [SerializeField] private string mainMenuScene = "MainMenuScene";

    private VisualElement pauseMenuRoot;

    private GameInputActions gameInputActions;
    private GameInputActions.UIActions uiActions;

    private void Awake()
    {
        // Initialize input actions for the menu
        gameInputActions = new GameInputActions();
        uiActions = gameInputActions.UI;
    }

    private void OnEnable()
    {
        if (uiDocument == null)
        {
            Debug.LogError("Pause menu is missing reference to UI document!", this);
            return;
        }

        VisualElement root = uiDocument.rootVisualElement;

        pauseMenuRoot = root.Q("PauseMenuPanel");
        if (pauseMenuRoot == null)
        {
            Debug.LogError("PauseMenuPanel not found in the UI Document!", this);
            return;
        }

        // Subscribe to pause event
        uiActions.Pause.performed += OnPauseAction;

        // Hide the pause menu initially
        pauseMenuRoot.style.display = DisplayStyle.None;

        // Setup button click handlers
        Button resumeBtn = root.Q<Button>("ResumeBtn");
        Button restartBtn = root.Q<Button>("RestartBtn");
        Button optionsBtn = root.Q<Button>("OptionsBtn");
        Button mainMenuBtn = root.Q<Button>("MainMenuBtn");

        if (resumeBtn != null)
        {
            resumeBtn.clicked += ResumeGame;
        }

        if (restartBtn != null)
        {
            restartBtn.clicked += RestartGame;
        }

        if (optionsBtn != null)
        {
            optionsBtn.clicked += ShowOptions;
        }

        if (mainMenuBtn != null)
        {
            mainMenuBtn.clicked += ReturnToMainMenu;
        }

        // Enable UI action map.
        uiActions.Enable();
    }

    private void OnDisable()
    {
       
        // Unsubscribe from UI input action
        uiActions.Pause.performed -= OnPauseAction;
        // Disable UI action map to prevent stray inputs
        uiActions.Disable();

        if (uiDocument == null || uiDocument.rootVisualElement == null)
        {
            return;
        }

        VisualElement root = uiDocument.rootVisualElement;

        Button resumeBtn = root.Q<Button>("ResumeBtn");
        Button optionsBtn = root.Q<Button>("OptionsBtn");
        Button mainMenuBtn = root.Q<Button>("MainMenuBtn");
        Button quitBtn = root.Q<Button>("QuitBtn");

        if (resumeBtn != null)
        {
            resumeBtn.clicked -= ResumeGame;
        }

        if (optionsBtn != null)
        {
            optionsBtn.clicked -= ShowOptions;
        }

        if (mainMenuBtn != null)
        {
            mainMenuBtn.clicked -= ReturnToMainMenu;
        }

        if (quitBtn != null)
        {
            quitBtn.clicked -= QuitGame;
        }
    }

    private void OnPauseAction(InputAction.CallbackContext context)
    {
        // Toggle pause state
        if (GameManager.Instance != null)
        {
            GameManager.Instance.SetPauseState(!GameManager.Instance.IsPaused);

            // Show or hide menu based on new state
            if (GameManager.Instance.IsPaused)
            {
                pauseMenuRoot.style.display = DisplayStyle.Flex;
            }
            else
            {
                pauseMenuRoot.style.display = DisplayStyle.None;
            }
        }
    }

    public void ResumeGame()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.SetPauseState(false);
            pauseMenuRoot.style.display = DisplayStyle.None;
        }
    }

    public void RestartGame()
    {
        // First, ensure the game is unpaused before loading the new scene.
        Time.timeScale = 1f;
        GameManager.Instance.SetPauseState(false);

        // Get the name of the currently active scene and reload it.
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    private void ShowOptions()
    {
        Debug.Log("Showing Options Menu...");
        // TODO: Actually create an options menu.
        // Should use same as main menu options
    }

    private void ReturnToMainMenu()
    {
        Debug.Log("Returning to Main Menu...");
        SceneManager.LoadScene(mainMenuScene);
    }

    private void QuitGame()
    {
        Debug.Log("Quitting game...");
        Application.Quit();
        #if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
        #endif
    }
}
