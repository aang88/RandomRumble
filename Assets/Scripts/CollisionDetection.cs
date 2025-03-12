using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

public class CollisionDetection : MonoBehaviour
{
    public WeaponController wp;
    public GameObject HitParticle;
    public float Damage;
    private bool hasHit = false;
    private Transform weaponOwner;
    private float lastHitTime = 0f; // Track when we last hit
    private float hitCooldown = 0.5f; // Cooldown between hits (in seconds)
    private HashSet<Collider> hitColliders = new HashSet<Collider>(); // Track which colliders we've hit

    private void Start()
    {
        weaponOwner = wp.weaponOwner;
    }

    private void OnTriggerEnter(Collider other)
    {
        UnityEngine.Debug.Log("Gelo Trigger with: " + other.name + ", Tag: " + other.tag);
        
        // First verify that wp exists
        if (wp == null)
        {
            UnityEngine.Debug.LogError("WeaponController reference is missing on CollisionDetection");
            return;
        }
       
        // Check if we can hit based on cooldown AND make sure we haven't hit this specific collider
        if (other.tag == "Enemy" && wp.isAttacking && !hasHit && 
            Time.time > lastHitTime + hitCooldown && !hitColliders.Contains(other))
        {
            // UnityEngine.Debug.Log("Trigger with: " + other.name + ", Tag: " + other.tag);
            if (other.transform == weaponOwner || other.transform.IsChildOf(weaponOwner))
            {
                UnityEngine.Debug.Log("Same Trigger with: " + other.name + ", Tag: " + other.tag);
                return;
            }
            
            hasHit = true;
            lastHitTime = Time.time; // Record this hit time
            hitColliders.Add(other); // Record this collider as hit
            
            // Get the Animator component safely
            Animator otherAnim = other.GetComponent<Animator>();
            if (otherAnim != null)
            {
                otherAnim.SetTrigger("Hit");
            }
            
            // Safely get the Entity component
            Entity enemy = null;
            if (other.TryGetComponent(out enemy))
            {
                // Only call takeDamage if enemy is not null
                enemy.takeDamage(Damage);
                
                if (enemy.Parried)
                {
                    // DisableWeapon();
                    enemy.Parried = false;
                }
            }
            else
            {
                UnityEngine.Debug.LogWarning("No Entity component found on " + other.name);
            }
            
            // Safely disable collider
            Collider col = GetComponent<Collider>();
            if (col != null)
            {
                col.enabled = false;
            }
        }
    }

    public void DisableWeapon(){
        wp.isAttacking = false;
        wp.isBlocking = false;
    }

    public void ResetHit()
    {
        hasHit = false;
        hitColliders.Clear(); // Clear the hit colliders for the next attack
        GetComponent<Collider>().enabled = true;
    }
}
