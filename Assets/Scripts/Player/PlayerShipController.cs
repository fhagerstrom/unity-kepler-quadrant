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
    [SerializeField] private CinemachineSplineCart cart;     // on PathFollower
    [SerializeField] private CinemachineCamera onRailsCam;
    [SerializeField] private CinemachineCamera freeFlightCam;
    [SerializeField] private AimTargetController aimController;
    [SerializeField] private Transform pathFollower;
    [SerializeField] private Transform shipMesh;

    [Header("Movement")]
    [SerializeField] private float baseSpeed = 10f;
    [Tooltip("Added by boost; subtracted by brake")]
    [SerializeField] private float boostDelta = 10f;
    [SerializeField] private float brakeDelta = 5f;
    [SerializeField] private float steeringSpeed = 15f;

    [Header("Ship Mesh Tilting")]
    [SerializeField] private float maxRollAngle = 60f;
    [SerializeField] private float maxYawAngle = 45f;
    [SerializeField] private float maxPitchAngle = 35f;

    [Header("Laser")]
    [SerializeField] private Transform firePointLeft;
    [SerializeField] private Transform firePointRight;
    [SerializeField] private float fireCooldown = 0.1f;

    private float fireTimer;

    // ──────────────────────────────────────────────────────────────
    // Private vars

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
        inputActions = new GameInputActions();
        flying = inputActions.Flying;

        cart = pathFollower.GetComponent<CinemachineSplineCart>();

        // inputActions bindings
        flying.Boost.performed += _ => ChangeSpeed(+boostDelta);
        flying.Boost.canceled += _ => ChangeSpeed(-boostDelta);
        flying.Brake.performed += _ => ChangeSpeed(-brakeDelta);
        flying.Brake.canceled += _ => ChangeSpeed(+brakeDelta);
        flying.Shoot.performed += _ => Shoot();
        flying.BarrelRollLeft.performed += _ => TriggerBarrelRoll(-1);
        flying.BarrelRollRight.performed += _ => TriggerBarrelRoll(1);
        flying.ToggleFlightMode.performed += _ => ToggleFlightMode();
    }

    private void OnEnable() => inputActions.Enable();
    private void OnDisable() => inputActions.Disable();

    void Start()
    {
        if (cart.AutomaticDolly.Method is SplineAutoDolly.FixedSpeed fs)
        {
            autoDolly = fs;
            currentSpeed = baseSpeed;
            fs.Speed = currentSpeed;    // initialize speed once
        }
        else
        {
            Debug.LogError($"{name}: Cart’s Automatic Dolly is not set to FixedSpeed!");
            enabled = false;
            return;
        }
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

        UpdateBarrelRoll();
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
        //currentSteerInput = Vector2.Lerp(currentSteerInput, targetInput, steeringSpeed * Time.deltaTime);

        //// Calculate movement change based on input in relation to cinemachine path
        //transform.localPosition += new Vector3(currentSteerInput.x, currentSteerInput.y, 0f) * steeringSpeed * Time.deltaTime;

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

    private void ChangeSpeed(float delta)
    {
        currentSpeed = Mathf.Max(0, currentSpeed + delta);
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

        // Rotate to match direction (optional look target smoothing)
        Quaternion targetRot = Quaternion.Euler(
            -currentSteerInput.y * maxPitchAngle,
            currentSteerInput.x * maxYawAngle,
            -currentSteerInput.x * maxRollAngle
        );

        shipMesh.localRotation = Quaternion.Lerp(
            shipMesh.localRotation,
            targetRot,
            10f * Time.deltaTime
        );
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

    // ──────────────────────────────────────────────────────────────
    // Misc.
    // ──────────────────────────────────────────────────────────────
}
