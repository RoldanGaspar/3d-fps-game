using UnityEngine;

/// <summary>
/// Sits in the arena scene and connects the pieces of a run: applies the player's loadout and the level's
/// difficulty, counts kills for rewards, shows the in-run UI (HUD, pause, perks, items, result screens),
/// offers a perk between waves and ends the level when the last wave is cleared.
/// Works when the scene is opened directly in the editor (starts level 1).
/// </summary>
public class LevelDirector : MonoBehaviour
{
    private EnemySpawner _spawner;
    private PerkPickerUI _perks;

    private void Awake()
    {
        // Make sure the managers exist even if the scene was opened directly.
        _ = GameManager.Instance;
        _ = LevelManager.Instance;
        GameSession.EnsureStarted();

        Ensure<RunResultUI>("RunResultUI");
        Ensure<RunHud>("RunHud");
        Ensure<PauseMenuUI>("PauseMenuUI");
        Ensure<ConsumableBar>("ConsumableBar");
        _perks = Ensure<PerkPickerUI>("PerkPickerUI");
    }

    private static T Ensure<T>(string name) where T : Component
    {
        var existing = FindFirstObjectByType<T>();
        return existing != null ? existing : new GameObject(name).AddComponent<T>();
    }

    private void Start()
    {
        Time.timeScale = 1f;
        Time.fixedDeltaTime = 0.02f;

        _spawner = FindFirstObjectByType<EnemySpawner>();
        if (_spawner != null)
        {
            _spawner.Configure(GameSession.Config);
            _spawner.OnLevelCleared += OnLevelCleared;
            _spawner.OnWaveCleared += OnWaveCleared;
        }

        EnemyBase.AnyEnemyDied += OnEnemyDied;
        GameManager.Instance.ArenaReady();

        StartCoroutine(ApplyLoadoutNextFrame());
    }

    // Wait one frame so the player's weapon manager has finished its own startup.
    private System.Collections.IEnumerator ApplyLoadoutNextFrame()
    {
        yield return null;
        var player = GameObject.FindGameObjectWithTag("Player");
        PlayerLoadout.Apply(player);

        var look = player != null ? player.GetComponent<PlayerLook>() : null;
        if (look != null) look.SetSensitivity(2f);   // the look speed scale comes from GameSettings.LookScale
    }

    private void OnDestroy()
    {
        EnemyBase.AnyEnemyDied -= OnEnemyDied;
        if (_spawner != null)
        {
            _spawner.OnLevelCleared -= OnLevelCleared;
            _spawner.OnWaveCleared -= OnWaveCleared;
        }
    }

    private void OnEnemyDied(EnemyBase enemy)
    {
        // The training dummy has no AI and gives no rewards.
        if (enemy.GetComponent<EnemyAI>() == null) return;
        float scale = enemy.Data != null ? Mathf.Max(0.5f, enemy.Data.scoreValue / 100f) : 1f;
        GameSession.RegisterKill(scale);
    }

    private void OnWaveCleared(int wave)
    {
        // Offer a perk between waves (the last wave goes straight to the victory screen).
        if (_spawner != null && wave >= _spawner.WavesInLevel) return;
        var death = FindFirstObjectByType<PlayerDeathController>();
        if (death != null && death.IsDead) return;
        if (_perks != null) _perks.Show();
    }

    private void OnLevelCleared()
    {
        var death = FindFirstObjectByType<PlayerDeathController>();
        if (death != null && death.IsDead) return;
        GameManager.Instance.Victory();
    }
}
