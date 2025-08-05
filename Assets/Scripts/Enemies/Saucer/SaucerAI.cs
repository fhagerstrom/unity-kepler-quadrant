using UnityEngine;

public class SaucerAI : MonoBehaviour
{
    [Header("Hover Settings")]
    [SerializeField] private float hoverHeight = 1.0f; // How high saucer should move up and down
    [SerializeField] private float hoverSpeed = 1.5f; // How fast the saucer should hover
    [SerializeField] private float hoverOffset = 0f; // Random var for hovering out of sync on multiple saucers

    [Header("Rotation Settings")]
    [SerializeField] private float rotationSpeedY = 90.0f;

    private Vector3 startPosition; // Initial spawn position

    private void Awake()
    {
        startPosition = transform.position; // Store initial world position
        hoverOffset = Random.Range(0f, 100f); // Random offset to hover so multiple saucers dont hover in sync
    }

    // Update is called once per frame
    void Update()
    {
        // For now, just make it hover up and down for target practice
        float hoverY = startPosition.y + Mathf.Sin((Time.time + hoverOffset) * hoverSpeed) * hoverHeight;
        transform.position = new Vector3(startPosition.x, hoverY, startPosition.z);

        // Constant Y rotation
        transform.Rotate(Vector3.up, rotationSpeedY * Time.deltaTime, Space.Self); // Local axis
    }

    public void ResetMovement()
    {
        startPosition = transform.position;
        hoverOffset = Random.Range(0f, 100f);
    }
}
