using System.Runtime.CompilerServices;
using UnityEngine;

public class LaserProjectile : MonoBehaviour
{
    [SerializeField] private float speed = 100f;
    [SerializeField] private float lifeTime = 2f;
    [SerializeField] private int damageAmount = 10;

    [Tooltip("The tag of the object that fired this laser.")]
    [SerializeField] private string ownerTag;

    private LaserPool currentPool;

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
    
    // Set owner tag of the laser. To distinguish who shot it. Prevents self-damage
    public void SetOwnerTag(string tag)
    {
        ownerTag = tag;
    }

    // Assigns the correct pool to this projectile when it's instantiated.
    public void SetPool(LaserPool pool)
    {
        currentPool = pool;
    }

    // Update is called once per frame
    void Update()
    {
        UpdateLaser();
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
        // Check for passthrough layer
        if (other.gameObject.layer == passthroughLayer)
        {
            // Do nothing if it's a passthrough collision
            return;
        }

        // Check for the owner tag, but only if the ownerTag variable is valid.
        if (!string.IsNullOrEmpty(ownerTag) && other.gameObject.CompareTag(ownerTag))
        {
            Debug.Log($"Laser from {ownerTag} ignored collision with its owner: {other.gameObject.name}");
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
        // Reset the state and return to the correct, assigned pool.
        ownerTag = string.Empty;
        if (currentPool != null)
        {
            currentPool.ReturnProjectile(gameObject);
        }
        else
        {
            gameObject.SetActive(false);
            Debug.LogWarning("Projectile has no assigned pool. Deactivating directly.");
        }
    }
}
