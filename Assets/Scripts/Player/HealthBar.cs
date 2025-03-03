using UnityEngine;
using UnityEngine.UI;

public class HealthBar : MonoBehaviour
{
    public Slider healthSlider;

    public void SetHealth(float currentHealth)
    {
        healthSlider.value = currentHealth;
    }
}
