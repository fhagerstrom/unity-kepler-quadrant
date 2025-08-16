using System.Runtime.CompilerServices;
using UnityEngine;

public class LaserProjectile : MonoBehaviour
{
    [SerializeField] private float speed = 100f;
    [SerializeField] private float lifeTime = 2f;
    [SerializeField] private int damageAmount = 10;

    [Tooltip("The tag of the object that fired this laser.")]
    [SerializeField] private string ownerTag;

    private float timer;
    private int passthroughLayer;

    private Vector3 direction = Vector3.forward;

    private void Awake()
    {
        // Cache the layer index
        passthroughLayer = LayerMask.NameToLayer("Passthrough");
    }

    private void OnEnable()
    {
        timer = 0f;
    }

    public void SetDirection(Vector3 dir)
    {
        direction = dir.normalized;
        transform.rotation = Quaternion.LookRotation(direction); 
    }

    // Update is called once per frame
    void Update()
    {
        UpdateLaser();
    }

    // Set owner tag of the laser. To distinguish who shot it. Prevents self-damage
    public void SetOwnerTag(string tag)
    {
        ownerTag = tag;
    }

    private void UpdateLaser()
    {
        transform.position += transform.forward * speed * Time.deltaTime; // Shoot projectile forward
        timer += Time.deltaTime; // Update internal timer

        // Return laser to object pool after life time is reached
        if (timer >= lifeTime)
        {
            ReturnToPool(); // Recycle
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // Check if the collided object has the same tag as the owner
        // Prevents the shooter from shooting themselves
        if (other.gameObject.CompareTag(ownerTag))
        {
            Debug.Log($"Laser from {ownerTag} ignored collision with its owner: {other.gameObject.name}");
            return;
        }

        // Check if the collided object is on the passthrough layer.
        // The laser will ignore it and continue flying.
        if (other.gameObject.layer == passthroughLayer)
        {
            return;
        }

        // Handle other collisions
        // PLAYER
        PlayerHealth playerHealth = other.GetComponent<PlayerHealth>();
        if (playerHealth != null)
        {
            playerHealth.TakeDamage(damageAmount);
        }

        // ENEMY
        EnemyHealth enemyHealth = other.GetComponent<EnemyHealth>();
        if (enemyHealth != null)
        {
            enemyHealth.TakeDamage(damageAmount);
        }

        // Always return the laser to the pool after hitting anything else
        ReturnToPool();

    }

    private void ReturnToPool()
    {
        if (LaserPool.instance != null)
        {
            LaserPool.instance.ReturnLaser(gameObject);
        }

        else
        {
            // Fallback if LaserPool instance is not found
            gameObject.SetActive(false);
            Debug.LogWarning("LaserPool.instance not found when trying to return laser. Deactivating directly.");
        }
    }
}
