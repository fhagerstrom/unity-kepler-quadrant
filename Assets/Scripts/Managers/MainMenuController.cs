// MainMenuController.cs
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

public class MainMenuController : MonoBehaviour
{
    [SerializeField] private UIDocument mainMenuDoc;
    [SerializeField] private UIDocument optionsMenuDoc;

    private OptionsUIController optionsUIController;

    // Save level names into strings
    // private const string mainGameScene = "LevelSelectorScene";
    private const string practiceModeScene = "PracticeModeScene";
    // Add more scenes later on

    private void OnEnable()
    {
        if (mainMenuDoc == null)
        {
            Debug.LogError("Main menu is missing reference to UI document!");
        }

        // Initially hide the options menu if it exists
        if (optionsMenuDoc != null)
        {
            optionsMenuDoc.enabled = false;
        }

        VisualElement root = mainMenuDoc.rootVisualElement;

        // References to all buttons in menu
        Button mainGameBtn = root.Q<Button>("MainGameBtn");
        Button practiceModeBtn = root.Q<Button>("PracticeModeBtn");
        Button optionsBtn = root.Q<Button>("OptionsBtn");
        Button rankingsBtn = root.Q<Button>("RankingsBtn");
        Button creditsBtn = root.Q<Button>("CreditsBtn");
        Button quitBtn = root.Q<Button>("QuitBtn");

        // Using `clicked +=` syntax to add a method to the button's event
        mainGameBtn.clicked += OnMainGameClicked;
        practiceModeBtn.clicked += OnTrainingClicked;
        optionsBtn.clicked += OnOptionsClicked;
        rankingsBtn.clicked += OnRankingsClicked;
        creditsBtn.clicked += OnCreditsClicked;
        quitBtn.clicked += OnQuitClicked;
    }

    // Unsubscribe from events
    private void OnDisable()
    {
        if (mainMenuDoc == null)
        {
            return;
        }

        VisualElement root = mainMenuDoc.rootVisualElement;

        // References to all buttons in menu
        Button mainGameBtn = root.Q<Button>("MainGameBtn");
        Button practiceModeBtn = root.Q<Button>("PracticeModeBtn");
        Button optionsBtn = root.Q<Button>("OptionsBtn");
        Button rankingsBtn = root.Q<Button>("RankingsBtn");
        Button creditsBtn = root.Q<Button>("CreditsBtn");
        Button quitBtn = root.Q<Button>("QuitBtn");

        // Using `clicked +=` syntax to add a method to the button's event
        mainGameBtn.clicked -= OnMainGameClicked;
        practiceModeBtn.clicked -= OnTrainingClicked;
        optionsBtn.clicked -= OnOptionsClicked;
        rankingsBtn.clicked -= OnRankingsClicked;
        creditsBtn.clicked -= OnCreditsClicked;
        quitBtn.clicked -= OnQuitClicked;
    }

    // --- Button Click Handler Methods ---

    private void OnMainGameClicked()
    {
        Debug.Log("Loading Main Game...");
    }

    private void OnTrainingClicked()
    {
        Debug.Log("Loading Training Mode...");
        SceneManager.LoadScene(practiceModeScene);
    }

    private void OnOptionsClicked()
    {
        Debug.Log("Loading Options Menu...");

        // Disable the main menu UI and enable the options menu UI
        if (mainMenuDoc != null)
        {
            mainMenuDoc.enabled = false;
        }
        if (optionsMenuDoc != null)
        {
            optionsMenuDoc.enabled = true;

            // Get the OptionsUIController from the options menu UIDocument's GameObject
            optionsUIController = optionsMenuDoc.GetComponent<OptionsUIController>();
            if (optionsUIController != null)
            {
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
        if (mainMenuDoc != null)
        {
            mainMenuDoc.enabled = true;
        }

        // Unsubscribe from the back button's event
        if (optionsUIController != null)
        {
            optionsUIController.OnBackButtonClicked -= HideOptions;
        }
    }

    private void OnRankingsClicked()
    {
        Debug.Log("Loading Rankings...");
    }

    private void OnCreditsClicked()
    {
        Debug.Log("Loading Credits...");
    }

    private void OnQuitClicked()
    {
        Debug.Log("Quitting Game...");

        // Build only
        Application.Quit();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}
