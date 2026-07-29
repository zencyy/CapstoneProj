using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Env3.Anxiety
{
    /// <summary>
    /// Sequences the whole beat: player reaches the corner past the door, MainDude comes
    /// over, the conversation runs, the player breaks down.
    ///
    /// Everything is wired in code rather than through inspector UnityEvents so the flow
    /// is readable in one place, but the individual components stay usable on their own.
    /// </summary>
    public class AfterPartyEncounter : MonoBehaviour
    {
        [Header("Parts")]
        public PlayerAreaTrigger approachTrigger;
        public MainDudeApproach mainDude;
        public AnxietyDialogueController dialogue;

        [Header("Player")]
        [Tooltip("XR rig root. Empty = first object tagged Player.")]
        public Transform playerRig;
        [Tooltip("Freeze move/turn/teleport while the conversation runs. Being unable to walk away is the point.")]
        public bool freezeLocomotion = true;

        [Header("Internal monologue")]
        [Tooltip("Thought shown the moment the player steps into the room, before Sean notices them.")]
        [TextArea(2, 3)] public string thoughtOnEnter = "I really hope nobody comes over...";
        [Tooltip("Thought shown as Sean spots them and starts walking over.")]
        [TextArea(2, 3)] public string thoughtOnSpotted = "Oh no... that's Sean Tay from school...";
        [Tooltip("How long each thought stays up after it finishes typing.")]
        public float thoughtHold = 1.6f;

        [Header("Events")]
        [Tooltip("Fired after the breakdown. Hook a fade, a scene load, or whatever comes next.")]
        public UnityEvent onEncounterFinished;

        readonly List<MonoBehaviour> _frozen = new List<MonoBehaviour>();
        Vector3 _dudeHome;
        Quaternion _dudeHomeRotation;
        bool _started;
        bool _locomotionFrozen;

        void Awake()
        {
            ResolveReferences();

            if (mainDude != null)
            {
                _dudeHome = mainDude.transform.position;
                _dudeHomeRotation = mainDude.transform.rotation;
            }

            if (approachTrigger != null) approachTrigger.onPlayerEntered.AddListener(OnPlayerReachedCorner);
            if (mainDude != null)
            {
                mainDude.onApproachStarted.AddListener(OnApproachStarted);
                mainDude.onReachedPlayer.AddListener(OnMainDudeArrived);
            }
            if (dialogue != null) dialogue.onBreakdown.AddListener(OnBreakdown);
        }

        void OnDestroy()
        {
            if (approachTrigger != null) approachTrigger.onPlayerEntered.RemoveListener(OnPlayerReachedCorner);
            if (mainDude != null)
            {
                mainDude.onApproachStarted.RemoveListener(OnApproachStarted);
                mainDude.onReachedPlayer.RemoveListener(OnMainDudeArrived);
            }
            if (dialogue != null) dialogue.onBreakdown.RemoveListener(OnBreakdown);
        }

        void ResolveReferences()
        {
            if (approachTrigger == null) approachTrigger = GetComponentInChildren<PlayerAreaTrigger>(true);
            if (dialogue == null) dialogue = GetComponentInChildren<AnxietyDialogueController>(true);
            if (mainDude == null) mainDude = FindObjectOfType<MainDudeApproach>();

            if (playerRig == null)
            {
                var tagged = GameObject.FindGameObjectWithTag("Player");
                if (tagged != null) playerRig = tagged.transform;
            }

            if (dialogue != null && dialogue.speaker == null) dialogue.speaker = mainDude;
        }

        // ------------------------------------------------------------------ flow

        void OnPlayerReachedCorner()
        {
            if (_started) return;
            _started = true;
            StartCoroutine(IntroRoutine());
        }

        /// <summary>
        /// The player's own head first, then Sean notices them. The second thought is fired off
        /// the approach starting, so it lands exactly as he turns and comes over.
        /// </summary>
        IEnumerator IntroRoutine()
        {
            if (dialogue != null && !string.IsNullOrEmpty(thoughtOnEnter))
            {
                // Held on screen rather than faded out, so it hands straight over to the next
                // thought without the panel blinking in between.
                var showing = dialogue.ShowThought(thoughtOnEnter, thoughtHold, false);
                if (showing != null) yield return showing;
            }

            if (mainDude != null) mainDude.BeginApproach();
            else OnMainDudeArrived();
        }

        void OnApproachStarted()
        {
            if (dialogue == null || string.IsNullOrEmpty(thoughtOnSpotted)) return;
            dialogue.ShowThought(thoughtOnSpotted, thoughtHold, false);
        }

        void OnMainDudeArrived()
        {
            if (freezeLocomotion) FreezeLocomotion(true);
            if (dialogue != null) dialogue.BeginDialogue();
        }

        void OnBreakdown()
        {
            if (freezeLocomotion) FreezeLocomotion(false);
            onEncounterFinished.Invoke();
        }

        /// <summary>Put the encounter back to its starting state so it can be played again.</summary>
        public void ResetEncounter()
        {
            _started = false;
            StopAllCoroutines();
            if (dialogue != null) dialogue.StopDialogue();
            if (mainDude != null) mainDude.ResetApproach(_dudeHome, _dudeHomeRotation);
            if (approachTrigger != null) approachTrigger.ResetTrigger();
            FreezeLocomotion(false);
        }

        // ------------------------------------------------------------------ locomotion

        /// <summary>
        /// Toggles every XR locomotion provider on the rig. Matched by base type name rather
        /// than a direct reference so this keeps working across XR Interaction Toolkit versions.
        /// </summary>
        void FreezeLocomotion(bool freeze)
        {
            if (freeze == _locomotionFrozen) return; // never re-scan while already frozen, or the saved list is lost
            _locomotionFrozen = freeze;

            if (freeze)
            {
                _frozen.Clear();
                if (playerRig == null) return;

                var behaviours = playerRig.GetComponentsInChildren<MonoBehaviour>(true);
                for (int i = 0; i < behaviours.Length; i++)
                {
                    var mb = behaviours[i];
                    if (mb == null || !mb.enabled) continue;
                    if (!IsLocomotionProvider(mb.GetType())) continue;

                    mb.enabled = false;
                    _frozen.Add(mb);
                }
                return;
            }

            for (int i = 0; i < _frozen.Count; i++)
                if (_frozen[i] != null) _frozen[i].enabled = true;
            _frozen.Clear();
        }

        static bool IsLocomotionProvider(System.Type type)
        {
            for (var t = type; t != null; t = t.BaseType)
                if (t.Name == "LocomotionProvider") return true;
            return false;
        }
    }
}
