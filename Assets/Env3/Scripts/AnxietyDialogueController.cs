using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Env3.Anxiety
{
    [Serializable] public class IntEvent : UnityEvent<int> { }
    [Serializable] public class FloatEvent : UnityEvent<float> { }
    [Serializable] public class ChoiceEvent : UnityEvent<int, string> { }

    /// <summary>
    /// The escalating-anxiety conversation.
    ///
    /// Stage 1 offers two answers. Every stage after that offers more, on a shorter timer,
    /// while the panel creeps closer to head-locked and the room narrows. The final stage
    /// stops waiting for an answer at all: options keep spawning across the whole panel
    /// until the player's options are the only thing left, and the encounter ends in a
    /// breakdown regardless of what they click.
    /// </summary>
    [DisallowMultipleComponent]
    public class AnxietyDialogueController : MonoBehaviour
    {
        [Header("Scene wiring")]
        public DialogueCanvasRig rig;
        public Canvas canvas;
        public CanvasGroup canvasGroup;
        public Image backdrop;
        public Image vignette;
        public TextMeshProUGUI npcLineText;
        [Tooltip("Bar that holds the subtitle line. Built automatically at Awake if left empty.")]
        public RectTransform subtitleBar;
        [Tooltip("Translucent plate behind the subtitle text. Built automatically at Awake if left empty.")]
        public Image subtitlePlate;
        public RectTransform choicesColumn;
        public RectTransform panicField;
        public ChoiceButton choiceTemplate;
        public RectTransform timerRoot;
        public Image timerFill;
        public MainDudeApproach speaker;

        [Header("Audio")]
        public AudioSource sfxSource;
        public AudioSource droneSource;
        [Tooltip("Leave empty to use the built-in synthesised clips.")]
        public AudioClip heartbeatClip;
        public AudioClip tickClip;
        public AudioClip droneClip;
        [Range(0f, 1f)] public float tickVolume = 0.22f;
        [Range(0f, 1f)] public float heartbeatVolume = 0.85f;
        [Range(0f, 1f)] public float maxDroneVolume = 0.5f;

        [Header("Conversation")]
        public AnxietyDialogueStage[] stages;

        [Header("Pacing")]
        [Tooltip("Characters per second for MainDude's lines.")]
        public float typeSpeed = 38f;
        public float postLineDelay = 0.3f;
        public float postChoiceDelay = 0.75f;
        public float fadeInDuration = 0.45f;

        [Header("Panic stage")]
        public float panicDuration = 10f;
        public int maxPanicChoices = 55;
        [Tooltip("Clicking during the panic stage does not answer anything. It spawns this many more.")]
        public int extraChoicesPerClick = 3;
        [Tooltip("Anxiety at or above this makes MainDude close the gap.")]
        [Range(0f, 1f)] public float leanInAt = 0.75f;

        [Header("Subtitle bar")]
        [Tooltip("Height above the bottom of the canvas that the bar sits at.")]
        public float subtitleBottomMargin = 70f;
        [Tooltip("The plate hugs the text between these widths.")]
        public Vector2 subtitleWidthRange = new Vector2(360f, 1280f);
        public float subtitleMinHeight = 92f;
        [Tooltip("Space between the text and the edge of the plate: x horizontal, y vertical.")]
        public Vector2 subtitlePadding = new Vector2(38f, 20f);
        public float subtitleFontSize = 40f;
        public Color subtitlePlateColor = new Color(0.02f, 0.02f, 0.03f, 0.66f);
        public Color subtitleTextColor = Color.white;
        [Tooltip("Where the stage 1-3 choice column sits, now that the subtitle bar owns the bottom of the view.")]
        public Vector2 choicesColumnPosition = new Vector2(0f, 60f);
        public Vector2 choicesColumnSize = new Vector2(780f, 620f);
        [Tooltip("Internal thoughts carry no name and are shown wrapped in these.")]
        public string thoughtOpen = "(";
        public string thoughtClose = ")";
        [Tooltip("Used when a stage leaves its speaker name blank.")]
        public string defaultSpeakerName = "Sean Tay";

        [Header("Panic arc")]
        [Tooltip("How far to either side the flood of choices wraps around the player, in degrees.")]
        public float arcMaxYaw = 68f;
        [Tooltip("How far above and below the flood of choices wraps, in degrees.")]
        public float arcMaxPitch = 26f;
        [Tooltip("Arc radius as a multiple of the panel distance. 1 keeps every choice the same distance away as the panel itself.")]
        public float arcRadiusScale = 1f;
        [Tooltip("Choices spawn near the centre at first and fan out to the full arc by this many spawns.")]
        public int arcWidenOver = 14;
        [Tooltip("Degrees of clear space kept around MainDude's head: x horizontal, y vertical.")]
        public Vector2 faceClearAngles = new Vector2(20f, 16f);
        [Tooltip("Random wobble added to each arc position so the spread does not look mechanical.")]
        public float arcJitterDegrees = 3.5f;

        [Header("Panic thought colors")]
        public Color panicThoughtDarkGrey = new Color(0.15f, 0.15f, 0.18f, 0.65f);
        public Color panicThoughtMurkyRed = new Color(0.25f, 0.08f, 0.08f, 0.65f);
        public Color panicThoughtDarkCharcoal = new Color(0.1f, 0.1f, 0.12f, 0.70f);

        [Header("Breakdown")]
        public float collapseDuration = 1.2f;
        public string breakdownLine = "...";
        public int breakdownHeartbeats = 3;
        public bool hideCanvasAfterBreakdown = true;
        public float hideDelayAfterBreakdown = 2f;

        [Header("Distress response")]
        [Tooltip("Backdrop opacity at zero and full anxiety.")]
        public Vector2 backdropAlphaRange = new Vector2(0.12f, 0.62f);
        public Vector2 vignetteAlphaRange = new Vector2(0.10f, 1f);
        [Tooltip("Vignette quad size when calm. Wide enough that its dark ring sits outside the player's view.")]
        public Vector2 vignetteOpenSize = new Vector2(11500f, 8000f);
        [Tooltip("Vignette quad size at full panic. Tightens the tunnel, but must stay wider than the field of view or the quad's own rectangular edge becomes visible.")]
        public Vector2 vignetteClosedSize = new Vector2(5200f, 3650f);
        public Color calmVignetteColor = new Color(0f, 0f, 0f, 1f);
        [Tooltip("Kept dark rather than a bright red, so full panic reads as vision closing down instead of a damage flash.")]
        public Color panicVignetteColor = new Color(0.17f, 0.012f, 0.022f, 1f);
        [Tooltip("How much each heartbeat squeezes the tunnel shut at full panic.")]
        [Range(0f, 0.4f)] public float vignettePulseAmount = 0.1f;
        [Tooltip("How quickly that squeeze relaxes again.")]
        public float vignettePulseDecay = 3.5f;
        public float choiceJitterScale = 0.85f;
        [Tooltip("How fast displayed anxiety catches up to the stage target.")]
        public float anxietySharpness = 2.2f;

        [Header("Events")]
        public UnityEvent onDialogueStarted;
        public IntEvent onStageStarted;
        public ChoiceEvent onChoicePicked;
        public FloatEvent onAnxietyChanged;
        public UnityEvent onBreakdown;

        readonly List<ChoiceButton> _live = new List<ChoiceButton>();
        Coroutine _encounter;
        Coroutine _heartbeat;
        Coroutine _thought;
        Vector2 _npcBaseAnchor;
        float _anxiety;
        float _anxietyTarget;
        float _vignettePulse;
        int _picked = Pending;
        bool _running;
        bool _panicActive;
        bool _leaningIn;

        const int Pending = -1;
        const int TimedOut = -2;

        public bool IsRunning { get { return _running; } }
        public float Anxiety { get { return _anxiety; } }

        // ------------------------------------------------------------------ setup

        void Reset()
        {
            stages = BuildDefaultStages();
        }

        void Awake()
        {
            if (stages == null || stages.Length == 0) stages = BuildDefaultStages();

            EnsureSprites();
            EnsureAudio();
            EnsureSubtitleBar();
            EnsureChoicesLayout();

            if (npcLineText != null) npcLineText.text = string.Empty;
            if (subtitleBar != null)
            {
                _npcBaseAnchor = subtitleBar.anchoredPosition;
                subtitleBar.gameObject.SetActive(false);
            }

            if (choiceTemplate != null) choiceTemplate.gameObject.SetActive(false);
            if (timerRoot != null) timerRoot.gameObject.SetActive(false);
            if (canvasGroup != null) canvasGroup.alpha = 0f;
            if (canvas != null) canvas.enabled = false;
            if (rig != null) rig.lockIn = 0f;

            _anxiety = _anxietyTarget = 0f;
            ApplyAnxietyVisuals();
        }

        void EnsureSprites()
        {
            if (backdrop != null && backdrop.sprite == null)
            {
                backdrop.sprite = Env3UiFactory.SoftRadial;
                backdrop.type = Image.Type.Simple;
            }
            if (vignette != null && vignette.sprite == null) vignette.sprite = Env3UiFactory.Vignette;
            if (timerFill != null && timerFill.sprite == null)
            {
                timerFill.sprite = Env3UiFactory.RoundedRect;
                timerFill.type = Image.Type.Filled;
                timerFill.fillMethod = Image.FillMethod.Horizontal;
            }
            if (choiceTemplate != null && choiceTemplate.background != null && choiceTemplate.background.sprite == null)
            {
                choiceTemplate.background.sprite = Env3UiFactory.RoundedRect;
                choiceTemplate.background.type = Image.Type.Sliced;
            }
        }

        void EnsureAudio()
        {
            if (heartbeatClip == null) heartbeatClip = Env3Audio.Heartbeat;
            if (tickClip == null) tickClip = Env3Audio.Tick;
            if (droneClip == null) droneClip = Env3Audio.Drone;

            if (droneSource != null)
            {
                droneSource.clip = droneClip;
                droneSource.loop = true;
                droneSource.volume = 0f;
                droneSource.playOnAwake = false;
            }
            if (sfxSource != null) sfxSource.playOnAwake = false;
        }

        /// <summary>
        /// Puts the spoken line into a low subtitle bar at the bottom of the view, on its own
        /// translucent plate. Builds the bar if the scene has not been set up with one, so the
        /// layout is correct whether or not the hierarchy has been updated.
        /// </summary>
        void EnsureSubtitleBar()
        {
            if (npcLineText == null) return;

            if (subtitleBar == null)
            {
                var parent = npcLineText.rectTransform.parent;
                var go = new GameObject("SubtitleBar", typeof(RectTransform));
                go.transform.SetParent(parent, false);
                subtitleBar = (RectTransform)go.transform;

                // Draw just under the vignette so full panic still tints the bar.
                if (vignette != null && vignette.transform.parent == parent)
                    subtitleBar.SetSiblingIndex(vignette.transform.GetSiblingIndex());

                npcLineText.rectTransform.SetParent(subtitleBar, false);
            }

            if (subtitlePlate == null)
            {
                subtitlePlate = subtitleBar.GetComponent<Image>();
                if (subtitlePlate == null) subtitlePlate = subtitleBar.gameObject.AddComponent<Image>();
            }
            if (subtitlePlate.sprite == null)
            {
                subtitlePlate.sprite = Env3UiFactory.RoundedRect;
                subtitlePlate.type = Image.Type.Sliced;
            }
            subtitlePlate.color = subtitlePlateColor;
            subtitlePlate.raycastTarget = false;

            subtitleBar.anchorMin = new Vector2(0.5f, 0f);
            subtitleBar.anchorMax = new Vector2(0.5f, 0f);
            subtitleBar.pivot = new Vector2(0.5f, 0f);
            subtitleBar.anchoredPosition = new Vector2(0f, subtitleBottomMargin);
            subtitleBar.sizeDelta = new Vector2(subtitleWidthRange.y, subtitleMinHeight);

            var textRect = npcLineText.rectTransform;
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(subtitlePadding.x, subtitlePadding.y);
            textRect.offsetMax = new Vector2(-subtitlePadding.x, -subtitlePadding.y);

            npcLineText.fontSize = subtitleFontSize;
            npcLineText.color = subtitleTextColor;
            npcLineText.alignment = TextAlignmentOptions.Center;
            npcLineText.raycastTarget = false;
        }

        /// <summary>
        /// "Name: line" for anything spoken aloud; an unattributed line in brackets for the
        /// player's own thoughts.
        /// </summary>
        string FormatLine(string speaker, string line, bool asThought)
        {
            if (string.IsNullOrEmpty(line)) return string.Empty;
            line = line.Trim();

            if (!asThought)
            {
                // Stages serialised before speakerName existed deserialise it as blank, so fall
                // back rather than silently rendering Sean's dialogue as the player's thoughts.
                string name = string.IsNullOrEmpty(speaker) ? defaultSpeakerName : speaker.Trim();
                return string.IsNullOrEmpty(name) ? line : name + ": " + line;
            }

            bool alreadyBracketed = !string.IsNullOrEmpty(thoughtOpen) && line.StartsWith(thoughtOpen);
            return alreadyBracketed ? line : thoughtOpen + line + thoughtClose;
        }

        /// <summary>Shows a fully formed line immediately, with the plate resized around it.</summary>
        void SetSubtitle(string formatted)
        {
            if (npcLineText == null) return;

            npcLineText.text = formatted != null ? formatted : string.Empty;
            npcLineText.maxVisibleCharacters = int.MaxValue;
            LayoutSubtitle();
        }

        /// <summary>Sizes the plate to hug the current line, wrapping once it hits the max width.</summary>
        void LayoutSubtitle()
        {
            if (npcLineText == null || subtitleBar == null) return;

            bool hasText = !string.IsNullOrEmpty(npcLineText.text);
            if (subtitleBar.gameObject.activeSelf != hasText) subtitleBar.gameObject.SetActive(hasText);
            if (!hasText) return;

            float innerMax = Mathf.Max(50f, subtitleWidthRange.y - subtitlePadding.x * 2f);
            Vector2 preferred = npcLineText.GetPreferredValues(npcLineText.text, innerMax, 0f);

            float width = Mathf.Clamp(preferred.x + subtitlePadding.x * 2f, subtitleWidthRange.x, subtitleWidthRange.y);
            float height = Mathf.Max(subtitleMinHeight, preferred.y + subtitlePadding.y * 2f);
            subtitleBar.sizeDelta = new Vector2(width, height);
        }

        /// <summary>
        /// Lifts the choice column clear of the subtitle bar. Kept in code alongside the bar
        /// itself so the two cannot drift apart.
        /// </summary>
        void EnsureChoicesLayout()
        {
            if (choicesColumn == null) return;

            choicesColumn.anchorMin = new Vector2(0.5f, 0.5f);
            choicesColumn.anchorMax = new Vector2(0.5f, 0.5f);
            choicesColumn.pivot = new Vector2(0.5f, 0.5f);
            choicesColumn.anchoredPosition = choicesColumnPosition;
            choicesColumn.sizeDelta = choicesColumnSize;
        }

        void ShowPanel()
        {
            if (canvas != null) canvas.enabled = true;
            if (rig != null) rig.enabled = true;
        }

        // ------------------------------------------------------------------ control

        /// <summary>
        /// Shows one of the player's own thoughts on the subtitle bar, before the conversation
        /// starts. Yield on the returned coroutine to wait for it. Ignored once the dialogue is
        /// running, which owns the bar from then on.
        /// </summary>
        public Coroutine ShowThought(string line, float holdSeconds, bool hideAfter = true)
        {
            if (_running || string.IsNullOrEmpty(line)) return null;

            if (_thought != null) StopCoroutine(_thought);
            _thought = StartCoroutine(ThoughtRoutine(line, holdSeconds, hideAfter));
            return _thought;
        }

        IEnumerator ThoughtRoutine(string line, float holdSeconds, bool hideAfter)
        {
            ShowPanel();
            if (rig != null && (canvasGroup == null || canvasGroup.alpha <= 0f)) rig.SnapToHead();

            if (canvasGroup != null && canvasGroup.alpha < 1f)
                yield return FadeCanvas(canvasGroup.alpha, 1f, fadeInDuration);

            yield return Typewriter(null, line, true);
            yield return new WaitForSeconds(Mathf.Max(0f, holdSeconds));

            if (hideAfter && !_running)
            {
                yield return FadeCanvas(1f, 0f, 0.4f);
                SetSubtitle(string.Empty);
                HidePanel();
            }

            _thought = null;
        }

        /// <summary>Start the conversation. Hook this to MainDudeApproach.onReachedPlayer.</summary>
        public void BeginDialogue()
        {
            if (_running) return;
            _running = true;

            if (_thought != null) { StopCoroutine(_thought); _thought = null; }

            ShowPanel();
            if (rig != null)
            {
                if (canvasGroup == null || canvasGroup.alpha <= 0f) rig.SnapToHead();
            }
            if (droneSource != null && !droneSource.isPlaying) droneSource.Play();

            onDialogueStarted.Invoke();
            _encounter = StartCoroutine(RunEncounter());
            _heartbeat = StartCoroutine(HeartbeatRoutine());
        }

        /// <summary>Stop everything and clear the panel. Does not fire onBreakdown.</summary>
        public void StopDialogue()
        {
            if (_encounter != null) StopCoroutine(_encounter);
            if (_heartbeat != null) StopCoroutine(_heartbeat);
            if (_thought != null) StopCoroutine(_thought);
            _encounter = null;
            _heartbeat = null;
            _thought = null;
            SetSubtitle(string.Empty);
            _running = false;
            _panicActive = false;

            ClearChoices();
            HidePanel();
            SetAnxiety(0f, true);
            if (droneSource != null) droneSource.Stop();
        }

        void HidePanel()
        {
            if (canvasGroup != null) canvasGroup.alpha = 0f;
            if (canvas != null) canvas.enabled = false;
            if (rig != null) rig.lockIn = 0f;
            if (subtitleBar != null) subtitleBar.gameObject.SetActive(false);
        }

        // ------------------------------------------------------------------ flow

        IEnumerator RunEncounter()
        {
            // Start from wherever an intro thought left the panel, so it does not blink.
            float startAlpha = canvasGroup != null ? canvasGroup.alpha : 0f;
            if (startAlpha < 1f) yield return FadeCanvas(startAlpha, 1f, fadeInDuration);

            for (int i = 0; i < stages.Length; i++)
            {
                var stage = stages[i];
                if (stage == null) continue;

                onStageStarted.Invoke(i);
                SetAnxiety(stage.anxiety, false);

                yield return Typewriter(stage.speakerName, stage.npcLine, false);
                yield return new WaitForSeconds(postLineDelay);

                if (stage.panicFlood) yield return RunPanicStage(stage);
                else yield return RunChoiceStage(stage);
            }

            yield return RunBreakdown();
        }

        IEnumerator RunChoiceStage(AnxietyDialogueStage stage)
        {
            ClearChoices();
            _picked = Pending;

            int count = stage.choices != null ? stage.choices.Length : 0;
            for (int i = 0; i < count; i++)
            {
                var button = SpawnChoice(stage.choices[i], choicesColumn, i);
                button.PlayIntro();
                PlayTick(0.7f);
                if (stage.choiceRevealInterval > 0f) yield return new WaitForSeconds(stage.choiceRevealInterval);
            }

            yield return WaitForChoice(stage.answerTimeLimit);

            string spoken = _picked >= 0 && _picked < count ? stage.choices[_picked] : null;
            onChoicePicked.Invoke(_picked, spoken != null ? spoken : string.Empty);

            yield return ResolveChoice();
        }

        IEnumerator WaitForChoice(float timeLimit)
        {
            bool timed = timeLimit > 0f;
            if (timerRoot != null) timerRoot.gameObject.SetActive(timed);

            float elapsed = 0f;
            while (_picked == Pending)
            {
                if (timed)
                {
                    elapsed += Time.deltaTime;
                    if (timerFill != null) timerFill.fillAmount = Mathf.Clamp01(1f - elapsed / timeLimit);
                    if (elapsed >= timeLimit)
                    {
                        _picked = TimedOut;
                        break;
                    }
                }
                yield return null;
            }

            if (timerRoot != null) timerRoot.gameObject.SetActive(false);
        }

        /// <summary>Dim the answers not taken, hold a beat, then clear the panel.</summary>
        IEnumerator ResolveChoice()
        {
            for (int i = 0; i < _live.Count; i++)
            {
                var b = _live[i];
                if (b == null) continue;
                b.Lock();
                if (b.Index != _picked) b.SetAlpha(0.18f);
            }

            PlayTick(1f);
            yield return new WaitForSeconds(postChoiceDelay);
            ClearChoices();
        }

        IEnumerator RunPanicStage(AnxietyDialogueStage stage)
        {
            ClearChoices();
            _panicActive = true;
            _picked = Pending;

            if (speaker != null && !_leaningIn)
            {
                _leaningIn = true;
                speaker.SetLeanIn(true);
            }

            int count = stage.choices != null ? stage.choices.Length : 0;
            float interval = Mathf.Max(0.04f, stage.choiceRevealInterval);
            float elapsed = 0f;
            int spawned = 0;

            while (elapsed < panicDuration)
            {
                if (count > 0 && spawned < maxPanicChoices)
                {
                    SpawnPanicChoice(stage.choices[spawned % count], spawned);
                    spawned++;
                    PlayTick(0.5f);
                }

                SetAnxiety(Mathf.Lerp(stage.anxiety, 1f, elapsed / panicDuration), false);

                yield return new WaitForSeconds(interval);
                elapsed += interval;
                interval = Mathf.Max(0.045f, interval * 0.92f);
            }

            _panicActive = false;
        }

        IEnumerator RunBreakdown()
        {
            _panicActive = false;
            SetAnxiety(1f, false);

            for (int i = 0; i < _live.Count; i++)
                if (_live[i] != null) _live[i].Lock();

            SetSubtitle(breakdownLine);

            // Everything the player could have said collapses into the middle and goes out.
            int n = _live.Count;
            var fromPos = new Vector3[n];
            var fromRot = new Quaternion[n];
            var fromScale = new Vector3[n];
            for (int i = 0; i < n; i++)
            {
                if (_live[i] == null) continue;
                fromPos[i] = _live[i].Rect.localPosition;
                fromRot[i] = _live[i].Rect.localRotation;
                fromScale[i] = _live[i].Rect.localScale;
            }

            float t = 0f;
            float duration = Mathf.Max(0.05f, collapseDuration);
            while (t < duration)
            {
                t += Time.deltaTime;
                float k = Mathf.SmoothStep(0f, 1f, t / duration);
                for (int i = 0; i < n; i++)
                {
                    var b = _live[i];
                    if (b == null) continue;
                    b.Rect.localPosition = Vector3.Lerp(fromPos[i], Vector3.zero, k * k);
                    b.Rect.localRotation = Quaternion.Slerp(fromRot[i], Quaternion.identity, k);
                    b.Rect.localScale = Vector3.Lerp(fromScale[i], Vector3.zero, k);
                    b.SetAlpha(1f - k);
                }
                yield return null;
            }

            ClearChoices();

            for (int i = 0; i < breakdownHeartbeats; i++)
            {
                PlayHeartbeat();
                yield return new WaitForSeconds(Mathf.Lerp(0.55f, 1.1f, i / Mathf.Max(1f, breakdownHeartbeats - 1f)));
            }

            onBreakdown.Invoke();

            if (hideCanvasAfterBreakdown)
            {
                yield return new WaitForSeconds(hideDelayAfterBreakdown);
                yield return FadeCanvas(1f, 0f, 0.6f);
                HidePanel();
                if (droneSource != null) droneSource.Stop();
            }

            _running = false;
        }

        // ------------------------------------------------------------------ choices

        ChoiceButton SpawnChoice(string text, RectTransform parent, int index)
        {
            var instance = Instantiate(choiceTemplate.gameObject, parent != null ? parent : (RectTransform)transform);
            instance.name = "Choice_" + index;
            instance.SetActive(true);

            var button = instance.GetComponent<ChoiceButton>();
            button.Bind(index, text, OnChoiceClicked);
            button.SetAlpha(0f);
            _live.Add(button);
            return button;
        }

        void SpawnPanicChoice(string text, int index)
        {
            var button = SpawnChoice(text, panicField, index);

            var rect = button.Rect;
            float width = 300f;
            if (button.label != null)
            {
                button.label.ForceMeshUpdate();
                width = Mathf.Clamp(button.label.preferredWidth + 52f, 150f, 520f);
            }
            rect.sizeDelta = new Vector2(width, 62f);

            // Apply dynamic panic colors based on anxiety level
            float anxietyLerp = Mathf.Clamp01(_anxiety);
            Color panicColor;
            if (anxietyLerp < 0.33f)
                panicColor = Color.Lerp(panicThoughtDarkGrey, panicThoughtMurkyRed, anxietyLerp * 3f);
            else if (anxietyLerp < 0.67f)
                panicColor = Color.Lerp(panicThoughtMurkyRed, panicThoughtDarkCharcoal, (anxietyLerp - 0.33f) * 1.5f);
            else
                panicColor = Color.Lerp(panicThoughtDarkCharcoal, new Color(0.08f, 0.04f, 0.06f, 0.75f), (anxietyLerp - 0.67f) * 3f);

            button.SetPanicColor(panicColor);

            PlaceOnArc(rect, index);

            button.SetRestAngle(UnityEngine.Random.Range(-7f, 7f));
            button.PlayIntro(0.12f);
        }

        /// <summary>
        /// Places a panic choice on a dome centred on the player's head instead of scattering it
        /// across the flat panel, so the options curve around them. A cone around MainDude's face
        /// is kept clear, so the choices crowd in around him rather than covering him up.
        /// </summary>
        void PlaceOnArc(RectTransform rect, int index)
        {
            var parent = rect.parent as RectTransform;
            if (parent == null) return;

            Transform head = rig != null && rig.head != null
                ? rig.head
                : (Camera.main != null ? Camera.main.transform : null);
            if (head == null) { rect.anchoredPosition = Vector2.zero; return; }

            Vector3 eyeLocal = parent.InverseTransformPoint(head.position);

            // Two low-discrepancy sequences: consecutive choices land far apart, and the set
            // stays evenly spread however many end up spawning.
            float u = Frac(index * 0.61803399f) * 2f - 1f;
            float v = Frac(index * 0.75487767f) * 2f - 1f;
            float widen = arcWidenOver > 0 ? Mathf.Lerp(0.5f, 1f, Mathf.Clamp01(index / (float)arcWidenOver)) : 1f;

            float yaw = u * arcMaxYaw * widen + UnityEngine.Random.Range(-arcJitterDegrees, arcJitterDegrees);
            float pitch = v * arcMaxPitch * widen + UnityEngine.Random.Range(-arcJitterDegrees, arcJitterDegrees) * 0.5f;

            PushClearOfFace(parent, eyeLocal, ref yaw, ref pitch);

            Vector3 direction = AnglesToDirection(yaw, pitch);
            rect.localPosition = eyeLocal + direction * ArcRadius();
            rect.localRotation = Quaternion.LookRotation(direction, Vector3.up);
        }

        /// <summary>Nudges an arc position out of the cone MainDude's head occupies.</summary>
        void PushClearOfFace(RectTransform parent, Vector3 eyeLocal, ref float yaw, ref float pitch)
        {
            if (speaker == null) return;

            float clearYaw = Mathf.Max(1f, faceClearAngles.x);
            float clearPitch = Mathf.Max(1f, faceClearAngles.y);

            Vector3 toHead = parent.InverseTransformPoint(speaker.HeadPosition) - eyeLocal;
            if (toHead.sqrMagnitude < 0.0001f) return;
            toHead.Normalize();

            float headYaw = Mathf.Atan2(toHead.x, toHead.z) * Mathf.Rad2Deg;
            float headPitch = Mathf.Asin(Mathf.Clamp(toHead.y, -1f, 1f)) * Mathf.Rad2Deg;

            // Work in units of the clear zone, so the test is a circle and the ellipse falls out of it.
            float nx = (yaw - headYaw) / clearYaw;
            float ny = (pitch - headPitch) / clearPitch;
            float r = Mathf.Sqrt(nx * nx + ny * ny);
            if (r >= 1f) return;

            if (r < 0.0001f) { nx = 1f; ny = 0.35f; r = Mathf.Sqrt(nx * nx + ny * ny); }
            nx /= r;
            ny /= r;

            yaw = headYaw + nx * clearYaw;
            pitch = headPitch + ny * clearPitch;

            // If pushing it clear ran off the end of the arc, come out the other side of him instead.
            if (Mathf.Abs(yaw) > arcMaxYaw)
            {
                yaw = Mathf.Clamp(headYaw - nx * clearYaw, -arcMaxYaw, arcMaxYaw);
                pitch = Mathf.Clamp(headPitch - ny * clearPitch, -arcMaxPitch, arcMaxPitch);
            }

            pitch = Mathf.Clamp(pitch, -arcMaxPitch, arcMaxPitch);
        }

        /// <summary>Arc radius in canvas units, matching the panel's own distance from the player.</summary>
        float ArcRadius()
        {
            float scale = canvas != null ? Mathf.Abs(canvas.transform.localScale.x) : 0.001f;
            if (scale < 0.00001f) scale = 0.001f;
            float metres = rig != null ? rig.distance : 1.25f;
            return metres * arcRadiusScale / scale;
        }

        static Vector3 AnglesToDirection(float yawDegrees, float pitchDegrees)
        {
            float y = yawDegrees * Mathf.Deg2Rad;
            float p = pitchDegrees * Mathf.Deg2Rad;
            float cp = Mathf.Cos(p);
            return new Vector3(Mathf.Sin(y) * cp, Mathf.Sin(p), Mathf.Cos(y) * cp);
        }

        static float Frac(float value)
        {
            return value - Mathf.Floor(value);
        }

        void OnChoiceClicked(ChoiceButton button)
        {
            if (_panicActive)
            {
                // There is no right answer any more. Trying to pick one only makes more.
                PlayHeartbeat();
                if (rig != null) rig.PunchShake();

                var stage = CurrentPanicStage();
                if (stage != null && stage.choices != null && stage.choices.Length > 0)
                {
                    for (int i = 0; i < extraChoicesPerClick && _live.Count < maxPanicChoices + extraChoicesPerClick * 4; i++)
                        SpawnPanicChoice(stage.choices[UnityEngine.Random.Range(0, stage.choices.Length)], _live.Count);
                }
                return;
            }

            if (_picked == Pending) _picked = button.Index;
        }

        AnxietyDialogueStage CurrentPanicStage()
        {
            for (int i = stages.Length - 1; i >= 0; i--)
                if (stages[i] != null && stages[i].panicFlood) return stages[i];
            return null;
        }

        void ClearChoices()
        {
            for (int i = 0; i < _live.Count; i++)
                if (_live[i] != null) Destroy(_live[i].gameObject);
            _live.Clear();
        }

        // ------------------------------------------------------------------ presentation

        IEnumerator Typewriter(string speaker, string line, bool asThought)
        {
            if (npcLineText == null) yield break;

            // Set the whole line first so the plate is sized for the finished text, then
            // reveal it. Sizing per character would make the bar grow as it types.
            npcLineText.text = FormatLine(speaker, line, asThought);
            LayoutSubtitle();
            npcLineText.maxVisibleCharacters = 0;
            npcLineText.ForceMeshUpdate();

            int total = npcLineText.textInfo.characterCount;
            float perChar = typeSpeed > 0f ? 1f / typeSpeed : 0f;

            for (int i = 0; i <= total; i++)
            {
                npcLineText.maxVisibleCharacters = i;
                if (i % 3 == 0) PlayTick(0.35f);
                if (perChar > 0f) yield return new WaitForSeconds(perChar);
            }

            npcLineText.maxVisibleCharacters = int.MaxValue;
        }

        IEnumerator FadeCanvas(float from, float to, float duration)
        {
            if (canvasGroup == null) yield break;

            float t = 0f;
            duration = Mathf.Max(0.01f, duration);
            while (t < duration)
            {
                t += Time.deltaTime;
                canvasGroup.alpha = Mathf.Lerp(from, to, t / duration);
                yield return null;
            }
            canvasGroup.alpha = to;
        }

        public void SetAnxiety(float value, bool immediate = false)
        {
            _anxietyTarget = Mathf.Clamp01(value);
            if (immediate) _anxiety = _anxietyTarget;
        }

        void Update()
        {
            if (!Mathf.Approximately(_anxiety, _anxietyTarget))
            {
                float k = 1f - Mathf.Exp(-anxietySharpness * Time.deltaTime);
                _anxiety = Mathf.Lerp(_anxiety, _anxietyTarget, k);
                onAnxietyChanged.Invoke(_anxiety);
            }

            if (_vignettePulse > 0f)
                _vignettePulse = Mathf.Max(0f, _vignettePulse - vignettePulseDecay * Time.deltaTime);

            ApplyAnxietyVisuals();

            if (!_leaningIn && speaker != null && _running && _anxiety >= leanInAt)
            {
                _leaningIn = true;
                speaker.SetLeanIn(true);
            }
        }

        void ApplyAnxietyVisuals()
        {
            float a = _anxiety;

            if (backdrop != null)
            {
                var c = backdrop.color;
                c.a = Mathf.Lerp(backdropAlphaRange.x, backdropAlphaRange.y, a);
                backdrop.color = c;
            }

            if (vignette != null)
            {
                var c = Color.Lerp(calmVignetteColor, panicVignetteColor, a);
                c.a = Mathf.Lerp(vignetteAlphaRange.x, vignetteAlphaRange.y, a);
                vignette.color = c;

                // Shrinking the quad drags the clear centre inwards, so the room narrows to a
                // tunnel rather than just getting darker, and every heartbeat squeezes it further.
                float squeeze = 1f - vignettePulseAmount * _vignettePulse * a;
                vignette.rectTransform.sizeDelta = Vector2.Lerp(vignetteOpenSize, vignetteClosedSize, a) * squeeze;
            }

            if (rig != null) rig.lockIn = a;

            if (droneSource != null) droneSource.volume = maxDroneVolume * a;

            // The text itself stays clean white at every anxiety level; the plate and the
            // tremble carry the distress instead, so the line never becomes hard to read.
            if (subtitlePlate != null)
            {
                var plate = subtitlePlateColor;
                plate.a = Mathf.Lerp(subtitlePlateColor.a, Mathf.Min(1f, subtitlePlateColor.a + 0.22f), a);
                subtitlePlate.color = plate;
            }

            if (subtitleBar != null)
            {
                float wobble = a * 6f;
                if (wobble > 0.01f)
                {
                    float t = Time.unscaledTime * 11f;
                    subtitleBar.anchoredPosition = _npcBaseAnchor + new Vector2(
                        (Mathf.PerlinNoise(3.1f, t) - 0.5f) * wobble,
                        (Mathf.PerlinNoise(7.7f, t) - 0.5f) * wobble);
                }
                else
                {
                    subtitleBar.anchoredPosition = _npcBaseAnchor;
                }
            }

            float jitter = a * choiceJitterScale;
            for (int i = 0; i < _live.Count; i++)
                if (_live[i] != null) _live[i].SetJitter(jitter);
        }

        // ------------------------------------------------------------------ audio

        IEnumerator HeartbeatRoutine()
        {
            while (_running)
            {
                if (_anxiety >= 0.28f)
                {
                    PlayHeartbeat();
                    yield return new WaitForSeconds(Mathf.Lerp(1.3f, 0.4f, Mathf.InverseLerp(0.28f, 1f, _anxiety)));
                }
                else
                {
                    yield return new WaitForSeconds(0.25f);
                }
            }
        }

        void PlayHeartbeat()
        {
            _vignettePulse = 1f;
            if (sfxSource != null && heartbeatClip != null)
                sfxSource.PlayOneShot(heartbeatClip, heartbeatVolume);
        }

        void PlayTick(float scale)
        {
            if (sfxSource != null && tickClip != null)
                sfxSource.PlayOneShot(tickClip, tickVolume * scale);
        }

        // ------------------------------------------------------------------ content

        /// <summary>
        /// Default script for the encounter. Two answers, then four, then eight, then a flood.
        /// </summary>
        public static AnxietyDialogueStage[] BuildDefaultStages()
        {
            return new[]
            {
                new AnxietyDialogueStage
                {
                    label = "1 - Blurt",
                    speakerName = "Sean Tay",
                    npcLine = "Hey, there you are!",
                    choices = new[] { "Hey.", "Sorry I'm late." },
                    answerTimeLimit = 0f,
                    anxiety = 0.10f,
                    choiceRevealInterval = 0.22f
                },
                new AnxietyDialogueStage
                {
                    label = "2 - Small talk",
                    npcLine = "Nah, you're good. So how've you been? Feels like ages.",
                    choices = new[]
                    {
                        "Good. Busy.",
                        "Yeah, fine.",
                        "Honestly? It's been a lot.",
                        "Sorry, what was the question?"
                    },
                    answerTimeLimit = 9f,
                    anxiety = 0.34f,
                    choiceRevealInterval = 0.16f
                },
                new AnxietyDialogueStage
                {
                    label = "3 - Pressure",
                    npcLine = "You went proper quiet this year, you know. People noticed.",
                    choices = new[]
                    {
                        "I've just been busy.",
                        "I didn't think anyone noticed.",
                        "Sorry.",
                        "Yeah.",
                        "It's not a big deal.",
                        "Who noticed?",
                        "Did someone say something?",
                        "I should probably go."
                    },
                    answerTimeLimit = 6f,
                    anxiety = 0.64f,
                    choiceRevealInterval = 0.1f
                },
                new AnxietyDialogueStage
                {
                    label = "4 - Panic",
                    npcLine = "...hey. You alright? You've gone kind of grey.",
                    choices = new[]
                    {
                        "I'm fine",
                        "Sorry",
                        "Yeah",
                        "No",
                        "Sorry, what?",
                        "I'm fine, sorry",
                        "It's loud in here",
                        "Say something",
                        "SAY SOMETHING",
                        "Do they hate me",
                        "Why did I come",
                        "Just breathe",
                        "Smile. Normally.",
                        "Was that too long",
                        "They're waiting",
                        "Nod",
                        "Laugh?",
                        "Everyone is looking",
                        "I can't-",
                        "Answer him",
                        "Too late",
                        "Leave",
                        "You're being weird",
                        "Stop",
                        "..."
                    },
                    answerTimeLimit = 0f,
                    anxiety = 0.8f,
                    choiceRevealInterval = 0.3f,
                    panicFlood = true
                }
            };
        }
    }
}
