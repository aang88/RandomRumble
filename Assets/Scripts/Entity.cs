using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

public class Entity : MonoBehaviour
{
    [SerializeField]
     public float StartingHealth;
    private float health;

    private bool parried;

    public Camera camera;

    private int stockCount;
    public Freezer freezer;


    public WeaponController weaponController;

    public float Health
    {
        get
        {
            return health;
        }
        set
        {
            health = value;
            UnityEngine.Debug.Log(health);
        }
    }


    public bool Parried
    {
        get
        {
            return parried;
        }
        set
        {
            parried = value;
        }
    }

    public int Stocks{
        get
        {
            return stockCount;
        }
        set
        {
            stockCount = value;
            UnityEngine.Debug.Log(stockCount);
        }
    }

    public void takeDamage(float damage)
    {
        // First check if weaponController exists
        if (weaponController == null)
        {
            UnityEngine.Debug.LogError("weaponController is null on " + gameObject.name);
            Health -= damage;
            return;
        }

        try
        {
            if (weaponController.IsBlocking() && !weaponController.SuccessfulParry())
            {
                Health -= damage / 2;
                UnityEngine.Debug.Log("HIT: DAMAGE BLOCKED!");
            }
            else if (weaponController.IsBlocking() && weaponController.SuccessfulParry())
            {
                parried = true;
                UnityEngine.Debug.Log("HIT: PARRY!");
                
                // Check if freezer exists before using it
                if (freezer != null)
                {
                    StartCoroutine(freezer.Freeze());
                }
                else
                {
                    UnityEngine.Debug.LogWarning("Freezer component is missing on " + gameObject.name);
                }
                
                // Safely get camera effects

                if (camera != null)
                {
                    CameraEffects cameraEffects = camera.GetComponent<CameraEffects>();
                    if (cameraEffects != null)
                    {
                        cameraEffects.TriggerScreenShake();
                        cameraEffects.TriggerFlash();
                    }
                    else
                    {
                        UnityEngine.Debug.LogWarning("CameraEffects component is missing on main camera");
                    }
                }
                else
                {
                    UnityEngine.Debug.LogWarning("Main camera not found");
                }
            }
            else
            {
                Health -= damage;
                UnityEngine.Debug.Log("HIT: FULL DAMAGE!");
            }
        }
        catch (System.Exception e)
        {
            UnityEngine.Debug.LogError("Error in takeDamage: " + e.Message);
            // Default damage if exception
            Health -= damage;
        }
    }

    void Start()
    {
        Health = StartingHealth;
    }
}