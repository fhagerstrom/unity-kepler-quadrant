// PauseMenuController.cs
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using UnityEngine.InputSystem;

public class PauseMenuController : MonoBehaviour
{
    [SerializeField] private UIDocument pauseMenuDoc;
    [SerializeField] private UIDocument optionsMenuDoc;
    [SerializeField] private string mainMenuScene = "MainMenuScene";

    private VisualElement pauseMenuRoot;
    private OptionsUIController optionsUIController;

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
        if (pauseMenuDoc == null)
        {
            Debug.LogError("Pause menu is missing reference to UI document!", this);
            return;
        }

        VisualElement root = pauseMenuDoc.rootVisualElement;

        pauseMenuRoot = root.Q("PauseMenuPanel");
        if (pauseMenuRoot == null)
        {
            Debug.LogError("PauseMenuPanel not found in the UI Document!", this);
            return;
        }

        // Initially hide the options menu if it exists
        if (optionsMenuDoc != null)
        {
            optionsMenuDoc.enabled = false;
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
        uiActions.Disable();


        if (pauseMenuDoc == null || pauseMenuDoc.rootVisualElement == null)
        {
            return;
        }

        VisualElement root = pauseMenuDoc.rootVisualElement;

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
        // Only toggle if we're not currently in the options menu
        if (optionsMenuDoc != null && optionsMenuDoc.enabled) return;

        // Toggle pause state
        if (GameManager.Instance != null)
        {
            GameManager.Instance.TogglePauseState();

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
            GameManager.Instance.ResumeGame();
            pauseMenuRoot.style.display = DisplayStyle.None;
        }
    }

    public void RestartGame()
    {
        // Ensure the game is unpaused before reloading scene.
        Time.timeScale = 1f;
        GameManager.Instance.ResumeGame();

        GameManager.Instance.ReloadCurrentScene();
    }

    private void ShowOptions()
    {
        Debug.Log("Showing Options Menu...");

        // Disable the pause menu UI and enable the options menu UI
        if (pauseMenuDoc != null)
        {
            pauseMenuDoc.enabled = false;
        }
        if (optionsMenuDoc != null)
        {
            optionsMenuDoc.enabled = true;

            // Get the OptionsUIController from the options menu UIDocument's GameObject
            optionsUIController = optionsMenuDoc.GetComponent<OptionsUIController>();
            if (optionsUIController != null)
            {
                // Subscribe to the options menu's back button event
                optionsUIController.OnBackButtonClicked += HideOptions;
            }
        }
    }

    public void HideOptions()
    {
        if (optionsMenuDoc != null)
        {
            optionsMenuDoc.enabled = false;
        }
        if (pauseMenuDoc != null)
        {
            pauseMenuDoc.enabled = true;
        }

        // Unsubscribe from the back button's event
        if (optionsUIController != null)
        {
            optionsUIController.OnBackButtonClicked -= HideOptions;
        }
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
