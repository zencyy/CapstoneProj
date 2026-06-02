using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Filtering; // Required for Unity 6 XRIT
using System.Collections;
public class OCDSocketValidator : MonoBehaviour, IXRSelectFilter, IXRHoverFilter
{
    [Header("Validation")]
    [Tooltip("The exact tag of the object that belongs here.")]
    public string requiredTag;

    [Header("Feedback")]
    public AudioSource snapAudio;
    public ParticleSystem snapParticles;

    private UnityEngine.XR.Interaction.Toolkit.Interactors.XRSocketInteractor socket;

    // Required by the filter interfaces to allow processing
    public bool canProcess => true;

    void Awake()
    {
        socket = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactors.XRSocketInteractor>();
    }

    void OnEnable()
    {
        // Inject this script as the logic filter for the socket
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

    // Unity 6 XRIT Select Validation
    public bool Process(UnityEngine.XR.Interaction.Toolkit.Interactors.IXRSelectInteractor interactor, UnityEngine.XR.Interaction.Toolkit.Interactables.IXRSelectInteractable interactable)
    {
        return interactable.transform.CompareTag(requiredTag);
    }

    // Unity 6 XRIT Hover Validation
    public bool Process(UnityEngine.XR.Interaction.Toolkit.Interactors.IXRHoverInteractor interactor, UnityEngine.XR.Interaction.Toolkit.Interactables.IXRHoverInteractable interactable)
    {
        return interactable.transform.CompareTag(requiredTag);
    }

    private void OnItemSnapped(SelectEnterEventArgs args)
    {
        if (snapAudio != null) snapAudio.Play();
        if (snapParticles != null) snapParticles.Play();

        // Turn off the glowing highlight
        OCDItemHighlight highlightScript = args.interactableObject.transform.GetComponent<OCDItemHighlight>();
        if (highlightScript != null)
        {
            highlightScript.DisableHighlight();
        }

        OCDGameManager.Instance.ItemRestored();

        // NEW: Get the 3D model that was just placed and start locking it
        GameObject snappedItem = args.interactableObject.transform.gameObject;
        StartCoroutine(LockItemInPlace(snappedItem));
    }

   private IEnumerator LockItemInPlace(GameObject item)
    {
        // 1. Wait half a second to let the socket smoothly pull the item in and align it perfectly
        yield return new WaitForSeconds(0.5f);

        // 2. Freeze the physical body FIRST so gravity doesn't pull it down
        Rigidbody rb = item.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true; 
        }

        // 3. Turn off the Grab script so the player can't pick it up anymore
        var grabInteractable = item.GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
        if (grabInteractable != null)
        {
            grabInteractable.enabled = false;
        }

        // 4. Turn off the Socket itself so it permanently shuts down and doesn't try to grab anything else
        var socket = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactors.XRSocketInteractor>();
        if (socket != null)
        {
            socket.enabled = false;
        }
        
        Debug.Log(item.name + " has been permanently locked in its correct place.");
    }
}