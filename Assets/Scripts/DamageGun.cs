using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;



public class DamageGun : MonoBehaviour
{
    public float damage;
    public float bulletRange;
    public Transform playerCamera;
    // Start is called before the first frame update
   
    // Update is called once per frame
    public void Shoot()
    {
        UnityEngine.Debug.Log("Readu to go!");
        Ray gunRay = new Ray(playerCamera.position, playerCamera.forward);

        if (Physics.Raycast(gunRay,out RaycastHit hitInfo, bulletRange))
        {
            UnityEngine.Debug.Log("Fire!");
            if (hitInfo.collider.gameObject.TryGetComponent(out Entity enemy))
            {
                UnityEngine.Debug.Log("Hit!");
                enemy.takeDamage(damage);
            }
        }
    }

    
}
