using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FishNet.Object;
using FishNet.Connection;

public class WeaponSwitch : NetworkBehaviour
{
    [Header("References")]
    [SerializeField] private Transform[] weapons;

    [Header("Keys")]
    [SerializeField] private KeyCode[] keys;

    [Header("Settings")]
    [SerializeField] private float switchTime;

    // Track selected weapon across network
    private int selectedWeapon = 0;
    private float timeSinceLastSwitch;
    
    public override void OnStartClient()
    {
        base.OnStartClient();
        SetWeapons();
        
        // Request the current weapon from server if not the owner
        if (!IsOwner)
        {
            RequestCurrentWeaponServerRpc();
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
    }

    private void Update()
    {
        if (!IsOwner) return;
        
        int previousSelectedWeapon = selectedWeapon;

        for(int i = 0; i < keys.Length; i++)
        {
            if (Input.GetKeyDown(keys[i]) && timeSinceLastSwitch >= switchTime)
            {
                selectedWeapon = i;
            }
        }

        if (previousSelectedWeapon != selectedWeapon) 
        {
            Select(selectedWeapon);
            SwitchWeaponServerRpc(selectedWeapon);
        }

        timeSinceLastSwitch += Time.deltaTime;
    }
    
    private void Select(int weaponIndex)
    {
        for(int i = 0; i < weapons.Length; i++)
        {
            weapons[i].gameObject.SetActive(i == weaponIndex);
        }

        timeSinceLastSwitch = 0f;
    }

    [ServerRpc]
    private void SwitchWeaponServerRpc(int weaponIndex)
    {
        SyncWeaponObserversRpc(weaponIndex);
    }
    
    [ObserversRpc(ExcludeOwner = true)]
    private void SyncWeaponObserversRpc(int weaponIndex)
    {
        selectedWeapon = weaponIndex;
        Select(selectedWeapon);
    }
    
    [ServerRpc(RequireOwnership = false)]
    private void RequestCurrentWeaponServerRpc(NetworkConnection conn = null)
    {
        // Server sends current weapon selection to the requesting client
        TargetSyncWeaponRpc(conn, selectedWeapon);
    }
    
    [TargetRpc]
    private void TargetSyncWeaponRpc(NetworkConnection target, int weaponIndex)
    {
        selectedWeapon = weaponIndex;
        Select(selectedWeapon);
    }
}
