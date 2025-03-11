using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

public class Entity : MonoBehaviour
{
    [SerializeField]
     public float StartingHealth;
    private float health;

    private int stockCount;


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
            UnityEngine.Debug.Log("DAMAGE BLOCKED!");
        }
        else if (weaponController.IsBlocking() && weaponController.SuccessfulParry())
        {
            UnityEngine.Debug.Log("PARRY!");
        }
        else
        {
            Health -= damage;
            UnityEngine.Debug.Log("FULL DAMAGE!");
        }
    }

    void Start()
    {
        Health = StartingHealth;
    }
}