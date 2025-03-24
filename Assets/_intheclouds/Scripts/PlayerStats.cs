using System;
using Fusion.XR.Shared.Rig;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;

public class PlayerStats : MonoBehaviour
{
    public static PlayerStats Instance;

    [SerializeField]
    private int startingHealth = 3;

    private int currentHealth;
    public int CurrentHealth
    {
        get => currentHealth;
        private set
        {
            currentHealth = value;
            if (!DesktopMode)
            {
                healthIndicatorVR.text = $"Health: {currentHealth}/{startingHealth}";
            }
            else
            {
                healthIndicatorDesktop.text = $"Health: {currentHealth}/{startingHealth}";
            }
        }
    }

    [SerializeField]
    private int lightDamage = 1;
    public int GetLightDamageValue => lightDamage;

    [SerializeField]
    private int dashDamage = 2;
    public int GetDashDamageValue => dashDamage;


    [FormerlySerializedAs("healthIndicator")]
    [SerializeField]
    private TextMeshProUGUI healthIndicatorVR;

    [SerializeField]
    private TextMeshProUGUI healthIndicatorDesktop;

    [FormerlySerializedAs("hitSFX")]
    [SerializeField]
    private AudioClip hurtSFX;
    public AudioClip HitSFX => hurtSFX;

    [SerializeField]
    private AudioClip diedSFX;
    public AudioClip DiedSFX => diedSFX;

    [SerializeField]
    private AudioClip dashSFX;
    public AudioClip DashSFX => dashSFX;

    [SerializeField]
    private AudioClip lightDamageSFX;
    public AudioClip LightDamageSFX => lightDamageSFX;

    [SerializeField]
    private AudioClip dashDamageSFX;
    public AudioClip DashDamageSFX => dashDamageSFX;

    [SerializeField]
    private AudioClip highFiveSFX;
    
    public HardwareRig HardwareRig { get; private set; }
    public NetworkUser NetworkUser { get; private set; }
    public bool DesktopMode { get; private set; }
    public bool HasHighFiveBuff { get; private set; }
    public float AttackCooldown { get; private set; } = 0.5f;
    public float LastAttackTime { get; set; }
    

    public void SetHighFiveBuff(bool toggle)
    {
        HasHighFiveBuff = toggle;
        if (toggle)
        {
            AudioSource.PlayClipAtPoint(highFiveSFX, HardwareRig.headset.transform.position);
        }
    }

    private AudioSource audioSource;


    private void Awake()
    {
        Instance = this;
        CurrentHealth = startingHealth;
    }

    private void Start()
    {
        audioSource = GetComponentInChildren<AudioSource>();
        HardwareRig = GetComponentInChildren<HardwareRig>();
        DesktopMode = DesktopMovement.Instance;
    }

    private void ResetStats()
    {
        CurrentHealth = startingHealth;
        HasHighFiveBuff = false;
    }

    public void SetNetworkUser(NetworkUser user)
    {
        NetworkUser = user;
        NetworkUser.onDamaged += TakeDamage;
        NetworkUser.onRespawned += OnRespawned;

        if (!DesktopMode)
        {
            GorillaMovement.Instance.SetNetworkUser(user);
        }
        else
        {
            DesktopMovement.Instance.SetNetworkUser(user);
        }
    }

    private void TakeDamage(int damage, Vector3 knockBackDirection)
    {
        var newHealth = CurrentHealth - damage;

        if (newHealth > 0)
        {
            // Debug.Log("Player damaged!");
            CurrentHealth = newHealth;
            audioSource.PlayOneShot(hurtSFX);
        }
        else
        {
            // Debug.Log("Player died!");
            CurrentHealth = 0;
            audioSource.PlayOneShot(diedSFX);
        }

        healthIndicatorVR.text = $"Health: {newHealth}/{startingHealth}";
    }

    private void OnRespawned()
    {
        ResetStats();
    }
}