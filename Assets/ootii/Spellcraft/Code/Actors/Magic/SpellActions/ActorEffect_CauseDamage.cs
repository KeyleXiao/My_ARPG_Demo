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
    [BaseName("Actor Effect - Cause Damage")]
    [BaseDescription("Places an actor effect on the target's ActorCore that causes damage over time.")]
    public class ActorEffect_CauseDamage : SpellAction
    {
        /// <summary>
        /// Name to give the actor effect. Useful for removing the effect
        /// </summary>
        public string _EffectName = "Cause Damage";
        public string EffectName
        {
            get { return _EffectName; }
            set { _EffectName = value; }
        }

        /// <summary>
        /// Damage type determines what kind of damage it is
        /// </summary>
        public int _DamageType = 0;
        public int DamageType
        {
            get { return _DamageType; }
            set { _DamageType = value; }
        }

        /// <summary>
        /// Impact type determines how the damage is delivered
        /// </summary>
        public int _ImpactType = 0;
        public int ImpactType
        {
            get { return _ImpactType; }
            set { _ImpactType = value; }
        }

        /// <summary>
        /// Index of the stored float to use for damage
        /// </summary>
        public int _DamageFloatValueIndex = -1;
        public int DamageFloatValueIndex
        {
            get { return _DamageFloatValueIndex; }
            set { _DamageFloatValueIndex = value; }
        }

        /// <summary>
        /// Minimum amount of damage caused
        /// </summary>
        public float _MinDamage = 5f;
        public float MinDamage
        {
            get { return _MinDamage; }
            set { _MinDamage = value; }
        }

        /// <summary>
        /// Maximum amount of damage caused
        /// </summary>
        public float _MaxDamage = 5f;
        public float MaxDamage
        {
            get { return _MaxDamage; }
            set { _MaxDamage = value; }
        }

        /// <summary>
        /// Determines if we should play the damage animation 
        /// </summary>
        public bool _PlayAnimation = false;
        public bool PlayAnimation
        {
            get { return _PlayAnimation; }
            set { _PlayAnimation = value; }
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
                LifeCores.CauseDamage lEffect = lActorCore.GetActiveEffectFromSourceID<LifeCores.CauseDamage>(mNode.ID);
                if (lEffect != null)
                {
                    lEffect.Age = 0f;
                }
                else
                {
                    // Determine the damage
                    float lDamage = UnityEngine.Random.Range(_MinDamage, _MaxDamage);
                    if (_DamageFloatValueIndex >= 0 && _Spell.Data.FloatValues != null)
                    {
                        lDamage = _Spell.Data.FloatValues[_DamageFloatValueIndex];
                    }

                    // Setup the message
                    DamageMessage lMessage = DamageMessage.Allocate();
                    lMessage.DamageType = DamageType;
                    lMessage.ImpactType = ImpactType;
                    lMessage.Damage = lDamage;
                    lMessage.AnimationEnabled = PlayAnimation;

                    lEffect = LifeCores.CauseDamage.Allocate();
                    lEffect.Name = EffectName;
                    lEffect.SourceID = mNode.ID;
                    lEffect.ActorCore = lActorCore;
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

            if (EditorHelper.PopUpField("Damage Type", "Type of damage to apply.", DamageType, EnumDamageType.Names, rTarget))
            {
                lIsDirty = true;
                DamageType = EditorHelper.FieldIntValue;
            }

            if (DamageType == 0 || DamageType >= EnumDamageType.Names.Length)
            {
                if (EditorHelper.IntField("Custom ID", "Enter your own custom ID for the damage type.", DamageType, rTarget))
                {
                    lIsDirty = true;
                    DamageType = EditorHelper.FieldIntValue;
                }

                GUILayout.Space(5f);
            }

            if (EditorHelper.PopUpField("Impact Type", "Way in which damage is applied.", ImpactType, EnumImpactType.Names, rTarget))
            {
                lIsDirty = true;
                ImpactType = EditorHelper.FieldIntValue;
            }

            if (ImpactType == 0 || ImpactType >= EnumImpactType.Names.Length)
            {
                if (EditorHelper.IntField("Custom ID", "Enter your own custom ID for the impact type.", ImpactType, rTarget))
                {
                    lIsDirty = true;
                    ImpactType = EditorHelper.FieldIntValue;
                }
            }

            GUILayout.Space(5f);

            // Damage
            if (EditorHelper.IntField("Value Index", "Index into the spell data values or -1 if constants are used.", DamageFloatValueIndex, rTarget))
            {
                lIsDirty = true;
                DamageFloatValueIndex = EditorHelper.FieldIntValue;
            }

            if (DamageFloatValueIndex < 0)
            {
                GUILayout.BeginHorizontal();

                EditorGUILayout.LabelField(new GUIContent("Damage", "Min and max damage to apply."), GUILayout.Width(EditorGUIUtility.labelWidth - 4f));

                if (EditorHelper.FloatField(MinDamage, "Min Damage", rTarget, 0f, 20f))
                {
                    lIsDirty = true;
                    MinDamage = EditorHelper.FieldFloatValue;
                }

                if (EditorHelper.FloatField(MaxDamage, "Max Damage", rTarget, 0f, 20f))
                {
                    lIsDirty = true;
                    MaxDamage = EditorHelper.FieldFloatValue;
                }

                GUILayout.EndHorizontal();
            }

            GUILayout.Space(5f);

            if (EditorHelper.BoolField("Play Animation", "Determines if the damage will cause an animation to play.", PlayAnimation, rTarget))
            {
                lIsDirty = true;
                PlayAnimation = EditorHelper.FieldBoolValue;
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