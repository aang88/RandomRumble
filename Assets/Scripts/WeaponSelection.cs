using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using FishNet.Object;
using FishNet.Connection;
using FishNet.Object.Synchronizing;
using FishNet.Component.Transforming;
public class WeaponSelection : NetworkBehaviour
{
    public List<GameObject> MeeleWeapons = new List<GameObject>();
    public List<GameObject> RangedWeapons = new List<GameObject>();
    public List<GameObject> MiscWeapons = new List<GameObject>();
    private GameObject[] selections = new GameObject[3];

    public GameObject buttonPrefab; 
    public Transform buttonParent; 

    public Transform weaponHolder;

    private GameObject[] PossibleMeeles = new GameObject[3];
    private GameObject[] PossibleGuns = new GameObject[3];
    private GameObject[] PossibleMiscs = new GameObject[3];

    private List<GameObject> OriginalMeeleWeapons;
    private List<GameObject> OriginalRangedWeapons;
    private List<GameObject> OriginalMiscWeapons;
    // Start is called before the first frame update
    public override void OnStartClient()
    {
        Debug.Log($"WeaponSelection OnStartClient. IsOwner: {IsOwner}, GameObject: {gameObject.name}, InstanceID: {GetInstanceID()}");
        if (buttonPrefab == null)
        {
            Debug.LogError($"ButtonPrefab is not assigned in WeaponSelection on {gameObject.name}, InstanceID: {GetInstanceID()}");
        }
        else
        {
            Debug.Log($"ButtonPrefab is assigned: {buttonPrefab.name} on {gameObject.name}, InstanceID: {GetInstanceID()}");
        }
        base.OnStartClient();
        Debug.Log($"WeaponSelection OnStartClient. IsOwner: {IsOwner}");
    }

   

    // Update is called once per frame
    void Update()
    {
        
    }

    public void PickRandomWeaponPool(){
        // Randomly select 3 weapons from the list
        Debug.Log("PickRandomWeaponPool called.");
        Debug.Log($"MeeleWeapons count: {MeeleWeapons.Count}");
        Debug.Log($"RangedWeapons count: {RangedWeapons.Count}");
        Debug.Log($"MiscWeapons count: {MiscWeapons.Count}");
        for (int i = 0; i < 2; i++)
        {
            PickRandomWeapon(ref MeeleWeapons, ref PossibleMeeles, i);
            PickRandomWeapon(ref RangedWeapons, ref PossibleGuns, i);
            PickRandomWeapon(ref MiscWeapons, ref PossibleMiscs, i);
        }

        CreateWeaponButtons(PossibleMeeles, "melee");
        CreateWeaponButtons(PossibleGuns, "ranged");
        CreateWeaponButtons(PossibleMiscs, "misc");
    }

    public void PickRandomWeapon(ref List<GameObject> weaponPool,ref GameObject[] weaponSelecitons, int iteration){
        if (weaponPool == null || weaponPool.Count == 0)
        {
            Debug.LogError("Weapon pool is empty or null.");
            return;
        }
        int randomIndex = Random.Range(0, weaponPool.Count);
        weaponSelecitons[iteration] = weaponPool[randomIndex];
        weaponPool.RemoveAt(randomIndex); // Remove to avoid duplicates
        
    }

     private void CreateWeaponButtons(GameObject[] weaponSelections, string category)
    {
       
        foreach (var weapon in weaponSelections)
        {
             Debug.Log("weapon: "+ weapon + "category: " + category);
            if (weapon == null) continue;
            Debug.Log($"Button instantiated for weapon: {weapon.name}, Category: {category}");


            // Instantiate a button
            GameObject button = Instantiate(buttonPrefab, buttonParent);

            // Set the button's text to the weapon's name
            button.GetComponentInChildren<Text>().text = weapon.name;

            // // Add a click event to the button
            button.GetComponent<Button>().onClick.AddListener(() => SelectWeapon(weapon,category));
        }
    }

    private void SelectWeapon(GameObject weapon,string category)
    {
        switch (category){
            case "melee":
                selections[1] = weapon;
                break;
            case "ranged":  
                selections[0] = weapon;
                break;
            case "misc":    
                selections[2] = weapon;
                break;
        }
    }

    private void ConfirmSelection()
    {
        foreach (var weapon in selections)
        {
            if (weapon == null)
            {
                Debug.LogWarning("Not all weapons have been selected!");
                return;
            }
        }

        SubmitWeaponSelectionServerRpc(selections);


        for (int i = 0; i < selections.Length; i++)
        {
            GameObject weaponInstance = Instantiate(selections[i], weaponHolder);
            weaponInstance.transform.localPosition = Vector3.zero; // Adjust position if needed
            weaponInstance.transform.localRotation = Quaternion.identity; // Adjust rotation if needed
        }

        Debug.Log("Weapons successfully attached to the player's weaponHolder.");
    }

    public void AssignWeapons(GameObject[] selectedWeapons)
    {
        foreach (var weapon in selectedWeapons)
        {
            GameObject weaponInstance = Instantiate(weapon, weaponHolder);
            weaponInstance.transform.localPosition = Vector3.zero;
            weaponInstance.transform.localRotation = Quaternion.identity;
        }

        Debug.Log("Weapons successfully assigned to the player's weaponHolder.");
    }

    [ServerRpc(RequireOwnership = false)]
    private void SubmitWeaponSelectionServerRpc(GameObject[] selectedWeapons, NetworkConnection sender = null)
    {
        GameStateManager.Instance.StorePlayerWeapons(sender, selectedWeapons);
    }

    private void ResetWeaponPool(List<GameObject> otherPlayerWeapons)
    {
        MeeleWeapons = new List<GameObject>(OriginalMeeleWeapons);
        RangedWeapons = new List<GameObject>(OriginalRangedWeapons);
        MiscWeapons = new List<GameObject>(OriginalMiscWeapons);

        // Remove weapons already attached to the other player
        foreach (var weapon in otherPlayerWeapons)
        {
            MeeleWeapons.Remove(weapon);
            RangedWeapons.Remove(weapon);
            MiscWeapons.Remove(weapon);
        }

        Debug.Log("Weapon pools have been reset for re-picking, excluding other player's weapons.");
    }
}
