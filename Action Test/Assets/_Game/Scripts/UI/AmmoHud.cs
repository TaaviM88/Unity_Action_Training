using TMPro;
using UnityEngine;
using DG.Tweening;

public class AmmoHud : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TMP_Text ammoText;
    [SerializeField] private TMP_Text reloadingText;

    [Header("Find Weapon")]
    [Tooltip("If empty, we'll find a BoltActionGun_ClipAim in scene (including inactive).")]
    [SerializeField] private BoltActionGun_ClipAim boltGun;

    [Header("Reload Pulse")]
    [SerializeField] private float pulseMinAlpha = 0.25f;
    [SerializeField] private float pulseTime = 0.45f;

    private Tween _reloadPulseTween;
    private bool _wasReloading;

    private void Awake()
    {
        if (!boltGun)
            boltGun = FindAnyObjectByType<BoltActionGun_ClipAim>(FindObjectsInactive.Include);
    }

    private void OnEnable()
    {
        SetReloading(false);
        ForceRefresh();
    }

    private void OnDisable()
    {
        _reloadPulseTween?.Kill();
    }

    private void Update()
    {
        if (!boltGun)
        {
            // in case weapon gets swapped or instantiated later
            boltGun = FindAnyObjectByType<BoltActionGun_ClipAim>(FindObjectsInactive.Include);
            if (!boltGun) return;
        }

        // Update ammo text
        if (ammoText)
            ammoText.text = $"AMMO {boltGun.AmmoInClip}/{boltGun.ClipSize}";

        // Reloading indicator
        bool isReloading = boltGun.IsReloading;
        if (isReloading != _wasReloading)
        {
            _wasReloading = isReloading;
            SetReloading(isReloading);
        }
    }

    private void ForceRefresh()
    {
        if (!boltGun) return;

        if (ammoText)
            ammoText.text = $"AMMO {boltGun.AmmoInClip}/{boltGun.ClipSize}";

        _wasReloading = boltGun.IsReloading;
        SetReloading(_wasReloading);
    }

    private void SetReloading(bool on)
    {
        if (!reloadingText) return;

        _reloadPulseTween?.Kill();

        reloadingText.gameObject.SetActive(on);

        if (!on)
        {
            // restore alpha
            Color c = reloadingText.color;
            c.a = 1f;
            reloadingText.color = c;
            return;
        }

        reloadingText.text = "RELOADING...";

        // Pulse alpha
        Color baseColor = reloadingText.color;
        baseColor.a = 1f;
        reloadingText.color = baseColor;

        _reloadPulseTween = reloadingText
            .DOFade(pulseMinAlpha, pulseTime)
            .SetLoops(-1, LoopType.Yoyo)
            .SetEase(Ease.InOutSine);
    }
}