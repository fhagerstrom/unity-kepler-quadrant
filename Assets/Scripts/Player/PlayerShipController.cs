// PlayerShipController.cs
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Splines;
using static Unity.Cinemachine.SplineAutoDolly;

[RequireComponent(typeof(PlayerInput))]
public class PlayerShipController : MonoBehaviour
{
    // ──────────────────────────────────────────────────────────────
    // Inspector vars
    // ──────────────────────────────────────────────────────────────
    [Header("References")]
    [SerializeField] private CinemachineSplineCart cart;
    [SerializeField] private CinemachineCamera onRailsCam;
    [SerializeField] private CinemachineCamera freeFlightCam;
    [SerializeField] private AimTargetController aimController;
    [SerializeField] private Transform pathFollower;
    [SerializeField] private Transform shipMesh;
    [SerializeField] private LaserPool playerLaserPool;

    [Header("Movement")]
    [SerializeField] private float baseSpeed = 5f;
    [Tooltip("Added by boost; subtracted by brake")]
    [SerializeField] private float boostDelta = 10f;
    [SerializeField] private float brakeDelta = 5f;
    [SerializeField] private float steeringSpeed = 15f;
    [SerializeField] private float speedSmoothTime = 0.2f; // Time for the speed to smooth between value

    [Header("Boost/Brake Fuel")]
    [Tooltip("How long the fuel lasts when boosting/braking")]
    [SerializeField] private float fuelDuration = 2.0f;
    [Tooltip("How quickly the fuel recharges when not in use")]
    [SerializeField] private float fuelRecoveryRate = 1.0f;
    [SerializeField] private float rechargeDelay = 0.4f;
    private float currentBoostFuel;
    private float rechargeCooldownTimer;
    private bool isBoosting;
    private bool isBraking;

    [Header("Ship Mesh Tilting")]
    [SerializeField] private float maxRollAngle = 60f;
    [SerializeField] private float maxYawAngle = 45f;
    [SerializeField] private float maxPitchAngle = 35f;

    [Header("Laser")]
    [SerializeField] private Transform firePointLeft;
    [SerializeField] private Transform firePointRight;
    [SerializeField] private float fireCooldown = 0.1f;
    private float fireTimer;

    [Header("Boost / Brake Effects")]
    [SerializeField] private float normalZOffset = 0f;
    [SerializeField] private float boostZOffset = 1.0f;
    [SerializeField] private float brakeZOffset = -0.5f;
    [SerializeField] private float positionTransitionSpeed = 5f;
    [SerializeField] private float normalFOV;
    [SerializeField] private float boostFOV = 60f;
    [SerializeField] private float brakeFOV = 35f;


    // ──────────────────────────────────────────────────────────────
    // Private vars
    // ──────────────────────────────────────────────────────────────
    private float currentFOV;
    private float targetFOV;
    private float currentZOffset;
    private float targetZOffset;

    private float currentSpeed;
    private float targetSpeed;
    private float speedVelocity; // For smoothDamp

    public float BoostFuelRatio => currentBoostFuel / fuelDuration;

    private enum FlightMode
    {
        OnRails,
        FreeFlight
    }

    private FlightMode currentMode = FlightMode.OnRails;

    private GameInputActions inputActions;
    private GameInputActions.FlyingActions flying;

    private ISplineAutoDolly autoDolly;

    // Steering
    private Vector2 targetInput;
    private Vector2 currentSteerInput;

    // https://www.youtube.com/watch?v=wIkJvY96i8w
    private bool isRolling = false;
    private float rollTimer = 0;
    private float rollDuration = 0.4f;
    private float rollTargetAngle = 0f;
    private float currentRollAngle = 0f;

    private bool hasCompletedMission = false;

    // ──────────────────────────────────────────────────────────────


    private void Awake()
    {
        // New instances
        inputActions = new GameInputActions();
        flying = inputActions.Flying;
        cart = pathFollower.GetComponent<CinemachineSplineCart>();

        // inputActions bindings
        flying.Boost.performed += ctx => OnBoostInput(true);
        flying.Boost.canceled += ctx => OnBoostInput(false);
        flying.Brake.performed += ctx => OnBrakeInput(true);
        flying.Brake.canceled += ctx => OnBrakeInput(false);
        flying.Shoot.performed += ctx => Shoot();
        flying.BarrelRollLeft.performed += ctx => TriggerBarrelRoll(-1);
        flying.BarrelRollRight.performed += ctx => TriggerBarrelRoll(1);
        flying.ToggleFlightMode.performed += ctx => ToggleFlightMode();
    }

    private void OnEnable()
    {
        // Subscribe to the GameManager's pause events
        GameManager.OnGamePaused += OnGamePaused;
        GameManager.OnGameResumed += OnGameResumed;

        // Check if GameManager is already in a paused state. Enable / disable input actions
        if (GameManager.Instance != null)
        {
            if (GameManager.Instance.IsPaused)
            {
                OnGamePaused();
            }
            else
            {
                OnGameResumed();
            }
        }
        else
        {
            // If GameManager is null, it means we are in main menu or other non-gameplay scene.
            // Disable input by default
            inputActions.Disable();
        }
    }

    private void OnDisable()
    {
        // Unsubscribe from the events
        GameManager.OnGamePaused -= OnGamePaused;
        GameManager.OnGameResumed -= OnGameResumed;
    }

    private void OnDestroy()
    {
        // Properly unbind input actions
        flying.Boost.performed -= ctx => OnBoostInput(true);
        flying.Boost.canceled -= ctx => OnBoostInput(false);
        flying.Brake.performed -= ctx => OnBrakeInput(true);
        flying.Brake.canceled -= ctx => OnBrakeInput(false);
        flying.Shoot.performed -= ctx => Shoot();
        flying.BarrelRollLeft.performed -= ctx => TriggerBarrelRoll(-1);
        flying.BarrelRollRight.performed -= ctx => TriggerBarrelRoll(1);
        flying.ToggleFlightMode.performed -= ctx => ToggleFlightMode();

        inputActions.Dispose();
    }


    void Start()
    {
        if (cart.AutomaticDolly.Method is SplineAutoDolly.FixedSpeed fs)
        {
            autoDolly = fs;
            currentSpeed = baseSpeed;
            targetSpeed = baseSpeed;
            fs.Speed = currentSpeed;     // initialize speed once
        }
        else
        {
            Debug.LogError($"{name}: Cart’s Automatic Dolly is not set to FixedSpeed!");
            enabled = false;
            return;
        }

        currentBoostFuel = fuelDuration;

        // Initialize Z-offset to the normal value
        currentZOffset = normalZOffset;
        targetZOffset = normalZOffset;
        shipMesh.localPosition = new Vector3(0, 0, currentZOffset);

        // initialize FOV values
        normalFOV = onRailsCam.Lens.FieldOfView;
        currentFOV = normalFOV;
        targetFOV = normalFOV;
    }

    // ──────────────────────────────────────────────────────────────
    // Update loops
    // ──────────────────────────────────────────────────────────────

    private void Update()
    {
        // Advance laser cooldown
        fireTimer += Time.deltaTime;

        // Handle movement & tilt by mode
        if (currentMode == FlightMode.OnRails)
        {
            HandleOnRails();
        }
        else
        {
            HandleFreeFlight();
        }

        // Decrement the cooldown timer each frame.
        if (rechargeCooldownTimer > 0)
        {
            rechargeCooldownTimer -= Time.deltaTime;
        }

        HandleBoostAndBrakeFuel();
        UpdateBarrelRoll();

        // Smoothly move towards the target speed
        currentSpeed = Mathf.SmoothDamp(currentSpeed, targetSpeed, ref speedVelocity, speedSmoothTime);

        UpdateZOffset();
        UpdateFovEffects();

        // Check for mission completion on every frame
        CheckForMissionComplete();
    }

    private void FixedUpdate()
    {
        // Apply speed to cart
        if (autoDolly is SplineAutoDolly.FixedSpeed fs)
            fs.Speed = currentSpeed;
    }

    private void CheckForMissionComplete()
    {
        // We only want this to run once, so we check our flag.
        if (hasCompletedMission)
        {
            return;
        }

        // Check if the player has reached the end of the spline.
        if (cart.SplinePosition >= cart.Spline.CalculateLength())
        {
            Debug.Log("Mission Complete! Final spline position reached.");

            // Set the flag to true to prevent this from running again.
            hasCompletedMission = true;

            // Stop all player controls and movement.
            DisableMovement();

            // Trigger the mission complete sequence in the GameManager.
            if (GameManager.Instance != null)
            {
                GameManager.Instance.StartCompleteSequence();
            }
        }
    }


    // ──────────────────────────────────────────────────────────────
    // Core behaviour
    // ──────────────────────────────────────────────────────────────
    private void OnGamePaused()
    {
        inputActions.Disable();
    }

    private void OnGameResumed()
    {
        inputActions.Enable();
    }

    private void HandleOnRails()
    {
        // Read the aimController offset for strafing the ship
        Vector2 offset2D = aimController.CloseTargetOffset;

        // Normalize to [-1, 1]
        float normalizedX = offset2D.x / aimController.maxRadius;
        float normalizedY = offset2D.y / aimController.maxRadius;

        // Apply strafing movement inside the viewport
        Vector3 strafe = new Vector3(normalizedX, normalizedY, 0f)
                             * steeringSpeed
                             * Time.deltaTime;

        transform.localPosition += strafe;

        ClampShipPosition();
        TiltShipMesh(new Vector2(normalizedX, normalizedY));
    }

    private void ClampShipPosition()
    {
        // Clamp position within camera viewport
        Vector3 position = Camera.main.WorldToViewportPoint(transform.position);
        position.x = Mathf.Clamp01(position.x);
        position.y = Mathf.Clamp01(position.y);
        transform.position = Camera.main.ViewportToWorldPoint(position);
    }

    private void TiltShipMesh(Vector2 steering)
    {
        // Base steering rotation (pitch, yaw, lean)
        float pitch = -steering.y * maxPitchAngle;
        float yaw = steering.x * maxYawAngle;
        float roll = -steering.x * maxRollAngle + currentRollAngle;

        // Combine rotation with current barrel roll angle
        Quaternion targetRotation = Quaternion.Euler(pitch, yaw, roll);
        shipMesh.localRotation = Quaternion.Lerp(shipMesh.localRotation, targetRotation, 15f * Time.deltaTime);
    }

    private void OnBoostInput(bool isPressed)
    {
        // If fuel is full (ratio is 1.0), We're allowed to brake
        if (isPressed && BoostFuelRatio >= 1.0f)
        {
            isBoosting = true;
            isBraking = false;
            targetSpeed = baseSpeed + boostDelta;
        }
        else
        {
            isBoosting = false;
            if (!isBraking)
            {
                targetSpeed = baseSpeed;
            }
        }
    }

    private void OnBrakeInput(bool isPressed)
    {
        // If fuel is full (ratio is 1.0), We're allowed to brake
        if (isPressed && BoostFuelRatio >= 1.0f)
        {
            isBraking = true;
            isBoosting = false;
            targetSpeed = baseSpeed - brakeDelta;
        }
        else
        {
            isBraking = false;
            if (!isBoosting)
            {
                targetSpeed = baseSpeed;
            }
        }
    }

    private void HandleBoostAndBrakeFuel()
    {
        // Drain fuel if boosting or braking
        if ((isBoosting || isBraking) && currentBoostFuel > 0)
        {
            currentBoostFuel -= Time.deltaTime;

            rechargeCooldownTimer = rechargeDelay;

            // If fuel runs out while holding, stop the boost/brake
            if (currentBoostFuel <= 0)
            {
                isBoosting = false;
                isBraking = false;
                targetSpeed = baseSpeed;
                currentBoostFuel = 0;
            }

            // Check cooldown timer before allowing boost gauge to recover
            else if (rechargeCooldownTimer <= 0)
            {
                currentBoostFuel = Mathf.Min(fuelDuration, currentBoostFuel + Time.deltaTime * fuelRecoveryRate);
            }

        }
        // Recover fuel if not boosting or braking
        else if (!isBoosting && !isBraking)
        {
            currentBoostFuel = Mathf.Min(fuelDuration, currentBoostFuel + Time.deltaTime * fuelRecoveryRate);
        }
    }

    private void Shoot()
    {
        Debug.Log("Pew pew!");

        if (fireTimer < fireCooldown)
            return;

        fireTimer = 0f;

        // Left laser
        var leftLaser = playerLaserPool.GetProjectile();
        leftLaser.transform.position = firePointLeft.position;
        leftLaser.transform.rotation = firePointLeft.rotation;

        // Set the owner tag for the left laser
        if (leftLaser.TryGetComponent<LaserProjectile>(out var leftLaserScript))
        {
            leftLaserScript.SetOwnerTag("Player");
            leftLaserScript.SetPool(playerLaserPool);
        }

        // Right Laser
        var rightLaser = playerLaserPool.GetProjectile();
        rightLaser.transform.position = firePointRight.position;
        rightLaser.transform.rotation = firePointRight.rotation;

        if (rightLaser.TryGetComponent<LaserProjectile>(out var rightLaserScript))
        {
            rightLaserScript.SetOwnerTag("Player");
            rightLaserScript.SetPool(playerLaserPool);
        }
    }

    private void TriggerBarrelRoll(int direction)
    {
        if (isRolling) return;

        isRolling = true;
        rollTimer = 0f;

        // Target full 360° rotation relative to current Z rotation
        rollTargetAngle = currentRollAngle + 360f * -direction;
    }

    private void UpdateBarrelRoll()
    {
        if (!isRolling) return;

        rollTimer += Time.deltaTime;
        float t = Mathf.Clamp01(rollTimer / rollDuration);

        // Smooth the roll interpolation
        float smoothedT = Mathf.SmoothStep(0f, 1f, t);
        currentRollAngle = Mathf.Lerp(0f, rollTargetAngle, smoothedT);

        if (t >= 1f)
        {
            isRolling = false;
            currentRollAngle = 0f;
        }
    }

    private void EnterFreeFlight()
    {
        currentMode = FlightMode.FreeFlight;
        cart.enabled = false;
    }

    private void HandleFreeFlight()
    {
        targetInput = flying.Steer.ReadValue<Vector2>();
        targetInput.y *= -1f;

        currentSteerInput = Vector2.Lerp(currentSteerInput, targetInput, steeringSpeed * Time.deltaTime);

        // Move forward
        transform.position += transform.forward * currentSpeed * Time.deltaTime;

        // Steer X/Y
        transform.position += transform.right * currentSteerInput.x * currentSpeed * 0.5f * Time.deltaTime;
        transform.position += transform.up * currentSteerInput.y * currentSpeed * 0.5f * Time.deltaTime;

        // Rotate to match direction with look target smoothing
        Quaternion targetRot = Quaternion.Euler(-currentSteerInput.y * maxPitchAngle, currentSteerInput.x * maxYawAngle, -currentSteerInput.x * maxRollAngle);

        shipMesh.localRotation = Quaternion.Lerp(shipMesh.localRotation, targetRot, 10f * Time.deltaTime);
    }

    private void ReturnToRails()
    {
        currentMode = FlightMode.OnRails;

        // Reattach to spline
        cart.enabled = true;
    }

    public void ToggleFlightMode()
    {
        if (currentMode == FlightMode.OnRails)
        {
            onRailsCam.Priority = 1;
            freeFlightCam.Priority = 2;
            EnterFreeFlight();
        }
        else
        {
            onRailsCam.Priority = 2;
            freeFlightCam.Priority = 1;
            ReturnToRails();
        }
    }

    private void UpdateZOffset()
    {
        // Determine the target Z-offset based on boost/brake state.
        if (isBoosting)
        {
            targetZOffset = boostZOffset;
        }
        else if (isBraking)
        {
            targetZOffset = brakeZOffset;
        }
        else
        {
            // When not boosting or braking, return to the normal offset.
            targetZOffset = normalZOffset;
        }

        // Smoothly transition the current Z-offset towards the target.
        currentZOffset = Mathf.Lerp(currentZOffset, targetZOffset, positionTransitionSpeed * Time.deltaTime);

        // Apply the new local Z-position to the ship's mesh.
        shipMesh.localPosition = new Vector3(0, 0, currentZOffset);
    }

    private void UpdateFovTargets()
    {
        if (isBoosting)
        {
            targetFOV = boostFOV;
        }
        else if (isBraking)
        {
            targetFOV = brakeFOV;
        }
        else
        {
            targetFOV = normalFOV;
        }
    }

    private void UpdateFovEffects()
    {
        UpdateFovTargets();

        // Smoothly transition the current FOV towards the target
        currentFOV = Mathf.Lerp(currentFOV, targetFOV, positionTransitionSpeed * Time.deltaTime);

        // Apply the new FOV to the camera
        onRailsCam.Lens.FieldOfView = currentFOV;
    }

    public void DisableMovement()
    {
        // Ensure the cart is not null before attempting to disable it
        if (cart != null)
        {
            cart.enabled = false;
        }

        // Also stop the player input and other movement-related logic
        this.enabled = false;

        // Find and disable the player's collider to prevent further hits
        Collider playerCollider = GetComponent<Collider>();
        if (playerCollider != null)
        {
            playerCollider.enabled = false;
        }

    }
}
