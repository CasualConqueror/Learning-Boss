using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    public float sprintMultiplier = 1.5f;
    public float jumpForce = 5f;
    public float groundDrag = 6f;
    public float airDrag = 2f;

    [Header("Sprint Settings")]
    public float minStaminaToSprint = 10f;
    public float sprintStaminaCost = 20f; // Stamina cost per second
    public float jumpStaminaCost = 15f;   // Stamina cost per jump
    public float staminaDrainMultiplier = 1f; // Adjust stamina drain speed
    public bool showDebugInfo = false;

    [Header("Collision Settings")]
    public LayerMask enemyLayers; // Set this to include enemy layers
    public float slideOffForce = 5f; // Force to push player off enemies
    public float maxSlopeAngle = 45f; // Maximum angle the player can walk up

    private Rigidbody rb;
    private Transform cameraTransform;
    private bool isGrounded;
    private Vector3 moveDirection;
    private float currentSpeed;
    private bool isSprinting;
    private PlayerStats playerStats;
    private RaycastHit slopeHit;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        cameraTransform = Camera.main.transform;
        playerStats = GetComponent<PlayerStats>();

        if (playerStats == null)
        {
            Debug.LogError("PlayerStats component not found on player!");
        }

        rb.freezeRotation = true;
        rb.drag = groundDrag;
    }

    void Update()
    {
        HandleInput();
        CheckGroundStatus();
        UpdateSpeedAndStamina();
        HandleEnemyCollisions();
    }

    void FixedUpdate()
    {
        ApplyMovement();
    }

    void HandleInput()
    {
        float moveX = Input.GetAxisRaw("Horizontal");
        float moveZ = Input.GetAxisRaw("Vertical");

        moveDirection = (cameraTransform.right * moveX + cameraTransform.forward * moveZ);
        moveDirection.y = 0f;
        moveDirection = moveDirection.normalized;

        if (Input.GetKeyDown(KeyCode.Space) && isGrounded)
        {
            if (playerStats != null && playerStats.currentStamina >= jumpStaminaCost)
            {
                Jump();
                playerStats.UseStamina(jumpStaminaCost);
            }
            else if (showDebugInfo)
            {
                Debug.Log("Not enough stamina to jump!");
            }
        }
    }

    void UpdateSpeedAndStamina()
    {
        // Only allow sprinting if we have enough stamina and are actually moving
        bool wantsToSprint = (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)) && moveDirection.magnitude > 0.1f;

        // Check if we have enough stamina to sprint
        bool canSprint = playerStats != null && playerStats.currentStamina >= minStaminaToSprint;

        // Set sprint state based on both wanting to sprint and having enough stamina
        isSprinting = wantsToSprint && canSprint;

        // Calculate current speed and handle stamina
        if (isSprinting && moveDirection.magnitude > 0.1f)
        {
            currentSpeed = moveSpeed * sprintMultiplier;

            // Drain stamina only when actually sprinting and moving
            if (playerStats != null)
            {
                float staminaCost = sprintStaminaCost * Time.deltaTime * staminaDrainMultiplier;
                playerStats.UseStamina(staminaCost);

                // Stop sprinting if stamina drops too low
                if (playerStats.currentStamina < minStaminaToSprint)
                {
                    isSprinting = false;
                    currentSpeed = moveSpeed;
                }
            }
        }
        else
        {
            currentSpeed = moveSpeed;
        }

        if (showDebugInfo)
        {
            Debug.Log($"Speed: {currentSpeed}, Sprinting: {isSprinting}, Stamina: {(playerStats != null ? playerStats.currentStamina : 0)}");
        }
    }

    void ApplyMovement()
    {
        if (moveDirection.magnitude > 0.1f)
        {
            // Check if we're on a slope that's too steep
            if (OnSteepSlope())
            {
                // Apply force down the slope to prevent climbing
                Vector3 slopeDirection = Vector3.ProjectOnPlane(moveDirection, slopeHit.normal).normalized;
                rb.AddForce(slopeDirection * currentSpeed * 0.5f, ForceMode.Force);
            }
            else
            {
                // Normal movement
                Vector3 targetVelocity = moveDirection * currentSpeed;
                rb.AddForce(targetVelocity, ForceMode.Force);
            }

            // Limit horizontal velocity
            Vector3 flatVel = new Vector3(rb.velocity.x, 0f, rb.velocity.z);
            if (flatVel.magnitude > currentSpeed)
            {
                Vector3 limitedVel = flatVel.normalized * currentSpeed;
                rb.velocity = new Vector3(limitedVel.x, rb.velocity.y, limitedVel.z);
            }
        }
    }

    bool OnSteepSlope()
    {
        if (Physics.Raycast(transform.position, Vector3.down, out slopeHit, 1.5f))
        {
            float angle = Vector3.Angle(Vector3.up, slopeHit.normal);
            return angle > maxSlopeAngle;
        }
        return false;
    }

    void HandleEnemyCollisions()
    {
        // Check if we're colliding with an enemy and slide off them
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, 0.6f, enemyLayers);
        foreach (var hitCollider in hitColliders)
        {
            // If we're above the enemy's center point, apply a sliding force
            if (transform.position.y > hitCollider.bounds.center.y)
            {
                Vector3 pushDirection = (transform.position - hitCollider.transform.position).normalized;
                pushDirection.y = 0; // Keep it horizontal

                // Add a small downward force to help the player slide off
                rb.AddForce((pushDirection * slideOffForce) + (Vector3.down * 2f), ForceMode.Impulse);

                if (showDebugInfo)
                {
                    Debug.Log("Sliding off enemy: " + hitCollider.name);
                }
            }
        }
    }

    void Jump()
    {
        rb.velocity = new Vector3(rb.velocity.x, 0f, rb.velocity.z);
        rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
    }

    void CheckGroundStatus()
    {
        // Additional ground check - more precise than just relying on collision events
        isGrounded = Physics.Raycast(transform.position, Vector3.down, 1.1f);
        rb.drag = isGrounded ? groundDrag : airDrag;
    }

    private void OnCollisionStay(Collision collision)
    {
        foreach (ContactPoint contact in collision.contacts)
        {
            // Consider ground only if the contact normal is pointing mostly upward
            if (contact.normal.y > 0.7f && collision.gameObject.CompareTag("Ground"))
            {
                isGrounded = true;
                return;
            }
        }

        // Check if we're colliding with an enemy
        if (enemyLayers == (enemyLayers | (1 << collision.gameObject.layer)))
        {
            // Find the contact point with the highest y value (to detect if we're on top)
            float highestY = float.MinValue;
            ContactPoint highestContact = new ContactPoint();

            foreach (ContactPoint contact in collision.contacts)
            {
                if (contact.point.y > highestY)
                {
                    highestY = contact.point.y;
                    highestContact = contact;
                }
            }

            // If we're on top of the enemy, add sliding force
            if (highestContact.normal.y > 0.5f)
            {
                Vector3 slideDirection = Vector3.ProjectOnPlane(rb.velocity, highestContact.normal).normalized;
                if (slideDirection.magnitude < 0.1f)
                {
                    slideDirection = (transform.position - collision.transform.position).normalized;
                    slideDirection.y = 0;
                }

                rb.AddForce(slideDirection * slideOffForce + Vector3.down * 3f, ForceMode.Impulse);
            }
        }
    }

    private void OnCollisionExit(Collision collision)
    {
        if (collision.gameObject.CompareTag("Ground"))
        {
            isGrounded = false;
        }
    }

    private void OnDrawGizmos()
    {
        if (showDebugInfo)
        {
            // Visualize the ground check ray
            Gizmos.color = isGrounded ? Color.green : Color.red;
            Gizmos.DrawRay(transform.position, Vector3.down * 1.1f);

            // Visualize the collision detection sphere
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, 0.6f);
        }
    }
}