using System;
using System.Collections.Generic;
using UnityEngine;

public class TrafficSpawner : MonoBehaviour
{
    public event Action OnTrafficCollision;

    [Header("Traffic Prefabs")]
    [Tooltip("Add all traffic car prefabs here.")]
    [SerializeField] private List<GameObject> trafficPrefabs = new List<GameObject>();

    [Header("Pooling Settings")]
    [Tooltip("Pool size per traffic prefab type.")]
    [SerializeField] private int poolSizePerPrefab = 10;

    [Header("Spawn Settings")]
    [SerializeField] private float spawnInterval = 3f;
    [SerializeField] private Transform player;

    private const float TrafficSpeed = 8f;
    private const float SpawnAheadDistance = 80f;
    private const float DespawnBehindPlayerDistance = 30f;

    private const float IntervalDecreaseEverySeconds = 10f;
    private const float IntervalDecreaseAmount = 0.3f;
    private const float MinSpawnInterval = 0.8f;

    private static readonly float[] LaneXs = { -3.5f, 0f, 3.5f };

    // Separate pool for each prefab
    private readonly Dictionary<int, Queue<TrafficCarPooled>> _pools =
        new Dictionary<int, Queue<TrafficCarPooled>>(8);

    private readonly List<TrafficCarPooled> _active =
        new List<TrafficCarPooled>(32);

    private readonly List<int> _validPrefabIndexes =
        new List<int>(8);

    private float _spawnTimer;
    private float _elapsed;

    private void Start()
    {
        if (player == null || trafficPrefabs == null || trafficPrefabs.Count == 0)
        {
            Debug.LogError("TrafficSpawner: Missing player or traffic prefabs.");
            enabled = false;
            return;
        }

        if (poolSizePerPrefab <= 0)
            poolSizePerPrefab = 1;

        if (spawnInterval <= 0f)
            spawnInterval = 0.1f;

        InitializePools();
    }

    private void InitializePools()
    {
        _validPrefabIndexes.Clear();

        for (int prefabIndex = 0; prefabIndex < trafficPrefabs.Count; prefabIndex++)
        {
            var prefab = trafficPrefabs[prefabIndex];

            if (prefab == null)
            {
                Debug.LogWarning($"TrafficSpawner: Null prefab at index {prefabIndex}");
                continue;
            }

            var pool = new Queue<TrafficCarPooled>(poolSizePerPrefab);

            for (int i = 0; i < poolSizePerPrefab; i++)
            {
                var go = Instantiate(prefab, transform);
                go.SetActive(false);

                var pooled = go.GetComponent<TrafficCarPooled>();

                if (pooled == null)
                    pooled = go.AddComponent<TrafficCarPooled>();

                pooled.Init(this, player, TrafficSpeed, prefabIndex);

                pool.Enqueue(pooled);
            }

            _pools.Add(prefabIndex, pool);
            _validPrefabIndexes.Add(prefabIndex);
        }

        if (_validPrefabIndexes.Count == 0)
        {
            Debug.LogError("TrafficSpawner: No valid traffic prefabs found.");
            enabled = false;
        }
    }

    private void Update()
    {
        if (player == null || GameManager.Instance.State != GameManager.GameState.Playing)
            return;

        _elapsed += Time.deltaTime;
        _spawnTimer += Time.deltaTime;

        float currentInterval = GetCurrentSpawnInterval();

        if (_spawnTimer >= currentInterval)
        {
            // Prevent burst spawning after frame hitch
            _spawnTimer = Mathf.Min(_spawnTimer - currentInterval, currentInterval);

            TrySpawn();
        }

        DespawnBehindPlayer();
    }

    private float GetCurrentSpawnInterval()
    {
        int steps = Mathf.FloorToInt(_elapsed / IntervalDecreaseEverySeconds);

        float interval = spawnInterval - (steps * IntervalDecreaseAmount);

        return Mathf.Max(MinSpawnInterval, interval);
    }

    private void TrySpawn()
    {
        if (_validPrefabIndexes.Count == 0)
            return;

        // Random prefab selection
        int randomPrefabListIndex =
            UnityEngine.Random.Range(0, _validPrefabIndexes.Count);

        int prefabIndex = _validPrefabIndexes[randomPrefabListIndex];

        if (!_pools.TryGetValue(prefabIndex, out Queue<TrafficCarPooled> pool))
            return;

        if (pool.Count == 0)
            return;

        TrafficCarPooled car = pool.Dequeue();

        Vector3 playerPos = player.position;

        float laneX = LaneXs[UnityEngine.Random.Range(0, LaneXs.Length)];

        Vector3 spawnPos = new Vector3(
            laneX,
            car.transform.position.y,
            playerPos.z + SpawnAheadDistance
        );

        car.transform.position = spawnPos;
        car.gameObject.SetActive(true);

        _active.Add(car);
    }

    private void DespawnBehindPlayer()
    {
        float despawnZ = player.position.z - DespawnBehindPlayerDistance;

        for (int i = _active.Count - 1; i >= 0; i--)
        {
            TrafficCarPooled car = _active[i];

            if (car == null)
            {
                _active.RemoveAt(i);
                continue;
            }

            if (car.transform.position.z >= despawnZ)
                continue;

            ReturnToPool(car);

            _active.RemoveAt(i);
        }
    }

    private void ReturnToPool(TrafficCarPooled car)
    {
        car.gameObject.SetActive(false);

        if (_pools.TryGetValue(car.PrefabPoolIndex, out Queue<TrafficCarPooled> pool))
        {
            pool.Enqueue(car);
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

        public int PrefabPoolIndex { get; private set; }

        public void Init(
            TrafficSpawner spawner,
            Transform player,
            float speed,
            int prefabPoolIndex)
        {
            _spawner = spawner;
            _player = player;
            _speed = speed;
            PrefabPoolIndex = prefabPoolIndex;
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

            Transform t = other.transform;

            if (t == _player || t.IsChildOf(_player))
            {
                _spawner.RaiseTrafficCollision();
            }
        }
    }
}
