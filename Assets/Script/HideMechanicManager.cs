using UnityEngine;
using TMPro;
using System.Collections;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal; 
using Unity.XR.CoreUtils; 

public class HideMechanicManager : MonoBehaviour
{
    [Header("Game Manager Link")]
    public AnxietyMinigameManager gameManager; 
    
    [Header("VR Rig Reference")]
    public XROrigin xrOrigin; 

    [Header("Level Elements")]
    public GameObject npcSpawner; 

    [Header("UI Elements")]
    public GameObject hideEventHUD; 
    public TMP_Text hideInstructionText;
    
    [Header("Settings")]
    public float timeToReachCorner = 10f;
    public float timeToStayInCorner = 3f;
    public float anxietyPenaltyOnFail = 20f;
    
    [Header("Arrow Animation Settings")]
    public float arrowRotateSpeed = 90f;
    public float arrowHoverSpeed = 4f;
    public float arrowHoverHeight = 0.15f;

    [Header("Arrow Light Settings")]
    public float arrowBaseLightIntensity = 5f;
    public float arrowLightPulseAmount = 3f;

    [Header("Post Processing (VFX)")]
    public Volume globalVolume;
    public float targetLensDistortion = -0.5f;
    public float vfxFadeDuration = 1.0f;

    [Header("Audio (Experiential)")]
    public AudioSource heartbeatAudio;
    public float targetHeartbeatVolume = 1.0f;

    private LensDistortion lensDistortion;
    private Coroutine vfxCoroutine;

    private bool isEventActive = false;
    private bool playerIsInCorner = false;
    private bool isSafeToLeave = false; 
    private Coroutine activeTimerCoroutine;

    private GameObject currentArrowIcon;
    private Light currentArrowLight; 
    private GameObject currentCornerZone;
    private GameObject[] currentBarriers; 
    private Transform currentTeleportDestination; 
    private Vector3 arrowOriginalLocalPos; 

    // ---> NEW: Expose this publicly so the Finish Line knows if the player is currently trapped
    public bool IsHiding => isEventActive;

    void Start()
    {
        if (hideEventHUD != null) hideEventHUD.SetActive(false);

        if (globalVolume != null && globalVolume.profile != null)
        {
            globalVolume.profile.TryGet(out lensDistortion);
            if (lensDistortion != null) lensDistortion.intensity.value = 0f; 
        }

        if (heartbeatAudio != null)
        {
            heartbeatAudio.volume = 0f;
            heartbeatAudio.loop = true;
        }
    }

    private void Update()
    {
        if (isEventActive && currentArrowIcon != null)
        {
            float sineWave = Mathf.Sin(Time.time * arrowHoverSpeed);

            currentArrowIcon.transform.Rotate(Vector3.up * arrowRotateSpeed * Time.deltaTime, Space.World);
            float newY = arrowOriginalLocalPos.y + (sineWave * arrowHoverHeight);
            currentArrowIcon.transform.localPosition = new Vector3(arrowOriginalLocalPos.x, newY, arrowOriginalLocalPos.z);

            if (currentArrowLight != null)
            {
                currentArrowLight.intensity = arrowBaseLightIntensity + (sineWave * arrowLightPulseAmount);
            }
        }
    }

    public void TriggerHallwayEvent(GameObject arrowIcon, GameObject cornerZone, GameObject[] barriers, Transform teleportDest)
    {
        if (isEventActive) return; 
        isEventActive = true;
        playerIsInCorner = false;
        isSafeToLeave = false; 

        currentArrowIcon = arrowIcon;
        currentCornerZone = cornerZone;
        currentBarriers = barriers; 
        currentTeleportDestination = teleportDest; 

        gameManager.isPaused = true;
        gameManager.mainHUD.SetActive(false);

        if (npcSpawner != null) npcSpawner.SetActive(false);

        if (currentArrowIcon != null) 
        {
            arrowOriginalLocalPos = currentArrowIcon.transform.localPosition;
            currentArrowLight = currentArrowIcon.GetComponentInChildren<Light>();
            currentArrowIcon.SetActive(true);
        }
        
        if (currentCornerZone != null) currentCornerZone.SetActive(true);
        hideEventHUD.SetActive(true);

        if (currentBarriers != null)
        {
            foreach (GameObject barrier in currentBarriers)
            {
                if (barrier != null) barrier.SetActive(false);
            }
        }

        if (vfxCoroutine != null) StopCoroutine(vfxCoroutine);
        vfxCoroutine = StartCoroutine(AnimateVFX(true, vfxFadeDuration));

        if (activeTimerCoroutine != null) StopCoroutine(activeTimerCoroutine);
        activeTimerCoroutine = StartCoroutine(ReachCornerRoutine());
    }

    private IEnumerator ReachCornerRoutine()
    {
        float timer = timeToReachCorner;

        while (timer > 0)
        {
            timer -= Time.deltaTime;
            hideInstructionText.text = "HIDE IN THE CORNER!\n" + Mathf.Ceil(timer).ToString() + "s";
            yield return null;
        }

        // ---> AMENDED: They failed to reach the corner in time!
        yield return StartCoroutine(FailEventRoutine("TOO SLOW!"));
    }

    public void EnteredCorner()
    {
        if (!isEventActive) return;
        playerIsInCorner = true;
        
        if (activeTimerCoroutine != null) StopCoroutine(activeTimerCoroutine);
        activeTimerCoroutine = StartCoroutine(StayInCornerRoutine());
    }

    public void ExitedCorner()
    {
        if (!isEventActive || !playerIsInCorner) return;
        playerIsInCorner = false;
        
        if (isSafeToLeave)
        {
            TeleportPlayer();
            EndHideEvent(); 
        }
        else
        {
            // ---> AMENDED: They stepped out before the 3 seconds finished!
            if (activeTimerCoroutine != null) StopCoroutine(activeTimerCoroutine);
            activeTimerCoroutine = StartCoroutine(FailEventRoutine("YOU MOVED!"));
        }
    }

    private IEnumerator StayInCornerRoutine()
    {
        float timer = timeToStayInCorner;

        while (timer > 0)
        {
            timer -= Time.deltaTime;
            hideInstructionText.text = "STAY STILL...\n" + Mathf.Ceil(timer).ToString() + "s";
            yield return null;
        }

        hideInstructionText.text = "SAFE.\nSTEP OUT.";
        isSafeToLeave = true; 
    }

    // ---> NEW: A universal function to handle failing the minigame
    private IEnumerator FailEventRoutine(string failMessage)
    {
        hideInstructionText.text = failMessage;
        gameManager.ModifyAnxiety(-anxietyPenaltyOnFail); // Drains the bar meter
        gameManager.TriggerHitFlash();
        
        yield return new WaitForSeconds(1.5f);
        
        // Teleport them and end the sequence just like a normal exit
        TeleportPlayer();
        EndHideEvent();
    }

    // ---> NEW: Extracted the physical movement so it can be called on a Win OR a Fail
    private void TeleportPlayer()
    {
        if (xrOrigin != null && currentTeleportDestination != null)
        {
            CharacterController cc = xrOrigin.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;

            xrOrigin.MoveCameraToWorldLocation(currentTeleportDestination.position);
            xrOrigin.MatchOriginUpCameraForward(Vector3.up, currentTeleportDestination.forward);

            if (cc != null) cc.enabled = true;
        }

        if (currentBarriers != null)
        {
            foreach (GameObject barrier in currentBarriers)
            {
                if (barrier != null) barrier.SetActive(true);
            }
        }
    }

    private void EndHideEvent()
    {
        isEventActive = false;
        hideEventHUD.SetActive(false);
        
        if (npcSpawner != null) npcSpawner.SetActive(true);

        if (currentArrowIcon != null) 
        {
            currentArrowIcon.transform.localPosition = arrowOriginalLocalPos;
            currentArrowIcon.SetActive(false);
        }
        
        if (currentCornerZone != null) currentCornerZone.SetActive(false);
        
        currentArrowIcon = null;
        currentArrowLight = null;
        currentCornerZone = null;
        currentBarriers = null;
        currentTeleportDestination = null;
        
        if (vfxCoroutine != null) StopCoroutine(vfxCoroutine);
        vfxCoroutine = StartCoroutine(AnimateVFX(false, vfxFadeDuration));
        
        gameManager.mainHUD.SetActive(true);
        gameManager.isPaused = false; 
    }

    private IEnumerator AnimateVFX(bool isFadingIn, float duration)
    {
        float timer = 0f;
        
        float startDistortion = lensDistortion != null ? lensDistortion.intensity.value : 0f;
        float endDistortion = isFadingIn ? targetLensDistortion : 0f;

        float startAudio = heartbeatAudio != null ? heartbeatAudio.volume : 0f;
        float endAudio = isFadingIn ? targetHeartbeatVolume : 0f;

        if (isFadingIn && heartbeatAudio != null && !heartbeatAudio.isPlaying)
        {
            heartbeatAudio.Play();
        }

        while (timer < duration)
        {
            timer += Time.deltaTime;
            float t = timer / duration;
            
            if (lensDistortion != null) lensDistortion.intensity.value = Mathf.Lerp(startDistortion, endDistortion, t);
            if (heartbeatAudio != null) heartbeatAudio.volume = Mathf.Lerp(startAudio, endAudio, t);
            
            yield return null;
        }
        
        if (lensDistortion != null) lensDistortion.intensity.value = endDistortion;
        if (heartbeatAudio != null)
        {
            heartbeatAudio.volume = endAudio;
            if (!isFadingIn) heartbeatAudio.Stop(); 
        }
    }
}