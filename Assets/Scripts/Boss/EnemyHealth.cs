using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    [SerializeField] private float maxHealth = 500f;
    private float currentHealth;

    public BossHealthBar bossHealthBar; // Reference to the Boss Health Bar UI

    [Header("Boss Components")]
    [SerializeField] private bool isBoss = false;
    private BossStateMachine bossStateMachine;
    private BossPersonalitySystem bossPersonality;

    [Header("Debug")]
    [SerializeField] private bool showDebugMessages = true;

    private void Awake()
    {
        // If this is a boss, try to get the boss-specific components
        if (isBoss)
        {
            bossStateMachine = GetComponent<BossStateMachine>();
            bossPersonality = GetComponent<BossPersonalitySystem>();

            if (bossStateMachine == null)
                Debug.LogWarning("[EnemyHealth] This is marked as a boss but has no BossStateMachine component!");

            if (bossPersonality == null)
                Debug.LogWarning("[EnemyHealth] This is marked as a boss but has no BossPersonalitySystem component!");
        }
    }

    private void Start()
    {
        currentHealth = maxHealth;

        if (bossHealthBar != null)
        {
            bossHealthBar.SetHealth(currentHealth);
        }
    }

    public void TakeDamage(float damage)
    {
        currentHealth -= damage;

        // Update UI
        if (bossHealthBar != null)
        {
            bossHealthBar.SetHealth(currentHealth);
        }

        // Handle boss-specific logic
        if (isBoss)
        {
            // Register damage with the personality system
            if (bossPersonality != null)
            {
                bossPersonality.RegisterDamageTaken(damage);

                if (showDebugMessages)
                {
                    Debug.Log($"[EnemyHealth] Boss took {damage} damage. Registered with personality system.");
                    Debug.Log($"[EnemyHealth] Total damage taken by current personality: {bossPersonality.damageTaken}");
                }
            }

            // Notify state machine
            if (bossStateMachine != null)
            {
                bossStateMachine.TakeDamage(damage);
            }
        }
        else if (showDebugMessages)
        {
            Debug.Log($"[EnemyHealth] {gameObject.name} took {damage} damage. Health: {currentHealth}/{maxHealth}");
        }

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    // Called by editor scripts or for debugging
    public void DisplayHealthStatus()
    {
        if (isBoss && bossPersonality != null)
        {
            Debug.Log($"[EnemyHealth] Boss health: {currentHealth}/{maxHealth}, " +
                      $"Current personality: {bossPersonality.currentPersonalityName}, " +
                      $"Damage dealt: {bossPersonality.damageDealt}, " +
                      $"Damage taken: {bossPersonality.damageTaken}");
        }
        else
        {
            Debug.Log($"[EnemyHealth] {gameObject.name} health: {currentHealth}/{maxHealth}");
        }
    }

    private void Die()
    {
        if (showDebugMessages)
        {
            Debug.Log($"[EnemyHealth] {gameObject.name} has died!");
        }

        // Optional: Hide or disable boss health bar on death
        if (bossHealthBar != null)
        {
            bossHealthBar.gameObject.SetActive(false);
        }

        // For a boss, you might want a different death behavior
        if (isBoss)
        {
            // Maybe a boss death animation or sequence instead of immediate destruction
            // For now, we'll disable the components instead of destroying the GameObject
            if (bossStateMachine != null) bossStateMachine.enabled = false;
            if (bossPersonality != null) bossPersonality.enabled = false;

            // Disable colliders
            foreach (Collider col in GetComponentsInChildren<Collider>())
            {
                col.enabled = false;
            }

            // Optionally play death animation
            Animator animator = GetComponent<Animator>();
            if (animator != null)
            {
                animator.SetTrigger("Death");
            }

            // Optional: Schedule destruction after a delay for death animation
            Invoke("DestroyBoss", 3.0f);
        }
        else
        {
            // Regular enemies get destroyed immediately
            Destroy(gameObject);
        }
    }

    // In EnemyHealth.cs - modify the Die() method
    private void DestroyBoss()
    {
        if (showDebugMessages)
        {
            Debug.Log($"[EnemyHealth] {gameObject.name} has died!");
        }

        // Optional: Hide or disable boss health bar on death
        if (bossHealthBar != null)
        {
            bossHealthBar.gameObject.SetActive(false);
        }

        // For a boss, show victory screen
        if (isBoss)
        {
            // Show boss defeated screen
            if (UIManager.Instance != null)
            {
                UIManager.Instance.ShowBossDefeatedScreen();
            }

            // Disable boss components
            if (bossStateMachine != null) bossStateMachine.enabled = false;
            if (bossPersonality != null) bossPersonality.enabled = false;

            // Disable colliders
            foreach (Collider col in GetComponentsInChildren<Collider>())
            {
                col.enabled = false;
            }

            // Optionally play death animation
            Animator animator = GetComponent<Animator>();
            if (animator != null)
            {
                animator.SetTrigger("Death");
            }
        }
        else
        {
            // Regular enemies get destroyed immediately
            Destroy(gameObject);
        }
    }
    // In EnemyHealth.cs
    public void ResetEnemy()
    {
        currentHealth = maxHealth;

        // Update UI
        if (bossHealthBar != null)
        {
            bossHealthBar.SetHealth(currentHealth);
            bossHealthBar.gameObject.SetActive(true);
        }
    }

    // For debugging in the inspector
    private void OnValidate()
    {
        if (isBoss)
        {
            // Auto-find boss components in the inspector if not assigned
            if (bossStateMachine == null)
                bossStateMachine = GetComponent<BossStateMachine>();

            if (bossPersonality == null)
                bossPersonality = GetComponent<BossPersonalitySystem>();
        }
    }
}