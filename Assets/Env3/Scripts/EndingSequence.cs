using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Env3.Anxiety
{
    /// <summary>
    /// What happens after the breakdown: the room fades out, the player is told what this is
    /// actually about, and then given somewhere to call.
    ///
    /// The whole thing lives inside AfterParty, so nothing loads until the player chooses to
    /// leave. The panels are built in code at Awake, the way the subtitle bar and the choice
    /// layout are, so the scene only has to hold one empty object.
    ///
    /// The canvas is World Space and parented straight to the player's head. A Screen Space
    /// Overlay canvas would only reach the desktop mirror window and never the headset, and a
    /// canvas that eases toward the head would open a lit gap at the edge of vision every time
    /// the player turned. Hard-parented, the black moves exactly with them and the text stays
    /// centred wherever they look.
    /// </summary>
    [DisallowMultipleComponent]
    public class EndingSequence : MonoBehaviour
    {
        [Header("Scene wiring")]
        [Tooltip("Fires the ending off its onEncounterFinished. Empty = found in the scene.")]
        public AfterPartyEncounter encounter;
        [Tooltip("Only used to borrow the scene's font and its XR raycaster. Empty = found in the scene.")]
        public AnxietyDialogueController dialogue;
        [Tooltip("Player head. Empty = Camera.main at Awake.")]
        public Transform head;

        [Header("Timing")]
        [Tooltip("Quiet beat after the breakdown, so the last heartbeat lands before the room goes.")]
        public float startDelay = 1.6f;
        public float fadeToBlackDuration = 3f;
        [Tooltip("How long the screen stays empty and black before the first line appears.")]
        public float holdOnBlack = 1.4f;
        public float panelFadeDuration = 1.2f;
        public float statsHold = 9f;
        public float hotlineHold = 12f;
        [Tooltip("A phase cannot be skipped until it has been readable this long, so the click that skipped the last one cannot skip this one too.")]
        public float minTimeBeforeSkip = 1.5f;

        [Header("Placement")]
        [Tooltip("Metres from the head to the text. Far enough that the player's eyes are not crossing to read it.")]
        public float contentDistance = 1.35f;
        [Tooltip("World metres per canvas unit. Sets how large the text reads.")]
        public float canvasScale = 0.0009f;
        [Tooltip("Metres from the head to the black. Deliberately nearer than the controller models, or they show through it. Nothing can physically get between an eye and this.")]
        public float blackoutDistance = 0.08f;
        [Tooltip("Drawn above the dialogue canvas, which sits at the default 0.")]
        public int sortingOrder = 200;
        [Tooltip("Layer the ending is built on. Once black, the camera renders only this layer, which is what keeps a nearby wall from cutting into the text. 5 is Unity's built-in UI layer.")]
        public int endingLayer = 5;
        [Tooltip("Move the XR pointer line onto the ending layer too, so the player can still see where they are aiming the Main Menu button.")]
        public bool keepPointerVisible = true;

        [Header("Phase 1 - Singapore statistics")]
        public string statsHeading = "In Singapore";
        [TextArea(4, 10)]
        [Tooltip("Please check these against the current published figures before you submit.")]
        public string statsBody =
            "1 in 3 young people aged 15 to 35 report severe symptoms of depression, anxiety or stress.\n\n" +
            "1 in 7 people here will live through a mental health condition at some point in their life.\n\n" +
            "Most of them never tell anyone.";
        [TextArea(1, 3)] public string statsFootnote =
            "Institute of Mental Health, National Youth Mental Health Study 2024\nSingapore Mental Health Study 2016";

        [Header("Phase 2 - Hotlines")]
        public string hotlineHeading = "If any of that felt familiar";
        [TextArea(6, 14)] public string hotlineBody =
            "SOS  Samaritans of Singapore\n1767   ·   24 hours\nCareText on WhatsApp   9151 1767\n\n" +
            "mindline.sg\n1771   ·   24 hours\n\n" +
            "IMH Mental Health Helpline\n6389 2222   ·   24 hours\n\n" +
            "Tinkle Friend  (ages 7 to 12)\n1800 274 4788\n\n" +
            "Emergency   995";

        [Header("End screen")]
        [TextArea(1, 3)] public string endLine = "You are not alone.";
        public string mainMenuButtonText = "Main Menu";
        [Tooltip("Must be in Build Settings or the button cannot load it.")]
        public string mainMenuSceneName = "mainmenu";

        [Header("Look")]
        public Color textColor = new Color(0.93f, 0.93f, 0.93f, 1f);
        public Color headingColor = Color.white;
        public Color footnoteColor = new Color(0.62f, 0.62f, 0.65f, 1f);
        public float headingFontSize = 60f;
        public float bodyFontSize = 44f;
        public float footnoteFontSize = 26f;
        public Vector2 menuButtonSize = new Vector2(620f, 190f);
        public Color menuButtonTextColor = new Color(0.09f, 0.09f, 0.09f, 1f);

        [Header("Events")]
        [Tooltip("Fired once the screen is fully black, before any text appears.")]
        public UnityEngine.Events.UnityEvent onFadedToBlack;
        [Tooltip("Fired when the player presses Main Menu, just before the scene loads.")]
        public UnityEngine.Events.UnityEvent onReturningToMenu;

        Canvas _canvas;
        RectTransform _canvasRect;
        RectTransform _blackoutRect;
        Image _blackout;
        Image _skipCatcher;
        Button _skipButton;
        CanvasGroup _statsGroup;
        CanvasGroup _hotlineGroup;
        CanvasGroup _endGroup;
        TMP_FontAsset _font;

        readonly System.Collections.Generic.List<GameObject> _pointerLines = new System.Collections.Generic.List<GameObject>();
        readonly System.Collections.Generic.List<int> _pointerLayers = new System.Collections.Generic.List<int>();
        int _savedCullingMask;
        CameraClearFlags _savedClearFlags;
        Color _savedBackground;
        bool _worldCulled;
        bool _played;
        bool _skipRequested;
        float _phaseStarted;

        // ------------------------------------------------------------------ setup

        void Awake()
        {
            ResolveReferences();
            BuildUi();

            if (encounter != null) encounter.onEncounterFinished.AddListener(Play);
        }

        void OnDestroy()
        {
            if (encounter != null) encounter.onEncounterFinished.RemoveListener(Play);
        }

        void ResolveReferences()
        {
            if (encounter == null) encounter = FindObjectOfType<AfterPartyEncounter>();
            if (dialogue == null)
            {
                dialogue = encounter != null && encounter.dialogue != null
                    ? encounter.dialogue
                    : FindObjectOfType<AnxietyDialogueController>();
            }
            if (head == null && Camera.main != null) head = Camera.main.transform;

            // Borrowed rather than re-picked, so the ending is set in the same face as the
            // subtitles the player has been reading all encounter.
            if (dialogue != null) _font = dialogue.dialogueFont;
        }

        /// <summary>
        /// Two canvases, at two very different distances, because the black and the text want
        /// opposite things.
        ///
        /// The black has to be nearer to the eye than anything that could poke through it, and
        /// the controller models sit about 25cm out, so it goes at 8cm where nothing can get in
        /// front of it. Text at 8cm would be unreadable: the eyes would have to cross to focus
        /// on it. So the text stays out at a normal reading distance and is drawn after the
        /// black by sorting order rather than by being nearer.
        /// </summary>
        void BuildUi()
        {
            var parent = head != null ? head : transform;

            var blackGo = new GameObject("EndingBlackout", typeof(RectTransform), typeof(Canvas));
            _blackoutRect = (RectTransform)blackGo.transform;
            _blackoutRect.SetParent(parent, false);
            _blackoutRect.localPosition = new Vector3(0f, 0f, blackoutDistance);
            _blackoutRect.localRotation = Quaternion.identity;
            _blackoutRect.sizeDelta = new Vector2(8000f, 8000f);
            _blackoutRect.localScale = Vector3.one * 0.001f;

            var blackCanvas = blackGo.GetComponent<Canvas>();
            blackCanvas.renderMode = RenderMode.WorldSpace;
            blackCanvas.worldCamera = Camera.main;
            blackCanvas.sortingOrder = sortingOrder;

            // 8m across at 8cm out: roughly 178 degrees, so no head angle brings an edge into view.
            _blackout = blackGo.AddComponent<Image>();
            _blackout.color = new Color(0f, 0f, 0f, 0f);
            _blackout.raycastTarget = false;

            var go = new GameObject("EndingCanvas", typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster));
            _canvasRect = (RectTransform)go.transform;
            _canvasRect.SetParent(parent, false);
            _canvasRect.localPosition = new Vector3(0f, 0f, contentDistance);
            _canvasRect.localRotation = Quaternion.identity;
            _canvasRect.sizeDelta = new Vector2(2000f, 1200f);
            _canvasRect.localScale = Vector3.one * canvasScale;

            _canvas = go.GetComponent<Canvas>();
            _canvas.renderMode = RenderMode.WorldSpace;
            _canvas.worldCamera = Camera.main;
            _canvas.sortingOrder = sortingOrder + 10;
            AddTrackedDeviceRaycaster(go);

            // Child order is draw order inside one canvas, so the skip catcher going in first is
            // what keeps it behind every panel instead of over them.
            BuildSkipCatcher();
            _statsGroup = BuildStatsPanel();
            _hotlineGroup = BuildHotlinePanel();
            _endGroup = BuildEndPanel();

            SetLayer(blackGo);
            SetLayer(go);
            blackGo.SetActive(false);
            go.SetActive(false);
        }

        /// <summary>
        /// An invisible plate that turns a click anywhere into "skip this phase".
        ///
        /// It lives on the text canvas rather than on the black, because an XR ray leaves the
        /// hand pointing away from the player and would never cross a surface sitting 8cm in
        /// front of their eyes. Done as a Button rather than by reading a key, because the
        /// project is on the new Input System only, and this way it works with an XR ray, the
        /// device simulator and a plain mouse without needing to know which is in use.
        /// </summary>
        void BuildSkipCatcher()
        {
            var go = new GameObject("SkipCatcher", typeof(RectTransform), typeof(Image), typeof(Button));
            var rect = (RectTransform)go.transform;
            rect.SetParent(_canvasRect, false);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(9000f, 9000f);

            _skipCatcher = go.GetComponent<Image>();
            _skipCatcher.color = new Color(0f, 0f, 0f, 0f);
            _skipCatcher.raycastTarget = true;

            _skipButton = go.GetComponent<Button>();
            _skipButton.transition = Selectable.Transition.None;
            _skipButton.navigation = new Navigation { mode = Navigation.Mode.None };
            _skipButton.onClick.AddListener(RequestSkip);
            _skipButton.enabled = false;
        }

        CanvasGroup BuildStatsPanel()
        {
            var panel = NewPanel("StatsPanel", new Vector2(1400f, 900f), new Vector2(0f, 20f));

            NewLabel(panel, "Heading", statsHeading, headingFontSize, headingColor,
                new Vector2(0f, 330f), new Vector2(1400f, 110f), TextAlignmentOptions.Center);
            NewLabel(panel, "Body", statsBody, bodyFontSize, textColor,
                new Vector2(0f, -30f), new Vector2(1320f, 560f), TextAlignmentOptions.Top);
            NewLabel(panel, "Footnote", statsFootnote, footnoteFontSize, footnoteColor,
                new Vector2(0f, -380f), new Vector2(1320f, 120f), TextAlignmentOptions.Top);

            return panel.GetComponent<CanvasGroup>();
        }

        CanvasGroup BuildHotlinePanel()
        {
            var panel = NewPanel("HotlinePanel", new Vector2(1400f, 1050f), new Vector2(0f, 0f));

            NewLabel(panel, "Heading", hotlineHeading, headingFontSize * 0.85f, headingColor,
                new Vector2(0f, 420f), new Vector2(1400f, 110f), TextAlignmentOptions.Center);
            NewLabel(panel, "Body", hotlineBody, bodyFontSize * 0.9f, textColor,
                new Vector2(0f, -60f), new Vector2(1320f, 830f), TextAlignmentOptions.Top);

            return panel.GetComponent<CanvasGroup>();
        }

        CanvasGroup BuildEndPanel()
        {
            var panel = NewPanel("EndPanel", new Vector2(1400f, 800f), Vector2.zero);

            NewLabel(panel, "EndLine", endLine, headingFontSize, headingColor,
                new Vector2(0f, 190f), new Vector2(1400f, 200f), TextAlignmentOptions.Center);
            BuildMenuButton(panel);

            return panel.GetComponent<CanvasGroup>();
        }

        void BuildMenuButton(RectTransform parent)
        {
            var go = new GameObject("MainMenuButton", typeof(RectTransform), typeof(Image), typeof(Button));
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            rect.anchoredPosition = new Vector2(0f, -150f);
            rect.sizeDelta = menuButtonSize;

            var image = go.GetComponent<Image>();
            image.sprite = Env3UiFactory.LinedPaper;
            image.type = Image.Type.Simple;
            image.raycastTarget = true;

            var button = go.GetComponent<Button>();
            button.targetGraphic = image;
            button.transition = Selectable.Transition.ColorTint;
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            button.colors = new ColorBlock
            {
                normalColor = Color.white,
                highlightedColor = new Color(1f, 0.97f, 0.86f),
                pressedColor = new Color(0.78f, 0.76f, 0.70f),
                selectedColor = Color.white,
                disabledColor = new Color(1f, 1f, 1f, 0.4f),
                colorMultiplier = 1f,
                fadeDuration = 0.1f
            };
            button.onClick.AddListener(ReturnToMainMenu);

            var label = NewLabel(rect, "Label", mainMenuButtonText, bodyFontSize * 1.15f, menuButtonTextColor,
                Vector2.zero, menuButtonSize, TextAlignmentOptions.Center);
            label.raycastTarget = false;
        }

        RectTransform NewPanel(string name, Vector2 size, Vector2 position)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasGroup));
            var rect = (RectTransform)go.transform;
            rect.SetParent(_canvasRect, false);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;

            var group = go.GetComponent<CanvasGroup>();
            group.alpha = 0f;
            group.blocksRaycasts = false;
            group.interactable = false;
            return rect;
        }

        TextMeshProUGUI NewLabel(RectTransform parent, string name, string content, float size, Color color,
            Vector2 position, Vector2 rectSize, TextAlignmentOptions alignment)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            rect.anchoredPosition = position;
            rect.sizeDelta = rectSize;

            var text = go.GetComponent<TextMeshProUGUI>();
            text.text = content;
            text.fontSize = size;
            text.color = color;
            text.alignment = alignment;
            text.raycastTarget = false;
            text.enableWordWrapping = true;

            // The shared material forces a white face and tints per label from vertex colour,
            // so a dark label over the paper button comes out dark and the light ones stay light.
            if (_font != null) Env3UiFactory.ApplySharedFont(text, _font, 0f, 0.5f);
            else Env3UiFactory.MakeLegible(text, 0f, 0.5f);

            return text;
        }

        /// <summary>
        /// Stops the camera drawing the room, once the room is behind a wall of black anyway.
        ///
        /// This exists because World Space UI is depth-tested against the scene like anything
        /// else, so a panel 1.35m from the head vanishes the moment the player turns to face a
        /// wall nearer than that. Distance cannot fix it: pulling the text in front of every
        /// wall would put it a hand's width from the eyes, which is painful to read in stereo.
        /// Disabling the depth test cannot fix it either, because the TextMeshPro distance
        /// field shader exposes no ZTest to switch off.
        ///
        /// So the room simply stops being drawn. With nothing but the ending on the layer, the
        /// depth buffer is empty and there is nothing left to occlude the text. It is also
        /// cheaper than rendering a bar full of NPCs nobody can see.
        /// </summary>
        void CullWorld()
        {
            var cam = _canvas != null ? _canvas.worldCamera : Camera.main;
            if (cam == null) return;

            _savedCullingMask = cam.cullingMask;
            _savedClearFlags = cam.clearFlags;
            _savedBackground = cam.backgroundColor;
            _worldCulled = true;

            cam.cullingMask = 1 << endingLayer;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Color.black;

            // The pointer line is the only thing the player still needs to see besides the
            // ending, or they cannot tell where they are aiming the Main Menu button.
            if (!keepPointerVisible) return;

            var rig = encounter != null && encounter.playerRig != null ? encounter.playerRig : cam.transform.root;
            if (rig == null) return;

            var lines = rig.GetComponentsInChildren<LineRenderer>(true);
            for (int i = 0; i < lines.Length; i++)
            {
                _pointerLines.Add(lines[i].gameObject);
                _pointerLayers.Add(lines[i].gameObject.layer);
                lines[i].gameObject.layer = endingLayer;
            }
        }

        /// <summary>
        /// Puts the room, the pointer and the ending itself back to how they started, so the
        /// encounter can be replayed without reloading the scene. Pair it with
        /// <see cref="AfterPartyEncounter.ResetEncounter"/>.
        /// </summary>
        public void RestoreWorld()
        {
            StopAllCoroutines();
            _played = false;
            _skipRequested = false;

            if (_blackoutRect != null) _blackoutRect.gameObject.SetActive(false);
            if (_canvasRect != null) _canvasRect.gameObject.SetActive(false);
            if (_blackout != null) _blackout.color = new Color(0f, 0f, 0f, 0f);
            if (_skipCatcher != null) _skipCatcher.raycastTarget = true;
            if (_statsGroup != null) _statsGroup.alpha = 0f;
            if (_hotlineGroup != null) _hotlineGroup.alpha = 0f;
            if (_endGroup != null)
            {
                _endGroup.alpha = 0f;
                _endGroup.blocksRaycasts = false;
                _endGroup.interactable = false;
            }

            if (!_worldCulled) return;
            _worldCulled = false;

            var cam = _canvas != null ? _canvas.worldCamera : Camera.main;
            if (cam != null)
            {
                cam.cullingMask = _savedCullingMask;
                cam.clearFlags = _savedClearFlags;
                cam.backgroundColor = _savedBackground;
            }

            for (int i = 0; i < _pointerLines.Count; i++)
                if (_pointerLines[i] != null) _pointerLines[i].layer = _pointerLayers[i];
            _pointerLines.Clear();
            _pointerLayers.Clear();
        }

        /// <summary>Puts a built object and everything under it on the ending's own layer.</summary>
        void SetLayer(GameObject go)
        {
            var all = go.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < all.Length; i++) all[i].gameObject.layer = endingLayer;
        }

        /// <summary>
        /// XR rays only hit a canvas that carries the toolkit's own raycaster. The type is taken
        /// off the dialogue canvas rather than referenced directly, matching how the rest of
        /// this folder stays clear of a hard dependency on one XR Interaction Toolkit version.
        /// </summary>
        void AddTrackedDeviceRaycaster(GameObject target)
        {
            Type type = null;

            if (dialogue != null && dialogue.canvas != null)
            {
                var components = dialogue.canvas.GetComponents<MonoBehaviour>();
                for (int i = 0; i < components.Length; i++)
                {
                    if (components[i] == null) continue;
                    if (components[i].GetType().Name != "TrackedDeviceGraphicRaycaster") continue;
                    type = components[i].GetType();
                    break;
                }
            }

            if (type == null)
            {
                var assemblies = AppDomain.CurrentDomain.GetAssemblies();
                for (int i = 0; i < assemblies.Length && type == null; i++)
                    type = assemblies[i].GetType("UnityEngine.XR.Interaction.Toolkit.UI.TrackedDeviceGraphicRaycaster");
            }

            if (type == null)
            {
                Debug.LogWarning("[EndingSequence] No TrackedDeviceGraphicRaycaster found. " +
                                 "The Main Menu button will not respond to an XR ray.", this);
                return;
            }

            if (target.GetComponent(type) == null) target.AddComponent(type);
        }

        // ------------------------------------------------------------------ flow

        /// <summary>Runs the ending. Safe to call twice; the second call is ignored.</summary>
        public void Play()
        {
            if (_played) return;
            _played = true;

            if (_canvasRect == null || _blackoutRect == null) return;

            // Camera.main can still have been missing at Awake, so the head is confirmed here too.
            if (head == null && Camera.main != null) head = Camera.main.transform;
            if (head != null)
            {
                if (_canvasRect.parent != head) _canvasRect.SetParent(head, false);
                if (_blackoutRect.parent != head) _blackoutRect.SetParent(head, false);
            }
            if (_canvas.worldCamera == null) _canvas.worldCamera = Camera.main;

            _blackoutRect.localPosition = new Vector3(0f, 0f, blackoutDistance);
            _blackoutRect.localRotation = Quaternion.identity;
            _canvasRect.localPosition = new Vector3(0f, 0f, contentDistance);
            _canvasRect.localRotation = Quaternion.identity;

            _blackoutRect.gameObject.SetActive(true);
            _canvasRect.gameObject.SetActive(true);

            StartCoroutine(Run());
        }

        IEnumerator Run()
        {
            yield return new WaitForSeconds(startDelay);

            yield return FadeBlackout(0f, 1f, fadeToBlackDuration);
            CullWorld();
            onFadedToBlack.Invoke();
            _skipButton.enabled = true;

            yield return new WaitForSeconds(holdOnBlack);

            yield return RunPhase(_statsGroup, statsHold);
            yield return RunPhase(_hotlineGroup, hotlineHold);

            // The end screen is the one panel that stays, so nothing about it is skippable and
            // the catcher stops swallowing clicks that were aimed at the button.
            _skipButton.enabled = false;
            _skipCatcher.raycastTarget = false;

            yield return FadeGroup(_endGroup, 0f, 1f, panelFadeDuration);
            _endGroup.blocksRaycasts = true;
            _endGroup.interactable = true;
        }

        IEnumerator RunPhase(CanvasGroup group, float hold)
        {
            yield return FadeGroup(group, 0f, 1f, panelFadeDuration);

            _skipRequested = false;
            _phaseStarted = Time.time;

            float elapsed = 0f;
            while (elapsed < hold && !_skipRequested)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            yield return FadeGroup(group, 1f, 0f, panelFadeDuration);
        }

        void RequestSkip()
        {
            if (Time.time - _phaseStarted < minTimeBeforeSkip) return;
            _skipRequested = true;
        }

        IEnumerator FadeBlackout(float from, float to, float duration)
        {
            float t = 0f;
            duration = Mathf.Max(0.01f, duration);
            var color = _blackout.color;

            while (t < duration)
            {
                t += Time.deltaTime;
                color.a = Mathf.Lerp(from, to, Mathf.SmoothStep(0f, 1f, t / duration));
                _blackout.color = color;
                yield return null;
            }

            color.a = to;
            _blackout.color = color;
        }

        IEnumerator FadeGroup(CanvasGroup group, float from, float to, float duration)
        {
            float t = 0f;
            duration = Mathf.Max(0.01f, duration);
            group.alpha = from;

            while (t < duration)
            {
                t += Time.deltaTime;
                group.alpha = Mathf.Lerp(from, to, Mathf.SmoothStep(0f, 1f, t / duration));
                yield return null;
            }

            group.alpha = to;
        }

        // ------------------------------------------------------------------ exit

        /// <summary>Leaves the scene. Wired to the Main Menu button, and callable on its own.</summary>
        public void ReturnToMainMenu()
        {
            onReturningToMenu.Invoke();

            if (string.IsNullOrEmpty(mainMenuSceneName) || !Application.CanStreamedLevelBeLoaded(mainMenuSceneName))
            {
                Debug.LogError("[EndingSequence] Scene '" + mainMenuSceneName +
                               "' is not in Build Settings, so it cannot be loaded.", this);
                return;
            }

            SceneManager.LoadScene(mainMenuSceneName);
        }
    }
}
