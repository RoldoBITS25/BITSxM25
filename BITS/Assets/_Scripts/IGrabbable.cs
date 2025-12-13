using UnityEngine;

namespace MultiplayerGame
{
    /// <summary>
    /// Interface for objects that can be grabbed and held by the player
    /// </summary>
    public interface IGrabbable
    {
        /// <summary>
        /// Check if the object can currently be grabbed
        /// </summary>
        bool CanBeGrabbed();

        /// <summary>
        /// Called when the object is grabbed by a player
        /// </summary>
        /// <param name="playerId">ID of the player grabbing the object</param>
        void OnGrabbed(string playerId);

        /// <summary>
        /// Called when the object is released by the player
        /// </summary>
        void OnReleased();

        /// <summary>
        /// Get the transform of the grabbable object
        /// </summary>
        Transform GetTransform();
    }
}
