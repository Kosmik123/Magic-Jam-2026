using System;
using UnityEngine;

namespace MagicJam.Actors
{
    /// <summary>The actor's two hands, each holding at most one item.</summary>
    [Serializable]
    public class Inventory : IReadOnlyInventory
    {
        [SerializeField]
        private Item leftHand;

        [SerializeField]
        private Item rightHand;

        public bool IsFull => leftHand && rightHand;

        public bool IsEmpty => leftHand == null && rightHand == null;

        public Item Get(Hand hand)
        {
            return hand == Hand.Left ? leftHand : rightHand;
        }

        public bool Contains(Item item)
        {
            return TryGetHand(item, out _);
        }

        public bool TryGetHand(Item item, out Hand hand)
        {
            hand = Hand.Right;
            if (item == null)
                return false;

            if (rightHand == item)
                return true;

            hand = Hand.Left;
            return leftHand == item;
        }

        /// <summary>Puts the item into a free hand (right first). Returns false if both hands are full.</summary>
        public bool TryAdd(Item item, out Hand hand)
        {
            hand = Hand.Right;
            if (item == null)
                return false;

            if (TryGetHand(item, out hand))
                return true;

            if (IsFull)
                return false;

            hand = rightHand == null ? Hand.Right : Hand.Left;
            Set(hand, item);
            return true;
        }

        /// <summary>Empties the hand holding the item. Returns false if the actor does not hold it.</summary>
        public bool Remove(Item item)
        {
            if (TryGetHand(item, out Hand hand) == false)
                return false;

            Set(hand, null);
            return true;
        }

        private void Set(Hand hand, Item item)
        {
            if (hand == Hand.Left)
                leftHand = item;
            else
                rightHand = item;
        }
    }
}
