using System;
using com.ootii.Actors.AnimationControllers;
using com.ootii.Actors.Combat;
using com.ootii.Base;
using com.ootii.Graphics.NodeGraph;
using com.ootii.Helpers;

namespace com.ootii.Actors.Magic
{
    [Serializable]
    [BaseName("Utility - Send Combat Message")]
    [BaseDescription("Sends a combat message to the motion controller. Typically to control a motion.")]
    public class Utility_SendCombatMessage : SpellAction
    {
        /// <summary>
        /// Message ID to be sent
        /// </summary>
        public int _MessageID = 0;
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
            // Create the pool of prefabs
            base.Awake();
        }

        /// <summary>
        /// Called when the action is first activated
        /// <param name="rPreviousSpellActionState">State of the action prior to this one activating</param>
        public override void Activate(int rPreviousSpellActionState = -1, object rData = null)
        {
            base.Activate(rPreviousSpellActionState, rData);

            MotionController lMotionController = _Spell.Owner.GetComponent<MotionController>();
            if (lMotionController != null)
            {
                CombatMessage lMessage = CombatMessage.Allocate();
                lMessage.ID = MessageID;

                lMotionController.SendMessage(lMessage);

                lMessage.Release();
            }

            base.Deactivate();
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

            if (EditorHelper.IntField("Message ID", "Message ID to be sent to the Motion Controller.", MessageID, rTarget))
            {
                lIsDirty = true;
                MessageID = EditorHelper.FieldIntValue;
            }

            return lIsDirty;
        }

#endif

        #endregion
    }
}