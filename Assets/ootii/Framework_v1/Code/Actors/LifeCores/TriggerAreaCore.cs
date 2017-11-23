using System;
using System.Collections.Generic;
using UnityEngine;
using com.ootii.Geometry;

namespace com.ootii.Actors.LifeCores
{
    /// <summary>
    /// A life cycle is the heart-beat of the area. This code is used to manage
    /// the area and objects that enter it
    /// </summary>
    public class TriggerAreaCore : ParticleCore
    {
        /// <summary>
        /// Event for when a collider enters the trigger area
        /// </summary>
        [NonSerialized]
        public LifeCoreDelegate OnTriggerEnterEvent = null;

        /// <summary>
        /// Event for when a collider stays in the trigger area
        /// </summary>
        [NonSerialized]
        public LifeCoreDelegate OnTriggerStayEvent = null;

        /// <summary>
        /// Event for when a collider exits the trigger area
        /// </summary>
        [NonSerialized]
        public LifeCoreDelegate OnTriggerExitEvent = null;

        // Collider that is the trigger
        protected Collider mCollider = null;

        // Colliders that are intruding on the trigger
        protected List<GameObject> mIntruders = new List<GameObject>();

        // Colliders that are intruding on the trigger
        protected List<Collider> mIntruderColliders = new List<Collider>();

        /// <summary>
        /// Awake is called when the script instance is being loaded.
        /// </summary>
        public override void Awake()
        {
            base.Awake();

            // Grab the trigger area
            mCollider = gameObject.GetComponent<Collider>();
            if (mCollider == null)
            {
                mCollider = gameObject.GetComponentInChildren<Collider>();
            }
        }

        /// <summary>
        /// Used to start the core all over
        /// </summary>
        public override void Play()
        {
            mIntruders.Clear();
            mIntruderColliders.Clear();

            ColliderProxy[] lProxies = gameObject.GetComponentsInChildren<ColliderProxy>();
            for (int i = 0; i < lProxies.Length; i++)
            {
                lProxies[i].Reset();
            }

            base.Play();
        }

        /// <summary>
        /// Releases the game object back to the pool (if allocated) or simply destroys if it not.
        /// </summary>
        public override void Release()
        {
            // Ensure we exit any remaining intruders
            for (int i = 0; i < mIntruders.Count; i++)
            {
                //Utilities.Debug.Log.FileWrite("TriggerAreaCore.Release collider:" + mIntruders[i].name);
                if (OnTriggerExitEvent != null) { OnTriggerExitEvent(this, mIntruders[i]); }
            }

            // Clean up
            mIntruders.Clear();
            mIntruderColliders.Clear();

            OnTriggerEnterEvent = null;
            OnTriggerStayEvent = null;
            OnTriggerExitEvent = null;

            base.Release();
        }

        /// <summary>
        /// Capture Unity's collision event. We use triggers since IsKinematic Rigidbodies don't
        /// raise collisions... only triggers.
        /// </summary>
        protected virtual void OnTriggerEnter(Collider rCollider)
        {
            mIntruderColliders.Add(rCollider);

            if (!mIntruders.Contains(rCollider.gameObject))
            {
                mIntruders.Add(rCollider.gameObject);

                //Utilities.Debug.Log.FileWrite("TriggerAreaCore.OnTriggerEnter collider:" + rCollider.name);
                if (OnTriggerEnterEvent != null) { OnTriggerEnterEvent(this, rCollider); }
            }
        }

        /// <summary>
        /// Capture Unity's collision event
        /// </summary>
        protected virtual void OnTriggerStay(Collider rCollider)
        {
            if (mIntruders.Contains(rCollider.gameObject))
            {
                //Utilities.Debug.Log.FileWrite("TriggerAreaCore.OnTriggerStay collider:" + rCollider.name);
                if (OnTriggerStayEvent != null) { OnTriggerStayEvent(this, rCollider); }
            }
        }

        /// <summary>
        /// Capture Unity's collision event. 
        /// The problem with OnTriggerExit is that it only works if the collider is enabled.
        /// So, if we disable the collider (trigger area), no exit ever fires. We overcome this
        /// with logic in the OnTriggerStay
        /// </summary>
        protected virtual void OnTriggerExit(Collider rCollider)
        {
            mIntruderColliders.Remove(rCollider);

            // Ensure the object wasn't already destroyed
            if (ReferenceEquals(rCollider, null)) { return; }
            if (ReferenceEquals(rCollider.gameObject, null)) { return; }

            // We don't want to remove the GameObject if another collider
            // is keeping it active.
            for (int i = mIntruderColliders.Count - 1; i >= 0; i--)
            {
                try
                {
                    if (mIntruderColliders[i].gameObject == rCollider.gameObject)
                    {
                        return;
                    }
                }
                catch
                {
                    mIntruderColliders.RemoveAt(i);
                    continue;
                }
            }

            // Since no other collider references the game object, we can remove it
            if (mIntruders.Contains(rCollider.gameObject))
            {
                mIntruders.Remove(rCollider.gameObject);

                //Utilities.Debug.Log.FileWrite("TriggerAreaCore.OnTriggerExit collider:" + rCollider.name);
                if (OnTriggerExitEvent != null) { OnTriggerExitEvent(this, rCollider); }
            }
        }
    }
}
