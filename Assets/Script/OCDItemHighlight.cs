using UnityEngine;

public class OCDItemHighlight : MonoBehaviour
{
    [Header("Highlight Settings")]
    [Tooltip("How close the player's camera needs to be to trigger the glow (in meters).")]
    public float highlightDistance = 1.5f; 
    
    [Tooltip("The normal material of the object.")]
    public Material defaultMaterial;
    
    [Tooltip("A glowing version of the material.")]
    public Material highlightMaterial;
    
    private Renderer itemRenderer;
    private Transform playerCamera;
    private bool isPlacedCorrectly = false;

    void Start()
    {
        itemRenderer = GetComponent<Renderer>();
        
        // Automatically find the VR player's head
        if (Camera.main != null)
        {
            playerCamera = Camera.main.transform;
        }
    }

    void Update()
    {
        // Stop checking if it's already in the correct socket or missing components
        if (isPlacedCorrectly || playerCamera == null || itemRenderer == null) return;

        // Calculate the distance from the player's head to this item
        float distance = Vector3.Distance(transform.position, playerCamera.position);

        if (distance <= highlightDistance)
        {
            itemRenderer.material = highlightMaterial;
        }
        else
        {
            itemRenderer.material = defaultMaterial;
        }
    }

    // This stops the item from highlighting once it is organized
    public void DisableHighlight()
    {
        isPlacedCorrectly = true;
        if (itemRenderer != null)
        {
            itemRenderer.material = defaultMaterial; 
        }
    }
}