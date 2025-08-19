using UnityEngine;
using UnityEngine.UIElements;

public class OptionsUIController : MonoBehaviour
{
    [SerializeField] private UIDocument uiDocument;

    // UI references
    private Toggle invertYToggle;
    private Button backButton;

    private void OnEnable()
    {
        if (uiDocument == null)
        {
            Debug.LogError("Options UI is missing reference to UI document!");
            return;
        }

        VisualElement root = uiDocument.rootVisualElement;

        // Get references to the elements by name
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

        if (backButton != null)
        {
            backButton.clicked += OnBackButtonClicked;
        }
    }

    private void OnDisable()
    {
        if (uiDocument == null || uiDocument.rootVisualElement == null)
        {
            return;
        }

        // Unregister event handlers to prevent memory leaks
        if (invertYToggle != null)
        {
            invertYToggle.UnregisterValueChangedCallback(OnInvertYToggleChanged);
        }
        if (backButton != null)
        {
            backButton.clicked -= OnBackButtonClicked;
        }
    }

    /// <summary>
    /// Handles the value change of the Invert Y-axis toggle.
    /// </summary>
    /// <param name="evt">The change event containing the new value.</param>
    private void OnInvertYToggleChanged(ChangeEvent<bool> evt)
    {
        // Update the setting in the OptionsManager
        if (OptionsManager.Instance != null)
        {
            OptionsManager.Instance.SetInvertY(evt.newValue);
        }
    }

    private void OnBackButtonClicked()
    {
        Debug.Log("Back button clicked!");

    }
}
