using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(Health))]
[RequireComponent(typeof(Animator))]
public class DollEnemyNav : MonoBehaviour
{
    [SerializeField] private EnemyArchetypeSO archetype;
    [SerializeField] private float repathInterval = 0.15f;

    private Transform _player;
    private NavMeshAgent _agent;
    private Health _health;
    private Animator _anim;

    private float _nextRepathTime;

    private static readonly int SpeedHash = Animator.StringToHash("Speed");

    private void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
        _health = GetComponent<Health>();
        _anim = GetComponent<Animator>();
    }
    private void Start()
    {
        if (_player == null)
        {
            var playerController = FindFirstObjectByType<DoomFpsController>();
            if (playerController != null)
                _player = playerController.transform;
        }

        _agent.Warp(transform.position);
    }

    public void Init(EnemyArchetypeSO a, Transform player)
    {
        archetype = a;
        _player = player;

        float spd = a != null ? a.moveSpeed : 3.5f;
        _agent.speed = spd;

        string id = a != null ? a.id : "Enemy";
        _health.SetEntityId(id + "_" + GetInstanceID());
    }

    private void Update()
    {
        if (_player == null) return;

        if (!_agent.isOnNavMesh) return;

        // Movement
        if (Time.time >= _nextRepathTime)
        {
            _nextRepathTime = Time.time + repathInterval;
            _agent.SetDestination(_player.position);
        }

        UpdateAnimator();
    }

    private void UpdateAnimator()
    {
        if (_anim == null) return;

        // Agent velocity magnitude (world space)
        float speed = _agent.velocity.magnitude;

        // Normalize speed relative to agent max speed
        float normalizedSpeed = speed / Mathf.Max(0.01f, _agent.speed);

        // Smooth animation transitions
        _anim.SetFloat(SpeedHash, normalizedSpeed, 0.1f, Time.deltaTime);
    }
}