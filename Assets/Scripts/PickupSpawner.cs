using UnityEngine;
using System.Collections.Generic;

public class PickupSpawner : MonoBehaviour
{
    public GameObject[] pickupPrefabs;
    public int numberOfPickups = 20;
    public float hoverOffset = 0.3f;
    public float minDistanceBetweenPickups = 3f; // Adjust for spacing
    public float spawnRadius = 50f; // Set how wide the spawn area is

    void Start()
    {
        SpawnPickups();
    }

    void SpawnPickups()
    {
        List<Vector3> usedPositions = new List<Vector3>();
        int attempts = 0;
        int spawned = 0;

        while (spawned < numberOfPickups && attempts < numberOfPickups * 10)
        {
            // Random XZ around the spawner
            Vector3 randomXZ = new Vector3(
                transform.position.x + Random.Range(-spawnRadius, spawnRadius),
                transform.position.y + 50f, // Start raycast from above
                transform.position.z + Random.Range(-spawnRadius, spawnRadius)
            );

            Ray ray = new Ray(randomXZ, Vector3.down);
            if (Physics.Raycast(ray, out RaycastHit hitInfo, 100f))
            {
                Vector3 spawnPosition = hitInfo.point + Vector3.up * hoverOffset;

                // Clumping check
                bool isFarEnough = true;
                foreach (var pos in usedPositions)
                {
                    if (Vector3.Distance(pos, spawnPosition) < minDistanceBetweenPickups)
                    {
                        isFarEnough = false;
                        break;
                    }
                }

                if (!isFarEnough)
                {
                    attempts++;
                    continue;
                }

                // Spawn it
                GameObject pickup = pickupPrefabs[Random.Range(0, pickupPrefabs.Length)];
                GameObject spawnedPickup = Instantiate(pickup, spawnPosition, Quaternion.identity);
                spawnedPickup.AddComponent<PickupRotator>();

                usedPositions.Add(spawnPosition);
                spawned++;

                Debug.Log($"[PickupSpawner] Spawned pickup #{spawned} at {spawnPosition}");
            }
            else
            {
                Debug.LogWarning($"[PickupSpawner] Raycast missed at {randomXZ}. Likely no ground.");
            }

            attempts++;
        }

        if (spawned < numberOfPickups)
        {
            Debug.LogWarning($"[PickupSpawner] Only spawned {spawned} of {numberOfPickups} after {attempts} attempts.");
        }
        else
        {
            Debug.Log($"[PickupSpawner] Successfully spawned all {spawned} pickups.");
        }
    }


    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(transform.position + Vector3.up * 0.5f, new Vector3(spawnRadius * 2f, 1f, spawnRadius * 2f));
    }
}
