using System.Collections.Generic;
using System.Diagnostics;
using FishNet.Object;
using UnityEngine;

public class Entity : NetworkBehaviour
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
        get { return parried; }
        set { parried = value; }
    }

    public int Stocks
    {
        get { return stockCount; }
        set
        {
            stockCount = value;
            UnityEngine.Debug.Log(stockCount);
        }
    }

    // Call this from the client to apply damage on the server
    [ServerRpc(RequireOwnership = false)]
    public void TakeDamageServerRpc(float damage)
    {
        TakeDamage(damage); // Apply damage logic on the server
    }

    public void TakeDamage(float damage)
    {
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

                if (freezer != null)
                {
                    StartCoroutine(freezer.Freeze());
                }
                else
                {
                    UnityEngine.Debug.LogWarning("Freezer component is missing on " + gameObject.name);
                }

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
            Health -= damage;
        }
    }

    void Start()
    {
        Health = StartingHealth;
        UnityEngine.Debug.Log("Entity Start() called: " + gameObject.name + " - StartingHealth: " + StartingHealth + " - Health: " + Health);
    }
}
