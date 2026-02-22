using System;
using UnityEngine;

[CreateAssetMenu(menuName = "Arena/Wave Config", fileName = "WaveConfig_")]
public class WaveConfigSO : ScriptableObject
{
    public Wave[] waves;

    [Serializable]
    public class Wave
    {
        public EnemyArchetypeSO[] enemyTypes;
        public int totalCount = 10;
        public float spawnInterval = 0.6f;
    }
}