using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FishNet.Object;
using FishNet.Object.Synchronizing;

public class TurnHead : NetworkBehaviour
{
    [Header("References")]
    [Tooltip("Reference to the camera. If null, will try to find main camera")]
    public Transform cameraTransform;
    
    [Header("Settings")]
    [Tooltip("How quickly the head rotates to target direction")]
    public float rotationSpeed = 15f;
    [Tooltip("Maximum angle the head can rotate (each side)")]
    public float maxRotationAngle = 360f;
    [Tooltip("How often to sync rotation (seconds)")]
    public float syncFrequency = 0.1f;
    
    // Reference to original rotation
    private Quaternion originalRotation;
    
    // SyncVar with proper settings
    private readonly SyncVar<float> _headYRotation = new SyncVar<float>(new SyncTypeSettings {
        WritePermission = WritePermission.ServerOnly,
        ReadPermission = ReadPermission.Observers,
        SendRate = 0.1f // Match your syncFrequency
    });
    
    private float timeSinceLastSync = 0f;
    
    private void Awake()
    {
        // Store original rotation
        originalRotation = transform.localRotation;
        
        // Set up the change callback for the SyncVar
        _headYRotation.OnChange += OnHeadRotationChanged;
    }
    
    private void OnHeadRotationChanged(float prev, float next, bool asServer)
    {
        // Skip if we're the owner or if this is happening on server
        if (IsOwner || asServer) return;
        
        Debug.Log($"Head rotation changed from {prev} to {next}, asServer: {asServer}");
        
        // Apply the networked rotation for other clients
        ApplyNetworkedRotation(next);
    }
    
    public override void OnStartClient()
    {
        base.OnStartClient();
        
        // Store the original rotation if it wasn't set in Awake
        if (originalRotation == Quaternion.identity)
            originalRotation = transform.localRotation;
        
        Debug.Log($"OnStartClient - IsOwner: {IsOwner}, Has Camera: {cameraTransform != null}");
    }
    
    public override void OnStartNetwork()
    {
        base.OnStartNetwork();
        
        // Find camera if we're the owner
        if (base.Owner.IsLocalClient && cameraTransform == null)
        {
            if (Camera.main != null)
            {
                cameraTransform = Camera.main.transform;
                Debug.Log("Found camera for head rotation: " + Camera.main.name);
            }
            else
            {
                Debug.LogWarning("No main camera found. Please assign a camera to TurnHead script.");
            }
        }
    }
    
    private void Update() // Using Update instead of LateUpdate
    {
        // Only calculate new head rotation if we're the owner
        if (IsOwner)
        {
            UpdateLocalHeadRotation();
        }
    }
    
    private void UpdateLocalHeadRotation()
    {
        if (cameraTransform == null)
        {
            Debug.LogWarning("Camera not assigned in TurnHead script!");
            return;
        }
        
        // Get camera forward direction and project it onto the horizontal plane
        Vector3 cameraForward = cameraTransform.forward;
        cameraForward.y = 0f; // Remove vertical component
        
        // Only rotate if we have a meaningful direction
        if (cameraForward.magnitude > 0.01f)
        {
            // Get the character's body rotation (main direction)
            Transform body = transform.parent;
            Quaternion bodyRotation = body ? body.rotation : Quaternion.identity;
            
            // Calculate the world-space forward direction we want to look at
            Vector3 worldLookDir = cameraForward.normalized;
            
            // Convert world direction to a look rotation
            Quaternion worldLookRotation = Quaternion.LookRotation(worldLookDir);
            
            // Calculate local rotation relative to the body
            Quaternion localRotation = Quaternion.Inverse(bodyRotation) * worldLookRotation;
            
            // Extract just the Y euler angle and clamp it
            float yRotation = localRotation.eulerAngles.y;
            if (yRotation > 180f) yRotation -= 360f;
            yRotation = Mathf.Clamp(yRotation, -maxRotationAngle, maxRotationAngle);
            
            // Create the final rotation using just the yaw component
            Quaternion targetRotation = Quaternion.Euler(0, yRotation, 0);
            
            // Apply rotation more directly for testing
            transform.localRotation = Quaternion.Slerp(
                transform.localRotation, 
                targetRotation, 
                Time.deltaTime * rotationSpeed
            );
            
            // Sync the rotation over network periodically
            timeSinceLastSync += Time.deltaTime;
            if (timeSinceLastSync >= syncFrequency)
            {
                // Only update if changed enough to matter
                if (Mathf.Abs(_headYRotation.Value - yRotation) > 0.1f)
                {
                    SyncHeadRotationServerRpc(yRotation);
                    Debug.Log($"Sending head rotation to server: {yRotation}");
                }
                timeSinceLastSync = 0f;
            }
        }
    }
    
    [ServerRpc]
    private void SyncHeadRotationServerRpc(float yRotation)
    {
        // Update the SyncVar on the server, which will automatically sync to clients
        _headYRotation.Value = yRotation;
        Debug.Log($"Server received head rotation: {yRotation}");
    }
    
    private void ApplyNetworkedRotation(float yRotation)
    {
        Debug.Log($"Applying networked rotation: {yRotation}");
        
        // Apply networked rotation value for non-local players
        Quaternion networkedRotation = Quaternion.Euler(0, yRotation, 0);
        transform.localRotation = Quaternion.Slerp(
            transform.localRotation,
            networkedRotation,
            Time.deltaTime * rotationSpeed
        );
    }
}