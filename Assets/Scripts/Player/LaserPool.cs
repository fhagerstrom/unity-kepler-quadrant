using System.Collections.Generic;
using UnityEngine;

public class LaserPool : MonoBehaviour
{
    public static LaserPool instance;

    [SerializeField] private GameObject laserProjectile;
    [SerializeField] private int poolSize = 50;

    private Queue<GameObject> pool = new Queue<GameObject>();

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        InitializePool();
    }

    // Create and initialize the pool of projectiles
    private void InitializePool()
    {
        for (int i = 0; i < poolSize; i++)
        {
            // Instantiate new projectile.
            GameObject obj = Instantiate(laserProjectile, this.transform);

            // Deactivate it and add it to the queue.
            obj.SetActive(false);
            pool.Enqueue(obj);
        }
    }

    // Get available laser from pool
    public GameObject GetProjectile()
    {
        if (pool.Count > 0)
        {
            // If the pool is not empty, get the first object in the queue.
            GameObject obj = pool.Dequeue();
            obj.SetActive(true);
            return obj;
        }
        else
        {
            // If the pool is empty, instantiate a new projectile as a fallback.
            GameObject obj = Instantiate(laserProjectile, this.transform);
            Debug.LogWarning("Pool was empty, instantiated a new projectile. Consider increasing poolSize.");
            return obj;
        }
    }

    // Return to pool for reuse
    public void ReturnProjectile(GameObject obj)
    {
        obj.SetActive(false);
        pool.Enqueue(obj);
    }
}
