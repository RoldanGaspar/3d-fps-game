using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Spawns enemies in waves at the designated spawn points. A new wave starts a few seconds after the
/// previous one has been cleared. The number of waves, enemies per wave and enemy strength come from
/// the current <see cref="LevelConfig"/>; when the last wave is cleared the level is complete.
/// </summary>
public class EnemySpawner : MonoBehaviour
{
    [Header("Spawn Settings")]
    [Tooltip("Index 0 is the basic enemy. The others unlock as levels go up (see UnlockLevels).")]
    [SerializeField] private GameObject[] _enemyPrefabs;
    [Tooltip("Level at which each prefab starts to appear (same order as the prefabs).")]
    [SerializeField] private int[] _unlockLevels = { 1, 3, 5, 7 };
    [SerializeField] private GameObject _bossPrefab;
    [SerializeField] private Transform[] _spawnPoints;
    [SerializeField] private float _spawnInterval = 1.2f;
    [SerializeField] private int _maxEnemies = 8;

    [Header("Waves")]
    [SerializeField] private bool _autoStart = true;
    [SerializeField] private float _firstWaveDelay = 4f;
    [SerializeField] private float _timeBetweenWaves = 6f;
    [SerializeField] private int _wavesInLevel = 3;
    [SerializeField] private int _firstWaveSize = 2;
    [SerializeField] private int _enemiesAddedPerWave = 1;
    [SerializeField] private float _minSpawnDistanceFromPlayer = 12f;

    private readonly List<GameObject> _activeEnemies = new List<GameObject>();
    private LevelConfig _config = DifficultyCurve.Get(1);
    private Transform _player;
    private int _wave;
    private bool _started;
    private bool _stopped;

    public int Wave => _wave;
    public int WavesInLevel => _wavesInLevel;
    public int EnemiesAlive
    {
        get
        {
            _activeEnemies.RemoveAll(e => e == null);
            return _activeEnemies.Count;
        }
    }

    public event System.Action<int> OnWaveStarted;
    public event System.Action<int> OnWaveCleared;
    public event System.Action OnLevelCleared;

    /// <summary>Applies a level's difficulty. Call before the first wave starts.</summary>
    public void Configure(LevelConfig config)
    {
        _config = config;
        _wavesInLevel = config.waves;
        _firstWaveSize = config.firstWaveSize;
        _enemiesAddedPerWave = config.enemiesAddedPerWave;
        _maxEnemies = config.maxAlive;
    }

    private void Start()
    {
        var p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) _player = p.transform;
        _started = true;
        if (_autoStart) StartCoroutine(WaveLoop());
    }

    public void StopSpawning()
    {
        _stopped = true;
    }

    private IEnumerator WaveLoop()
    {
        yield return new WaitForSeconds(_firstWaveDelay);

        while (!_stopped)
        {
            _wave++;
            int count = _firstWaveSize + (_wave - 1) * _enemiesAddedPerWave;
            GameSession.RegisterWave(_wave);
            OnWaveStarted?.Invoke(_wave);

            // Boss levels: the boss arrives with the final wave.
            bool bossWave = _config.hasBoss && _wave >= _wavesInLevel && _bossPrefab != null;
            if (bossWave) SpawnEnemy(_bossPrefab);

            for (int i = 0; i < count && !_stopped; i++)
            {
                while (EnemiesAlive >= _maxEnemies && !_stopped) yield return null;
                SpawnEnemy();
                yield return new WaitForSeconds(_spawnInterval);
            }

            while (EnemiesAlive > 0 && !_stopped) yield return new WaitForSeconds(0.5f);
            if (_stopped) yield break;

            OnWaveCleared?.Invoke(_wave);
            if (_wave >= _wavesInLevel)
            {
                OnLevelCleared?.Invoke();
                yield break;
            }

            yield return new WaitForSeconds(_timeBetweenWaves);
        }
    }

    public GameObject SpawnEnemy() => SpawnEnemy(null);

    /// <summary>Spawns one enemy. Pass a prefab to force a type (boss); otherwise one is picked for the level.</summary>
    public GameObject SpawnEnemy(GameObject forcedPrefab)
    {
        if (_enemyPrefabs == null || _enemyPrefabs.Length == 0 || _spawnPoints == null || _spawnPoints.Length == 0)
            return null;

        Transform point = PickSpawnPoint();
        Vector3 pos = point.position;
        if (NavMesh.SamplePosition(pos, out var hit, 4f, NavMesh.AllAreas))
            pos = hit.position;

        var prefab = forcedPrefab != null ? forcedPrefab : PickPrefab();
        var enemy = Instantiate(prefab, pos, Quaternion.Euler(0f, Random.Range(0f, 360f), 0f));

        var health = enemy.GetComponent<EnemyBase>();
        if (health != null) health.ApplyHealthMultiplier(_config.healthMultiplier);
        var ai = enemy.GetComponent<EnemyAI>();
        if (ai != null) ai.ApplyDifficulty(_config.damageMultiplier, _config.spreadMultiplier, _config.reactionMultiplier);

        _activeEnemies.Add(enemy);
        return enemy;
    }

    /// <summary>Picks among the enemy types unlocked at this level. Newer types are slightly rarer.</summary>
    private GameObject PickPrefab()
    {
        int level = _config.level;
        float total = 0f;
        for (int i = 0; i < _enemyPrefabs.Length; i++)
        {
            if (_enemyPrefabs[i] == null) continue;
            int unlock = i < _unlockLevels.Length ? _unlockLevels[i] : 1;
            if (level >= unlock) total += i == 0 ? 3f : 1.5f;
        }

        float roll = Random.value * total;
        for (int i = 0; i < _enemyPrefabs.Length; i++)
        {
            if (_enemyPrefabs[i] == null) continue;
            int unlock = i < _unlockLevels.Length ? _unlockLevels[i] : 1;
            if (level < unlock) continue;
            roll -= i == 0 ? 3f : 1.5f;
            if (roll <= 0f) return _enemyPrefabs[i];
        }
        return _enemyPrefabs[0];
    }

    private Transform PickSpawnPoint()
    {
        // Prefer points that are not right next to the player.
        for (int i = 0; i < 6; i++)
        {
            var candidate = _spawnPoints[Random.Range(0, _spawnPoints.Length)];
            if (_player == null || Vector3.Distance(candidate.position, _player.position) >= _minSpawnDistanceFromPlayer)
                return candidate;
        }
        return _spawnPoints[Random.Range(0, _spawnPoints.Length)];
    }
}
