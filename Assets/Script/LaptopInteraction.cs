using UnityEngine;

public class LaptopInteraction : MonoBehaviour
{
    [Header("UI Reference")]
    public GameObject ticketUICanvas;

    // This method will be called when the player clicks the laptop
    public void ToggleTicketUI()
    {
        if (ticketUICanvas != null)
        {
            // This flips the active state (if off, turns on; if on, turns off)
            bool isActive = ticketUICanvas.activeSelf;
            ticketUICanvas.SetActive(!isActive);
        }
    }
}