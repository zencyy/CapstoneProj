using UnityEngine;
using System.Collections;
using System.Collections.Generic; 
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

    [Header("Psychological Mechanics")]
    [Tooltip("How many times must the player get it perfectly right before the game accepts it?")]
    public int requiredSuccesses = 2; 
    [Tooltip("The text that shows when they get it right, but are forced to repeat it.")]
    public string puzzleDoubtMessage = "Are you sure? Do it again.";
    public AudioSource puzzleDoubtAudio;
    
    private int currentSuccessCount = 0;

    [Header("Drawer Mechanics")]
    public UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable drawerGrabScript;
    public AudioSource successAudio;
    public AudioSource errorAudio; 

    private bool isSolved = false;
    private bool isChecking = false;
    private int lastFilledCount = 0; 

    private List<GameObject> gluedItems = new List<GameObject>();
    private List<Vector3> gluedOffsets = new List<Vector3>();
    private List<Quaternion> gluedRotations = new List<Quaternion>();

    void Start()
    {
        if (drawerGrabScript != null) drawerGrabScript.enabled = false;
        if (feedbackCanvas != null) feedbackCanvas.SetActive(false);

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
            // NEW LOGIC: They got the items in the right order! But is the compulsion satisfied?
            currentSuccessCount++;

            if (currentSuccessCount < requiredSuccesses)
            {
                // THEY MUST REPEAT IT
                if (feedbackText != null) feedbackText.text = puzzleDoubtMessage;
                if (puzzleDoubtAudio != null) puzzleDoubtAudio.Play();
                
                StartCoroutine(RejectEntirePuzzle());
                // We stop here so the success code doesn't run yet!
            }
            else
            {
                // TRUE SUCCESS
                isSolved = true;
                if (feedbackText != null) feedbackText.text = successMessage;
                if (successAudio != null) successAudio.Play();
                
                if (OCDGameManager.Instance != null)
                {
                    for (int i = 0; i < drawerSockets.Length; i++)
                    {
                        OCDGameManager.Instance.ItemRestored();
                    }
                }
                
                foreach (var socket in drawerSockets) 
                { 
                    if (socket.hasSelection)
                    {
                        GameObject item = socket.firstInteractableSelected.transform.gameObject;

                        var itemGrab = item.GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
                        if (itemGrab != null) Destroy(itemGrab);

                        Rigidbody itemRb = item.GetComponent<Rigidbody>();
                        if (itemRb != null) Destroy(itemRb);

                        Collider[] colliders = item.GetComponentsInChildren<Collider>();
                        foreach (Collider col in colliders) col.enabled = false;

                        item.transform.SetParent(null, true);

                        gluedItems.Add(item);
                        gluedOffsets.Add(transform.InverseTransformPoint(item.transform.position));
                        gluedRotations.Add(Quaternion.Inverse(transform.rotation) * item.transform.rotation);
                    }
                    socket.enabled = false; 
                }
                
                Rigidbody drawerRb = GetComponent<Rigidbody>();
                if (drawerRb != null) drawerRb.isKinematic = false;

                if (drawerGrabScript != null) drawerGrabScript.enabled = true; 

                Debug.Log("Puzzle Solved! Items mathematically glued to the drawer.");
                isChecking = false;
            }
        }
        else
        {
            // THEY GOT THE ORDER WRONG
            if (feedbackText != null) feedbackText.text = errorMessage;
            if (errorAudio != null) errorAudio.Play();
            isChecking = false;
        }
    }

    // NEW COROUTINE: Violently rejects all items so the player has to put them back
    private IEnumerator RejectEntirePuzzle()
    {
        // Let the player read the frustrating "Do it again" text for 2 seconds
        yield return new WaitForSeconds(2.0f);

        // Pop all 3 items out simultaneously
        foreach (var socket in drawerSockets)
        {
            if (socket.hasSelection)
            {
                GameObject item = socket.firstInteractableSelected.transform.gameObject;
                
                // Disable socket to release the item
                socket.enabled = false;
                
                Rigidbody rb = item.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    // 1. Shoot them straight UP in World Space to clear the drawer walls (adjust the 4f if they don't pop high enough)
                    Vector3 upwardPop = Vector3.up * 4f;

                    // 2. Add a random horizontal spread so they don't land in the exact same spot
                    Vector3 randomScatter = new Vector3(Random.Range(-1.5f, 1.5f), 0, Random.Range(-1.5f, 1.5f));

                    // Apply the pop and scatter
                    rb.AddForce(upwardPop + randomScatter, ForceMode.Impulse);

                    // 3. Add random rotation (Torque) so the jars tumble naturally in the air
                    rb.AddTorque(Random.insideUnitSphere * 3f, ForceMode.Impulse);
                }
            }
        }

        // Wait 1.5 seconds before turning the sockets back on so the items have time to fall completely out of the trigger zones
        yield return new WaitForSeconds(1.5f);

        foreach (var socket in drawerSockets)
        {
            socket.enabled = true;
        }

        if (feedbackCanvas != null) feedbackCanvas.SetActive(false);
        
        Debug.Log($"Macro Doubt Triggered! The player must re-do the puzzle. ({currentSuccessCount}/{requiredSuccesses})");
        
        // Reset the check so the player can try again
        isChecking = false; 
    }
}