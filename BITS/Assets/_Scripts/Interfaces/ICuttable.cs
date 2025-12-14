using UnityEngine;

namespace MultiplayerGame
{
    /// <summary>
    /// Interface for objects that can be cut
    /// </summary>
    public interface ICuttable
    {
        /// <summary>
        /// Check if the object can currently be cut
        /// </summary>
        bool CanBeCut();

        /// <summary>
        /// Called when the object is cut
        /// </summary>
        /// <param name="cutPosition">Position where the cut occurred</param>
        void OnCut(Vector2 cutPosition);

        /// <summary>
        /// Get the transform of the cuttable object
        /// </summary>
        Transform GetTransform();
    }
}
