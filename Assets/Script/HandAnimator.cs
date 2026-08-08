using UnityEngine;
using UnityEngine.InputSystem;

public class HandAnimator : MonoBehaviour
{
    [Header("Input Actions")]
    public InputActionProperty pinchAnimationAction; // The Trigger button
    public InputActionProperty gripAnimationAction;  // The Grip button

    private Animator anim;

    void Start()
    {
        anim = GetComponent<Animator>();
    }

    void Update()
    {
        if (anim == null) return;

        // Read how hard the player is squeezing the trigger (0.0 to 1.0)
        float triggerValue = pinchAnimationAction.action.ReadValue<float>();
        anim.SetFloat("Trigger", triggerValue);

        // Read how hard the player is squeezing the grip (0.0 to 1.0)
        float gripValue = gripAnimationAction.action.ReadValue<float>();
        anim.SetFloat("Grip", gripValue);
    }
}