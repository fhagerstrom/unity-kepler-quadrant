using System.Collections;
using System.Runtime.CompilerServices;
using Unity.Cinemachine;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Splines;
using UnityEngine.Windows;
using static Unity.Cinemachine.SplineAutoDolly;

[RequireComponent(typeof(PlayerInput))]
public class PlayerShipController : MonoBehaviour
{
    // ──────────────────────────────────────────────────────────────
    // Inspector vars
    [Header("Hierarchy")]
    [SerializeField] private CinemachineSplineCart cart;     // on PathFollower
    [SerializeField] private CinemachineCamera onRailsCam;
    [SerializeField] private CinemachineCamera freeFlightCam;
    [SerializeField] private Transform pathFollower;
    [SerializeField] private Transform aimTarget;
    [SerializeField] private Transform shipMesh;

    [Header("Movement")]
    [SerializeField] private float baseSpeed = 10f;
    [Tooltip("Added by boost; subtracted by brake")]
    [SerializeField] private float boostDelta = 10f;
    [SerializeField] private float brakeDelta = 5f;
    [SerializeField] private float steeringSpeed = 15f;

    [Header("Ship Tilt & Look")]
    [SerializeField] private float maxRollAngle = 60f;
    [SerializeField] private float maxYawAngle = 30f;
    [SerializeField] private float maxPitchAngle = 15f;
    [SerializeField] private float rotationSpeed = 340f;

    [Header("Laser")]
    [SerializeField] private ParticleSystem laserBeam;
    [SerializeField] private Transform fireLeft;
    [SerializeField] private Transform fireRight;

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
        // Update input
        targetInput = flying.Steer.ReadValue<Vector2>();
        targetInput.y *= -1f; // Invert vertical movement

        if (Mathf.Abs(currentSteerInput.x) < 0.001) 
            currentSteerInput.x = 0f;
        if (Mathf.Abs(currentSteerInput.y) < 0.001) 
            currentSteerInput.y = 0f;

        switch (currentMode)
        {
            case FlightMode.OnRails:
                UpdateOnRails();
                break;
            case FlightMode.FreeFlight:
                HandleFreeFlight();
                break;
        }

        UpdateBarrelRoll();
    }

    private void UpdateOnRails()
    {
        currentSteerInput = Vector2.Lerp(currentSteerInput, targetInput, steeringSpeed * Time.deltaTime);
        MoveShip();
        LookRotation();
        RotateShip();
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

    private void MoveShip()
    {
        // Calculate movement change based on input in relation to cinemachine path
        transform.localPosition += new Vector3(currentSteerInput.x, currentSteerInput.y, 0f) * steeringSpeed * Time.deltaTime;

        ClampShipPosition();
    }

    private void ClampShipPosition()
    {
        // Clamp position within camera viewport
        Vector3 position = Camera.main.WorldToViewportPoint(transform.position);
        position.x = Mathf.Clamp01(position.x);
        position.y = Mathf.Clamp01(position.y);
        transform.position = Camera.main.ViewportToWorldPoint(position);
    }

    private void LookRotation()
    {
        aimTarget.parent.position = Vector3.zero;
        aimTarget.localPosition = new Vector3(currentSteerInput.x, currentSteerInput.y, 1f);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(aimTarget.position), Mathf.Deg2Rad * rotationSpeed * Time.deltaTime);
    }

    private void RotateShip()
    {
        // Base steering rotation (pitch, yaw, lean)
        float pitch = -currentSteerInput.y * maxPitchAngle * 0.5f;
        float yaw = currentSteerInput.x * maxYawAngle * 0.5f;
        float lean = -currentSteerInput.x * maxRollAngle;

        // Combine rotation with current barrel roll angle
        Quaternion targetRotation = Quaternion.Euler(pitch, yaw, lean + currentRollAngle);
        shipMesh.localRotation = Quaternion.Lerp(shipMesh.localRotation, targetRotation, 15f * Time.deltaTime);
    }

    private void ChangeSpeed(float delta)
    {
        currentSpeed = Mathf.Max(0, currentSpeed + delta);
    }

    private void Shoot()
    {
        Debug.Log("Pew pew!");
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

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(aimTarget.position, .5f);
        Gizmos.DrawSphere(aimTarget.position, .15f);
    }
}
