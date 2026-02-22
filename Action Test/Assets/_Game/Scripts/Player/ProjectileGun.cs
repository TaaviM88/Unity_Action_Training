using UnityEngine;
using UnityEngine.InputSystem;
using DG.Tweening;

public class ProjectileGun : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private DoomFpsController player;
    [SerializeField] private Camera cam;

    [Tooltip("This must be RecoilPivot (child of LookPivot).")]
    [SerializeField] private Transform recoilPivot;

    [Tooltip("Weapon visual root (kickback).")]
    [SerializeField] private Transform weaponRoot;

    [SerializeField] private Transform muzzle;
    [SerializeField] private ParticleSystem muzzleFlash;

    [Header("Bullet")]
    [SerializeField] private BulletProjectile bulletPrefab;
    [SerializeField] private float bulletSpeed = 45f;
    [SerializeField] private float bulletLifetime = 2f;
    [SerializeField] private int bulletDamage = 2;

    [Header("Fire")]
    [SerializeField] private float fireRate = 9f;

    [Header("Recoil (safe additive)")]
    [SerializeField] private float recoilKickDegrees = 1.2f;
    [SerializeField] private float recoilDuration = 0.10f;
    [SerializeField] private float weaponKickBack = 0.05f;

    private ArenaInput _input;
    private bool _fireHeld;
    private float _nextShotTime;

    private Tween _recoilTween;
    private Tween _weaponTween;

    private Vector3 _weaponBasePos;

    private void Awake()
    {
        if (!player) player = GetComponentInParent<DoomFpsController>();
        if (!cam) cam = GetComponentInChildren<Camera>(true);

        // recoilPivot should be Camera's parent (RecoilPivot)
        if (!recoilPivot && cam) recoilPivot = cam.transform.parent;

        if (!weaponRoot) weaponRoot = transform;
        _weaponBasePos = weaponRoot.localPosition;

        _input = new ArenaInput
            ();
    }

    private void OnEnable()
    {
        _input.Enable();
        _input.Player.Fire.performed += OnFire;
        _input.Player.Fire.canceled += OnFire;
    }

    private void OnDisable()
    {
        _input.Player.Fire.performed -= OnFire;
        _input.Player.Fire.canceled -= OnFire;
        _input.Disable();

        _recoilTween?.Kill();
        _weaponTween?.Kill();
    }

    private void OnFire(InputAction.CallbackContext ctx) => _fireHeld = ctx.ReadValueAsButton();

    private void Update()
    {
        if (!_fireHeld) return;
        if (Time.time < _nextShotTime) return;

        _nextShotTime = Time.time + (1f / Mathf.Max(0.01f, fireRate));
        FireOnce();
    }

    private void FireOnce()
    {
        if (!bulletPrefab || !muzzle || !cam || !player) return;

        if (muzzleFlash)
        {
            muzzleFlash.transform.localScale = Vector3.one * Random.Range(0.9f, 1.15f);
            muzzleFlash.Play(true);
        }

        var bullet = BulletPool.Instance.Get(bulletPrefab);
        bullet.transform.position = muzzle.position;
        bullet.transform.rotation = Quaternion.LookRotation(cam.transform.forward, Vector3.up);

        bullet.Launch(
            ownerId: player.PlayerId,
            damage: bulletDamage,
            velocity: cam.transform.forward * bulletSpeed,
            lifeTime: bulletLifetime
        );

        DoRecoil();
    }

    private void DoRecoil()
    {
        // Additive recoil: never resets aim.
        if (recoilPivot)
        {
            _recoilTween?.Kill();

            // Punch rotation is additive and returns automatically.
            // Use small random horizontal recoil too (Doom vibe).
            float yaw = Random.Range(-0.35f, 0.35f);

            _recoilTween = recoilPivot.DOPunchRotation(
                new Vector3(-recoilKickDegrees, yaw, 0f),
                recoilDuration,
                vibrato: 10,
                elasticity: 0.5f
            ).SetUpdate(UpdateType.Normal);
        }

        if (weaponRoot)
        {
            _weaponTween?.Kill();
            weaponRoot.localPosition = _weaponBasePos;

            _weaponTween = weaponRoot.DOPunchPosition(
                new Vector3(0f, 0f, -weaponKickBack),
                recoilDuration,
                vibrato: 10,
                elasticity: 0.5f
            );
        }
    }

    public void AddDamage(int amount) => bulletDamage += amount;
    public void MultiplyFireRate(float multiplier) => fireRate *= multiplier;
}