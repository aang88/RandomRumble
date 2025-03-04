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

    void Start()
    {
        Health = StartingHealth;
    }
}