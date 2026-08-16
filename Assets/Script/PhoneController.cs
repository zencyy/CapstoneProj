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
    public TMP_Text photoPromptText; 
    public TMP_Text subtitleDisplay;
    public AudioSource voiceOverAudioSource;

    [Header("Prompt Messages")]
    public string openGalleryPrompt = "Press 'Trigger' to view photos";
    public string nextPhotoPrompt = "Press 'Trigger' for next photo";
    public string closePhonePrompt = "Press 'Trigger' to switch off phone";
    // ---> NEW: Prompt for when the phone is turned off after viewing the gallery
    public string switchOnPhonePrompt = "Press 'Trigger' to switch on phone";

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
    
    private bool isDialoguePlaying = false;
    private bool[] hasViewedPhoto;

    void Start()
    {
        if (uiPromptCanvas != null) uiPromptCanvas.SetActive(false);
        if (photoPromptCanvas != null) photoPromptCanvas.SetActive(false);
        if (subtitleDisplay != null) subtitleDisplay.text = "";
        
        if (photoPromptText != null) photoPromptText.text = openGalleryPrompt;
        
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
        else if (alarmDismissed && currentPhotoIndex >= 0)
        {
            if (uiPromptCanvas != null) uiPromptCanvas.SetActive(false);
            
            if (photoPromptCanvas != null) 
            {
                photoPromptCanvas.SetActive(isLookingAtPhone && !isDialoguePlaying);
            }
        }
        else
        {
            if (uiPromptCanvas != null) uiPromptCanvas.SetActive(false);
            if (photoPromptCanvas != null) photoPromptCanvas.SetActive(false);
        }
    }

    private void OnButtonPressed(InputAction.CallbackContext context)
    {
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
        if (currentPhotoIndex >= 0 && currentPhotoIndex < reassurancePhotos.Length)
        {
            if (reassurancePhotos[currentPhotoIndex].photoCanvas != null)
            {
                reassurancePhotos[currentPhotoIndex].photoCanvas.SetActive(false);
            }
        }

        currentPhotoIndex++;

        if (currentPhotoIndex < reassurancePhotos.Length)
        {
            if (reassurancePhotos[currentPhotoIndex].photoCanvas != null)
            {
                reassurancePhotos[currentPhotoIndex].photoCanvas.SetActive(true);
            }

            if (phoneScreenLight != null) phoneScreenLight.SetActive(true);

            if (!hasViewedPhoto[currentPhotoIndex])
            {
                hasViewedPhoto[currentPhotoIndex] = true;
                StartCoroutine(PlayDialogueAndWait(currentPhotoIndex));
            }
            else
            {
                if (subtitleDisplay != null) subtitleDisplay.text = "";
                if (voiceOverAudioSource != null) voiceOverAudioSource.Stop();
                
                UpdatePromptText(currentPhotoIndex);
            }
        }
        else
        {
            // The player has clicked past the final photo and turned off the phone
            currentPhotoIndex = -1;
            
            if (phoneScreenLight != null) phoneScreenLight.SetActive(false);
            if (subtitleDisplay != null) subtitleDisplay.text = "";
            if (voiceOverAudioSource != null) voiceOverAudioSource.Stop();
            
            // ---> NEW: Set the prompt to tell them how to turn it back on
            if (photoPromptText != null) photoPromptText.text = switchOnPhonePrompt;
        }
    }

    private IEnumerator PlayDialogueAndWait(int index)
    {
        isDialoguePlaying = true;

        if (voiceOverAudioSource != null && reassurancePhotos[index].voiceOver != null)
        {
            voiceOverAudioSource.Stop();
            voiceOverAudioSource.PlayOneShot(reassurancePhotos[index].voiceOver);
        }

        SubtitleSequence[] lines = reassurancePhotos[index].subtitleLines;

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
            yield return new WaitForSeconds(reassurancePhotos[index].voiceOver.length);
        }

        if (subtitleDisplay != null) subtitleDisplay.text = "";
        
        UpdatePromptText(index);

        isDialoguePlaying = false; 
    }

    private void UpdatePromptText(int index)
    {
        if (photoPromptText != null)
        {
            if (index == reassurancePhotos.Length - 1)
            {
                photoPromptText.text = closePhonePrompt;
            }
            else
            {
                photoPromptText.text = nextPhotoPrompt;
            }
        }
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