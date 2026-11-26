using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public float ElapsedTime { get; private set; }
    public int Kills { get; private set; }
    public int Level { get; set; } = 1;
    public bool IsGameOver { get; private set; }
    public bool IsPaused { get; private set; }

    public event Action onGameOver;
    public event Action onPauseChanged;

    void Awake()
    {
        Instance = this;
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = 60;
        Time.timeScale = 1f;
        DamageTracker.Reset();
        Enemy.ClearAll();
    }

    void Update()
    {
        if (!IsGameOver && !IsPaused) ElapsedTime += Time.deltaTime;
    }

    public void AddKill() { Kills++; }

    public void SetPaused(bool paused)
    {
        if (IsGameOver) return;
        IsPaused = paused;
        Time.timeScale = paused ? 0f : 1f;
        onPauseChanged?.Invoke();
    }

    public void GameOver()
    {
        if (IsGameOver) return;
        IsGameOver = true;
        Time.timeScale = 0f;
        onGameOver?.Invoke();
    }

    public void Restart()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

#if !UNITY_EDITOR
    void OnApplicationFocus(bool focus) { if (!focus) SetPaused(true); }
#endif

    [ContextMenu("TEST: fake damage + kills")]
    void TestFake()
    {
        DamageTracker.Add("starshot", 125000f);
        DamageTracker.Add("moonlets", 48000f);
        DamageTracker.Add("pulsar", 310000f);
        DamageTracker.Add("supernova", 9000f);
        Kills += 137;
    }
}