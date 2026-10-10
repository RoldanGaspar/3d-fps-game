using System;
using UnityEngine;

/// <summary>
/// Central game state manager: menu, loading, playing, pause, game over and victory.
/// Owns the flow between the main menu and the arena, and banks the run rewards at the right moment.
/// </summary>
public class GameManager : Singleton<GameManager>
{
    public enum GameState { Menu, Loading, Playing, Paused, GameOver, Victory }

    public const string MenuScene = "MainMenu";
    public const string ArenaScene = "TestArena";

    [SerializeField] private GameState _currentState = GameState.Menu;

    public GameState CurrentState => _currentState;
    public event Action<GameState> OnStateChanged;

    protected override void Awake()
    {
        base.Awake();
        _ = SaveSystem.Profile; // load the profile early
        GameAudio.Initialize();
    }

    public void SetState(GameState newState)
    {
        if (_currentState == newState) return;
        _currentState = newState;
        OnStateChanged?.Invoke(newState);
    }

    // --- Flow ---

    public void StartLevel(int level)
    {
        GameSession.Begin(level);
        Time.timeScale = 1f;
        SetState(GameState.Loading);
        LevelManager.Instance.LoadScene(ArenaScene);
    }

    /// <summary>Called by the arena once it has finished setting up.</summary>
    public void ArenaReady()
    {
        Time.timeScale = 1f;
        SetState(GameState.Playing);
    }

    public void RestartLevel()
    {
        FinishRunIfNeeded();
        StartLevel(GameSession.CurrentLevel);
    }

    public void NextLevel()
    {
        FinishRunIfNeeded();
        StartLevel(GameSession.CurrentLevel + 1);
    }

    public void ReturnToMenu()
    {
        FinishRunIfNeeded();
        Time.timeScale = 1f;
        SetState(GameState.Menu);
        LevelManager.Instance.LoadScene(MenuScene);
    }

    public void PauseGame()
    {
        if (_currentState != GameState.Playing) return;
        SetState(GameState.Paused);
        Time.timeScale = 0f;
    }

    public void ResumeGame()
    {
        if (_currentState != GameState.Paused) return;
        Time.timeScale = 1f;
        SetState(GameState.Playing);
    }

    public void GameOver()
    {
        if (_currentState == GameState.GameOver) return;
        SetState(GameState.GameOver);
    }

    public void Victory()
    {
        if (_currentState == GameState.Victory) return;
        GameSession.EndRun(true);
        SetState(GameState.Victory);
    }

    /// <summary>Called when the player picks Revive and the run continues.</summary>
    public void Revived()
    {
        Time.timeScale = 1f;
        SetState(GameState.Playing);
    }

    private void FinishRunIfNeeded()
    {
        if (GameSession.Active) GameSession.EndRun(false);
    }

    // The player can close the app on the game over screen: don't lose the earned coins.
    private void OnApplicationPause(bool paused)
    {
        if (paused && _currentState == GameState.GameOver) FinishRunIfNeeded();
    }

    protected override void OnApplicationQuit()
    {
        if (_currentState == GameState.GameOver) FinishRunIfNeeded();
        base.OnApplicationQuit();
    }
}
