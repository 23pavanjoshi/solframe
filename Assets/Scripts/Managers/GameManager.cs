using System;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public enum GameState
    {
        Menu,
        Playing,
        GameOver
    }

    public static GameManager Instance { get; private set; }

    public static event Action OnGameStart;
    public static event Action OnGameOver;
    public static event Action OnGameRestart;

    private const string BestScoreKey = "BestScore";

    [SerializeField] private Transform player;
    [SerializeField] private TrafficSpawner trafficSpawner;

    public GameState State { get; private set; } = GameState.Menu;

    public float Score { get; private set; }
    public float BestScore { get; private set; }

    private float _scoreStartPlayerZ;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        // DontDestroyOnLoad requires a root GameObject. If this was placed under another object in
        // the scene hierarchy, detach it first to avoid the warning.
        if (transform.parent != null)
            transform.SetParent(null);
        DontDestroyOnLoad(gameObject);

        BestScore = PlayerPrefs.GetFloat(BestScoreKey, 0f);
    }

    private void OnEnable()
    {
        TryHookTrafficSpawner();
    }

    private void OnDisable()
    {
        if (trafficSpawner != null)
            trafficSpawner.OnTrafficCollision -= HandleTrafficCollision;
    }

    private void Update()
    {
        if (State != GameState.Playing || player == null)
            return;

        Score = Mathf.Max(0f, player.position.z - _scoreStartPlayerZ);

        UIManager.Instance.UpdateScore(Score);
        // UIManager.Instance.UpdateSpeed(speed);
        if (Score > BestScore)
        {
            Debug.Log("Score : " + Score);
            BestScore = Score;
        }
    }

    public void StartGame()
    {
        if (player == null)
            TryResolvePlayer();

        Score = 0f;
        _scoreStartPlayerZ = player != null ? player.position.z : 0f;

        SetState(GameState.Playing);
        OnGameStart?.Invoke();
    }

    public void RestartGame()
    {
        SaveBestScoreIfNeeded();

        Score = 0f;
        _scoreStartPlayerZ = player != null ? player.position.z : 0f;

        SetState(GameState.Playing);
        OnGameRestart?.Invoke();
    }

    public void PauseGame()
    {
        // Time.timeScale = 0f;
    }

    public void QuitGame()
    {
        SaveBestScoreIfNeeded();
        Application.Quit();
    }

    private void HandleTrafficCollision()
    {
        if (State == GameState.GameOver)
            return;

        SetState(GameState.GameOver);
        SaveBestScoreIfNeeded();
        OnGameOver?.Invoke();
        UIManager.Instance.UpdateBestScore(BestScore);
    }

    private void TryHookTrafficSpawner()
    {
        if (trafficSpawner == null)
        {
            trafficSpawner = FindFirstObjectByType<TrafficSpawner>();
            if (trafficSpawner == null)
                trafficSpawner = FindAnyObjectByType<TrafficSpawner>();
        }

        if (trafficSpawner != null)
        {
            trafficSpawner.OnTrafficCollision -= HandleTrafficCollision;
            trafficSpawner.OnTrafficCollision += HandleTrafficCollision;
        }
    }

    private void TryResolvePlayer()
    {
        // Prefer explicit assignment; otherwise try to find the CarController in scene.
        var car = FindFirstObjectByType<CarController>();
        if (car == null)
            car = FindAnyObjectByType<CarController>();

        if (car != null)
            player = car.transform;
    }

    private void SetState(GameState newState)
    {
        if (State == newState)
            return;

        State = newState;
        Debug.Log($"[GameManager] State changed to: {State}");
    }

    private void SaveBestScoreIfNeeded()
    {
        if (BestScore <= 0f)
            return;

        PlayerPrefs.SetFloat(BestScoreKey, BestScore);
        PlayerPrefs.Save();
    }
}
