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

    public void Shoot()
    {
        if (!IsOwner) return; // Only the owner can shoot

        Ray gunRay = new Ray(playerCamera.position, playerCamera.forward);
        UnityEngine.Debug.DrawRay(playerCamera.position, playerCamera.forward * bulletRange, Color.red, 1f);
        if (Physics.Raycast(gunRay, out RaycastHit hitInfo, bulletRange))
        {
            UnityEngine.Debug.Log("Fire!");
            Entity entity = hitInfo.collider.GetComponentInParent<Entity>();

            if (entity != null)
            {
                UnityEngine.Debug.Log("Hit Entity: " + entity.name);

                UnityEngine.Debug.Log("Hit!");  
                RequestDamageServerRpc(entity.NetworkObject, damage);
            }
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void RequestDamageServerRpc(NetworkObject enemyObject, float damage)
    {
        if (enemyObject != null)
        {
            Entity enemyEntity = enemyObject.GetComponent<Entity>();
            if (enemyEntity != null)
            {
                enemyEntity.TakeDamage(damage);
            }
        }
    }
}