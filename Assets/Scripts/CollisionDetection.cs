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

    private void OnTriggerEnter(Collider other)
    {
        if(other.tag == "Enemy" && wp.isAttacking  && !hasHit)
        {
            UnityEngine.Debug.Log("HIT!");
            hasHit = true;
            other.GetComponent<Animator>().SetTrigger("Hit");
            other.TryGetComponent(out Entity enemy);
            Instantiate(HitParticle, new Vector3(other.transform.position.x, 
                transform.position.y, other.transform.position.z), 
                other.transform.rotation);
            
            enemy.Health -= Damage;

            GetComponent<Collider>().enabled = false;


        }
    }

    public void ResetHit()
    {
        hasHit = false;
        GetComponent<Collider>().enabled = true;
    }
}
