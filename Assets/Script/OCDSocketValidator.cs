using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Filtering;
using System.Collections;

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

    [Header("First Doubt Sequence")]
    public GameObject doubt1UICanvas; 
    public AudioSource doubt1Audio;

    [Header("Second Doubt Sequence")]
    public GameObject doubt2UICanvas; 
    public AudioSource doubt2Audio;

    void Awake()
    {
        socket = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactors.XRSocketInteractor>();
    }

    void Start()
    {
        // Randomly decide how many times this specific socket will doubt (0, 1, or 2)
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

        // Check against our randomized max limit instead of a hardcoded number
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
        
        // Determine which UI and Audio to play based on which doubt this is
        GameObject activeCanvas = null;
        AudioSource activeAudio = null;

        if (currentDoubtCount == 1)
        {
            activeCanvas = doubt1UICanvas;
            activeAudio = doubt1Audio;
        }
        else if (currentDoubtCount == 2)
        {
            activeCanvas = doubt2UICanvas;
            activeAudio = doubt2Audio;
        }

        // Turn on the selected UI and Audio
        if (activeCanvas != null) activeCanvas.SetActive(true);
        if (activeAudio != null) activeAudio.Play();
        
        // Calculate the exact length of the voiceover (default to 1.5s if missing)
        float waitTime = 1.5f;
        if (activeAudio != null && activeAudio.clip != null)
        {
            waitTime = activeAudio.clip.length;
        }

        // Wait for the voiceover to completely finish before spitting the item out
        yield return new WaitForSeconds(waitTime);

        // Safely disable the socket to push the item out
        if (socket != null)
        {
            socket.enabled = false;
            Rigidbody rb = item.GetComponent<Rigidbody>();
            
            if (rb != null) 
            {
                // Pop the item slightly up and forward
                rb.AddForce(Vector3.up * 1.5f + transform.forward * 3.5f, ForceMode.Impulse);
            }

            yield return new WaitForSeconds(0.5f);
            socket.enabled = true; 
        }
        
        // Turn the UI off again
        if (activeCanvas != null) activeCanvas.SetActive(false);
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