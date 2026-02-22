using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class BulletProjectile : MonoBehaviour
{
    [Header("Impact")]
    [SerializeField] private ParticleSystem impactPrefab;
    [SerializeField] private ParticleSystem bloodImpactPrefab;
    [SerializeField] private LayerMask hitMask = ~0;

    [Header("Ballistics")]
    [SerializeField] private bool enableBulletDrop = true;

    [Tooltip("Gravity strength multiplier. 1 = Physics.gravity. Rifle-like: 0.2�0.6.")]
    [SerializeField] private float gravityScale = 0.45f;

    [Tooltip("Bullet travels this distance before drop starts (rifle feel).")]
    [SerializeField] private float dropStartDistance = 18f;

    [Tooltip("Optional: also delay drop by time (use 0 to disable).")]
    [SerializeField] private float dropStartTime = 0f;

    [Header("Safety Despawn")]
    [Tooltip("Hard cap: if bullet travels farther than this from its spawn point, despawn.")]
    [SerializeField] private float maxTravelDistance = 140f;

    [Tooltip("Optional world bounds check. If enabled and bullet is outside, despawn.")]
    [SerializeField] private bool useWorldBounds = false;

    [Tooltip("World-space center of allowed area (if useWorldBounds).")]
    [SerializeField] private Vector3 worldBoundsCenter = Vector3.zero;

    [Tooltip("World-space size of allowed area (if useWorldBounds).")]
    [SerializeField] private Vector3 worldBoundsSize = new Vector3(300f, 200f, 300f);

    private Rigidbody _rb;
    private string _ownerId;
    private int _damage;
    private float _dieTime;

    private Vector3 _spawnPos;
    private float _spawnTime;

    public int PoolKey { get; private set; }

    private Bounds _worldBounds;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();

        _rb.useGravity = false; // we apply custom gravity for better control
        _rb.isKinematic = false;
        _rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        _rb.interpolation = RigidbodyInterpolation.Interpolate;

        _worldBounds = new Bounds(worldBoundsCenter, worldBoundsSize);
    }

    private void OnValidate()
    {
        maxTravelDistance = Mathf.Max(5f, maxTravelDistance);
        dropStartDistance = Mathf.Max(0f, dropStartDistance);
        dropStartTime = Mathf.Max(0f, dropStartTime);
        gravityScale = Mathf.Max(0f, gravityScale);
        worldBoundsSize.x = Mathf.Max(1f, worldBoundsSize.x);
        worldBoundsSize.y = Mathf.Max(1f, worldBoundsSize.y);
        worldBoundsSize.z = Mathf.Max(1f, worldBoundsSize.z);
    }

    public void SetPoolKey(int key) => PoolKey = key;

    public void Launch(string ownerId, int damage, Vector3 velocity, float lifeTime)
    {
        _ownerId = ownerId;
        _damage = damage;

        // Reset physics state
        _rb.linearVelocity = Vector3.zero;
        _rb.angularVelocity = Vector3.zero;

        _rb.linearVelocity = velocity;

        _spawnPos = transform.position;
        _spawnTime = Time.time;

        _dieTime = Time.time + Mathf.Max(0.05f, lifeTime);

        // Refresh bounds at launch in case you edit in inspector/runtime
        _worldBounds = new Bounds(worldBoundsCenter, worldBoundsSize);
    }

    private void Update()
    {
        // Lifetime cap
        if (Time.time >= _dieTime)
        {
            Despawn();
            return;
        }

        // Travel distance cap (prevents forever bullets even if lifetime is large)
        if ((transform.position - _spawnPos).sqrMagnitude >= (maxTravelDistance * maxTravelDistance))
        {
            Despawn();
            return;
        }

        // Optional world bounds cap
        if (useWorldBounds)
        {
            // keep bounds updated if you tweak values at runtime
            _worldBounds.center = worldBoundsCenter;
            _worldBounds.size = worldBoundsSize;

            if (!_worldBounds.Contains(transform.position))
            {
                Despawn();
                return;
            }
        }
    }

    private void FixedUpdate()
    {
        if (!enableBulletDrop) return;

        // Rifle feel: no drop until we've traveled far enough (and optionally enough time)
        float dist = Vector3.Distance(_spawnPos, transform.position);
        if (dist < dropStartDistance) return;

        if (dropStartTime > 0f && (Time.time - _spawnTime) < dropStartTime) return;

        _rb.AddForce(Physics.gravity * gravityScale, ForceMode.Acceleration);
    }

    private void OnCollisionEnter(Collision collision)
    {
        var col = collision.collider;

        if (((1 << col.gameObject.layer) & hitMask) == 0)
        {
            Despawn();
            return;
        }

        Vector3 hitPoint = collision.contacts.Length > 0 ? collision.contacts[0].point : transform.position;
        Vector3 hitNormal = collision.contacts.Length > 0 ? collision.contacts[0].normal : -transform.forward;

        var health = col.GetComponentInParent<Health>();
        bool didDamage = false;

        if (health != null && health.EntityId != _ownerId)
        {
            int finalDamage = _damage;

            // Weakspot (optional)
            var weak = col.GetComponent<WeakSpot>();
            if (weak != null)
            {
                if (weak.instantKill) finalDamage = 999999;
                else finalDamage = Mathf.RoundToInt(finalDamage * Mathf.Max(0f, weak.damageMultiplier));

                if (weak.type == WeakSpotType.StunSpot)
                {
                    var stunnable = health.GetComponent<Stunnable>();
                    if (stunnable != null) stunnable.Stun(weak.stunDuration);
                }
            }

            health.TakeDamage(finalDamage, _ownerId);
            didDamage = true;
        }

        // Blood vs sparks
        if (didDamage && bloodImpactPrefab != null)
            SpawnParticle(bloodImpactPrefab, hitPoint, hitNormal);
        else if (!didDamage && impactPrefab != null)
            SpawnParticle(impactPrefab, hitPoint, hitNormal);

        Despawn();
    }

    private void SpawnParticle(ParticleSystem prefab, Vector3 pos, Vector3 normal)
    {
        var ps = Instantiate(prefab, pos, Quaternion.LookRotation(normal, Vector3.up));
        ps.Play(true);

        var auto = ps.GetComponent<AutoDestroyParticle>();
        if (!auto) ps.gameObject.AddComponent<AutoDestroyParticle>();
    }

    private void Despawn()
    {
        if (BulletPool.Instance != null)
            BulletPool.Instance.Return(this);
        else
            Destroy(gameObject);
    }
}