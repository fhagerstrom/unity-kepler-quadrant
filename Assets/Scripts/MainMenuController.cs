using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

public class MainMenuController : MonoBehaviour
{
    [SerializeField] private UIDocument mainMenuDoc;

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
        // SceneManager.LoadScene(mainGameScene);
    }

    private void OnTrainingClicked()
    {
        Debug.Log("Loading Training Mode...");
        SceneManager.LoadScene(practiceModeScene);
    }

    private void OnOptionsClicked()
    {
        Debug.Log("Loading Options Menu...");
        // Load a new scene or simply show a different VisualElement
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
