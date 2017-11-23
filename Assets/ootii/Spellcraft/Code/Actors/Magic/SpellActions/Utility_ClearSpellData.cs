using System;
using System.Collections.Generic;
using UnityEngine;
using com.ootii.Base;
using com.ootii.Graphics.NodeGraph;
using com.ootii.Helpers;

namespace com.ootii.Actors.Magic
{
    [Serializable]
    [BaseName("Utility - Clear Spell Data")]
    [BaseDescription("Clears the current spell data object.")]
    public class Utility_ClearSpellData : SpellAction
    {
        /// <summary>
        /// Setting the owner as a previous target is sometimes helpful when
        /// chaining effects.
        /// </summary>
        public bool _SetOwnerAsPreviousTarget = false;
        public bool SetOwnerAsPreviousTarget
        {
            get { return _SetOwnerAsPreviousTarget; }
            set { _SetOwnerAsPreviousTarget = value; }
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

            _Spell.Data.Clear();

            if (SetOwnerAsPreviousTarget)
            {
                _Spell.Data.PreviousTargets = new List<GameObject>();
                _Spell.Data.PreviousTargets.Add(_Spell.Owner);
            }
            
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

            if (EditorHelper.BoolField("Set Owner", "Determines if we set the owner as a previous target.", SetOwnerAsPreviousTarget, rTarget))
            {
                lIsDirty = true;
                SetOwnerAsPreviousTarget = EditorHelper.FieldBoolValue;
            }

            return lIsDirty;
        }

#endif

        #endregion
    }
}