using System;
using System.Collections.Generic;
using System.Linq;
using Fusion;
using UnityEngine;

public class EnemySpawner : NetworkBehaviour
{
    public static EnemySpawner Instance;

    [SerializeField]
    private NetworkPrefabRef enemyPrefab;

    [SerializeField]
    private List<SpawnPoint> spawnPoints;

    private bool spawned;


    private void Awake()
    {
        Instance = this;
    }

    public void SpawnEnemies()
    {
        if (spawned) return;

        spawned = true;
        var spawnPoint = spawnPoints.First();
        // Debug.Log($"Spawning enemies! enemy: {enemyPrefab}, spawnPoint: {spawnPoint}");
        Runner.Spawn(enemyPrefab, spawnPoint.transform.position, spawnPoint.transform.rotation);
    }
}