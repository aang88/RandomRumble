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


    private void Start()
    {
        weaponOwner = wp.weaponOwner;
    }

    private void OnTriggerEnter(Collider other)
    {
        UnityEngine.Debug.Log("Gelo Trigger with: " + other.name + ", Tag: " + other.tag);
        
           
        
        if (other.tag == "Enemy" && wp.isAttacking  && !hasHit)
        {
            UnityEngine.Debug.Log("Trigger with: " + other.name + ", Tag: " + other.tag);
            if (other.transform == weaponOwner || other.transform.IsChildOf(weaponOwner))
            {
                UnityEngine.Debug.Log("Same Trigger with: " + other.name + ", Tag: " + other.tag);
                return;
            }
            hasHit = true;
            Animator otherAnim = other.GetComponent<Animator>();
            if (otherAnim != null)
            {
                otherAnim.SetTrigger("Hit");
            }
            other.TryGetComponent(out Entity enemy);
            // Instantiate(HitParticle, new Vector3(other.transform.position.x, 
            //     transform.position.y, other.transform.position.z), 
            //     other.transform.rotation);
            

            enemy.takeDamage(Damage);

            if (enemy.Parried)
            {
                // DisableWeapon();
                enemy.Parried = false;
            }

            GetComponent<Collider>().enabled = false;


        }
    }

    public void DisableWeapon(){
        wp.isAttacking = false;

        wp.isBlocking = false;
      
    }

    public void ResetHit()
    {
        hasHit = false;
        GetComponent<Collider>().enabled = true;
    }
}
