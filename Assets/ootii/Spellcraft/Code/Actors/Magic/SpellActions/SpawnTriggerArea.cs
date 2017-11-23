using System;
using System.Collections.Generic;
using UnityEngine;
using com.ootii.Actors.LifeCores;
using com.ootii.Base;
using com.ootii.Graphics.NodeGraph;
using com.ootii.Helpers;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace com.ootii.Actors.Magic
{
    [Serializable]
    [BaseName("Spawn Trigger Area")]
    [BaseDescription("Spawns a trigger area from the specified prefab. Links are traversed when used with the link action.")]
    public class SpawnTriggerArea : SpawnGameObject
    {
        /// <summary>
        /// Determines the size of the area
        /// </summary>
        public Vector3 _Size = Vector3.one;
        public Vector3 Size
        {
            get { return _Size; }
            set { _Size = value; }
        }

        /// <summary>
        /// Determines if we use the camera forward as the direction of the projectile's launch
        /// </summary>
        public bool _ReleaseFromCameraForward = false;
        public bool ReleaseFromCameraForward
        {
            get { return _ReleaseFromCameraForward; }
            set { _ReleaseFromCameraForward = value; }
        }

        /// <summary>
        /// Name of the link activates when someone enters
        /// </summary>
        public string _OnTriggerEnterTag = "OnEnter";
        public string OnTriggerEnterTag
        {
            get { return _OnTriggerEnterTag; }
            set { _OnTriggerEnterTag = value; }
        }

        /// <summary>
        /// Name of the link activates when someone stays
        /// </summary>
        public string _OnTriggerStayTag = "OnStay";
        public string OnTriggerStayTag
        {
            get { return _OnTriggerStayTag; }
            set { _OnTriggerStayTag = value; }
        }

        /// <summary>
        /// Name of the link activates when someone exits
        /// </summary>
        public string _OnTriggerExitTag = "OnExit";
        public string OnTriggerExitTag
        {
            get { return _OnTriggerExitTag; }
            set { _OnTriggerExitTag = value; }
        }

        /// <summary>
        /// Area core that was spawned
        /// </summary>
        protected List<TriggerAreaCore> mTriggerAreaCores = null;

        /// <summary>
        /// Called when the action is first activated
        /// <param name="rPreviousSpellActionState">State of the action prior to this one activating</param>
        public override void Activate(int rPreviousSpellActionState = -1, object rData = null)
        {
            base.Activate(rPreviousSpellActionState, rData);

            if (mInstances != null && mInstances.Count > 0)
            {
                if (_Spell.ShowDebug)
                {
                    for (int i = 0; i < mInstances.Count; i++)
                    {
                        mInstances[i].hideFlags = HideFlags.None;
                    }
                }

                if (mTriggerAreaCores == null) { mTriggerAreaCores = new List<TriggerAreaCore>(); }

                for (int i = 0; i < mInstances.Count; i++)
                {
                    //// Ensure we have a position
                    //Vector3 lPosition = Vector3.zero;
                    //if (_Spell.Data.Positions != null && _Spell.Data.Positions.Count > 0)
                    //{
                    //    lPosition = _Spell.Data.Positions[0];
                    //}

                    //if (lPosition.sqrMagnitude == 0)
                    //{
                    //    OnFailure();
                    //    return;
                    //}

                    Vector3 lForward = _Spell.Owner.transform.forward;
                    //if (_Spell.Data.Forwards != null && _Spell.Data.Forwards.Count > 0)
                    //{
                    //    lForward = _Spell.Data.Forwards[0];
                    //}

                    // Set the basics
                    //mInstances[i].transform.parent = null;
                    mInstances[i].transform.localScale = Size;
                    //mInstance.transform.position = lPosition;
                    mInstances[i].transform.rotation = Quaternion.LookRotation(lForward, _Spell.Owner.transform.up);

                    // Now we want to define the area
                    TriggerAreaCore lTriggerAreaCore = mInstances[i].GetComponent<TriggerAreaCore>();
                    if (lTriggerAreaCore != null)
                    {
                        lTriggerAreaCore.Age = 0f;

                        if (MaxAge > 0f)
                        {
                            lTriggerAreaCore.MaxAge = MaxAge;
                        }

                        lTriggerAreaCore.Prefab = _Prefab;
                        lTriggerAreaCore.OnReleasedEvent = OnCoreReleased;
                        lTriggerAreaCore.OnTriggerEnterEvent = OnAreaEnter;
                        lTriggerAreaCore.OnTriggerStayEvent = OnAreaStay;
                        lTriggerAreaCore.OnTriggerExitEvent = OnAreaExit;
                        lTriggerAreaCore.Play();

                        mTriggerAreaCores.Add(lTriggerAreaCore);
                    }

                    // Determine how we release the spell
                    if (_Spell.ReleaseFromCamera && Camera.main != null)
                    {
                        mInstances[i].transform.rotation = Camera.main.transform.rotation;
                    }
                    else if (ReleaseFromCameraForward && Camera.main != null)
                    {
                        mInstances[i].transform.rotation = Camera.main.transform.rotation;
                    }
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
            // Determine if we can just get out
            if (mInstances == null || mTriggerAreaCores == null)
            {
                base.Deactivate();
            }
            // If not, wait for the particles to end
            else if (!mIsShuttingDown)
            {
                mIsShuttingDown = true;
                State = EnumSpellActionState.SUCCEEDED;

                if (mTriggerAreaCores != null)
                {
                    for (int i = 0; i < mTriggerAreaCores.Count; i++)
                    {
                        mTriggerAreaCores[i].Stop();
                    }
                }
            }
        }

        /// <summary>
        /// Called each frame that the action is active
        /// </summary>
        /// <param name="rDeltaTime">Time in seconds since the last update</param>
        public override void Update()
        {
            mAge = mAge + Time.deltaTime;

            // Determine if it's time to shut down
            if (mState == EnumSpellActionState.ACTIVE)
            {
                if (TestDeactivate())
                {
                    Deactivate();
                }
            }
        }

        /// <summary>
        /// Called when the core has finished running and is released
        /// </summary>
        /// <param name="rCore"></param>
        private void OnCoreReleased(ILifeCore rCore, object rUserData = null)
        {
            TriggerAreaCore lTriggerAreaCore = rCore as TriggerAreaCore;
            if (lTriggerAreaCore != null && lTriggerAreaCore != null)
            {
                mTriggerAreaCores.Remove(lTriggerAreaCore);
            }

            if (mTriggerAreaCores == null || mTriggerAreaCores.Count == 0)
            {
                base.Deactivate();
            }

            base.Deactivate();
        }

        /// <summary>
        /// Raised when a collider enters the trigger area
        /// </summary>
        /// <param name="rArea">Area core that was entered</param>
        /// <param name="rCollider">Collider who entered</param>
        private void OnAreaEnter(ILifeCore rArea, object rCollider)
        {
            if (mIsShuttingDown) { return; }

            // Check if we should force a node link to activate
            if (mNode != null && _OnTriggerEnterTag.Length > 0)
            {
                for (int i = 0; i < mNode.Links.Count; i++)
                {
                    if (mNode.Links[i].TestActivate(_OnTriggerEnterTag))
                    {
                        _Spell.ActivateLink(mNode.Links[i], rCollider);
                    }
                }
            }
        }

        /// <summary>
        /// Raised when a collider stays the trigger area
        /// </summary>
        /// <param name="rArea">Area core that was entered</param>
        /// <param name="rCollider">Collider who entered</param>
        private void OnAreaStay(ILifeCore rArea, object rCollider)
        {
            if (mIsShuttingDown) { return; }

            // Check if we should force a node link to activate
            if (mNode != null && _OnTriggerStayTag.Length > 0)
            {
                for (int i = 0; i < mNode.Links.Count; i++)
                {
                    if (mNode.Links[i].TestActivate(_OnTriggerStayTag))
                    {
                        _Spell.ActivateLink(mNode.Links[i], rCollider);
                    }
                }
            }
        }

        /// <summary>
        /// Raised when a collider exits the trigger area
        /// </summary>
        /// <param name="rArea">Area core that was entered</param>
        /// <param name="rCollider">Collider who entered</param>
        private void OnAreaExit(ILifeCore rArea, object rCollider)
        {
            // Check if we should force a node link to activate
            if (mNode != null && _OnTriggerExitTag.Length > 0)
            {
                for (int i = 0; i < mNode.Links.Count; i++)
                {
                    if (mNode.Links[i].TestActivate(_OnTriggerExitTag))
                    {
                        _Spell.ActivateLink(mNode.Links[i], rCollider);
                    }
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
            mEditorShowParentFields = true;
            mEditorShowDeactivationField = true;
            bool lIsDirty = base.OnInspectorGUI(rTarget);

            GUILayout.Space(5f);

            if (EditorHelper.FloatField("Max Age", "Time in seconds the area will live for.", MaxAge, rTarget))
            {
                lIsDirty = true;
                MaxAge = EditorHelper.FieldFloatValue;
            }

            if (EditorHelper.Vector3Field("Scale (len, height, depth)", "Determines the size of the area. This is typically a 'radius' or half size.", Size, rTarget))
            {
                lIsDirty = true;
                Size = EditorHelper.FieldVector3Value;
            }

            if (EditorHelper.BoolField("Use Camera Forward", "Determines if we use the camera forward as the direction of the particles.", ReleaseFromCameraForward, rTarget))
            {
                lIsDirty = true;
                ReleaseFromCameraForward = EditorHelper.FieldBoolValue;
            }

            GUILayout.Space(5f);

            if (EditorHelper.TextField("Enter Tag", "Name of the link to activate when someone enters the area. (Empty means none)", OnTriggerEnterTag, rTarget))
            {
                lIsDirty = true;
                OnTriggerEnterTag = EditorHelper.FieldStringValue;
            }

            if (EditorHelper.TextField("Stay Tag", "Name of the link to activate when someone stays in the area. (Empty means none)", OnTriggerStayTag, rTarget))
            {
                lIsDirty = true;
                OnTriggerStayTag = EditorHelper.FieldStringValue;
            }

            if (EditorHelper.TextField("Exit Tag", "Name of the link to activate when someone leaves the area. (Empty means none)", OnTriggerExitTag, rTarget))
            {
                lIsDirty = true;
                OnTriggerExitTag = EditorHelper.FieldStringValue;
            }

            return lIsDirty;
        }

#endif

        #endregion
    }
}