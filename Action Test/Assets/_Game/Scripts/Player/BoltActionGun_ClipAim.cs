using UnityEngine;
using UnityEngine.InputSystem;
using DG.Tweening;

public class BoltActionGun_ClipAim : MonoBehaviour
{
    public enum GunState { Ready, Cycling, Reloading }

    [Header("Refs")]
    [SerializeField] private DoomFpsController player;
    [SerializeField] private Camera cam;
    [SerializeField] private Transform recoilPivot;     // RecoilPivot (child of LookPivot)
    [SerializeField] private Transform weaponRoot;      // visual root (kick/ads)
    [SerializeField] private Transform weaponVisual; // rotates for reload/sway (child of weaponRoot)
    [SerializeField] private Transform muzzle;          // preferably stable MuzzleAnchor
    [SerializeField] private ParticleSystem muzzleFlash;

    [Header("Audio")]
    [SerializeField] private bool playAudio = true;
    [SerializeField] private float sfxRange = 25f;

    [Header("Bullet")]
    [SerializeField] private BulletProjectile bulletPrefab;
    [SerializeField] private float bulletSpeed = 55f;
    [SerializeField] private float bulletLifetime = 2f;
    [SerializeField] private int bulletDamage = 8;

    [Header("Clip / Reload (infinite reserve)")]
    [SerializeField] private int clipSize = 5;
    [SerializeField] private float reloadTime = 1.35f;

    [Header("Bolt cycle")]
    [SerializeField] private float cycleTime = 0.75f;

    [Header("Aim (ADS)")]
    [Tooltip("Hold Aim to zoom. Shooting will cancel aim.")]
    [SerializeField] private bool holdToAim = true;

    [SerializeField] private float hipFov = 75f;
    [SerializeField] private float aimFov = 55f;
    [SerializeField] private float aimInTime = 0.10f;
    [SerializeField] private float aimOutTime = 0.08f;

    [Tooltip("WeaponRoot local position when hip firing.")]
    [SerializeField] private Vector3 hipLocalPos = new Vector3(0.25f, -0.25f, 0.5f);

    [Tooltip("WeaponRoot local position when aiming.")]
    [SerializeField] private Vector3 aimLocalPos = new Vector3(0.02f, -0.18f, 0.35f);

    [Header("Reload Animation")]
    [SerializeField] private bool animateReload = true;
    [SerializeField] private Vector3 reloadLocalEuler = new Vector3(18f, 0f, -22f);
    [SerializeField] private float reloadAnimIn = 0.12f;
    [SerializeField] private float reloadAnimOut = 0.10f;

    [Header("Recoil (additive)")]
    [SerializeField] private float recoilKickDegrees = 2.0f;
    [SerializeField] private float recoilDuration = 0.12f;
    [SerializeField] private float weaponKickBack = 0.08f;

    [Header("Bolt animation (optional)")]
    [SerializeField] private float boltBack = 0.06f;
    [SerializeField] private float boltDuration = 0.10f;

    // ---- runtime ----
    private ArenaInput _input;

    private WeaponSway weaponSway;

    private GunState _state = GunState.Ready;
    private float _readyTime;

    private int _ammoInClip;
    private bool _aimHeld;
    private bool _firePressed;
    private bool _reloadPressed;

    private Tween _recoilTween;
    private Tween _weaponTween;
    private Tween _aimTween;
    private Tween _fovTween;
    private Tween _reloadFinishTween;
    private Tween _reloadRotateTween;

    private Quaternion _weaponBaseRot;

    private bool _isAiming;

    public int AmmoInClip => _ammoInClip;
    public int ClipSize => clipSize;
    public bool IsAiming => _isAiming;
    public bool IsReloading => _state == GunState.Reloading;
    

    private void Awake()
    {
        if (!player) player = GetComponentInParent<DoomFpsController>();
        if (!cam) cam = GetComponentInChildren<Camera>(true);
        if (!recoilPivot && cam) recoilPivot = cam.transform.parent;
        if (!weaponRoot) weaponRoot = transform;

         weaponSway = weaponVisual.GetComponent<WeaponSway>();


        _ammoInClip = Mathf.Max(1, clipSize);

        // Initialize hip pose
        weaponRoot.localPosition = hipLocalPos;

        _weaponBaseRot = weaponRoot.localRotation;

        if (cam) cam.fieldOfView = hipFov;

        _input = new ArenaInput();
    }

    private void OnEnable()
    {
        _input.Enable();

        _input.Player.Fire.performed += OnFirePerformed;
        _input.Player.Reload.performed += OnReloadPerformed;

        _input.Player.Aim.performed += OnAimPerformed;
        _input.Player.Aim.canceled += OnAimCanceled;
    }

    private void OnDisable()
    {
        _input.Player.Fire.performed -= OnFirePerformed;
        _input.Player.Reload.performed -= OnReloadPerformed;

        _input.Player.Aim.performed -= OnAimPerformed;
        _input.Player.Aim.canceled -= OnAimCanceled;

        _input.Disable();

        _recoilTween?.Kill();
        _weaponTween?.Kill();
        _aimTween?.Kill();
        _fovTween?.Kill();
        _reloadFinishTween?.Kill();
        _reloadRotateTween?.Kill();
    }

    private void OnFirePerformed(InputAction.CallbackContext ctx) => _firePressed = true;
    private void OnReloadPerformed(InputAction.CallbackContext ctx) => _reloadPressed = true;

    private void OnAimPerformed(InputAction.CallbackContext ctx)
    {
        _aimHeld = true;
        if (holdToAim) SetAiming(true);
        else ToggleAiming();
    }

    private void OnAimCanceled(InputAction.CallbackContext ctx)
    {
        _aimHeld = false;
        if (holdToAim) SetAiming(false);
    }

    private void ToggleAiming()
    {
        // Only used if holdToAim = false
        SetAiming(!_isAiming);
    }

    private void Update()
    {
        // State timers
        if ((_state == GunState.Cycling || _state == GunState.Reloading) && Time.time >= _readyTime)
            _state = GunState.Ready;

        // Reload input
        if (_reloadPressed)
        {
            _reloadPressed = false;
            TryStartReload();
        }

        // Fire input (single-shot)
        if (_firePressed)
        {
            _firePressed = false;
            TryFireOnce();
        }

        // If hold-to-aim and we are reloading/cycling, keep aim off
        if ((_state != GunState.Ready) && _isAiming)
            SetAiming(false);
    }

    private void TryFireOnce()
    {
        if (_state != GunState.Ready) return;
        if (_ammoInClip <= 0)
        {
            // auto-reload if empty
            TryStartReload();
            return;
        }

        // Shooting cancels aim (your requirement)
        if (_isAiming)
            SetAiming(false);

        if (playAudio && AudioManager.Instance != null)
            AudioManager.Instance.PlaySfx3D(AudioManager.Instance.Library.rifleShot, muzzle.position, 1f, 2f, sfxRange);

        FireOnce();
        _ammoInClip--;

        BeginCycle();
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

    private void BeginCycle()
    {
        _state = GunState.Cycling;
        _readyTime = Time.time + Mathf.Max(0.05f, cycleTime);

        if (playAudio && AudioManager.Instance != null)
            AudioManager.Instance.PlaySfx3D(AudioManager.Instance.Library.rifleBolt, muzzle.position, 0.9f, 2f, sfxRange);

        // Simple bolt feel: kick back + small bolt motion
        if (weaponRoot)
        {
            _weaponTween?.Kill();

            Sequence seq = DOTween.Sequence()
                .Append(weaponRoot.DOPunchPosition(new Vector3(0f, 0f, -weaponKickBack), recoilDuration, 10, 0.5f))
                .AppendInterval(0.05f)
                .Append(weaponRoot.DOLocalMove((_isAiming ? aimLocalPos : hipLocalPos) + new Vector3(0f, 0f, -boltBack), boltDuration).SetEase(Ease.OutCubic))
                .Append(weaponRoot.DOLocalMove(_isAiming ? aimLocalPos : hipLocalPos, boltDuration * 1.2f).SetEase(Ease.OutCubic));

            _weaponTween = seq;
        }
    }

    private void TryStartReload()
    {
        if (_state != GunState.Ready) return;
        if (_ammoInClip >= clipSize) return;

        // Reload cancels aim too (feels right)
        if (_isAiming)
            SetAiming(false);

        _state = GunState.Reloading;
        _readyTime = Time.time + Mathf.Max(0.05f, reloadTime);

        if (playAudio && AudioManager.Instance != null)
            AudioManager.Instance.PlaySfx3D(AudioManager.Instance.Library.rifleReload, muzzle.position, 1f, 2f, sfxRange);

        PlayReloadRotateIn();
        // Optional: tiny reload “dip”
        if (weaponRoot)
        {
            _weaponTween?.Kill();
            Vector3 startPos = weaponRoot.localPosition;
            _weaponTween = DOTween.Sequence()
                .Append(weaponRoot.DOLocalMove(startPos + new Vector3(0f, -0.05f, 0f), 0.12f).SetEase(Ease.OutCubic))
                .AppendInterval(Mathf.Max(0f, reloadTime - 0.24f))
                .Append(weaponRoot.DOLocalMove(hipLocalPos, 0.12f).SetEase(Ease.OutCubic));
        }

        _reloadFinishTween?.Kill();
        _reloadFinishTween = DOVirtual.DelayedCall(reloadTime, () =>
        {
            if (this == null || !isActiveAndEnabled) return;

            if (_state == GunState.Reloading)
                _ammoInClip = clipSize;

            // After reload completes, return rotation
            PlayReloadRotateOut();
        });
    }

    private void SetAiming(bool aiming)
    {
        if (_isAiming == aiming) return;

        // Don’t allow aiming while not ready
        if (aiming && _state != GunState.Ready) return;


        _isAiming = aiming;

        _aimTween?.Kill();
        _fovTween?.Kill();

        if (player) player.SetAiming(aiming);

        float t = aiming ? aimInTime : aimOutTime;
        float targetFov = aiming ? aimFov : hipFov;
        Vector3 targetPos = aiming ? aimLocalPos : hipLocalPos;
        if (weaponSway) weaponSway.SetBasePose(aiming ? aimLocalPos : hipLocalPos, weaponRoot.localRotation);
        if (cam)
            _fovTween = DOTween.To(() => cam.fieldOfView, v => cam.fieldOfView = v, targetFov, t).SetEase(Ease.OutCubic);

        if (weaponRoot)
            _aimTween = weaponRoot.DOLocalMove(targetPos, t).SetEase(Ease.OutCubic);
    }

    private void DoRecoil()
    {
        if (recoilPivot)
        {
            _recoilTween?.Kill();

            float yaw = Random.Range(-0.6f, 0.6f);

            _recoilTween = recoilPivot.DOPunchRotation(
                new Vector3(-recoilKickDegrees, yaw, 0f),
                recoilDuration,
                vibrato: 10,
                elasticity: 0.45f
            );
        }
    }

    // Upgrades later
    public void AddDamage(int amount) => bulletDamage += amount;
    public void SetClipSize(int newClipSize)
    {
        clipSize = Mathf.Max(1, newClipSize);
        _ammoInClip = Mathf.Clamp(_ammoInClip, 0, clipSize);
    }

    private void PlayReloadRotateIn()
    {
        if (!animateReload || weaponRoot == null) return;

        _reloadRotateTween?.Kill();

        // Cache current base (important if sway/other systems changed it)
        _weaponBaseRot = weaponRoot.localRotation;

        Quaternion target = Quaternion.Euler(reloadLocalEuler);
        _reloadRotateTween = weaponRoot.DOLocalRotateQuaternion(target, reloadAnimIn)
            .SetEase(Ease.OutCubic);
    }

    private void PlayReloadRotateOut()
    {
        if (!animateReload || weaponRoot == null) return;

        _reloadRotateTween?.Kill();

        // Return to "neutral" rotation (usually identity)
        _reloadRotateTween = weaponRoot.DOLocalRotateQuaternion(_weaponBaseRot, reloadAnimOut)
            .SetEase(Ease.OutCubic);
    }
}