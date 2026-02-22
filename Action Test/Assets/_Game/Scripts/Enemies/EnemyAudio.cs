using UnityEngine;

[RequireComponent(typeof(Health))]
public class EnemyAudio : MonoBehaviour
{
    [SerializeField] private float maxDistance = 30f;

    private Health _health;

    private void Awake()
    {
        _health = GetComponent<Health>();
    }

    private void OnEnable()
    {
        GameEvents.DamageDealt += OnDamage;
        GameEvents.EntityDied += OnDied;
    }

    private void OnDisable()
    {
        GameEvents.DamageDealt -= OnDamage;
        GameEvents.EntityDied -= OnDied;
    }

    private void OnDamage(DamageEvent e)
    {
        if (AudioManager.Instance == null) return;
        if (e.TargetId != _health.EntityId) return;
        if (e.Amount <= 0) return;

        AudioManager.Instance.PlaySfx3D(AudioManager.Instance.Library.enemyHurt, transform.position, 1f, 2f, maxDistance);
    }

    private void OnDied(DeathEvent e)
    {
        if (AudioManager.Instance == null) return;
        if (e.EntityId != _health.EntityId) return;

        AudioManager.Instance.PlaySfx3D(AudioManager.Instance.Library.enemyDeath, transform.position, 1f, 2f, maxDistance);
    }
}