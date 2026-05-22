using DG.Tweening;
using TMPro;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject hudPanel;
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private GameObject gameSettingPanel;

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
        GameManager.OnGamePause += HandleGamePause;
        GameManager.OnGameResume += HandleGameResume;
        GameManager.OnHomeButtonClick += HandleHomeClick;
        CarController.OnSpeedChanged += UpdateSpeed;
    }

    private void OnDisable()
    {
        GameManager.OnGameStart -= HandleGameStart;
        GameManager.OnGameOver -= HandleGameOver;
        GameManager.OnGameRestart -= HandleGameRestart;
        GameManager.OnGamePause -= HandleGamePause;
        GameManager.OnGameResume -= HandleGameResume;
        GameManager.OnHomeButtonClick -= HandleHomeClick;
        CarController.OnSpeedChanged -= UpdateSpeed;
    }

    private void HandleGameStart()
    {
        SetActiveSafe(mainMenuPanel, false);
        SetActiveSafe(gameOverPanel, false);
        SetActiveSafe(gameSettingPanel, false);
        SetActiveSafe(hudPanel, true);

        UpdateScore(0);
        
        if (_hudCanvasGroup != null)
        {
            _hudCanvasGroup.DOKill();
            _hudCanvasGroup.alpha = 0f;
            _hudCanvasGroup.DOFade(1f, 0.3f);
        }
    }

    private void HandleGamePause()
    {
        SetActiveSafe(gameSettingPanel,true);
    }
    
    private void HandleGameResume()
    {
        SetActiveSafe(gameSettingPanel,false);
    }

    private void HandleGameOver()
    {
        SetActiveSafe(hudPanel, false);
        SetActiveSafe(gameOverPanel, true);
        SetActiveSafe(gameSettingPanel,false);

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
        SetActiveSafe(mainMenuPanel, false);
        SetActiveSafe(gameSettingPanel,false);
    }

    private void HandleHomeClick()
    {
        SetActiveSafe(mainMenuPanel, true);
        SetActiveSafe(gameOverPanel, false);
        SetActiveSafe(gameSettingPanel,false);
        SetActiveSafe(hudPanel,false);
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
        
        finalScoreText.text = Mathf.FloorToInt(distance).ToString("D3") + "m";
    }

    public void UpdateSpeed(float speed)
    {
        if (speedText != null)
            speedText.text = Mathf.RoundToInt(speed).ToString("D2") + " km/h";
    }

    public void UpdateBestScore(float best)
    {
        Debug.Log($"UpdateBestScore => {best}");
        string text = Mathf.RoundToInt(best).ToString("D3");
        if (bestScoreText != null)
            bestScoreText.text = text;
        if (bestScoreLabel != null)
            bestScoreLabel.text = text;
    }
}
