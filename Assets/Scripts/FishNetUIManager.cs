using UnityEngine;
using FishNet.Managing;
using FishNet.Transporting;
using FishNet.Connection;  // Make sure this namespace is included

public class FishNetUIManager : MonoBehaviour
{
    public Canvas fishnetCanvas;  // The UI canvas for FishNet loading screen
    public NetworkManager networkManager;  // Reference to the NetworkManager

    void Start()
    {
        // Ensure you reference the actual instance of NetworkManager
        if (networkManager == null)
        {
            networkManager = FindObjectOfType<NetworkManager>();  // If not set, find the NetworkManager in the scene
        }

        // Listen for the client connection state event
        networkManager.ClientManager.OnClientConnectionState += HandleClientConnectionState;
    }

    // This handles client connection states
    private void HandleClientConnectionState(ClientConnectionStateArgs args)
    {
        if (args.ConnectionState == LocalConnectionState.Started)
        {
            // Client connected, hide FishNet UI and show player UI
            fishnetCanvas.gameObject.SetActive(false);
        }
    }

    void OnDestroy()
    {
        // Unsubscribe from events to avoid potential memory leaks
        if (networkManager != null && networkManager.ClientManager != null)
        {
            networkManager.ClientManager.OnClientConnectionState -= HandleClientConnectionState;
        }
    }
}
