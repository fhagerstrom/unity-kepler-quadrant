using UnityEngine;
using UnityEngine.UIElements;

public class HealthUIController : MonoBehaviour
{
    [SerializeField] private UIDocument uIDocument; // HealthUI.uxml
    [SerializeField] private PlayerHealth playerHealth; // PlayerHealth script

    private VisualElement healthBarFill;
    private Label healthTextLabel;

    private void OnEnable()
    {
        if (uIDocument == null)
        {
            Debug.LogError("Missing UIDocument for HealthUIController!");
            return;
        }

        // Get UI element references
        healthBarFill = uIDocument.rootVisualElement.Q<VisualElement>("HealthBarFill");
        healthTextLabel = uIDocument.rootVisualElement.Q<Label>("HealthText");

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
            Debug.LogError("playerHealth is not assigned to HealthUIController.", this);
        }
    }

    void OnDisable()
    {
        // Unsubscribe from the event to prevent memory leaks
        if (playerHealth != null)
        {
            playerHealth.OnHealthChanged -= UpdateHealthUI;
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

        // Set the width of the health bar fill using UI Toolkit styling
        // Length.Percent creates a percentage-based length
        healthBarFill.style.width = Length.Percent(healthPercentage);

        // Update the health text
        healthTextLabel.text = $"{currentHealth}/{maxHealth}";

        // Change color based on health (e.g., yellow below 50%, red below 25%)
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


}
