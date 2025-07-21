using System.Collections.Generic;
using UnityEngine;

public class LaserPool : MonoBehaviour
{
    public static LaserPool instance;

    [SerializeField] private GameObject laserObject;
    [SerializeField] private int poolSize = 20;

    private Queue<GameObject> pool = new Queue<GameObject>();

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        if (instance == null)
        {
            instance = this;

            for (int i = 0; i < poolSize; i++)
            {
                GameObject obj = Instantiate(laserObject);
                obj.SetActive(false);
                pool.Enqueue(obj);
            }
        }

        else
        {
            Destroy(gameObject);
        }
    }

    public GameObject GetLaser()
    {
        if (pool.Count > 0)
        {
            GameObject obj = pool.Dequeue();
            obj.SetActive(true);
            return obj;
        }

        else
        {
            GameObject obj = Instantiate(laserObject);
            return obj;
        }
    }

    public void ReturnLaser(GameObject obj)
    {
        obj.SetActive(false);
        pool.Enqueue(obj);
    }
}
