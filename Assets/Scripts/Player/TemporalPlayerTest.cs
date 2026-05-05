using UnityEngine;
using UnityEngine.InputSystem;

public class TemporalPlayerTest : MonoBehaviour
{
    public float speed = 5f;
    public float jumpForce = 5f;

    private Rigidbody rb;
    private bool isGrounded;

    private Vector2 moveInput;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    void Update()
    {
        moveInput = Vector2.zero;

        var keyboard = Keyboard.current;

        if (keyboard.wKey.isPressed) moveInput.y += 1;
        if (keyboard.sKey.isPressed) moveInput.y -= 1;
        if (keyboard.aKey.isPressed) moveInput.x -= 1;
        if (keyboard.dKey.isPressed) moveInput.x += 1;

        if (keyboard.spaceKey.wasPressedThisFrame && isGrounded)
        {
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
        }
    }

    void FixedUpdate()
    {
        Vector3 movement = new Vector3(moveInput.x, 0, moveInput.y).normalized * speed;
        Vector3 velocity = rb.linearVelocity;

        rb.linearVelocity = new Vector3(movement.x, velocity.y, movement.z);
    }

    void OnCollisionStay(Collision collision)
    {
        isGrounded = true;
    }

    void OnCollisionExit(Collision collision)
    {
        isGrounded = false;
    }
}