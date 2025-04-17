using UnityEngine;
using TMPro;
using FishNet.Connection;
using FishNet;
using FishNet.Object;

public class FloatingText : MonoBehaviour
{
    public float moveSpeed = 1.5f;
    public float fadeSpeed = 1f;
    public float lifetime = 2f;

    public Camera playerCam;
    
    private TextMeshPro textMesh;
    private float timer = 0f;
    
    void Start()
    {
        textMesh = GetComponent<TextMeshPro>();
        FindLocalPlayerCamera();
        // Ensure we're looking at the camera
        FaceCamera();
    }
    
    void LateUpdate()
    {
        // Move upward
        transform.position += Vector3.up * moveSpeed * Time.deltaTime;
        UnityEngine.Debug.Log("Camera: " + playerCam.transform.position);
        // Always face the camera
        FaceCamera();
        
        // Fade out gradually
        timer += Time.deltaTime;
        if (timer > lifetime * 0.5f) // Start fading after half the lifetime
        {
            Color color = textMesh.color;
            color.a = Mathf.Lerp(1, 0, (timer - lifetime * 0.5f) / (lifetime * 0.5f));
            textMesh.color = color;
        }
    }

    void FindLocalPlayerCamera()
    {
        // Get local connection's first player object
        NetworkConnection localConn = InstanceFinder.ClientManager.Connection;
        if (localConn != null)
        {
            // Get first owned object (the player)
            NetworkObject playerNetObj = localConn.FirstObject;
            if (playerNetObj != null)
            {
                // Find the camera component on the player
                Entity playerEntity = playerNetObj.GetComponent<Entity>();
                if (playerEntity != null && playerEntity.camera != null)
                {
                    // Set reference to player's camera
                    playerCam = playerEntity.camera;
                    Debug.Log($"Found local player camera: {playerCam.name}");
                }
            }
        }
        
        // Fallback to any available camera if we couldn't find the player's
        if (playerCam == null)
        {
            playerCam = FindObjectOfType<Camera>();
            Debug.LogWarning("Couldn't find local player camera, using fallback camera");
        }
    }
    
    void FaceCamera()
    {
        if (playerCam != null)
        {
            transform.forward = -playerCam.transform.forward;
            transform.Rotate(0, 180, 0);
        }
    }
}