using System;
using com.ootii.Base;
using com.ootii.Graphics.NodeGraph;
using com.ootii.Helpers;

namespace com.ootii.Actors.Magic
{
    [Serializable]
    [BaseName("Utility - Debug")]
    [BaseDescription("Writes a line to the console for debugging.")]
    public class Utility_Debug : SpellAction
    {
        /// <summary>
        /// Text to write
        /// </summary>
        public string _Text = "Debug text";
        public string Text
        {
            get { return _Text; }
            set { _Text = value; }
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

            UnityEngine.Debug.Log("[" + UnityEngine.Time.time.ToString("f3") + "] " + _Text);

            OnSuccess();
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

            NodeEditorStyle.DrawLine(NodeEditorStyle.LineBlue);

            if (EditorHelper.TextField("Text", "Text to write.", Text, rTarget))
            {
                lIsDirty = true;
                Text = EditorHelper.FieldStringValue;
            }

            return lIsDirty;
        }

#endif

        #endregion
    }
}