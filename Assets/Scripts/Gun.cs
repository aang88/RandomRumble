using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.Events;

public class Gun : MonoBehaviour
{

    public UnityEvent onGunShoot;
    public float fireCooldown;
    public bool automatic;
    public float currentCooldown;
    public GameObject muzzleFlash;
    public Transform muzzleFlashPosition;
    // Start is called before the first frame update
    void Start()
    {
        currentCooldown = fireCooldown;
    }

    // Update is called once per frame
    void Update()
    {
        if (automatic)
        {
            if (Input.GetMouseButton(0))
            {
                if(currentCooldown <= 0f)
                {

                    onGunShoot?.Invoke();
                    currentCooldown = fireCooldown;
                    // Flash();
                }
            }
        }
        else
        {
            if (Input.GetMouseButtonDown(0))
            {
                if (currentCooldown <= 0f)
                {
                    onGunShoot?.Invoke();
                    currentCooldown = fireCooldown;
                    // Flash();
                }
            }
        }

        currentCooldown -= Time.deltaTime;
    }

    // void Flash()
    // {
    //     GameObject Flash = Instantiate(muzzleFlash, muzzleFlashPosition);
    //     Destroy(Flash, 0.1f);
    // }
}
