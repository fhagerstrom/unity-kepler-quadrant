using System;
using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    [SerializeField] private int maxHealth = 25;
    private int currentHealth;

    [SerializeField] private int scoreValue = 1;

    // Events
    public event Action<int, int> OnHealthChanged; // currentHealth, maxHealth
    public event Action OnDied; // When enemy is destroyed

    public int CurrentHealth => currentHealth;
    public int MaxHealth => maxHealth;

    private void Awake()
    {
        currentHealth = maxHealth;
    }

    public void TakeDamage(int damageAmount)
    {
        if (damageAmount < 0)
        {
            return;
        }

        currentHealth -= damageAmount;
        currentHealth = Mathf.Max(currentHealth, 0);

        Debug.Log($"{gameObject.name} took {damageAmount} damage. Current health: {currentHealth}");

        OnHealthChanged?.Invoke(currentHealth, maxHealth);

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        Debug.Log($"{gameObject.name} has been defeated!");
        OnDied?.Invoke(); // Notify listeners of death

        // Check if gamemanager exists
        if (GameManager.Instance != null)
        {
            // Add score to tally when enemy is killed
            GameManager.Instance.AddScore(scoreValue);
        }

        // TODO: Add explosion effects, sound, score, etc.
        gameObject.SetActive(false);
    }

    public void ResetHealth()
    {
        currentHealth = maxHealth;
        gameObject.SetActive(true);
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }
}
