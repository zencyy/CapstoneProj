using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class StickyNoteReader : MonoBehaviour
{
    [Header("UI Elements")]
    [Tooltip("The small text that says 'Press to read'")]
    public GameObject hoverPromptUI; 
    
    [Tooltip("The large UI canvas showing the actual clue")]
    public GameObject bigClueUI;     

    [Header("Audio (Optional)")]
    public AudioSource paperRustleSound; 

    private UnityEngine.XR.Interaction.Toolkit.Interactables.XRSimpleInteractable interactable;
    private bool isReading = false;

    void Awake()
    {
        interactable = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRSimpleInteractable>();
        
        // Ensure both UIs are hidden when the game starts
        if (hoverPromptUI != null) hoverPromptUI.SetActive(false);
        if (bigClueUI != null) bigClueUI.SetActive(false);
    }

    void OnEnable()
    {
        // Listen for the XR system's hover and click events
        interactable.hoverEntered.AddListener(OnHoverEnter);
        interactable.hoverExited.AddListener(OnHoverExit);
        interactable.selectEntered.AddListener(OnSelectEnter);
    }

    void OnDisable()
    {
        interactable.hoverEntered.RemoveListener(OnHoverEnter);
        interactable.hoverExited.RemoveListener(OnHoverExit);
        interactable.selectEntered.RemoveListener(OnSelectEnter);
    }

    private void OnHoverEnter(HoverEnterEventArgs args)
    {
        // Only show the 'Press to read' prompt if they aren't currently reading the big note
        if (!isReading && hoverPromptUI != null)
        {
            hoverPromptUI.SetActive(true);
        }
    }

    private void OnHoverExit(HoverExitEventArgs args)
    {
        // Hide the prompt when they look away
        if (hoverPromptUI != null)
        {
            hoverPromptUI.SetActive(false);
        }
    }

    private void OnSelectEnter(SelectEnterEventArgs args)
    {
        // Toggle the reading state
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
            
            // If their laser is still pointing at the sticky note after closing, bring the prompt back
            if (interactable.isHovered && hoverPromptUI != null) 
            {
                hoverPromptUI.SetActive(true);
            }
        }
    }
}