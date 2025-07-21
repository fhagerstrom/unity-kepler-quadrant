using System.Runtime.CompilerServices;
using UnityEngine;

public class LaserProjectile : MonoBehaviour
{
    [SerializeField] private float speed = 100f;
    [SerializeField] private float lifeTime = 2f;

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
            gameObject.SetActive(false);
            LaserPool.instance.ReturnLaser(gameObject); // Recycle
        }
    }

    // Collision logic
    private void OnTriggerEnter(Collider other)
    {
        // TODO: Damage logic and effects


        // Deactivate on hit
        gameObject.SetActive(false);
        LaserPool.instance.ReturnLaser(gameObject); // Recycle
    }


}
