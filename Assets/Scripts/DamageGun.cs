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
    private NetworkObject playerNetworkObject;

    private Entity ownerEntity;

    private void Start()
    {
        FindPlayerReferences(); 
    }

    private void FindPlayerReferences()
    {
        // Find player Entity in parent hierarchy
        ownerEntity = GetComponentInParent<Entity>();
        
        if (ownerEntity == null)
        {
            UnityEngine.Debug.LogError($"Gun {gameObject.name} couldn't find Entity in parent hierarchy!");
            return;
        }
        
        // Get the player's NetworkObject
        playerNetworkObject = ownerEntity.GetComponent<NetworkObject>();
        
        if (playerNetworkObject == null)
        {
            UnityEngine.Debug.LogError("Player Entity doesn't have a NetworkObject component!");
            return;
        }
        
        // Try to find camera
        if (playerCamera == null)
        {
            // Try to get from entity first
            if (ownerEntity.camera != null)
            {
                playerCamera = ownerEntity.camera.transform;
                UnityEngine.Debug.Log($"Using entity's camera: {playerCamera.name}");
            }
            else
            {
                // Try hierarchy search
                playerCamera = ownerEntity.GetComponentInChildren<Camera>()?.transform;
                
                if (playerCamera == null && Camera.main != null)
                {
                    playerCamera = Camera.main.transform;
                    UnityEngine.Debug.Log("Using main camera as fallback");
                }
            }
        }
        
        UnityEngine.Debug.Log($"Gun initialized on {ownerEntity.name}, IsOwner: {playerNetworkObject.IsOwner}");
    }

    public void Shoot()
    {
        if (ownerEntity == null || playerNetworkObject == null)
        {
            FindPlayerReferences();
            
            // If still null, we can't proceed
            if (ownerEntity == null || playerNetworkObject == null)
            {
                UnityEngine.Debug.LogError("Cannot shoot - missing player references!");
                return;
            }
        }

        if (!playerNetworkObject.IsOwner)
        {
            UnityEngine.Debug.Log("Not the owner of the player, cannot shoot.");
            return;
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
                UnityEngine.Debug.Log($"Gun parent: {transform.parent?.name ?? "No parent"}");
                UnityEngine.Debug.Log($"Gun GameObject: {gameObject.name}");

            }
            else
            {
                UnityEngine.Debug.Log("Found camera: " + playerCamera.name);
            }
        }
        
        Ray gunRay = new Ray(playerCamera.position, playerCamera.forward);
        UnityEngine.Debug.DrawRay(playerCamera.position, playerCamera.forward * bulletRange, Color.red, 1f);
        UnityEngine.Debug.Log("Today is Friday in CALIFORNIA!");
        if (Physics.Raycast(gunRay, out RaycastHit hitInfo, bulletRange))
        {
            UnityEngine.Debug.Log("Fire!");
            Entity entityToDamage = hitInfo.collider.GetComponentInParent<Entity>();
            if (entityToDamage != null)
            {
                NetworkObject networkObj = entityToDamage.GetComponent<NetworkObject>();
                if (networkObj != null)
                {
                    UnityEngine.Debug.Log("Hit Entity: " + entityToDamage.name);
                    UnityEngine.Debug.Log("Hit!");  
                    ownerEntity.RequestHitEntityServerRpc(networkObj, damage);
                }
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