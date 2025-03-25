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
    private bool aiActive = true;
    private bool respawning;
    private Color startingColor;
    private MeshRenderer mr;


    private void Start()
    {
        AudioSource = GetComponentInChildren<AudioSource>();
        spawnedLocation = transform.position;
        mr = GetComponentInChildren<MeshRenderer>();
        startingColor = mr.material.color;
        
        if (HasStateAuthority)
        {
            NetworkedHealth = startingHealth;
            GameManager.Instance.onLevelCompleted += Respawn;
        }
    }

    public void HealthChanged()
    {
        // Debug.Log($"Enemy Networked health updated: {NetworkedHealth}");
        if (NetworkedHealth == startingHealth)
        {
            mr.material.color = startingColor;
        }
        else if (NetworkedHealth == startingHealth - 1)
        {
            mr.material.color = Color.yellow;
        }
        else if (NetworkedHealth == startingHealth - 2)
        {
            mr.material.color = Color.red;
        }
        else
        {
            mr.material.color = Color.black;
        }
        
        mr.material.color = new Color(mr.material.color.r, mr.material.color.g, mr.material.color.b, startingColor.a);
    }

    public override void FixedUpdateNetwork()
    {
        if (respawning)
        {
            respawning = false;
            transform.position = spawnedLocation;
        }

        CheckDisengageTargets();

        if (aiActive)
        {
            if (NetworkedHealth > 0 && targets.Any())
            {
                var newSpeed = (startingHealth - NetworkedHealth) * 0.5f + baseSpeed;
                var target = targets.Last();
                transform.position = Vector3.MoveTowards(transform.position, target.position, newSpeed * Runner.DeltaTime);
                transform.LookAt(target);
                transform.eulerAngles = new Vector3(0f, transform.eulerAngles.y, 0f);
            }
            else if (NetworkedHealth <= 0)
            {
                transform.Rotate(Vector3.up, 180f * Runner.DeltaTime);
            }
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
                // Debug.Log("Disengaged target");
                targets.Remove(target);
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        var netUser = other.GetComponentInParent<NetworkUser>();
        if (netUser && other.gameObject.layer == LayerMask.NameToLayer("Damageable"))
        {
            if (Vector3.Distance(transform.position, other.transform.position) < 2f)
            {
                // If recently damaged, don't hurt player
                if (NetworkedHealth > 0 && Time.time - lastDamageTime > damageCooldown + 1f)
                {
                    var knockBackDirection = (transform.position - lastPosition).normalized;
                    knockBackDirection -= Vector3.up * 0.5f;
                    netUser.RPC_TakeDamage(baseDamage, -knockBackDirection * 9f);
                }
            }
            else if (!targets.Contains(other.transform))
            {
                targets.Add(other.transform);
            }
        }
    }

    [Rpc(RpcSources.All, RpcTargets.All)]
    public void RPC_TakeDamageEnemy(int damage, Vector3 knockBack)
    {
        var newHealth = NetworkedHealth - damage;

        if (HasStateAuthority)
        {
            if (!CanDamage()) return;
            if (newHealth > 0)
            {
                NetworkedHealth = newHealth;
            }
            else
            {
                NetworkedHealth = 0;
                // Invoke(nameof(Respawn), 3f); // Respawn after 3 seconds
            }

            lastDamageTime = Time.time;
        }

        AudioSource.PlayOneShot(newHealth <= 0 ? DiedSFX : HitSFX);
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

    private void Respawn()
    {
        respawning = true;
        NetworkedHealth = startingHealth;
    }

    [Rpc(RpcSources.All, RpcTargets.All)]
    public void RPC_ToggleEnemyAI()
    {
        // Debug.Log($"EnemyAI toggled to: {aiActive}");
        aiActive = !aiActive;
    }
}