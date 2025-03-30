using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using FishNet.Object;
using FishNet.Connection;
using FishNet.Object.Synchronizing;
using FishNet.Component.Transforming;
using System.Linq;
public class WeaponSelection : NetworkBehaviour
{
    public List<GameObject> MeeleWeapons = new List<GameObject>();
    public List<GameObject> RangedWeapons = new List<GameObject>();
    public List<GameObject> MiscWeapons = new List<GameObject>();
    private GameObject[] selections = new GameObject[3];
    private Dictionary<NetworkConnection, GameObject[]> playerWeapons = new Dictionary<NetworkConnection, GameObject[]>();

    public GameObject buttonPrefab; 
    public RectTransform buttonParent; 

    public Transform weaponHolder;

    private bool weaponConfirmed = false;

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
        if (!weaponConfirmed && IsOwner)
        {
            ConfirmSelection();
        }
    }

    void Start()
    {
        // Find the buttonParent in the scene (e.g., by tag or name)
        RectTransform buttonParentInScene = GameObject.Find("ButtonParent")?.GetComponent<RectTransform>();

        // Assign the buttonParent to the WeaponSelection script
        if (buttonParentInScene != null)
        {
            SetButtonParent(buttonParentInScene);
        }
        else
        {
            Debug.LogError("ButtonParent not found in the scene!");
        }
    }

    public void SetButtonParent(RectTransform parent)
    {
        buttonParent = parent;
        Debug.Log($"buttonParent assigned dynamically: {buttonParent.name}");
    }

    public void PickRandomWeaponPool(){
        // Randomly select 3 weapons from the list

        if (!IsOwner)
        {
            Debug.LogWarning("PickRandomWeaponPool called on a non-owner client. Ignoring.");
            return;
        }
        Debug.Log("PickRandomWeaponPool called.");
        Debug.Log($"MeeleWeapons count: {MeeleWeapons.Count}");
        Debug.Log($"RangedWeapons count: {RangedWeapons.Count}");
        Debug.Log($"MiscWeapons count: {MiscWeapons.Count}");
        for (int i = 0; i < 3; i++)
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
       if (buttonParent == null)
        {
            Debug.LogError("buttonParent is not assigned! Buttons cannot be instantiated.");
            return;
        }
        foreach (var weapon in weaponSelections)
        {
            //  Debug.Log("weapon: "+ weapon + "category: " + category);
            if (weapon == null) continue;
            // Debug.Log($"Button instantiated for weapon: {weapon.name}, Category: {category}");

            // Instantiate a button
            GameObject button = Instantiate(buttonPrefab, buttonParent);

            // Set the button's text to the weapon's name
            button.GetComponentInChildren<Text>().text = weapon.name;

            // // Add a click event to the button
            Button buttonComponent = button.GetComponent<Button>();
            if (buttonComponent != null)
            {
                buttonComponent.onClick.AddListener(() => SelectWeapon(weapon, category));
                Debug.Log($"Listener added to button for weapon: {weapon.name}");
            }
            else
            {
                Debug.LogError("Button prefab is missing a Button component!");
            } 
        }
    }

    private void SelectWeapon(GameObject weapon,string category)
    {
        UnityEngine.Debug.Log("Selected weapon: " + weapon.name + " from category: " + category);
        switch (category){
            case "melee":
                UnityEngine.Debug.Log("Melee weapon selected: " + weapon.name);
                selections[1] = weapon;
                break;
            case "ranged":
                UnityEngine.Debug.Log("Ranged weapon selected: " + weapon.name);  
                selections[0] = weapon;
                break;
            case "misc":
                UnityEngine.Debug.Log("Misc weapon selected: " + weapon.name);    
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
                Debug.LogWarning("Not all weapons have been selected! Current selections: " +
                             string.Join(", ", selections.Select(w => w != null ? w.name : "null")));
                return;
            }
        }
        weaponConfirmed = true;

        Debug.Log("All weapons selected: " + string.Join(", ", selections.Select(w => w.name)));

        // Send weapon names instead of GameObject references
        string[] weaponNames = selections.Select(w => w.name).ToArray();
        SubmitWeaponSelectionServerRpc(weaponNames);

        for (int i = 0; i < selections.Length; i++)
        {
            GameObject weaponInstance = Instantiate(selections[i], weaponHolder);
            weaponInstance.transform.localPosition = Vector3.zero; // Adjust position if needed
            weaponInstance.transform.localRotation = Quaternion.identity; // Adjust rotation if needed
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // Remove or deactivate the buttonParent
        if (buttonParent != null)
        {
            Destroy(buttonParent.gameObject); // Completely removes the buttonParent and its children
            // Alternatively, deactivate it:
            // buttonParent.gameObject.SetActive(false);
            Debug.Log("buttonParent has been removed or deactivated.");
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
            Debug.Log($"Weapon {weapon.name} instantiated and aligned for player {gameObject.name}");

        }

        Debug.Log("Weapons successfully assigned to the player's weaponHolder.");
    }

    [ServerRpc(RequireOwnership = false)]
    private void SubmitWeaponSelectionServerRpc(string[] weaponNames, NetworkConnection sender = null)
    {
        if (sender == null)
        {
            Debug.LogError("SubmitWeaponSelectionServerRpc called without a valid sender.");
            return;
        }

        if (weaponNames == null || weaponNames.Length == 0)
        {
            Debug.LogError($"SubmitWeaponSelectionServerRpc called by player {sender.ClientId}, but weaponNames is null or empty.");
            return;
        }

        // Check for null or empty elements in the array
        if (weaponNames.Any(string.IsNullOrEmpty))
        {
            Debug.LogError($"SubmitWeaponSelectionServerRpc called by player {sender.ClientId}, but one or more weapon names is null or empty.");
            return;
        }

        Debug.Log($"SubmitWeaponSelectionServerRpc called by player {sender.ClientId} with weapons: {string.Join(", ", weaponNames)}");

        // Convert weapon names back to GameObject references
        GameObject[] selectedWeapons = new GameObject[weaponNames.Length];
        for (int i = 0; i < weaponNames.Length; i++)
        {
            string weaponName = weaponNames[i];
            
            // Find the weapon GameObject from the available pools
            GameObject weapon = FindWeaponByName(weaponName);
            
            if (weapon == null)
            {
                Debug.LogError($"Could not find weapon with name {weaponName} for player {sender.ClientId}");
                return;
            }
            
            selectedWeapons[i] = weapon;
        }

        // Store the player's selected weapons on the server
        GameStateManager.Instance.StorePlayerWeapons(sender, selectedWeapons);

        // Broadcast the selected weapons to all clients
        SyncSelectedWeaponsObserversRpc(weaponNames, sender);
    }

    // Helper method to find weapon GameObject by name
    private GameObject FindWeaponByName(string weaponName)
    {
        // Search in all weapon pools
        foreach (var weapon in MeeleWeapons)
        {
            if (weapon != null && weapon.name == weaponName)
                return weapon;
        }
        
        foreach (var weapon in RangedWeapons)
        {
            if (weapon != null && weapon.name == weaponName)
                return weapon;
        }
        
        foreach (var weapon in MiscWeapons)
        {
            if (weapon != null && weapon.name == weaponName)
                return weapon;
        }
        
        return null;
    }

    [ObserversRpc]
    private void SyncSelectedWeaponsObserversRpc(string[] weaponNames, NetworkConnection sender)
    {
        Debug.Log($"Syncing weapons for player {sender.ClientId} on all clients: {string.Join(", ", weaponNames)}");
        
        if (!IsOwner)
        {
            Debug.Log($"Weapons assigned for player {sender.ClientId} on client {NetworkManager.ClientManager.Connection.ClientId}");
            
            // Convert weapon names back to GameObject references
            GameObject[] selectedWeapons = new GameObject[weaponNames.Length];
            for (int i = 0; i < weaponNames.Length; i++)
            {
                GameObject weapon = FindWeaponByName(weaponNames[i]);
                if (weapon != null)
                {
                    selectedWeapons[i] = weapon;
                }
                else
                {
                    Debug.LogError($"Could not find weapon with name {weaponNames[i]} on client");
                    return;
                }
            }
            
            AssignWeapons(selectedWeapons);
        }
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

        UnityEngine.Debug.Log("Weapon pools have been reset for re-picking, excluding other player's weapons.");
    }
}
