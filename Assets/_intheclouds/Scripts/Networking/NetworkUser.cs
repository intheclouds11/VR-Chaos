using System;
using Fusion;
using Photon.Voice.Unity;
using UnityEngine;

public class NetworkUser : NetworkBehaviour
{
    [Networked, OnChangedRender(nameof(HealthChanged))]
    public int NetworkedHealth { get; set; }

    private PlayerStats playerStats;
    public event Action<int, Vector3> onDamaged;
    public event Action onRespawned;
    private const float damageCooldown = 0.5f;
    private float lastDamageTime;
    private AudioSource audioSource;


    private void Start()
    {
        audioSource = GetComponentInChildren<AudioSource>();

        if (HasInputAuthority)
        {
            playerStats = PlayerStats.Instance;
            playerStats.SetNetworkUser(this);

            NetworkedHealth = playerStats.CurrentHealth;

            Debug.Log("HasInputAuthority, disabling Speaker ------------");
            GetComponentInChildren<Speaker>().gameObject.SetActive(false);
        }
    }

    private void Update()
    {
        if (HasInputAuthority && MyInputSystem.Instance.WasSecondaryButtonActivated(HandSide.Left))
        {
            var netEnemy = FindAnyObjectByType<NetworkEnemy>();
            if (netEnemy)
            {
                netEnemy.RPC_ToggleEnemyAI();
            }
        }
    }

    public override void Spawned()
    {
        Debug.Log("NetworkUser -------Spawned");

        if (Runner.IsSharedModeMasterClient)
        {
            EnemySpawner.Instance.SpawnEnemies();
        }
    }

    public void HealthChanged()
    {
        if (HasInputAuthority)
        {
            Debug.Log($"Networked health updated: {NetworkedHealth}");
        }
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_TakeDamage(int damage, Vector3 knockBack)
    {
        if (!CanDamage()) return;

        var newHealth = NetworkedHealth - damage;
        if (newHealth > 0)
        {
            NetworkedHealth = newHealth;
        }
        else
        {
            NetworkedHealth = 0;
            RPC_HandleDeath();
        }

        RPC_PlayDamageAudio(newHealth <= 0);

        lastDamageTime = Time.time;
        onDamaged?.Invoke(damage, knockBack);
    }

    private bool CanDamage()
    {
        if (NetworkedHealth <= 0)
        {
            // Debug.Log($"Damage ignored due to player already dead");
            return false;
        }

        if (Time.time - lastDamageTime < damageCooldown)
        {
            // Debug.Log($"Damage ignored due to cooldown");
            return false;
        }

        return true;
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_PlayDamageAudio(bool died)
    {
        if (HasInputAuthority)
        {
            // damaged user plays their sfx in PlayerStats
        }
        else
        {
            // Debug.Log($"RPC_PlayDamageAudio !HasInputAuthority");
            audioSource.PlayOneShot(died ? PlayerStats.Instance.DiedSFX : PlayerStats.Instance.HitSFX);
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_PlayDashAttackAudio()
    {
        if (HasInputAuthority)
        {
            // damaged user plays their sfx in PlayerStats
        }
        else
        {
            // Debug.Log($"Playing sfx at position {position}");
            audioSource.PlayOneShot(PlayerStats.Instance.DashSFX);
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.StateAuthority)]
    private void RPC_HandleDeath()
    {
        if (HasInputAuthority)
        {
            Invoke(nameof(Respawn), 3f); // Respawn after 3 seconds
        }
    }

    private void Respawn()
    {
        onRespawned?.Invoke();
        NetworkedHealth = playerStats.CurrentHealth;
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_HandleHighFive()
    {
        if (!playerStats.HasHighFiveBuff)
        {
            Debug.Log($"RPC_HandleHighFive");
            playerStats.SetHighFiveBuff(true);
        }
    }
}