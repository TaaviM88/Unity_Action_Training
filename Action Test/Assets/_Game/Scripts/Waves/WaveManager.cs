using System.Collections;
using UnityEngine;

public class WaveManager : MonoBehaviour
{
    [SerializeField] private WaveConfigSO config;
    [SerializeField] private Transform[] spawnPoints;
    [SerializeField] private Transform player;

    private int _waveIndex;
    private int _alive;
    private int _score;

    private void OnEnable()
    {
        GameEvents.EntityDied += OnEntityDied;
    }

    private void OnDisable()
    {
        GameEvents.EntityDied -= OnEntityDied;
    }

    private void Start()
    {
        if (!player)
        {
            var pc = FindAnyObjectByType<DoomFpsController>();
            if (pc) player = pc.transform;
        }

        StartCoroutine(RunWaves());
    }

    private IEnumerator RunWaves()
    {
        _waveIndex = 0;

        while (config != null && _waveIndex < config.waves.Length)
        {
            var w = config.waves[_waveIndex];
            GameEvents.Raise(new WaveEvent(_waveIndex));

            _alive = 0;

            for (int i = 0; i < w.totalCount; i++)
            {
                SpawnRandom(w);
                yield return new WaitForSeconds(w.spawnInterval);
            }

            // Wait until wave cleared
            while (_alive > 0)
                yield return null;

            GameEvents.Raise(new WaveEvent(_waveIndex), completed: true);
            _waveIndex++;

            yield return new WaitForSeconds(1.0f);
        }
    }

    private void SpawnRandom(WaveConfigSO.Wave w)
    {
        if (w.enemyTypes == null || w.enemyTypes.Length == 0) return;
        if (spawnPoints == null || spawnPoints.Length == 0) return;

        var archetype = w.enemyTypes[Random.Range(0, w.enemyTypes.Length)];
        if (archetype == null || archetype.prefab == null) return;

        Transform sp = spawnPoints[Random.Range(0, spawnPoints.Length)];
        GameObject go = Instantiate(archetype.prefab, sp.position, Quaternion.identity);

        var enemy = go.GetComponent<CubeEnemy>();
        if (enemy != null) enemy.Init(archetype, player);

        _alive++;
    }

    private void OnEntityDied(DeathEvent e)
    {
        // If an enemy died, decrement alive and add score.
        // We identify enemies by prefix convention in Init() (EnemyId_InstanceID).
        if (e.EntityId.StartsWith("CubeEnemy") || e.EntityId.StartsWith("Enemy"))
        {
            _alive = Mathf.Max(0, _alive - 1);

            // For v0.1 score: flat
            _score += 10;
            GameEvents.Raise(new ScoreEvent(_score));
        }

        // If player died: later we can stop the run / show game over
        if (e.EntityId == "Player")
        {
            // TODO: game over
        }
    }
}