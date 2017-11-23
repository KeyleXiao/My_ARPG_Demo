using System;
using com.ootii.Base;
using com.ootii.Graphics.NodeGraph;
using com.ootii.Helpers;

namespace com.ootii.Actors.Magic
{
    [Serializable]
    [BaseName("Utility - Wait For Input")]
    [BaseDescription("Waits for a specific input to occur.")]
    public class Utility_WaitForInput : SpellAction
    {
        /// <summary>
        /// Action alias whose input we're looking for
        /// </summary>
        public string _ActionAlias = "";
        public string ActionAlias
        {
            get { return _ActionAlias; }
            set { _ActionAlias = value; }
        }

        /// <summary>
        /// Default constructor
        /// </summary>
        public Utility_WaitForInput() : base()
        {
            _DeactivationIndex = EnumSpellActionDeactivation.MANAGED;
        }

        /// <summary>
        /// Determines if the spell should deactivate
        /// </summary>
        public override bool TestDeactivate()
        {
            bool lDeactivate = base.TestDeactivate();

            if (!lDeactivate)
            {
                if (_ActionAlias.Length == 0 || _Spell.SpellInventory._InputSource == null)
                {
                    lDeactivate = true;
                }
                else if (_Spell.SpellInventory._InputSource.IsJustPressed(_ActionAlias))
                {
                    lDeactivate = true;
                }
            }

            return lDeactivate;
        }

        #region Editor Functions

#if UNITY_EDITOR

        /// <summary>
        /// Called when the inspector needs to draw
        /// </summary>
        public override bool OnInspectorGUI(UnityEngine.Object rTarget)
        {
            mEditorShowDeactivationField = true;
            bool lIsDirty = base.OnInspectorGUI(rTarget);

            NodeEditorStyle.DrawLine(NodeEditorStyle.LineBlue);

            if (EditorHelper.TextField("Action Alias", "Input entry that we're waiting for.", ActionAlias, rTarget))
            {
                lIsDirty = true;
                ActionAlias = EditorHelper.FieldStringValue;
            }

            return lIsDirty;
        }

#endif

        #endregion
    }
}