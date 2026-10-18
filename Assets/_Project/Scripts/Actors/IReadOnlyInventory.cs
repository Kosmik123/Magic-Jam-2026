namespace MagicJam.Actors
{
    /// <summary>Read access to what the actor holds in its hands.</summary>
    public interface IReadOnlyInventory
    {
        bool IsFull { get; }

        bool IsEmpty { get; }

        /// <summary>Item taken into a hand first, or null if the hands are empty.</summary>
        Item Oldest { get; }

        /// <summary>Hand the next item goes to: a free one (right first), or the one with the oldest item when both are full.</summary>
        Hand NextHand { get; }

        /// <summary>Item held in the hand, or null if the hand is empty.</summary>
        Item Get(Hand hand);

        bool Contains(Item item);

        /// <summary>Returns true and the hand holding the item if the actor holds it.</summary>
        bool TryGetHand(Item item, out Hand hand);
    }
}
