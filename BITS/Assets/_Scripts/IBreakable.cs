using UnityEngine;

namespace MultiplayerGame
{
    /// <summary>
    /// Interface for objects that can be broken
    /// </summary>
    public interface IBreakable
    {
        /// <summary>
        /// Check if the object can currently be broken
        /// </summary>
        bool CanBeBroken();

        /// <summary>
        /// Called when the object is broken
        /// </summary>
        void OnBreak();

        /// <summary>
        /// Get the unique ID of this object
        /// </summary>
        string GetObjectId();

        /// <summary>
        /// Get the transform of the breakable object
        /// </summary>
        Transform GetTransform();
    }
}
