using UnityEngine;
using UnityEngine.UIElements;
using System.Collections;
using UnityEngine.SceneManagement;
using System;

public class HUDUIController : MonoBehaviour
{
    [SerializeField] private UIDocument hudUiDoc;
    [SerializeField] private UIDocument deathScreenUiDoc;
    [SerializeField] private UIDocument winScreenUiDoc;
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private PlayerShipController playerShipController;

    private VisualElement healthBarFill;
    private VisualElement boostMeter;
    private VisualElement hudContainer;
    private Label healthTextLabel;
    private Label ringsTextLabel;
    private Label scoreTextLabel;

    private VisualElement winScreenPanel;
    private Label winTextLabel;
    private Label finalRingsTextLabel;
    private Label finalEnemiesTextLabel;
    private Label finalScoreTextLabel;

    // Buttons for the win screen
    private Button winRestartButton;
    private Button winQuitButton;

    // Buttons for the death screen
    private Button deathRestartButton;
    private Button deathQuitButton;

    [SerializeField] private string mainMenuScene = "MainMenuScene";

    private Color greenHealthColor = new Color();
    private Color yellowHealthColor = new Color();
    private Color redHealthColor = new Color();

    [SerializeField] private float fadeDuration = 0.5f; // Fade out the HUD on completion / death

    private void Start()
    {
        // Parse the hex codes into Color objects once.
        ColorUtility.TryParseHtmlString("#009900", out greenHealthColor);
        ColorUtility.TryParseHtmlString("#FFEC00", out yellowHealthColor);
        ColorUtility.TryParseHtmlString("#FF0000", out redHealthColor);
    }

    private void OnEnable()
    {
        if (hudUiDoc == null)
        {
            Debug.LogError("Missing UIDocument for HUDUIController!");
            return;
        }

        VisualElement root = hudUiDoc.rootVisualElement;

        hudContainer = root.Q<VisualElement>("Panel");
        if (hudContainer == null)
        {
            Debug.LogError("Panel not found in HUD.uxml. Please ensure a VisualElement with this name exists and contains all HUD elements.", this);
        }

        // Get UI element references
        healthBarFill = hudUiDoc.rootVisualElement.Q<VisualElement>("HealthBarFill");
        if (healthBarFill == null)
        {
            Debug.LogError("HealthBar visual not found.");
        }

        boostMeter = hudUiDoc.rootVisualElement.Q<VisualElement>("BoostMeterFill");
        if (boostMeter == null)
        {
            Debug.LogError("Boost meter visual not found.", this);
        }

        if (playerHealth != null)
        {
            playerHealth.OnHealthChanged += UpdateHealthUI;
            // Call once to set initial state in case OnEnable runs after Awake on playerHealth
            UpdateHealthUI(playerHealth.CurrentHealth, playerHealth.MaxHealth);
        }

        else
        {
            Debug.LogError("playerHealth is not assigned to HUDUIController.", this);
        }

        healthTextLabel = hudUiDoc.rootVisualElement.Q<Label>("HealthText");
        ringsTextLabel = hudUiDoc.rootVisualElement.Q<Label>("RingsText");

        if (ringsTextLabel == null)
        {
            Debug.LogError("RingsText not found in HUD.uxml. Check name in UI Builder.", this);
        }

        scoreTextLabel = hudUiDoc.rootVisualElement.Q<Label>("ScoreText");
        if (scoreTextLabel == null)
        {
            Debug.LogError("ScoreText not found in HUD.uxml. Check name in UI Builder.", this);
        }

        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnRingsPassedChanged += UpdateRingsPassedUI;
            GameManager.Instance.OnScoreChanged += UpdateScoreUI;

            // Call once to set UI state
            UpdateRingsPassedUI(GameManager.Instance.RingsPassed);
            UpdateScoreUI(GameManager.Instance.Hits);
        }

        else
        {
            Debug.LogError("GameManager Instance not found! Check if GameManager is initialized before", this);
        }

        if (deathScreenUiDoc != null)
        {
            // Hide death screen initially
            deathScreenUiDoc.rootVisualElement.style.display = DisplayStyle.None;

            // Find buttons
            deathRestartButton = deathScreenUiDoc.rootVisualElement.Q<Button>("RestartBtn");
            deathQuitButton = deathScreenUiDoc.rootVisualElement.Q<Button>("QuitBtn");

            deathRestartButton.clicked += RestartGame;
            deathQuitButton.clicked += ReturnToMainMenu;
        }

        if (winScreenUiDoc != null)
        {
            // Hide win screen initially
            winScreenUiDoc.rootVisualElement.style.display = DisplayStyle.None;

            // Get references to all the win screen elements
            winScreenPanel = winScreenUiDoc.rootVisualElement.Q<VisualElement>("WinScreenPanel");
            winTextLabel = winScreenUiDoc.rootVisualElement.Q<Label>("WinText");
            finalRingsTextLabel = winScreenUiDoc.rootVisualElement.Q<Label>("RingsPassedText");
            finalEnemiesTextLabel = winScreenUiDoc.rootVisualElement.Q<Label>("EnemiesDefeatedText");
            finalScoreTextLabel = winScreenUiDoc.rootVisualElement.Q<Label>("FinalScoreText");

            // Register click events
            winRestartButton = winScreenUiDoc.rootVisualElement.Q<Button>("RestartBtn");
            winQuitButton = winScreenUiDoc.rootVisualElement.Q<Button>("QuitBtn");

            if (winRestartButton != null)
            {
                winRestartButton.clicked += RestartGame;
            }

            if (winQuitButton != null)
            { 
                winQuitButton.clicked += ReturnToMainMenu; 
            }
        }

    }

    void OnDisable()
    {
        // Unsubscribe from the event
        if (playerHealth != null)
        {
            playerHealth.OnHealthChanged -= UpdateHealthUI;
        }

        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnRingsPassedChanged -= UpdateRingsPassedUI;
            GameManager.Instance.OnScoreChanged -= UpdateScoreUI;
        }

        if (deathScreenUiDoc != null)
        {
            deathRestartButton.clicked -= RestartGame;
            deathQuitButton.clicked -= ReturnToMainMenu;
        }

        if (winScreenUiDoc != null)
        {
            if (winRestartButton != null) winRestartButton.clicked -= RestartGame;
            if (winQuitButton != null) winQuitButton.clicked -= ReturnToMainMenu;
        }
    }

    void Update()
    {
        // Update the boost cooldown UI
        UpdateBoostCooldownUI();
    }

    /// <summary>
    /// Updates health bar fill width and text label based on current health.
    /// Called when OnHealthChanged event is invoked.
    /// </summary>
    private void UpdateHealthUI(int currentHealth, int maxHealth)
    {
        if (healthBarFill == null || healthTextLabel == null)
        {
            return;
        }

        // Calculate health percentage
        float healthPercentage = (float)currentHealth / maxHealth * 100f;

        // Set the width of the health bar fill
        healthBarFill.style.width = Length.Percent(healthPercentage);

        // Update the health text
        healthTextLabel.text = $"{currentHealth}/{maxHealth}";

        // Change color based on health (yellow below 50%, red below 25%)
        if (healthPercentage <= 25f)
        {
            healthBarFill.style.backgroundColor = new StyleColor(redHealthColor);
        }
        else if (healthPercentage <= 50f)
        {
            healthBarFill.style.backgroundColor = new StyleColor(yellowHealthColor);
        }
        else
        {
            healthBarFill.style.backgroundColor = new StyleColor(greenHealthColor);
        }
    }

    private void UpdateBoostCooldownUI()
    {
        if (boostMeter == null || playerShipController == null)
        {
            return;
        }

        // Get the current fuel ratio from the PlayerShipController (0 to 1)
        float fuelRatio = playerShipController.BoostFuelRatio;

        // Set width of the boost meter fill element
        boostMeter.style.width = Length.Percent(fuelRatio * 100f);

        // Change color to indicate when the fuel is full
        if (fuelRatio < 1.0f)
        {
            // Fuel is being used or recharging
            boostMeter.style.backgroundColor = new StyleColor(Color.grey);
        }
        else
        {
            // Fuel is full and ready to use
            boostMeter.style.backgroundColor = new StyleColor(Color.cyan);
        }
    }

    /// <summary>
    /// Updates the rings passed UI label.
    /// </summary>
    private void UpdateRingsPassedUI(int rings)
    {
        if (ringsTextLabel != null)
        {
            ringsTextLabel.text = $"Rings: {rings.ToString("D3")}"; // Format numbers as 000, 001 etc.
        }
    }

    /// <summary>
    /// Updates the score UI label.
    /// </summary>
    private void UpdateScoreUI(int hits)
    {
        if (scoreTextLabel != null)
        {
            scoreTextLabel.text = $"Hits: {hits.ToString("D3")}"; // Format numbers as 000, 001 etc.
        }
    }

    public void HideHUD()
    {
        StartCoroutine(FadeOutUI(hudUiDoc));
    }

    /// <summary>
    /// Show death screen UI.
    /// </summary>
    public void ShowDeathScreen(Action onCompleteAction)
    {
        // Immediately enable the death screen so it can fade in.
        if (deathScreenUiDoc != null)
        {
            deathScreenUiDoc.rootVisualElement.style.display = DisplayStyle.Flex;
            // Start the coroutine to smoothly fade in the death screen
            StartCoroutine(FadeInUI(deathScreenUiDoc, onCompleteAction));
        }
    }

    /// <summary>
    /// Show win screen UI with final stats and trigger a callback upon completion.
    /// </summary>
    public void ShowWinScreen(int ringsPassed, int enemiesDefeated, int finalScore, Action onCompleteAction)
    {
        if (winScreenUiDoc != null)
        {
            // Populate the labels with the final stats
            finalRingsTextLabel.text = $"Rings Passed: {ringsPassed.ToString()}";
            finalEnemiesTextLabel.text = $"Enemies Defeated: {enemiesDefeated.ToString()}";
            finalScoreTextLabel.text = $"Final Score: {finalScore.ToString()}";

            // Set the panel to display:flex before fading in
            winScreenUiDoc.rootVisualElement.style.display = DisplayStyle.Flex;

            // Start the coroutine to smoothly fade in the win screen
            // The game is already paused by the GameManager, so we don't need to do it here
            StartCoroutine(FadeInUI(winScreenUiDoc, onCompleteAction));
        }
    }

    private IEnumerator FadeOutUI(UIDocument uiDoc)
    {
        if (uiDoc == null)
        {
            yield break;
        }

        VisualElement root = uiDoc.rootVisualElement;
        float timer = 0f;

        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            float alpha = Mathf.Lerp(1f, 0f, timer / fadeDuration);
            root.style.opacity = alpha;
            yield return null;
        }

        // Ensure the opacity is exactly 0 and then hide the element
        root.style.opacity = 0f;
        root.style.display = DisplayStyle.None;
    }

    private IEnumerator FadeInUI(UIDocument uiDoc, Action onCompleteAction = null)
    {
        if (uiDoc == null)
        {
            yield break;
        }

        VisualElement root = uiDoc.rootVisualElement;
        float timer = 0f;

        // Make sure the element is transparent before fading in
        root.style.opacity = 0f;

        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            float alpha = Mathf.Lerp(0f, 1f, timer / fadeDuration);
            root.style.opacity = alpha;
            yield return null;
        }

        // Ensure the opacity is exactly 1 when done
        root.style.opacity = 1f;

        onCompleteAction?.Invoke();
    }

    public void RestartGame()
    {
        // First, ensure the game is unpaused before loading the new scene.
        Time.timeScale = 1f;
        GameManager.Instance.ResumeGame();

        GameManager.Instance.ReloadCurrentScene();
    }

    private void ReturnToMainMenu()
    {
        Debug.Log("Returning to Main Menu...");
        SceneManager.LoadScene(mainMenuScene);
    }
}
