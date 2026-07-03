using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Filtering;
using System.Collections;

public class OCDSocketValidator : MonoBehaviour, IXRSelectFilter, IXRHoverFilter
{
    [Header("Validation")]
    [Tooltip("The exact tag of the object that belongs here.")]
    public string requiredTag;
    
    [Tooltip("CHECK THIS BOX if this socket is part of the Drawer Puzzle so it doesn't auto-lock!")]
    public bool isDrawerSocket = false;

    [Header("Feedback")]
    public AudioSource snapAudio;
    public ParticleSystem snapParticles;

    private UnityEngine.XR.Interaction.Toolkit.Interactors.XRSocketInteractor socket;

    public bool canProcess => true;

    [Header("Psychological Mechanics")]
    public bool enablePhantomDoubt = true;
    
    [Tooltip("The socket has a 1-in-X chance to doubt. (e.g., 3 means a 1 in 3 chance)")]
    public int chanceToDoubt = 3; 
    
    public GameObject doubtUICanvas; 
    public AudioSource doubtAudio;

    void Awake()
    {
        socket = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactors.XRSocketInteractor>();
    }

    void OnEnable()
    {
        socket.selectFilters.Add(this);
        socket.hoverFilters.Add(this);
        
        // ALWAYS listen for the snap so we can play audio and particles!
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
        // 1. Always play the feedback effects regardless of socket type
        if (snapAudio != null) snapAudio.Play();
        if (snapParticles != null) snapParticles.Play();

        OCDItemHighlight highlightScript = args.interactableObject.transform.GetComponent<OCDItemHighlight>();
        if (highlightScript != null) highlightScript.DisableHighlight();

        // 2. If this is a puzzle socket, STOP here. Let the Puzzle Manager handle the locking.
        if (isDrawerSocket) return;

        // 3. If it is a normal standalone socket, proceed with the GameManager and doubt mechanics
        if (OCDGameManager.Instance != null) OCDGameManager.Instance.ItemRestored();

        GameObject snappedItem = args.interactableObject.transform.gameObject;
        StartCoroutine(LockItemInPlace(snappedItem));
    }

   private IEnumerator LockItemInPlace(GameObject item)
    {
        yield return new WaitForSeconds(0.5f);

        bool randomDoubtTrigger = Random.Range(0, chanceToDoubt) == 0;

        if (enablePhantomDoubt && randomDoubtTrigger)
        {
            Debug.Log("Intrusive thought triggered! Rejecting item...");
            if (doubtUICanvas != null) doubtUICanvas.SetActive(true);
            if (doubtAudio != null) doubtAudio.Play();
            yield return new WaitForSeconds(1.5f);

            var socketComponent = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactors.XRSocketInteractor>();
            if (socketComponent != null)
            {
                socketComponent.enabled = false;
                Rigidbody rb = item.GetComponent<Rigidbody>();
                if (rb != null) rb.AddForce(transform.forward * 3f, ForceMode.Impulse);

                yield return new WaitForSeconds(0.5f);
                socketComponent.enabled = true; 
            }
            
            if (doubtUICanvas != null) doubtUICanvas.SetActive(false);
        }
        else
        {
            Rigidbody rb = item.GetComponent<Rigidbody>();
            if (rb != null) rb.isKinematic = true; 

            var grabInteractable = item.GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
            if (grabInteractable != null) grabInteractable.enabled = false;

            var socketComponent = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactors.XRSocketInteractor>();
            if (socketComponent != null) socketComponent.enabled = false;
            
            Debug.Log(item.name + " permanently locked.");
        }
    }
}