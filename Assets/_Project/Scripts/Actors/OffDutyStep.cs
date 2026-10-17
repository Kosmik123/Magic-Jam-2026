using System;
using UnityEngine;

namespace MagicJam.Actors
{
    /// <summary>
    /// One off-duty behaviour: walk to a spot, play an animation for a while (coffee, gossip, catering...).
    /// </summary>
    [Serializable]
    public class OffDutyStep
    {
        [SerializeField]
        private Transform spot;

        [Tooltip("Animator trigger played on arrival. Optional.")]
        [SerializeField]
        private string animTrigger;

        [SerializeField]
        private float duration = 5f;

        public Transform Spot => spot;
        public string AnimTrigger => animTrigger;
        public float Duration => duration;
    }
}
