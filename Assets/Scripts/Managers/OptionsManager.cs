using System;
using UnityEngine;

public class OptionsManager : MonoBehaviour
{
    // Singleton
    public static OptionsManager Instance { get; private set; }

    // Options to be able to change
    public bool InvertY { get; private set; }

    // Events
    public static event Action<bool> OnInvertChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
        }

        else
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        LoadSettings();
    }
    public void SetInvertY(bool inverted)
    {
        if (InvertY != inverted)
        {
            InvertY = inverted;
            OnInvertChanged?.Invoke(InvertY);
            SaveSettings();
            Debug.Log($"Inverted Y-axis is now: {(inverted ? "On" : "Off")}");
        }
    }

    private void SaveSettings()
    {
        // Data is stored in PlayerPrefs.
        PlayerPrefs.SetInt("InvertY", InvertY ? 1 : 0);
        PlayerPrefs.Save();
    }

    private void LoadSettings()
    {
        InvertY = PlayerPrefs.GetInt("InvertY", 1) == 1;
        Debug.Log($"Setting for {InvertY} loaded!");
    }
}
