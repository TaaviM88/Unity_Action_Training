using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class CrosshairAimOnly : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Image crosshairImage;

    [Tooltip("Weapon that provides IsAiming. If null, we'll find it.")]
    [SerializeField] private BoltActionGun_ClipAim boltGun;

    [Header("Fade")]
    [SerializeField] private float fadeInTime = 0.08f;
    [SerializeField] private float fadeOutTime = 0.06f;
    [SerializeField] private float aimAlpha = 1f;
    [SerializeField] private float hipAlpha = 0f;

    private Tween _fadeTween;
    private bool _lastAim;

    private void Awake()
    {
        if (!crosshairImage)
            crosshairImage = GetComponent<Image>();

        if (!boltGun)
            boltGun = FindAnyObjectByType<BoltActionGun_ClipAim>(FindObjectsInactive.Include);

        // Start hidden
        SetAlphaInstant(hipAlpha);
        _lastAim = false;
    }

    private void OnDisable()
    {
        _fadeTween?.Kill();
    }

    private void Update()
    {
        if (!boltGun)
        {
            boltGun = FindAnyObjectByType<BoltActionGun_ClipAim>(FindObjectsInactive.Include);
            if (!boltGun) return;
        }

        bool aiming = boltGun.IsAiming;

        if (aiming == _lastAim) return;
        _lastAim = aiming;

        FadeTo(aiming ? aimAlpha : hipAlpha, aiming ? fadeInTime : fadeOutTime);
    }

    private void FadeTo(float targetAlpha, float time)
    {
        if (!crosshairImage) return;

        _fadeTween?.Kill();
        _fadeTween = crosshairImage.DOFade(targetAlpha, time).SetEase(Ease.OutCubic);
    }

    private void SetAlphaInstant(float a)
    {
        if (!crosshairImage) return;
        Color c = crosshairImage.color;
        c.a = a;
        crosshairImage.color = c;
    }
}