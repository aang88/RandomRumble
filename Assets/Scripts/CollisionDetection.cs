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
            other.GetComponent<Animator>().SetTrigger("Hit");
            other.TryGetComponent(out Entity enemy);
            Instantiate(HitParticle, new Vector3(other.transform.position.x, 
                transform.position.y, other.transform.position.z), 
                other.transform.rotation);


            WeaponController enemyWeaponController = enemy.weaponController;

            if (enemyWeaponController.IsBlocking())
            {
                enemy.Health -= Damage / 2;
                UnityEngine.Debug.Log("DAMAGE BLOCKED!");
            }
            else
            {
                enemy.Health -= Damage;
                UnityEngine.Debug.Log("FULL DAMAGE!");
            }


            GetComponent<Collider>().enabled = false;


        }
    }

    public void ResetHit()
    {
        hasHit = false;
        GetComponent<Collider>().enabled = true;
    }
}
