using System;
using UnityEngine;
using com.ootii.Base;
using com.ootii.Collections;
using com.ootii.Helpers;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace com.ootii.Actors.Magic
{
    [Serializable]
    [BaseName("Play Audio Source")]
    [BaseDescription("Plays the audio clip on the specified audio source.")]
    public class PlayAudioSource : SpawnGameObject
    {
        /// <summary>
        /// Particle system associated with the effect
        /// </summary>
        protected AudioSource mAudioSource = null;

        /// <summary>
        /// Called when the action is first activated
        /// <param name="rPreviousSpellActionState">State of the action prior to this one activating</param>
        public override void Activate(int rPreviousSpellActionState = -1, object rData = null)
        {
            // Grab an instance from the pool
            base.Activate(rPreviousSpellActionState, rData);

            // Set the audio clip and play
            if (mInstances != null && mInstances.Count > 0)
            {
                mAudioSource = mInstances[0].GetComponent<AudioSource>();
                if (mAudioSource != null)
                {
                    mAudioSource.Play();
                }
            }
            else
            {
                Deactivate();
            }

            // Determine if we're returnning immediately
            if (_DeactivationIndex == EnumSpellActionDeactivation.IMMEDIATELY)
            {
                mIsShuttingDown = true;
                State = EnumSpellActionState.SUCCEEDED;
            }
        }

        /// <summary>
        /// Called when the action is meant to be deactivated
        /// </summary>
        public override void Deactivate()
        {
            if (mInstances == null || mAudioSource == null)
            {
                base.Deactivate();
            }
            else
            {
                mIsShuttingDown = true;
                if (mState == EnumSpellActionState.ACTIVE) { State = EnumSpellActionState.SUCCEEDED; }

                mAudioSource.Stop();
            }
        }

        /// <summary>
        /// Called each frame that the action is active
        /// </summary>
        /// <param name="rDeltaTime">Time in seconds since the last update</param>
        public override void Update()
        {
            if (mIsShuttingDown || !mAudioSource.loop)
            {
                bool lIsAlive = false;

                if (mAudioSource != null && mAudioSource.isPlaying)
                {
                    lIsAlive = true;
                }

                if (!lIsAlive)
                {
                    mAudioSource = null;
                    base.Deactivate();
                }
            }
        }

        #region Editor Functions

#if UNITY_EDITOR

        /// <summary>
        /// Called when the inspector needs to draw
        /// </summary>
        public override bool OnInspectorGUI(UnityEngine.Object rTarget)
        {
            mEditorShowDeactivationField = true;
            bool lIsDirty = base.OnInspectorGUI(rTarget);

            return lIsDirty;
        }

#endif

        #endregion
    }
}