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
    private readonly  SyncVar<float> syncedTimeLeft = new SyncVar<float>(50f);
    private readonly SyncVar<GameState> currentState = new SyncVar<GameState>(GameState.RoundStart);

    // Add a SyncVar for player NetworkObjects
    private readonly SyncVar<NetworkObject> syncedPlayer1 = new SyncVar<NetworkObject>();
    private readonly SyncVar<NetworkObject> syncedPlayer2 = new SyncVar<NetworkObject>();

    float startingTime = 40f;

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

        switch (currentState.Value)
        {
            case GameState.RoundStart:
                UpdateUIValues();
                ResetRound();
                break;
            case GameState.RoundPlaying:
                CountDown();
                UpdateUIValues();
                if (CheckRoundEnd())
                {
                    currentState.Value = GameState.RoundEnd;
                }
                break;
            case GameState.RoundEnd:
                CheckDeath();
                EndRound();
                break;
            case GameState.GameOver:
                player1Movement.enabled = false;
                player2Movement.enabled = false;
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
            playerNumber = 1;
        }
        else if (player2 == null)
        {
            player2 = playerEntity;
            player2.Health = player2.StartingHealth;
            player2Movement = playerEntity.GetComponent<PlayerMovement1>();
            playerEntity.transform.position = player2SpawnPoint.position;
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

        Transform player1Root = GetRootParent(player1.transform);
        player1Root.position = player1SpawnPoint.position;
        player2.GetComponent<Rigidbody>().position = player2SpawnPoint.position;

        player1Root.GetComponent<Rigidbody>().velocity = Vector3.zero;
        player2.GetComponent<Rigidbody>().velocity = Vector3.zero;

        player1Movement.enabled = true;
        player2Movement.enabled = true;

        currentState.Value = GameState.RoundPlaying;
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

    public void PlayerDied(Entity player)
    {
        if (player == player1)
        {
            player2.Stocks++;
        }
        else
        {
            player1.Stocks++;
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

    public void ShowWinScreen()
    {
        if (winScreen != null)
        {
            Color imageColor = winScreen.color;
            imageColor.a = 1;
            winScreen.color = imageColor;
        }
    }

    public void UpdateUIValues()
    {
        if (Health1 != null)
        {
            Health1.text = player1.Health.ToString();
        }

        if (player1 == null)
        {
            UnityEngine.Debug.LogWarning("player1 is null! Cannot update UI values.");
        }
        else if (player1.Health > 0)
        {
            Health1.text = player1.Health.ToString();
        }
        else
        {
            Health1.text = "Initializing...";
        }

        if (Health2 != null)
        {
            Health2.text = player2.Health.ToString();
        }

        if (player2 == null)
        {
            UnityEngine.Debug.LogWarning("player2 is null! Cannot update UI values.");
        }
        else if (player2.Health > 0)
        {
            Health2.text = player2.Health.ToString();
        }
        else
        {
            Health2.text = "Initializing...";
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
}
