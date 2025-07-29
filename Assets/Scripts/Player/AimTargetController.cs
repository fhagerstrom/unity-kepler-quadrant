using UnityEngine;

public class AimTargetController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform shipTransform;
    [SerializeField] private Transform closeTarget;   // child of the ship
    [SerializeField] private Transform farTarget;     // Child of AimParent

    [Header("Settings")]
    [SerializeField] public float maxRadius = 1f;   // max local offset of close target
    [SerializeField] private float returnSpeed = 5f;   // higher = faster recentre
    [SerializeField] private float farDistance = 30f;  // distance of far target
    [SerializeField] private float closeDistance = 10f;  // distance of close target
    [SerializeField] private float rotationSpeed = 90f;  // Reticle rotation speed
    [SerializeField] private float deadzone = 0.1f; // ignore tiny input jitters

    [SerializeField] private bool invertY = true; // Invert controls?

    private Vector2 input;

    private GameInputActions inputActions;
    private GameInputActions.FlyingActions flying;

    // Expose how far the closeTarget is offset in local X/Y
    public Vector2 CloseTargetOffset
    {
        get
        {
            // Z axis is not needed in this context
            return new Vector2(closeTarget.localPosition.x, closeTarget.localPosition.y);
        }
    }

    private void Awake()
    {
        inputActions = new GameInputActions();
        flying = inputActions.Flying;

        flying.Steer.performed += ctx => input = ctx.ReadValue<Vector2>();
        flying.Steer.canceled += ctx => input = Vector2.zero;
    }

    private void OnEnable() => inputActions.Enable();
    private void OnDisable() => inputActions.Disable();

    private void Update()
    {
        UpdateCloseTarget();
        UpdateFarTarget();
        RotateShipTowardsClose();
    }

    private void UpdateCloseTarget()
    {
        //// Read input, check if y input should be inverted or not (player option)
        //float verticalInput = input.y;
        //if (invertY)
        //    verticalInput = -verticalInput;

        //Vector2 aim2D = new Vector2(input.x, verticalInput);
        //float mag = aim2D.magnitude;

        //// Center ‐ forward position
        //Vector3 centerLocal = new Vector3(0f, 0f, closeDistance);

        //if (mag >= deadzone)
        //{
        //    // Snap exactly to stick direction
        //    Vector2 dir = aim2D.normalized * Mathf.Min(mag, 1f) * maxRadius;
        //    Vector3 desired = new Vector3(dir.x, dir.y, closeDistance);
        //    closeTarget.localPosition = desired;

        //    // Clear smoothing velocity for clean recenter later
        //    //velocity = Vector3.zero;
        //}
        //else
        //{
        //    // Smoothly drift back to center
        //    //closeTarget.localPosition = Vector3.SmoothDamp(
        //    //    closeTarget.localPosition,
        //    //    centerLocal,
        //    //    ref velocity,
        //    //    1f / returnSpeed
        //    //);

        //    closeTarget.localPosition = Vector3.Lerp(closeTarget.localPosition, centerLocal, Time.deltaTime * returnSpeed);
        //}

        // Read input, check if y input should be inverted or not (player option)
        float verticalInput = input.y;
        if (invertY)
            verticalInput = -verticalInput;

        Vector2 rawInput = new Vector2(input.x, verticalInput);

        // Apply deadzone
        if (rawInput.magnitude < deadzone)
            rawInput = Vector2.zero;

        // Determine local offset
        Vector2 inputDirection = rawInput.normalized;
        float clampedMagnitude = Mathf.Min(rawInput.magnitude, 1f);
        Vector2 offset2D = inputDirection * clampedMagnitude * maxRadius;

        // Final position in local space (with forward Z offset)
        Vector3 desiredLocalPosition = new Vector3(offset2D.x, offset2D.y, closeDistance);

        // Smoothing speed based on input state
        float smoothingSpeed = (rawInput == Vector2.zero)
            ? returnSpeed      // faster when recentering
            : returnSpeed * 0.5f; // slower when steering

        // Move the close target
        closeTarget.localPosition = Vector3.Lerp(closeTarget.localPosition, desiredLocalPosition, Time.deltaTime * smoothingSpeed);
    }

    private void UpdateFarTarget()
    {
        // Project far target along the ship to closeTarget vector
        Vector3 worldAimDir = (closeTarget.position - shipTransform.position).normalized;
        farTarget.position = shipTransform.position + worldAimDir * farDistance;
    }

    private void RotateShipTowardsClose()
    {
        Vector3 aimDir = (closeTarget.position - shipTransform.position).normalized;
        Quaternion desired = Quaternion.LookRotation(aimDir, shipTransform.up);
        float inputStrength = Mathf.Clamp01(input.magnitude);
        float effectiveSpeed = rotationSpeed * inputStrength; // Rotation "sensitivity" multiplied by amount of stick input

        shipTransform.rotation = Quaternion.RotateTowards(shipTransform.rotation, desired, effectiveSpeed * Time.deltaTime);
    }
}
