using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Laser : MonoBehaviour
{
    public float lifetime = 2f; // Lifetime of laser before returning to pool
    private void OnEnable()
    {
        Invoke("ReturnToPool", lifetime); // Invoke ReturnToPool after lifetime seconds
    }

    private void OnCollisionEnter(Collision collision)
    {
        // Return to pool when hitting something
        ReturnToPool();
    }

    private void ReturnToPool()
    {
        gameObject.SetActive(false); // Deactivate the laser
        //LaserObjectPool.instance.ReturnToPool(gameObject);
    }
}
