using System;
using UnityEngine;

public class DesktopMovement : MonoBehaviour
{
    public static DesktopMovement Instance;
    [SerializeField]
    private float moveSpeed = 5f;

    [SerializeField]
    private float jumpForce = 5f;

    private Rigidbody playerRigidbody;
    private bool isGrounded;
    private MyInputSystem inputSystem;
    private Vector3 spawnedLocation;


    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        playerRigidbody = GetComponent<Rigidbody>();
        inputSystem = MyInputSystem.Instance;
        spawnedLocation = transform.position;
    }
    
    public void SetNetworkUser(NetworkUser user)
    {
        user.onDamaged += OnDamaged;
        user.onRespawned += OnRespawned;
    }
    
    private void OnDamaged(int damage, float knockBackAmount, Vector3 knockBackDirection)
    {
        playerRigidbody.AddForce(knockBackDirection * knockBackAmount, ForceMode.Impulse);
    }
    
    private void OnRespawned()
    {
        playerRigidbody.MovePosition(spawnedLocation);
    }

    private void Update()
    {
        if (PlayerStats.Instance.CurrentHealth == 0)
        {
            // playerRigidbody.linearVelocity = Vector3.zero;
            return;
        }
        
        float moveX = inputSystem.GetDesktopTranslation().x;
        float moveZ = inputSystem.GetDesktopTranslation().y;

        Vector3 move = new Vector3(moveX, 0f, moveZ) * moveSpeed;
        playerRigidbody.AddForce(new Vector3(move.x, 0, move.z), ForceMode.Force);
        // playerRigidbody.linearVelocity = new Vector3(move.x, playerRigidbody.linearVelocity.y, move.z);

        if (inputSystem.WasDesktopJumpActivated() && isGrounded)
        {
            playerRigidbody.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.layer != LayerMask.NameToLayer("Surface"))
        {
            Debug.LogWarning("Player should only be able to collide with Surface layer");
            return;
        }

        isGrounded = true;
    }

    private void OnCollisionExit(Collision collision)
    {
        if (collision.gameObject.layer != LayerMask.NameToLayer("Surface"))
        {
            Debug.LogWarning("Player should only be able to collide with Surface layer");
            return;
        }

        isGrounded = false;
    }
}