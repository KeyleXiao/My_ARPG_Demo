using System;
using UnityEngine;
using com.ootii.Base;
using com.ootii.Graphics.NodeGraph;
using com.ootii.Helpers;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace com.ootii.Actors.Magic
{
    [Serializable]
    [BaseName("Apply Force - Vector")]
    [BaseDescription("Given a specific point, produces a directional force that will affect physics objects and ActorControllers.")]
    public class ApplyForceVector : SpellAction
    {
        /// <summary>
        /// Min force to apply
        /// </summary>
        public float _MinPower = 200f;
        public float MinPower
        {
            get { return _MinPower; }
            set { _MinPower = value; }
        }

        /// <summary>
        /// Max force to apply
        /// </summary>
        public float _MaxPower = 200f;
        public float MaxPower
        {
            get { return _MaxPower; }
            set { _MaxPower = value; }
        }

        /// <summary>
        /// Determines if we invert the force to pull instead of push
        /// </summary>
        public bool _Invert = false;
        public bool Invert
        {
            get { return _Invert; }
            set { _Invert = value; }
        }

        /// <summary>
        /// Determines if we add the force to the Actor Controller
        /// </summary>
        public bool _AddForce = true;
        public bool AddForce
        {
            get { return _AddForce; }
            set { _AddForce = value; }
        }

        /// <summary>
        /// Power multiplier for the added force
        /// </summary>
        public float _AddForceFactor = 0.02f;
        public float AddForceFactor
        {
            get { return _AddForceFactor; }
            set { _AddForceFactor = value; }
        }

        /// <summary>
        /// Used to initialize any actions prior to them being activated
        /// </summary>
        public override void Awake()
        {
            _DeactivationIndex = EnumSpellActionDeactivation.IMMEDIATELY;
            base.Awake();
        }

        /// <summary>
        /// Called when the action is first activated
        /// <param name="rPreviousSpellActionState">State of the action prior to this one activating</param>
        public override void Activate(int rPreviousSpellActionState = -1, object rData = null)
        {
            base.Activate(rPreviousSpellActionState, rData);

            if (_Spell != null && _Spell.Data != null && _Spell.Data.Targets != null)
            {
                for (int i = 0; i < _Spell.Data.Targets.Count; i++)
                {
                    GameObject lTarget = _Spell.Data.Targets[i];
                    if (object.ReferenceEquals(lTarget, null)) { continue; }

                    Vector3 lPoint = lTarget.transform.position;
                    Vector3 lForward = lTarget.transform.forward;

                    if (i < _Spell.Data.Positions.Count)
                    {
                        lPoint = _Spell.Data.Positions[i];
                    }

                    if (i < _Spell.Data.Positions.Count)
                    {
                        lForward = _Spell.Data.Forwards[i];
                    }

                    ActivateInstance(lTarget, lPoint, lForward);
                }
            }
            else
            {
                GameObject lTarget = GetBestTarget(1, rData, _Spell.Data);
                if (lTarget != null)
                {
                    Vector3 lPoint = lTarget.transform.position;
                    Vector3 lForward = (lTarget.transform.position - _Spell.Owner.transform.position).normalized;

                    ActivateInstance(lTarget.gameObject, lPoint, lForward);
                }
            }

            // Immediately deactivate
            Deactivate();
        }

        /// <summary>
        /// Activates the action on a single target
        /// </summary>
        /// <param name="rTarget">Target to activate on</param>
        protected void ActivateInstance(GameObject rTarget, Vector3 rPoint, Vector3 rForward)
        {
            float lPower = UnityEngine.Random.Range(MinPower, MaxPower);
            if (Invert) { lPower = -lPower; }

            // Push back the Motion Controller if we can
            ActorController lActorController = rTarget.GetComponent<ActorController>();
            if (AddForce && lActorController != null)
            {
                Vector3 lMovement = rForward.normalized * (lPower * AddForceFactor);
                lActorController.AddImpulse(lMovement);
            }
            else
            {
                Rigidbody lRigidBody = rTarget.GetComponent<Rigidbody>();
                if (lRigidBody != null)
                {
                    // Compensate for the exaggerated force describe in Unity documentation
                    lRigidBody.AddForceAtPosition(rForward.normalized * lPower * 0.01f, rPoint, ForceMode.Impulse);
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

            // Impact Power
            GUILayout.BeginHorizontal();

            EditorGUILayout.LabelField(new GUIContent("Impulse Power", "Min and max force to apply."), GUILayout.Width(EditorGUIUtility.labelWidth - 4f));

            if (EditorHelper.FloatField(MinPower, "Min Power", rTarget, 0f, 20f))
            {
                lIsDirty = true;
                MinPower = EditorHelper.FieldFloatValue;
            }

            if (EditorHelper.FloatField(MaxPower, "Max Power", rTarget, 0f, 20f))
            {
                lIsDirty = true;
                MaxPower = EditorHelper.FieldFloatValue;
            }

            GUILayout.EndHorizontal();

            if (EditorHelper.BoolField("Invert", "Determines if we invert the power to create a pull instead of a push.", Invert, rTarget))
            {
                lIsDirty = true;
                Invert = EditorHelper.FieldBoolValue;
            }

            GUILayout.Space(5f);

            // Add Force
            GUILayout.BeginHorizontal();

            EditorGUILayout.LabelField(new GUIContent("Add AC Force", "Determines if we move the actor by adding an impulse to the Actor Controller directly."), GUILayout.Width(EditorGUIUtility.labelWidth - 4f));

            if (EditorHelper.BoolField(AddForce, "Add Force", rTarget, 16f))
            {
                lIsDirty = true;
                AddForce = EditorHelper.FieldBoolValue;
            }

            if (EditorHelper.FloatField(AddForceFactor, "Force Factor", rTarget, 0f, 20f))
            {
                lIsDirty = true;
                AddForceFactor = EditorHelper.FieldFloatValue;
            }

            GUILayout.EndHorizontal();

            return lIsDirty;
        }

#endif

        #endregion
    }
}