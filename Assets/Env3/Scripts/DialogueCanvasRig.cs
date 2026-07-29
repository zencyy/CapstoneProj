using UnityEngine;

namespace Env3.Anxiety
{
    /// <summary>
    /// Keeps the dialogue panel floating in front of the player's head.
    /// At low <see cref="lockIn"/> it lags behind comfortably and you can look away from it.
    /// As lockIn rises the panel snaps to the head and starts to tremble, so by the panic
    /// stage there is nowhere to look that isn't the conversation.
    /// </summary>
    [DisallowMultipleComponent]
    public class DialogueCanvasRig : MonoBehaviour
    {
        [Header("Target")]
        [Tooltip("Player head. Left empty, the rig grabs Camera.main at Awake.")]
        public Transform head;

        [Header("Placement")]
        public float distance = 1.5f;
        public float verticalOffset = -0.06f;
        [Tooltip("Keep the panel level with the horizon instead of tilting with the player's pitch.")]
        public bool lockPitch = true;

        [Header("Follow")]
        [Tooltip("Follow responsiveness when calm. Low values feel loose and comfortable.")]
        public float relaxedSharpness = 2.6f;
        [Tooltip("Follow responsiveness when fully locked in. High values feel head-locked and inescapable.")]
        public float lockedSharpness = 26f;

        [Header("Distress")]
        [Range(0f, 1f)] public float lockIn;
        [Tooltip("Metres of positional tremble at lockIn = 1.")]
        public float maxShake = 0.018f;
        [Tooltip("Extra one-off shake, decays on its own. Use PunchShake().")]
        public float punchDecay = 4f;

        float _punch;
        Vector3 _velocity;
        float _seed;

        void Awake()
        {
            _seed = Random.value * 100f;
            if (head == null && Camera.main != null) head = Camera.main.transform;
        }

        void OnEnable()
        {
            if (head != null) SnapToHead();
        }

        /// <summary>Drop the panel straight into place with no easing.</summary>
        public void SnapToHead()
        {
            if (head == null) return;
            transform.position = DesiredPosition();
            transform.rotation = DesiredRotation(transform.position);
        }

        /// <summary>Kick the panel, for when a click during the panic stage spawns yet more choices.</summary>
        public void PunchShake(float amount = 1f)
        {
            _punch = Mathf.Max(_punch, amount);
        }

        void LateUpdate()
        {
            if (head == null)
            {
                if (Camera.main == null) return;
                head = Camera.main.transform;
            }

            float sharpness = Mathf.Lerp(relaxedSharpness, lockedSharpness, lockIn);
            float k = 1f - Mathf.Exp(-sharpness * Time.deltaTime);

            Vector3 target = DesiredPosition() + ShakeOffset();
            transform.position = Vector3.Lerp(transform.position, target, k);
            transform.rotation = Quaternion.Slerp(transform.rotation, DesiredRotation(transform.position), k);

            if (_punch > 0f) _punch = Mathf.Max(0f, _punch - punchDecay * Time.deltaTime);
        }

        Vector3 DesiredPosition()
        {
            Vector3 forward = head.forward;
            if (lockPitch)
            {
                forward.y = 0f;
                if (forward.sqrMagnitude < 0.0001f) forward = head.up; // player is staring at their feet
                forward.Normalize();
            }

            return head.position + forward * distance + Vector3.up * verticalOffset;
        }

        Quaternion DesiredRotation(Vector3 at)
        {
            Vector3 away = at - head.position;
            if (away.sqrMagnitude < 0.0001f) return transform.rotation;
            return Quaternion.LookRotation(away.normalized, Vector3.up);
        }

        Vector3 ShakeOffset()
        {
            float amount = maxShake * lockIn + maxShake * 1.6f * _punch;
            if (amount <= 0.00001f) return Vector3.zero;

            float t = Time.unscaledTime * 24f;
            return new Vector3(
                Mathf.PerlinNoise(_seed, t) - 0.5f,
                Mathf.PerlinNoise(_seed + 17f, t) - 0.5f,
                0f) * (amount * 2f);
        }
    }
}
