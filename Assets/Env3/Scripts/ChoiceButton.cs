using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Env3.Anxiety
{
    /// <summary>
    /// One answer the player can blurt out. Works with an XR ray, a gaze interactor or a
    /// plain mouse, because it only listens for standard pointer events.
    /// The root is what layout groups position; <see cref="visual"/> is what trembles,
    /// so the shake never fights the layout.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class ChoiceButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        [Header("Parts")]
        public RectTransform visual;
        public Image background;
        public TextMeshProUGUI label;
        public CanvasGroup group;

        [Header("Colours")]
        // Text is white in both states. Hover lifts the plate instead of inverting it, so a
        // choice is never dependent on being pointed at to be readable.
        public Color idleBackground = new Color(0.05f, 0.05f, 0.07f, 0.72f);
        public Color hoverBackground = new Color(0.22f, 0.23f, 0.28f, 0.92f);
        public Color idleText = Color.white;
        public Color hoverText = Color.white;

        [Header("Feel")]
        public float hoverScale = 1.04f;
        public float colourSharpness = 14f;

        [Header("Panic state")]
        public bool isPanicChoice = false;
        public Color panicBackgroundColor = new Color(0.08f, 0.08f, 0.1f, 0.7f);
        [Tooltip("Text colour while this is a flood thought. Kept light so a hot plate stays readable.")]
        public Color panicTextColor = Color.white;

        public int Index { get; private set; }
        public string Text { get { return label != null ? label.text : string.Empty; } }
        public RectTransform Rect { get; private set; }

        Action<ChoiceButton> _onPicked;
        Vector2 _restOffset;
        float _restAngle;
        float _jitter;
        float _seed;
        bool _hovered;
        bool _locked;
        bool _introRunning;

        void Awake()
        {
            Rect = (RectTransform)transform;
            if (visual == null) visual = Rect;
            _seed = UnityEngine.Random.value * 100f;
            Env3UiFactory.MakeLegible(label);
            ApplyColours(true);
        }

        public void Bind(int index, string text, Action<ChoiceButton> onPicked)
        {
            Index = index;
            _onPicked = onPicked;
            if (label != null)
            {
                label.text = text;
                label.ForceMeshUpdate();
            }
        }

        /// <summary>Baseline rotation for this button; tremble is applied on top of it.</summary>
        public void SetRestAngle(float degrees)
        {
            _restAngle = degrees;
            if (visual != null) visual.localRotation = Quaternion.Euler(0f, 0f, _restAngle);
        }

        public void SetJitter(float amount)
        {
            _jitter = Mathf.Max(0f, amount);
            if (_jitter <= 0.001f && visual != null)
            {
                visual.anchoredPosition = _restOffset;
                visual.localRotation = Quaternion.Euler(0f, 0f, _restAngle);
            }
        }

        public void SetAlpha(float a)
        {
            if (group != null) group.alpha = a;
        }

        public void SetPanicColor(Color background, Color text)
        {
            isPanicChoice = true;
            panicBackgroundColor = background;
            panicTextColor = text;
            ApplyColours(true);
        }

        /// <summary>
        /// Draws this thought bigger or smaller than the rest of the flood. Applied to the root
        /// rather than <see cref="visual"/>, which the intro and hover tweens already own.
        /// </summary>
        public void SetSize(float multiplier)
        {
            transform.localScale = Vector3.one * Mathf.Max(0.05f, multiplier);
        }

        /// <summary>Stop accepting input, once the encounter has moved on.</summary>
        public void Lock()
        {
            _locked = true;
            _hovered = false;
            if (group != null) group.blocksRaycasts = false;
        }

        /// <summary>Fade and pop the button in. Returns immediately; the tween runs on the component.</summary>
        public void PlayIntro(float duration = 0.16f)
        {
            StopAllCoroutines();
            StartCoroutine(IntroRoutine(Mathf.Max(0.01f, duration)));
        }

        System.Collections.IEnumerator IntroRoutine(float duration)
        {
            _introRunning = true;
            float t = 0f;
            Vector3 from = Vector3.one * 0.86f;
            while (t < duration)
            {
                t += Time.deltaTime;
                float k = Mathf.SmoothStep(0f, 1f, t / duration);
                if (group != null) group.alpha = k;
                if (visual != null) visual.localScale = Vector3.Lerp(from, Vector3.one, k);
                yield return null;
            }
            if (group != null) group.alpha = 1f;
            if (visual != null) visual.localScale = Vector3.one;
            _introRunning = false;
        }

        void Update()
        {
            if (visual == null) return;

            if (_jitter > 0.001f)
            {
                float t = Time.unscaledTime * 9f;
                float nx = Mathf.PerlinNoise(_seed, t) - 0.5f;
                float ny = Mathf.PerlinNoise(_seed + 13f, t) - 0.5f;
                float nr = Mathf.PerlinNoise(_seed + 29f, t * 0.7f) - 0.5f;

                visual.anchoredPosition = _restOffset + new Vector2(nx, ny) * (_jitter * 22f);
                visual.localRotation = Quaternion.Euler(0f, 0f, _restAngle + nr * _jitter * 9f);
            }

            ApplyColours(false);
        }

        void ApplyColours(bool immediate)
        {
            float k = immediate ? 1f : 1f - Mathf.Exp(-colourSharpness * Time.deltaTime);
            bool on = _hovered && !_locked;

            if (background != null)
            {
                Color targetBg = isPanicChoice ? panicBackgroundColor : (on ? hoverBackground : idleBackground);
                background.color = Color.Lerp(background.color, targetBg, k);
            }
            if (label != null)
            {
                Color targetText = isPanicChoice ? panicTextColor : (on ? hoverText : idleText);
                label.color = Color.Lerp(label.color, targetText, k);
            }
            if (visual != null && !_locked)
            {
                // The intro tween owns scale until it finishes, then hover takes over.
                if (!_introRunning)
                {
                    float target = on ? hoverScale : 1f;
                    float s = Mathf.Lerp(visual.localScale.x, target, k);
                    visual.localScale = new Vector3(s, s, 1f);
                }
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (_locked) return;
            _hovered = true;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _hovered = false;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (_locked) return;
            var cb = _onPicked;
            if (cb != null) cb(this);
        }
    }
}
