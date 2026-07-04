using UnityEngine;
using UnityEngine.InputSystem;

public class PhoneAlarmController : MonoBehaviour
{
    [Header("References")]
    public AudioSource phoneAudio;
    public GameObject phoneScreenLight;
    public GameObject uiPromptCanvas; 
    
    [Header("Reassurance Gallery UI")]
    public GameObject photoPromptCanvas; 
    
    [Tooltip("Add all your photo UI canvases here in the order you want them to appear.")]
    public GameObject[] reassurancePhotos; 

    [Header("Gaze Settings")]
    public float lookThreshold = 25f; 
    public float maxDistance = 3f;
    
    [Header("Input")]
    public InputActionReference interactButton;

    [HideInInspector]
    public bool isRinging = false; 
    
    private Transform playerCamera;
    private bool isLookingAtPhone = false;
    private bool alarmDismissed = false;
    
    // -1 means the gallery is currently closed. 0 is the first photo, 1 is the second, etc.
    private int currentPhotoIndex = -1; 

    void Start()
    {
        if (uiPromptCanvas != null) uiPromptCanvas.SetActive(false);
        if (photoPromptCanvas != null) photoPromptCanvas.SetActive(false);
        
        // Ensure ALL photos are completely hidden when the game starts
        foreach (GameObject photo in reassurancePhotos)
        {
            if (photo != null) photo.SetActive(false);
        }
        
        if (Camera.main != null) playerCamera = Camera.main.transform;
    }

    void OnEnable()
    {
        if (interactButton != null)
        {
            interactButton.action.Enable();
            interactButton.action.started += OnButtonPressed;
        }
    }

    void OnDisable()
    {
        if (interactButton != null)
        {
            interactButton.action.started -= OnButtonPressed;
        }
    }

    public void TriggerAlarm()
    {
        isRinging = true;
        alarmDismissed = false;
        
        if (phoneAudio != null) phoneAudio.Play();
        if (phoneScreenLight != null) phoneScreenLight.SetActive(true);
    }

    void Update()
    {
        if (playerCamera == null) return;

        Vector3 directionToPhone = (transform.position - playerCamera.position).normalized;
        float distance = Vector3.Distance(playerCamera.position, transform.position);
        float angle = Vector3.Angle(playerCamera.forward, directionToPhone);

        isLookingAtPhone = (angle < lookThreshold && distance < maxDistance);

        // STATE 1: Ringing
        if (isRinging)
        {
            if (uiPromptCanvas != null) uiPromptCanvas.SetActive(isLookingAtPhone);
            if (photoPromptCanvas != null) photoPromptCanvas.SetActive(false);
        }
        // STATE 2: Alarm off, Gallery Closed
        else if (alarmDismissed && currentPhotoIndex == -1) 
        {
            if (uiPromptCanvas != null) uiPromptCanvas.SetActive(false);
            if (photoPromptCanvas != null) photoPromptCanvas.SetActive(isLookingAtPhone);
        }
        // STATE 3: Reading Photos
        else
        {
            if (uiPromptCanvas != null) uiPromptCanvas.SetActive(false);
            if (photoPromptCanvas != null) photoPromptCanvas.SetActive(false);
        }
    }

    private void OnButtonPressed(InputAction.CallbackContext context)
    {
        // Notice the new check here: we let them click if they are looking at the phone, 
        // OR if the gallery is already open (so they don't have to perfectly stare at the phone to flip pages)
        if (isLookingAtPhone || currentPhotoIndex != -1)
        {
            if (isRinging)
            {
                TurnOffAlarm();
            }
            else if (alarmDismissed)
            {
                CyclePhotos();
            }
        }
    }

    private void TurnOffAlarm()
    {
        isRinging = false;
        alarmDismissed = true; 
        
        if (phoneAudio != null) phoneAudio.Stop();
        if (phoneScreenLight != null) phoneScreenLight.SetActive(false);
    }

    private void CyclePhotos()
    {
        // 1. If a photo is currently open, hide it
        if (currentPhotoIndex >= 0 && currentPhotoIndex < reassurancePhotos.Length)
        {
            if (reassurancePhotos[currentPhotoIndex] != null)
            {
                reassurancePhotos[currentPhotoIndex].SetActive(false);
            }
        }

        // 2. Move to the next photo in line
        currentPhotoIndex++;

        // 3. Did we run out of photos?
        if (currentPhotoIndex < reassurancePhotos.Length)
        {
            // We have another photo! Show it.
            if (reassurancePhotos[currentPhotoIndex] != null)
            {
                reassurancePhotos[currentPhotoIndex].SetActive(true);
            }
        }
        else
        {
            // We reached the end of the list. Close the gallery.
            currentPhotoIndex = -1;
        }
    }
}