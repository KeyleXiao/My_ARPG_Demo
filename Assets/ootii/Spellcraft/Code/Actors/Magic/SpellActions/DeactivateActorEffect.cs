using System;
using UnityEngine;
using com.ootii.Actors.Attributes;
using com.ootii.Actors.Combat;
using com.ootii.Actors.LifeCores;
using com.ootii.Base;
using com.ootii.Graphics.NodeGraph;
using com.ootii.Helpers;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace com.ootii.Actors.Magic
{
    [Serializable]
    [BaseName("Deactivate Actor Effect")]
    [BaseDescription("Given an instance name, the action deactivates an effect from the specified target.")]
    public class DeactivateActorEffect : SpellAction
    {
        /// <summary>
        /// Name to give the actor effect. Useful for removing the effect
        /// </summary>
        public string _EffectName = "";
        public string EffectName
        {
            get { return _EffectName; }
            set { _EffectName = value; }
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

            if (rData != null && rData != _Spell.Data)
            {
                if (rData is Collider)
                {
                    DeactivateEffect(((Collider)rData).gameObject);
                }
                else if (rData is Transform)
                {
                    DeactivateEffect(((Transform)rData).gameObject);
                }
                else if (rData is GameObject)
                {
                    DeactivateEffect((GameObject)rData);
                }
                else if (rData is MonoBehaviour)
                {
                    DeactivateEffect(((MonoBehaviour)rData).gameObject);
                }
            }
            else if (_Spell.Data != null && _Spell.Data.Targets != null)
            {
                for (int i = 0; i < _Spell.Data.Targets.Count; i++)
                {
                    DeactivateEffect(_Spell.Data.Targets[i]);
                }
            }

            // Immediately deactivate
            Deactivate();
        }

        /// <summary>
        /// Adds an effect to the game object
        /// </summary>
        /// <param name="rObject">GameObject to add the effect to</param>
        protected void DeactivateEffect(GameObject rTarget)
        {
            ActorCore lActorCore = rTarget.GetComponent<ActorCore>();
            if (lActorCore != null)
            {
                LifeCores.ActorCoreEffect lEffect = lActorCore.GetActiveEffectFromName(EffectName);
                if (lEffect != null)
                {
                    lEffect.Deactivate();
                }
            }
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

            if (EditorHelper.TextField("Effect Name", "Name of the actor effect to deactivate.", EffectName, rTarget))
            {
                lIsDirty = true;
                EffectName = EditorHelper.FieldStringValue;
            }

            return lIsDirty;
        }

#endif

        #endregion
    }
}