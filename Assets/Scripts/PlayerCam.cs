using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Threading;
using UnityEngine;
using FishNet.Object;
using FishNet.Object.Synchronizing;

public class PlayerCam : NetworkBehaviour
{
    public float sensX;
    public float sensY;

    public Transform orientation;
    float xRotation;
    float yRotation;

    // Network rotation values - no longer using SyncVar
    private float syncedYRotation;
    private float syncedXRotation;

    private bool cursorLocked = false;

    // Replace Start with OnStartClient
    public override void OnStartClient()
    {
        base.OnStartClient();
        
        // This is now safe to use
        if (base.IsOwner)
        {
            LockCursor();
        }
    }
    
    private void LockCursor()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        cursorLocked = true;
    }

    void Update()
    {
        if (base.IsOwner)
        {
            // Double check cursor lock state (safety)
            if (!cursorLocked)
            {
                LockCursor();
            }
            
            HandleLocalCameraInput();
        }
        else
        {
            // For non-owners, apply the synced rotation
            ApplySyncedRotation();
        }
    }

    private void HandleLocalCameraInput()
    {
        //Get Mouse Input
        float mouseX = Input.GetAxisRaw("Mouse X") * Time.deltaTime * sensX;
        float mouseY = Input.GetAxisRaw("Mouse Y") * Time.deltaTime * sensY;

        yRotation += mouseX;
        xRotation -= mouseY;

        //We don't want to turn more than 90 degrees
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        //Rotate cam and Orientation
        transform.rotation = Quaternion.Euler(xRotation, yRotation, 0);
        orientation.rotation = Quaternion.Euler(0, yRotation, 0);

        // Sync to server only when significant movement happens
        // Using a timer or delta check would be more efficient
        SyncRotationServerRpc(xRotation, yRotation);
    }

    [ServerRpc(RunLocally = false)]
    private void SyncRotationServerRpc(float xRot, float yRot)
    {
        // Update server values
        syncedXRotation = xRot;
        syncedYRotation = yRot;
        
        // Broadcast to all clients
        SyncRotationObserversRpc(xRot, yRot);
    }
    
    [ObserversRpc(ExcludeOwner = true, RunLocally = false)]
    private void SyncRotationObserversRpc(float xRot, float yRot)
    {
        // This will only run on non-owner clients now
        syncedXRotation = xRot;
        syncedYRotation = yRot;
    }

    private void ApplySyncedRotation()
    {
        // For non-owner clients, apply the synced rotation values
        transform.rotation = Quaternion.Euler(syncedXRotation, syncedYRotation, 0);
        if (orientation != null)
        {
            orientation.rotation = Quaternion.Euler(0, syncedYRotation, 0);
        }
    }
    
    // Optional: Add cleanup when player disconnects
    public override void OnStopClient()
    {
        base.OnStopClient();
        
        if (base.IsOwner)
        {
            // Restore cursor state when leaving game
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }
}
