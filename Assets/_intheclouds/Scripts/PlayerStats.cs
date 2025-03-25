using System;
using Fusion.XR.Shared.Rig;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
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
    private AudioClip groundPoundSFX;
    public AudioClip GroundPoundSFX => groundPoundSFX;

    [SerializeField]
    private AudioClip lightDamageSFX;
    public AudioClip LightDamageSFX => lightDamageSFX;

    [SerializeField]
    private AudioClip dashDamageSFX;
    public AudioClip DashDamageSFX => dashDamageSFX;
    
    [SerializeField]
    private AudioClip climbSFX;
    public AudioClip ClimbSFX => climbSFX;

    [SerializeField]
    private AudioClip highFiveSFX;

    public HardwareRig HardwareRig { get; private set; }
    public NetworkUser NetworkUser { get; private set; }
    public bool DesktopMode { get; private set; }
    public bool HasHighFiveBuff { get; private set; }
    public float AttackCooldown { get; private set; } = 0.5f;
    public float LastAttackTime { get; set; }
    private Volume globalVolume;
    private float startingSaturation;
    private float startingPostExposure;


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
        globalVolume = FindAnyObjectByType<Volume>();
        globalVolume.profile.TryGet(out ColorAdjustments colorAdjustments);
        startingSaturation = colorAdjustments.saturation.value;
        startingPostExposure = colorAdjustments.postExposure.value;
    }

    private void ResetStats()
    {
        CurrentHealth = startingHealth;
        globalVolume.profile.TryGet(out ColorAdjustments colorAdjustments);
        colorAdjustments.saturation.value = startingSaturation;
        colorAdjustments.postExposure.value = startingSaturation;
        HasHighFiveBuff = false;
    }

    public void SetNetworkUser(NetworkUser netUser)
    {
        NetworkUser = netUser;
        NetworkUser.onDamaged += TakeDamage;
        NetworkUser.onRespawned += OnRespawned;

        if (!DesktopMode)
        {
            GorillaMovement.Instance.SetNetworkUser(netUser);
        }
        else
        {
            DesktopMovement.Instance.SetNetworkUser(netUser);
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

        globalVolume.profile.TryGet(out ColorAdjustments colorAdjustments);
        colorAdjustments.saturation.value -= 33f;
        colorAdjustments.postExposure.value -= 0.5f;
    }

    private void OnRespawned()
    {
        ResetStats();
    }
}