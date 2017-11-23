using System;
using com.ootii.Base;
using com.ootii.Graphics.NodeGraph;
using com.ootii.Helpers;

namespace com.ootii.Actors.Magic
{
    /// <summary>
    /// A NodeLinkAction is used to determine if links can be
    /// traversed. We can also use them to modify data.
    /// </summary>
    [Serializable]
    [BaseName("Compare Spell State")]
    [BaseDescription("Checks if the spell's state matches the one we expect.")]
    public class CompareSpellState : NodeLinkAction
    {
        /// <summary>
        /// State we will compare
        /// </summary>
        public int State = 0;

        /// <summary>
        /// Simple test to determine if the link can be traversed
        /// </summary>
        /// <param name="rUserData">Optional data to help with the test</param>
        /// <returns>Determines if the link can be traversed</returns>
        public override bool TestActivate(object rUserData = null)
        {
            if (_Link == null || _Link.StartNode == null || _Link.StartNode._Content == null) { return false; }

            SpellAction lSpellAction = _Link.StartNode._Content as SpellAction;
            if (lSpellAction == null) { return false; }
            if (lSpellAction._Spell == null) { return false; }

            if (lSpellAction._Spell.State == State)
            {
                return true;
            }

            return false;
        }

#if UNITY_EDITOR

        /// <summary>
        /// Called when the inspector needs to draw
        /// </summary>
        public override bool OnInspectorGUI(UnityEngine.Object rTarget)
        {
            bool lIsDirty = false;

            if (EditorHelper.PopUpField("Spell State", "State of the spell we were comparing to. If equal, the test passes.", State, EnumSpellState.Names, rTarget))
            {
                lIsDirty = true;
                State = EditorHelper.FieldIntValue;
            }

            return lIsDirty;
        }

#endif
    }
}