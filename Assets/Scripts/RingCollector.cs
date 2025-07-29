using UnityEngine;

public class RingCollector : MonoBehaviour
{
    [SerializeField] private GameObject ringPrefab;
    [SerializeField] private float passDelay = 0.1f;

    private bool ringPassed = true;

    private void OnTriggerEnter(Collider other)
    {
        // Check if entering collider belongs to player ship.
        // PlayerShip should have Player tag
        if (other.CompareTag("Player") && !ringPassed)
        {
            ringPassed = true;

            Debug.Log("Ring passed!");
        }

        if (GameManager.Instance != null)
        {
            GameManager.Instance.AddRing();
        }

        else
        {
            Debug.LogWarning("GameManager instance not found.");
        }

        Invoke(nameof(DeactivateRing), passDelay);

    }

    private void DeactivateRing()
    {
        gameObject.SetActive(false);
    }

    public void ResetRing()
    {
        ringPassed = false;
        gameObject.SetActive(true);
    }
}
