using UnityEngine;

public class TurretAI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform playerTarget;
    [SerializeField] private Transform turretHead;
    [SerializeField] private Transform firePoint;
    [SerializeField] private LaserPool enemyLaserPool;

    [Header("Behaviour")]
    [SerializeField] private float detectionRadius = 25f;
    [SerializeField] private float fieldOfViewAngle = 90f;
    [SerializeField] private float rotationSpeed = 5f;
    [SerializeField] private float fireRate = 1f;

    [Header("Firing Accuracy")]
    [SerializeField] private float aimTolerance = 5f;

    private float fireTimer;
    private bool isPlayerInRange;
    private bool isPlayerInFOV;
    private bool hasLineOfSight;

    private void Start()
    {
        // Init stuff
        fireTimer = fireRate;

        if (playerTarget == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                playerTarget = player.transform;
            }
            else
            {
                Debug.LogWarning("TurretAI: Player not found!");
                enabled = false; // Disable the script if no player is found.
            }
        }
    }

    private void Update()
    {
        // Early return if no player target exists to prevent errors.
        if (playerTarget == null)
        {
            isPlayerInRange = false;
            isPlayerInFOV = false;
            hasLineOfSight = false;
            return;
        }

        // Check if the player is within the detection radius.
        float distanceToPlayer = Vector3.Distance(transform.position, playerTarget.position);
        isPlayerInRange = distanceToPlayer < detectionRadius;

        Vector3 directionToPlayer = (playerTarget.position - turretHead.position).normalized;
        float angleToPlayer = Vector3.Angle(turretHead.forward, directionToPlayer);

        // Check if the player is within the field of view.
        isPlayerInFOV = angleToPlayer < fieldOfViewAngle / 2f;

        // Check for line of sight
        RaycastHit hit;

        // The ray is cast from the turret's head towards the player.
        if (Physics.Raycast(turretHead.position, directionToPlayer, out hit, detectionRadius))
        {
            // If the ray hits something
            if (hit.transform == playerTarget)
            {
                // and that something is the player, we have line of sight.
                hasLineOfSight = true;
            }
            else
            {
                // Otherwise, something is blocking the view.
                hasLineOfSight = false;
            }
        }
        else
        {
            // If the ray hits nothing, it means the player is out of range.
            hasLineOfSight = false;
        }

        // The turret now only activates if all three conditions are met.
        if (isPlayerInRange && isPlayerInFOV && hasLineOfSight)
        {
            // Calculate the rotation needed to look at the player.
            Quaternion targetRotation = Quaternion.LookRotation(directionToPlayer);

            // Smoothly rotate the turret head towards the target.
            turretHead.rotation = Quaternion.Slerp(turretHead.rotation, targetRotation, rotationSpeed * Time.deltaTime);

            // Increment the fire timer.
            fireTimer += Time.deltaTime;

            // Check if it's time to fire and if the turret is aimed accurately enough.
            if (fireTimer >= fireRate)
            {
                float currentAngleFromAim = Vector3.Angle(turretHead.forward, directionToPlayer);
                if (currentAngleFromAim < aimTolerance)
                {
                    Shoot();
                    fireTimer = 0f;
                }
            }
        }
    }

    private void Shoot()
    {
        // Use the enemyLaserPool to get a pre-existing projectile instead of instantiating.
        GameObject newLaser = enemyLaserPool.GetProjectile();

        // Position and rotate the laser correctly.
        newLaser.transform.position = firePoint.position;
        newLaser.transform.rotation = firePoint.rotation;

        // Get laserprojectile component and set owner tag and pool reference.
        LaserProjectile laserScript = newLaser.GetComponent<LaserProjectile>();
        if (laserScript != null)
        {
            laserScript.SetOwnerTag("Enemy");
            // Tell the laser which pool it belongs to.
            laserScript.SetPool(enemyLaserPool);
        }
    }

    // Draw a sphere and rays in the scene to visualize the detection.
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);

        // Draw FOV cone.
        if (turretHead != null)
        {
            Gizmos.color = Color.yellow;

            // Calculate the directions of the cone edges.
            Vector3 leftRayDirection = Quaternion.Euler(0, -fieldOfViewAngle / 2, 0) * turretHead.forward;
            Vector3 rightRayDirection = Quaternion.Euler(0, fieldOfViewAngle / 2, 0) * turretHead.forward;

            // Draw the rays to visualize the cone edges.
            Gizmos.DrawRay(turretHead.position, leftRayDirection * detectionRadius);
            Gizmos.DrawRay(turretHead.position, rightRayDirection * detectionRadius);

            // Draw the line of sight raycast if we have a target.
            if (playerTarget != null)
            {
                Vector3 directionToPlayer = (playerTarget.position - turretHead.position).normalized;
                Gizmos.color = Color.green; // Green if line of sight is clear
                if (!hasLineOfSight)
                {
                    Gizmos.color = Color.gray; // Gray if line of sight is blocked
                }
                Gizmos.DrawRay(turretHead.position, directionToPlayer * detectionRadius);
            }
        }
    }
}
