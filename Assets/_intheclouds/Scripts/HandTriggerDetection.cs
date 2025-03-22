using System;
using Fusion;
using Fusion.XR.Shared.Rig;
using UnityEngine;

public enum HandSide
{
    Left,
    Right
}

public class HandTriggerDetection : MonoBehaviour
{
    [SerializeField]
    private HandSide handSide;
    private HardwareRig hardwareRig;
    private Transform handTransform;
    private Transform headTransform;
    private Vector3 lastHandPos;
    private Vector3 lastHeadPos;
    private AudioSource audioSource;

    /// <summary>
    /// Hand velocity relative to user's head. This is to decouple player's velocity from hand velocity, requiring user to thrust hand
    /// </summary>
    public Vector3 handRelativeVelocity { get; private set; }

    private PlayerStats playerStats;

    private void Start()
    {
        audioSource = GetComponent<AudioSource>();
        playerStats = GetComponentInParent<PlayerStats>();
        hardwareRig = GetComponentInParent<HardwareRig>();
        handTransform = handSide == HandSide.Left ? hardwareRig.leftHand.transform : hardwareRig.rightHand.transform;
        headTransform = hardwareRig.headset.transform;

        lastHandPos = handTransform.position;
        lastHeadPos = hardwareRig.headset.transform.position;
    }

    private void Update()
    {
        HandVelocityCheck();
    }

    private void HandVelocityCheck()
    {
        Vector3 handVelocity = (handTransform.position - lastHandPos) / Time.deltaTime;
        Vector3 headVelocity = (headTransform.position - lastHeadPos) / Time.deltaTime;

        if (!playerStats.desktopMode)
        {
            handRelativeVelocity = handVelocity - headVelocity;
        }
        else
        {
            handRelativeVelocity = handVelocity;
        }

        lastHandPos = handTransform.position;
        lastHeadPos = headTransform.position;

        if (handRelativeVelocity.magnitude > 3)
        {
            // Debug.Log($"{handSide} Relative Hand Velocity Magnitude: {handRelativeVelocity.magnitude}");
            // Debug.Log($"Head Velocity and Magnitude: {headVelocity}  {headVelocity.magnitude}");
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        var netObj = other.GetComponentInParent<NetworkObject>();
        if (!netObj)
        {
            // Debug.Log("fuark");
            return;
        }

        if (netObj.HasInputAuthority)
        {
            // Debug.LogWarning($"OnTriggerEnter: {other.transform.name} owned by player! ", other.transform);
        }
        else
        {
            // Debug.Log($"{handSide} Hand hit {other.transform.name} with velocity {handRelativeVelocity.magnitude} relative to head");

            if (other.gameObject.layer == LayerMask.NameToLayer("Damageable"))
            {
                HandleDamage(other);
            }
            else if (other.gameObject.layer == LayerMask.NameToLayer("Hand"))
            {
                // TODO high five buff (networked too!)
            }
        }
    }

    private void HandleDamage(Collider other)
    {
        var isDashAttacking = !playerStats.desktopMode && GorillaMovement.Instance.isDashAttacking;
        if (!isDashAttacking && handRelativeVelocity.magnitude <= 3)
        {
            // Debug.Log("-------Hand too slow to damage--------");
            return;
        }

        var damage = isDashAttacking ? playerStats.GetDashDamageValue : playerStats.GetLightDamageValue;
        damage = playerStats.highFiveBuff ? damage + 1 : damage;
            
        var knockBackDirection = handRelativeVelocity.normalized;
        float knockbackAmount;
        if (!playerStats.desktopMode && isDashAttacking)
        {
            knockbackAmount = 10f;
        }
        else
        {
            knockbackAmount = 5f;
        }

        var netUser = other.GetComponentInParent<NetworkUser>();
        if (netUser && netUser.NetworkedHealth > 0)
        {
            netUser.RPC_DealDamage(damage, knockbackAmount, knockBackDirection);
            HitFeedback(isDashAttacking);
        }
    }

    private void HitFeedback(bool isDashAttacking)
    {
        MyInputSystem.Instance.Vibrate(handSide, 2f, 0.05f, 10f);
        audioSource.PlayOneShot(isDashAttacking ? playerStats.DashDamageSFX : playerStats.LightDamageSFX);
    }

    private void HandleHighFiveBuff()
    {
        
    }
}