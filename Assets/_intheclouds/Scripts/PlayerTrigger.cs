using System;
using Fusion;
using Fusion.XR.Shared.Rig;
using UnityEngine;

public class PlayerTrigger : MonoBehaviour
{
    // private NetworkRig networkRig;
    private HardwareRig hardwareRig;
    private Transform leftHandTransform;
    private Transform rightHandTransform;
    private Transform headTransform;
    
    private Vector3 lastLeftHandPos;
    private Vector3 lastRightHandPos;
    private Vector3 lastHeadPos;
    private float lastTime;

    private void Start()
    {
        hardwareRig = GetComponentInParent<HardwareRig>();
        leftHandTransform = hardwareRig.leftHand.transform;
        rightHandTransform = hardwareRig.rightHand.transform;
        headTransform = hardwareRig.headset.transform;
        
        lastLeftHandPos = leftHandTransform.position;
        lastLeftHandPos = rightHandTransform.position;
        lastHeadPos = hardwareRig.headset.transform.position;
        lastTime = Time.time;
    }

    private void Update()
    {
        float deltaTime = Time.time - lastTime;
        if (deltaTime <= 0) return;

        Vector3 leftHandVelocity = (leftHandTransform.position - lastLeftHandPos) / deltaTime;
        Vector3 rightHandVelocity = (rightHandTransform.position - lastRightHandPos) / deltaTime;
        Vector3 headVelocity = (headTransform.position - lastHeadPos) / deltaTime;

        Vector3 leftHandRelativeVelocity = leftHandVelocity - headVelocity;
        Vector3 rightHandRelativeVelocity = rightHandVelocity - headVelocity;

        // Update last positions & time
        lastLeftHandPos = leftHandTransform.position;
        lastRightHandPos = rightHandTransform.position;
        lastHeadPos = headTransform.position;
        lastTime = Time.time;

        if (leftHandRelativeVelocity.magnitude > 1)
        {
            Debug.Log("Relative LEFT Hand Velocity: " + leftHandRelativeVelocity);
        }
        if (rightHandRelativeVelocity.magnitude > 1)
        {
            Debug.Log("Relative RIGHT Hand Velocity: " + rightHandRelativeVelocity);
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
            Debug.LogWarning($"OnTriggerEnter: {other.transform.name} owned by player! ", other.transform);
            
        }
        else
        {
            Debug.Log($"APPLY DAMAGE HERE. PlayerTrigger OnTriggerEnter: {other.transform.name} NOT owned by player", other.transform);
        }
    }
}