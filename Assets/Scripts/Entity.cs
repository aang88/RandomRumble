using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

public class Entity : MonoBehaviour
{
    [SerializeField] private float StartingHealth;
    private float health;

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

            if (health <= 0f)
            {
                Destroy(gameObject);
            }
        }
    }

    void Start()
    {
        Health = StartingHealth;
    }
}