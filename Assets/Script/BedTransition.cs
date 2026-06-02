using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class BedTransition : MonoBehaviour
{
    [Header("Scene Settings")]
    public string wakeUpSceneName = "WakeUpScene"; 

    [Header("Fade Settings")]
    public Image fadeScreen;
    public float fadeDuration = 2f;

    [Header("UI & Gaze Setup")]
    public GameObject sleepPromptCanvas; 
    public float lookThreshold = 25f; 
    public float maxDistance = 4f;

    [Header("Input Setup")]
    public InputActionReference sleepButton;

    private Transform playerCamera;
    private bool isLookingAtBed = false;
    private bool isSleeping = false;

    void Start()
    {
        // Hide UI on start and find the VR headset
        if (sleepPromptCanvas != null) sleepPromptCanvas.SetActive(false);
        if (Camera.main != null) playerCamera = Camera.main.transform;
        
        // Ensure fade screen starts clear
        if (fadeScreen != null)
        {
            Color c = fadeScreen.color;
            c.a = 0f;
            fadeScreen.color = c;
        }
    }

    void OnEnable()
    {
        if (sleepButton != null)
        {
            sleepButton.action.Enable();
            sleepButton.action.started += TryGoToSleep;
        }
    }

    void OnDisable()
    {
        if (sleepButton != null)
        {
            sleepButton.action.started -= TryGoToSleep;
        }
    }

    void Update()
    {
        // If the fade sequence has already started, ignore everything else
        if (isSleeping || playerCamera == null) return;

        // Calculate distance and angle
        Vector3 directionToBed = (transform.position - playerCamera.position).normalized;
        float distance = Vector3.Distance(playerCamera.position, transform.position);
        float angle = Vector3.Angle(playerCamera.forward, directionToBed);

        isLookingAtBed = (angle < lookThreshold && distance < maxDistance);

        if (sleepPromptCanvas != null)
        {
            sleepPromptCanvas.SetActive(isLookingAtBed);
        }
    }

    private void TryGoToSleep(InputAction.CallbackContext context)
    {
        // Only trigger the fade if they are looking at the bed and aren't already sleeping
        if (isLookingAtBed && !isSleeping)
        {
            if (sleepPromptCanvas != null) sleepPromptCanvas.SetActive(false);
            StartCoroutine(SleepSequence());
        }
    }

    private IEnumerator SleepSequence()
    {
        isSleeping = true;
        float elapsedTime = 0f;
        
        if (fadeScreen != null)
        {
            Color fadeColor = fadeScreen.color;

            while (elapsedTime < fadeDuration)
            {
                elapsedTime += Time.deltaTime;
                fadeColor.a = Mathf.Clamp01(elapsedTime / fadeDuration);
                fadeScreen.color = fadeColor;
                yield return null;
            }
        }

        yield return new WaitForSeconds(1f);
        SceneManager.LoadScene(wakeUpSceneName);
    }
}