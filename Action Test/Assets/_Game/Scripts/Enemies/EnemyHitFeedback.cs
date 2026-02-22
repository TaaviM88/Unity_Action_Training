using UnityEngine;
using DG.Tweening;

[RequireComponent(typeof(Health))]
public class EnemyHitFeedback : MonoBehaviour
{
    [Header("Tween")]
    [SerializeField] private float punchScale = 0.12f;
    [SerializeField] private float duration = 0.12f;

    private Health _health;
    private Vector3 _baseScale;
    private Tween _tween;

    private void Awake()
    {
        _health = GetComponent<Health>();
        _baseScale = transform.localScale;
    }

    private void OnEnable()
    {
        GameEvents.DamageDealt += OnDamage;
        GameEvents.EntityDied += OnDeath;
    }

    private void OnDisable()
    {
        GameEvents.DamageDealt -= OnDamage;
        GameEvents.EntityDied -= OnDeath;
        _tween?.Kill();
    }

    private void OnDamage(DamageEvent e)
    {
        if (e.TargetId != _health.EntityId) return;

        _tween?.Kill();
        transform.localScale = _baseScale;
        _tween = transform.DOPunchScale(Vector3.one * punchScale, duration, vibrato: 8, elasticity: 0.6f);
    }

    private void OnDeath(DeathEvent e)
    {
        if (e.EntityId != _health.EntityId) return;

        // Optional: quick shrink before destroy (Health destroys immediately now)
        // If you want this visible, we can move destruction responsibility out of Health later.
    }
}