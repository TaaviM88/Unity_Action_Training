using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Health))]
public class EnemyTouchDamage : MonoBehaviour
{
    [Header("Damage")]
    [SerializeField] private int touchDamage = 1;
    [SerializeField] private float damageInterval = 0.35f;

    [Tooltip("Optional: only damage colliders on these layers (e.g., Player). Leave as Everything if you don't care.")]
    [SerializeField] private LayerMask targetLayers = ~0;

    [Header("Optional Knockback (Doom-ish)")]
    [SerializeField] private bool applyKnockback = true;
    [SerializeField] private float knockbackStrength = 3.5f;

    private Health _self;

    // Per-target cooldown (so if multiple targets are inside, each has its own interval)
    private readonly Dictionary<Health, float> _nextDamageTimeByTarget = new();

    private void Awake()
    {
        _self = GetComponent<Health>();
    }

    public void SetDamage(int dmg) => touchDamage = Mathf.Max(0, dmg);

    private void OnTriggerStay(Collider other)
    {
        // Fast layer filter (optional but good)
        if (((1 << other.gameObject.layer) & targetLayers) == 0)
            return;

        // Find Health on the thing we touched
        var targetHealth = other.GetComponentInParent<Health>();
        if (targetHealth == null)
            return;

        // v0.1: only damage the player
        if (targetHealth.EntityId != "Player")
            return;

        float now = Time.time;

        // Per-target cooldown
        if (_nextDamageTimeByTarget.TryGetValue(targetHealth, out float nextTime) && now < nextTime)
            return;

        _nextDamageTimeByTarget[targetHealth] = now + damageInterval;

        // Deal damage (raises events)
        Debug.Log($"EnemyTouchDamage: Dealing {touchDamage} to {targetHealth.EntityId}");
        targetHealth.TakeDamage(touchDamage, _self.EntityId);

        // Optional: tiny knockback on the player's CharacterController
        if (applyKnockback)
        {
            var cc = targetHealth.GetComponent<CharacterController>();
            if (cc != null)
            {
                Vector3 dir = (cc.transform.position - transform.position);
                dir.y = 0f;

                if (dir.sqrMagnitude > 0.0001f)
                {
                    dir.Normalize();
                    cc.Move(dir * (knockbackStrength * Time.deltaTime));
                }
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        // Cleanup dictionary entries when things leave the trigger
        var h = other.GetComponentInParent<Health>();
        if (h != null)
            _nextDamageTimeByTarget.Remove(h);
    }
}