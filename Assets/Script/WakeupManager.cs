using UnityEngine;
using System.Collections;
using Unity.XR.CoreUtils; // Required for XR Origin

public class WakeUpManager : MonoBehaviour
{
    [Header("VR Rig References")]
    public XROrigin xrOrigin;
    
    [Header("Getting Out Of Bed")]
    [Tooltip("Drag an Empty GameObject here to represent where they stand when they get out of bed")]
    public Transform floorSpawnPoint;
    public float timeSpentInBed = 17.0f;

    void Start()
    {
        // Start the sequence when the scene loads
        StartCoroutine(GetOutOfBedSequence());
    }

    private IEnumerator GetOutOfBedSequence()
    {
        // Let the player look around from the bed for a few seconds
        yield return new WaitForSeconds(timeSpentInBed);

        // 1. Move the XR Origin to the floor next to the bed
        xrOrigin.transform.position = floorSpawnPoint.position;
        
        // ---> AMENDED: Force the player to stand upright! 
        // This strips away the backward tilt (X) from the bed and matches the facing direction (Y) of your spawn point.
        xrOrigin.transform.rotation = Quaternion.Euler(0, floorSpawnPoint.eulerAngles.y, 0);

        // 2. Change the tracking mode back to Floor so they have their real-world standing height back
        xrOrigin.RequestedTrackingOriginMode = XROrigin.TrackingOriginMode.Floor;
        
        Debug.Log("Player has gotten out of bed and is now standing upright!");
    }
}