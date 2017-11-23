using System;
using UnityEngine;
using com.ootii.Actors.AnimationControllers;
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
    [BaseName("Apply Force - Spherical")]
    [BaseDescription("Given a specific point, produces a spherical force that will affect physics objects and ActorControllers.")]
    public class ApplyForceSpherical : SpellAction
    {
        /// <summary>
        /// Determines how we'll position force
        /// </summary>
        public int _AnchorTypeIndex = 0;
        public int AnchorTypeIndex
        {
            get { return _AnchorTypeIndex; }
            set { _AnchorTypeIndex = value; }
        }

        /// <summary>
        /// Offset to the force's starting position
        /// </summary>
        public Vector3 _AnchorOffset = new Vector3(0f, 1f, 0f);
        public Vector3 AnchorOffset
        {
            get { return _AnchorOffset; }
            set { _AnchorOffset = value; }
        }

        /// <summary>
        /// Targets to be affected by the force
        /// </summary>
        public int _TargetTypeIndex = 0;
        public int TargetTypeIndex
        {
            get { return _TargetTypeIndex; }
            set { _TargetTypeIndex = value; }
        }

        /// <summary>
        /// Offset to the force's ending position
        /// </summary>
        public Vector3 _TargetOffset = new Vector3(0f, 1f, 0f);
        public Vector3 TargetOffset
        {
            get { return _TargetOffset; }
            set { _TargetOffset = value; }
        }

        /// <summary>
        /// Radius of the explosive force
        /// </summary>
        public float _Radius = 5f;
        public float Radius
        {
            get { return _Radius; }
            set { _Radius = value; }
        }

        /// <summary>
        /// Min force to apply
        /// </summary>
        public float _MinPower = 400f;
        public float MinPower
        {
            get { return _MinPower; }
            set { _MinPower = value; }
        }

        /// <summary>
        /// Max force to apply
        /// </summary>
        public float _MaxPower = 400f;
        public float MaxPower
        {
            get { return _MaxPower; }
            set { _MaxPower = value; }
        }

        /// <summary>
        /// Min force to apply
        /// </summary>
        public float _MinLift = 5f;
        public float MinLift
        {
            get { return _MinLift; }
            set { _MinLift = value; }
        }

        /// <summary>
        /// Max force to apply
        /// </summary>
        public float _MaxLift = 5f;
        public float MaxLift
        {
            get { return _MaxLift; }
            set { _MaxLift = value; }
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

        ///// <summary>
        ///// Determines if we send a push-back message for Motion Controllers
        ///// </summary>
        //public bool _SendPushBackMessage = false;
        //public bool SendPushBackMessage
        //{
        //    get { return _SendPushBackMessage; }
        //    set { _SendPushBackMessage = value; }
        //}

        ///// <summary>
        ///// Power multiplier for the push back
        ///// </summary>
        //public float _PushBackFactor = 0.02f;
        //public float PushBackFactor
        //{
        //    get { return _PushBackFactor; }
        //    set { _PushBackFactor = value; }
        //}

        ///// <summary>
        ///// Determines if we add a ModifyMovement actor effect to the Actor Core
        ///// </summary>
        //public bool _SendModifyMovement = false;
        //public bool SendModifyMovement
        //{
        //    get { return _SendModifyMovement; }
        //    set { _SendModifyMovement = value; }
        //}

        ///// <summary>
        ///// Power multiplier for the movement
        ///// </summary>
        //public float _ModifyMovementFactor = 0.02f;
        //public float ModifyMovementFactor
        //{
        //    get { return _ModifyMovementFactor; }
        //    set { _ModifyMovementFactor = value; }
        //}

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
        /// </summary>
        /// <param name="rPreviousSpellActionState">State of the action prior to this one activating</param>
        public override void Activate(int rPreviousSpellActionState = -1, object rData = null)
        {
            base.Activate(rPreviousSpellActionState, rData);

            // Trigger the forces for each target
            GetBestTargets(TargetTypeIndex, rData, _Spell.Data, ActivateInstance);

            // Immediately deactivate
            Deactivate();
        }

        /// <summary>
        /// Activates a single target by running the links for that target
        /// </summary>
        /// <param name="rTarget"></param>
        protected virtual bool ActivateInstance(GameObject rTarget, object rData)
        {
            if (rTarget == null) { return true; }

            // We calculate the value each time in case the ApplyForceSpherical is called
            // again while we're running (it shouldn't, but just in case)
            Transform lCenterTransform = null;
            Vector3 lCenterPosition = Vector3.zero;
            GetBestPosition(AnchorTypeIndex, null, _Spell.Data, AnchorOffset, out lCenterTransform, out lCenterPosition);

            float lPower = UnityEngine.Random.Range(MinPower, MaxPower);
            if (Invert) { lPower = -lPower; }

            // Push back the Motion Controller if we can
            ActorController lActorController = rTarget.GetComponent<ActorController>();
            //MotionController lMotionController = rTarget.GetComponent<MotionController>();
            if (AddForce && lActorController != null)
            {
                Vector3 lMovement = ((rTarget.transform.position + _TargetOffset) - lCenterPosition).normalized;
                lMovement = lMovement * (lPower * AddForceFactor);

                lActorController.AddImpulse(lMovement);
            }
            //else if (SendPushBackMessage && lMotionController != null)
            //{
            //    Vector3 lMovement = ((rTarget.transform.position + _TargetOffset) - lCenterPosition).normalized;
            //    lMovement = lMovement * (lPower * PushBackFactor);

            //    Navigation.NavigationMessage lMessage = Navigation.NavigationMessage.Allocate();
            //    lMessage.ID = Navigation.NavigationMessage.MSG_NAVIGATE_PUSHED_BACK;
            //    lMessage.Data = lMovement;
            //    lMotionController.SendMessage(lMessage);
            //    lMessage.Release();

            //    if (_Spell.ShowDebug)
            //    {
            //        Graphics.GraphicsManager.DrawSphere(lCenterPosition, 0.2f, Color.blue);
            //        Graphics.GraphicsManager.DrawLine(lCenterPosition, lCenterPosition + lMovement, Color.blue, null, 5f);
            //    }
            //}
            else
            {
                // Use the RigidBody if we have it
                Rigidbody lRigidBody = rTarget.GetComponent<Rigidbody>();
                if (lRigidBody != null && !lRigidBody.isKinematic)
                {
                    float lLift = UnityEngine.Random.Range(MinLift, MaxLift);

                    lRigidBody.AddExplosionForce(lPower, lCenterPosition, Radius, -lLift);
                }
                //else
                //{
                //    // Modify Movement if we should
                //    ActorCore lActorCore = rTarget.GetComponent<ActorCore>();
                //    if (SendModifyMovement && lActorCore != null)
                //    {
                //        string lEffectName = "AFS";

                //        LifeCores.ForceMovement lEffect = lActorCore.GetActiveEffectFromName<LifeCores.ForceMovement>(lEffectName);
                //        if (lEffect != null)
                //        {
                //            lEffect.Age = 0f;
                //        }
                //        else
                //        {
                //            Vector3 lMovement = ((rTarget.transform.position + _TargetOffset) - lCenterPosition).normalized;
                //            lMovement = lMovement * (lPower * ModifyMovementFactor);

                //            lEffect = LifeCores.ForceMovement.Allocate();
                //            lEffect.Name = lEffectName;
                //            lEffect.SourceID = mNode.ID;
                //            lEffect.ActorCore = lActorCore;
                //            lEffect.Movement = lMovement;
                //            lEffect.ReduceMovementOverTime = true;
                //            lEffect.Activate(0f, 1f);

                //            lActorCore.ActiveEffects.Add(lEffect);
                //        }
                //    }
                //}
            }

            return true;              
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

            if (EditorHelper.PopUpField("Anchor", "Determines the kind of positioning we'll use.", AnchorTypeIndex, SpellAction.GetBestPositionTypes, rTarget))
            {
                lIsDirty = true;
                AnchorTypeIndex = EditorHelper.FieldIntValue;
            }

            if (EditorHelper.Vector3Field("Anchor Offset", "Offset from the anchor.", AnchorOffset, rTarget))
            {
                lIsDirty = true;
                AnchorOffset = EditorHelper.FieldVector3Value;
            }

            GUILayout.Space(5f);

            if (EditorHelper.PopUpField("Targets", "Determines the kind of targets we'll affect.", TargetTypeIndex, SpellAction.GetBestTargetTypes, rTarget))
            {
                lIsDirty = true;
                TargetTypeIndex = EditorHelper.FieldIntValue;
            }

            if (EditorHelper.Vector3Field("Target Offset", "Offset from the target.", TargetOffset, rTarget))
            {
                lIsDirty = true;
                TargetOffset = EditorHelper.FieldVector3Value;
            }

            GUILayout.Space(5f);

            if (EditorHelper.FloatField("Radius", "Radius of the explosive force", Radius, rTarget))
            {
                lIsDirty = true;
                Radius = EditorHelper.FieldFloatValue;
            }

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

            // Impact Lift
            GUILayout.BeginHorizontal();

            EditorGUILayout.LabelField(new GUIContent("Impulse Lift", "Min and max force to apply."), GUILayout.Width(EditorGUIUtility.labelWidth - 4f));

            if (EditorHelper.FloatField(MinLift, "Min Lift", rTarget, 0f, 20f))
            {
                lIsDirty = true;
                MinLift = EditorHelper.FieldFloatValue;
            }

            if (EditorHelper.FloatField(MaxLift, "Max Lift", rTarget, 0f, 20f))
            {
                lIsDirty = true;
                MaxLift = EditorHelper.FieldFloatValue;
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

            //// Push Back
            //GUILayout.BeginHorizontal();

            //EditorGUILayout.LabelField(new GUIContent("Send Push-Back Msg", "Determines if we send the Push-Back message to the Motion Controller and the Impulse Power multiplier."), GUILayout.Width(EditorGUIUtility.labelWidth - 4f));

            //if (EditorHelper.BoolField(SendPushBackMessage, "Push Back", rTarget, 16f))
            //{
            //    lIsDirty = true;
            //    SendPushBackMessage = EditorHelper.FieldBoolValue;
            //}

            //if (EditorHelper.FloatField(PushBackFactor, "Push Back Factor", rTarget, 0f, 20f))
            //{
            //    lIsDirty = true;
            //    PushBackFactor = EditorHelper.FieldFloatValue;
            //}

            //GUILayout.EndHorizontal();

            //// Modify Movement
            //GUILayout.BeginHorizontal();

            //EditorGUILayout.LabelField(new GUIContent("Send Movement", "Determines if we force movement with the Actor Core and the Impulse Power multiplier. This is not used with Send Push-Back Msg and Motion Controllers."), GUILayout.Width(EditorGUIUtility.labelWidth - 4f));

            //if (EditorHelper.BoolField(SendModifyMovement, "Modify Movement", rTarget, 16f))
            //{
            //    lIsDirty = true;
            //    SendModifyMovement = EditorHelper.FieldBoolValue;
            //}

            //if (EditorHelper.FloatField(ModifyMovementFactor, "Movement Factor", rTarget, 0f, 20f))
            //{
            //    lIsDirty = true;
            //    ModifyMovementFactor = EditorHelper.FieldFloatValue;
            //}

            //GUILayout.EndHorizontal();

            return lIsDirty;
        }

#endif

        #endregion
    }
}