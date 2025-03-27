using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using FishNet.Object;
using FishNet.Object.Synchronizing;

public class CollisionDetection : MonoBehaviour
{
    public WeaponController wp;
    public GameObject HitParticle;
    public float Damage;
    private bool hasHit = false;
    private Transform weaponOwner;
    private float lastHitTime = 0f;
    private float hitCooldown = 0.5f;
    private HashSet<Collider> hitColliders = new HashSet<Collider>();

    private void Start()
    {
        weaponOwner = wp.weaponOwner;
    }

    private void OnTriggerEnter(Collider other)
    {
        UnityEngine.Debug.Log("Weapon Trigger with: " + other.name + ", Tag: " + other.tag);

        if (wp == null)
        {
            UnityEngine.Debug.LogError("WeaponController reference is missing on CollisionDetection");
            return;
        }

        NetworkBehaviour ownerNetBehaviour = weaponOwner?.GetComponent<NetworkBehaviour>();
        bool isLocallyControlled = ownerNetBehaviour != null && ownerNetBehaviour.IsOwner;

        // Only register hits from locally controlled players
        if (other.CompareTag("Enemy") && wp.isAttacking && !hasHit &&
            Time.time > lastHitTime + hitCooldown && !hitColliders.Contains(other) &&
            isLocallyControlled)
        {
            // Skip if hitting self
            if (other.transform == weaponOwner || other.transform.IsChildOf(weaponOwner))
            {
                UnityEngine.Debug.Log("Skipping hit on self: " + other.name);
                return;
            }
            
            // Valid hit - mark it and apply damage through network
            hitColliders.Add(other);
            hasHit = true;
            lastHitTime = Time.time;
            
            // Try to get the entity to damage
            Entity entityToDamage = other.GetComponentInParent<Entity>();
            if (entityToDamage != null)
            {
                // Apply damage through network - find the NetworkObject first
                NetworkObject networkObj = entityToDamage.GetComponent<NetworkObject>();
                if (networkObj != null)
                {
                    // Get the player's NetworkBehaviour component to call the RPC
                    NetworkBehaviour playerNetworkComponent = weaponOwner.GetComponent<NetworkBehaviour>();
                    if (playerNetworkComponent != null)
                    {
                        // Call through a method that will use the ServerRpc
                        Entity playerEntity = weaponOwner.GetComponent<Entity>();
                        if (playerEntity != null)
                        {
                            playerEntity.RequestHitEntityServerRpc(networkObj, Damage);
                        }
                    }
                }

                // Spawn hit effect locally (all clients will handle their own effects)
                Vector3 hitPoint = other.ClosestPointOnBounds(transform.position);
                if (HitParticle != null)
                {
                    GameObject hitEffect = GameObject.Instantiate(HitParticle, hitPoint, Quaternion.identity);
                    GameObject.Destroy(hitEffect, 1f);
                }
            }
        }
    }

    public void DisableWeapon()
    {
        gameObject.SetActive(false);
    }

    public void ResetHit()
    {
        hasHit = false;
        hitColliders.Clear();
    }
}
