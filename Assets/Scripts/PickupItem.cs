using UnityEngine;

public class PickupItem : MonoBehaviour
{
    public enum PickupType { Health, Ammo }
    public PickupType type;
    public int amount = 10;

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log($"[PickupItem] Triggered by: {other.name}");

        // Only react if the collider belongs to a player (has an Entity)
        Entity entity = other.GetComponentInParent<Entity>();
        if (entity == null)
        {
            Debug.Log("[PickupItem] Ignored non-player collision.");
            return;
        }

        Debug.Log($"[PickupItem] Valid pickup by player: {entity.name}");

        switch (type)
        {
            case PickupType.Health:
                float newHealth = Mathf.Min(entity.Health + amount, entity.StartingHealth);
                entity.Health = newHealth;
                Debug.Log($"[PickupItem] +{amount} health → {entity.Health}");
                break;

            case PickupType.Ammo:
                if (entity.weaponController != null)
                {
                    entity.weaponController.AddAmmo(amount);
                    Debug.Log($"[PickupItem] +{amount} ammo → {entity.weaponController.currentAmmo}");
                }
                break;
        }

        Destroy(gameObject);
    }

}
