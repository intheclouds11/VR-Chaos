using System;
using System.Collections;
using System.Collections.Generic;
using Fusion.XR.Shared.Rig;
using UnityEngine;
using UnityEngine.Serialization;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [SerializeField]
    private AudioClip levelCompletedSFX;

    [SerializeField]
    private AudioClip goalUnlockedSFX;

    public event Action onLevelCompleted;
    private bool levelComplete;
    private EnemySpawner enemySpawner;
    private List<NetworkUser> netUsers = new();
    private List<Transform> netUserHeads = new();
    private Goal goal;
    private bool goalUnlocked;
    public bool gameStarted;


    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        enemySpawner = EnemySpawner.Instance;
        goal = Goal.Instance;
    }

    public void SetNetworkUser(NetworkUser netUser)
    {
        if (!netUsers.Contains(netUser))
        {
            netUsers.Add(netUser);
            netUserHeads.Add(netUser.GetComponent<NetworkRig>().headset.transform);
        }
    }

    private void Update()
    {
        if (!gameStarted) return;

        if (!levelComplete && enemySpawner.EnemiesDefeated)
        {
            if (!goalUnlocked)
            {
                goalUnlocked = true;
                foreach (var netUser in netUsers)
                {
                    netUser.RPC_OnGoalUnlocked();
                }
            }

            if (AllUsersInGoal())
            {
                levelComplete = true;
                StartCoroutine(OnLevelCompleted());
            }
        }
    }

    private bool AllUsersInGoal()
    {
        bool allUsersInGoal = false;
        foreach (var userHead in netUserHeads)
        {
            allUsersInGoal = Vector3.Distance(goal.transform.position, userHead.position) <= 0.8f;
        }

        return allUsersInGoal;
    }

    private IEnumerator OnLevelCompleted()
    {
        // Debug.Log($"LevelComplete, netUsers.count is {netUsers.Count}");
        foreach (var netUser in netUsers)
        {
            netUser.RPC_OnLevelCompleted();
        }

        yield return new WaitForSeconds(3f);
        onLevelCompleted?.Invoke();
        // Debug.Log($"onLevelCompleted?.Invoke()");
        yield return new WaitForSeconds(3f);
        levelComplete = false;
        goalUnlocked = false;
        // Debug.Log($"levelComplete = false");
    }

    public void PlayCompletedSFX()
    {
        AudioSource.PlayClipAtPoint(levelCompletedSFX, goal.transform.position);
    }

    public void PlayGoalUnlockedSFX()
    {
        AudioSource.PlayClipAtPoint(goalUnlockedSFX, PlayerStats.Instance.HardwareRig.headset.transform.position);
    }
}