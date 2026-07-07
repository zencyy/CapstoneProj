using UnityEngine;
using UnityEngine.InputSystem;
using TMPro; 
using System.Collections; // Required for Coroutines


public class PhoneAlarmController : MonoBehaviour
{
    [Header("Core References")]
    public AudioSource phoneAudio;
    public GameObject phoneScreenLight;
    public GameObject uiPromptCanvas; 
    
    [Header("Reassurance Gallery UI")]
    public GameObject photoPromptCanvas; 
    public TMP_Text subtitleDisplay;
    public AudioSource voiceOverAudioSource;

    [Tooltip("Add your photo canvases, subtitles, and voiceovers here in order.")]
    public PhotoClue[] reassurancePhotos; 

    [Header("Alarm Settings")]
    [Tooltip("How many seconds after the scene starts before the phone rings?")]
    public float timeBeforeAlarm = 5.0f;

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
    
    private int currentPhotoIndex = -1; 
    
    // NEW: We use this to track the subtitle timer so we can stop it if they click 'Next' early
    private Coroutine subtitleTimerCoroutine;

    void Start()
    {
        if (uiPromptCanvas != null) uiPromptCanvas.SetActive(false);
        if (photoPromptCanvas != null) photoPromptCanvas.SetActive(false);
        if (subtitleDisplay != null) subtitleDisplay.text = "";
        
        foreach (PhotoClue clue in reassurancePhotos)
        {
            if (clue.photoCanvas != null) clue.photoCanvas.SetActive(false);
        }
        
        if (Camera.main != null) playerCamera = Camera.main.transform;

        // Start the alarm timer automatically
        Invoke("TriggerAlarm", timeBeforeAlarm);
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

    [ContextMenu("Test Trigger Alarm")]
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

        if (isRinging)
        {
            if (uiPromptCanvas != null) uiPromptCanvas.SetActive(isLookingAtPhone);
            if (photoPromptCanvas != null) photoPromptCanvas.SetActive(false);
        }
        else if (alarmDismissed && currentPhotoIndex == -1) 
        {
            if (uiPromptCanvas != null) uiPromptCanvas.SetActive(false);
            if (photoPromptCanvas != null) photoPromptCanvas.SetActive(isLookingAtPhone);
        }
        else
        {
            if (uiPromptCanvas != null) uiPromptCanvas.SetActive(false);
            if (photoPromptCanvas != null) photoPromptCanvas.SetActive(false);
        }
    }

    private void OnButtonPressed(InputAction.CallbackContext context)
    {
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
        // 1. Hide the current photo
        if (currentPhotoIndex >= 0 && currentPhotoIndex < reassurancePhotos.Length)
        {
            if (reassurancePhotos[currentPhotoIndex].photoCanvas != null)
            {
                reassurancePhotos[currentPhotoIndex].photoCanvas.SetActive(false);
            }
        }

        // 2. Stop any existing subtitle timer if they clicked early
        if (subtitleTimerCoroutine != null)
        {
            StopCoroutine(subtitleTimerCoroutine);
        }

        // 3. Move to the next photo
        currentPhotoIndex++;

        // 4. Did we run out of photos?
        if (currentPhotoIndex < reassurancePhotos.Length)
        {
            // Show the next photo
            if (reassurancePhotos[currentPhotoIndex].photoCanvas != null)
            {
                reassurancePhotos[currentPhotoIndex].photoCanvas.SetActive(true);
            }

            // Update the Subtitle Text
            if (subtitleDisplay != null)
            {
                subtitleDisplay.text = reassurancePhotos[currentPhotoIndex].subtitleText;
            }
            
            if (phoneScreenLight != null) phoneScreenLight.SetActive(true);

            // Play the Voiceover Audio and start the clearing timer
            if (voiceOverAudioSource != null && reassurancePhotos[currentPhotoIndex].voiceOver != null)
            {
                voiceOverAudioSource.Stop(); 
                voiceOverAudioSource.PlayOneShot(reassurancePhotos[currentPhotoIndex].voiceOver);
                
                // NEW: Start the timer to clear the text based on the exact length of the audio clip
                subtitleTimerCoroutine = StartCoroutine(ClearSubtitleAfterDelay(reassurancePhotos[currentPhotoIndex].voiceOver.length));
            }
        }
        else
        {
            // Reached the end of the list. Close the gallery.
            currentPhotoIndex = -1;
            
            if (phoneScreenLight != null) phoneScreenLight.SetActive(false);
            if (subtitleDisplay != null) subtitleDisplay.text = "";
            if (voiceOverAudioSource != null) voiceOverAudioSource.Stop();
        }
    }

    // NEW: The timer function that waits for the audio to finish
    private IEnumerator ClearSubtitleAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        
        if (subtitleDisplay != null)
        {
            subtitleDisplay.text = "";
        }
    }
}

[System.Serializable]
public struct PhotoClue
{
    public GameObject photoCanvas;
    
    [TextArea(2, 4)]
    public string subtitleText;
    
    public AudioClip voiceOver;
}
