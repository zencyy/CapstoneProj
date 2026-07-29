using UnityEngine;

namespace Env3.Anxiety
{
    /// <summary>
    /// Desynchronises a crowd. Without this, every NPC sharing an Animator Controller starts
    /// on frame zero at identical speed, and a dozen dancers move as one organism.
    /// </summary>
    [RequireComponent(typeof(Animator))]
    [DisallowMultipleComponent]
    public class AnimatorStartOffset : MonoBehaviour
    {
        [Tooltip("Start somewhere random in the loop instead of at the beginning.")]
        public bool randomizePhase = true;

        [Tooltip("Vary playback speed slightly so the crowd drifts apart instead of staying locked.")]
        public bool randomizeSpeed = true;
        public Vector2 speedRange = new Vector2(0.9f, 1.1f);

        [Tooltip("Root motion would walk these NPCs away from where they were placed.")]
        public bool forceRootMotionOff = true;

        void Start()
        {
            var animator = GetComponent<Animator>();
            if (animator == null || animator.runtimeAnimatorController == null) return;

            if (forceRootMotionOff) animator.applyRootMotion = false;

            if (randomizePhase)
            {
                var state = animator.GetCurrentAnimatorStateInfo(0);
                animator.Play(state.fullPathHash, 0, Random.value);
            }

            if (randomizeSpeed) animator.speed = Random.Range(speedRange.x, speedRange.y);
        }
    }
}
