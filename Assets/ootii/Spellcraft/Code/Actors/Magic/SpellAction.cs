using System;
using UnityEngine;
using com.ootii.Geometry;
using com.ootii.Graphics.NodeGraph;
using com.ootii.Helpers;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace com.ootii.Actors.Magic
{
    /// <summary>
    /// Delegate for calling back to the spell action
    /// </summary>
    public delegate void SpellActionEvent();

    /// <summary>
    /// A SpellAction is the effect that the spell has. For example, a spell could
    /// cause damage, heal, cause invisiblity, etc.
    /// 
    /// Spells can have more tha one action.
    /// </summary>
    [Serializable]
    public abstract class SpellAction : NodeContent
    {
        // Position types that determine how we'll position the GameObject at creation
        public static string[] GetBestTargetTypes = new string[] { "Owner", "Custom Data", "SpellData Targets", "SpellData Prev Targets" };

        // Position types that determine how we'll position the GameObject at creation
        public static string[] GetBestPositionTypes = new string[] { "Fixed Position", "Owner Transform", "Custom Data", "SpellData Position", "SpellData Target", "SpellData Prev Target" };

        /// <summary>
        /// Spell the action belongs to
        /// </summary>
        [NonSerialized]
        public Spell _Spell = null;
        public Spell Spell
        {
            get { return _Spell; }
            set { _Spell = value; }
        }

        /// <summary>
        /// Determines if the content is processed immediately. In this case,
        /// the flow is also immediate and no Update() is used.
        /// </summary>
        public override bool IsImmediate
        {
            get { return (_DeactivationIndex == EnumSpellActionDeactivation.IMMEDIATELY); }
        }

        /// <summary>
        /// Description of the action
        /// </summary>
        public string _Description = "";
        public virtual string Description
        {
            get { return _Description; }
            set { _Description = value; }
        }

        /// <summary>
        /// Determines if the action can run
        /// </summary>
        public bool _IsEnabled = true;
        public bool IsEnabled
        {
            get { return _IsEnabled; }
            set { _IsEnabled = value; }
        }

        /// <summary>
        /// Determines if the action is done, but still needs to run to 
        /// shut down effects.
        /// </summary>
        public bool mIsShuttingDown = false;
        public bool IsShuttingDown
        {
            get { return mIsShuttingDown; }
        }

        /// <summary>
        /// Actual state of the action
        /// </summary>
        protected int mState = EnumSpellActionState.READY;
        public int State
        {
            get { return mState; }

            set
            {
                if (value != mState)
                {
                    if (value == EnumSpellActionState.READY)
                    {
                        mIsShuttingDown = false;
                    }
                }

                mState = value;

                // Update the node state based on this content
                if (mNode != null)
                {
                    if (mState == EnumSpellActionState.READY || mState == EnumSpellActionState.INACTIVE)
                    {
                        mNode.State = EnumNodeState.IDLE;
                    }
                    else if (mState == EnumSpellActionState.ACTIVE)
                    {
                        mNode.State = EnumNodeState.WORKING;
                    }
                    else if (mState == EnumSpellActionState.SUCCEEDED)
                    {
                        mNode.State = EnumNodeState.SUCCEEDED;
                    }
                    else if (mState == EnumSpellActionState.FAILED)
                    {
                        mNode.State = EnumNodeState.FAILED;
                    }
                }
            }
        }

        /// <summary>
        /// Determines when the action will be deactivated
        /// </summary>
        public int _DeactivationIndex = EnumSpellActionDeactivation.IMMEDIATELY;
        public int DeactivationIndex
        {
            get { return _DeactivationIndex; }
            set { _DeactivationIndex = value; }
        }

        /// <summary>
        /// Maximum age at which the action ends
        /// </summary>
        public float _MaxAge = 2f;
        public virtual float MaxAge
        {
            get { return _MaxAge; }
            set { _MaxAge = value; }
        }

        /// <summary>
        /// Age the action has been active for
        /// </summary>
        protected float mAge = 0f;
        public virtual float Age
        {
            get { return mAge; }
            set { mAge = value; }
        }

        /// <summary>
        /// Default constructor
        /// </summary>
        public SpellAction()
        {
        }

        /// <summary>
        /// Used to initialize any actions prior to them being activated
        /// </summary>
        public virtual void Awake()
        {
            Clear();
        }

        /// <summary>
        /// Called when we're done with the action and we need to reset it
        /// </summary>
        public virtual void Clear()
        {
            State = EnumSpellActionState.READY;
        }

        /// <summary>
        /// Called when the action is first activated
        /// </summary>
        /// <param name="rPreviousSpellActionState">State of the action prior to this one activating</param>
        public virtual void Activate(int rPreviousSpellActionState = -1, object rData = null)
        {
            mAge = 0f;
            mIsShuttingDown = false;
            State = EnumSpellActionState.ACTIVE;

            mNode.Data = (rData != _Spell.Data ? rData : null);

            // Determine if we're returnning immediately
            if (_DeactivationIndex == EnumSpellActionDeactivation.IMMEDIATELY)
            {
                State = EnumSpellActionState.SUCCEEDED;
            }
        }

        /// <summary>
        /// Called when the action is meant to be deactivated
        /// </summary>
        public virtual void Deactivate()
        {
            mIsShuttingDown = false;

            if (mState == EnumSpellActionState.READY || 
                mState == EnumSpellActionState.ACTIVE)
            {
                State = EnumSpellActionState.SUCCEEDED;
            }
        }

        /// <summary>
        /// Determines if the spell should deactivate
        /// </summary>
        public virtual bool TestDeactivate()
        {
            if (mIsShuttingDown) { return true; }

            if (DeactivationIndex == EnumSpellActionDeactivation.IMMEDIATELY)
            {
                return true;
            }
            else if (DeactivationIndex == EnumSpellActionDeactivation.CASTING_STARTED)
            {
                if (_Spell.State == EnumSpellState.CASTING_STARTED) { return true; }
            }
            else if (DeactivationIndex == EnumSpellActionDeactivation.SPELL_CAST)
            {
                if (_Spell.State == EnumSpellState.SPELL_CAST) { return true; }
            }
            else if (DeactivationIndex == EnumSpellActionDeactivation.CASTING_ENDED)
            {
                if (_Spell.State == EnumSpellState.CASTING_ENDED) { return true; }
            }
            else if (DeactivationIndex == EnumSpellActionDeactivation.TIMER)
            {
                if (mAge >= _MaxAge) { return true; }
            }

            return false;
        }

        /// <summary>
        /// Called each frame that the action is active
        /// </summary>
        public virtual void Update()
        {
            mAge = mAge + Time.deltaTime;

            if (!mIsShuttingDown && TestDeactivate())
            {
                Deactivate();
            }
        }

        /// <summary>
        /// Utility function used to get the target given some different options
        /// </summary>
        /// <param name="rData">Data that typically comes from activation</param>
        /// <param name="rSpellData">SpellData belonging to the spell</param>
        /// <returns>GameObject that is the expected target or null</returns>
        public virtual GameObject GetBestTarget(int rTargetType, object rData, SpellData rSpellData)
        {
            GameObject lTarget = null;

            // Owner
            if (rTargetType == 0)
            {
                lTarget = _Spell.Owner;
            }
            // Explicit data
            else if (rTargetType == 1 && rData != null && rData != rSpellData)
            {
                mNode.Data = rData;

                if (rData is Collider)
                {
                    lTarget = ((Collider)rData).gameObject;
                }
                else if (rData is Transform)
                {
                    lTarget = ((Transform)rData).gameObject;
                }
                else if (rData is GameObject)
                {
                    lTarget = (GameObject)rData;
                }
                else if (rData is MonoBehaviour)
                {
                    lTarget = ((MonoBehaviour)rData).gameObject;
                }
            }
            // Spell data
            else if (rSpellData != null)
            {
                // Targets
                if (rTargetType == 1 || rTargetType == 2)
                {
                    if (rSpellData != null && rSpellData.Targets != null)
                    {
                        if (rSpellData.Targets.Count > 0)
                        {
                            lTarget = rSpellData.Targets[0];
                        }
                    }
                }
                // Previous Targets
                else if (rTargetType == 3)
                {
                    if (rSpellData != null && rSpellData.PreviousTargets != null)
                    {
                        if (rSpellData.PreviousTargets.Count > 0)
                        {
                            lTarget = rSpellData.PreviousTargets[0];
                        }
                    }
                }
            }

            return lTarget;
        }

        /// <summary>
        /// Utility function used to get the target given some different options
        /// </summary>
        /// <param name="rType">Type of search we'll do</param>
        /// <param name="rOffset">Vector offset for position</param>
        /// <param name="rData">Data that typically comes from activation (overrides the SpellData)</param>
        /// <param name="rSpellData">SpellData belonging to the spell</param>
        /// <param name="rFunction">Function that will process the results</param>
        public virtual void GetBestTargets(int rTargetType, object rData, SpellData rSpellData, Func<GameObject, object, bool> rFunction)
        {
            // Owner transform
            if (rTargetType == 0)
            {
                rFunction(_Spell.Owner, rData);
            }
            // Data will override the SpellData
            else if (rTargetType == 1 && rData != null && rData != rSpellData)
            {
                if (rData is Collider)
                {
                    rFunction(((Collider)rData).gameObject, rData);
                }
                else if (rData is Transform)
                {
                    rFunction(((Transform)rData).gameObject, rData);
                }
                else if (rData is GameObject)
                {
                    rFunction((GameObject)rData, rData);
                }
                else if (rData is MonoBehaviour)
                {
                    rFunction(((MonoBehaviour)rData).gameObject, rData);
                }
            }
            // Spell data
            else if (rSpellData != null)
            {
                // Targets
                if (rTargetType == 1 || rTargetType == 2)
                {
                    if (rSpellData != null && rSpellData.Targets != null && rSpellData.Targets.Count > 0)
                    {
                        for (int i = 0; i < rSpellData.Targets.Count; i++)
                        {
                            bool lContinue = rFunction(rSpellData.Targets[i], rData);
                            if (!lContinue) { break; }
                        }
                    }
                    // Previous target
                    else if (rTargetType == 3)
                    {
                        if (rSpellData != null && rSpellData.PreviousTargets != null && rSpellData.PreviousTargets.Count > 0)
                        {
                            for (int i = 0; i < rSpellData.PreviousTargets.Count; i++)
                            {
                                bool lContinue = rFunction(rSpellData.PreviousTargets[i], rData);
                                if (!lContinue) { break; }
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Utility function used to get the target given some different options
        /// </summary>
        /// <param name="rType">Type of search we'll do</param>
        /// <param name="rData">Data that typically comes from activation (overrides the SpellData)</param>
        /// <param name="rSpellData">SpellData belonging to the spell</param>
        /// <param name="rOffset">Vector offset for position</param>
        /// <param name="rTarget">Resulting target transform</param>
        /// <param name="rTargetPosition">Resulting target position</param>
        public virtual void GetBestPosition(int rPositionType, object rData, SpellData rSpellData, Vector3 rOffset, out Transform rTarget, out Vector3 rTargetPosition)
        {
            rTarget = null;
            rTargetPosition = Vector3Ext.Null;

            // Fixed position
            if (rPositionType == 0)
            {
                rTargetPosition = rOffset;
            }
            // Owner transform
            else if (rPositionType == 1)
            {
                rTarget = _Spell.Owner.transform;
            }
            // Data will override the SpellData
            else if (rData != null && rData != rSpellData)
            {
                if (rData is Vector3)
                {
                    rTargetPosition = (Vector3)rData + rOffset;
                }
                else if (rData is Collider)
                {
                    rTarget = ((Collider)rData).transform;
                }
                else if (rData is Transform)
                {
                    rTarget = ((Transform)rData);
                }
                else if (rData is GameObject)
                {
                    rTarget = ((GameObject)rData).transform;
                }
                else if (rData is MonoBehaviour)
                {
                    rTarget = ((MonoBehaviour)rData).gameObject.transform;
                }
            }
            // Spell Position
            else if (rPositionType == 3)
            {
                if (rSpellData != null)
                {
                    if (rSpellData.Positions != null && rSpellData.Positions.Count > 0)
                    {
                        rTargetPosition = rSpellData.Positions[rSpellData.Positions.Count - 1] + rOffset;
                    }
                }
            }
            // Spell Target
            else if (rPositionType == 4)
            {
                if (rSpellData != null)
                {
                    if (rSpellData.Targets != null && rSpellData.Targets.Count > 0)
                    {
                        rTarget = rSpellData.Targets[rSpellData.Targets.Count - 1].transform;
                    }
                }
            }
            // Previous target
            else if (rPositionType == 5)
            {
                if (rSpellData != null)
                {
                    if (rSpellData.PreviousTargets != null && rSpellData.PreviousTargets.Count > 0)
                    {
                        rTarget = rSpellData.PreviousTargets[rSpellData.PreviousTargets.Count - 1].transform;
                    }
                }
            }

            // If we have a transform, grab the position from it
            if (rTarget != null)
            {
                rTargetPosition = rTarget.position + (rTarget.rotation * rOffset);
            }
        }

        /// <summary>
        /// Typcially called by external objects to report success 
        /// </summary>
        public virtual void OnSuccess()
        {
            State = EnumSpellActionState.SUCCEEDED;
            Deactivate();
        }

        /// <summary>
        /// Typcially called by external objects to report failure 
        /// </summary>
        public virtual void OnFailure()
        {
            State = EnumSpellActionState.FAILED;
            Deactivate();
        }

        #region Editor Functions

#if UNITY_EDITOR

        /// <summary>
        /// Determine if we show the deactivation field
        /// </summary>
        protected bool mEditorShowDeactivationField = false;

        /// <summary>
        /// Called before the action is removed. Allows us to clear any variables
        /// </summary>
        public virtual void Clear(UnityEngine.Object rTarget)
        {
        }

        /// <summary>
        /// Called when the inspector needs to draw
        /// </summary>
        public virtual bool OnInspectorGUI(UnityEngine.Object rTarget)
        {
            bool lIsDirty = false;

            if (EditorHelper.TextField("Name", "Name of the spell action", Name, rTarget))
            {
                lIsDirty = true;
                Name = EditorHelper.FieldStringValue;
            }

            //if (EditorHelper.TextField("Description", "Description of the spell action", Description, rTarget))
            //{
            //    lIsDirty = true;
            //    Description = EditorHelper.FieldStringValue;
            //}

            if (mEditorShowDeactivationField)
            {
                NodeEditorStyle.DrawLine(NodeEditorStyle.LineBlue);

                if (EditorHelper.PopUpField("Deactivation", "", DeactivationIndex, EnumSpellActionDeactivation.Names, rTarget))
                {
                    lIsDirty = true;
                    DeactivationIndex = EditorHelper.FieldIntValue;
                }

                if (DeactivationIndex == EnumSpellActionDeactivation.TIMER)
                {
                    if (EditorHelper.FloatField("Max Age", "Max age the action can live for.", MaxAge, rTarget))
                    {
                        lIsDirty = true;
                        MaxAge = EditorHelper.FieldFloatValue;
                    }
                }
            }

            return lIsDirty;
        }

#endif

        #endregion
    }
}
