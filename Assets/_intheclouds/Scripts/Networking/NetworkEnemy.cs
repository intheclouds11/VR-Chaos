using System;
using System.Collections.Generic;
using System.Linq;
using Fusion;
using UnityEngine;
using UnityEngine.Serialization;

public class NetworkEnemy : NetworkBehaviour
{
    [SerializeField]
    private float baseSpeed = 1f;

    [SerializeField]
    private float disengageDistance = 15f;

    [SerializeField]
    private int startingHealth = 3;

    [SerializeField]
    private int baseDamage = 1;

    [SerializeField]
    private AudioClip hurtSFX;
    public AudioClip HitSFX => hurtSFX;

    [SerializeField]
    private AudioClip diedSFX;
    public AudioClip DiedSFX => diedSFX;

    [Networked, OnChangedRender(nameof(HealthChanged))]
    public int NetworkedHealth { get; set; }

    private const float damageCooldown = 0.5f;
    private float lastDamageTime;

    public AudioSource AudioSource { get; private set; }
    private List<Transform> targets = new();
    private Transform currentTarget;
    private Vector3 lastPosition;
    private Vector3 spawnedLocation;


    private void Start()
    {
        AudioSource = GetComponentInChildren<AudioSource>();
        spawnedLocation = transform.position;

        if (HasStateAuthority)
        {
            NetworkedHealth = startingHealth;
        }
    }

    public void HealthChanged()
    {
        // Debug.Log($"Enemy Networked health updated: {NetworkedHealth}");
    }

    public override void FixedUpdateNetwork()
    {
        CheckDisengageTargets();

        if (NetworkedHealth > 0 && targets.Any())
        {
            var newSpeed = (startingHealth - NetworkedHealth) * 0.5f + baseSpeed;
            transform.position = Vector3.MoveTowards(transform.position, targets.Last().position, newSpeed * Runner.DeltaTime);
        }
        else if (NetworkedHealth <= 0)
        {
            transform.Rotate(Vector3.up, 180f * Runner.DeltaTime);
        }

        lastPosition = transform.position;
    }

    private void CheckDisengageTargets()
    {
        if (!targets.Any()) return;

        for (int i = targets.Count - 1; i >= 0; i--)
        {
            var target = targets[i];
            var distanceFromPlayer = Vector3.Distance(transform.position, target.position);

            if (distanceFromPlayer >= disengageDistance)
            {
                Debug.Log("Disengaged target");
                targets.Remove(target);
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        var netUser = other.GetComponentInParent<NetworkUser>();
        if (netUser)
        {
            if (other.gameObject.layer == LayerMask.NameToLayer("Damageable") &&
                Vector3.Distance(transform.position, other.transform.position) < 2f)
            {
                // If recently damaged, don't hurt player
                if (NetworkedHealth > 0 && Time.time - lastDamageTime > damageCooldown)
                {
                    var knockBackDirection = (transform.position - lastPosition).normalized;
                    netUser.RPC_DealDamage(baseDamage, -knockBackDirection * 5);
                }
            }
            else if (!targets.Contains(other.transform))
            {
                targets.Add(other.transform);
            }
        }
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_DealDamageEnemy(int damage, Vector3 knockBack)
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
            RPC_HandleEnemyDeath();
        }

        RPC_PlayDamageAudio(newHealth <= 0);
        lastDamageTime = Time.time;
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
            AudioSource.PlayOneShot(died ? DiedSFX : HitSFX);
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.StateAuthority)]
    private void RPC_HandleEnemyDeath()
    {
        Invoke(nameof(Respawn), 3f); // Respawn after 3 seconds
    }

    private void Respawn()
    {
        NetworkedHealth = startingHealth;
        transform.position = spawnedLocation;
    }
}