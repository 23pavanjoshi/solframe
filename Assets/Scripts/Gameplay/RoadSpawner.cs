using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RoadSpawner : MonoBehaviour
{
    [SerializeField] private GameObject tilePrefab;
    [SerializeField] private int poolSize = 10;
    [SerializeField] private float tileLength = 20f;
    [SerializeField] private Transform player;

    private const float SpawnThreshold = 60f;
    private const float RecycleBehindCameraDistance = 40f;

    private readonly Queue<Transform> _tiles = new Queue<Transform>(16);
    private Transform _mainCameraTransform;
    private float _nextSpawnZ;
    private float _lastTileZ;
    private bool _recycledThisFrame;

    private void Awake()
    {
        if (Camera.main != null)
            _mainCameraTransform = Camera.main.transform;
        else
            StartCoroutine(AssignMainCameraNextFrame());
    }

    private IEnumerator AssignMainCameraNextFrame()
    {
        yield return null;
        if (Camera.main != null)
            _mainCameraTransform = Camera.main.transform;
    }

    private void Start()
    {
        if (tilePrefab == null)
        {
            enabled = false;
            return;
        }

        if (poolSize <= 0)
            poolSize = 1;

        if (tileLength <= 0f)
            tileLength = 20f;

        var basePos = transform.position;
        _nextSpawnZ = basePos.z;
        _lastTileZ = basePos.z;

        for (var i = 0; i < poolSize; i++)
        {
            var go = Instantiate(tilePrefab, transform);
            var t = go.transform;

            var p = basePos;
            p.z = _nextSpawnZ;
            t.position = p;

            _tiles.Enqueue(t);

            _lastTileZ = _nextSpawnZ;
            _nextSpawnZ += tileLength;
        }
    }

    private void Update()
    {
        _recycledThisFrame = false;

        if (_tiles.Count == 0)
            return;

        float playerZ = 0f;
        if (player != null)
            playerZ = player.position.z;

        // Keep road filled ahead of the player.
        if (!_recycledThisFrame && player != null && (playerZ + SpawnThreshold) >= _lastTileZ)
        {
            RecycleOldestToFront();
            _recycledThisFrame = true;
        }

        // Recycle any tiles that are safely behind the camera.
        if (!_recycledThisFrame && _mainCameraTransform != null)
        {
            var recycleZ = _mainCameraTransform.position.z - RecycleBehindCameraDistance;
            while (_tiles.Count > 0 && _tiles.Peek().position.z <= recycleZ)
                RecycleOldestToFront();
        }
    }

    private void RecycleOldestToFront()
    {
        if (_tiles.Count == 0)
            return;

        var tile = _tiles.Dequeue();

        var p = tile.position;
        p.z = _nextSpawnZ;
        tile.position = p;

        _tiles.Enqueue(tile);

        _lastTileZ = _nextSpawnZ;
        _nextSpawnZ += tileLength;
    }
}
