using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

public class CollisionDetection : MonoBehaviour
{
    public WeaponController wp;
    public GameObject HitParticle;
   
    private void OnTriggerEnter(Collider other)
    {
        if(other.tag == "Enemy" && wp.isAttacking)
        {
            UnityEngine.Debug.Log(other.name);
            other.GetComponent<Animator>().SetTrigger("Hit");
            Instantiate(HitParticle, new Vector3(other.transform.position.x, 
                transform.position.y, other.transform.position.z), 
                other.transform.rotation);


        }
    }
}
