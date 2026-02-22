using UnityEngine;

[RequireComponent(typeof(Health))]
public class CubeEnemy : MonoBehaviour
{
    [Header("Config (set at runtime)")]
    [SerializeField] private EnemyArchetypeSO archetype;

    [Header("Movement")]
    [SerializeField] private float rotateSpeedDegPerSec = 720f;

    private Transform _player;
    private Health _health;
    private Rigidbody _rb;
    private EnemyTouchDamage _touch;

    private float _moveSpeed = 3.5f;
    private Stunnable _stunnable;
    private void Awake()
    {
        _health = GetComponent<Health>();
        _rb = GetComponent<Rigidbody>();               // recommended on enemy: kinematic, no gravity
        _touch = GetComponent<EnemyTouchDamage>();     // optional but recommended

        _stunnable = GetComponent<Stunnable>();
    }

    /// <summary>
    /// Initialize enemy instance from an archetype and target (player).
    /// </summary>
    public void Init(EnemyArchetypeSO a, Transform player)
    {
        archetype = a;
        _player = player;

        // Fallbacks if archetype is missing (keeps prefab playable)
        string baseId = (a != null && !string.IsNullOrWhiteSpace(a.id)) ? a.id : "Enemy";
        _moveSpeed = (a != null) ? a.moveSpeed : 3.5f;

        int touchDamage = (a != null) ? a.touchDamage : 1;
        if (_touch != null)
            _touch.SetDamage(touchDamage);

        // Stable unique id for events/debug
        _health.SetEntityId($"{baseId}_{GetInstanceID()}");
    }

    private void FixedUpdate()
    {
        if (_player == null) return;

        if (_stunnable != null && _stunnable.IsStunned) return;

        Vector3 to = _player.position - transform.position;
        to.y = 0f;

        float sqrDist = to.sqrMagnitude;
        if (sqrDist < 0.0001f) return;

        Vector3 dir = to.normalized;

        // Move (prefer Rigidbody.MovePosition when Rigidbody exists)
        Vector3 nextPos = transform.position + dir * (_moveSpeed * Time.fixedDeltaTime);

        if (_rb != null)
            _rb.MovePosition(nextPos);
        else
            transform.position = nextPos;

        // Rotate to face target
        Quaternion targetRot = Quaternion.LookRotation(dir, Vector3.up);
        Quaternion nextRot = Quaternion.RotateTowards(
            transform.rotation,
            targetRot,
            rotateSpeedDegPerSec * Time.fixedDeltaTime
        );

        if (_rb != null)
            _rb.MoveRotation(nextRot);
        else
            transform.rotation = nextRot;
    }
}