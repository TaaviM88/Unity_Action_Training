using UnityEngine;

[CreateAssetMenu(menuName = "Arena/Enemy Archetype", fileName = "EnemyArchetype_")]
public class EnemyArchetypeSO : ScriptableObject
{
    public string id = "CubeEnemy";
    public GameObject prefab;
    public int hp = 6;
    public float moveSpeed = 3.5f;
    public int touchDamage = 1;
    public int scoreOnKill = 10;
}