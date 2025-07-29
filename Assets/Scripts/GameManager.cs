using UnityEngine;
using System;

public class GameManager : MonoBehaviour
{
    // Singleton
    public static GameManager Instance { get; private set; }

    private int ringsPassed = 0;
    public int RingsPassed
    {
        get => ringsPassed;
        private set 
        { 
            ringsPassed = value;
            OnRingsPassedChanged?.Invoke(ringsPassed);
        }
    }

    private int score = 0;
    public int Score
    {
        get => score;
        private set
        {
            score = value;
            OnScoreChanged?.Invoke(score);
        }
    }

    public event Action<int> OnRingsPassedChanged;
    public event Action<int> OnScoreChanged;


    private void Awake()
    {
        // Singleton pattern
        if (Instance != null && Instance != this)
        {
            // If another instance already exists, destroy this one
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject); // Keep the GameManager alive across scene loads

        // Initialize counters
        ringsPassed = 0;
        score = 0;
    }

    // Add ring to passed count.
    public void AddRing(int amount = 1)
    {
        if (amount < 0) // Prevent negative additions
        {
            return;
        }

        RingsPassed += amount;
        Debug.Log($"Rings passed: {RingsPassed}");
    }

    // Add a point (enemy destroyed) to score
    public void AddScore(int amount = 1)
    {
        if (amount < 0) // Prevent negative additions
        {
            return;
        }

        Score += amount;
        Debug.Log($"Score: {Score}");
    }

    // If needed
    public void ResetGameCounters()
    {
        RingsPassed = 0;
        Score = 0;
        Debug.Log("Game counters reset.");
    }
}
