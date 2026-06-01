using UnityEngine;
using UnityEngine.InputSystem;

public class PhoneAlarmController : MonoBehaviour
{
    [Header("References")]
    public AudioSource phoneAudio;
    public GameObject phoneScreenLight;
    public GameObject uiPromptCanvas; // Drag your new PhonePromptCanvas here
    
    [Header("Gaze Settings")]
    [Tooltip("How close the phone needs to be to the center of the screen to count as 'looking' (in degrees)")]
    public float lookThreshold = 25f; 
    [Tooltip("How close the player needs to be to interact (in meters)")]
    public float maxDistance = 3f;
    
    [Header("Input")]
    [Tooltip("The button used to turn off the alarm (e.g., XRI LeftHand/Primary Button)")]
    public InputActionReference turnOffButton;

    // This lets the CutsceneManager know if it's still ringing
    [HideInInspector]
    public bool isRinging = false; 
    
    private Transform playerCamera;
    private bool isLookingAtPhone = false;

    void Start()
    {
        if (uiPromptCanvas != null) uiPromptCanvas.SetActive(false);
        if (Camera.main != null) playerCamera = Camera.main.transform;
    }

    void OnEnable()
    {
        // Listen for the button press
        if (turnOffButton != null)
        {
            turnOffButton.action.Enable();
            turnOffButton.action.started += OnButtonPressed;
        }
    }

    void OnDisable()
    {
        if (turnOffButton != null)
        {
            turnOffButton.action.started -= OnButtonPressed;
        }
    }

    public void TriggerAlarm()
    {
        isRinging = true;
        if (phoneAudio != null) phoneAudio.Play();
        if (phoneScreenLight != null) phoneScreenLight.SetActive(true);
    }

    void Update()
    {
        // If the alarm is off, hide the UI and do nothing
        if (!isRinging || playerCamera == null) 
        {
            if (uiPromptCanvas != null && uiPromptCanvas.activeSelf) uiPromptCanvas.SetActive(false);
            return;
        }

        // Calculate the direction and distance from the camera to the phone
        Vector3 directionToPhone = (transform.position - playerCamera.position).normalized;
        float distance = Vector3.Distance(playerCamera.position, transform.position);
        
        // Calculate the angle between where the player is looking and where the phone is
        float angle = Vector3.Angle(playerCamera.forward, directionToPhone);

        // If the angle is small, they are looking directly at it!
        isLookingAtPhone = (angle < lookThreshold && distance < maxDistance);

        // Show or hide the UI based on whether they are looking
        if (uiPromptCanvas != null)
        {
            uiPromptCanvas.SetActive(isLookingAtPhone);
        }
    }

    private void OnButtonPressed(InputAction.CallbackContext context)
    {
        // Only turn it off if it's currently ringing AND they are looking right at it
        if (isRinging && isLookingAtPhone)
        {
            TurnOffAlarm();
        }
    }

    private void TurnOffAlarm()
    {
        isRinging = false;
        
        if (phoneAudio != null) phoneAudio.Stop();
        if (phoneScreenLight != null) phoneScreenLight.SetActive(false);
        if (uiPromptCanvas != null) uiPromptCanvas.SetActive(false);
        
        Debug.Log("Player successfully silenced the phone!");
    }
}