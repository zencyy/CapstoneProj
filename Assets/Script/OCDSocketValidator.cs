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
    
    [Tooltip("How many times will the socket reject the item before accepting it?")]
    public int maxDoubts = 3; 
    
    public GameObject doubtUICanvas; 
    public AudioSource doubtAudio;

    // We use this to track how many times this specific socket has doubted
    private int currentDoubtCount = 0;

    void Awake()
    {
        socket = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactors.XRSocketInteractor>();
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
        // 1. Play standard feedback
        if (snapAudio != null) snapAudio.Play();
        if (snapParticles != null) snapParticles.Play();

        OCDItemHighlight highlightScript = args.interactableObject.transform.GetComponent<OCDItemHighlight>();
        if (highlightScript != null) highlightScript.DisableHighlight();

        GameObject snappedItem = args.interactableObject.transform.gameObject;

        // 2. CHECK DOUBT: Has the player done it enough times yet?
        if (enablePhantomDoubt && currentDoubtCount < maxDoubts)
        {
            StartCoroutine(TriggerDoubt(snappedItem));
        }
        else
        {
            // 3. SUCCESSFUL PLACEMENT
            if (isDrawerSocket) return;

            if (OCDGameManager.Instance != null) OCDGameManager.Instance.ItemRestored();
            
            // FIX: Start the Coroutine instead of calling it directly!
            StartCoroutine(LockItemPermanently(snappedItem));
        }
    }

    private IEnumerator TriggerDoubt(GameObject item)
    {
        // Add to the doubt counter
        currentDoubtCount++;
        
        yield return new WaitForSeconds(0.5f);

        Debug.Log($"Intrusive thought triggered! Rejecting item... ({currentDoubtCount}/{maxDoubts})");
        
        if (doubtUICanvas != null) doubtUICanvas.SetActive(true);
        if (doubtAudio != null) doubtAudio.Play();
        
        yield return new WaitForSeconds(1.5f);

        // Safely disable the socket to push the item out
        if (socket != null)
        {
            socket.enabled = false;
            Rigidbody rb = item.GetComponent<Rigidbody>();
            if (rb != null) rb.AddForce(transform.forward * 4.5f, ForceMode.Impulse);

            yield return new WaitForSeconds(0.5f);
            socket.enabled = true; 
        }
        
        if (doubtUICanvas != null) doubtUICanvas.SetActive(false);
    }

    private IEnumerator LockItemPermanently(GameObject item)
    {
        // FIX: Give the XR socket 0.5 seconds to smoothly slide the item into perfect position
        yield return new WaitForSeconds(0.5f);

        Rigidbody rb = item.GetComponent<Rigidbody>();
        if (rb != null) rb.isKinematic = true; 

        var grabInteractable = item.GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
        if (grabInteractable != null) grabInteractable.enabled = false;

        if (socket != null) socket.enabled = false;
        
        Debug.Log(item.name + " permanently locked.");
    }
}