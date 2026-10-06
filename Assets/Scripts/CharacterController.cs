using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class CharacterController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private InputReader inputReader;
    [Tooltip("Transform enfant qui porte le visuel et qui tourne vers la direction de mouvement.")]
    [SerializeField] private Transform visual;

    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float acceleration = 10f;
    [SerializeField] private float friction = 8f;

    [Header("Rotation Settings")]
    [SerializeField] private float rotationSpeed = 15f;

    [Header("Ground Check")]
    [SerializeField] private float groundCheckDistance = 0.1f;
    [SerializeField] private float groundCheckOffset = 0.1f;
    [SerializeField] private LayerMask groundLayer;

    private Rigidbody rb;
    private bool isGrounded;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    private void FixedUpdate()
    {
        if (inputReader == null) return;

        Vector2 moveInput = inputReader.MoveDirection;

        isGrounded = Physics.Raycast(
            transform.position + Vector3.up * groundCheckOffset,
            Vector3.down,
            groundCheckDistance + groundCheckOffset,
            groundLayer);

        Vector3 moveDirection = new Vector3(moveInput.x, 0f, moveInput.y).normalized;

        Vector3 currentVelocity = rb.linearVelocity;
        Vector3 horizontalVelocity = new Vector3(currentVelocity.x, 0f, currentVelocity.z);

        if (moveInput.sqrMagnitude > 0.01f)
        {
            Vector3 targetVelocity = moveDirection * moveSpeed;
            horizontalVelocity = Vector3.MoveTowards(horizontalVelocity, targetVelocity, acceleration * Time.fixedDeltaTime);
        }
        else if (isGrounded)
        {
            horizontalVelocity = Vector3.MoveTowards(horizontalVelocity, Vector3.zero, friction * Time.fixedDeltaTime);
        }

        rb.linearVelocity = new Vector3(horizontalVelocity.x, currentVelocity.y, horizontalVelocity.z);

        if (isGrounded && rb.linearVelocity.y > 0f)
        {
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, -1f, rb.linearVelocity.z);
        }

        // Rotation visuelle vers la direction de mouvement
        if (visual != null && moveDirection.sqrMagnitude > 0.001f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection, Vector3.up);
            visual.rotation = Quaternion.Slerp(
                visual.rotation,
                targetRotation,
                rotationSpeed * Time.fixedDeltaTime);
        }
    }
}