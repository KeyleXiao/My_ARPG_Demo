using System;
using UnityEngine;
using com.ootii.Actors.Attributes;
using com.ootii.Base;
using com.ootii.Graphics.NodeGraph;
using com.ootii.Helpers;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace com.ootii.Actors.Magic
{
    [Serializable]
    [BaseName("Test Attribute Exists")]
    [BaseDescription("Checks if the attribute on the specified target exists or not.")]
    public class TestAttributeExists : SpellAction
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
        /// Name of the attribute whose value will be compared
        /// </summary>
        public string _AttributeName = "";
        public string AttributeName
        {
            get { return _AttributeName; }
            set { _AttributeName = value; }
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

            // Determines if the test is valid
            bool lIsValid = false;

            // Grab the target and test it
            GameObject lTarget = GetBestTarget(TargetTypeIndex, rData, _Spell.Data);
            if (lTarget != null)
            {
                lIsValid = ActivateInstance(lTarget);
            }

            // Immediately deactivate
            if (lIsValid)
            {
                OnSuccess();
            }
            else
            {
                OnFailure();
            }
        }

        /// <summary>
        /// Activates the action on a single target
        /// </summary>
        /// <param name="rTarget">Target to activate on</param>
        protected bool ActivateInstance(GameObject rTarget)
        {
            if (rTarget == null) { return false; }

            IAttributeSource lAttributeSource = rTarget.GetComponent<IAttributeSource>();
            if (lAttributeSource == null) { return false; }

            if (!lAttributeSource.AttributeExists(AttributeName)) { return false; }

            return true;
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

            if (EditorHelper.PopUpField("Target Type", "Determines the target(s) we'll do the test on.", TargetTypeIndex, ActivateMotion.GetBestTargetTypes, rTarget))
            {
                lIsDirty = true;
                TargetTypeIndex = EditorHelper.FieldIntValue;
            }

            if (EditorHelper.TextField("Attribute Name", "Name of the attribute whose value we will compare", AttributeName, rTarget))
            {
                lIsDirty = true;
                AttributeName = EditorHelper.FieldStringValue;
            }

            return lIsDirty;
        }

#endif

        #endregion
    }
}