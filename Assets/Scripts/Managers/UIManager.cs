using DG.Tweening;
using TMPro;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject hudPanel;
    [SerializeField] private GameObject gameOverPanel;

    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private TextMeshProUGUI speedText;
    [SerializeField] private TextMeshProUGUI bestScoreText;
    [SerializeField] private TextMeshProUGUI finalScoreText;
    [SerializeField] private TextMeshProUGUI bestScoreLabel;

    private CanvasGroup _hudCanvasGroup;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (hudPanel != null)
        {
            _hudCanvasGroup = hudPanel.GetComponent<CanvasGroup>();
            if (_hudCanvasGroup == null)
                _hudCanvasGroup = hudPanel.AddComponent<CanvasGroup>();
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private void OnEnable()
    {
        GameManager.OnGameStart += HandleGameStart;
        GameManager.OnGameOver += HandleGameOver;
        GameManager.OnGameRestart += HandleGameRestart;
        CarController.OnSpeedChanged += UpdateSpeed;
    }

    private void OnDisable()
    {
        GameManager.OnGameStart -= HandleGameStart;
        GameManager.OnGameOver -= HandleGameOver;
        GameManager.OnGameRestart -= HandleGameRestart;
        CarController.OnSpeedChanged -= UpdateSpeed;
    }

    private void HandleGameStart()
    {
        SetActiveSafe(mainMenuPanel, false);
        SetActiveSafe(gameOverPanel, false);
        SetActiveSafe(hudPanel, true);

        UpdateScore(0);
        
        if (_hudCanvasGroup != null)
        {
            _hudCanvasGroup.DOKill();
            _hudCanvasGroup.alpha = 0f;
            _hudCanvasGroup.DOFade(1f, 0.3f);
        }
    }

    private void HandleGameOver()
    {
        SetActiveSafe(hudPanel, false);
        SetActiveSafe(gameOverPanel, true);

        if (gameOverPanel != null)
        {
            Transform t = gameOverPanel.transform;
            t.DOKill();
            t.localScale = Vector3.one * 0.8f;
            t.DOScale(Vector3.one, 0.4f).SetEase(Ease.OutBack);
        }
    }

    private void HandleGameRestart()
    {
        SetActiveSafe(gameOverPanel, false);
        SetActiveSafe(mainMenuPanel, true);
    }

    private static void SetActiveSafe(GameObject go, bool active)
    {
        if (go != null && go.activeSelf != active)
            go.SetActive(active);
    }

    public void UpdateScore(float distance)
    {
        if (scoreText != null)
            scoreText.text = Mathf.FloorToInt(distance).ToString("D3") + "m";
    }

    public void UpdateSpeed(float speed)
    {
        if (speedText != null)
            speedText.text = Mathf.RoundToInt(speed).ToString("D2") + " km/h";
    }

    public void UpdateBestScore(float best)
    {
        string text = best.ToString("D3");
        if (bestScoreText != null)
            bestScoreText.text = text;
        if (bestScoreLabel != null)
            bestScoreLabel.text = text;
    }
}
