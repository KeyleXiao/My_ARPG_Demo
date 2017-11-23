using System;
using System.Collections.Generic;
using UnityEngine;
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
    [BaseName("Utility - Select Position")]
    [BaseDescription("Select a position for a future action.")]
    public class Utility_SelectPosition : SpawnGameObject
    {
        /// <summary>
        /// Used as the GO prefab for this action. The GO includes a projector
        /// </summary>
        protected static GameObject ProjectorPrefab = null;

        /// <summary>
        /// Action alias for the spell to complete
        /// </summary>
        public string _ActionAlias = "Spell Casting Continue";
        public string ActionAlias
        {
            get { return _ActionAlias; }
            set { _ActionAlias = value; }
        }

        /// <summary>
        /// Action alias for the spell to cancel
        /// </summary>
        public string _CancelActionAlias = "Spell Casting Cancel";
        public string CancelActionAlias
        {
            get { return _CancelActionAlias; }
            set { _CancelActionAlias = value; }
        }

        /// <summary>
        /// Minimum Distance the projectile can succeed
        /// </summary>
        public float _MinDistance = 2f;
        public float MinDistance
        {
            get { return _MinDistance; }
            set { _MinDistance = value; }
        }

        /// <summary>
        /// Maximum Distance the projectile can succeed
        /// </summary>
        public float _MaxDistance = 8f;
        public float MaxDistance
        {
            get { return _MaxDistance; }
            set { _MaxDistance = value; }
        }

        /// <summary>
        /// Radius to use with the raycast
        /// </summary>
        public float _Radius = 0f;
        public float Radius
        {
            get { return _Radius; }
            set { _Radius = value; }
        }

        /// <summary>
        /// Determines if we're using the mouse to select
        /// </summary>
        public bool _UseMouse = false;
        public bool UseMouse
        {
            get { return _UseMouse; }
            set { _UseMouse = value; }
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
        /// Determines if the position's normal must match our owner's normal
        /// </summary>
        public bool _RequireMatchingUp = true;
        public bool RequireMatchingUp
        {
            get { return _RequireMatchingUp; }
            set { _RequireMatchingUp = value; }
        }

        /// <summary>
        /// Material to display on the ground
        /// </summary>
        public Material _TargetMaterial = null;
        public Material TargetMaterial
        {
            get { return _TargetMaterial; }
            set { _TargetMaterial = value; }
        }

        /// <summary>
        /// Position that was selected
        /// </summary>
        protected Vector3 mPosition = Vector3.zero;

        /// <summary>
        /// Current forward of the selector
        /// </summary>
        protected Vector3 mForward = Vector3.forward;

        /// <summary>
        /// Life core used for a projector object
        /// </summary>
        protected SelectPositionCore mSelectorCore = null;

        /// <summary>
        /// Projector associated with the action
        /// </summary>
        protected Projector mProjector = null;

        /// <summary>
        /// Used to initialize any actions prior to them being activated
        /// </summary>
        public override void Awake()
        {
            if (Application.isPlaying)
            {
                // If we don't have a prefab set, use a basic projector
                if (Prefab == null)
                {
                    if (Utility_SelectPosition.ProjectorPrefab == null)
                    {
                        Utility_SelectPosition.ProjectorPrefab = new GameObject("ProjectorPrefab ", typeof(Projector));
                        Utility_SelectPosition.ProjectorPrefab.hideFlags = HideFlags.HideInHierarchy;
                    }

                    Prefab = Utility_SelectPosition.ProjectorPrefab;
                }
            }

            // Create the pool of prefabs
            base.Awake();
       }

        /// <summary>
        /// Called when the action is first activated
        /// <param name="rPreviousSpellActionState">State of the action prior to this one activating</param>
        public override void Activate(int rPreviousSpellActionState = -1, object rData = null)
        {
            // Ensure we don't parent to the target
            ParentToTarget = false;

            // Grab an instance from the pool
            base.Activate(rPreviousSpellActionState, rData);

            // Set the material
            if (mInstances != null && mInstances.Count > 0)
            {
                mSelectorCore = mInstances[0].GetComponent<SelectPositionCore>();
                if (mSelectorCore != null)
                {
                    mSelectorCore.Age = 0f;
                    mSelectorCore.MaxAge = 0f;
                    mSelectorCore.Prefab = _Prefab;
                    mSelectorCore.Owner = _Spell.Owner;
                    mSelectorCore.InputSource = _Spell.SpellInventory._InputSource;
                    mSelectorCore.MinDistance = MinDistance;
                    mSelectorCore.MaxDistance = MaxDistance;
                    mSelectorCore.Radius = Radius;
                    mSelectorCore.UseMouse = UseMouse;
                    mSelectorCore.CollisionLayers = CollisionLayers;
                    mSelectorCore.Tags = Tags;
                    mSelectorCore.ActionAlias = ActionAlias;
                    mSelectorCore.CancelActionAlias = CancelActionAlias;
                    mSelectorCore.RequireMatchingUp = RequireMatchingUp;
                    mSelectorCore.OnSelectedEvent = OnSelected;
                    mSelectorCore.OnCancelledEvent = OnCancelled;
                    mSelectorCore.OnReleasedEvent = OnCoreReleased;
                    mSelectorCore.Play();
                }
                else
                {
                    mProjector = mInstances[0].GetComponent<Projector>();
                    if (mProjector != null)
                    {
                        mProjector.nearClipPlane = 0.4f;
                        mProjector.farClipPlane = 3.0f;
                        mProjector.aspectRatio = 4f;
                        mProjector.ignoreLayers = 0;
                        mProjector.material = TargetMaterial;

                        mInstances[0].transform.rotation = Quaternion.LookRotation(-_Spell.Owner.transform.up, _Spell.Owner.transform.forward);
                        mInstances[0].SetActive(true);

                        // Initialize the position
                        mPosition = _Spell.Owner.transform.position + (_Spell.Owner.transform.forward * 2f);
                    }
                }

            }
            else
            {
                Deactivate();
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
            if (mProjector != null)
            {
                RaycastHit lHitInfo;
                Transform lOwner = _Spell.Owner.transform;

                Ray lRay = Camera.main.ScreenPointToRay(UnityEngine.Input.mousePosition);
                if (RaycastExt.SafeRaycast(lRay.origin, lRay.direction, out lHitInfo, 200f, -1, lOwner))
                {
                    float lAngle = Vector3.Angle(lHitInfo.normal, lOwner.up);
                    if (!_RequireMatchingUp || Mathf.Abs(lAngle) <= 2f)
                    {
                        Vector3 lToPoint = lHitInfo.point - lOwner.position;
                        float lToPointDistance = lToPoint.magnitude;
                        mForward = lToPoint.normalized;

                        lToPointDistance = Mathf.Clamp(lToPointDistance, (MinDistance > 0f ? MinDistance : lToPointDistance), MaxDistance);
                        mPosition = lOwner.position + (mForward * lToPointDistance);

                        mProjector.transform.rotation = Quaternion.LookRotation(-lOwner.up, mForward);
                        mProjector.transform.position = mPosition + (Vector3.up * 1f);
                    }
                }

                // Determine if we're done choosing a point
                if (_Spell.SpellInventory != null && _Spell.SpellInventory._InputSource != null)
                {
                    if (_Spell.SpellInventory._InputSource.IsJustPressed(_ActionAlias))
                    {
                        OnSelected(null, mPosition);
                    }
                }
            }
        }

        /// <summary>
        /// Called when the selector has finished selecting
        /// </summary>
        /// <param name="rCore"></param>
        /// <param name="rUserData"></param>
        private void OnSelected(ILifeCore rCore, object rUserData = null)
        {
            Vector3 lPosition = Vector3Ext.Null;
            Vector3 lForward = Vector3Ext.Null;

            if (mProjector != null)
            {
                lPosition = mPosition;
                lForward = mForward;
            }
            else if (rCore is SelectPositionCore)
            {
                lPosition = ((SelectPositionCore)rCore).SelectedPosition;
                lForward = ((SelectPositionCore)rCore).SelectedForward;
            }

            // Determine if we have a valid position
            if (lPosition != Vector3Ext.Null)
            {
                // Determine if we're done choosing a point
                if (_Spell.Data.Positions == null) { _Spell.Data.Positions = new List<Vector3>(); }
                _Spell.Data.Positions.Clear();

                _Spell.Data.Positions.Add(lPosition);

                if (_Spell.Data.Forwards == null) { _Spell.Data.Forwards = new List<Vector3>(); }
                _Spell.Data.Forwards.Clear();

                _Spell.Data.Forwards.Add(lForward);

                // Flag the action as done
                State = EnumSpellActionState.SUCCEEDED;
                mIsShuttingDown = true;
                //OnSuccess();
            }
            else
            {
                OnFailure();
            }
        }

        /// <summary>
        /// Called when the selector has finished selecting
        /// </summary>
        /// <param name="rCore"></param>
        /// <param name="rUserData"></param>
        private void OnCancelled(ILifeCore rCore, object rUserData = null)
        {
            State = EnumSpellActionState.FAILED;
            mIsShuttingDown = true;
        }
        
        /// <summary>
        /// Called when the core has finished running and is released
        /// </summary>
        /// <param name="rCore"></param>
        private void OnCoreReleased(ILifeCore rCore, object rUserData = null)
        {
            if (mSelectorCore != null)
            {
                mSelectorCore.Owner = null;
                mSelectorCore.InputSource = null;
                mSelectorCore.OnSelectedEvent = null;
                mSelectorCore.OnReleasedEvent = null;
                mSelectorCore = null;
            }

            // If our state isn't set to succes (meaning we have something)
            // we are going to flag this node as a failure
            if (State != EnumSpellActionState.SUCCEEDED)
            {
                State = EnumSpellActionState.FAILED;
            }

            base.Deactivate();
        }

        #region Editor Functions

#if UNITY_EDITOR

        /// <summary>
        /// Called when the inspector needs to draw
        /// </summary>
        public override bool OnInspectorGUI(UnityEngine.Object rTarget)
        {
            mEditorShowPrefabField = true;
            mEditorShowParentFields = false;
            bool lIsDirty = base.OnInspectorGUI(rTarget);

            NodeEditorStyle.DrawLine(NodeEditorStyle.LineBlue);

            if (EditorHelper.TextField("Action Alias", "Action alias used to select the position", ActionAlias, rTarget))
            {
                lIsDirty = true;
                ActionAlias = EditorHelper.FieldStringValue;
            }

            if (EditorHelper.TextField("Cancel Alias", "Action alias used to cancel the selection", CancelActionAlias, rTarget))
            {
                lIsDirty = true;
                CancelActionAlias = EditorHelper.FieldStringValue;
            }

            if (EditorHelper.BoolField("Use Mouse", "Determines if we use the mouse to select the position or the reticle (camera).", UseMouse, mTarget))
            {
                lIsDirty = true;
                UseMouse = EditorHelper.FieldBoolValue;
            }

            GUILayout.Space(5f);

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

            if (EditorHelper.FloatField("Radius", "Radius to use with the cast to ensure the selected position isn't too close to an object. Default is 0.", Radius, mTarget))
            {
                lIsDirty = true;
                Radius = EditorHelper.FieldFloatValue;
            }

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

            if (EditorHelper.BoolField("Matching Up Required", "Determines if the position's normal must match the owner's up.", RequireMatchingUp, rTarget))
            {
                lIsDirty = true;
                RequireMatchingUp = EditorHelper.FieldBoolValue;
            }

            //if (Prefab == null)
            //{
            //    if (EditorHelper.ObjectField<Material>("Target Material", "Material to use when we're selecting a point.", TargetMaterial, rTarget))
            //    {
            //        lIsDirty = true;
            //        TargetMaterial = EditorHelper.FieldObjectValue as Material;
            //    }
            //}

            return lIsDirty;
        }

#endif

        #endregion
    }
}