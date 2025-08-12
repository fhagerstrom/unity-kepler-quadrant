using System.Runtime.CompilerServices;
using UnityEngine;

public class LaserProjectile : MonoBehaviour
{
    [SerializeField] private float speed = 100f;
    [SerializeField] private float lifeTime = 2f;
    [SerializeField] private int damageAmount = 10;

    private float timer;

    private Vector3 direction = Vector3.forward;

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

    // Collision logic
    private void OnCollisionEnter(Collision collision)
    {
        // TODO: Damage logic and effects
        HandleHit(collision.gameObject);

        // Deactivate on hit
        gameObject.SetActive(false);
        LaserPool.instance.ReturnLaser(gameObject); // Recycle
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

        // else if (hitObject.CompareTag("Environment"))
        // {
        //     Debug.Log("Hit environment!");
        // }

        else
        {
            // --- ADD THIS DEBUG LOG ---
            Debug.Log($"Laser hit {hitObject.name}, but no EnemyHealth script found on it or its direct children.", hitObject);
            // --- END ADDITION ---
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
