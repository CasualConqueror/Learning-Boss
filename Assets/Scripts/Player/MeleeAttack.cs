using UnityEngine;
using System.Collections;

public class MeleeAttack : MonoBehaviour
{
    [Header("Attack Settings")]
    [SerializeField] private float attackDamage = 20f;
    [SerializeField] private float attackRange = 2f;
    [SerializeField] private float attackCooldown = 0.8f;
    [SerializeField] private LayerMask enemyLayers;

    [Header("Hand Movement")]
    [SerializeField] private Transform handTransform;
    [SerializeField] private Vector3 punchStartPosition = new Vector3(0.3f, -0.2f, 0.2f);
    [SerializeField] private Vector3 punchEndPosition = new Vector3(0.3f, -0.2f, 1.0f);
    [SerializeField] private float punchDuration = 0.2f;
    [SerializeField] private float returnDuration = 0.15f;

    [Header("Optional Effects")]
    [SerializeField] private GameObject attackEffect;
    [SerializeField] private AudioClip attackSound;
    [SerializeField] private AudioSource audioSource;

    private bool canAttack = true;
    private Transform attackPoint;
    private Vector3 originalHandPosition;

    private void Start()
    {
        // Create an attack point in front of the player
        attackPoint = new GameObject("AttackPoint").transform;
        attackPoint.SetParent(transform);
        attackPoint.localPosition = new Vector3(0, 0, 0.5f); // Positioned in front of player

        // Initialize audio source if needed
        if (audioSource == null && attackSound != null)
            audioSource = gameObject.AddComponent<AudioSource>();

        // Store the original hand position
        if (handTransform != null)
            originalHandPosition = handTransform.localPosition;
    }

    private void Update()
    {
        // Check for attack input (can be customized based on your control scheme)
        if (Input.GetMouseButtonDown(0) && canAttack)
        {
            PerformAttack();
        }
    }

    public void PerformAttack()
    {
        if (!canAttack) return;

        // Start the attack cooldown
        StartCoroutine(AttackCooldown());

        // Start the hand punch animation
        if (handTransform != null)
        {
            StartCoroutine(MovePunch());
        }

        // Play attack sound (if assigned)
        if (audioSource != null && attackSound != null)
        {
            audioSource.PlayOneShot(attackSound);
        }

        // Show attack effect (if assigned)
        if (attackEffect != null)
        {
            Instantiate(attackEffect, attackPoint.position, attackPoint.rotation);
        }

        // Detect enemies in range
        Collider[] hitEnemies = Physics.OverlapSphere(attackPoint.position, attackRange, enemyLayers);

        // Apply damage to enemies with the "Enemy" tag
        foreach (Collider enemy in hitEnemies)
        {
            if (enemy.CompareTag("Enemy"))
            {
                // Try to get health component and apply damage
                EnemyHealth enemyHealth = enemy.GetComponent<EnemyHealth>();
                if (enemyHealth != null)
                {
                    enemyHealth.TakeDamage(attackDamage);
                }
                else
                {
                    // Alternative: Send a damage message that any script can receive
                    enemy.SendMessage("ApplyDamage", attackDamage, SendMessageOptions.DontRequireReceiver);
                }

                Debug.Log("Hit enemy: " + enemy.name);
            }
        }
    }

    private IEnumerator MovePunch()
    {
        // Store original position if not already stored
        if (originalHandPosition == Vector3.zero)
            originalHandPosition = handTransform.localPosition;

        // Forward punch movement
        float elapsedTime = 0f;
        while (elapsedTime < punchDuration)
        {
            handTransform.localPosition = Vector3.Lerp(
                originalHandPosition,
                punchEndPosition,
                elapsedTime / punchDuration
            );

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // Ensure it reaches the end position
        handTransform.localPosition = punchEndPosition;

        // Wait a tiny fraction at full extension
        yield return new WaitForSeconds(0.05f);

        // Return movement
        elapsedTime = 0f;
        while (elapsedTime < returnDuration)
        {
            handTransform.localPosition = Vector3.Lerp(
                punchEndPosition,
                originalHandPosition,
                elapsedTime / returnDuration
            );

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // Ensure it returns to the start position
        handTransform.localPosition = originalHandPosition;
    }

    private IEnumerator AttackCooldown()
    {
        // Disable attacking during cooldown
        canAttack = false;

        // Wait for the cooldown period
        yield return new WaitForSeconds(attackCooldown);

        // Enable attacking again
        canAttack = true;
    }

    // Used to visualize the attack range in the editor
    private void OnDrawGizmosSelected()
    {
        if (attackPoint == null)
        {
            // Draw gizmo at approximate position if attack point isn't created yet
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position + transform.forward * 0.5f, attackRange);
        }
        else
        {
            // Draw gizmo at actual attack point
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(attackPoint.position, attackRange);
        }

        // Draw punch movement path
        if (handTransform != null)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawLine(
                handTransform.parent.TransformPoint(punchStartPosition),
                handTransform.parent.TransformPoint(punchEndPosition)
            );
            Gizmos.DrawSphere(handTransform.parent.TransformPoint(punchEndPosition), 0.05f);
        }
    }
}