using System;
using UnityEngine;

public class GorillaMovement : MonoBehaviour
{
    public static GorillaMovement Instance;

    [Header("Special Abilities")]
    [SerializeField]
    private float dashAttackCooldown = 3f;

    [SerializeField]
    private float dashSpeed = 10f;

    [Header("Gorilla Movement")]
    public SphereCollider headCollider;
    public CapsuleCollider bodyCollider;

    public Transform leftHandFollower;
    public Transform rightHandFollower;

    public Transform rightHandTransform;
    public Transform leftHandTransform;

    private Vector3 lastLeftHandPosition;
    private Vector3 lastRightHandPosition;
    private Vector3 lastHeadPosition;

    private Rigidbody playerRigidBody;

    public int velocityHistorySize;
    public float maxArmLength = 1.5f;
    public float unStickDistance = 1f;

    public float velocityLimit;
    public float maxJumpSpeed;
    public float jumpMultiplier;
    public float minimumRaycastDistance = 0.05f;
    public float defaultSlideFactor = 0.03f;
    public float defaultPrecision = 0.995f;

    private Vector3[] velocityHistory;
    private int velocityIndex;
    private Vector3 currentVelocity;
    private Vector3 denormalizedVelocityAverage;
    private bool jumpHandIsLeft;
    private Vector3 lastPosition;

    public Vector3 rightHandOffset;
    public Vector3 leftHandOffset;

    public LayerMask locomotionEnabledLayers;

    public bool wasLeftHandTouching;
    public bool wasRightHandTouching;

    public bool disableMovement = false;

    private Vector3 spawnedLocation;
    public bool isDashAttacking { get; private set; }
    private float lastDashAttackTime;

    private HandTriggerDetection leftHandTriggerDetection, rightHandTriggerDetection;
    private MeshRenderer leftFollowerMR, rightFollowerMR;
    private Color originalFollowerColor;
    private AudioSource audioSource;


    private void Awake()
    {
        Instance = this;
        audioSource = GetComponent<AudioSource>();
        InitializeValues();
    }

    public void InitializeValues()
    {
        leftHandTriggerDetection = leftHandFollower.GetComponentInChildren<HandTriggerDetection>();
        rightHandTriggerDetection = rightHandFollower.GetComponentInChildren<HandTriggerDetection>();
        playerRigidBody = GetComponent<Rigidbody>();
        leftFollowerMR = leftHandFollower.GetComponent<MeshRenderer>();
        rightFollowerMR = rightHandFollower.GetComponent<MeshRenderer>();
        originalFollowerColor = leftFollowerMR.material.color;

        velocityHistory = new Vector3[velocityHistorySize];
        lastLeftHandPosition = leftHandFollower.transform.position;
        lastRightHandPosition = rightHandFollower.transform.position;
        lastHeadPosition = headCollider.transform.position;
        velocityIndex = 0;
        lastPosition = transform.position;
        spawnedLocation = lastPosition;
    }

    public void SetNetworkUser(NetworkUser user)
    {
        user.onDamaged += OnDamaged;
        user.onRespawned += OnRespawned;
    }

    private void OnDamaged(int damage, float knockBackAmount, Vector3 knockBackDirection)
    {
        playerRigidBody.AddForce(knockBackDirection * knockBackAmount, ForceMode.Impulse);
    }

    private void OnRespawned()
    {
        playerRigidBody.MovePosition(spawnedLocation);
    }

    private void Update()
    {
        disableMovement = PlayerStats.Instance.CurrentHealth == 0;

        if (!disableMovement)
        {
            CheckDashAttack();
        }
        else
        {
            ResetHandFollowTriggers();
        }

        bool leftHandColliding = false;
        bool rightHandColliding = false;
        Vector3 finalPosition;
        Vector3 rigidBodyMovement = Vector3.zero;
        Vector3 firstIterationLeftHand = Vector3.zero;
        Vector3 firstIterationRightHand = Vector3.zero;
        RaycastHit hitInfo;

        bodyCollider.transform.eulerAngles = new Vector3(0, headCollider.transform.eulerAngles.y, 0);

        //left hand

        Vector3 distanceTraveled = CurrentLeftHandPosition() - lastLeftHandPosition +
                                   Vector3.down * 2f * 9.8f * Time.deltaTime * Time.deltaTime;

        if (IterativeCollisionSphereCast(lastLeftHandPosition, minimumRaycastDistance, distanceTraveled, defaultPrecision,
                out finalPosition, true))
        {
            //this lets you stick to the position you touch, as long as you keep touching the surface this will be the zero point for that hand
            if (wasLeftHandTouching)
            {
                firstIterationLeftHand = lastLeftHandPosition - CurrentLeftHandPosition();
            }
            else
            {
                firstIterationLeftHand = finalPosition - CurrentLeftHandPosition();
            }

            playerRigidBody.linearVelocity = Vector3.zero;

            leftHandColliding = true;
        }

        //right hand

        distanceTraveled = CurrentRightHandPosition() - lastRightHandPosition +
                           Vector3.down * 2f * 9.8f * Time.deltaTime * Time.deltaTime;

        if (IterativeCollisionSphereCast(lastRightHandPosition, minimumRaycastDistance, distanceTraveled, defaultPrecision,
                out finalPosition, true))
        {
            if (wasRightHandTouching)
            {
                firstIterationRightHand = lastRightHandPosition - CurrentRightHandPosition();
            }
            else
            {
                firstIterationRightHand = finalPosition - CurrentRightHandPosition();
            }

            playerRigidBody.linearVelocity = Vector3.zero;

            rightHandColliding = true;
        }

        //average or add

        if ((leftHandColliding || wasLeftHandTouching) && (rightHandColliding || wasRightHandTouching))
        {
            //this lets you grab stuff with both hands at the same time
            rigidBodyMovement = (firstIterationLeftHand + firstIterationRightHand) / 2;
        }
        else
        {
            rigidBodyMovement = firstIterationLeftHand + firstIterationRightHand;
        }

        //check valid head movement

        if (IterativeCollisionSphereCast(lastHeadPosition, headCollider.radius,
                headCollider.transform.position + rigidBodyMovement - lastHeadPosition, defaultPrecision, out finalPosition, false))
        {
            rigidBodyMovement = finalPosition - lastHeadPosition;
            //last check to make sure the head won't phase through geometry
            if (Physics.Raycast(lastHeadPosition, headCollider.transform.position - lastHeadPosition + rigidBodyMovement, out hitInfo,
                    (headCollider.transform.position - lastHeadPosition + rigidBodyMovement).magnitude +
                    headCollider.radius * defaultPrecision * 0.999f, locomotionEnabledLayers.value))
            {
                rigidBodyMovement = lastHeadPosition - headCollider.transform.position;
            }
        }

        if (rigidBodyMovement != Vector3.zero)
        {
            transform.position = transform.position + rigidBodyMovement;
        }

        lastHeadPosition = headCollider.transform.position;

        //do final left hand position

        distanceTraveled = CurrentLeftHandPosition() - lastLeftHandPosition;

        if (IterativeCollisionSphereCast(lastLeftHandPosition, minimumRaycastDistance, distanceTraveled, defaultPrecision,
                out finalPosition, !((leftHandColliding || wasLeftHandTouching) && (rightHandColliding || wasRightHandTouching))))
        {
            lastLeftHandPosition = finalPosition;
            leftHandColliding = true;
        }
        else
        {
            lastLeftHandPosition = CurrentLeftHandPosition();
        }

        //do final right hand position

        distanceTraveled = CurrentRightHandPosition() - lastRightHandPosition;

        if (IterativeCollisionSphereCast(lastRightHandPosition, minimumRaycastDistance, distanceTraveled, defaultPrecision,
                out finalPosition, !((leftHandColliding || wasLeftHandTouching) && (rightHandColliding || wasRightHandTouching))))
        {
            lastRightHandPosition = finalPosition;
            rightHandColliding = true;
        }
        else
        {
            lastRightHandPosition = CurrentRightHandPosition();
        }

        StoreVelocities();

        if ((rightHandColliding || leftHandColliding) && !disableMovement)
        {
            if (denormalizedVelocityAverage.magnitude > velocityLimit)
            {
                if (denormalizedVelocityAverage.magnitude * jumpMultiplier > maxJumpSpeed)
                {
                    playerRigidBody.linearVelocity = denormalizedVelocityAverage.normalized * maxJumpSpeed;
                }
                else
                {
                    playerRigidBody.linearVelocity = jumpMultiplier * denormalizedVelocityAverage;
                }
            }
        }

        //check to see if left hand is stuck and we should unstick it

        if (leftHandColliding && (CurrentLeftHandPosition() - lastLeftHandPosition).magnitude > unStickDistance && !Physics.SphereCast(
                headCollider.transform.position, minimumRaycastDistance * defaultPrecision,
                CurrentLeftHandPosition() - headCollider.transform.position, out hitInfo,
                (CurrentLeftHandPosition() - headCollider.transform.position).magnitude - minimumRaycastDistance,
                locomotionEnabledLayers.value))
        {
            lastLeftHandPosition = CurrentLeftHandPosition();
            leftHandColliding = false;
        }

        //check to see if right hand is stuck and we should unstick it

        if (rightHandColliding && (CurrentRightHandPosition() - lastRightHandPosition).magnitude > unStickDistance &&
            !Physics.SphereCast(headCollider.transform.position, minimumRaycastDistance * defaultPrecision,
                CurrentRightHandPosition() - headCollider.transform.position, out hitInfo,
                (CurrentRightHandPosition() - headCollider.transform.position).magnitude - minimumRaycastDistance,
                locomotionEnabledLayers.value))
        {
            lastRightHandPosition = CurrentRightHandPosition();
            rightHandColliding = false;
        }

        leftHandFollower.position = lastLeftHandPosition;
        rightHandFollower.position = lastRightHandPosition;

        wasLeftHandTouching = leftHandColliding;
        wasRightHandTouching = rightHandColliding;
    }

    private void CheckDashAttack()
    {
        // will be true before first dash attack and then anytime after cooldown
        if (Time.time - lastDashAttackTime > dashAttackCooldown)
        {
            if (isDashAttacking)
            {
                isDashAttacking = false;
                ResetHandFollowTriggers();
            }
            else
            {
                if (MyInputSystem.Instance.IsTriggerActive(HandSide.Left))
                {
                    leftFollowerMR.material.color = Color.red;
                    if (leftHandTriggerDetection.handRelativeVelocity.magnitude > 2.5f)
                    {
                        DashAttack(HandSide.Left);
                        return;
                    }
                }
                else
                {
                    leftFollowerMR.material.color = originalFollowerColor;
                }

                if (MyInputSystem.Instance.IsTriggerActive(HandSide.Right))
                {
                    rightFollowerMR.material.color = Color.red;
                    if (rightHandTriggerDetection.handRelativeVelocity.magnitude > 2.5f)
                    {
                        DashAttack(HandSide.Right);
                        return;
                    }
                }
                else
                {
                    rightFollowerMR.material.color = originalFollowerColor;
                }
            }
        }
    }

    private void ResetHandFollowTriggers()
    {
        leftHandTriggerDetection.transform.localScale = Vector3.one;
        rightHandTriggerDetection.transform.localScale = Vector3.one;
        leftFollowerMR.material.color = originalFollowerColor;
        rightFollowerMR.material.color = originalFollowerColor;
    }

    private void DashAttack(HandSide handSide)
    {
        // Debug.Log($"Dash attacking with handVelocity: {handVelocity}");
        isDashAttacking = true;

        // Scale up hand trigger for easier dash attack hitting
        if (handSide == HandSide.Left)
        {
            leftHandTriggerDetection.transform.localScale *= 2f;
        }
        else
        {
            rightHandTriggerDetection.transform.localScale *= 2f;
        }

        var handDirection = handSide == HandSide.Left
            ? leftHandTriggerDetection.handRelativeVelocity.normalized
            : rightHandTriggerDetection.handRelativeVelocity.normalized;

        playerRigidBody.linearVelocity = Vector3.zero;
        playerRigidBody.AddForce(handDirection * dashSpeed, ForceMode.Impulse);

        MyInputSystem.Instance.Vibrate(handSide, 0.75f, 1f, 100f);
        audioSource.PlayOneShot(PlayerStats.Instance.DashSFX);
        PlayerStats.Instance.networkUser.RPC_PlayDashAttackAudio();

        leftFollowerMR.material.color = Color.grey;
        rightFollowerMR.material.color = Color.grey;
        lastDashAttackTime = Time.time;
    }

    private Vector3 CurrentLeftHandPosition()
    {
        if ((PositionWithOffset(leftHandTransform, leftHandOffset) - headCollider.transform.position).magnitude < maxArmLength)
        {
            return PositionWithOffset(leftHandTransform, leftHandOffset);
        }

        return headCollider.transform.position +
               (PositionWithOffset(leftHandTransform, leftHandOffset) - headCollider.transform.position).normalized * maxArmLength;
    }

    private Vector3 CurrentRightHandPosition()
    {
        if ((PositionWithOffset(rightHandTransform, rightHandOffset) - headCollider.transform.position).magnitude < maxArmLength)
        {
            return PositionWithOffset(rightHandTransform, rightHandOffset);
        }

        return headCollider.transform.position +
               (PositionWithOffset(rightHandTransform, rightHandOffset) - headCollider.transform.position).normalized *
               maxArmLength;
    }

    private Vector3 PositionWithOffset(Transform transformToModify, Vector3 offsetVector)
    {
        return transformToModify.position + transformToModify.rotation * offsetVector;
    }

    private bool IterativeCollisionSphereCast(Vector3 startPosition, float sphereRadius, Vector3 movementVector, float precision,
        out Vector3 endPosition, bool singleHand)
    {
        RaycastHit hitInfo;
        Vector3 movementToProjectedAboveCollisionPlane;
        Surface gorillaSurface;
        float slipPercentage;
        //first spherecast from the starting position to the final position
        if (CollisionsSphereCast(startPosition, sphereRadius * precision, movementVector, precision, out endPosition, out hitInfo))
        {
            //if we hit a surface, do a bit of a slide. this makes it so if you grab with two hands you don't stick 100%, and if you're pushing along a surface while braced with your head, your hand will slide a bit

            //take the surface normal that we hit, then along that plane, do a spherecast to a position a small distance away to account for moving perpendicular to that surface
            Vector3 firstPosition = endPosition;
            gorillaSurface = hitInfo.collider.GetComponent<Surface>();
            slipPercentage = gorillaSurface != null ? gorillaSurface.slipPercentage : (!singleHand ? defaultSlideFactor : 0.001f);
            movementToProjectedAboveCollisionPlane =
                Vector3.ProjectOnPlane(startPosition + movementVector - firstPosition, hitInfo.normal) * slipPercentage;
            if (CollisionsSphereCast(endPosition, sphereRadius, movementToProjectedAboveCollisionPlane, precision * precision,
                    out endPosition, out hitInfo))
            {
                //if we hit trying to move perpendicularly, stop there and our end position is the final spot we hit
                return true;
            }
            //if not, try to move closer towards the true point to account for the fact that the movement along the normal of the hit could have moved you away from the surface
            else if (CollisionsSphereCast(movementToProjectedAboveCollisionPlane + firstPosition, sphereRadius,
                         startPosition + movementVector - (movementToProjectedAboveCollisionPlane + firstPosition),
                         precision * precision * precision, out endPosition, out hitInfo))
            {
                //if we hit, then return the spot we hit
                return true;
            }
            else
            {
                //this shouldn't really happe, since this means that the sliding motion got you around some corner or something and let you get to your final point. back off because something strange happened, so just don't do the slide
                endPosition = firstPosition;
                return true;
            }
        }
        //as kind of a sanity check, try a smaller spherecast. this accounts for times when the original spherecast was already touching a surface so it didn't trigger correctly
        else if (CollisionsSphereCast(startPosition, sphereRadius * precision * 0.66f,
                     movementVector.normalized * (movementVector.magnitude + sphereRadius * precision * 0.34f), precision * 0.66f,
                     out endPosition, out hitInfo))
        {
            endPosition = startPosition;
            return true;
        }
        else
        {
            endPosition = Vector3.zero;
            return false;
        }
    }

    private bool CollisionsSphereCast(Vector3 startPosition, float sphereRadius, Vector3 movementVector, float precision,
        out Vector3 finalPosition, out RaycastHit hitInfo)
    {
        //kind of like a souped up spherecast. includes checks to make sure that the sphere we're using, if it touches a surface, is pushed away the correct distance (the original sphereradius distance). since you might
        //be pushing into sharp corners, this might not always be valid, so that's what the extra checks are for

        //initial spherecase
        RaycastHit innerHit;
        if (Physics.SphereCast(startPosition, sphereRadius * precision, movementVector, out hitInfo,
                movementVector.magnitude + sphereRadius * (1 - precision), locomotionEnabledLayers.value))
        {
            //if we hit, we're trying to move to a position a sphereradius distance from the normal
            finalPosition = hitInfo.point + hitInfo.normal * sphereRadius;

            //check a spherecase from the original position to the intended final position
            if (Physics.SphereCast(startPosition, sphereRadius * precision * precision, finalPosition - startPosition, out innerHit,
                    (finalPosition - startPosition).magnitude + sphereRadius * (1 - precision * precision),
                    locomotionEnabledLayers.value))
            {
                finalPosition = startPosition + (finalPosition - startPosition).normalized *
                    Mathf.Max(0, hitInfo.distance - sphereRadius * (1f - precision * precision));
                hitInfo = innerHit;
            }
            //bonus raycast check to make sure that something odd didn't happen with the spherecast. helps prevent clipping through geometry
            else if (Physics.Raycast(startPosition, finalPosition - startPosition, out innerHit,
                         (finalPosition - startPosition).magnitude + sphereRadius * precision * precision * 0.999f,
                         locomotionEnabledLayers.value))
            {
                finalPosition = startPosition;
                hitInfo = innerHit;
                return true;
            }

            return true;
        }
        //anti-clipping through geometry check
        else if (Physics.Raycast(startPosition, movementVector, out hitInfo,
                     movementVector.magnitude + sphereRadius * precision * 0.999f, locomotionEnabledLayers.value))
        {
            finalPosition = startPosition;
            return true;
        }
        else
        {
            finalPosition = Vector3.zero;
            return false;
        }
    }

    public bool IsHandTouching(bool forLeftHand)
    {
        if (forLeftHand)
        {
            return wasLeftHandTouching;
        }
        else
        {
            return wasRightHandTouching;
        }
    }

    public void Turn(float degrees)
    {
        transform.RotateAround(headCollider.transform.position, transform.up, degrees);
        denormalizedVelocityAverage = Quaternion.Euler(0, degrees, 0) * denormalizedVelocityAverage;
        for (int i = 0; i < velocityHistory.Length; i++)
        {
            velocityHistory[i] = Quaternion.Euler(0, degrees, 0) * velocityHistory[i];
        }
    }

    private void StoreVelocities()
    {
        velocityIndex = (velocityIndex + 1) % velocityHistorySize;
        Vector3 oldestVelocity = velocityHistory[velocityIndex];
        currentVelocity = (transform.position - lastPosition) / Time.deltaTime;
        denormalizedVelocityAverage += (currentVelocity - oldestVelocity) / (float) velocityHistorySize;
        velocityHistory[velocityIndex] = currentVelocity;
        lastPosition = transform.position;
    }
}