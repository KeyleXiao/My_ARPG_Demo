using System;
using System.Reflection;
using UnityEngine;
using com.ootii.Base;
using com.ootii.Graphics.NodeGraph;
using com.ootii.Helpers;

namespace com.ootii.Actors.Magic
{
    [Serializable]
    [BaseName("Set Property")]
    [BaseDescription("Sets a property on the component of the specified target(s).")]
    public class SetProperty : SpellAction
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
        /// Component where the function exists
        /// </summary>
        public string _ComponentClass = "";
        public string ComponentClass
        {
            get { return _ComponentClass; }
            set { _ComponentClass = value; }
        }

        /// <summary>
        /// Name of the property to set
        /// </summary>
        public string _PropertyName = "";
        public string PropertyName
        {
            get { return _PropertyName; }
            set { _PropertyName = value; }
        }

        /// <summary>
        /// Optional string argument to pass to the function
        /// </summary>
        public string _StringArgument = "";
        public string StringArgument
        {
            get { return _StringArgument; }
            set { _StringArgument = value; }
        }

        /// <summary>
        /// Default constructor
        /// </summary>
        public SetProperty() : base()
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

            // Grab the component
            Component lComponent = rTarget.GetComponent(ComponentClass) as Component;

            if (lComponent == null)
            {
                lComponent = rTarget.GetComponent(ComponentClass + ", Assembly - CSharp, Version = 0.0.0.0, Culture = neutral, PublicKeyToken = null") as Component;
            }

            if (lComponent != null)
            {
                Type lType = lComponent.GetType();

                // Grab the property
                PropertyInfo lPropertyInfo = lType.GetProperty(PropertyName, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
                if (lPropertyInfo != null)
                {
                    Type lPropertyType = lPropertyInfo.PropertyType;

                    try
                    {
                        object lValue = Convert.ChangeType(StringArgument, lPropertyType);
                        lPropertyInfo.SetValue(lComponent, lValue, null);
                    }
                    catch { }
                }
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

            if (EditorHelper.TextField("Component Class", "Full class name of the component. For example: ''", ComponentClass, rTarget))
            {
                lIsDirty = true;
                ComponentClass = EditorHelper.FieldStringValue;
            }

            if (EditorHelper.TextField("Property Name", "Name of the property to set", PropertyName, rTarget))
            {
                lIsDirty = true;
                PropertyName = EditorHelper.FieldStringValue;
            }

            if (EditorHelper.TextField("String", "String representation of the value.", StringArgument, rTarget))
            {
                lIsDirty = true;
                StringArgument = EditorHelper.FieldStringValue;
            }

            return lIsDirty;
        }

#endif

        #endregion
    }
}