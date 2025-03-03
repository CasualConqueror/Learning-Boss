using UnityEngine;
using UnityEngine.UI;

public class StaminaBar : MonoBehaviour
{
    public Slider staminaSlider;


    public void SetStamina(float currentStamina)
    {
        staminaSlider.value = currentStamina;
    }
}
