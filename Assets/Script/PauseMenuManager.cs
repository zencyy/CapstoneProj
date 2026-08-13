using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem; // Required for reading controller inputs

public class PauseMenuManager : MonoBehaviour
{
    [Header("UI Setup")]
    [Tooltip("Drag the PauseMenuCanvas here")]
    public GameObject pauseMenuCanvas;

    [Header("Input Setup")]
    [Tooltip("Select the input action you want to trigger the pause menu (e.g., Left Hand Menu Button)")]
    public InputActionReference pauseButton;

    [Header("Scene Management")]
    [Tooltip("Type the exact name of your Main Menu scene")]
    public string mainMenuSceneName = "MainMenu";

    private bool isPaused = false;

    void Start()
    {
        // Ensure the menu is hidden when the game starts
        if (pauseMenuCanvas != null) pauseMenuCanvas.SetActive(false);
    }

    void OnEnable()
    {
        // Listen for the button press
        if (pauseButton != null)
        {
            pauseButton.action.performed += TogglePauseMenu;
            pauseButton.action.Enable();
        }
    }

    void OnDisable()
    {
        // Stop listening when this object is turned off
        if (pauseButton != null)
        {
            pauseButton.action.performed -= TogglePauseMenu;
            pauseButton.action.Disable();
        }
    }

    private void TogglePauseMenu(InputAction.CallbackContext context)
    {
        if (isPaused) ResumeGame();
        else PauseGame();
    }

    public void PauseGame()
    {
        isPaused = true;
        pauseMenuCanvas.SetActive(true);
        Time.timeScale = 0f; // This stops all physics, animations, and time-based scripts
    }

    public void ResumeGame()
    {
        isPaused = false;
        pauseMenuCanvas.SetActive(false);
        Time.timeScale = 1f; // This resumes time
    }

    public void BackToMainMenu()
    {
        // CRITICAL: You must set time scale back to 1 before loading a new scene, 
        // otherwise your main menu will be frozen!
        Time.timeScale = 1f; 
        SceneManager.LoadScene(mainMenuSceneName);
    }

    public void QuitGame()
    {
        Debug.Log("Quitting Game...");
        Application.Quit();
    }
}