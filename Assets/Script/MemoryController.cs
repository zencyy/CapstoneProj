using UnityEngine;
using UnityEngine.InputSystem;

public class MemoryController : MonoBehaviour
{
    [Header("Input Setup")]
    [Tooltip("The button used to flash the memory (e.g., XRI LeftHand/Primary Button)")]
    public InputActionReference memoryButtonAction;

    [Header("UI Reference")]
    [Tooltip("Drag your MemoryCanvas here")]
    public GameObject memoryCanvas;

    private void Awake()
    {
        // Ensure the memory is hidden when the game starts
        if (memoryCanvas != null)
        {
            memoryCanvas.SetActive(false);
        }
    }

    private void OnEnable()
    {
        // Listen for when the button is pressed down
        if (memoryButtonAction != null)
        {
            memoryButtonAction.action.started += ShowMemory;
            memoryButtonAction.action.canceled += HideMemory;
        }
    }

    private void OnDisable()
    {
        // Stop listening when the object is destroyed
        if (memoryButtonAction != null)
        {
            memoryButtonAction.action.started -= ShowMemory;
            memoryButtonAction.action.canceled -= HideMemory;
        }
    }

    private void ShowMemory(InputAction.CallbackContext context)
    {
        if (memoryCanvas != null) memoryCanvas.SetActive(true);
    }

    private void HideMemory(InputAction.CallbackContext context)
    {
        if (memoryCanvas != null) memoryCanvas.SetActive(false);
    }
}