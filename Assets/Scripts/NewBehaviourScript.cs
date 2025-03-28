using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WeaponSelection : MonoBehaviour
{
    public List<GameObject> MeeleWeapons = new List<GameObject>();
    public List<GameObject> RangedWeapons = new List<GameObject>();
    public List<GameObject> MiscWeapons = new List<GameObject>();
    private GameObject[] selections = new GameObject[3];

    public GameObject buttonPrefab; 
    public Transform buttonParent; 

    private GameObject[] PossibleMeeles = new GameObject[3];
    private GameObject[] PossibleGuns = new GameObject[3];
    private GameObject[] PossibleMiscs = new GameObject[3];
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void PickRandomWeaponPool(){
        // Randomly select 3 weapons from the list
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
        int randomIndex = Random.Range(0, weaponPool.Count);
        weaponSelecitons[iteration] = weaponPool[randomIndex];
        weaponPool.RemoveAt(randomIndex); // Remove to avoid duplicates
        
    }

     private void CreateWeaponButtons(GameObject[] weaponSelections, string category)
    {
        foreach (var weapon in weaponSelections)
        {
            if (weapon == null) continue;

            // Instantiate a button
            GameObject button = Instantiate(buttonPrefab, buttonParent);

            // Set the button's text to the weapon's name
            button.GetComponentInChildren<Text>().text = weapon.name;

            // Add a click event to the button
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

    private void ConfirmSelection(){
        foreach (var weapon in selections)
        {
            if (weapon = null)
            {
                return;
            }
        }

        foreach (var weapon in selections)
        {
            GameObject weaponInstance = Instantiate(weapon, weaponHolder);
            weaponInstance.transform.localPosition = Vector3.zero; // Adjust position if needed
            weaponInstance.transform.localRotation = Quaternion.identity; // Adjust rotation if needed
        }

        // Instantiate the selected weapons in the game world
}
