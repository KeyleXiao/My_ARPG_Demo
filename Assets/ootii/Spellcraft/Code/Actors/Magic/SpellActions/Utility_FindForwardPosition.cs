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
    [BaseName("Utility - Find Forward Position")]
    [BaseDescription("Typically used by an NPC. It selects a position that is 'forward' of the owner based on a downward raycast.")]
    public class Utility_FindForwardPosition : SpellAction
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
            bool lFound = FindPosition();
            if (lFound)
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
        /// Find a position by targeting down
        /// </summary>
        /// <returns></returns>
        public virtual bool FindPosition()
        {
            Transform lOwner = _Spell.Owner.transform;

            float lStep = (_MinDistance - _MinDistance) / 5f;

            // Start at the center and spiral out
            for (float lDistance = _MaxDistance; lDistance >= _MinDistance; lDistance = lDistance - lStep)
            {
                //GraphicsManager.DrawLine(mMotionController.CameraTransform.position, mMotionController.CameraTransform.TransformPoint(lPosition), (lCount == 0 ? Color.red : lColor), null, 5f);

                RaycastHit lHitInfo;
                Vector3 lStart = lOwner.position + _StartOffset + (lOwner.forward * lDistance);
                Vector3 lDirection = -lOwner.up;
                if (RaycastExt.SafeRaycast(lStart, lDirection, out lHitInfo, _MaxDistance, _CollisionLayers, lOwner))
                {
                    // Grab the gameobject this collider belongs to
                    GameObject lGameObject = lHitInfo.collider.gameObject;

                    // Don't count the ignore
                    if (lGameObject.transform == lOwner) { continue; }

                    if (_Tags != null && _Tags.Length > 0)
                    {
                        IAttributeSource lAttributeSource = lGameObject.GetComponent<IAttributeSource>();
                        if (lAttributeSource == null || !lAttributeSource.AttributesExist(_Tags)) { continue; }
                    }

                    // Determine if we're done choosing a point
                    if (_Spell.Data.Positions == null) { _Spell.Data.Positions = new List<Vector3>(); }
                    _Spell.Data.Positions.Clear();

                    _Spell.Data.Positions.Add(lHitInfo.point);

                    return true;
                }
            }

            // Return the target hit
            return false;
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

            return lIsDirty;
        }

#endif

        #endregion
    }
}