using UnityEngine;
using UnityEngine.UIElements;

public class HUDUIController : MonoBehaviour
{
    [SerializeField] private UIDocument hudUiDoc; // HUDUI.uxml
    [SerializeField] private PlayerHealth playerHealth;

    private VisualElement healthBarFill;
    private Label healthTextLabel;

    // Rings UI Elements
    private Label ringsPassedLabel;

    // Score UI Elements
    private Label scoreLabel;

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
        ringsPassedLabel = hudUiDoc.rootVisualElement.Q<Label>("RingsPassedLabel");
        
        if (ringsPassedLabel == null)
        {
            Debug.LogError("RingsPassedLabel not found in HUD.uxml. Check name in UI Builder.", this);
        }

        scoreLabel = hudUiDoc.rootVisualElement.Q<Label>("ScoreLabel");
        if (scoreLabel == null)
        {
            Debug.LogError("ScoreLabel not found in HUD.uxml. Check name in UI Builder.", this);
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



    /// <summary>
    /// Updates health bar fill width and text label based on current health.
    /// Called when OnHealthChanged event is invoked.
    /// </summary>
    private void UpdateHealthUI(int currentHealth, int maxHealth)
    {
        if (healthBarFill == null || healthTextLabel == null) return;

        // Calculate health percentage
        float healthPercentage = (float)currentHealth / maxHealth * 100f;

        // Set the width of the health bar fill
        healthBarFill.style.width = Length.Percent(healthPercentage);

        // Update the health text
        healthTextLabel.text = $"{currentHealth}/{maxHealth}";

        // Change color based on health (yellow below 50%, red below 25%)
        if (healthPercentage <= 25f)
        {
            healthBarFill.style.backgroundColor = new StyleColor(Color.red);
        }

        else if (healthPercentage <= 50f)
        {
            healthBarFill.style.backgroundColor = new StyleColor(Color.yellow);
        }

        else
        {
            healthBarFill.style.backgroundColor = new StyleColor(Color.green);
        }
    }

    /// <summary>
    /// Updates the rings passed UI label.
    /// </summary>
    private void UpdateRingsPassedUI(int rings)
    {
        if (ringsPassedLabel != null)
        {
            ringsPassedLabel.text = $"Rings: {rings.ToString("D3")}"; // Format numbers as 000, 001 etc.
        }
    }

    /// <summary>
    /// Updates the score UI label.
    /// </summary>
    private void UpdateScoreUI(int score)
    {
        if (scoreLabel != null)
        {
            scoreLabel.text = $"Hits: {score.ToString("D3")}"; // Format numbers as 000, 001 etc.
        }
    }
}
