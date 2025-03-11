using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


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

    float timeLeft = 40;

    float startingTime = 40;

    [Header("UI Elements")]
    public RawImage winScreen;
    public Text TimeLeftText;
    public Text Health1;
    public Text Health2;
    public Text Rounds1;
    public Text Rounds2;
    private GameState currentState = GameState.RoundStart;
    public Entity player1;
    public Entity player2;
    public Transform player1SpawnPoint;  // Changed from 'transform' to 'Transform'
    public Transform player2SpawnPoint;  // Changed from 'transform' to 'Transform'
    public PlayerMovement1 player1Movement;
    // public PlayerMovement1 player2Movement;

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
                UpdateUIValues();
                ResetRound();
                break;
            case GameState.RoundPlaying:
                CountDown();
                UpdateUIValues();
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
                player1Movement.enabled = false;
                ShowWinScreen();
                HideText();
                break;
        }
    }

    private bool CheckRoundEnd()
    {
        if (player1.Health <= 0 || player2.Health <= 0 || timeLeft <= 0)  // Added '|| timeLeft <= 0'
        {
            return true;
        }
        return false;
    }

    public void ResetRound()
    {
        timeLeft = startingTime;
        player1.Health = player1.StartingHealth;
        player2.Health = player2.StartingHealth;

        Transform player1Root = GetRootParent(player1.transform);

        player1Root.position = player1SpawnPoint.position;
        player2.GetComponent<Rigidbody>().position = player2SpawnPoint.position; //Change late for multiplayer
        
        // Force physics update
        Rigidbody rb1 = player1Root.GetComponent<Rigidbody>();
        player2.GetComponent<Rigidbody>().velocity = Vector3.zero;
        
        player1Movement.enabled = true;
        // player2Movement.enabled = true;
        currentState = GameState.RoundPlaying;
    }


    private void CountDown(){
        timeLeft -= Time.deltaTime;
    }

    private Transform GetRootParent(Transform child)
    {
        Transform current = child;
        
        // Go up until we find a Rigidbody or hit the root
        while (current.parent != null)
        {
            if (current.GetComponent<Rigidbody>() != null)
                return current;
                
            current = current.parent;
        }
        
        // If no Rigidbody found in hierarchy, return the topmost parent
        return current;
    }

    public void EndRound()
    {
        player1Movement.enabled = false;
        // player2Movement.enabled = false;

         // Disable movement initially
      
        // player2Movement.enabled = false;
        
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
        // player2Movement.enabled = true;
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
            imageColor.a = 1; // Alpha = 1 (fully visible)
            winScreen.color = imageColor;
        }
    }

    public void UpdateUIValues(){
        Health1.text = player1.Health.ToString();
        Health2.text = player2.Health.ToString();
        Rounds1.text = player1.Stocks.ToString();
        Rounds2.text = player2.Stocks.ToString();
        TimeLeftText.text = timeLeft.ToString("0");
    }

    public void HideText(){
        Health1.text = "";
        Health2.text = "";
        Rounds1.text = "";
        Rounds2.text = "";
        TimeLeftText.text = "";
    }

}