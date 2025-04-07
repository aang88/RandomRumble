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

    public Transform muzzlePosition;
    public GameObject buttonPrefab; 
    public Camera playerCamera;
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
        if (!IsServer)
        {
            Debug.Log("Requesting inventory from the server...");
            RequestInventoryServerRpc(NetworkManager.ClientManager.Connection);
        }
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
        OriginalMeeleWeapons = new List<GameObject>(MeeleWeapons);
        OriginalRangedWeapons = new List<GameObject>(RangedWeapons);
        OriginalMiscWeapons = new List<GameObject>(MiscWeapons);
        if (muzzlePosition == null)
        {
            muzzlePosition = transform.Find("MuzzlePosition"); // Replace with the actual path
            if (muzzlePosition == null)
            {
                Debug.LogError("MuzzlePosition not found!");
            }
        }
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
        
        StartCoroutine(DelayedUISetup());
    
    }

    private IEnumerator DelayedUISetup()
    {
        // Short delay to ensure everything is properly initialized
        yield return new WaitForSeconds(0.1f);
        
        for (int i = 0; i < 3; i++)
        {
            PickRandomWeapon(ref MeeleWeapons, ref PossibleMeeles, i);
            PickRandomWeapon(ref RangedWeapons, ref PossibleGuns, i);
            PickRandomWeapon(ref MiscWeapons, ref PossibleMiscs, i);
        }

        CreateWeaponButtons(PossibleMeeles, "melee");
        CreateWeaponButtons(PossibleGuns, "ranged");
        CreateWeaponButtons(PossibleMiscs, "misc");
        
        // Force UI refresh
        Canvas canvas = buttonParent.GetComponentInParent<Canvas>();
        if (canvas != null)
        {
            yield return new WaitForEndOfFrame();
            LayoutRebuilder.ForceRebuildLayoutImmediate(buttonParent);
            canvas.enabled = false;
            canvas.enabled = true;
            Debug.Log("Canvas forcefully refreshed after button creation");
        }
    }

    public void PickRandomWeapon(ref List<GameObject> weaponPool,ref GameObject[] weaponSelecitons, int iteration){
        if (weaponPool == null || weaponPool.Count == 0)
        {
            Debug.LogError("Weapon pool is empty or null.");
            return;
        }
        int randomIndex = Random.Range(0, weaponPool.Count);
        weaponSelecitons[iteration] = weaponPool[randomIndex];
        UnityEngine.Debug.Log("Removing weapon: " + weaponPool[randomIndex].name + " from pool.");
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
            Debug.Log($"Button instantiated for weapon: {weapon.name}, Category: {category}");

            // Instantiate a button
            GameObject button = Instantiate(buttonPrefab, buttonParent);

            // Set the button's text to the weapon's name
            button.GetComponentInChildren<Text>().text = weapon.name;
            Debug.Log($"Setting Button text to: {weapon.name}");
      



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

    private void ResetWeaponPoolsForNextRound()
    {
        Debug.Log("Resetting weapon pools for the next round.");

        foreach (var weaponSelection in FindObjectsOfType<WeaponSelection>())
        {
            if (weaponSelection.IsOwner)
            {
                weaponSelection.ResetWeaponPool(); // Reset without excluding other player's weapons
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
        NotifyWeaponSelectionCompleteServerRpc();
        // Send weapon names instead of GameObject references
        string[] weaponNames = selections.Select(w => w.name).ToArray();
        SubmitWeaponSelectionServerRpc(weaponNames);

        for (int i = 0; i < selections.Length; i++)
        {
            GameObject weaponInstance = Instantiate(selections[i], weaponHolder);
            weaponInstance.transform.localPosition = Vector3.zero;
            weaponInstance.transform.localRotation = Quaternion.identity;

            DamageGun damageGun = weaponInstance.GetComponent<DamageGun>();
            if (damageGun != null)
            {
                damageGun.playerCamera = playerCamera.transform;
            }

            Gun gun = weaponInstance.GetComponent<Gun>();
            if (gun != null && damageGun != null)
            {
                gun.muzzleFlashPosition = muzzlePosition;
                gun.onGunShoot.AddListener(damageGun.Shoot);
            }

            // NetworkObject gunNetworkObject = weaponInstance.GetComponent<NetworkObject>();
            // if (gunNetworkObject == null)
            // {
            //     Debug.LogError($"NetworkObject is missing on weapon prefab: {weaponInstance.name}");
            //     continue; // Skip this weapon instance
            // }
            // if (Owner == null)
            // {
            //     Debug.LogError("Owner is null. Cannot assign ownership.");
            //     continue; // Skip this weapon instance
            // }
            
            // gunNetworkObject.GiveOwnership(Owner);
            
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // Remove or deactivate the buttonParent
        if (buttonParent != null)
        {
            foreach (Transform child in buttonParent)
            {
                Destroy(child.gameObject); // Destroy each child of buttonParent
            }
            Debug.Log("All children of buttonParent have been removed.");
        }

        Debug.Log("Weapons successfully attached to the player's weaponHolder.");
    }

    [ServerRpc(RequireOwnership = false)]
    private void NotifyWeaponSelectionCompleteServerRpc(NetworkConnection sender = null)
    {
        if (sender == null)
        {
            Debug.LogError("NotifyWeaponSelectionCompleteServerRpc called without a valid sender.");
            return;
        }

        GameStateManager.Instance.SetPlayerReady(sender, true);
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

    public void UpdateLocalInventory(string[] weaponNames)
    {
        Debug.Log($"Updating local inventory: {string.Join(", ", weaponNames)}");

        // Convert weapon names back to GameObjects
        GameObject[] selectedWeapons = weaponNames
            .Select(name => FindWeaponByName(name))
            .Where(weapon => weapon != null)
            .ToArray();

        AssignWeapons(selectedWeapons);
    }

    [ServerRpc(RequireOwnership = false)]
    public void RequestInventoryServerRpc(NetworkConnection conn)
    {
        if (GameStateManager.Instance == null)
        {
            Debug.LogError("GameStateManager instance is null. Cannot access player inventories.");
            return;
        }

        foreach (var entry in GameStateManager.Instance.PlayerInventories)
        {
            TargetSyncInventoryRpc(conn, entry.Key, entry.Value);
        }
    }

    [TargetRpc]
    private void TargetSyncInventoryRpc(NetworkConnection target, NetworkConnection playerConn, string[] weaponNames)
    {
        Debug.Log($"Sending inventory to player {target.ClientId} for player {playerConn.ClientId}: {string.Join(", ", weaponNames)}");

        WeaponSelection weaponSelection = playerConn.FirstObject.GetComponent<WeaponSelection>();
        if (weaponSelection != null)
        {
            weaponSelection.UpdateLocalInventory(weaponNames);
        }
    }

    public void ResetWeaponPool()
    {
        MeeleWeapons = new List<GameObject>(OriginalMeeleWeapons);
        RangedWeapons = new List<GameObject>(OriginalRangedWeapons);
        MiscWeapons = new List<GameObject>(OriginalMiscWeapons);


        UnityEngine.Debug.Log("Weapon pools have been reset for re-picking, excluding other player's weapons.");
    }
}
