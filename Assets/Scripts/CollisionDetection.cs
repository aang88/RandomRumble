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
    private float colliderActiveRadius = 0.25f; // Used for additional overlap checks

    private void Start()
    {
        if (wp == null)
        {
            wp = GetComponentInParent<WeaponController>();
            if (wp == null)
            {
                UnityEngine.Debug.LogError("WeaponController (wp) is not assigned and could not be found in parent.");
                return;
            }
        }

        weaponOwner = wp.weaponOwner;
    }

    private void OnTriggerEnter(Collider other)
    {
        CheckForHit(other);
    }
    
    private void OnTriggerStay(Collider other)
    {
        // Also check hits during stay - helps with fast moving weapons
        CheckForHit(other);
    }

    private void Update()
    {
        // Additional hit detection using manual sphere overlap
        // This helps catch hits that might be missed by standard collision detection
        if (wp != null && wp.isAttacking && !hasHit && Time.time > lastHitTime + hitCooldown)
        {
            PerformAdditionalHitDetection();
        }
    }
    
    private void PerformAdditionalHitDetection()
    {
        // Use sphere overlap for additional hit detection
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, colliderActiveRadius);
        foreach (Collider col in hitColliders)
        {
            // Skip if not a valid target
            if (!col.CompareTag("Enemy") && !col.CompareTag("Player"))
                continue;
                
            // Skip if it's our own collider or parent
            if (col.transform == weaponOwner || col.transform.IsChildOf(weaponOwner))
                continue;
                
            // Process as a hit
            CheckForHit(col);
        }
    }
    
    private void CheckForHit(Collider other)
    {
        // Skip processing if weapon is inactive or already hit something
        if (wp == null || !wp.isAttacking || hasHit || Time.time <= lastHitTime + hitCooldown)
            return;

        NetworkBehaviour ownerNetBehaviour = weaponOwner?.GetComponent<NetworkBehaviour>();
        bool isLocallyControlled = ownerNetBehaviour != null && ownerNetBehaviour.IsOwner;

        // Only register hits from locally controlled players
        if ((other.CompareTag("Enemy") || other.CompareTag("Player")) && 
            isLocallyControlled && 
            !this.hitColliders.Contains(other))
        {
            // Skip if hitting self
            if (other.transform == weaponOwner || other.transform.IsChildOf(weaponOwner))
            {
                return;
            }
            
            // Valid hit detected
            this.hitColliders.Add(other);
            hasHit = true;
            lastHitTime = Time.time;
            
            UnityEngine.Debug.Log("VALID HIT DETECTED on: " + other.name);
            
            // Try to get the entity to damage
            Entity entityToDamage = other.GetComponentInParent<Entity>();
            if (entityToDamage != null)
            {
                UnityEngine.Debug.Log("Found entity to damage: " + entityToDamage.name);

                // Apply damage through network - find the NetworkObject first
                NetworkObject networkObj = entityToDamage.GetComponent<NetworkObject>();
                if (networkObj != null)
                {
                    // Get the player's Entity component to call the RPC
                    Entity playerEntity = weaponOwner.GetComponent<Entity>();
                    if (playerEntity != null)
                    {
                        UnityEngine.Debug.Log("Calling RequestHitEntityServerRpc with damage: " + Damage);
                        playerEntity.RequestHitEntityServerRpc(networkObj, Damage);
                        
                        // Visual feedback
                        if (HitParticle != null)
                        {
                            Instantiate(HitParticle, other.ClosestPoint(transform.position), Quaternion.identity);
                        }
                    }
                }
            }
        }
    }

    public void DisableWeapon()
    {
        wp.isAttacking = false;
        ResetHit();
    }

    public void ResetHit()
    {
        hasHit = false;
        hitColliders.Clear();
    }
    
    // For debugging hit detection regions
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, colliderActiveRadius);
    }
}
