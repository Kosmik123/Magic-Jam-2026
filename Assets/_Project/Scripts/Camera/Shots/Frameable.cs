using System.Collections.Generic;
using UnityEngine;

namespace MagicJam.CameraSystem.Shots
{
    /// <summary>
    /// Marks an object that can take part in a shot (an actor or a prop).
    /// Registers itself so the shot detection does not have to search the scene.
    /// </summary>
    public class Frameable : MonoBehaviour
    {
        private const float FallbackSize = 0.1f;

        private static readonly List<Frameable> all = new List<Frameable>();

        [SerializeField]
        private FrameableKind kind;

        [Tooltip("Renderers that define the bounds. Empty = all renderers in children.")]
        [SerializeField]
        private Renderer[] boundsRenderers;

        [Header("Humanoid points")]
        [SerializeField]
        private Transform head;

        [SerializeField]
        private Transform waist;

        [SerializeField]
        private Transform knees;

        [SerializeField]
        private Transform feet;

        private Renderer[] renderers;

        public static IReadOnlyList<Frameable> All => all;

        public FrameableKind Kind => kind;

        public Transform Head => head;

        public Transform Waist => waist;

        public Transform Knees => knees;

        public Transform Feet => feet;

        public Bounds WorldBounds
        {
            get
            {
                if (renderers == null || renderers.Length == 0)
                    return new Bounds(transform.position, Vector3.one * FallbackSize);

                Bounds bounds = renderers[0].bounds;
                for (int i = 1; i < renderers.Length; i++)
                    bounds.Encapsulate(renderers[i].bounds);

                return bounds;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            all.Clear();
        }

        private void Awake()
        {
            bool hasExplicitRenderers = boundsRenderers != null && boundsRenderers.Length > 0;
            renderers = hasExplicitRenderers ? boundsRenderers : GetComponentsInChildren<Renderer>();
        }

        private void OnEnable()
        {
            all.Add(this);
        }

        private void OnDisable()
        {
            all.Remove(this);
        }
    }
}
