using System;
using System.Reflection;
using UnityEngine;
using com.ootii.Base;
using com.ootii.Graphics.NodeGraph;
using com.ootii.Helpers;

namespace com.ootii.Actors.Magic
{
    [Serializable]
    [BaseName("Call Function")]
    [BaseDescription("Calls a function on the component of the specified target(s).")]
    public class CallFunction : SpellAction
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
        /// Name of the function to call
        /// </summary>
        public string _FunctionName = "";
        public string FunctionName
        {
            get { return _FunctionName; }
            set { _FunctionName = value; }
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
        public CallFunction() : base()
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

                // Grab the method
                MethodInfo lMethodInfo = lType.GetMethod(FunctionName, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
                if (lMethodInfo != null)
                {
                    ParameterInfo[] lParameters = lMethodInfo.GetParameters();
                    if (lParameters.Length == 0)
                    {
                        lMethodInfo.Invoke(lComponent, null);
                    }
                    else if (lParameters.Length == 1 && lParameters[0].ParameterType == typeof(string))
                    {
                        lMethodInfo.Invoke(lComponent, new object[] { StringArgument });
                    }
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

            if (EditorHelper.TextField("Function Name", "Name of the function to be called", FunctionName, rTarget))
            {
                lIsDirty = true;
                FunctionName = EditorHelper.FieldStringValue;
            }

            if (EditorHelper.TextField("String", "Optional string argument to pass to the function.", StringArgument, rTarget))
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