using System;
using System.Reflection;
using UnityEngine;
using com.ootii.Actors.Attributes;
using com.ootii.Base;
using com.ootii.Graphics.NodeGraph;
using com.ootii.Helpers;

namespace com.ootii.Actors.Magic
{
    [Serializable]
    [BaseName("Set Attribute")]
    [BaseDescription("Sets an attribute's value given the specified attribute name and value.")]
    public class SetAttribute : SpellAction
    {
        /// <summary>
        /// Determines the actual target source
        /// </summary>
        public int _TargetTypeIndex = 0;
        public int TargetTypeIndex
        {
            get { return _TargetTypeIndex; }
            set { _TargetTypeIndex = value; }
        }
        
        /// <summary>
        /// Name of the attribute to set
        /// </summary>
        public string _AttributeName = "";
        public string AttributeName
        {
            get { return _AttributeName; }
            set { _AttributeName = value; }
        }

        /// <summary>
        /// String representation of the attribute to set
        /// </summary>
        public string _StringValue = "";
        public string StringValue
        {
            get { return _StringValue; }
            set { _StringValue = value; }
        }

        /// <summary>
        /// Default constructor
        /// </summary>
        public SetAttribute() : base()
        {
            _DeactivationIndex = EnumSpellActionDeactivation.IMMEDIATELY;
        }

        /// <summary>
        /// Called when the action is first activated
        /// </summary>
        /// <param name="rPreviousSpellActionState">State of the action prior to this one activating</param>
        public override void Activate(int rPreviousSpellActionState = -1, object rData = null)
        {
            // Process each of the targets
            GetBestTargets(TargetTypeIndex, rData, _Spell.Data, ActivateInstance);

            // Continue with the activation
            base.Activate(rPreviousSpellActionState, rData);
        }

        /// <summary>
        /// Activates a single target by running the links for that target
        /// </summary>
        /// <param name="rTarget"></param>
        protected virtual bool ActivateInstance(GameObject rTarget, object rData)
        {
            if (rTarget == null) { return true; }

            IAttributeSource lAttributes = rTarget.GetComponent<IAttributeSource>();
            if (lAttributes == null) { return true; }

            Type lType = lAttributes.GetAttributeType(_AttributeName);

            if (lType == typeof(int))
            {
                int lValue = 0;
                if (int.TryParse(_StringValue, out lValue))
                {
                    lAttributes.SetAttributeValue<int>(_AttributeName, lValue);
                }
            }
            else if (lType == typeof(float))
            {
                float lValue = 0f;
                if (float.TryParse(_StringValue, out lValue))
                {
                    lAttributes.SetAttributeValue<float>(_AttributeName, lValue);
                }
            }
            else if (lType == typeof(string))
            {
                lAttributes.SetAttributeValue<string>(_AttributeName, _StringValue);
            }

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

            if (EditorHelper.TextField("Attribute Name", "Name of the property to set", AttributeName, rTarget))
            {
                lIsDirty = true;
                AttributeName = EditorHelper.FieldStringValue;
            }

            if (EditorHelper.TextField("Value", "String representation of the value to set.", StringValue, rTarget))
            {
                lIsDirty = true;
                StringValue = EditorHelper.FieldStringValue;
            }

            return lIsDirty;
        }

#endif

        #endregion
    }
}