using TMPro;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class HudController : MonoBehaviour
{
    [Header("Text")]
    [SerializeField] private TMP_Text scoreText;
    [SerializeField] private TMP_Text waveText;
    [SerializeField] private TMP_Text hpText;

    [Header("HP Bar (optional)")]
    [SerializeField] private Image hpFill; // set type Filled or simple stretched image

    [Header("Tween")]
    [SerializeField] private float punchScale = 0.18f;
    [SerializeField] private float punchDuration = 0.18f;

    [SerializeField] private float waveShowY = -20f;   // anchored offset when visible
    [SerializeField] private float waveHideY = 40f;    // anchored offset when hidden
    [SerializeField] private float waveSlideDuration = 0.25f;

    private int _score;
    private int _waveIndex;
    private Health _playerHealth;

    private Tween _scoreTween;
    private Tween _waveTween;
    private Tween _hpTween;

    private Vector3 _scoreBaseScale;
    private Vector3 _waveBaseScale;
    private Vector3 _hpBaseScale;

    private RectTransform _waveRect;

    private void Awake()
    {
        if (scoreText) _scoreBaseScale = scoreText.rectTransform.localScale;
        if (waveText)
        {
            _waveBaseScale = waveText.rectTransform.localScale;
            _waveRect = waveText.rectTransform;
        }
        if (hpText) _hpBaseScale = hpText.rectTransform.localScale;

        // Find player health once (simple v0.1 approach)
        var player = FindAnyObjectByType<DoomFpsController>();
        if (player != null) _playerHealth = player.GetComponent<Health>();
    }

    private void OnEnable()
    {
        GameEvents.ScoreChanged += OnScoreChanged;
        GameEvents.WaveStarted += OnWaveStarted;
        GameEvents.WaveCompleted += OnWaveCompleted;
        GameEvents.DamageDealt += OnDamageDealt;
        GameEvents.EntityDied += OnEntityDied;

        // Initial UI state
        SetScore(0, animate: false);
        SetWaveText("Wave 1", animate: false);
        UpdateHpUI(animate: false);

        HideWaveInstant();
    }

    private void OnDisable()
    {
        GameEvents.ScoreChanged -= OnScoreChanged;
        GameEvents.WaveStarted -= OnWaveStarted;
        GameEvents.WaveCompleted -= OnWaveCompleted;
        GameEvents.DamageDealt -= OnDamageDealt;
        GameEvents.EntityDied -= OnEntityDied;

        _scoreTween?.Kill();
        _waveTween?.Kill();
        _hpTween?.Kill();
    }

    // ---------------- Events ----------------

    private void OnScoreChanged(ScoreEvent e)
    {
        SetScore(e.NewScore, animate: true);
    }

    private void OnWaveStarted(WaveEvent e)
    {
        _waveIndex = e.WaveIndex;
        SetWaveText($"Wave {_waveIndex + 1}", animate: true);
        ShowWaveAnimated();
    }

    private void OnWaveCompleted(WaveEvent e)
    {
        SetWaveText($"Wave {e.WaveIndex + 1} cleared!", animate: true);
        ShowWaveAnimated();

        // Auto-hide after a moment
        DOVirtual.DelayedCall(1.1f, () =>
        {
            if (this == null || !isActiveAndEnabled) return;
            HideWaveAnimated();
        });
    }

    private void OnDamageDealt(DamageEvent e)
    {
        // Update HP only when the player is the target
        if (_playerHealth == null) return;
        if (e.TargetId != _playerHealth.EntityId) return;

        UpdateHpUI(animate: true);
    }

    private void OnEntityDied(DeathEvent e)
    {
        if (_playerHealth != null && e.EntityId == _playerHealth.EntityId)
        {
            SetWaveText("GAME OVER", animate: true);
            ShowWaveAnimated();
        }
    }

    // ---------------- UI helpers ----------------

    private void SetScore(int newScore, bool animate)
    {
        _score = newScore;
        if (scoreText) scoreText.text = $"SCORE {_score}";

        if (animate && scoreText)
        {
            _scoreTween?.Kill();
            scoreText.rectTransform.localScale = _scoreBaseScale;
            _scoreTween = scoreText.rectTransform.DOPunchScale(
                Vector3.one * punchScale,
                punchDuration,
                vibrato: 8,
                elasticity: 0.65f
            );
        }
    }

    private void SetWaveText(string txt, bool animate)
    {
        if (waveText) waveText.text = txt;

        if (animate && waveText)
        {
            _waveTween?.Kill();
            waveText.rectTransform.localScale = _waveBaseScale;
            _waveTween = waveText.rectTransform.DOPunchScale(
                Vector3.one * (punchScale * 0.9f),
                punchDuration,
                vibrato: 7,
                elasticity: 0.6f
            );
        }
    }

    private void UpdateHpUI(bool animate)
    {
        if (_playerHealth == null)
        {
            // try re-find if player respawned
            var player = FindAnyObjectByType<DoomFpsController>();
            if (player != null) _playerHealth = player.GetComponent<Health>();
            if (_playerHealth == null) return;
        }

        int hp = _playerHealth.Hp;
        int max = _playerHealth.MaxHp;

        if (hpText) hpText.text = $"HP {hp}/{max}";

        if (hpFill)
        {
            float t = (max <= 0) ? 0f : Mathf.Clamp01((float)hp / max);

            // If it's a Filled image, just set fillAmount. If not, you can scale X.
            if (hpFill.type == Image.Type.Filled)
            {
                if (animate)
                {
                    _hpTween?.Kill();
                    _hpTween = DOTween.To(() => hpFill.fillAmount, v => hpFill.fillAmount = v, t, 0.12f);
                }
                else
                {
                    hpFill.fillAmount = t;
                }
            }
            else
            {
                // scale X fallback
                var rt = hpFill.rectTransform;
                Vector3 s = rt.localScale;
                if (animate)
                {
                    _hpTween?.Kill();
                    _hpTween = rt.DOScale(new Vector3(t, s.y, s.z), 0.12f);
                }
                else
                {
                    rt.localScale = new Vector3(t, s.y, s.z);
                }
            }
        }

        if (animate && hpText)
        {
            _hpTween?.Kill();
            hpText.rectTransform.localScale = _hpBaseScale;
            _hpTween = hpText.rectTransform.DOPunchScale(
                Vector3.one * (punchScale * 0.7f),
                punchDuration,
                vibrato: 8,
                elasticity: 0.6f
            );
        }
    }

    // ---------------- Wave banner motion ----------------

    private void HideWaveInstant()
    {
        if (_waveRect == null) return;
        Vector2 p = _waveRect.anchoredPosition;
        p.y = waveHideY;
        _waveRect.anchoredPosition = p;
    }

    private void ShowWaveAnimated()
    {
        if (_waveRect == null) return;
        _waveRect.gameObject.SetActive(true);

        _waveRect.DOKill();
        _waveRect.DOAnchorPosY(waveShowY, waveSlideDuration).SetEase(Ease.OutCubic);
    }

    private void HideWaveAnimated()
    {
        if (_waveRect == null) return;

        _waveRect.DOKill();
        _waveRect.DOAnchorPosY(waveHideY, waveSlideDuration)
            .SetEase(Ease.InCubic);
    }
}