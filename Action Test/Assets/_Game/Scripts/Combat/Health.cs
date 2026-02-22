using UnityEngine;

public class Health : MonoBehaviour
{
    [SerializeField] private string entityId = "Entity";
    [SerializeField] private int maxHp = 10;

    public string EntityId => entityId;
    public int MaxHp => maxHp;
    public int Hp { get; private set; }

    private bool _dead;

    private void Awake()
    {
        Hp = maxHp;
        _dead = false;
    }

    public void SetEntityId(string id) => entityId = id;

    public void TakeDamage(int amount, string sourceId)
    {
        if (_dead) return;

        amount = Mathf.Max(0, amount);
        Hp = Mathf.Max(0, Hp - amount);

        GameEvents.Raise(new DamageEvent(amount, sourceId, entityId));

        if (Hp <= 0)
        {
            _dead = true;
            GameEvents.Raise(new DeathEvent(entityId, sourceId));
            OnDeath();
        }
    }

    private void OnDeath()
    {
        // Enemy: destroy. Player: handle later.
        Destroy(gameObject);
    }
}