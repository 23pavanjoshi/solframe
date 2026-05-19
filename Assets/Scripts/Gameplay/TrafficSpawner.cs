using System;
using System.Collections.Generic;
// using Unity.Cinemachine;
using UnityEngine;

public class TrafficSpawner : MonoBehaviour
{
    public event Action OnTrafficCollision;

    [SerializeField] private GameObject trafficPrefab;
    [SerializeField] private int poolSize = 10;
    [SerializeField] private float spawnInterval = 3f;
    [SerializeField] private Transform player;

    private const float TrafficSpeed = 8f;
    private const float SpawnAheadDistance = 80f;
    private const float DespawnBehindPlayerDistance = 30f;

    private const float IntervalDecreaseEverySeconds = 30f;
    private const float IntervalDecreaseAmount = 0.3f;
    private const float MinSpawnInterval = 0.8f;

    private static readonly float[] LaneXs = { -3.5f, 0f, 3.5f };

    private readonly Queue<TrafficCarPooled> _pool = new Queue<TrafficCarPooled>(16);
    private readonly List<TrafficCarPooled> _active = new List<TrafficCarPooled>(16);

    private float _spawnTimer;
    private float _elapsed;
    
    // private static CinemachineImpulseSource _impulseSource;


    // private void Awake()
    // {
    //     _impulseSource = GetComponent<CinemachineImpulseSource>();
    // }

    private void Start()
    {
        if (trafficPrefab == null || player == null)
        {
            enabled = false;
            return;
        }

        if (poolSize <= 0)
            poolSize = 1;

        if (spawnInterval <= 0f)
            spawnInterval = 0.1f;

        for (var i = 0; i < poolSize; i++)
        {
            var go = Instantiate(trafficPrefab, transform);
            go.SetActive(false);

            var pooled = go.GetComponent<TrafficCarPooled>();
            if (pooled == null)
                pooled = go.AddComponent<TrafficCarPooled>();

            pooled.Init(this, player, TrafficSpeed);

            _pool.Enqueue(pooled);
        }
    }

    private void Update()
    {
        if (player == null || GameManager.Instance.State != GameManager.GameState.Playing)
            return;

        _elapsed += Time.deltaTime;
        _spawnTimer += Time.deltaTime;

        var currentInterval = GetCurrentSpawnInterval();
        if (_spawnTimer >= currentInterval)
        {
            // Prevent huge burst spawning if app hitches.
            _spawnTimer = Mathf.Min(_spawnTimer - currentInterval, currentInterval);
            TrySpawn();
        }

        DespawnBehindPlayer();
    }

    private float GetCurrentSpawnInterval()
    {
        var steps = Mathf.FloorToInt(_elapsed / IntervalDecreaseEverySeconds);
        var interval = spawnInterval - (steps * IntervalDecreaseAmount);
        return Mathf.Max(MinSpawnInterval, interval);
    }

    private void TrySpawn()
    {
        if (_pool.Count == 0)
            return;

        var car = _pool.Dequeue();

        var p = player.position;
        var laneX = LaneXs[UnityEngine.Random.Range(0, LaneXs.Length)];
        var spawnPos = new Vector3(laneX, car.transform.position.y, p.z + SpawnAheadDistance);

        car.transform.position = spawnPos;
        car.gameObject.SetActive(true);

        _active.Add(car);
    }

    private void DespawnBehindPlayer()
    {
        var despawnZ = player.position.z - DespawnBehindPlayerDistance;
        for (var i = _active.Count - 1; i >= 0; i--)
        {
            var car = _active[i];
            if (car == null)
            {
                _active.RemoveAt(i);
                continue;
            }

            if (car.transform.position.z >= despawnZ)
                continue;

            car.gameObject.SetActive(false);
            _active.RemoveAt(i);
            _pool.Enqueue(car);
        }
    }

    internal void RaiseTrafficCollision()
    {
        OnTrafficCollision?.Invoke();
    }

    private sealed class TrafficCarPooled : MonoBehaviour
    {
        private TrafficSpawner _spawner;
        private Transform _player;
        private float _speed;

        public void Init(TrafficSpawner spawner, Transform player, float speed)
        {
            _spawner = spawner;
            _player = player;
            _speed = speed;
        }

        private void Update()
        {
            if (GameManager.Instance.State != GameManager.GameState.Playing)
                return;
            
            transform.position += Vector3.back * (_speed * Time.deltaTime);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (GameManager.Instance.State != GameManager.GameState.Playing)
                return;
            
            if (_spawner == null || _player == null || other == null)
                return;

            var t = other.transform;
            if (t == _player || t.IsChildOf(_player))
                _spawner.RaiseTrafficCollision();
            
            // if (other.CompareTag("Traffic"))
            // {
            //     // Fire screen shake
            //     _impulseSource.GenerateImpulse();
            //
            //     // Fire game over
            //     _spawner.RaiseTrafficCollision();
            // }
        }
    }
}
