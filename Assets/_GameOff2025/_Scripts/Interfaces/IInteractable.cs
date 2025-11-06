namespace Route24.Core
{
    public interface IInteractable
    {
        /// <summary>Text to display when looking at the object.</summary>
        string GetInteractionText();

        /// <summary>Can the player currently interact with this object?</summary>
        bool CanInteract();
        
        /// <summary>Can we show the interaction text to the player?</summary>
        bool CanShowMessage();

        /// <summary>Called when the player clicks or presses Use.</summary>
        void OnInteract();
    }
}