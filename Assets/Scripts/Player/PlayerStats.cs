using UnityEngine;
using UnityEngine.UI;

public class PlayerStats : MonoBehaviour
{
    public float maxHealth = 100f;
    public float currentHealth = 100f;
    public float healthRegenRate = 2f; // Health per second
    public float healthRegenDelay = 5f; // Delay before regen starts
    private float healthRegenTimer;

    public float maxStamina = 100f;
    public float currentStamina = 100f;
    public float staminaRegenRate = 5f; // Stamina per second

    [Header("Stamina Costs")]
    public float sprintStaminaCost = 20f; // Cost per second
    public float jumpStaminaCost = 15f;   // Cost per jump

    [Header("UI References")]
    public HealthBar healthBar;
    public StaminaBar staminaBar;

    private void Start()
    {
        currentHealth = maxHealth;
        currentStamina = maxStamina;

        healthBar.SetHealth(currentHealth);
        staminaBar.SetStamina(currentStamina);
    }

    private void Update()
    {
        RegenerateStamina();
        RegenerateHealth();

        if (transform.position.y < -15)
            {
                Die();
            }
    }

    public void TakeDamage(float damage)
    {
        currentHealth = Mathf.Max(currentHealth - damage, 0);
        healthBar.SetHealth(currentHealth);

        healthRegenTimer = healthRegenDelay; // Reset regen timer on damage

        if (currentHealth <= 0)
        {
            Die();
        }
    }
    // In PlayerStats.cs
    public void ResetPlayer()
    {
        currentHealth = maxHealth;
        currentStamina = maxStamina;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // Update UI
        healthBar.SetHealth(currentHealth);
        staminaBar.SetStamina(currentStamina);

        // Re-enable player controller if it was disabled
        PlayerController controller = GetComponent<PlayerController>();
        if (controller != null) controller.enabled = true;
    }

    private void RegenerateHealth()
    {
        if (currentHealth < maxHealth)
        {
            if (healthRegenTimer > 0)
            {
                healthRegenTimer -= Time.deltaTime; // Wait before regen starts
            }
            else
            {
                currentHealth = Mathf.Min(currentHealth + healthRegenRate * Time.deltaTime, maxHealth);
                healthBar.SetHealth(currentHealth);
            }
        }
    }

    public void UseStamina(float amount)
    {
        if (currentStamina >= amount)
        {
            currentStamina -= amount;
            staminaBar.SetStamina(currentStamina);
        }
    }

    public bool HasStaminaToSprint()
    {
        return currentStamina >= sprintStaminaCost * Time.deltaTime;
    }

    public bool HasStaminaToJump()
    {
        return currentStamina >= jumpStaminaCost;
    }

    public void UseSprintStamina()
    {
        if (HasStaminaToSprint())
        {
            UseStamina(sprintStaminaCost * Time.deltaTime);
        }
    }

    public bool UseJumpStamina()
    {
        if (HasStaminaToJump())
        {
            UseStamina(jumpStaminaCost);
            return true;
        }
        return false;
    }

    private void RegenerateStamina()
    {
        if (currentStamina < maxStamina)
        {
            currentStamina = Mathf.Min(currentStamina + staminaRegenRate * Time.deltaTime, maxStamina);
            staminaBar.SetStamina(currentStamina);
        }
    }

    // In PlayerStats.cs
    private void Die()
    {
        Debug.Log("Player has died.");

        // Show death screen
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowPlayerDeathScreen();
        }

        // Disable player controls
        GetComponent<PlayerController>().enabled = false;
    }
}
