// OptionsUIController.cs
using UnityEngine;
using UnityEngine.UIElements;
using System;

/// <summary>
/// Controls the interactive elements of the Options Menu UI.
/// </summary>
public class OptionsUIController : MonoBehaviour
{
    // A public Action that other classes can subscribe to
    public Action OnBackButtonClicked;

    // References to the UI elements
    private UIDocument optionsMenuDoc;
    private Toggle invertYToggle;
    private Button backButton;

    private void OnEnable()
    {
        // Get the UIDocument component attached to this same GameObject
        optionsMenuDoc = GetComponent<UIDocument>();

        if (optionsMenuDoc == null)
        {
            Debug.LogError("Options UI is missing reference to UI document!");
            return;
        }

        VisualElement root = optionsMenuDoc.rootVisualElement;

        // Get references to the interactive elements by name
        invertYToggle = root.Q<Toggle>("InvertYToggle");
        backButton = root.Q<Button>("BackBtn");

        // Set the initial state of the toggle from the OptionsManager
        if (OptionsManager.Instance != null && invertYToggle != null)
        {
            invertYToggle.value = OptionsManager.Instance.InvertY;
        }

        // Register event handlers
        if (invertYToggle != null)
        {
            invertYToggle.RegisterValueChangedCallback(OnInvertYToggleChanged);
        }

        // Make sure the button is not null before trying to add the event listener
        if (backButton != null)
        {
            // Subscribe the private method to the back button's clicked event
            backButton.clicked += OnBackClicked;
        }
        else
        {
            Debug.LogError("BackBtn not found in the Options UI. Check your UXML file!");
        }
    }

    private void OnDisable()
    {
        // Only proceed if we have a valid reference
        if (optionsMenuDoc == null || optionsMenuDoc.rootVisualElement == null)
        {
            return;
        }

        // Unregister event handlers
        if (invertYToggle != null)
        {
            invertYToggle.UnregisterValueChangedCallback(OnInvertYToggleChanged);
        }

        if (backButton != null)
        {
            // Unsubscribe the private method from the back button's clicked event
            backButton.clicked -= OnBackClicked;
        }
    }

    /// <summary>
    /// Handles the value change of the Invert Y-axis toggle.
    /// </summary>
    private void OnInvertYToggleChanged(ChangeEvent<bool> evt)
    {
        // Update the setting in the OptionsManager
        if (OptionsManager.Instance != null)
        {
            OptionsManager.Instance.SetInvertY(evt.newValue);
        }
    }

    /// <summary>
    /// Handles the back button click and invokes the public action.
    /// </summary>
    private void OnBackClicked()
    {
        Debug.Log("Back button clicked!");
        // Invoke the public action, which the other controllers are listening for
        OnBackButtonClicked?.Invoke();
    }
}
