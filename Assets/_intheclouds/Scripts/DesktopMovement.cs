using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class DesktopMovement : MonoBehaviour
{
    public static DesktopMovement Instance;
    [SerializeField]
    private float moveSpeed = 5f;

    [SerializeField]
    private float jumpForce = 5f;

    public float lookSensitivity = 2f;
    public InputAction lookAction;

    private Vector2 lookInput;
    private float xRotation;
    private Rigidbody playerRigidbody;
    private bool isGrounded;
    private MyInputSystem inputSystem;
    private Vector3 spawnedLocation;


    void OnEnable()
    {
        lookAction.performed += ctx => lookInput = ctx.ReadValue<Vector2>();
        lookAction.canceled += ctx => lookInput = Vector2.zero;
        lookAction.Enable();
    }

    void OnDisable()
    {
        lookAction.Disable();
    }

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

    private void OnDamaged(int damage, Vector3 knockBack)
    {
        playerRigidbody.linearVelocity = Vector3.zero;
        playerRigidbody.AddForce(knockBack, ForceMode.Impulse);
    }

    private void OnRespawned()
    {
        playerRigidbody.linearVelocity = Vector3.zero;
        playerRigidbody.MovePosition(spawnedLocation);
    }

    private void Update()
    {
        if (Mouse.current.rightButton.isPressed)
        {
            float mouseX = lookInput.x * lookSensitivity;
            float mouseY = lookInput.y * lookSensitivity;

            xRotation -= mouseY;
            xRotation = Mathf.Clamp(xRotation, -90f, 90f);

            // transform.parent.transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
            // transform.parent.transform.Rotate(Vector3.up * mouseX);
            transform.parent.transform.Rotate(0, 1 * mouseX, 0);
        }

        if (!isGrounded || PlayerStats.Instance.CurrentHealth == 0)
        {
            // playerRigidbody.linearVelocity = Vector3.zero;
            return;
        }

        float moveX = inputSystem.GetDesktopTranslation().x;
        float moveZ = inputSystem.GetDesktopTranslation().y;

        Vector3 move = new Vector3(moveX, 0f, moveZ) * moveSpeed;
        playerRigidbody.AddForce(new Vector3(move.x, 0, move.z), ForceMode.Force);
        // playerRigidbody.linearVelocity = new Vector3(move.x, playerRigidbody.linearVelocity.y, move.z);

        if (inputSystem.WasDesktopJumpActivated())
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