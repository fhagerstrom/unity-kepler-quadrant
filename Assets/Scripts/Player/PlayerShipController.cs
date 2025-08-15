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
    [Header("References")]
    [SerializeField] private CinemachineSplineCart cart;
    [SerializeField] private CinemachineCamera onRailsCam;
    [SerializeField] private CinemachineCamera freeFlightCam;
    [SerializeField] private AimTargetController aimController;
    [SerializeField] private Transform pathFollower;
    [SerializeField] private Transform shipMesh;

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

    
    [Header("Visual Effects")]
    [Tooltip("The Z-offset of the ship mesh relative to the camera.")]
    [SerializeField] private float normalZOffset = 0f;
    [Tooltip("How far forward the ship moves when boosting.")]
    [SerializeField] private float boostZOffset = 1.0f;
    [Tooltip("How far back the ship moves when braking.")]
    [SerializeField] private float brakeZOffset = -0.5f;
    [Tooltip("How smoothly the ship moves to the target Z-offset.")]
    [SerializeField] private float positionTransitionSpeed = 5f;
    [SerializeField] private float normalFOV;
    [SerializeField] private float boostFOV = 60f;
    [SerializeField] private float brakeFOV = 35f;

    private float currentFOV;
    private float targetFOV;
    private float currentZOffset;
    private float targetZOffset;

    // ──────────────────────────────────────────────────────────────
    // Private vars

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
    private float currentSpeed;
    private float targetSpeed;
    private float speedVelocity; // For smoothDamp

    // Steering
    private Vector2 targetInput;
    private Vector2 currentSteerInput;

    // https://www.youtube.com/watch?v=wIkJvY96i8w
    private bool isRolling = false;
    private float rollTimer = 0;
    private float rollDuration = 0.4f;
    private float rollTargetAngle = 0f;
    private float currentRollAngle = 0f;

    // ──────────────────────────────────────────────────────────────

    private void Awake()
    {
        // New instances
        inputActions = new GameInputActions();
        flying = inputActions.Flying;
        cart = pathFollower.GetComponent<CinemachineSplineCart>();

        // inputActions bindings
        flying.Boost.performed += _ => OnBoostInput(true);
        flying.Boost.canceled += _ => OnBoostInput(false);
        flying.Brake.performed += _ => OnBrakeInput(true);
        flying.Brake.canceled += _ => OnBrakeInput(false);
        flying.Shoot.performed += _ => Shoot();
        flying.BarrelRollLeft.performed += _ => TriggerBarrelRoll(-1);
        flying.BarrelRollRight.performed += _ => TriggerBarrelRoll(1);
        flying.ToggleFlightMode.performed += _ => ToggleFlightMode();
    }

    private void OnEnable()
    {
        // Subscribe to the GameManager's pause events
        GameManager.OnGamePaused += inputActions.Disable;
        GameManager.OnGameResumed += inputActions.Enable;

        // Check if GameManager is already in a paused state. Enable / disable input actions
        if (GameManager.Instance != null)
        {
            if (GameManager.Instance.IsPaused)
            {
                inputActions.Disable();
            }
            else
            {
                inputActions.Enable();
            }
        }
        else
        {
            // If GameManager is null, it means we are in main menu or other non-gameplay scene.
            // Disable input by default to avoid unintended actions.
            inputActions.Disable();
        }
    }

    private void OnDisable()
    {
        // Unsubscribe from the events
        flying.Boost.performed -= _ => OnBoostInput(true);
        flying.Boost.canceled -= _ => OnBoostInput(false);
        flying.Brake.performed -= _ => OnBrakeInput(true);
        flying.Brake.canceled -= _ => OnBrakeInput(false);
        flying.Shoot.performed -= _ => Shoot();
        flying.BarrelRollLeft.performed -= _ => TriggerBarrelRoll(-1);
        flying.BarrelRollRight.performed -= _ => TriggerBarrelRoll(1);
        flying.ToggleFlightMode.performed -= _ => ToggleFlightMode();

        GameManager.OnGamePaused -= inputActions.Disable;
        GameManager.OnGameResumed -= inputActions.Enable;

        inputActions.Disable();

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
    }

    private void FixedUpdate()
    {
        // Apply speed to cart
        if (autoDolly is SplineAutoDolly.FixedSpeed fs)
            fs.Speed = currentSpeed;
    }

    // ──────────────────────────────────────────────────────────────
    // Core behaviour
    // ──────────────────────────────────────────────────────────────
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
        var leftLaser = LaserPool.instance.GetLaser();
        leftLaser.transform.position = firePointLeft.position;
        leftLaser.transform.rotation = firePointLeft.rotation;

        // Right Laser
        var rightLaser = LaserPool.instance.GetLaser();
        rightLaser.transform.position = firePointRight.position;
        rightLaser.transform.rotation = firePointRight.rotation;
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

    // ──────────────────────────────────────────────────────────────
    // Misc.
    // ──────────────────────────────────────────────────────────────
}
