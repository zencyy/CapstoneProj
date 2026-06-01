using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Filtering; // Required for Unity 6 XRIT

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

        // Find the highlight script on the item that just snapped in
        OCDItemHighlight highlightScript = args.interactableObject.transform.GetComponent<OCDItemHighlight>();
        
        // If it has the script, disable the glow forever
        if (highlightScript != null)
        {
            highlightScript.DisableHighlight();
        }

        OCDGameManager.Instance.ItemRestored();
    }
}