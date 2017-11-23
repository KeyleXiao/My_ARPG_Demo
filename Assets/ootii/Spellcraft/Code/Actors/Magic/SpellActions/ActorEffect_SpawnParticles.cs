using System;
using UnityEngine;
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
    [BaseName("Actor Effect - Spawn Particles")]
    [BaseDescription("Places an actor effect on the target's ActorCore that spawns and runs a particle effect.")]
    public class ActorEffect_SpawnParticles : SpellAction
    {
        /// <summary>
        /// Name to give the actor effect. Useful for removing the effect
        /// </summary>
        public string _EffectName = "Spawn Particles";
        public string EffectName
        {
            get { return _EffectName; }
            set { _EffectName = value; }
        }

        /// <summary>
        /// Prefab that is the particles that are being spawned
        /// </summary>
        public GameObject _Prefab = null;
        public virtual GameObject Prefab
        {
            get { return _Prefab; }
            set { _Prefab = value; }
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
                LifeCores.SpawnParticles lEffect = lActorCore.GetActiveEffectFromSourceID<LifeCores.SpawnParticles>(mNode.ID);
                if (lEffect != null)
                {
                    lEffect.Age = 0f;
                }
                else
                {
                    lEffect = LifeCores.SpawnParticles.Allocate();
                    lEffect.Name = EffectName;
                    lEffect.SourceID = mNode.ID;
                    lEffect.ActorCore = lActorCore;
                    lEffect.Activate(MaxAge, Prefab);

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

            NodeEditorStyle.DrawInspectorDescription("The prefab must include a Particle Core component.", MessageType.None);

            if (EditorHelper.ObjectField<GameObject>("Prefab", "Prefab we'll use as a template to spawn GameObjects.", Prefab, rTarget))
            {
                lIsDirty = true;
                Prefab = EditorHelper.FieldObjectValue as GameObject;
            }

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