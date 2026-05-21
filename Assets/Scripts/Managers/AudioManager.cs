using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    private const string MutedKey = "Muted";
    private const float EngineSpeedMin = 10f;
    private const float EngineSpeedMax = 30f;
    private const float EnginePitchMin = 0.8f;
    private const float EnginePitchMax = 1.4f;

    [SerializeField] private AudioClip backgroundMusic;
    [SerializeField] private AudioClip engineSound;
    [SerializeField] private AudioClip collisionSFX;
    [SerializeField] private AudioClip coinSFX;
    [SerializeField] private AudioClip uiClickSFX;
    [SerializeField] private AudioClip speedUpSFX;

    private AudioSource musicSource;
    private AudioSource sfxSource;

    private CarController _car;
    private bool _muted;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        musicSource = gameObject.AddComponent<AudioSource>();
        musicSource.loop = true;
        musicSource.volume = 0.4f;
        musicSource.playOnAwake = false;

        sfxSource = gameObject.AddComponent<AudioSource>();
        sfxSource.loop = false;
        sfxSource.volume = 1f;
        sfxSource.playOnAwake = false;

        _muted = PlayerPrefs.GetInt(MutedKey, 0) == 1;
        ApplyMute();

        _car = FindFirstObjectByType<CarController>();
        if (_car == null)
            _car = FindAnyObjectByType<CarController>();
    }

    private void Update()
    {
        if (_car == null || GameManager.Instance == null)
            return;

        if (GameManager.Instance.State != GameManager.GameState.Playing)
            return;

        if (sfxSource == null || !sfxSource.isPlaying || sfxSource.clip != engineSound)
            return;

        UpdateEnginePitch(_car.CurrentSpeed);
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
    }

    private void OnDisable()
    {
        GameManager.OnGameStart -= HandleGameStart;
        GameManager.OnGameOver -= HandleGameOver;
        GameManager.OnGameRestart -= HandleGameRestart;
    }

    private void HandleGameStart()
    {
        PlayMusic();
        StartEngine();
    }

    private void HandleGameOver()
    {
        StopMusic();
        StopEngine();
        PlayCollision();
    }

    private void HandleGameRestart()
    {
        PlayMusic();
        StartEngine();
    }

    public void PlayMusic()
    {
        if (backgroundMusic == null || musicSource == null)
            return;

        if (musicSource.clip == backgroundMusic && musicSource.isPlaying)
            return;

        musicSource.clip = backgroundMusic;
        musicSource.loop = true;
        if (!_muted)
            musicSource.Play();
    }

    public void StopMusic()
    {
        if (musicSource != null)
            musicSource.Stop();
    }

    public void PlaySFX(AudioClip clip)
    {
        if (clip == null || sfxSource == null || _muted)
            return;

        sfxSource.PlayOneShot(clip);
    }

    public void PlayCollision() => PlaySFX(collisionSFX);
    public void PlayCoin() => PlaySFX(coinSFX);
    public void PlayUIClick() => PlaySFX(uiClickSFX);
    public void PlaySpeedUp() => PlaySFX(speedUpSFX);

    public void UpdateEnginePitch(float speed)
    {
        if (sfxSource == null || engineSound == null)
            return;

        float t = Mathf.InverseLerp(EngineSpeedMin, EngineSpeedMax, speed);
        sfxSource.pitch = Mathf.Lerp(EnginePitchMin, EnginePitchMax, t);
    }

    public void MuteAll(bool mute)
    {
        _muted = mute;
        PlayerPrefs.SetInt(MutedKey, mute ? 1 : 0);
        PlayerPrefs.Save();
        ApplyMute();
    }

    private void StartEngine()
    {
        if (engineSound == null || sfxSource == null)
            return;

        sfxSource.clip = engineSound;
        sfxSource.loop = true;
        if (!_muted)
            sfxSource.Play();
    }

    private void StopEngine()
    {
        if (sfxSource == null)
            return;

        sfxSource.Stop();
        sfxSource.loop = false;
        sfxSource.pitch = 1f;
    }

    private void ApplyMute()
    {
        if (musicSource != null)
            musicSource.mute = _muted;

        if (sfxSource != null)
            sfxSource.mute = _muted;
    }
}
