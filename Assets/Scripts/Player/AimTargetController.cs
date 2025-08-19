using UnityEngine;
using System;
using UnityEngine.InputSystem;

public class AimTargetController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform shipTransform;
    [SerializeField] private Transform closeTarget;
    [SerializeField] private Transform farTarget;

    [Header("Settings")]
    [SerializeField] public float maxRadius = 1f;   // max local offset of close target
    [SerializeField] private float returnSpeed = 5f;   // higher = faster recentre
    [SerializeField] private float farDistance = 30f;  // distance of far target
    [SerializeField] private float closeDistance = 10f;  // distance of close target
    [SerializeField] private float rotationSpeed = 90f;  // Reticle rotation speed
    [SerializeField] private float deadzone = 0.1f; // ignore tiny input jitters

    [SerializeField] private bool invertY = true; // Bool for option later. Let player choose.

    private Vector2 input;
    private GameInputActions.FlyingActions flyingActions;
    private bool isPaused = false;

    // Expose how far the closeTarget is offset in local X/Y
    public Vector2 CloseTargetOffset
    {
        get
        {
            return new Vector2(closeTarget.localPosition.x, closeTarget.localPosition.y);
        }
    }

    private void Awake()
    {
        // Get the single, correct instance of the Flying actions.
        if (GameManager.Instance != null)
        {
            flyingActions = GameManager.Instance.FlyingActions;

            // Subscribe to the Steer actions
            flyingActions.Steer.performed += ctx => input = ctx.ReadValue<Vector2>();
            flyingActions.Steer.canceled += ctx => input = Vector2.zero;
        }
        else
        {
            Debug.LogError("GameManager instance not found. Steering will not work!");
        }

        if (OptionsManager.Instance != null)
        {
            invertY = OptionsManager.Instance.InvertY;
        }
    }

    private void OnEnable()
    {
        // Subscribe to the GameManager's pause events
        GameManager.OnGamePaused += OnGamePaused;
        GameManager.OnGameResumed += OnGameResumed;

        // Sub to OptionsManager events
        OptionsManager.OnInvertChanged += SetInvertY;

        // Get the initial pause state from the GameManager
        if (GameManager.Instance != null)
        {
            isPaused = GameManager.Instance.IsPaused;

            // Enable/disable based on the initial state
            if (!isPaused)
            {
                flyingActions.Enable();
            }
            else
            {
                flyingActions.Disable();
            }
        }
    }

    private void OnDisable()
    {
        // Unsubscribe from the events
        GameManager.OnGamePaused -= OnGamePaused;
        GameManager.OnGameResumed -= OnGameResumed;
        OptionsManager.OnInvertChanged -= SetInvertY;

        // Also disable input when the script is disabled.
        flyingActions.Disable();
    }

    private void SetInvertY(bool inverted)
    {
        this.invertY = inverted;
    }

    private void OnGamePaused()
    {
        isPaused = true;
        flyingActions.Disable();
    }

    private void OnGameResumed()
    {
        isPaused = false;
        flyingActions.Enable();
    }

    private void Update()
    {
        UpdateCloseTarget();
        UpdateFarTarget();
        RotateShipTowardsClose();
    }

    private void UpdateCloseTarget()
    {
        float verticalInput = input.y;
        if (invertY)
            verticalInput = -verticalInput;

        Vector2 rawInput = new Vector2(input.x, verticalInput);

        if (rawInput.magnitude < deadzone)
            rawInput = Vector2.zero;

        Vector2 inputDirection = rawInput.normalized;
        float clampedMagnitude = Mathf.Min(rawInput.magnitude, 1f);
        Vector2 offset2D = inputDirection * clampedMagnitude * maxRadius;

        Vector3 desiredLocalPosition = new Vector3(offset2D.x, offset2D.y, closeDistance);

        float smoothingSpeed = (rawInput == Vector2.zero) ? returnSpeed : returnSpeed * 0.5f;

        closeTarget.localPosition = Vector3.Lerp(closeTarget.localPosition, desiredLocalPosition, Time.deltaTime * smoothingSpeed);
    }

    private void UpdateFarTarget()
    {
        Vector3 worldAimDir = (closeTarget.position - shipTransform.position).normalized;
        farTarget.position = shipTransform.position + worldAimDir * farDistance;
    }

    private void RotateShipTowardsClose()
    {
        if (input.magnitude > deadzone)
        {
            Vector3 aimDir = (closeTarget.position - shipTransform.position).normalized;
            Quaternion desired = Quaternion.LookRotation(aimDir, shipTransform.up);
            float inputStrength = Mathf.Clamp01(input.magnitude);
            float effectiveSpeed = rotationSpeed * inputStrength;

            shipTransform.rotation = Quaternion.RotateTowards(shipTransform.rotation, desired, effectiveSpeed * Time.deltaTime);
        }
    }
}
