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

    [Header("Psychological Mechanics")]
    [Tooltip("Check this to enable the compulsive re-checking mechanic")]
    public bool enablePhantomDoubt = true;
    [Tooltip("The chance (0 to 1) that the item will be rejected")]
    [Range(0f, 1f)]
    public float doubtProbability = 1f; // 40% chance to fail
    public GameObject doubtUICanvas; // Drag your DoubtCanvas here
    public AudioSource doubtAudio;

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
        // 1. Wait a moment to let the socket pull the item in
        yield return new WaitForSeconds(0.5f);

        // 2. Roll the dice to see if an "Intrusive Thought" triggers
        if (enablePhantomDoubt && Random.value < doubtProbability)
        {
            Debug.Log("Intrusive thought triggered! Rejecting item...");
            
            // Show the UI and play the stressful sound
            if (doubtUICanvas != null) doubtUICanvas.SetActive(true);
            if (doubtAudio != null) doubtAudio.Play();

            // Give the player a second to realize what happened
            yield return new WaitForSeconds(1.5f);

            // Force the socket to drop the item
            var socket = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactors.XRSocketInteractor>();
            if (socket != null)
            {
                // Temporarily disable the socket to drop the item, then turn it back on
                socket.enabled = false;
                
                // Add a tiny physical bump so it visibly pops out of the socket
                Rigidbody rb = item.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.AddForce(transform.forward * 3f, ForceMode.Impulse);
                }

                yield return new WaitForSeconds(0.5f);
                socket.enabled = true; // Ready for the player to try again
            }
            
            // Hide the UI text after they grab it again
            if (doubtUICanvas != null) doubtUICanvas.SetActive(false);
        }
        else
        {
            // 3. SUCCESS! The compulsion is satisfied. Lock it permanently.
            Rigidbody rb = item.GetComponent<Rigidbody>();
            if (rb != null) rb.isKinematic = true; 

            var grabInteractable = item.GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
            if (grabInteractable != null) grabInteractable.enabled = false;

            var socket = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactors.XRSocketInteractor>();
            if (socket != null) socket.enabled = false;
            
            Debug.Log(item.name + " permanently locked.");
        }
    }
}