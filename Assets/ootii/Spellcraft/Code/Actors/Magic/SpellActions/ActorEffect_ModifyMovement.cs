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
    [BaseName("Actor Effect - Modify Movement")]
    [BaseDescription("Places an actor effect on the target's ActorCore that modifies the Actor Controller's movement.")]
    public class ActorEffect_ModifyMovement : SpellAction
    {
        /// <summary>
        /// Name to give the actor effect. Useful for removing the effect
        /// </summary>
        public string _EffectName = "Modify Movement";
        public string EffectName
        {
            get { return _EffectName; }
            set { _EffectName = value; }
        }

        /// <summary>
        /// Minimum amount of value to change
        /// </summary>
        public float _MinValue = 1f;
        public float MinValue
        {
            get { return _MinValue; }
            set { _MinValue = value; }
        }

        /// <summary>
        /// Maximum amount of value to change
        /// </summary>
        public float _MaxValue = 1f;
        public float MaxValue
        {
            get { return _MaxValue; }
            set { _MaxValue = value; }
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
                    AddEffect(((Collider)rData).gameObject);
                }
                else if (rData is Transform)
                {
                    AddEffect(((Transform)rData).gameObject);
                }
                else if (rData is GameObject)
                {
                    AddEffect((GameObject)rData);
                }
                else if (rData is MonoBehaviour)
                {
                    AddEffect(((MonoBehaviour)rData).gameObject);
                }
            }
            else if (_Spell.Data != null && _Spell.Data.Targets != null)
            {
                for (int i = 0; i < _Spell.Data.Targets.Count; i++)
                {
                    AddEffect(_Spell.Data.Targets[i]);
                }
            }

            // Immediately deactivate
            Deactivate();
        }

        /// <summary>
        /// Adds an effect to the game object
        /// </summary>
        /// <param name="rObject">GameObject to add the effect to</param>
        protected void AddEffect(GameObject rTarget)
        {
            ActorCore lActorCore = rTarget.GetComponent<ActorCore>();
            if (lActorCore != null)
            {
                LifeCores.ModifyMovement lEffect = lActorCore.GetActiveEffectFromName<LifeCores.ModifyMovement>(EffectName);
                if (lEffect != null)
                {
                    lEffect.Age = 0f;
                }
                else
                {
                    lEffect = LifeCores.ModifyMovement.Allocate();
                    lEffect.Name = EffectName;
                    lEffect.SourceID = mNode.ID;
                    lEffect.ActorCore = lActorCore;
                    lEffect.MovementFactor = UnityEngine.Random.Range(MinValue, MaxValue); 
                    lEffect.Activate(0f, MaxAge);

                    lActorCore.Effects.Add(lEffect);
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

            if (EditorHelper.TextField("Effect Name", "Unique name to give the actor effect.", EffectName, rTarget))
            {
                lIsDirty = true;
                EffectName = EditorHelper.FieldStringValue;
            }

            // Movement Factor
            GUILayout.BeginHorizontal();

            EditorGUILayout.LabelField(new GUIContent("Value", "Min and max factor to change movement by."), GUILayout.Width(EditorGUIUtility.labelWidth - 4f));

            if (EditorHelper.FloatField(MinValue, "Min Value", rTarget, 0f, 20f))
            {
                lIsDirty = true;
                MinValue = EditorHelper.FieldFloatValue;
            }

            if (EditorHelper.FloatField(MaxValue, "Max Value", rTarget, 0f, 20f))
            {
                lIsDirty = true;
                MaxValue = EditorHelper.FieldFloatValue;
            }

            GUILayout.EndHorizontal();

            GUILayout.Space(5f);

            if (EditorHelper.FloatField("Max Age", "Time before the effect expires.", MaxAge, rTarget))
            {
                lIsDirty = true;
                MaxAge = EditorHelper.FieldFloatValue;
            }

            return lIsDirty;
        }

#endif

        #endregion
    }
}