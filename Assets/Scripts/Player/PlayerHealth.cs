using UnityEngine;
using System;

public class PlayerHealth : MonoBehaviour
{

    [SerializeField] private int maxHealth = 100;
    private int currentHealth;

    public int CurrentHealth => currentHealth; // Get currentHealth between scripts
    public int MaxHealth => maxHealth; // Get maxHealth between scripts

    // Notify listeners when health changes
    public event Action<int, int> OnHealthChanged; // current, max

    void Awake()
    {
        currentHealth = maxHealth;
        // Invoke event to set initial UI state
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    /// <summary>
    /// Reduces the current health by a specified amount.
    /// </summary>
    public void TakeDamage(int damageAmount)
    {
        if (damageAmount < 0) // Damage shouldnt be negative
        {
            return;
        }

        currentHealth -= damageAmount;
        currentHealth = Mathf.Max(currentHealth, 0); // Health shouldnt be negative

        Debug.Log($"Took {damageAmount} damage. Current health: {currentHealth}");

        // Health has changed, notify listeners
        OnHealthChanged?.Invoke(currentHealth, maxHealth);

        if (currentHealth < 0)
        {
            Die();
        }
    }

    /// <summary>
    /// Increases the current health by a specified amount.
    /// </summary>
    public void Heal(int healAmount)
    {
        if (healAmount < 0) // Heal shouldnt be negative
        {
            return;
        }

        currentHealth += healAmount;
        currentHealth = Mathf.Min(currentHealth, maxHealth); // Health shouldnt exceed max

        Debug.Log($"Healed {healAmount} health. Current health: {currentHealth}");

        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    /// <summary>
    ///  Handle player death logic
    /// </summary>
    public void Die()
    {
        Debug.Log("Player is dead!");

        // TODO: Implement death logic (animation or w/e)
        // If (extraLives > 0), restart level. If (extraLives == 0), game over
    }

    // Reset health. For testing purposes.
    public void ResetHealth()
    {
        currentHealth = maxHealth;
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
        Debug.Log("Health reset.");
    }
}
