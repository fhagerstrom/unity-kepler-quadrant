using UnityEngine;

public class RingTracker : MonoBehaviour
{
    [SerializeField] private GameObject ringPrefab;
    [SerializeField] private float passDelay = 0.1f;

    private bool ringPassed = false;

    private void OnTriggerEnter(Collider other)
    {
        // Check if entering collider belongs to player ship AND if the ring hasn't been passed yet
        if (other.CompareTag("Player") && !ringPassed)
        {
            ringPassed = true;

            Debug.Log("Ring passed!");

            if (GameManager.Instance != null)
            {
                GameManager.Instance.AddRing();
            }
            else
            {
                Debug.LogWarning("GameManager instance not found. Cannot add ring count.");
            }

            Invoke(nameof(DeactivateRing), passDelay);
        }
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
