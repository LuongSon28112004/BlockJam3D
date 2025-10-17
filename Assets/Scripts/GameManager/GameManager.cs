using UnityEngine;
using System;
using master;
using System.Collections;
using UnityEngine.SceneManagement;

public enum GameState
{
    None,
    Menu,
    GamePlay,
    Pause,
    Win,
    Lose
}

public class GameManager : SingletonDDOL<GameManager>
{
    public GameState gameState = GameState.None;

    public static event Action<GameState> OnGameStateChanged;

    public int Level = 1;

    private void Start()
    {
        // Khi bắt đầu game, luôn vào menu
        StartCoroutine(ChangeState(GameState.Menu));
    }

    public IEnumerator ChangeState(GameState newState)
    {
        if (gameState == newState)
            yield break;

        gameState = newState;
        if (newState != GameState.Pause && newState != GameState.Lose && newState != GameState.Win)
        {
            Time.timeScale = 1;
        }
        OnGameStateChanged?.Invoke(gameState);

        switch (gameState)
        {
            case GameState.Menu:
                UIManager.Instance.ShowScreen<ScreenMainMenu>();
                break;

            case GameState.GamePlay:
                yield return LoadSceneAndWait("GamePlay", () =>
                {
                    LevelManager.Instance.Init();
                    UIManager.Instance.ShowScreen<ScreenGamePlay>();
                });
                break;
            case GameState.Win:
                UIManager.Instance.ShowPopup<PopupWinGame>(null);
                Time.timeScale = 0;
                break;

            case GameState.Lose:
                UIManager.Instance.ShowPopup<PopupLoseGame>(null);
                Time.timeScale = 0;
                break;

            case GameState.Pause:
                Time.timeScale = 0;
                break;
        }
    }
    
    private IEnumerator LoadSceneAndWait(string sceneName, Action onLoaded)
    {
        AsyncOperation async = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
        while (!async.isDone)
            yield return null;

        yield return null; // đợi 1 frame để scene hoàn toàn active
        onLoaded?.Invoke();
    }


    // ---- Các hàm public gọi state ----
    public void StartGame()
    {
        StartCoroutine(ChangeState(GameState.GamePlay));
    }

    public void WinGame()
    {
        StartCoroutine(ChangeState(GameState.Win));
    }

    public void LoseGame()
    {
        StartCoroutine(ChangeState(GameState.Lose));
    }

    public void BackToMenu()
    {
        StartCoroutine(ChangeState(GameState.Menu));
    }

    public void PauseGame()
    {
        StartCoroutine(ChangeState(GameState.Pause));
    }

    public void ResumeGame()
    {
        Time.timeScale = 1;
        StartCoroutine(ChangeState(GameState.GamePlay));
    }
}
