using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class DoomFpsController : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Camera playerCam;
    [SerializeField] private Transform lookPivot; // assign LookPivot in inspector
    [Header("Look")]
    [SerializeField] private float mouseSensitivity = 0.12f;
    [SerializeField] private float padSensitivity = 2.2f;
    [SerializeField] private float maxPitch = 85f;

    [Header("Movement (Doom-ish)")]
    [Tooltip("Top speed on ground.")]
    [SerializeField] private float maxGroundSpeed = 9.0f;

    [Tooltip("Top speed in air (usually similar or slightly lower).")]
    [SerializeField] private float maxAirSpeed = 9.0f;

    [Tooltip("How quickly we accelerate towards wish direction on ground.")]
    [SerializeField] private float groundAccel = 55f;

    [Tooltip("How quickly we accelerate in air (air control).")]
    [SerializeField] private float airAccel = 18f;

    [Tooltip("How quickly we slow down when no input (ground only).")]
    [SerializeField] private float groundFriction = 12f;

    [Tooltip("Gravity (negative).")]
    [SerializeField] private float gravity = -28f;

    [Tooltip("Jump impulse height in meters (converted to velocity).")]
    [SerializeField] private float jumpHeight = 1.2f;

    [Tooltip("Small downward force to keep grounded stable.")]
    [SerializeField] private float groundStickForce = -2f;

    [Header("Speed Multipliers")]
    [SerializeField] private float runMultiplier = 1.25f;
    [SerializeField] private float aimMultiplier = 0.65f;
     

    public bool IsAiming { get; private set; }
    public bool IsRunning { get; private set; }
    private bool _runHeld;
    [Header("Optional")]
    [SerializeField] private bool allowBunnyHop = true;
    [SerializeField] private bool clampDiagonalSpeed = true;

    [Header("Id")]
    [SerializeField] private string playerId = "Player";
    public string PlayerId => playerId;

    private CharacterController _cc;
    private ArenaInput _input;

    private Vector2 _move;
    private Vector2 _look;
    private bool _jumpHeld;
    private bool _jumpPressedThisFrame;

    private float _pitch;
    private Vector3 _velocity; // world-space velocity (x,z) + y

    private void Awake()
    {
        _cc = GetComponent<CharacterController>();
        if (!playerCam) playerCam = GetComponentInChildren<Camera>(true);

        if (!lookPivot && playerCam) lookPivot = playerCam.transform.parent; // LookPivot
        _input = new ArenaInput();
    }

    private void OnEnable()
    {
        _input.Enable();

        _input.Player.Move.performed += OnMove;
        _input.Player.Move.canceled += OnMove;

        _input.Player.Look.performed += OnLook;
        _input.Player.Look.canceled += OnLook;

        _input.Player.Jump.performed += OnJump;
        _input.Player.Jump.canceled += OnJump;

        _input.Player.Run.performed += OnRun;
        _input.Player.Run.canceled += OnRun;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void OnDisable()
    {
        _input.Player.Move.performed -= OnMove;
        _input.Player.Move.canceled -= OnMove;

        _input.Player.Look.performed -= OnLook;
        _input.Player.Look.canceled -= OnLook;

        _input.Player.Jump.performed -= OnJump;
        _input.Player.Jump.canceled -= OnJump;

        _input.Disable();
    }

    private void OnMove(InputAction.CallbackContext ctx) => _move = ctx.ReadValue<Vector2>();
    private void OnLook(InputAction.CallbackContext ctx) => _look = ctx.ReadValue<Vector2>();

    private void OnJump(InputAction.CallbackContext ctx)
    {
        bool held = ctx.ReadValueAsButton();
        if (held && !_jumpHeld) _jumpPressedThisFrame = true;
        _jumpHeld = held;
    }

    private void OnRun(InputAction.CallbackContext ctx) => _runHeld = ctx.ReadValueAsButton();

    private void Update()
    {
        HandleLook();
        HandleMove(Time.deltaTime);
        _jumpPressedThisFrame = false;
    }

    private void HandleLook()
    {
        if (!playerCam) return;

        float sens = Mouse.current != null ? mouseSensitivity : padSensitivity;

        float yaw = _look.x * sens;
        float pitchDelta = -_look.y * sens;

        transform.Rotate(0f, yaw, 0f);

        _pitch = Mathf.Clamp(_pitch + pitchDelta, -maxPitch, maxPitch);
        if (lookPivot) lookPivot.localRotation = Quaternion.Euler(_pitch, 0f, 0f);
    }

    private void HandleMove(float dt)
    {
        bool grounded = _cc.isGrounded;

        // Convert move input to wish direction in world space
        Vector3 wishDir =
            transform.right * _move.x +
            transform.forward * _move.y;

        if (clampDiagonalSpeed)
            wishDir = Vector3.ClampMagnitude(wishDir, 1f);
        else if (wishDir.sqrMagnitude > 1f)
            wishDir.Normalize();

        if (grounded)
        {
            // Stick to ground
            if (_velocity.y < 0f)
                _velocity.y = groundStickForce;

            ApplyFriction(ref _velocity, groundFriction, dt);

            float speedMult = 1f;

            IsRunning = _runHeld && !IsAiming;
            if (IsRunning) speedMult *= runMultiplier;
            if (IsAiming) speedMult *= aimMultiplier;

            float groundMax = maxGroundSpeed * speedMult;
            Accelerate(ref _velocity, wishDir, groundMax, groundAccel, dt);

            bool wantsJump = allowBunnyHop ? _jumpHeld : _jumpPressedThisFrame;
            if (wantsJump)
            {
                _velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
            }
        }
        else
        {
            float speedMult = 1f;
            // Air movement + gravity
            float airMax = maxAirSpeed * speedMult;
            Accelerate(ref _velocity, wishDir, airMax, airAccel, dt);
            _velocity.y += gravity * dt;
        }

        // CharacterController.Move expects displacement (meters) this frame
        _cc.Move(_velocity * dt);

        // If we hit head on ceiling, stop upward velocity
        if ((_cc.collisionFlags & CollisionFlags.Above) != 0 && _velocity.y > 0f)
            _velocity.y = 0f;
    }

    private static void ApplyFriction(ref Vector3 vel, float friction, float dt)
    {
        // Only apply to horizontal velocity
        Vector3 horiz = new Vector3(vel.x, 0f, vel.z);
        float speed = horiz.magnitude;
        if (speed < 0.001f) return;

        float drop = speed * friction * dt;
        float newSpeed = Mathf.Max(0f, speed - drop);

        if (newSpeed == speed) return;

        float scale = newSpeed / speed;
        vel.x *= scale;
        vel.z *= scale;
    }

    private static void Accelerate(ref Vector3 vel, Vector3 wishDir, float maxSpeed, float accel, float dt)
    {
        if (wishDir.sqrMagnitude < 0.0001f) return;

        Vector3 wish = wishDir.normalized;
        float wishSpeed = maxSpeed;

        // Current speed in that direction
        float currentSpeed = Vector3.Dot(new Vector3(vel.x, 0f, vel.z), wish);
        float addSpeed = wishSpeed - currentSpeed;
        if (addSpeed <= 0f) return;

        float accelSpeed = accel * wishSpeed * dt;
        if (accelSpeed > addSpeed) accelSpeed = addSpeed;

        vel.x += accelSpeed * wish.x;
        vel.z += accelSpeed * wish.z;
    }

    public void SetAiming(bool aiming)
    {
        IsAiming = aiming;

        // If aiming, force run off
        if (aiming) _runHeld = false;
    }
}