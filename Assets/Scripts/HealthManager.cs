using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class HealthManager : MonoBehaviour
{
    public Image healthBar;
    public float healthAmount = 100f;
    // Start is called before the first frame update
    void Start()
    {
        Debug.Log("HealthManager script started!");
    }

    // Update is called once per frame
    void Update()
    {
        if(healthAmount <= 0){
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
        if(Input.GetKeyDown(KeyCode.Alpha6)){
            TakeDamage(10);
            Debug.Log("The 6 key was pressed, taking 10 damage");
        }
        if(Input.GetKeyDown(KeyCode.Alpha7)){
            Heal(5);
            Debug.Log("The 7 key was pressed, healing 5 health points");
        }
    }

    public void TakeDamage(float damage){
        healthAmount -= damage;
        healthBar.fillAmount = healthAmount / 100f;
    }

    public void Heal(float healingAmount){
        healthAmount += healingAmount;
        healthAmount = Mathf.Clamp(healthAmount, 0, 199);
        healthBar.fillAmount = healthAmount / 100f;
    }
}
