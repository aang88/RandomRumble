using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using FishNet.Object;
using FishNet.Object.Synchronizing; 


public class DamageGun : NetworkBehaviour
{
    public float damage;
    public float bulletRange;
    public Transform playerCamera;
    private NetworkObject playerNetworkObject;

    private Entity ownerEntity;

    // Add at the top of the class with your other variables

    [Header("Recoil Settings")]
    public float recoilAmount = 2.0f;       // How much the gun kicks back
    public float recoilRecoverySpeed = 10f;  // How fast it returns to normal
    public float recoilRotationAmount = 5f;  // How much the gun rotates when fired
    private Vector3 originalPosition;        // Starting position for recovery
    private Vector3 recoilVelocity;          // For SmoothDamp
    private Vector3 currentRotation;         // Current recoil rotation
    private Vector3 rotationVelocity;        // For rotation SmoothDamp

    [Header("Bullet Tracer")]
    public GameObject bulletTracerPrefab;    // Line renderer prefab for the tracer
    public Transform muzzlePoint;            // Where the bullet exits the gun
    public float tracerDuration = 0.05f;     // How long the tracer is visible
    public Material tracerMaterial;          // Optional material for tracer

    private void Start()
    {
        FindPlayerReferences(); 
        originalPosition = transform.localPosition;
    }

    private void Update()
    {
        // Return gun to original position after recoil
        if (transform.localPosition != originalPosition)
        {
            transform.localPosition = Vector3.SmoothDamp(
                transform.localPosition, 
                originalPosition, 
                ref recoilVelocity, 
                1 / recoilRecoverySpeed
            );
        }

        // Return gun to original rotation after recoil
        if (currentRotation != Vector3.zero)
        {
            currentRotation = Vector3.SmoothDamp(
                currentRotation,
                Vector3.zero,
                ref rotationVelocity,
                1 / recoilRecoverySpeed
            );
            
            transform.localRotation = Quaternion.Euler(currentRotation);
        }
    }

    private void FindPlayerReferences()
    {
        // Find player Entity in parent hierarchy
        ownerEntity = GetComponentInParent<Entity>();
        
        if (ownerEntity == null)
        {
            UnityEngine.Debug.LogError($"Gun {gameObject.name} couldn't find Entity in parent hierarchy!");
            return;
        }
        
        // Get the player's NetworkObject
        playerNetworkObject = ownerEntity.GetComponent<NetworkObject>();
        
        if (playerNetworkObject == null)
        {
            UnityEngine.Debug.LogError("Player Entity doesn't have a NetworkObject component!");
            return;
        }
        
        // Try to find camera
        if (playerCamera == null)
        {
            // Try to get from entity first
            if (ownerEntity.camera != null)
            {
                playerCamera = ownerEntity.camera.transform;
                UnityEngine.Debug.Log($"Using entity's camera: {playerCamera.name}");
            }
            else
            {
                // Try hierarchy search
                playerCamera = ownerEntity.GetComponentInChildren<Camera>()?.transform;
                
                if (playerCamera == null && Camera.main != null)
                {
                    playerCamera = Camera.main.transform;
                    UnityEngine.Debug.Log("Using main camera as fallback");
                }
            }
        }
        
        UnityEngine.Debug.Log($"Gun initialized on {ownerEntity.name}, IsOwner: {playerNetworkObject.IsOwner}");
    }

    public void Shoot()
    {
        if (ownerEntity == null || playerNetworkObject == null)
        {
            FindPlayerReferences();
            
            // If still null, we can't proceed
            if (ownerEntity == null || playerNetworkObject == null)
            {
                UnityEngine.Debug.LogError("Cannot shoot - missing player references!");
                return;
            }
        }

        if (!playerNetworkObject.IsOwner)
        {
            UnityEngine.Debug.Log("Not the owner of the player, cannot shoot.");
            return;
        }
        if (playerCamera == null)
        {
            // Try to get camera from parent hierarchy
            playerCamera = transform.root.GetComponentInChildren<Camera>()?.transform;
            
            // If there's an entity owner, try to use its camera
            if (playerCamera == null && ownerEntity != null)
            {
                playerCamera = ownerEntity.camera?.transform;
            }
            
            if (playerCamera == null)
            {
                UnityEngine.Debug.LogError("Player camera not assigned and couldn't be found automatically!");
                UnityEngine.Debug.Log($"Gun parent: {transform.parent?.name ?? "No parent"}");
                UnityEngine.Debug.Log($"Gun GameObject: {gameObject.name}");

            }
            else
            {
                UnityEngine.Debug.Log("Found camera: " + playerCamera.name);
            }
        }
        ApplyRecoil();
        Ray gunRay = new Ray(playerCamera.position, playerCamera.forward);
        UnityEngine.Debug.DrawRay(playerCamera.position, playerCamera.forward * bulletRange, Color.red, 1f);
        UnityEngine.Debug.Log("Today is Friday in CALIFORNIA!");
        if (Physics.Raycast(gunRay, out RaycastHit hitInfo, bulletRange))
        {
            UnityEngine.Debug.Log("Fire!");
            CreateBulletTracer(hitInfo.point);
            Vector3 hitPoint = hitInfo.point;
            if (ownerEntity != null && playerNetworkObject != null)
            {
                ownerEntity.SyncVisualEffectsServerRpc(hitPoint, muzzlePoint.position, gameObject.GetComponent<NetworkObject>());
            }
           
            Entity entityToDamage = hitInfo.collider.GetComponentInParent<Entity>();
            if (entityToDamage != null)
            {
                NetworkObject networkObj = entityToDamage.GetComponent<NetworkObject>();
                if (networkObj != null && networkObj != playerNetworkObject)
                {
                    // Check if it's a headshot
                    float damageToApply = damage;
                    bool isHeadshot = false;
                    
                    // Check if the hit object is named "Head"
                    if (hitInfo.collider.gameObject.name == "Head" || 
                        hitInfo.collider.gameObject.name.Contains("Head"))
                    {
                        // Double the damage for headshots
                        damageToApply *= 2f;
                        isHeadshot = true;
                        UnityEngine.Debug.Log("HEADSHOT! Double damage applied.");
                    }
                    
                    UnityEngine.Debug.Log("Hit Entity: " + entityToDamage.name);
                    UnityEngine.Debug.Log("Hit!");  
                    
                    // Pass the modified damage value
                    ownerEntity.RequestHitEntityServerRpc(networkObj, damageToApply);
                    
                    // Optional: Show headshot effect or play sound
                    if (isHeadshot)
                    {
                        UnityEngine.Debug.Log("headshot !");  
                        // Example: Instantiate(headshotEffectPrefab, hitPoint, Quaternion.identity);
                    }
                }
            }
        }
        else
        {
            // Tracer for misses (goes to max range)
            Vector3 endPoint = playerCamera.position + playerCamera.forward * bulletRange;
            CreateBulletTracer(endPoint);
            if (ownerEntity != null && playerNetworkObject != null)
            {
                ownerEntity.SyncVisualEffectsServerRpc(endPoint, muzzlePoint.position, gameObject.GetComponent<NetworkObject>());
            }
        }
    }

    private void ApplyRecoil()
    {
        // Apply position-based recoil
        transform.localPosition -= Vector3.forward * recoilAmount;
        
        // Apply rotation-based recoil
        float xRecoil = Random.Range(0.5f, 1f) * recoilRotationAmount;
        float yRecoil = Random.Range(-0.5f, 0.5f) * (recoilRotationAmount * 0.5f);
        
        currentRotation += new Vector3(-xRecoil, yRecoil, 0);
        transform.localRotation = Quaternion.Euler(currentRotation);
    }

    private void CreateBulletTracer(Vector3 hitPoint)
    {
        if (muzzlePoint == null)
        {
            UnityEngine.Debug.LogWarning("No muzzle point assigned for bullet tracer");
            return;
        }
    
        CreateBulletTracerOnClient(muzzlePoint.position, hitPoint);
    }

    public void CreateBulletTracerOnClient(Vector3 startPoint, Vector3 hitPoint)
    {
        // Skip if no muzzle point is defined
        GameObject tracer;
        if (bulletTracerPrefab != null)
        {
            tracer = Instantiate(bulletTracerPrefab);
        }
        else
        {
            tracer = new GameObject("BulletTracer");
            LineRenderer line = tracer.AddComponent<LineRenderer>();
            line.startWidth = 0.05f;
            line.endWidth = 0.03f;
            
            // Use the tracer material if provided, otherwise create a basic one
            if (tracerMaterial != null)
            {
                line.material = tracerMaterial;
            }
            else
            {
                line.material = new Material(Shader.Find("Sprites/Default"));
                line.material.color = new Color(1f, 0.9f, 0.5f);  // Yellow-orange color
            }
            
            line.positionCount = 2;
        }
        
        // Set tracer position from the provided start to end points
        LineRenderer lineRenderer = tracer.GetComponent<LineRenderer>();
        if (lineRenderer != null)
        {
            lineRenderer.SetPosition(0, startPoint);
            lineRenderer.SetPosition(1, hitPoint);
        }
        
        // Destroy the tracer after a short duration
        Destroy(tracer, tracerDuration);
    }

    

    // [ServerRpc(RequireOwnership = false)]
    // private void RequestDamageServerRpc(NetworkObject enemyObject, float damage)
    // {
    //     if (enemyObject != null)
    //     {
    //         Entity enemyEntity = enemyObject.GetComponent<Entity>();
    //         if (enemyEntity != null)
    //         {
    //             enemyEntity.TakeDamage(damage);
    //         }
    //     }
    // }
}