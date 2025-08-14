using System.Runtime.CompilerServices;
using UnityEngine;

public class LaserProjectile : MonoBehaviour
{
    [SerializeField] private float speed = 100f;
    [SerializeField] private float lifeTime = 2f;
    [SerializeField] private int damageAmount = 10;

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
        if (other.gameObject.layer != passthroughLayer)
        {
            // Valid hit, perform checks.
            // Check if the collided object has the "Enemy" tag.
            if (other.CompareTag("Enemy"))
            {
                HandleHit(other.gameObject);
            }

            // After all other checks, return the laser to the pool.
            // Ensures ReturnToPool() is only ever called once per hit.
            ReturnToPool();
        }
    }

    private void HandleHit(GameObject hitObject)
    {
        // Try to get the EnemyHealth component from the hit object
        EnemyHealth enemyHealth = hitObject.GetComponent<EnemyHealth>();
        if (enemyHealth != null)
        {
            // If it has EnemyHealth, apply damage
            enemyHealth.TakeDamage(damageAmount);
            Debug.Log($"Laser hit {hitObject.name} for {damageAmount} damage.");
        }

        else
        {
            Debug.Log($"Laser hit {hitObject.name}, but no EnemyHealth script found on it.", hitObject);
        }

        // Always return the laser to the pool after hitting anything
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
