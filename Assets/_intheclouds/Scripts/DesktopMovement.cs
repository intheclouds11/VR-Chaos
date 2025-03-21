using UnityEngine;

public class DesktopMovement : MonoBehaviour
{
    [SerializeField]
    private float moveSpeed = 5f;

    [SerializeField]
    private float jumpForce = 5f;

    private Rigidbody rb;
    private bool isGrounded;
    private MyInputSystem inputSystem;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        inputSystem = MyInputSystem.Instance;
    }

    private void Update()
    {
        float moveX = inputSystem.GetDesktopTranslation().x;
        float moveZ = inputSystem.GetDesktopTranslation().y;

        Vector3 move = new Vector3(moveX, 0f, moveZ) * moveSpeed;
        rb.linearVelocity = new Vector3(move.x, rb.linearVelocity.y, move.z);

        if (inputSystem.WasDesktopJumpActivated() && isGrounded)
        {
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
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