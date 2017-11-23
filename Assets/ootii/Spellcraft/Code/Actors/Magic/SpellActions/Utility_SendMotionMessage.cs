﻿using System;
using UnityEngine;
using com.ootii.Actors.AnimationControllers;
using com.ootii.Base;
using com.ootii.Graphics.NodeGraph;
using com.ootii.Helpers;

namespace com.ootii.Actors.Magic
{
    [Serializable]
    [BaseName("Utility - Send Motion Message")]
    [BaseDescription("Sends a motion message to the motion controller. Typically to control a motion.")]
    public class Utility_SendMotionMessage : SpellAction
    {
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
        /// Message ID to be sent
        /// </summary>
        public int _MessageID = MotionMessage.MSG_UNKNOWN;
        public int MessageID
        {
            get { return _MessageID; }
            set { _MessageID = value; }
        }

        /// <summary>
        /// Used to initialize any actions prior to them being activated
        /// </summary>
        public override void Awake()
        {
            base.Awake();

            _DeactivationIndex = EnumSpellActionDeactivation.IMMEDIATELY;
        }

        /// <summary>
        /// Called when the action is first activated
        /// <param name="rPreviousSpellActionState">State of the action prior to this one activating</param>
        public override void Activate(int rPreviousSpellActionState = -1, object rData = null)
        {
            base.Activate(rPreviousSpellActionState, rData);

            // Owner
            if (TargetTypeIndex == 0)
            {
                ActivateInstance(_Spell.Owner);
            }
            // Explicit data
            else if (TargetTypeIndex == 1 && rData != null && rData != _Spell.Data)
            {
                mNode.Data = rData;

                if (rData is Collider)
                {
                    ActivateInstance(((Collider)rData).gameObject);
                }
                else if (rData is Transform)
                {
                    ActivateInstance(((Transform)rData).gameObject);
                }
                else if (rData is GameObject)
                {
                    ActivateInstance((GameObject)rData);
                }
                else if (rData is MonoBehaviour)
                {
                    ActivateInstance(((MonoBehaviour)rData).gameObject);
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
                            ActivateInstance(_Spell.Data.Targets[i]);
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
                            ActivateInstance(_Spell.Data.PreviousTargets[i]);
                        }
                    }
                }
            }

            Deactivate();
        }

        /// <summary>
        /// Deactivates the motion on the specified target
        /// </summary>
        /// <param name="rTarget">GameObject to activate the motion on</param>
        /// <returns>Bool that determines if the motion was activated</returns>
        public void ActivateInstance(GameObject rTarget)
        {
            MotionController lMotionController = rTarget.GetComponent<MotionController>();
            if (lMotionController != null)
            {
                MotionMessage lMessage = MotionMessage.Allocate();
                lMessage.ID = MessageID;

                lMotionController.SendMessage(lMessage);

                lMessage.Release();
            }
        }

        /// <summary>
        /// Called when the action is meant to be deactivated
        /// </summary>
        public override void Deactivate()
        {
            base.Deactivate();
        }

        #region Editor Functions

#if UNITY_EDITOR

        /// <summary>
        /// Called when the inspector needs to draw
        /// </summary>
        public override bool OnInspectorGUI(UnityEngine.Object rTarget)
        {
            bool lIsDirty = base.OnInspectorGUI(rTarget);

            NodeEditorStyle.DrawLine(NodeEditorStyle.LineBlue);

            if (EditorHelper.PopUpField("Target Type", "Determines the target(s) we'll activate the motions on.", TargetTypeIndex, ActivateMotion.GetBestTargetTypes, rTarget))
            {
                lIsDirty = true;
                TargetTypeIndex = EditorHelper.FieldIntValue;
            }

            int lIndex = (_MessageID == 0 ? 0 : _MessageID - MotionMessage.MSG_UNKNOWN);
            if (EditorHelper.PopUpField("Message ID", "", lIndex, MotionMessage.Names, rTarget))
            {
                lIsDirty = true;
                MessageID = EditorHelper.FieldIntValue + MotionMessage.MSG_UNKNOWN;
            }

            return lIsDirty;
        }

#endif

        #endregion
    }
}