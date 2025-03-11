using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

public class Entity : MonoBehaviour
{
    [SerializeField]
     public float StartingHealth;
    private float health;

    private bool parried;

    public Camera camera;

    private int stockCount;
    public Freezer freezer;


    public WeaponController weaponController;

    public float Health
    {
        get
        {
            return health;
        }
        set
        {
            health = value;
            UnityEngine.Debug.Log(health);
        }
    }


    public bool Parried
    {
        get
        {
            return parried;
        }
        set
        {
            parried = value;
        }
    }

    public int Stocks{
        get
        {
            return stockCount;
        }
        set
        {
            stockCount = value;
            UnityEngine.Debug.Log(stockCount);
        }
    }

    public void takeDamage(float damage)
    {
        if (weaponController.IsBlocking() && !weaponController.SuccessfulParry())
        {
            Health -= damage / 2;
            UnityEngine.Debug.Log("HIT: DAMAGE BLOCKED!");
        }
        else if (weaponController.IsBlocking() && weaponController.SuccessfulParry())
        {
            parried = true;
            UnityEngine.Debug.Log("HIT: PARRY!");
            StartCoroutine(freezer.Freeze());
            CameraEffects cameraEffects = Camera.main.GetComponent<CameraEffects>();
            if (cameraEffects != null)
            {
                cameraEffects.TriggerScreenShake();
            }
            // UnityEngine.Debug.Log("HIT: PARRY!");
        }
        else
        {
            Health -= damage;
            UnityEngine.Debug.Log("HIT:  FULL DAMAGE!");
        }
    }

    void Start()
    {
        Health = StartingHealth;
    }
}