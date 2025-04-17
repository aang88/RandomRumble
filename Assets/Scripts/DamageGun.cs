using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using FishNet.Object;
using FishNet.Object.Synchronizing; 


public class DamageGun : NetworkBehaviour
{
    public float damage;
    public float bulletRange;
    public Transform playerCamera;

    private Entity ownerEntity;

    private void Start()
    {
        // Auto-find if not set manually
        if (ownerEntity == null)
        {
            // Try to find the owner entity in the parent hierarchy
            ownerEntity = GetComponentInParent<Entity>();
            if (ownerEntity == null)
            {
                UnityEngine.Debug.LogError("Owner Entity not assigned and could not be found in parent.");
                return;
            }
        }
        if (playerCamera == null)
        {
            // Try to get camera from parent hierarchy
            playerCamera = transform.root.GetComponentInChildren<Camera>()?.transform;
            
            // If there's an entity owner, try to use its camera
            if (playerCamera == null && ownerEntity != null)
            {
                playerCamera = ownerEntity.camera?.transform;
            }
            
            if (playerCamera == null)
            {
                UnityEngine.Debug.LogError("Player camera not assigned and couldn't be found automatically!");
            }
            else
            {
                UnityEngine.Debug.Log("Found camera: " + playerCamera.name);
            }
        }
    }

    public void Shoot()
    {
        
        Ray gunRay = new Ray(playerCamera.position, playerCamera.forward);
        UnityEngine.Debug.DrawRay(playerCamera.position, playerCamera.forward * bulletRange, Color.red, 1f);
        if (Physics.Raycast(gunRay, out RaycastHit hitInfo, bulletRange))
        {
            UnityEngine.Debug.Log("Fire!");
            Entity entityToDamage = hitInfo.collider.GetComponentInParent<Entity>();
            NetworkObject networkObj = entityToDamage.GetComponent<NetworkObject>();
            if (entityToDamage != null)
            {
                UnityEngine.Debug.Log("Hit Entity: " + entityToDamage.name);

                UnityEngine.Debug.Log("Hit!");  
                ownerEntity.RequestHitEntityServerRpc(networkObj, damage);
                // RequestDamageServerRpc(entity.NetworkObject, damage);
            }
        }
    }

    // [ServerRpc(RequireOwnership = false)]
    // private void RequestDamageServerRpc(NetworkObject enemyObject, float damage)
    // {
    //     if (enemyObject != null)
    //     {
    //         Entity enemyEntity = enemyObject.GetComponent<Entity>();
    //         if (enemyEntity != null)
    //         {
    //             enemyEntity.TakeDamage(damage);
    //         }
    //     }
    // }
}