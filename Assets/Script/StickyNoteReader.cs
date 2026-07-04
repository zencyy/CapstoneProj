using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class StickyNoteReader : MonoBehaviour
{
    [Header("UI Elements")]
    [Tooltip("The small text that says 'Press to read'")]
    public GameObject hoverPromptUI; 
    
    [Tooltip("The large UI canvas showing the actual clue")]
    public GameObject bigClueUI;     

    [Header("Gaze Settings")]
    [Tooltip("How close the player needs to be to see the prompt (in meters)")]
    public float viewingDistance = 3f;

    [Header("Audio (Optional)")]
    public AudioSource paperRustleSound; 

    private UnityEngine.XR.Interaction.Toolkit.Interactables.XRSimpleInteractable interactable;
    private bool isReading = false;
    private Camera mainCamera;
    private Collider noteCollider;

    void Awake()
    {
        interactable = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRSimpleInteractable>();
        noteCollider = GetComponent<Collider>();
        mainCamera = Camera.main; // Automatically finds the player's headset camera
        
        if (hoverPromptUI != null) hoverPromptUI.SetActive(false);
        if (bigClueUI != null) bigClueUI.SetActive(false);
    }

    void OnEnable()
    {
        if (interactable == null) return;
        
        // We only listen for the click now. The hover is handled by the camera!
        interactable.selectEntered.AddListener(OnSelectEnter);
    }

    void OnDisable()
    {
        if (interactable == null) return;
        interactable.selectEntered.RemoveListener(OnSelectEnter);
    }

    void Update()
    {
        // If the note is currently open, or we are missing parts, stop here
        if (isReading || mainCamera == null || noteCollider == null) return;

        // Shoot an invisible raycast from the exact center of the player's headset
        Ray gazeRay = new Ray(mainCamera.transform.position, mainCamera.transform.forward);
        RaycastHit hit;

        bool isLookingAtNote = false;

        // If the player's gaze hits something within the viewing distance...
        if (Physics.Raycast(gazeRay, out hit, viewingDistance))
        {
            // ...and that something is this exact sticky note...
            if (hit.collider == noteCollider)
            {
                isLookingAtNote = true; // They are looking at it!
            }
        }

        // Show or hide the prompt based on where their eyes are pointing
        if (hoverPromptUI != null)
        {
            // Only update if the state needs to change (saves performance)
            if (hoverPromptUI.activeSelf != isLookingAtNote)
            {
                hoverPromptUI.SetActive(isLookingAtNote);
            }
        }
    }

    private void OnSelectEnter(SelectEnterEventArgs args)
    {
        isReading = !isReading; 

        if (isReading)
        {
            // OPENING THE NOTE
            if (hoverPromptUI != null) hoverPromptUI.SetActive(false); 
            if (bigClueUI != null) bigClueUI.SetActive(true); 
            if (paperRustleSound != null) paperRustleSound.Play();
        }
        else
        {
            // CLOSING THE NOTE
            if (bigClueUI != null) bigClueUI.SetActive(false); 
            
            // The Update loop will automatically handle bringing the prompt back if they are still looking at it!
        }
    }
}