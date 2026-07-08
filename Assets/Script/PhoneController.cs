using UnityEngine;
using UnityEngine.InputSystem;
using TMPro; 
using System.Collections; 

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
    
    // NEW: Tracks if a photo's dialogue is currently running to block input
    private bool isDialoguePlaying = false;
    
    // NEW: Remembers which photos have already been viewed
    private bool[] hasViewedPhoto;

    void Start()
    {
        if (uiPromptCanvas != null) uiPromptCanvas.SetActive(false);
        if (photoPromptCanvas != null) photoPromptCanvas.SetActive(false);
        if (subtitleDisplay != null) subtitleDisplay.text = "";
        
        // Initialize the memory array to match the number of photos you have
        hasViewedPhoto = new bool[reassurancePhotos.Length];
        
        foreach (PhotoClue clue in reassurancePhotos)
        {
            if (clue.photoCanvas != null) clue.photoCanvas.SetActive(false);
        }
        
        if (Camera.main != null) playerCamera = Camera.main.transform;

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
        // NEW: If the dialogue is playing, completely ignore the button press!
        if (isDialoguePlaying) return;

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

        // 2. Move to the next photo
        currentPhotoIndex++;

        // 3. Did we run out of photos?
        if (currentPhotoIndex < reassurancePhotos.Length)
        {
            // Show the next photo visually
            if (reassurancePhotos[currentPhotoIndex].photoCanvas != null)
            {
                reassurancePhotos[currentPhotoIndex].photoCanvas.SetActive(true);
            }

            if (phoneScreenLight != null) phoneScreenLight.SetActive(true);

            // NEW: Check if this is the FIRST time seeing this photo
            if (!hasViewedPhoto[currentPhotoIndex])
            {
                // Mark it as viewed so it never plays again
                hasViewedPhoto[currentPhotoIndex] = true;
                
                // Start the master coroutine that locks input and plays the sequence
                StartCoroutine(PlayDialogueAndWait(currentPhotoIndex));
            }
            else
            {
                // If they've already seen it, ensure no audio or text is lingering
                if (subtitleDisplay != null) subtitleDisplay.text = "";
                if (voiceOverAudioSource != null) voiceOverAudioSource.Stop();
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

    // NEW: Unified Coroutine that handles audio, subtitles, and input locking
    private IEnumerator PlayDialogueAndWait(int index)
    {
        // 1. Lock the player's input immediately
        isDialoguePlaying = true;

        // 2. Start the audio
        if (voiceOverAudioSource != null && reassurancePhotos[index].voiceOver != null)
        {
            voiceOverAudioSource.Stop();
            voiceOverAudioSource.PlayOneShot(reassurancePhotos[index].voiceOver);
        }

        SubtitleSequence[] lines = reassurancePhotos[index].subtitleLines;

        // 3. Play the subtitles if they exist
        if (lines != null && lines.Length > 0)
        {
            foreach (SubtitleSequence line in lines)
            {
                if (subtitleDisplay != null) subtitleDisplay.text = line.text;
                yield return new WaitForSeconds(line.duration);
            }
        }
        else if (reassurancePhotos[index].voiceOver != null)
        {
            // Failsafe: If you forgot to type subtitles, just wait for the audio clip to finish
            yield return new WaitForSeconds(reassurancePhotos[index].voiceOver.length);
        }

        // 4. Clean up and unlock input
        if (subtitleDisplay != null) subtitleDisplay.text = "";
        isDialoguePlaying = false; 
    }
}

[System.Serializable]
public struct SubtitleSequence
{
    [TextArea(2, 3)]
    public string text;
    [Tooltip("How many seconds this specific line should stay on screen")]
    public float duration;
}

[System.Serializable]
public struct PhotoClue
{
    public GameObject photoCanvas;
    public AudioClip voiceOver;
    
    [Tooltip("Add each line of dialogue and how long it should appear here.")]
    public SubtitleSequence[] subtitleLines; 
}