using UnityEngine;

public class HealthTester : MonoBehaviour
{
    [SerializeField] private PlayerHealth playerHealth; // Drag gamemanager object into here

    public void TestTakeDamage(int amount)
    {
        if (playerHealth != null)
        {
            playerHealth.TakeDamage(amount);
        }
        else
        {
            Debug.LogError("HealthSystem reference not set in HealthTester.");
        }
    }

    public void TestHeal(int amount)
    {
        if (playerHealth != null)
        {
            playerHealth.Heal(amount);
        }
        else
        {
            Debug.LogError("HealthSystem reference not set in HealthTester.");
        }
    }

    public void TestResetHealth()
    {
        if (playerHealth != null)
        {
            playerHealth.ResetHealth();
        }
        else
        {
            Debug.LogError("HealthSystem reference not set in HealthTester.");
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.L))
        {
            TestTakeDamage(10); // Take 10 damage on L key press
        }

        if (Input.GetKeyDown(KeyCode.H))
        {
            TestHeal(5); // Heal 5 health on H key press
        }

        if (Input.GetKeyDown(KeyCode.R))
        {
            TestResetHealth(); // Reset health on R key press
        }
    }
}
