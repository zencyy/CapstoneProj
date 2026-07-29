using UnityEngine;
using UnityEngine.Events;

namespace Env3.Anxiety
{
    /// <summary>
    /// Walks MainDude over to the player, then keeps him facing them for the rest of the
    /// conversation. Deliberately does not use a NavMeshAgent: nothing is baked in this
    /// scene, and the route from the bar to the door corner is open floor.
    /// </summary>
    public class MainDudeApproach : MonoBehaviour
    {
        public enum State { Waiting, Turning, Walking, Arrived }

        [Header("Target")]
        [Tooltip("Player rig root. Empty = first object tagged Player.")]
        public Transform player;
        [Tooltip("Player head, used for eye contact. Empty = Camera.main.")]
        public Transform playerHead;

        [Header("Movement")]
        public float moveSpeed = 1.15f;
        public float turnSpeedDeg = 240f;
        [Tooltip("How close he gets during normal conversation.")]
        public float stopDistance = 2.2f;
        [Tooltip("How close he gets once the player starts to panic. Uncomfortably close is the point.")]
        public float leanInDistance = 1.55f;
        [Tooltip("Degrees he must be turned within before he starts moving.")]
        public float turnBeforeWalkDeg = 45f;

        [Header("Grounding")]
        [Tooltip("Raycast down from this height above him to stay glued to the floor.")]
        public float groundProbeHeight = 3f;
        public float groundProbeDistance = 12f;
        public LayerMask groundMask = ~0;

        [Header("Life")]
        [Tooltip("Vertical bob while walking. There is no walk animation in this scene, so this stops him looking like he is on rails.")]
        public float bobHeight = 0.022f;
        public float bobSpeed = 7.5f;

        [Header("Head")]
        [Tooltip("Head bone, used by the dialogue UI to keep choices off his face. Empty = found automatically.")]
        public Transform headBone;

        [Header("Animation")]
        [Tooltip("Animator to drive. Empty = the one on this object.")]
        public Animator animator;
        [Tooltip("Bool parameter raised while he is walking over. Ignored if the controller has no such parameter.")]
        public string walkingParameter = "IsWalking";
        [Tooltip("Root motion would fight this script for control of his position, so it is forced off.")]
        public bool forceRootMotionOff = true;

        [Header("Events")]
        public UnityEvent onApproachStarted;
        public UnityEvent onReachedPlayer;

        State _state = State.Waiting;
        Transform _resolvedHead;
        bool _announcedArrival;
        bool _hasWalkParameter;
        bool _walkFlag;
        float _footOffset;
        float _targetDistance;
        float _bobPhase;

        public State CurrentState { get { return _state; } }

        /// <summary>Where his face is in world space, so UI can avoid pasting itself over it.</summary>
        public Vector3 HeadPosition
        {
            get
            {
                var head = ResolveHead();
                return head != null ? head.position : FallbackHeadPosition();
            }
        }

        Transform ResolveHead()
        {
            if (headBone != null) return headBone;
            if (_resolvedHead != null) return _resolvedHead;

            // The attached hair and beard rigs carry their own "Head" bones, so take the
            // shallowest match, which is the one on his actual skeleton.
            Transform best = null;
            int bestDepth = int.MaxValue;
            var all = GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < all.Length; i++)
            {
                if (!string.Equals(all[i].name, "Head", System.StringComparison.OrdinalIgnoreCase)) continue;

                int depth = 0;
                for (var p = all[i]; p != null && p != transform; p = p.parent) depth++;
                if (depth < bestDepth) { bestDepth = depth; best = all[i]; }
            }

            _resolvedHead = best;
            return _resolvedHead;
        }

        Vector3 FallbackHeadPosition()
        {
            var rends = GetComponentsInChildren<Renderer>();
            if (rends.Length == 0) return transform.position + Vector3.up * 1.7f;

            var b = rends[0].bounds;
            for (int i = 1; i < rends.Length; i++) b.Encapsulate(rends[i].bounds);
            return new Vector3(b.center.x, b.max.y - 0.14f, b.center.z);
        }

        void Awake()
        {
            _targetDistance = stopDistance;
            ResolveTargets();
            ResolveAnimator();

            // Preserve whatever vertical offset he was authored with relative to the floor.
            RaycastHit hit;
            if (Physics.Raycast(transform.position + Vector3.up * groundProbeHeight, Vector3.down,
                    out hit, groundProbeDistance, groundMask, QueryTriggerInteraction.Ignore))
                _footOffset = transform.position.y - hit.point.y;
        }

        void ResolveTargets()
        {
            if (player == null)
            {
                var tagged = GameObject.FindGameObjectWithTag("Player");
                if (tagged != null) player = tagged.transform;
            }
            if (playerHead == null && Camera.main != null) playerHead = Camera.main.transform;
            if (player == null && playerHead != null) player = playerHead;
        }

        /// <summary>Turn towards the player and walk over. Safe to call more than once.</summary>
        public void BeginApproach()
        {
            if (_state != State.Waiting) return;
            ResolveTargets();
            if (player == null)
            {
                Debug.LogWarning("[MainDudeApproach] No player to approach.", this);
                return;
            }

            _state = State.Turning;
            onApproachStarted.Invoke();
        }

        /// <summary>Close the gap. Called when the dialogue tips into the panic stage.</summary>
        public void SetLeanIn(bool leaning)
        {
            _targetDistance = leaning ? leanInDistance : stopDistance;
            if (_state == State.Arrived && leaning) _state = State.Walking;
        }

        /// <summary>Put him back where he started, ready to run the encounter again.</summary>
        public void ResetApproach(Vector3 position, Quaternion rotation)
        {
            _state = State.Waiting;
            _announcedArrival = false;
            _targetDistance = stopDistance;
            transform.SetPositionAndRotation(position, rotation);
        }

        void ResolveAnimator()
        {
            if (animator == null) animator = GetComponent<Animator>();
            if (animator == null) return;

            if (forceRootMotionOff) animator.applyRootMotion = false;

            _hasWalkParameter = false;
            if (animator.runtimeAnimatorController == null || string.IsNullOrEmpty(walkingParameter)) return;

            var parameters = animator.parameters;
            for (int i = 0; i < parameters.Length; i++)
            {
                if (parameters[i].type != AnimatorControllerParameterType.Bool) continue;
                if (parameters[i].name != walkingParameter) continue;
                _hasWalkParameter = true;
                break;
            }
        }

        /// <summary>Mirrors the walk state onto the animator, if the controller exposes the parameter.</summary>
        void ApplyWalkFlag()
        {
            if (!_hasWalkParameter || animator == null) return;

            bool walking = _state == State.Walking;
            if (walking == _walkFlag) return;

            _walkFlag = walking;
            animator.SetBool(walkingParameter, walking);
        }

        void Update()
        {
            ApplyWalkFlag();

            if (_state == State.Waiting || player == null) return;

            Vector3 targetPoint = player.position;
            Vector3 toPlayer = targetPoint - transform.position;
            toPlayer.y = 0f;
            float distance = toPlayer.magnitude;
            if (distance < 0.001f) return;

            Vector3 direction = toPlayer / distance;
            FaceDirection(direction);

            if (_state == State.Turning)
            {
                float angle = Vector3.Angle(transform.forward, direction);
                if (angle <= turnBeforeWalkDeg) _state = State.Walking;
                return;
            }

            if (_state == State.Walking)
            {
                if (distance <= _targetDistance)
                {
                    _state = State.Arrived;
                    StickToGround(0f);
                    // Only the first arrival opens the conversation. Leaning in during the
                    // panic stage sends him back to Walking, and that second arrival must
                    // not re-fire the event.
                    if (!_announcedArrival)
                    {
                        _announcedArrival = true;
                        onReachedPlayer.Invoke();
                    }
                    return;
                }

                float step = Mathf.Min(moveSpeed * Time.deltaTime, distance - _targetDistance);
                transform.position += direction * step;

                _bobPhase += Time.deltaTime * bobSpeed;
                StickToGround(Mathf.Abs(Mathf.Sin(_bobPhase)) * bobHeight);
                return;
            }

            // Arrived: if lean-in shortened the target distance, close the remaining gap.
            if (distance > _targetDistance + 0.05f) _state = State.Walking;
            else StickToGround(0f);
        }

        void FaceDirection(Vector3 direction)
        {
            Quaternion want = Quaternion.LookRotation(direction, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, want, turnSpeedDeg * Time.deltaTime);
        }

        void StickToGround(float extraHeight)
        {
            RaycastHit hit;
            if (Physics.Raycast(transform.position + Vector3.up * groundProbeHeight, Vector3.down,
                    out hit, groundProbeDistance, groundMask, QueryTriggerInteraction.Ignore))
            {
                Vector3 p = transform.position;
                p.y = hit.point.y + _footOffset + extraHeight;
                transform.position = p;
            }
        }
    }
}
