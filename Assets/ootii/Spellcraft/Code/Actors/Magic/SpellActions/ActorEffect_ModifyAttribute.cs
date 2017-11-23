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
    [BaseName("Actor Effect - Modify Attribute")]
    [BaseDescription("Places an actor effect on the target's ActorCore that modifies the specified attribute value over time.")]
    public class ActorEffect_ModifyAttribute : SpellAction
    {
        /// <summary>
        /// Name to give the actor effect. Useful for removing the effect
        /// </summary>
        public string _EffectName = "Modify Attribute";
        public string EffectName
        {
            get { return _EffectName; }
            set { _EffectName = value; }
        }

        /// <summary>
        /// Attribute whose value is changing
        /// </summary>
        public string _AttributeID = "";
        public string AttributeID
        {
            get { return _AttributeID; }
            set { _AttributeID = value; }
        }

        /// <summary>
        /// Attribute that defines the minimum value
        /// </summary>
        public string _MinAttributeID = "";
        public string MinAttributeID
        {
            get { return _MinAttributeID; }
            set { _MinAttributeID = value; }
        }

        /// <summary>
        /// Attribute that defines the maximum value
        /// </summary>
        public string _MaxAttributeID = "";
        public string MaxAttributeID
        {
            get { return _MaxAttributeID; }
            set { _MaxAttributeID = value; }
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
        /// Time in seconds between each instance of the effect
        /// </summary>
        public float _TriggerDelay = 0.25f;
        public float TriggerDelay
        {
            get { return _TriggerDelay; }
            set { _TriggerDelay = value; }
        }

        /// <summary>
        /// Determines if we reset the attribute value when the effect expires
        /// </summary>
        public bool _ResetOnDeactivate = false;
        public bool ResetOnDeactivate
        {
            get { return _ResetOnDeactivate; }
            set { _ResetOnDeactivate = value; }
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
                LifeCores.ModifyAttribute lEffect = lActorCore.GetActiveEffectFromSourceID<LifeCores.ModifyAttribute>(mNode.ID);
                if (lEffect != null)
                {
                    lEffect.Age = 0f;
                }
                else
                {
                    AttributeMessage lMessage = AttributeMessage.Allocate();
                    lMessage.AttributeID = AttributeID;
                    lMessage.MinAttributeID = MinAttributeID;
                    lMessage.MaxAttributeID = MaxAttributeID;
                    lMessage.Value = UnityEngine.Random.Range(MinValue, MaxValue);

                    lEffect = LifeCores.ModifyAttribute.Allocate();
                    lEffect.Name = EffectName;
                    lEffect.SourceID = mNode.ID;
                    lEffect.ActorCore = lActorCore;
                    lEffect.ResetOnDeactivate = ResetOnDeactivate;
                    lEffect.Activate(TriggerDelay, MaxAge, lMessage);

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

            if (EditorHelper.TextField("Attribute ID", "Attribute ID that is to be changed.", AttributeID, rTarget))
            {
                lIsDirty = true;
                AttributeID = EditorHelper.FieldStringValue;
            }

            if (EditorHelper.TextField("Min Attribute ID", "Min Attribute ID that defines the minimum value.", MinAttributeID, rTarget))
            {
                lIsDirty = true;
                MinAttributeID = EditorHelper.FieldStringValue;
            }

            if (EditorHelper.TextField("Max Attribute ID", "Max Attribute ID that defines the maximum value.", MaxAttributeID, rTarget))
            {
                lIsDirty = true;
                MaxAttributeID = EditorHelper.FieldStringValue;
            }

            // Damage
            GUILayout.BeginHorizontal();

            EditorGUILayout.LabelField(new GUIContent("Value", "Min and max value to change the attribute by."), GUILayout.Width(EditorGUIUtility.labelWidth - 4f));

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

            if (EditorHelper.BoolField("Reset On Deactivate", "Determines if we put back all the changes when the effect deactivates.", ResetOnDeactivate, rTarget))
            {
                lIsDirty = true;
                ResetOnDeactivate = EditorHelper.FieldBoolValue;
            }

            GUILayout.Space(5f);

            if (EditorHelper.FloatField("Max Age", "Time before the effect expires.", MaxAge, rTarget))
            {
                lIsDirty = true;
                MaxAge = EditorHelper.FieldFloatValue;
            }

            if (EditorHelper.FloatField("Trigger Delay", "Delay before the effect is reapplied.", TriggerDelay, rTarget))
            {
                lIsDirty = true;
                TriggerDelay = EditorHelper.FieldFloatValue;
            }

            return lIsDirty;
        }

#endif

        #endregion
    }
}