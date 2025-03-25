using System;
using System.Collections.Generic;
using System.Linq;
using Fusion;
using Fusion.XR.Shared.Rig;
using UnityEngine;
using Random = UnityEngine.Random;

public enum HandSide
{
    Left,
    Right
}

public class HandTriggerDetection : MonoBehaviour
{
    public event Action onStartClimbing;
    public event Action<Vector3, bool> onStopClimbing;

    [SerializeField]
    private HandSide handSide;

    [SerializeField]
    private Transform playerTransform;

    private HardwareRig hardwareRig;
    private Transform handTransform;
    private Transform headTransform;
    private Vector3 lastHandPos;
    private Vector3 lastHeadPos;
    public AudioSource AttackAudioSource { get; private set; }
    public AudioSource ClimbAudioSource;
    private List<Climbable> climbablesInRange = new();
    public bool IsClimbing { get; private set; }
    private bool climbingCanceled;
    private bool startedClimbing;
    private bool wasClimbing;
    private Vector3 climbAnchor;
    private Vector3 handDelta;
    private HandTriggerDetection otherHandTriggerDetection;
    private PlayerStats playerStats;

    /// <summary>
    /// Hand velocity relative to user's head. This is to decouple player's velocity from hand velocity, requiring user to thrust hand
    /// </summary>
    public Vector3 handRelativeVelocity { get; private set; }

    private Vector3 headVelocity;


    private void Start()
    {
        AttackAudioSource = GetComponent<AudioSource>();
        playerStats = GetComponentInParent<PlayerStats>();
        hardwareRig = GetComponentInParent<HardwareRig>();
        if (!playerStats.DesktopMode)
        {
            otherHandTriggerDetection = handSide == HandSide.Left
                ? GorillaMovement.Instance.rightHandTriggerDetection
                : GorillaMovement.Instance.leftHandTriggerDetection;
        }

        handTransform = handSide == HandSide.Left ? hardwareRig.leftHand.transform : hardwareRig.rightHand.transform;
        headTransform = hardwareRig.headset.transform;

        lastHandPos = handTransform.position;
        lastHeadPos = hardwareRig.headset.transform.position;
    }


    private void Update()
    {
        CheckHandVelocity();
        if (!playerStats.DesktopMode && GorillaMovement.Instance.AllowClimbing)
        {
            CheckClimbing();
        }

        lastHandPos = handTransform.position;
        lastHeadPos = headTransform.position;
    }

    private void CheckHandVelocity()
    {
        Vector3 handVelocity = (handTransform.position - lastHandPos) / Time.deltaTime;
        headVelocity = (headTransform.position - lastHeadPos) / Time.deltaTime;

        if (!playerStats.DesktopMode)
        {
            handRelativeVelocity = handVelocity - headVelocity;
        }
        else
        {
            handRelativeVelocity = handVelocity;
        }

        if (handRelativeVelocity.magnitude > 3)
        {
            // Debug.Log($"{handSide} Relative Hand Velocity Magnitude: {handRelativeVelocity.magnitude}");
            // Debug.Log($"Head Velocity and Magnitude: {headVelocity}  {headVelocity.magnitude}");
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (playerStats.CurrentHealth < 1) return;

        var netUser = other.GetComponentInParent<NetworkUser>();
        var netEnemy = other.GetComponentInParent<NetworkEnemy>();
        var isDashAttacking = !playerStats.DesktopMode && GorillaMovement.Instance.isDashAttacking;
        var isGroundPounding = !playerStats.DesktopMode && GorillaMovement.Instance.isGroundPounding;

        if (other.gameObject.layer == LayerMask.NameToLayer("Damageable") &&
            Time.time - playerStats.LastAttackTime > playerStats.AttackCooldown)
        {
            if (!isGroundPounding && !isDashAttacking && handRelativeVelocity.magnitude <= 3.5f)
            {
                // Debug.Log("-------Hand too slow to damage--------");
                return;
            }

            var damage = isDashAttacking ? playerStats.GetDashDamageValue : playerStats.GetLightDamageValue;
            damage = playerStats.HasHighFiveBuff ? damage + 1 : damage;

            var knockBackDirection = handRelativeVelocity.normalized;
            knockBackDirection -= Vector3.up * 0.1f;

            float knockbackAmount;
            if (isGroundPounding || isDashAttacking)
            {
                knockbackAmount = 10f;
            }
            else
            {
                knockbackAmount = 5f;
            }

            if (netUser && !netUser.HasInputAuthority && netUser.NetworkedHealth > 0)
            {
                netUser.RPC_TakeDamage(damage, knockBackDirection * knockbackAmount);
                AfterAttack(isDashAttacking);
            }
            else if (netEnemy && netEnemy.NetworkedHealth > 0)
            {
                netEnemy.RPC_TakeDamageEnemy(damage, knockBackDirection * knockbackAmount);
                AfterAttack(isDashAttacking);
            }
        }
        else if (!isDashAttacking && !isGroundPounding && other.gameObject.layer == LayerMask.NameToLayer("Hand") && netUser &&
                 !netUser.HasInputAuthority)
        {
            HandleHighFiveBuff(netUser);
        }
        else if (!isDashAttacking && !isGroundPounding && other.TryGetComponent(out Climbable climbable) &&
                 !climbablesInRange.Contains(climbable))
        {
            // Debug.Log("climbablesInRange.Add(climbable)");
            climbablesInRange.Add(climbable);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.TryGetComponent(out Climbable climbable) && climbablesInRange.Contains(climbable))
        {
            climbablesInRange.Remove(climbable);
            // Debug.Log("climbablesInRange.Remove(climbable)");
        }
    }

    private void AfterAttack(bool isDashAttacking)
    {
        playerStats.LastAttackTime = Time.time;
        MyInputSystem.Instance.Vibrate(handSide, 2f, 0.05f, 10f);
        AttackAudioSource.PlayOneShot(isDashAttacking ? playerStats.DashDamageSFX : playerStats.LightDamageSFX);
    }

    private void HandleHighFiveBuff(NetworkUser netUser)
    {
        if (!playerStats.HasHighFiveBuff)
        {
            playerStats.SetHighFiveBuff(true);
        }

        netUser.RPC_HandleHighFive();
    }

    private void CheckClimbing()
    {
        if (playerStats.DesktopMode) return;

        var gripActivated = handSide == HandSide.Left
            ? MyInputSystem.Instance.WasGripActivated(HandSide.Left)
            : MyInputSystem.Instance.WasGripActivated(HandSide.Right);

        var gripDeactivated = handSide == HandSide.Left
            ? MyInputSystem.Instance.WasGripDeactivated(HandSide.Left)
            : MyInputSystem.Instance.WasGripDeactivated(HandSide.Right);

        if (!IsClimbing)
        {
            if (climbablesInRange.Count >= 1 && gripActivated)
            {
                // Debug.Log($"climbablesInRange.Count: {climbablesInRange.Count}");
                MyInputSystem.Instance.Vibrate(handSide, 0.2f, 0.1f, 100f);
                ClimbAudioSource.pitch = Random.Range(0.5f, 0.85f);
                ClimbAudioSource.PlayOneShot(playerStats.ClimbSFX, 0.3f);
                IsClimbing = true;
                wasClimbing = true;
            }
        }
        else if (gripDeactivated)
        {
            IsClimbing = false;
        }

        handDelta = handTransform.position - lastHandPos;

        if (IsClimbing)
        {
            if (!startedClimbing)
            {
                // Debug.Log($"{handSide} Hand Started Climbing");
                startedClimbing = true;

                if (!GorillaMovement.Instance.IsClimbing)
                {
                    onStartClimbing?.Invoke();
                }
                else
                {
                    otherHandTriggerDetection.StopClimbing();
                }
            }

            playerTransform.position -= handDelta;
        }
        else if (wasClimbing)
        {
            // Debug.Log($"{handSide} Hand Stopped Climbing");
            wasClimbing = false;
            startedClimbing = false;

            if (!otherHandTriggerDetection.IsClimbing)
            {
                onStopClimbing?.Invoke(handDelta, climbingCanceled);
            }

            climbingCanceled = false;
        }
    }

    public void StopClimbing()
    {
        IsClimbing = false;
    }

    public void CancelClimbing()
    {
        if (IsClimbing)
        {
            climbingCanceled = true;
        }

        StopClimbing();
    }
}