using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using FishNet.Object;
using FishNet.Connection;
using FishNet.Serializing;
using FishNet.Object.Synchronizing;

public class GameStateManager : NetworkBehaviour
{
    public enum GameState
    {
        RoundStart,
        RoundPlaying,
        RoundEnd,
        ItemPick,
        GameOver
    }

    // Use NetworkVariable to sync data
    private readonly SyncVar<float> syncedTimeLeft = new SyncVar<float>(50f);
    private readonly SyncVar<GameState> currentState = new SyncVar<GameState>(GameState.RoundStart);

    // Add a SyncVar for player NetworkObjects
    private readonly SyncVar<NetworkObject> syncedPlayer1 = new SyncVar<NetworkObject>();
    private readonly SyncVar<NetworkObject> syncedPlayer2 = new SyncVar<NetworkObject>();

    // Add these SyncVars to the class, near your other SyncVar declarations
    private readonly SyncVar<float> player1Health = new SyncVar<float>();
    private readonly SyncVar<float> player2Health = new SyncVar<float>();
    private readonly SyncVar<int> player1Stocks = new SyncVar<int>();
    private readonly SyncVar<int> player2Stocks = new SyncVar<int>();

    public float startingTime = 40f;

    [Header("UI Elements")]
    public RawImage winScreen;
    public Text TimeLeftText;
    public Text Health1;
    public Text Health2;
    public Text Rounds1;
    public Text Rounds2;

    public Entity player1;
    public Entity player2;
    public Transform player1SpawnPoint;
    public Transform player2SpawnPoint;
    public PlayerMovement1 player1Movement;
    public PlayerMovement1 player2Movement;

    private bool playersInitialized = false;

    void Start()
    {
        // Initialize player setup if needed.
    }

    void Update()
    {
        
        // Only run basic checks on both server and client
        if (player1 != null && player2 != null)
        {
            playersInitialized = true;
        }

        if (!playersInitialized)
        {
            UnityEngine.Debug.Log(player1 + " " + player2);
            UnityEngine.Debug.Log("Players not initialized yet.");
            return;
        }

        // Update UI on all clients
        UpdateUIValues();

        // Only run game logic on server
        if (IsServer)
        {
            HandleGameState();
        }
    }

    private void HandleGameState()
    {
        // Call SyncPlayerValues() at the beginning to update all synced values
        SyncPlayerValues();
        
        switch (currentState.Value)
        {
            case GameState.RoundStart:
                ResetRound();
                break;
            case GameState.RoundPlaying:
                CountDown();
                if (CheckRoundEnd())
                {
                    // Add this line to check deaths ONCE before changing state
                    CheckDeath();
                    currentState.Value = GameState.RoundEnd;
                }
                break;
            case GameState.RoundEnd:
                // Remove CheckDeath() call from here
                ProcessRoundEnd();
                break;
            case GameState.GameOver:
                SetPlayersEnabled(false);
                ShowWinScreen();
                HideText();
                break;
        }
    }

    public override void OnStartClient()
    {
        base.OnStartClient();

        if (!IsOwner)
        {
            RequestCurrentStateServerRpc();
            RequestPlayersServerRpc();
            RequestPlayerStatsServerRpc();
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void RequestCurrentStateServerRpc(NetworkConnection conn = null)
    {
        TargetSetState(conn, currentState.Value, syncedTimeLeft.Value);
    }

    [ServerRpc(RequireOwnership = false)]
    private void RequestPlayersServerRpc(NetworkConnection conn = null)
    {
        if (player1 != null)
        {
            BroadcastPlayerJoinedRpc(player1.GetComponent<NetworkObject>(), 1);
        }
        
        if (player2 != null)
        {
            BroadcastPlayerJoinedRpc(player2.GetComponent<NetworkObject>(), 2);
        }
    }

    [TargetRpc]
    private void TargetSetState(NetworkConnection target, GameState state, float timeLeft)
    {
        currentState.Value = state;
        syncedTimeLeft.Value = timeLeft;
    }

    [ObserversRpc]
    private void BroadcastPlayerJoinedRpc(NetworkObject playerNetObj, int playerNumber)
    {
        UnityEngine.Debug.Log($"Received player {playerNumber} join broadcast");
        
        if (playerNetObj == null)
        {
            UnityEngine.Debug.LogError($"Player {playerNumber} NetworkObject is null in BroadcastPlayerJoinedRpc");
            return;
        }

        Entity playerEntity = playerNetObj.GetComponent<Entity>();
        if (playerEntity == null)
        {
            UnityEngine.Debug.LogError($"Player {playerNumber} Entity component not found");
            return;
        }

        if (playerNumber == 1 && player1 == null)
        {
            player1 = playerEntity;
            player1Movement = playerEntity.GetComponent<PlayerMovement1>();
        }
        else if (playerNumber == 2 && player2 == null)
        {
            player2 = playerEntity;
            player2Movement = playerEntity.GetComponent<PlayerMovement1>();
        }

        // Check if both players are initialized
        if (player1 != null && player2 != null)
        {
            playersInitialized = true;
            UnityEngine.Debug.Log("Both players are now initialized on client");
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void RequestPlayerInitializationServerRpc(float health, int stocks, NetworkConnection conn = null)
    {
        // Initialize the player's state on the client (on the server).
        TargetPlayerInitializationRpc(conn, health, stocks);
    }

    [TargetRpc]
    private void TargetPlayerInitializationRpc(NetworkConnection target, float health, int stocks)
    {
        // Set player health and stocks on the client when it joins.
        if (player1 != null)
        {
            player1.Health = health;
            player1.Stocks = stocks;
        }
        else if (player2 != null)
        {
            player2.Health = health;
            player2.Stocks = stocks;
        }
    }

    public static GameStateManager Instance { get; private set; }

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnGameStateChanged(GameState oldState, GameState newState)
    {
        UnityEngine.Debug.Log("State changed to: " + newState);
        UpdateUIValues();
    }

    public void AddPlayer(Entity playerEntity)
    {
        UnityEngine.Debug.Log("Attempting to add a player: " + playerEntity);
        int playerNumber = 0;
        
        if (player1 == null)
        {
            player1 = playerEntity;
            player1.Health = player1.StartingHealth;
            player1Movement = playerEntity.GetComponent<PlayerMovement1>();
            playerEntity.transform.position = player1SpawnPoint.position;
            
            // Initialize the SyncVars on the server
            if (IsServer)
            {
                player1Health.Value = player1.StartingHealth;
                player1Stocks.Value = 0;
            }
            
            playerNumber = 1;
        }
        else if (player2 == null)
        {
            player2 = playerEntity;
            player2.Health = player2.StartingHealth;
            player2Movement = playerEntity.GetComponent<PlayerMovement1>();
            playerEntity.transform.position = player2SpawnPoint.position;
            
            // Initialize the SyncVars on the server
            if (IsServer)
            {
                player2Health.Value = player2.StartingHealth;
                player2Stocks.Value = 0;
            }
            
            playerNumber = 2;
        }

        if (IsServer && playerNumber > 0)
        {
            // Broadcast to all clients that a player has joined
            NetworkObject playerNetObj = playerEntity.GetComponent<NetworkObject>();
            BroadcastPlayerJoinedRpc(playerNetObj, playerNumber);
        }
        
        if (!IsServer) // Client-side initialization
        {
            RequestPlayerInitializationServerRpc(player2?.Health ?? 0, player2?.Stocks ?? 0);
        }
    }


    private bool CheckRoundEnd()
    {
        if (player1.Health <= 0 || player2.Health <= 0 || syncedTimeLeft.Value <= 0)
        {
            return true;
        }
        return false;
    }

    public void ResetRound()
    {
        if (!IsServer) return;

        syncedTimeLeft.Value = startingTime;
        player1.Health = player1.StartingHealth;
        player2.Health = player2.StartingHealth;
        
        // Update the SyncVars
        player1Health.Value = player1.StartingHealth;
        player2Health.Value = player2.StartingHealth;

        // Handle Player1 respawn
        Transform player1Root = GetRootParent(player1.transform);
        player1Root.position = player1SpawnPoint.position;
        Rigidbody rb1 = player1Root.GetComponent<Rigidbody>();
        if (rb1 != null) {
            rb1.velocity = Vector3.zero;
        }

        // Handle Player2 respawn - use the same pattern as Player1
        Transform player2Root = GetRootParent(player2.transform);
        player2Root.position = player2SpawnPoint.position;
        Rigidbody rb2 = player2Root.GetComponent<Rigidbody>();
        if (rb2 != null) {
            rb2.velocity = Vector3.zero;
        }

        player1Movement.enabled = true;
        player2Movement.enabled = true;

        // Use RPC to ensure all clients reset player positions
        BroadcastPlayerPositionsRpc(player1SpawnPoint.position, player2SpawnPoint.position);

        currentState.Value = GameState.RoundPlaying;
    }

    // Update the BroadcastPlayerPositionsRpc method to include rotation
    [ObserversRpc]
    private void BroadcastPlayerPositionsRpc(Vector3 player1Position, Vector3 player2Position)
    {
        if (player1 != null)
        {
            Transform player1Root = GetRootParent(player1.transform);
            player1Root.position = player1Position;
            // Reset rotation to upright
            player1Root.rotation = Quaternion.identity;
            Rigidbody rb1 = player1Root.GetComponent<Rigidbody>();
            if (rb1 != null) {
                rb1.velocity = Vector3.zero;
                rb1.angularVelocity = Vector3.zero; // Stop any spinning
            }
        }
        
        if (player2 != null)
        {
            Transform player2Root = GetRootParent(player2.transform);
            player2Root.position = player2Position;
            // Reset rotation to upright
            player2Root.rotation = Quaternion.identity;
            Rigidbody rb2 = player2Root.GetComponent<Rigidbody>();
            if (rb2 != null) {
                rb2.velocity = Vector3.zero;
                rb2.angularVelocity = Vector3.zero; // Stop any spinning
            }
        }
        
        UnityEngine.Debug.Log("Player positions and rotations reset on client");
    }

    private void CountDown()
    {
        if (!IsServer) return;

        syncedTimeLeft.Value -= Time.deltaTime;
    }

    private Transform GetRootParent(Transform child)
    {
        Transform current = child;

        while (current.parent != null)
        {
            if (current.GetComponent<Rigidbody>() != null)
                return current;

            current = current.parent;
        }

        return current;
    }

    public void EndRound()
    {
        player1Movement.enabled = false;
        player2Movement.enabled = false;

        StartCoroutine(EnableMovementAfterDelay(0.2f));
        if (player1.Stocks >= 3 || player2.Stocks >= 3)
        {
            currentState.Value = GameState.GameOver;
        }
        else
        {
            currentState.Value = GameState.RoundStart;
        }
    }

    private IEnumerator EnableMovementAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        player1Movement.enabled = true;
        player2Movement.enabled = true;
    }

    // Modify PlayerDied to use SyncVars
    public void PlayerDied(Entity player)
    {
        if (!IsServer) return;
        
        if (player == player1)
        {
            player2.Stocks++;
            player2Stocks.Value = player2.Stocks;
        }
        else
        {
            player1.Stocks++;
            player1Stocks.Value = player1.Stocks;
        }
    }

    public void CheckDeath()
    {
        if (player1.Health <= 0)
        {
            PlayerDied(player1);
        }
        else if (player2.Health <= 0)
        {
            PlayerDied(player2);
        }
    }

    public void ProcessRoundEnd()
    {
        // Disable player movement
        SetPlayersEnabled(false);

        // Delay before proceeding to next state
        StartCoroutine(DelayedStateTransition(2.0f));
    }

    private void SetPlayersEnabled(bool enabled)
    {
        if (player1Movement != null) player1Movement.enabled = enabled;
        if (player2Movement != null) player2Movement.enabled = enabled;
    }

    private IEnumerator DelayedStateTransition(float delay)
    {
        yield return new WaitForSeconds(delay);
        
        // Re-enable movement
        SetPlayersEnabled(true);
        
        // Transition to appropriate state
        if (player1.Stocks >= 3 || player2.Stocks >= 3)
        {
            currentState.Value = GameState.GameOver;
        }
        else
        {
            currentState.Value = GameState.RoundStart;
        }
    }

    public void ShowWinScreen()
    {
        if (winScreen != null)
        {
            Color imageColor = winScreen.color;
            imageColor.a = 1;
            winScreen.color = imageColor;
        }
    }

    private void UpdateUIValues()
    {
        if (Health1 != null && player1 != null)
        {
            // Use the synced value instead of directly accessing the local player
            Health1.text = player1Health.Value.ToString("0");
        }
        else
        {
            Health1.text = "Initializing...";
        }

        if (Health2 != null && player2 != null)
        {
            // Use the synced value instead of directly accessing the local player
            Health2.text = player2Health.Value.ToString("0");
        }
        else
        {
            Health2.text = "Initializing...";
        }

        if (Rounds1 != null && player1 != null)
        {
            Rounds1.text = player1Stocks.Value.ToString();
        }

        if (Rounds2 != null && player2 != null)
        {
            Rounds2.text = player2Stocks.Value.ToString();
        }

        TimeLeftText.text = syncedTimeLeft.Value.ToString("0");
    }

    public void HideText()
    {
        Health1.text = "";
        Health2.text = "";
        Rounds1.text = "";
        Rounds2.text = "";
        TimeLeftText.text = "";
    }

    // Add this method to sync health and stocks from local entities to SyncVars
    public void SyncPlayerValues()
    {
        if (!IsServer) return;
        
        if (player1 != null)
        {
            player1Health.Value = player1.Health;
            player1Stocks.Value = player1.Stocks;
        }
        
        if (player2 != null)
        {
            player2Health.Value = player2.Health;
            player2Stocks.Value = player2.Stocks;
        }
    }

    // Add this to ensure clients have current player data
    [ServerRpc(RequireOwnership = false)]
    private void RequestPlayerStatsServerRpc(NetworkConnection conn = null)
    {
        if (player1 != null && player2 != null)
        {
            TargetUpdatePlayerStatsRpc(conn, player1.Health, player1.Stocks, player2.Health, player2.Stocks);
        }
    }

    [TargetRpc]
    private void TargetUpdatePlayerStatsRpc(NetworkConnection target, float p1Health, int p1Stocks, float p2Health, int p2Stocks)
    {
        player1Health.Value = p1Health;
        player1Stocks.Value = p1Stocks;
        player2Health.Value = p2Health;
        player2Stocks.Value = p2Stocks;
        
        // Also update local entity values for any client-side logic
        if (player1 != null)
        {
            player1.Health = p1Health;
            player1.Stocks = p1Stocks;
        }
        
        if (player2 != null)
        {
            player2.Health = p2Health;
            player2.Stocks = p2Stocks;
        }
        
        UpdateUIValues();
    }
}
