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
            if (!desktopMode)
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


    public NetworkUser networkUser { get; private set; }
    public Transform head { get; private set; }
    public bool desktopMode { get; private set; }
    public bool highFiveBuff { get; private set; }
    
    private AudioSource audioSource;


    private void Awake()
    {
        Instance = this;
        CurrentHealth = startingHealth;
    }

    private void Start()
    {
        audioSource = GetComponentInChildren<AudioSource>();
        head = Camera.main?.transform;
        desktopMode = DesktopMovement.Instance;
    }

    private void ResetStats()
    {
        CurrentHealth = startingHealth;
        highFiveBuff = false;
    }

    public void SetNetworkUser(NetworkUser user)
    {
        networkUser = user;
        networkUser.onDamaged += TakeDamage;
        networkUser.onRespawned += OnRespawned;

        if (!desktopMode)
        {
            GorillaMovement.Instance.SetNetworkUser(user);
        }
        else
        {
            DesktopMovement.Instance.SetNetworkUser(user);
        }
    }

    private void TakeDamage(int damage, float knockBackAmount, Vector3 knockBackDirection)
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