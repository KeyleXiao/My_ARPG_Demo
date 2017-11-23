using System;
using System.Collections;
using UnityEngine;

namespace com.ootii.Actors.Combat
{
    /// <summary>
    /// Used by the CombatManager to hook into the unity update process. This allows us
    /// to update the combat and track old values
    /// </summary>
    [Serializable]
    public class CombatManagerCore : MonoBehaviour
    {
        /// <summary>
        /// Raised first when the object comes into existance. Called
        /// even if script is not enabled.
        /// </summary>
        void Awake()
        {
            // Don't destroyed automatically when loading a new scene
            DontDestroyOnLoad(gameObject);

            // Initialize the manager
            CombatManager.Awake();
        }

        /// <summary>
        /// Called after the Awake() and before any Update() is called.
        /// </summary>
        public IEnumerator Start()
        {
            // Create the coroutine here so we don't re-create over and over
            WaitForEndOfFrame lWaitForEndOfFrame = new WaitForEndOfFrame();

            // Loop endlessly so we can process the combat
            while (true)
            {
                yield return lWaitForEndOfFrame;
                CombatManager.EndOfFrameUpdate();
            }
        }
    }
}

