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

    public TMPro.TextMeshProUGUI promptText;
    public Transform muzzlePosition;
    public GameObject buttonPrefab; 
    public Camera playerCamera;
    public RectTransform buttonParent; 
    public RectTransform buttonParentRanged; 
    public RectTransform buttonParentMisc;

    public Transform weaponHolder;

    public bool weaponConfirmed = false;

    private GameObject[] PossibleMeeles = new GameObject[3];
    private GameObject[] PossibleGuns = new GameObject[3];
    private GameObject[] PossibleMiscs = new GameObject[3];

    private List<GameObject> OriginalMeeleWeapons;
    private List<GameObject> OriginalRangedWeapons;
    private List<GameObject> OriginalMiscWeapons;

    [Header("Button Animation")]
    public float buttonAnimDuration = 0.5f;
    public float buttonDelayBetween = 0.05f;
    public Vector2 buttonFlyInOffset = new Vector2(-300f, 0f); // Fly in from left
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
            if (selections.All(w => w != null)) // Ensure all weapons are selected
            {
                ConfirmSelection();
            }
        }
    }

    public void ResetSelections()
    {
        // Reset the selections array
        selections = new GameObject[3];
        Debug.Log("Weapon selections have been cleared for re-picking.");
    }

    void Start()
    {
        // Find the buttonParent in the scene (e.g., by tag or name)
        RectTransform buttonParentInScene = GameObject.Find("ButtonParent")?.GetComponent<RectTransform>();
        RectTransform buttonParentRangedInScene = GameObject.Find("ButtonParentRanged")?.GetComponent<RectTransform>();
        RectTransform buttonParentMiscInScene = GameObject.Find("ButtonParentMisc")?.GetComponent<RectTransform>();
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
        if (buttonParentInScene != null && buttonParentRangedInScene != null && buttonParentMiscInScene != null)
        {
            buttonParent = buttonParentInScene;
            buttonParentRanged = buttonParentRangedInScene;
            buttonParentMisc = buttonParentMiscInScene;
            Debug.Log($"buttonParent assigned from scene: {buttonParent.name}");
        }
        else
        {
            Debug.LogError("ButtonParent not found in the scene!");
        }

        if (promptText == null)
        {
            // Try to find by name - replace "WeaponPrompt" with your actual prompt's name
            promptText = GameObject.Find("Prompt")?.GetComponent<TMPro.TextMeshProUGUI>();
            
            // If not found, search for any TextMeshProUGUI tagged "Prompt" (optional)
            if (promptText == null)
            {
                GameObject promptObj = GameObject.FindGameObjectWithTag("Prompt");
                if (promptObj != null)
                    promptText = promptObj.GetComponent<TMPro.TextMeshProUGUI>();
            }
            
            if (promptText != null)
                Debug.Log("Found prompt text: " + promptText.name);
            else
                Debug.LogWarning("Prompt text not found in scene!");
            
        }
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
        ShowPrompt();
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

    public void ClearWeaponHolder()
    {
        foreach (Transform child in weaponHolder)
        {
            Destroy(child.gameObject); // Destroy each child GameObject
        }

        WeaponSwitch weaponSwitch = weaponHolder.GetComponent<WeaponSwitch>();
        if (weaponSwitch != null)
        {
            weaponSwitch.Clear();
            Debug.Log($"Reset weaponsSet flag on WeaponSwitch attached to {weaponHolder.name}");
        }
        else
        {
            Debug.LogError($"WeaponSwitch component not found on {weaponHolder.name}");
        }
        
        Debug.Log("All children of weaponHolder have been cleared.");
    }

    private void CreateWeaponButtons(GameObject[] weaponSelections, string category)
    {
        if (buttonParent == null || buttonParentMisc == null || buttonParentRanged == null)
        {
            Debug.LogError("buttonParent is not assigned! Buttons cannot be instantiated.");
            return;
        }
        
        // Determine which parent to use and color based on category
        RectTransform targetParent;
        Color buttonColor;
        
        switch(category)
        {
            case "melee":
                targetParent = buttonParent;
                buttonColor = new Color(1f, 0.3f, 0.3f); // Red for melee
                break;
            case "ranged":
                targetParent = buttonParentRanged; 
                buttonColor = new Color(0.4f, 0.6f, 1f); // Blue for ranged
                break;
            case "misc":
                targetParent = buttonParentMisc;
                buttonColor = new Color(1f, 0.9f, 0.2f); // Yellow for misc
                break;
            default:
                Debug.LogError("Invalid category: " + category);
                return;
        }
        
        // Clear any existing buttons
        foreach (Transform child in targetParent)
        {
            Destroy(child.gameObject);
        }
        
        // Create all buttons but set them invisible initially
        List<GameObject> createdButtons = new List<GameObject>();
        
        foreach (var weapon in weaponSelections)
        {
            if (weapon == null) continue;
            
            // Create the button with the proper parent
            GameObject button = Instantiate(buttonPrefab, targetParent);
            
            // Set button text
            button.GetComponentInChildren<Text>().text = weapon.name;
            
            // Get the button's image component and set initial transparent color
            Image buttonImage = button.GetComponent<Image>();
            if (buttonImage != null)
            {
                buttonImage.color = new Color(buttonColor.r, buttonColor.g, buttonColor.b, 0f);
            }
            
            // Set text to be initially transparent
            Text buttonText = button.GetComponentInChildren<Text>();
            if (buttonText != null)
            {
                Color textColor = buttonText.color;
                buttonText.color = new Color(textColor.r, textColor.g, textColor.b, 0f);
            }
            
            // Position for animation - add the fly-in offset
            RectTransform buttonRect = button.GetComponent<RectTransform>();
            if (buttonRect != null)
            {
                buttonRect.anchoredPosition += buttonFlyInOffset;
            }
            
            // Add click event
            Button buttonComponent = button.GetComponent<Button>();
            if (buttonComponent != null)
            {
                buttonComponent.onClick.AddListener(() => SelectWeapon(weapon, category));
            }
            
            // Add to list for animation
            createdButtons.Add(button);
        }
        
        // Start animations for this category
        StartCoroutine(AnimateButtonCategory(createdButtons, category));
    }

    // Animate buttons in a category with staggered timing
    private IEnumerator AnimateButtonCategory(List<GameObject> buttons, string category)
    {
        // Delay each category based on its type
        float categoryDelay = 0f;
        switch(category)
        {
            case "melee":
                categoryDelay = 0f; // First category, no delay
                break;
            case "ranged": 
                categoryDelay = 0.3f; // Start after melee buttons
                break;
            case "misc":
                categoryDelay = 0.6f; // Start after ranged buttons
                break;
        }
        
        yield return new WaitForSeconds(categoryDelay);
        
        // Animate each button in the category
        for (int i = 0; i < buttons.Count; i++)
        {
            StartCoroutine(AnimateSingleButton(buttons[i], i * buttonDelayBetween));
            yield return new WaitForSeconds(buttonDelayBetween);
        }
    }

    // Animate a single button with fade-in and movement
    private IEnumerator AnimateSingleButton(GameObject button, float delay)
    {
        yield return new WaitForSeconds(delay);
        
        Image buttonImage = button.GetComponent<Image>();
        Text buttonText = button.GetComponentInChildren<Text>();
        RectTransform buttonRect = button.GetComponent<RectTransform>();
        
        // Store the final position from the layout system
        Vector2 finalPosition = buttonRect.anchoredPosition;
        
        // Apply the initial offset for animation
        Vector2 startPos = finalPosition + buttonFlyInOffset;
        buttonRect.anchoredPosition = startPos;
        
        float elapsed = 0f;
        
        while (elapsed < buttonAnimDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / buttonAnimDuration);
            t = Mathf.SmoothStep(0f, 1f, t); // Smooth easing
            
            // Move from start to final position (not calculating offset again)
            buttonRect.anchoredPosition = Vector2.Lerp(startPos, finalPosition, t);
            
            // Fade in image
            if (buttonImage != null)
            {
                Color color = buttonImage.color;
                buttonImage.color = new Color(color.r, color.g, color.b, t);
            }
            
            // Fade in text
            if (buttonText != null)
            {
                Color color = buttonText.color;
                buttonText.color = new Color(color.r, color.g, color.b, t);
            }
            
            yield return null;
        }
        
        // Ensure final values are exact - use the original calculated position
        buttonRect.anchoredPosition = finalPosition;
        
        // Finalize alpha values
        if (buttonImage != null) 
        {
            Color color = buttonImage.color;
            buttonImage.color = new Color(color.r, color.g, color.b, 1f);
        }
        if (buttonText != null)
        {
            Color color = buttonText.color;
            buttonText.color = new Color(color.r, color.g, color.b, 1f);
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
        Debug.Log("Confirming weapon selection... " + weaponConfirmed);
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
            
            if (weaponInstance != null)
            {
                // Make sure the weapon has a NetworkObject component
                NetworkObject weaponNetObj = weaponInstance.GetComponent<NetworkObject>();
                if (weaponNetObj == null)
                {
                    Debug.LogError($"Weapon {weaponInstance.name} is missing a NetworkObject component");
                }
                else if (!weaponNetObj.IsSpawned)
                {
                    Debug.LogError($"Weapon {weaponInstance.name} is not spawned on the network");
                }
            }
            
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
        HidePrompt();
        // Remove or deactivate the buttonParent
        if (buttonParent != null && buttonParentMisc != null && buttonParentRanged != null)
        {
            foreach (Transform child in buttonParent)
            {
                Destroy(child.gameObject); // Destroy each child of buttonParent
            }
            foreach (Transform child in buttonParentMisc)
            {
                Destroy(child.gameObject); // Destroy each child of buttonParentMisc
            }
            foreach (Transform child in buttonParentRanged)
            {
                Destroy(child.gameObject); // Destroy each child of buttonParentRanged
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

    public void ShowPrompt()
    {
        if (promptText != null)
            StartCoroutine(FadePrompt(true, buttonAnimDuration));
    }

    // Hide the prompt with fade-out animation
    public void HidePrompt()
    {
        if (promptText != null)
            StartCoroutine(FadePrompt(false, buttonAnimDuration));
    }

    // Animation coroutine for fading the prompt
    private IEnumerator FadePrompt(bool fadeIn, float duration)
    {
        if (promptText == null) yield break;
        
        float startAlpha = fadeIn ? 0f : 1f;
        float targetAlpha = fadeIn ? 1f : 0f;
        Color textColor = promptText.color;
        
        // Set initial alpha
        promptText.color = new Color(textColor.r, textColor.g, textColor.b, startAlpha);
        
        // Fade over time
        float elapsedTime = 0;
        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float t = Mathf.Clamp01(elapsedTime / duration);
            float smoothT = Mathf.SmoothStep(0, 1, t); // Smooth easing
            float currentAlpha = Mathf.Lerp(startAlpha, targetAlpha, smoothT);
            
            promptText.color = new Color(textColor.r, textColor.g, textColor.b, currentAlpha);
            yield return null;
        }
        
        // Ensure we end at exactly the target alpha
        promptText.color = new Color(textColor.r, textColor.g, textColor.b, targetAlpha);
    }
}
