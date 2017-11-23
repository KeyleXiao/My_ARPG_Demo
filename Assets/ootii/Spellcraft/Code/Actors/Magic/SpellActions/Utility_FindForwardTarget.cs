using System;
using System.Collections.Generic;
using UnityEngine;
using com.ootii.Actors.Attributes;
using com.ootii.Actors.Combat;
using com.ootii.Actors.LifeCores;
using com.ootii.Base;
using com.ootii.Geometry;
using com.ootii.Graphics.NodeGraph;
using com.ootii.Helpers;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace com.ootii.Actors.Magic
{
    [Serializable]
    [BaseName("Utility - Find Forward Target")]
    [BaseDescription("Typically used by an NPC. It selects a target that is 'forward' of the owner based on a spiral raycast.")]
    public class Utility_FindForwardTarget : SpellAction
    {
        /// <summary>
        /// Offset to start the forward raycast
        /// </summary>
        public Vector3 _StartOffset = new Vector3(0f, 1f, 0f);
        public Vector3 StartOffset
        {
            get { return _StartOffset; }
            set { _StartOffset = value; }
        }

        /// <summary>
        /// Minimum Distance the projectile can succeed
        /// </summary>
        public float _MinDistance = 0f;
        public float MinDistance
        {
            get { return _MinDistance; }
            set { _MinDistance = value; }
        }

        /// <summary>
        /// Maximum Distance the projectile can succeed
        /// </summary>
        public float _MaxDistance = 15f;
        public float MaxDistance
        {
            get { return _MaxDistance; }
            set { _MaxDistance = value; }
        }        

        /// <summary>
        /// Layers that we can collide with
        /// </summary>
        public int _CollisionLayers = 1;
        public int CollisionLayers
        {
            get { return _CollisionLayers; }
            set { _CollisionLayers = value; }
        }

        /// <summary>
        /// Comma delimited list of tags where one must exists in order
        /// for the position to be valid
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
        public bool _RequiresRigidbody = false;
        public bool RequiresRigidbody
        {
            get { return _RequiresRigidbody; }
            set { _RequiresRigidbody = value; }
        }

        /// <summary>
        /// Determines if the target must have an actor core
        /// </summary>
        public bool _RequiresActorCore = true;
        public bool RequiresActorCore
        {
            get { return _RequiresActorCore; }
            set { _RequiresActorCore = value; }
        }

        /// <summary>
        /// Determines if the target must be a combatant
        /// </summary>
        public bool _RequiresCombatant = true;
        public bool RequiresCombatant
        {
            get { return _RequiresCombatant; }
            set { _RequiresCombatant = value; }
        }

        /// <summary>
        /// Used to initialize any actions prior to them being activated
        /// </summary>
        public override void Awake()
        {
            // Create the pool of prefabs
            base.Awake();
        }

        /// <summary>
        /// Called when the action is first activated
        /// <param name="rPreviousSpellActionState">State of the action prior to this one activating</param>
        public override void Activate(int rPreviousSpellActionState = -1, object rData = null)
        {
            // Activate the motion
            base.Activate(rPreviousSpellActionState, rData);

            // Find a target and deactivates
            Transform lTarget = FindTarget();
            if (lTarget != null)
            {
                OnSuccess();
            }
            else
            {
                OnFailure();
            }
        }

        /// <summary>
        /// Called when the action is meant to be deactivated
        /// </summary>
        public override void Deactivate()
        {
            base.Deactivate();
        }

        /// <summary>
        /// Called each frame that the action is active
        /// </summary>
        public override void Update()
        {
        }

        /// <summary>
        /// Attempt to find a target that we can focus on. This approach uses a spiralling raycast
        /// </summary>
        /// <returns></returns>
        public virtual Transform FindTarget()
        {
            float lMaxRadius = 8f;
            float lMaxDistance = 20f;
            float lRevolutions = 2f;
            float lDegreesPerStep = 27f;
            float lSteps = lRevolutions * (360f / lDegreesPerStep);
            float lRadiusPerStep = lMaxRadius / lSteps;

            float lAngle = 0f;
            float lRadius = 0f;
            Vector3 lPosition = Vector3.zero;
            //float lColorPerStep = 1f / lSteps;
            //Color lColor = Color.white;

            Transform lOwner = _Spell.Owner.transform;

            // We want our final revolution to be max radius. So, increase the steps
            lSteps = lSteps + (360f / lDegreesPerStep) - 1f;

            // Start at the center and spiral out
            int lCount = 0;
            for (lCount = 0; lCount < lSteps; lCount++)
            {
                lPosition.x = lRadius * Mathf.Cos(lAngle * Mathf.Deg2Rad);
                lPosition.y = lRadius * Mathf.Sin(lAngle * Mathf.Deg2Rad);
                lPosition.z = lMaxDistance;

                //GraphicsManager.DrawLine(mMotionController.CameraTransform.position, mMotionController.CameraTransform.TransformPoint(lPosition), (lCount == 0 ? Color.red : lColor), null, 5f);

                RaycastHit lHitInfo;
                Vector3 lStart = lOwner.position + _StartOffset;
                Vector3 lDirection = lOwner.forward;
                if (RaycastExt.SafeRaycast(lStart, lDirection, out lHitInfo, _MaxDistance, _CollisionLayers, lOwner))
                {
                    // Grab the gameobject this collider belongs to
                    GameObject lGameObject = lHitInfo.collider.gameObject;

                    // Don't count the ignore
                    if (lGameObject.transform == lOwner) { continue; }
                    if (lHitInfo.collider is TerrainCollider) { continue; }

                    if (_Tags != null && _Tags.Length > 0)
                    {
                        IAttributeSource lAttributeSource = lGameObject.GetComponent<IAttributeSource>();
                        if (lAttributeSource == null || !lAttributeSource.AttributesExist(_Tags)) { continue; }
                    }

                    if (RequiresRigidbody && lGameObject.GetComponent<Rigidbody>() == null) { continue; }
                    if (RequiresActorCore && lGameObject.GetComponent<ActorCore>() == null) { continue; }
                    if (RequiresCombatant && lGameObject.GetComponent<ICombatant>() == null) { continue; }

                    // Store the target
                    if (_Spell.Data.Targets == null) { _Spell.Data.Targets = new List<GameObject>(); }
                    _Spell.Data.Targets.Clear();

                    _Spell.Data.Targets.Add(lGameObject);

                    return lGameObject.transform;
                }

                // Increment the spiral
                lAngle += lDegreesPerStep;
                lRadius = Mathf.Min(lRadius + lRadiusPerStep, lMaxRadius);

                //lColor.r = lColor.r - lColorPerStep;
                //lColor.g = lColor.g - lColorPerStep;
            }

            // Return the target hit
            return null;
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

            if (EditorHelper.Vector3Field("Start Offset", "Offset from the owner to start the raycasting.", StartOffset, rTarget))
            {
                lIsDirty = true;
                StartOffset = EditorHelper.FieldVector3Value;
            }

            // Distance
            GUILayout.BeginHorizontal();

            EditorGUILayout.LabelField(new GUIContent("Distance", "Min and max Distance for the projectile to succeed."), GUILayout.Width(EditorGUIUtility.labelWidth - 4f));

            if (EditorHelper.FloatField(MinDistance, "Min Distance", rTarget, 0f, 20f))
            {
                lIsDirty = true;
                MinDistance = EditorHelper.FieldFloatValue;
            }

            if (EditorHelper.FloatField(MaxDistance, "Max Distance", rTarget, 0f, 20f))
            {
                lIsDirty = true;
                MaxDistance = EditorHelper.FieldFloatValue;
            }

            GUILayout.EndHorizontal();

            GUILayout.Space(5f);

            int lNewGroundingLayers = EditorHelper.LayerMaskField(new GUIContent("Layers", "Layers that we'll test collisions against."), CollisionLayers);
            if (lNewGroundingLayers != CollisionLayers)
            {
                lIsDirty = true;
                CollisionLayers = lNewGroundingLayers;
            }

            if (EditorHelper.TextField("Tags", "Comma delimited list of tags where at least one must exist for the selection to be valid.", Tags, rTarget))
            {
                lIsDirty = true;
                Tags = EditorHelper.FieldStringValue;
            }

            if (EditorHelper.BoolField("Rigidbody Required", "Determines if the target must have a Rigidbody component.", RequiresRigidbody, rTarget))
            {
                lIsDirty = true;
                RequiresRigidbody = EditorHelper.FieldBoolValue;
            }

            if (EditorHelper.BoolField("ActorCore Required", "Determines if an Actor Core is required to be a target.", RequiresActorCore, rTarget))
            {
                lIsDirty = true;
                RequiresActorCore = EditorHelper.FieldBoolValue;
            }

            if (EditorHelper.BoolField("Combatant Required", "Determines if a Combatant is required to be a target.", RequiresCombatant, rTarget))
            {
                lIsDirty = true;
                RequiresCombatant = EditorHelper.FieldBoolValue;
            }

            return lIsDirty;
        }

#endif

        #endregion
    }
}