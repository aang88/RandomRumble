using System.Collections.Generic;
using System.Diagnostics;
using FishNet.Object;
using UnityEngine;

public class Entity : NetworkBehaviour
{
    [SerializeField]
    public float StartingHealth;
    private float health;

    public GameObject floatingTextPrefab;

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

    public void Update(){
    //   UnityEngine.Debug.Log("IsBlocking" + weaponController.IsBlocking());
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
    public void RpcToggleCanvas(bool isPlayer1, bool isVisible)
    {
        UnityEngine.Debug.Log("Canvas toggle called ");
        // Find the Canvas component in the player entity
        Canvas playerCanvas = GetComponentInChildren<Canvas>();
        if (playerCanvas == null)
        {
            UnityEngine.Debug.LogWarning("Canvas not found for " + gameObject.name);
        }
        // Set the canvas active or inactive based on visibility
        if (playerCanvas != null)
        {
            UnityEngine.Debug.Log("Toggled it");
            playerCanvas.gameObject.SetActive(isVisible);
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


    [ObserversRpc]
    private void ShowDamageTextObserversRpc(float damageAmount, Vector3 position)
    {
        // Create the floating text object
        if (floatingTextPrefab != null)
        {
            // Instantiate at position slightly above the entity
            Vector3 spawnPosition = transform.position + Vector3.up * 2f;
            
            // Create the floating text game object
            GameObject floatingTextObj = Instantiate(floatingTextPrefab, spawnPosition, Quaternion.identity);
            
            // Get the TextMeshPro component
            TMPro.TextMeshPro textMesh = floatingTextObj.GetComponent<TMPro.TextMeshPro>();
            FloatingText floatingText = floatingTextObj.GetComponent<FloatingText>();
            if (textMesh != null)
            {
                // Format the damage text (e.g. "-25")
                textMesh.text = "-" + damageAmount.ToString("0");
                
            }

            if (floatingText != null)
            {
                // Pass the local camera - on each client this will be their own camera
                floatingText.playerCam = camera;
            }
            
            // Destroy the text after some time
            Destroy(floatingTextObj, 2f);
        }
    }

    public void TakeDamage(float damage)
    {
        if (weaponController == null)
        {
            UnityEngine.Debug.LogError("weaponController is null on " + gameObject.name);
            Health -= damage;
            ShowDamageTextObserversRpc(damage, transform.position);
            return;
        }

        if (IsServer) // Ensure this logic runs only on the server
        {
            UnityEngine.Debug.Log("TAKING DAMAGE");

            if (weaponController.isBlocking.Value)
            {
                if (weaponController.SuccessfulParry())
                {
                    parried = true;
                    UnityEngine.Debug.Log("HIT: PARRY!");
                    NotifyParryResult(true);
                }
                else
                {
                    Health -= damage / 2;
                    UnityEngine.Debug.Log("HIT: DAMAGE BLOCKED!");
                    ShowDamageTextObserversRpc(damage, transform.position);
                }
            }
            else
            {
                Health -= damage;
                UnityEngine.Debug.Log("HIT: FULL DAMAGE!");
                NotifyParryResult(false);
                ShowDamageTextObserversRpc(damage, transform.position);
            }
        }
    }

    [ObserversRpc]
    private void NotifyParryResult(bool isParrySuccessful)
    {
        if (isParrySuccessful)
        {
            UnityEngine.Debug.Log("Parry succeed on the client.");
            ParryEffects();
        }
        else
        {
            UnityEngine.Debug.Log("Parry failed on the client.");
            // Handle failed parry logic
        }
    }
    public void ParryEffects()
    {
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

    [ServerRpc(RequireOwnership = false)]
    public void SyncVisualEffectsServerRpc(Vector3 hitPoint, Vector3 muzzlePosition, NetworkObject gunObject)
    {
        SyncVisualEffectsObserversRpc(hitPoint, muzzlePosition, gunObject);
    }

    [ObserversRpc]
    private void SyncVisualEffectsObserversRpc(Vector3 hitPoint, Vector3 muzzlePosition, NetworkObject gunObject)
    {
        // Don't show effects on the client who fired
        if (IsOwner) return;

        if (gunObject != null)
        {
            DamageGun gun = gunObject.GetComponent<DamageGun>();
            if (gun != null)
            {
                gun.CreateBulletTracerOnClient(muzzlePosition, hitPoint);
            }
        }
    }

    void Start()
    {
        Health = StartingHealth;
        UnityEngine.Debug.Log("Entity Start() called: " + gameObject.name + " - StartingHealth: " + StartingHealth + " - Health: " + Health);
    }
}
