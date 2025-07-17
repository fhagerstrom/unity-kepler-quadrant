using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LaserObjectPool : MonoBehaviour
{
    public static LaserObjectPool instance;

    public GameObject laserBeam;
    public int laserPoolSize = 20;

    private List<GameObject> laserPool;

    // Awake is called before the first frame update
    void Awake()
    {
        // Singleton check
        if (instance == null)
        {
            instance = this;
            InitializePool(); // Initialize the object pool
        }
        else
        {
            Destroy(gameObject); // Destroy duplicates
        }
    }

    private void InitializePool()
    {
        laserPool = new List<GameObject>();

        // Instantiate lasers
        for (int i = 0; i < laserPoolSize; i++)
        {
            GameObject bullet = Instantiate(laserBeam);
            bullet.SetActive(false);
            laserPool.Add(bullet);
        }
    }

    public GameObject GetLaser()
    {
        foreach (GameObject laser in laserPool)
        {
            if (!laser.activeSelf)
            {
                laser.SetActive(true);
                return laser;
            }
        }

        // If no inactive lasers found, create new ones
        GameObject newLaser = Instantiate(laserBeam);
        laserPool.Add(newLaser);
        return newLaser;
    }

    public void ReturnToPool(GameObject laser)
    {
        laser.SetActive(false); // Deactivate the laser
    }

}
