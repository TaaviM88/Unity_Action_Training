using UnityEngine;

/// <summary>
/// Simple FPS weapon sway: based on look input + movement.
/// Put this on WeaponRoot (the visual weapon transform).
/// </summary>
public class WeaponSway : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private DoomFpsController controller; // optional, used for aiming/run state
    [SerializeField] private CharacterController characterController; // for movement sway

    [Header("Look Sway")]
    [SerializeField] private float lookSwayAmount = 1.5f;     // degrees
    [SerializeField] private float lookSwaySpeed = 12f;

    [Header("Move Sway")]
    [SerializeField] private float moveSwayAmount = 0.04f;    // meters
    [SerializeField] private float moveSwaySpeed = 8f;

    [Header("Aiming")]
    [SerializeField] private float aimMultiplier = 0.35f;     // reduce sway when aiming

    private Vector3 _baseLocalPos;
    private Quaternion _baseLocalRot;

    private Vector3 _posVel;
    private Quaternion _rotTarget;

    private void Awake()
    {
        _baseLocalPos = transform.localPosition;
        _baseLocalRot = transform.localRotation;

        if (!controller) controller = GetComponentInParent<DoomFpsController>();
        if (!characterController) characterController = GetComponentInParent<CharacterController>();
    }

    private void LateUpdate()
    {
        // Look input (mouse/gamepad) from InputSystem directly is annoying to pull here.
        // So we use camera angular changes indirectly by reading Mouse delta when available.
        Vector2 look = Vector2.zero;
        if (UnityEngine.InputSystem.Mouse.current != null)
            look = UnityEngine.InputSystem.Mouse.current.delta.ReadValue();
        else if (UnityEngine.InputSystem.Gamepad.current != null)
            look = UnityEngine.InputSystem.Gamepad.current.rightStick.ReadValue() * 25f;

        float mult = (controller != null && controller.IsAiming) ? aimMultiplier : 1f;

        // Rotation sway from look
        float swayYaw = -look.x * 0.01f * lookSwayAmount * mult;
        float swayPitch = look.y * 0.01f * lookSwayAmount * mult;

        Quaternion lookRot = Quaternion.Euler(swayPitch, swayYaw, 0f);
        Quaternion targetRot = _baseLocalRot * lookRot;

        transform.localRotation = Quaternion.Slerp(
            transform.localRotation,
            targetRot,
            1f - Mathf.Exp(-lookSwaySpeed * Time.deltaTime)
        );

        // Position sway from movement
        Vector3 move = Vector3.zero;
        if (characterController != null)
        {
            Vector3 v = characterController.velocity;
            v.y = 0f;
            move = v;
        }

        // Small bob-ish strafe/forward sway
        Vector3 moveOffset =
            transform.right * (-move.x) * 0.0025f +
            transform.up * (Mathf.Sin(Time.time * moveSwaySpeed) * 0.5f) * (move.magnitude * 0.002f);

        moveOffset = Vector3.ClampMagnitude(moveOffset, moveSwayAmount) * mult;

        Vector3 targetPos = _baseLocalPos + moveOffset;

        transform.localPosition = Vector3.SmoothDamp(
            transform.localPosition,
            targetPos,
            ref _posVel,
            0.08f
        );
    }

    /// <summary>
    /// Call this if ADS/weapon scripts change base local pos/rot (e.g. aim local position).
    /// </summary>
    public void SetBasePose(Vector3 localPos, Quaternion localRot)
    {
        _baseLocalPos = localPos;
        _baseLocalRot = localRot;
    }
}