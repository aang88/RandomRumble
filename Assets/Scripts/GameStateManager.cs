using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameStateManager : MonoBehaviour
{
    public enum GameState
    {
        RoundStart,
        RoundPlaying,
        RoundEnd,
        ItemPick,
        GameOver
    }

    private GameState currentState = GameState.RoundStart;
    public Entity player1;
    public Entity player2;
    public Transform player1SpawnPoint;  // Changed from 'transform' to 'Transform'
    public Transform player2SpawnPoint;  // Changed from 'transform' to 'Transform'
    public PlayerMovement1 player1Movement;
    public PlayerMovement1 player2Movement;

    // Start is called before the first frame update
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
        UnityEngine.Debug.Log(currentState);
        switch (currentState)
        {
            case GameState.RoundStart:
                ResetRound();
                break;
            case GameState.RoundPlaying:
                if (CheckRoundEnd())  // Added missing parentheses
                {
                    currentState = GameState.RoundEnd;
                }
                break;
            case GameState.RoundEnd:
                CheckDeath();
                EndRound();
                break;
            case GameState.GameOver:
                break;
        }
    }

    private bool CheckRoundEnd()
    {
        if (player1.Health <= 0 || player2.Health <= 0)
        {
            return true;
        }
        return false;
    }

    public void ResetRound()
    {
        player1.Health = player1.StartingHealth;
        player2.Health = player2.StartingHealth;
        player1.transform.position = player1SpawnPoint.position;
        player2.transform.position = player2SpawnPoint.position;
        player1Movement.enabled = true;
        player2Movement.enabled = true;
        currentState = GameState.RoundPlaying;
    }

    public void EndRound()
    {
        player1Movement.enabled = false;
        player2Movement.enabled = false;

         // Disable movement initially
        player1Movement.enabled = false;
        player2Movement.enabled = false;
        
        // Start coroutine to enable movement after a short delay
        StartCoroutine(EnableMovementAfterDelay(0.2f));
        if (player1.Stocks >= 3 || player2.Stocks >= 3)  // Changed '=>' to '>='
        {
            currentState = GameState.GameOver;
        }
        else
        {
            currentState = GameState.RoundStart;
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
}