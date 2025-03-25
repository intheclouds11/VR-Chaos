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

    public List<NetworkEnemy> SpawnedEnemies { get; } = new();
    public bool EnemiesDefeated { get; private set; }
    private bool spawned;


    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        GameManager.Instance.onLevelCompleted += Reset;
    }

    public void Reset()
    {
        EnemiesDefeated = false;
    }

    private void Update()
    {
        foreach (var spawnedEnemy in SpawnedEnemies)
        {
            if (spawnedEnemy.NetworkedHealth > 0)
            {
                EnemiesDefeated = false;
                return;
            }

            EnemiesDefeated = true;
        }
    }

    public void SpawnEnemies()
    {
        if (spawned) return;

        spawned = true;
        foreach (var spawnPoint in spawnPoints)
        {
            Debug.Log($"Spawning enemies! enemy: {enemyPrefab}, spawnPoint: {spawnPoint}", spawnPoint);
            var obj = Runner.Spawn(enemyPrefab, spawnPoint.transform.position, spawnPoint.transform.rotation);
            if (obj.TryGetComponent(out NetworkEnemy netEnemy))
            {
                SpawnedEnemies.Add(netEnemy);
            }
            else
            {
                Debug.LogError($"Failed to add to spawnedEnemies list!");
            }
        }

        Invoke(nameof(StartGame), 3f);
    }

    private void StartGame()
    {
        GameManager.Instance.gameStarted = true;
    }
}