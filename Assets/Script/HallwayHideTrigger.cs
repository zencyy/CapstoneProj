using UnityEngine;
using TMPro;
using System.Collections;

public class HallwayHideTrigger : MonoBehaviour
{
    public HideMechanicManager manager;
    
    [Header("Trap Specifics")]
    public GameObject arrowIconToActivate;
    public GameObject cornerZoneToActivate;
    public GameObject[] barriersToDisable; 

    [Header("Teleport Settings")]
    [Tooltip("Drag the Empty GameObject here where the player should spawn after hiding")]
    public Transform teleportDestination; // ---> NEW: Slot for your Empty Object
    
    private void Start()
    {
        if (arrowIconToActivate != null) arrowIconToActivate.SetActive(false);
        if (cornerZoneToActivate != null) cornerZoneToActivate.SetActive(false);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") || other.CompareTag("MainCamera"))
        {
            // ---> AMENDED: Pass the designated destination instead of the trap's location
            manager.TriggerHallwayEvent(arrowIconToActivate, cornerZoneToActivate, barriersToDisable, teleportDestination);
            gameObject.SetActive(false); 
        }
    }
}