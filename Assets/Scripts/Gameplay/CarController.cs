using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.InputSystem;

public class CarController : MonoBehaviour
{
    public event Action<int> OnLaneChanged;
    public static event Action<float> OnSpeedChanged;

    [Header("Speed")]
    [SerializeField] private float startSpeed = 10f;
    [SerializeField] private float speedIncreaseAmount = 0.5f;
    [SerializeField] private float speedIncreaseIntervalSeconds = 10f;
    [SerializeField] private float maxSpeed = 30f;

    [Header("Lanes")]
    [SerializeField] private float leftLaneX = -3.5f;
    [SerializeField] private float centerLaneX = 0f;
    [SerializeField] private float rightLaneX = 3.5f;
    [SerializeField] private float laneChangeDuration = 0.2f;

    [Header("Input")]
    [SerializeField] private float swipeDeltaThresholdPixels = 50f;

    private readonly float[] _laneXs = new float[3];
    private int _currentLaneIndex = 1; // 0=left, 1=center, 2=right

    private float _currentSpeed;
    private float _speedTimer;

    private Tween _laneTween;
    private bool _isChangingLane;

    private bool _touchTracking;
    private Vector2 _touchStartPos;

    private void Awake()
    {
        _laneXs[0] = leftLaneX;
        _laneXs[1] = centerLaneX;
        _laneXs[2] = rightLaneX;
    }

    private void OnEnable()
    {
        GameManager.OnGameStart += HandleGameStart;
        _currentSpeed = Mathf.Clamp(startSpeed, 0f, maxSpeed);
        _speedTimer = 0f;

        SnapToCurrentLaneX();
        OnSpeedChanged?.Invoke(_currentSpeed);
        OnLaneChanged?.Invoke(_currentLaneIndex);
    }

    private void OnDisable()
    {
        GameManager.OnGameStart -= HandleGameStart;
        _laneTween?.Kill();
        _laneTween = null;
        _isChangingLane = false;
        _touchTracking = false;
    }
    
    private void HandleGameStart()
    {
        OnSpeedChanged?.Invoke(_currentSpeed);
    }

    private void Update()
    {
        if (GameManager.Instance.State != GameManager.GameState.Playing)
            return;
        
        MoveForward();
        TickSpeedRamp();
        HandleKeyboardInput();
        HandleTouchSwipeInput();
    }

    private void MoveForward()
    {
        transform.position += Vector3.forward * (_currentSpeed * Time.deltaTime);
    }

    private void TickSpeedRamp()
    {
        if (_currentSpeed >= maxSpeed)
            return;

        _speedTimer += Time.deltaTime;
        if (_speedTimer < speedIncreaseIntervalSeconds)
            return;

        _speedTimer -= speedIncreaseIntervalSeconds;

        var oldSpeed = _currentSpeed;
        _currentSpeed = Mathf.Min(maxSpeed, _currentSpeed + speedIncreaseAmount);
        if (!Mathf.Approximately(oldSpeed, _currentSpeed))
            OnSpeedChanged?.Invoke(_currentSpeed);
    }

    private void HandleKeyboardInput()
    {
        var kb = Keyboard.current;
        if (kb == null)
            return;

        if (kb.leftArrowKey.wasPressedThisFrame || kb.aKey.wasPressedThisFrame)
            TryChangeLane(-1);
        else if (kb.rightArrowKey.wasPressedThisFrame || kb.dKey.wasPressedThisFrame)
            TryChangeLane(+1);
    }

    private void HandleTouchSwipeInput()
    {
        var ts = Touchscreen.current;
        if (ts == null)
            return;

        var touch = ts.primaryTouch;

        if (touch.press.wasPressedThisFrame)
        {
            _touchTracking = true;
            _touchStartPos = touch.position.ReadValue();
        }

        if (!_touchTracking)
            return;

        if (touch.press.wasReleasedThisFrame)
        {
            _touchTracking = false;

            // Prefer delta (as requested) but fall back to end-start if delta is unavailable/zero.
            var delta = touch.delta.ReadValue();
            if (delta == Vector2.zero)
            {
                var endPos = touch.position.ReadValue();
                delta = endPos - _touchStartPos;
            }

            if (Mathf.Abs(delta.x) < swipeDeltaThresholdPixels)
                return;

            TryChangeLane(delta.x < 0f ? -1 : +1);
        }
    }

    private void TryChangeLane(int direction)
    {
        if (_isChangingLane)
            return;

        var targetLane = Mathf.Clamp(_currentLaneIndex + direction, 0, 2);
        if (targetLane == _currentLaneIndex)
            return;

        _currentLaneIndex = targetLane;
        _isChangingLane = true;

        _laneTween?.Kill();

        var targetX = _laneXs[_currentLaneIndex];
        _laneTween = transform
            .DOMoveX(targetX, laneChangeDuration)
            .SetEase(Ease.OutQuad)
            .OnComplete(() =>
            {
                _isChangingLane = false;
                OnLaneChanged?.Invoke(_currentLaneIndex);
            });
    }

    private void SnapToCurrentLaneX()
    {
        var p = transform.position;
        p.x = _laneXs[_currentLaneIndex];
        transform.position = p;
    }
}
