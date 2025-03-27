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
        // Apply damage on the server
        TakeDamage(damage);
        
        // Make sure to update GameStateManager
        GameStateManager.Instance.SyncPlayerValues();
    }

    // Add this method to your Entity class:
    [ServerRpc(RequireOwnership = false)]
    public void RequestHitEntityServerRpc(NetworkObject targetEntity, float damageAmount)
    {
        UnityEngine.Debug.Log($"Server received hit request. Target: {(targetEntity ? targetEntity.name : "null")}, Damage: {damageAmount}");
        
        // Validate the target entity still exists
        if (targetEntity != null && targetEntity.IsSpawned)
        {
            // Get the Entity component from the NetworkObject
            Entity entityToDamage = targetEntity.GetComponent<Entity>();
            if (entityToDamage != null)
            {
                UnityEngine.Debug.Log($"Applying damage {damageAmount} to {entityToDamage.name}");
                
                // Apply damage on the server
                entityToDamage.TakeDamage(damageAmount);
                
                // Update GameStateManager to sync player values
                if (GameStateManager.Instance != null)
                {
                    GameStateManager.Instance.SyncPlayerValues();
                    
                    // Broadcast hit to clients
                    NotifyHitObserversRpc(targetEntity, damageAmount);
                }
                else
                {
                    UnityEngine.Debug.LogError("GameStateManager.Instance is null!");
                }
            }
        }
    }

    [ObserversRpc]
    private void NotifyHitObserversRpc(NetworkObject hitEntity, float damage)
    {
        // Only apply effects if this is the hit entity
        if (hitEntity == NetworkObject)
        {
            UnityEngine.Debug.Log($"I was hit for {damage} damage!");
            
            // Apply visual/audio feedback here
            if (camera != null)
            {
                CameraEffects cameraEffects = camera.GetComponent<CameraEffects>();
                if (cameraEffects != null)
                {
                    cameraEffects.TriggerScreenShake();
                    cameraEffects.TriggerFlash();
                }
            }
        }
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
