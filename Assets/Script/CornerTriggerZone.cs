using UnityEngine;
using TMPro;
using System.Collections;

public class CornerHideTrigger : MonoBehaviour
{
    public HideMechanicManager manager;
    
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") || other.CompareTag("MainCamera")) manager.EnteredCorner();
    }
    
    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player") || other.CompareTag("MainCamera")) manager.ExitedCorner();
    }
}