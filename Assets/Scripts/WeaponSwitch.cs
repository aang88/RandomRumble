using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using FishNet.Connection;

public class WeaponSwitch : NetworkBehaviour
{
    [Header("References")]
    [SerializeField] private Transform[] weapons;

    [Header("Keys")]
    [SerializeField] private KeyCode[] keys;

    [Header("Settings")]
    [SerializeField] private float switchTime;

    // Track selected weapon across network using SyncVar
    private readonly SyncVar<int> _selectedWeapon = new SyncVar<int>();
    
    private float timeSinceLastSwitch;
    public bool weaponsSet = false;
    
    private void Awake()
    {
        // Register callback for when _selectedWeapon changes
        _selectedWeapon.OnChange += OnSelectedWeaponChanged;
    }
    
    private void OnDestroy()
    {
        // Unregister callback when object is destroyed
        _selectedWeapon.OnChange -= OnSelectedWeaponChanged;
    }
    
    // Callback for when the _selectedWeapon SyncVar changes
    private void OnSelectedWeaponChanged(int prev, int next, bool asServer)
    {
        Debug.Log($"Weapon changed from {prev} to {next} on {(asServer ? "server" : "client")}");
        
        // Ensure weapons are set before trying to select one
        if (!weaponsSet && transform.childCount > 0)
        {
            SetWeapons();
        }
        
        // Select the weapon on both server and clients
        Select(next);
    }
    
    public override void OnStartClient()
    {
        base.OnStartClient();
        
        // Initialize weapons array for this client
        SetWeapons();
        
        // Make sure to select the current weapon based on the SyncVar value
        // that might have been set before this client connected
        Select(_selectedWeapon.Value);
    }
    
    public override void OnStartNetwork()
    {
        base.OnStartNetwork();
        
        // Make sure weapons are set when object is spawned on the network
        if (!weaponsSet && transform.childCount > 0)
        {
            SetWeapons();
        }
    }
    
    private void SetWeapons()
    {
        weapons = new Transform[transform.childCount];

        for(int i = 0; i < transform.childCount; i++)
        {
            weapons[i] = transform.GetChild(i);
        }
        
        if (keys == null) keys = new KeyCode[weapons.Length];
        if(transform.childCount > 0){
            weaponsSet = true;
        }
        
        // Select the current weapon based on SyncVar
        Select(_selectedWeapon.Value);
    }

    private void Update()
    {
        if (!IsOwner) return;

        if(!weaponsSet && transform.childCount > 0){
            SetWeapons();
        }
        
        for(int i = 0; i < keys.Length; i++)
        {
            if (Input.GetKeyDown(keys[i]) && timeSinceLastSwitch >= switchTime)
            {
                SwitchWeaponServerRpc(i);
                break;
            }
        }

        timeSinceLastSwitch += Time.deltaTime;
    }
    
    private void Select(int weaponIndex)
    {
        if (weapons == null || weapons.Length == 0)
        {
            Debug.LogWarning("No weapons available to select.");
            return;
        }
        
        for(int i = 0; i < weapons.Length; i++)
        {
            weapons[i].gameObject.SetActive(i == weaponIndex);
        }

        timeSinceLastSwitch = 0f;
    }

    [ServerRpc]
    private void SwitchWeaponServerRpc(int weaponIndex)
    {
        // Update the SyncVar on the server, which will automatically sync to clients
        _selectedWeapon.Value = weaponIndex;
        Select(_selectedWeapon.Value);
        Debug.Log($"Server: Changed weapon to index {weaponIndex}");
    }
}