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


    private void Awake()
    {
        Instance = this;
    }

    // public void PlayerJoined(PlayerRef player)
    // {
    //     Debug.Log("EnemySpawner -------PlayerJoined");
    //     if (Runner.IsSharedModeMasterClient)
    //     {
    //         SpawnEnemies();
    //     }
    // }

    public void SpawnEnemies()
    {
        var spawnPoint = spawnPoints.First();
        // Debug.Log($"Spawning enemies! enemy: {enemyPrefab}, spawnPoint: {spawnPoint}");
        Runner.Spawn(enemyPrefab, spawnPoint.transform.position, spawnPoint.transform.rotation);
    }
}