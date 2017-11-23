using System;
using UnityEngine;
using com.ootii.Base;
using com.ootii.Collections;
using com.ootii.Graphics.NodeGraph;
using com.ootii.Helpers;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace com.ootii.Actors.Magic
{
    [Serializable]
    [BaseName("Play Audio Clip")]
    [BaseDescription("Plays the specified audio clip.")]
    public class PlayAudioClip : SpawnGameObject
    {
        /// <summary>
        /// Used as the GO prefab for this action. The GO includes an AudioSource
        /// </summary>
        protected static GameObject AudioSourcePrefab = null;

        /// <summary>
        /// Audio clip that we'll play with the source
        /// </summary>
        public AudioClip _AudioClip = null;
        public AudioClip AudioClip
        {
            get { return _AudioClip; }
            set { _AudioClip = value; }
        }

        /// <summary>
        /// AudioSource associated with the action
        /// </summary>
        protected AudioSource mAudioSource = null;

        /// <summary>
        /// Used to initialize any actions prior to them being activated
        /// </summary>
        public override void Awake()
        {
            // Only run if we're actually playing
            if (Application.isPlaying)
            {
                // Ensure our prefab is instantiated and create some instances in the pool
                if (PlayAudioClip.AudioSourcePrefab == null)
                {
                    PlayAudioClip.AudioSourcePrefab = new GameObject("AudioSourcePrefab ", typeof(AudioSource));
                    PlayAudioClip.AudioSourcePrefab.hideFlags = HideFlags.HideInHierarchy;

                    Prefab = PlayAudioClip.AudioSourcePrefab;
                }
            }

            // Create the pool of prefabs
            base.Awake();
        }

        /// <summary>
        /// Called when the action is first activated
        /// <param name="rPreviousSpellActionState">State of the action prior to this one activating</param>
        public override void Activate(int rPreviousSpellActionState = -1, object rData = null)
        {
            // Set the prefab from our original source
            Prefab = PlayAudioClip.AudioSourcePrefab;

            // Grab an instance from the pool
            base.Activate(rPreviousSpellActionState, rData);

            // Set the audio clip and play
            if (mInstances != null && _AudioClip != null)
            {
                mAudioSource = mInstances[0].GetComponent<AudioSource>();
                if (mAudioSource != null)
                {
                    mAudioSource.clip = _AudioClip;
                    mAudioSource.Play();
                }
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
            if (mInstances == null || mAudioSource == null || _AudioClip == null)
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
            mEditorShowPrefabField = false;
            mEditorShowDeactivationField = true;
            bool lIsDirty = base.OnInspectorGUI(rTarget);

            NodeEditorStyle.DrawLine(NodeEditorStyle.LineBlue);

            if (EditorHelper.ObjectField<AudioClip>("Audio Clip", "", AudioClip, rTarget))
            {
                lIsDirty = true;
                AudioClip = EditorHelper.FieldObjectValue as AudioClip;
            }

            return lIsDirty;
        }

#endif

        #endregion
    }
}