using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class CutsceneManager : MonoBehaviour
{
    [Header("Player Movement Setup")]
    public MonoBehaviour playerMovementScript; 

    [Header("Fade Effect")]
    public Image fadeScreen;
    public float fadeDuration = 3f;

    [Header("The Phone Controller")]
    public PhoneAlarmController phoneAlarm; // We link the new script here

    [Header("The Cat")]
    public AudioSource catAudio;
    public Animator catAnimator; 

    void Start()
    {
        if (playerMovementScript != null) playerMovementScript.enabled = false;
        
        if (fadeScreen != null) 
        {
            fadeScreen.color = Color.black;
            StartCoroutine(PlayWakeUpCutscene());
        }
    }

    private IEnumerator PlayWakeUpCutscene()
    {
        yield return new WaitForSeconds(1f);

        // 1. Fade from black to clear
        float elapsedTime = 0f;
        Color color = fadeScreen.color;
        
        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.deltaTime;
            color.a = Mathf.Lerp(1f, 0f, elapsedTime / fadeDuration);
            fadeScreen.color = color;
            yield return null;
        }

        yield return new WaitForSeconds(1.5f);

        // 2. Tell the new Phone script to start the alarm
        if (phoneAlarm != null)
        {
            phoneAlarm.TriggerAlarm();
            
            // This is the magic line! The cutscene FREEZES here until the player turns off the phone.
            yield return new WaitUntil(() => phoneAlarm.isRinging == false);
        }

        yield return new WaitForSeconds(1f);

        // 3. The Cat triggers AFTER the phone is silenced
        if (catAudio != null) catAudio.Play();
        if (catAnimator != null) catAnimator.SetTrigger("Action"); 

        yield return new WaitForSeconds(2.5f);

        // 4. Unlock player
        if (playerMovementScript != null) playerMovementScript.enabled = true;
    }
}