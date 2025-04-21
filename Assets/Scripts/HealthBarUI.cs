using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HealthBarUI : MonoBehaviour
{
    public Slider healthSlider;         // Assign in Inspector
    public Entity playerEntity;         // Assign the player's Entity script

    void Start()
    {
        if (playerEntity != null)
            healthSlider.maxValue = playerEntity.StartingHealth;
    }

    void Update()
    {
        if (playerEntity != null)
        {
            float targetHealth = playerEntity.Health;
            healthSlider.value = Mathf.Lerp(healthSlider.value, targetHealth, Time.deltaTime * 10f);
        }
    }

}
