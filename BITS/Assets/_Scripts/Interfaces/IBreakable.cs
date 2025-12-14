using UnityEngine;

namespace MultiplayerGame
{
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
    }
}
