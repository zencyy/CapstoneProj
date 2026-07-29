using UnityEngine;
using UnityEngine.Events;

namespace Env3.Anxiety
{
    /// <summary>
    /// Fires once when the player walks into a box volume.
    /// Uses a real trigger collider, and also checks the head position directly each frame,
    /// because an XR rig can be teleported straight past a trigger without ever colliding with it.
    /// </summary>
    [RequireComponent(typeof(BoxCollider))]
    public class PlayerAreaTrigger : MonoBehaviour
    {
        [Header("Detection")]
        public string playerTag = "Player";
        [Tooltip("Also test the player's head against the box every frame. Catches teleport locomotion.")]
        public bool proximityFallback = true;
        [Tooltip("Head transform used by the fallback test. Empty = Camera.main.")]
        public Transform playerHead;

        [Header("Behaviour")]
        public bool triggerOnce = true;

        [Header("Events")]
        public UnityEvent onPlayerEntered;

        [Header("Gizmo")]
        public Color gizmoColor = new Color(0.3f, 0.85f, 1f, 0.25f);

        BoxCollider _box;
        bool _fired;

        public bool HasFired { get { return _fired; } }

        void Reset()
        {
            var box = GetComponent<BoxCollider>();
            box.isTrigger = true;
            box.size = new Vector3(4.5f, 3f, 3.4f);
        }

        void Awake()
        {
            _box = GetComponent<BoxCollider>();
            _box.isTrigger = true;
            if (playerHead == null && Camera.main != null) playerHead = Camera.main.transform;
        }

        void Update()
        {
            if (!proximityFallback || (_fired && triggerOnce)) return;

            if (playerHead == null)
            {
                if (Camera.main == null) return;
                playerHead = Camera.main.transform;
            }

            if (Contains(playerHead.position)) Fire();
        }

        void OnTriggerEnter(Collider other)
        {
            if (_fired && triggerOnce) return;
            if (IsPlayer(other)) Fire();
        }

        bool IsPlayer(Collider other)
        {
            if (other == null) return false;
            if (!string.IsNullOrEmpty(playerTag) && other.CompareTag(playerTag)) return true;
            // An XR rig's body collider is a CharacterController on the rig root.
            return other.GetComponentInParent<CharacterController>() != null;
        }

        bool Contains(Vector3 worldPoint)
        {
            if (_box == null) return false;
            Vector3 local = transform.InverseTransformPoint(worldPoint) - _box.center;
            Vector3 half = _box.size * 0.5f;
            return Mathf.Abs(local.x) <= half.x
                && Mathf.Abs(local.y) <= half.y
                && Mathf.Abs(local.z) <= half.z;
        }

        void Fire()
        {
            if (_fired && triggerOnce) return;
            _fired = true;
            onPlayerEntered.Invoke();
        }

        /// <summary>Allow the volume to fire again. Handy when replaying the encounter.</summary>
        public void ResetTrigger()
        {
            _fired = false;
        }

        void OnDrawGizmos()
        {
            var box = _box != null ? _box : GetComponent<BoxCollider>();
            if (box == null) return;

            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.color = gizmoColor;
            Gizmos.DrawCube(box.center, box.size);
            Gizmos.color = new Color(gizmoColor.r, gizmoColor.g, gizmoColor.b, 1f);
            Gizmos.DrawWireCube(box.center, box.size);
        }
    }
}
