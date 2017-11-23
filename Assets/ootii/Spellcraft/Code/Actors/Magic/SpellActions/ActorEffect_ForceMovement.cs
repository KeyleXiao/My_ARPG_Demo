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
    [BaseName("Actor Effect - ForceMovement")]
    [BaseDescription("Places an actor effect on the target's ActorCore that forces the actor to move by applying a movement force to the Motion Controller.")]
    public class ActorEffect_ForceMovement : SpellAction
    {
        /// <summary>
        /// Name to give the actor effect. Useful for removing the effect
        /// </summary>
        public string _EffectName = "Force Movement";
        public string EffectName
        {
            get { return _EffectName; }
            set { _EffectName = value; }
        }

        /// <summary>
        /// Defines how much we'll move
        /// </summary>
        public Vector3 _Movement = Vector3.zero;
        public Vector3 Movement
        {
            get { return _Movement; }
            set { _Movement = value; }
        }

        /// <summary>
        /// Determines how long we'll move back for
        /// </summary>
        public float _Duration = 2f;
        public float Duration
        {
            get { return _Duration; }
            set { _Duration = value; }
        }

        /// <summary>
        /// Determines if we reduce the movement based on the elapsed time
        /// </summary>
        public bool _ReduceMovementOverTime = true;
        public bool ReduceMovementOverTime
        {
            get { return _ReduceMovementOverTime; }
            set { _ReduceMovementOverTime = value; }
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
                LifeCores.ForceMovement lEffect = lActorCore.GetActiveEffectFromName<LifeCores.ForceMovement>(EffectName);
                if (lEffect != null)
                {
                    lEffect.Age = 0f;
                }
                else
                {
                    lEffect = LifeCores.ForceMovement.Allocate();
                    lEffect.Name = EffectName;
                    lEffect.SourceID = mNode.ID;
                    lEffect.ActorCore = lActorCore;
                    lEffect.Movement = Movement;
                    lEffect.ReduceMovementOverTime = ReduceMovementOverTime;
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

            if (EditorHelper.Vector3Field("Movement", "Movement that is the direction and speed (per second) that the character should move.", Movement, rTarget))
            {
                lIsDirty = true;
                Movement = EditorHelper.FieldVector3Value;
            }

            if (EditorHelper.FloatField("Max Age", "Time (in seconds) that the movement should continue for.", MaxAge, rTarget))
            {
                lIsDirty = true;
                MaxAge = EditorHelper.FieldFloatValue;
            }

            if (EditorHelper.BoolField("Reduce Over Time", "Determines if we reduce movement over time", ReduceMovementOverTime, rTarget))
            {
                lIsDirty = true;
                ReduceMovementOverTime = EditorHelper.FieldBoolValue;
            }

            return lIsDirty;
        }

#endif

        #endregion
    }
}