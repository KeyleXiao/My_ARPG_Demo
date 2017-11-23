using System;
using UnityEngine;
using com.ootii.Actors.AnimationControllers;
using com.ootii.Base;
using com.ootii.Graphics.NodeGraph;
using com.ootii.Helpers;

namespace com.ootii.Actors.Magic
{
    [Serializable]
    [BaseName("Activate Motion")]
    [BaseDescription("Activates a motion on the target based on the motion's name or class name.")]
    public class ActivateMotion : SpellAction
    {
        // Determines how we'll deactivate
        private static string[] CompletionList = new string[] { "Immediate", "Activated", "Deactivated", "Motion Time", "State Time" };

        /// <summary>
        /// Determines who we'll activate the motions on
        /// </summary>
        public int _TargetTypeIndex = 0;
        public int TargetTypeIndex
        {
            get { return _TargetTypeIndex; }
            set { _TargetTypeIndex = value; }
        }

        /// <summary>
        /// Determines when the action will be considered completed
        /// </summary>
        public int _CompletionIndex = 2;
        public int CompletionIndex
        {
            get { return _CompletionIndex; }
            set { _CompletionIndex = value; }
        }

        /// <summary>
        /// State we'll look for to exit on
        /// </summary>
        public string _ExitState = "";
        public string ExitState
        {
            get { return _ExitState; }
            set { _ExitState = value; }
        }

        /// <summary>
        /// Time in the state that we'll look to exit on
        /// </summary>
        public float _ExitTime = 0f;
        public float ExitTime
        {
            get { return _ExitTime; }
            set { _ExitTime = value; }
        }

        /// <summary>
        /// Name of the motion to activate
        /// </summary>
        public string _MotionName = "";
        public string MotionName
        {
            get { return _MotionName; }
            set { _MotionName = value; }
        }

        // Motion instance being activated
        protected MotionControllerMotion mMotion = null;

        // Internal ID of the state we'll exit on
        protected int mExitStateID = 0;

        // Tracks if our motion has been activated yet
        protected bool mIsActive = false;

        /// <summary>
        /// Used to initialize any actions prior to them being activated
        /// </summary>
        public override void Awake()
        {
            base.Awake();

            _DeactivationIndex = EnumSpellActionDeactivation.MANAGED;
        }

        /// <summary>
        /// Called when the action is first activated
        /// <param name="rPreviousSpellActionState">State of the action prior to this one activating</param>
        public override void Activate(int rPreviousSpellActionState = -1, object rData = null)
        {
            base.Activate(rPreviousSpellActionState, rData);

            bool lIsActivated = false;

            // Owner
            if (TargetTypeIndex == 0)
            {
                lIsActivated = ActivateInstance(_Spell.Owner);
            }
            // Explicit data
            else if (TargetTypeIndex == 1 && rData != null && rData != _Spell.Data)
            {
                mNode.Data = rData;

                if (rData is Collider)
                {
                    lIsActivated = ActivateInstance(((Collider)rData).gameObject);
                }
                else if (rData is Transform)
                {
                    lIsActivated = ActivateInstance(((Transform)rData).gameObject);
                }
                else if (rData is GameObject)
                {
                    lIsActivated = ActivateInstance((GameObject)rData);
                }
                else if (rData is MonoBehaviour)
                {
                    lIsActivated = ActivateInstance(((MonoBehaviour)rData).gameObject);
                }
            }
            // Spell data
            else if (_Spell.Data != null)
            {
                // Targets
                if (TargetTypeIndex == 2)
                {
                    if (_Spell.Data != null && _Spell.Data.Targets != null)
                    {
                        for (int i = 0; i < _Spell.Data.Targets.Count; i++)
                        {
                            bool lIsTargetActivated = ActivateInstance(_Spell.Data.Targets[i]);
                            if (lIsTargetActivated) { lIsActivated = true; }
                        }
                    }
                }
                // Previous Targets
                else if (TargetTypeIndex == 3)
                {
                    if (_Spell.Data != null && _Spell.Data.PreviousTargets != null)
                    {
                        for (int i = 0; i < _Spell.Data.PreviousTargets.Count; i++)
                        {
                            bool lIsTargetActivated = ActivateInstance(_Spell.Data.PreviousTargets[i]);
                            if (lIsTargetActivated) { lIsActivated = true; }
                        }
                    }
                }
            }

            // If there were no particles, stop
            if (!lIsActivated || !(TargetTypeIndex == 0 || TargetTypeIndex == 1))
            {
                Deactivate();
            }
        }

        /// <summary>
        /// Activates the motion on the specified target
        /// </summary>
        /// <param name="rTarget">GameObject to activate the motion on</param>
        /// <returns>Bool that determines if the motion was activated</returns>
        public bool ActivateInstance(GameObject rTarget)
        {
            bool lIsActivated = false;

            // Check if we have a motion controller and activate the motion
            MotionController lMotionController = rTarget.GetComponent<MotionController>();
            if (lMotionController != null)
            {
                mMotion = lMotionController.ActivateMotion(MotionName);
                if (mMotion != null)
                {
                    lIsActivated = true;

                    // Clear out any event first, then add our event
                    if (TargetTypeIndex == 0 || TargetTypeIndex == 1)
                    {
                        mMotion.OnDeactivatedEvent -= OnMotionDeactivated;
                        mMotion.OnDeactivatedEvent += OnMotionDeactivated;
                    }
                }
                // Grab the state as needed
                if (ExitState.Length > 0)
                {
                    mExitStateID = lMotionController.AddAnimatorName(ExitState);
                }
            }

            return lIsActivated;
        }

        /// <summary>
        /// Runs each frame to see if the action should continue
        /// </summary>
        public override void Update()
        {
            // Determine if we're active yet. This allows us to test if the
            // motion has even started
            if (!mIsActive && mMotion.IsActive)
            {
                mIsActive = true;
            }

            // If the motion is done, flag the action as done
            if (mMotion == null)
            {
                Deactivate();
            }
            // If we're not waiting or we've completed, call it doen
            else if (CompletionIndex == 0 || (mIsActive && !mMotion.IsActive))
            {
                Deactivate();
            }
            // Exit on activate
            else if (CompletionIndex == 1)
            {
                if (mIsActive && mMotion.IsActive)
                {
                    Deactivate();
                }
            }
            // Exit on deactivate
            else if (CompletionIndex == 2)
            {
                if (mIsActive && !mMotion.IsActive)
                {
                    Deactivate();
                }
            }
            // Exit on age
            else if (CompletionIndex == 3)
            {
                if (mIsActive && mMotion.Age > ExitTime)
                {
                    Deactivate();
                }
            }
            // Exit on state
            else if (CompletionIndex == 4)
            {
                if (mIsActive && (mExitStateID == 0 || mMotion.MotionLayer._AnimatorStateID == mExitStateID))
                {
                    if (mMotion.MotionLayer._AnimatorStateNormalizedTime > ExitTime)
                    {
                        Deactivate();
                    }
                }
            }
        }

        /// <summary>
        /// Raised when the motion has deactivated
        /// </summary>
        /// <param name="rLayer">Layer the motion is on</param>
        /// <param name="rMotion">Motion being deactivated</param>
        public void OnMotionDeactivated(int rLayer, MotionControllerMotion rMotion)
        {
            if (rMotion == null) { return; }
            if (rMotion != mMotion) { return; }

            rMotion.OnDeactivatedEvent -= OnMotionDeactivated;

            if (mState == EnumSpellActionState.ACTIVE)
            {
                Deactivate();
            }
        }

        #region Editor Functions

#if UNITY_EDITOR

        /// <summary>
        /// Called when the inspector needs to draw
        /// </summary>
        public override bool OnInspectorGUI(UnityEngine.Object rTarget)
        {
            mEditorShowDeactivationField = false;
            bool lIsDirty = base.OnInspectorGUI(rTarget);

            if (TargetTypeIndex == 0 || TargetTypeIndex == 1)
            {
                NodeEditorStyle.DrawLine(NodeEditorStyle.LineBlue);

                if (EditorHelper.PopUpField("Deactivation", "", CompletionIndex, CompletionList, rTarget))
                {
                    lIsDirty = true;
                    CompletionIndex = EditorHelper.FieldIntValue;
                }

                if (CompletionIndex == 3)
                {
                    if (EditorHelper.FloatField("Exit Time", "Elapsed time (in seconds) that the motion has run for.", ExitTime, rTarget))
                    {
                        lIsDirty = true;
                        ExitTime = EditorHelper.FieldFloatValue;
                    }
                }
                else if (CompletionIndex == 4)
                {
                    if (EditorHelper.TextField("Exit State", "Full path to the state we'll exit on.", ExitState, rTarget))
                    {
                        lIsDirty = true;
                        ExitState = EditorHelper.FieldStringValue;
                    }

                    if (EditorHelper.FloatField("Exit Time", "Normalized time (0 to 1) of the state specified.", ExitTime, rTarget))
                    {
                        lIsDirty = true;
                        ExitTime = Mathf.Clamp01(EditorHelper.FieldFloatValue);
                    }
                }
            }

            NodeEditorStyle.DrawLine(NodeEditorStyle.LineBlue);

            if (EditorHelper.PopUpField("Target Type", "Determines the target(s) we'll activate the motions on.", TargetTypeIndex, ActivateMotion.GetBestTargetTypes, rTarget))
            {
                lIsDirty = true;
                TargetTypeIndex = EditorHelper.FieldIntValue;
            }

            if (EditorHelper.TextField("Motion", "Name of the motion to activate.", MotionName, rTarget))
            {
                lIsDirty = true;
                MotionName = EditorHelper.FieldStringValue;
            }

            return lIsDirty;
        }

#endif

        #endregion
    }
}