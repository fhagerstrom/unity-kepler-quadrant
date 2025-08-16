using UnityEngine;
using UnityEngine.UIElements;

public class HUDUIController : MonoBehaviour
{
    [SerializeField] private UIDocument hudUiDoc; // HUDUI.uxml
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private PlayerShipController playerShipController;

    private VisualElement healthBarFill;
    private VisualElement boostMeter;
    private VisualElement gameOverScreen;
    private Label healthTextLabel;
    private Label ringsTextLabel;
    private Label scoreTextLabel;

    private Color greenHealthColor = new Color();
    private Color yellowHealthColor = new Color();
    private Color redHealthColor = new Color();

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

        // Get UI element references
        healthBarFill = hudUiDoc.rootVisualElement.Q<VisualElement>("HealthBarFill");
        if(healthBarFill == null)
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
            UpdateScoreUI(GameManager.Instance.Score);
        }

        else
        {
            Debug.LogError("GameManager Instance not found! Check if GameManager is initialized before", this);
        }

        gameOverScreen = hudUiDoc.rootVisualElement.Q<VisualElement>("gameOverScreen");
        if (gameOverScreen == null)
        {
            Debug.LogError("Game Over Screen not found in HUD.uxml. Check name in UI Builder.", this);
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
    private void UpdateScoreUI(int score)
    {
        if (scoreTextLabel != null)
        {
            scoreTextLabel.text = $"Hits: {score.ToString("D3")}"; // Format numbers as 000, 001 etc.
        }
    }

    /// <summary>
    /// Show Game Over UI.
    /// </summary>
    public void ShowGameOverScreen()
    {
        if (gameOverScreen != null)
        {
            gameOverScreen.style.display = DisplayStyle.Flex;
        }
    }
}
