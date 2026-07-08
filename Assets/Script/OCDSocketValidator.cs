using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Filtering;
using System.Collections;
using TMPro; // Required for TextMeshPro subtitles

public class OCDSocketValidator : MonoBehaviour, IXRSelectFilter, IXRHoverFilter
{
    [Header("Validation")]
    [Tooltip("The exact tag of the object that belongs here.")]
    public string requiredTag;
    
    [Tooltip("CHECK THIS BOX if this socket is part of a Puzzle (Drawer/Books) so it doesn't auto-lock!")]
    public bool isDrawerSocket = false;

    [Header("Feedback")]
    public AudioSource snapAudio;
    public ParticleSystem snapParticles;

    private UnityEngine.XR.Interaction.Toolkit.Interactors.XRSocketInteractor socket;
    public bool canProcess => true;

    [Header("Psychological Mechanics")]
    public bool enablePhantomDoubt = true;
    
    // The script will randomly pick 0, 1, or 2 when the game starts
    private int randomizedMaxDoubts; 
    private int currentDoubtCount = 0;

    [Header("Central Subtitle UI")]
    [Tooltip("Drag your main Subtitle Text (TMP) object here")]
    public TMP_Text subtitleDisplay;

    [Header("First Doubt Sequence")]
    [TextArea(2, 3)]
    public string doubt1Text = "Wait, is this really right?";
    public AudioSource doubt1Audio;

    [Header("Second Doubt Sequence")]
    [TextArea(2, 3)]
    public string doubt2Text = "No, let me check it one more time.";
    public AudioSource doubt2Audio;

    void Awake()
    {
        socket = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactors.XRSocketInteractor>();
    }

    void Start()
    {
        randomizedMaxDoubts = Random.Range(0, 3);
    }

    void OnEnable()
    {
        socket.selectFilters.Add(this);
        socket.hoverFilters.Add(this);
        socket.selectEntered.AddListener(OnItemSnapped);
    }

    void OnDisable()
    {
        socket.selectFilters.Remove(this);
        socket.hoverFilters.Remove(this);
        socket.selectEntered.RemoveListener(OnItemSnapped);
    }

    public bool Process(UnityEngine.XR.Interaction.Toolkit.Interactors.IXRSelectInteractor interactor, UnityEngine.XR.Interaction.Toolkit.Interactables.IXRSelectInteractable interactable)
    {
        return interactable.transform.CompareTag(requiredTag);
    }

    public bool Process(UnityEngine.XR.Interaction.Toolkit.Interactors.IXRHoverInteractor interactor, UnityEngine.XR.Interaction.Toolkit.Interactables.IXRHoverInteractable interactable)
    {
        return interactable.transform.CompareTag(requiredTag);
    }

    private void OnItemSnapped(SelectEnterEventArgs args)
    {
        if (snapAudio != null) snapAudio.Play();
        if (snapParticles != null) snapParticles.Play();

        OCDItemHighlight highlightScript = args.interactableObject.transform.GetComponent<OCDItemHighlight>();
        if (highlightScript != null) highlightScript.DisableHighlight();

        GameObject snappedItem = args.interactableObject.transform.gameObject;

        if (enablePhantomDoubt && currentDoubtCount < randomizedMaxDoubts)
        {
            StartCoroutine(TriggerDoubt(snappedItem));
        }
        else
        {
            if (isDrawerSocket) return;

            if (OCDGameManager.Instance != null) OCDGameManager.Instance.ItemRestored();
            
            StartCoroutine(LockItemPermanently(snappedItem));
        }
    }

    private IEnumerator TriggerDoubt(GameObject item)
    {
        currentDoubtCount++;
        
        yield return new WaitForSeconds(0.5f);

        Debug.Log($"Intrusive thought triggered! Rejecting item... ({currentDoubtCount}/{randomizedMaxDoubts})");
        
        string activeText = "";
        AudioSource activeAudio = null;

        if (currentDoubtCount == 1)
        {
            activeText = doubt1Text;
            activeAudio = doubt1Audio;
        }
        else if (currentDoubtCount == 2)
        {
            activeText = doubt2Text;
            activeAudio = doubt2Audio;
        }

        // Display the text on the central subtitle system
        if (subtitleDisplay != null) subtitleDisplay.text = activeText;
        if (activeAudio != null) activeAudio.Play();
        
        float waitTime = 1.5f;
        if (activeAudio != null && activeAudio.clip != null)
        {
            waitTime = activeAudio.clip.length;
        }

        yield return new WaitForSeconds(waitTime);

        // Safely disable the socket to push the item out
        if (socket != null)
        {
            socket.enabled = false;
            Rigidbody rb = item.GetComponent<Rigidbody>();
            
            if (rb != null) 
            {
                rb.AddForce(Vector3.up * 1.5f + transform.forward * 3.5f, ForceMode.Impulse);
            }

            yield return new WaitForSeconds(0.5f);
            socket.enabled = true; 
        }
        
        // Clear the text, which will automatically hide your black background box
        if (subtitleDisplay != null) subtitleDisplay.text = "";
    }

    private IEnumerator LockItemPermanently(GameObject item)
    {
        yield return new WaitForSeconds(0.5f);

        Rigidbody rb = item.GetComponent<Rigidbody>();
        if (rb != null) rb.isKinematic = true; 

        var grabInteractable = item.GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
        if (grabInteractable != null) grabInteractable.enabled = false;

        if (socket != null) socket.enabled = false;
        
        Debug.Log(item.name + " permanently locked.");
    }
}