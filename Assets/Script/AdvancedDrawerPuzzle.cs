using UnityEngine;
using System.Collections;
using System.Collections.Generic; // Required for Lists
using TMPro;

public class AdvancedDrawerPuzzle : MonoBehaviour
{
    [Header("Puzzle Configuration")]
    public UnityEngine.XR.Interaction.Toolkit.Interactors.XRSocketInteractor[] drawerSockets;
    public GameObject[] expectedItems; 

    [Header("Feedback UI")]
    public GameObject feedbackCanvas; 
    public TMP_Text feedbackText;     
    public string successMessage = "Perfect.";
    public string errorMessage = "Order is incorrect.";

    [Header("Drawer Mechanics")]
    public UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable drawerGrabScript;
    public AudioSource successAudio;
    public AudioSource errorAudio; 

    private bool isSolved = false;
    private bool isChecking = false;
    private int lastFilledCount = 0; 

    // NEW: We use these lists to mathematically glue the items to the drawer
    private List<GameObject> gluedItems = new List<GameObject>();
    private List<Vector3> gluedOffsets = new List<Vector3>();
    private List<Quaternion> gluedRotations = new List<Quaternion>();

    void Start()
    {
        if (drawerGrabScript != null) drawerGrabScript.enabled = false;
        if (feedbackCanvas != null) feedbackCanvas.SetActive(false);

        // Completely freeze the drawer's physics so bumps don't move it
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null) rb.isKinematic = true;
    }

    void Update()
    {
        if (isSolved) return; 

        int filledSockets = 0;
        foreach (var socket in drawerSockets)
        {
            if (socket.hasSelection) filledSockets++;
        }

        if (!isChecking)
        {
            if (filledSockets == drawerSockets.Length && lastFilledCount < drawerSockets.Length)
            {
                StartCoroutine(EvaluatePuzzle());
            }
            else if (filledSockets < drawerSockets.Length && feedbackCanvas != null && feedbackCanvas.activeSelf)
            {
                feedbackCanvas.SetActive(false);
            }
        }

        lastFilledCount = filledSockets;
    }

    // NEW: This forces the items to follow the drawer flawlessly, completely bypassing Unity Physics
    void LateUpdate()
    {
        for (int i = 0; i < gluedItems.Count; i++)
        {
            if (gluedItems[i] != null)
            {
                gluedItems[i].transform.position = transform.TransformPoint(gluedOffsets[i]);
                gluedItems[i].transform.rotation = transform.rotation * gluedRotations[i];
            }
        }
    }

    private IEnumerator EvaluatePuzzle()
    {
        isChecking = true;
        yield return new WaitForSeconds(0.5f); 

        bool isCorrect = true;

        for (int i = 0; i < drawerSockets.Length; i++)
        {
            if (!drawerSockets[i].hasSelection)
            {
                isCorrect = false;
                break;
            }

            GameObject itemInSocket = drawerSockets[i].firstInteractableSelected.transform.gameObject;
            
            if (itemInSocket != expectedItems[i])
            {
                isCorrect = false;
                break; 
            }
        }

        if (feedbackCanvas != null) feedbackCanvas.SetActive(true);

        if (isCorrect)
        {
            isSolved = true;
            if (feedbackText != null) feedbackText.text = successMessage;
            if (successAudio != null) successAudio.Play();
            
            foreach (var socket in drawerSockets) 
            { 
                if (socket.hasSelection)
                {
                    GameObject item = socket.firstInteractableSelected.transform.gameObject;

                    // 1. Destroy XR Grab so the system immediately forgets about the item
                    var itemGrab = item.GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
                    if (itemGrab != null) Destroy(itemGrab);

                    // 2. Destroy Rigidbody so it cannot fall down or explode
                    Rigidbody itemRb = item.GetComponent<Rigidbody>();
                    if (itemRb != null) Destroy(itemRb);

                    // 3. Turn off colliders so they don't clip into the drawer
                    Collider[] colliders = item.GetComponentsInChildren<Collider>();
                    foreach (Collider col in colliders) col.enabled = false;

                    // 4. Detach from ALL parents. This guarantees the drawer's weird scale never stretches the item!
                    item.transform.SetParent(null, true);

                    // 5. Calculate and save the exact distance and angle from the drawer
                    gluedItems.Add(item);
                    gluedOffsets.Add(transform.InverseTransformPoint(item.transform.position));
                    gluedRotations.Add(Quaternion.Inverse(transform.rotation) * item.transform.rotation);
                }

                // Safely turn off the socket
                socket.enabled = false; 
            }
            
            // Unfreeze the drawer's physics so the player can push it along the track
            Rigidbody drawerRb = GetComponent<Rigidbody>();
            if (drawerRb != null) drawerRb.isKinematic = false;

            // Unlock drawer so the player can grab it
            if (drawerGrabScript != null) drawerGrabScript.enabled = true; 

            Debug.Log("Puzzle Solved! Items mathematically glued to the drawer.");
        }
        else
        {
            if (feedbackText != null) feedbackText.text = errorMessage;
            if (errorAudio != null) errorAudio.Play();
        }
        
        isChecking = false;
    }
}
public class ItemSticky : MonoBehaviour
{
    public Transform drawer;
    private Vector3 positionOffset;
    private Quaternion rotationOffset;

    void Start()
    {
        // The exact millisecond this is attached, remember the math distance from the drawer
        if (drawer != null)
        {
            positionOffset = drawer.InverseTransformPoint(transform.position);
            rotationOffset = Quaternion.Inverse(drawer.rotation) * transform.rotation;
        }
    }

    void LateUpdate()
    {
        // Every single frame, force the item to maintain that exact distance
        if (drawer != null)
        {
            transform.position = drawer.TransformPoint(positionOffset);
            transform.rotation = drawer.rotation * rotationOffset;
        }
    }
}