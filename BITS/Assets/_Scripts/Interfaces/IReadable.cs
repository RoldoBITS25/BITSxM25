using UnityEngine;

namespace MultiplayerGame
{
    /// <summary>
    /// Interface for objects that can display text when read/interacted with
    /// Reading interactions are local-only and do not send data to the backend
    /// </summary>
    public interface IReadable
    {
        /// <summary>
        /// Called when a player reads/interacts with this object
        /// </summary>
        /// <param name="playerId">ID of the player reading the object</param>
        void OnInteract(string playerId);
        
        /// <summary>
        /// Get the text to display when read
        /// </summary>
        string GetDisplayText();
        
        /// <summary>
        /// Set highlight state for visual feedback
        /// </summary>
        void SetHighlight(bool highlighted);
    }
}
