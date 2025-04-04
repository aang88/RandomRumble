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
    private bool weaponsSet = false;
    
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
        if (!asServer) // Only select on clients
        {
            Select(next);
        }
    }
    
    public override void OnStartClient()
    {
        base.OnStartClient();
        SetWeapons();
        
        // No need to request current weapon as SyncVar will automatically synchronize
    }
    
    private void SetWeapons()
    {
        weapons = new Transform[transform.childCount];

        for(int i = 0; i < transform.childCount; i++)
        {
            weapons[i] = transform.GetChild(i);
        }
        
        if (keys == null) keys = new KeyCode[weapons.Length];
        if(transform.childCount==3){
            weaponsSet = true;
        }
        
        // Select the current weapon based on SyncVar
        Select(_selectedWeapon.Value);
    }

    private void Update()
    {
        if (!IsOwner) return;

        if(!weaponsSet){
            SetWeapons();
        }
        
        int previousSelectedWeapon = _selectedWeapon.Value;

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
        Debug.Log($"Server: Changed weapon to index {weaponIndex}");
    }
}