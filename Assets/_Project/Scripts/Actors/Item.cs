using MagicJam.CameraSystem.Shots;
using UnityEngine;

namespace MagicJam.Actors
{
    /// <summary>A prop the actor can take into a hand.</summary>
    [RequireComponent(typeof(Frameable))]
    public class Item : MonoBehaviour
    {
        private Frameable frameable;

        public Frameable Frameable => frameable;

        private void Awake()
        {
            frameable = GetComponent<Frameable>();
        }
    }
}
