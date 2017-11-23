using System;
using System.Collections.Generic;
using UnityEngine;
using com.ootii.Actors.Attributes;
using com.ootii.Actors.LifeCores;
using com.ootii.Actors.Combat;
using com.ootii.Base;
using com.ootii.Graphics.NodeGraph;
using com.ootii.Helpers;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace com.ootii.Actors.Magic
{
    [Serializable]
    [BaseName("Utility - Find Owner")]
    [BaseDescription("Adds the spell owner to the list of targets.")]
    public class Utility_FindOwner : SpellAction
    {
        /// <summary>
        /// Shift current targets to the previous targets list first
        /// </summary>
        public bool _ShiftToPreviousTargets = false;
        public bool ShiftToPreviousTargets
        {
            get { return _ShiftToPreviousTargets; }
            set { _ShiftToPreviousTargets = value; }
        }

        /// <summary>
        /// Determines if we replace the current list or append to it
        /// </summary>
        public bool _Replace = false;
        public bool Replace
        {
            get { return _Replace; }
            set { _Replace = value; }
        }

        /// <summary>
        /// Comma delimited list of tags where one must exists in order
        /// for the owner to be valid
        /// </summary>
        public string _Tags = "";
        public string Tags
        {
            get { return _Tags; }
            set { _Tags = value; }
        }

        /// <summary>
        /// Determines if the targets require a rigidbody
        /// </summary>
        public bool _RequireRigidbody = false;
        public bool RequireRigidbody
        {
            get { return _RequireRigidbody; }
            set { _RequireRigidbody = value; }
        }

        /// <summary>
        /// Determines if the targets require a rigidbody
        /// </summary>
        public bool _RequireActorCore = false;
        public bool RequireActorCore
        {
            get { return _RequireActorCore; }
            set { _RequireActorCore = value; }
        }

        /// <summary>
        /// Determines if the targets require a rigidbody
        /// </summary>
        public bool _RequireCombatant = false;
        public bool RequireCombatant
        {
            get { return _RequireCombatant; }
            set { _RequireCombatant = value; }
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

            if (_Spell != null && _Spell.Data != null)
            {
                SpellData lSpellData = _Spell.Data;
                GameObject lGameObject = _Spell.Owner;

                // Ignore any existing targets for future tests
                if (ShiftToPreviousTargets && lSpellData.Targets != null && lSpellData.Targets.Count > 0)
                {
                    if (lSpellData.PreviousTargets == null) { lSpellData.PreviousTargets = new List<GameObject>(); }

                    for (int i = 0; i < lSpellData.Targets.Count; i++)
                    {
                        if (!lSpellData.PreviousTargets.Contains(lSpellData.Targets[i]))
                        {
                            lSpellData.PreviousTargets.Add(lSpellData.Targets[i]);
                        }
                    }
                }

                // Remove any existing targets
                if (Replace)
                {
                    if (lSpellData.Targets != null)
                    {
                        lSpellData.Targets.Clear();
                        lSpellData.Targets = null;
                    }
                }

                // Find new targets
                bool lAdd = true;

                if (lAdd && RequireRigidbody && lGameObject.GetComponent<Rigidbody>() == null) { lAdd = false; }
                if (lAdd && RequireActorCore && lGameObject.GetComponent<ActorCore>() == null) { lAdd = false; }
                if (lAdd && RequireCombatant && lGameObject.GetComponent<ICombatant>() == null) { lAdd = false; }
                if (lAdd && lSpellData.PreviousTargets != null && lSpellData.PreviousTargets.Contains(lGameObject)) { lAdd = false; }

                if (lAdd && _Tags != null && _Tags.Length > 0)
                {
                    IAttributeSource lAttributeSource = lGameObject.GetComponent<IAttributeSource>();
                    if (lAttributeSource == null || !lAttributeSource.AttributesExist(_Tags)) { lAdd = false; }
                }

                if (lAdd & !lSpellData.Targets.Contains(lGameObject))
                {
                    lSpellData.Targets.Add(lGameObject);
                }

                if (lAdd)
                {
                    OnSuccess();
                    return;
                }
            }

            // Immediately deactivate
            OnFailure();
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

            if (EditorHelper.BoolField("Shift Targets", "Determines if we shift the current targets to the 'previous targets' list first.", ShiftToPreviousTargets, rTarget))
            {
                lIsDirty = true;
                ShiftToPreviousTargets = EditorHelper.FieldBoolValue;
            }

            if (EditorHelper.BoolField("Replace Targets", "Determines if we clear existing targets before attempting to add the owner.", Replace, rTarget))
            {
                lIsDirty = true;
                Replace = EditorHelper.FieldBoolValue;
            }

            GUILayout.Space(5f);

            if (EditorHelper.TextField("Tags", "Comma delimited list of tags where at least one must exist for the owner to be valid.", Tags, rTarget))
            {
                lIsDirty = true;
                Tags = EditorHelper.FieldStringValue;
            }

            if (EditorHelper.BoolField("Rigidbody Required", "Determines if the owner must have a Rigidbody component.", RequireRigidbody, rTarget))
            {
                lIsDirty = true;
                RequireRigidbody = EditorHelper.FieldBoolValue;
            }

            if (EditorHelper.BoolField("ActorCore Required", "Determines if the owner must have an ActorCore component.", RequireActorCore, rTarget))
            {
                lIsDirty = true;
                RequireActorCore = EditorHelper.FieldBoolValue;
            }

            if (EditorHelper.BoolField("Combatant Required", "Determines if the owner must have a Combatant component.", RequireCombatant, rTarget))
            {
                lIsDirty = true;
                RequireCombatant = EditorHelper.FieldBoolValue;
            }

            return lIsDirty;
        }

#endif

        #endregion
    }
}