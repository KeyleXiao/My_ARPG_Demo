using System;
using System.Collections.Generic;
using UnityEngine;
using com.ootii.Actors.LifeCores;
using com.ootii.Actors.AnimationControllers;
using com.ootii.Base;
using com.ootii.Graphics.NodeGraph;
using com.ootii.Helpers;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace com.ootii.Actors.Magic
{
    [Serializable]
    [BaseName("Spawn Projectile")]
    [BaseDescription("Spawns a projectile from the specified prefab and launches it.")]
    public class SpawnProjectile : SpawnGameObject
    {
        /// <summary>
        /// Angle applied to the aim to make it look like
        /// we're pointing towards the target. Posative value is right
        /// and negative value is left.
        /// </summary>
        public float _HorizontalAngle = 0f;
        public float HorizontalAngle
        {
            get { return _HorizontalAngle; }
            set { _HorizontalAngle = value; }
        }

        /// <summary>
        /// Angle applied to the aim to make it look like
        /// we're pointing towards the target. Posative value is down
        /// and negative value is up.
        /// </summary>
        public float _VerticalAngle = 0f;
        public float VerticalAngle
        {
            get { return _VerticalAngle; }
            set { _VerticalAngle = value; }
        }

        /// <summary>
        /// Speed of the projectile
        /// </summary>
        public float _Speed = 3f;
        public float Speed
        {
            get { return _Speed; }
            set { _Speed = value; }
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
        public float _MaxDistance = 5f;
        public float MaxDistance
        {
            get { return _MaxDistance; }
            set { _MaxDistance = value; }
        }

        /// <summary>
        /// Determines if the projectile moves towards a target
        /// </summary>
        public bool _IsHoming = false;
        public bool IsHoming
        {
            get { return _IsHoming; }
            set { _IsHoming = value; }
        }

        /// <summary>
        /// Offset from the target transform to home in on
        /// </summary>
        public Vector3 _TargetOffset = new Vector3(0f, 1f, 0f);
        public Vector3 TargetOffset
        {
            get { return _TargetOffset; }
            set { _TargetOffset = value; }
        }

        /// <summary>
        /// Determines if we use the camera postition and camera forward as the direction of the projectile's launch
        /// </summary>
        public bool _ReleaseFromCamera = false;
        public bool ReleaseFromCamera
        {
            get { return _ReleaseFromCamera; }
            set { _ReleaseFromCamera = value; }
        }

        /// <summary>
        /// When releasing from the camera, the release distance
        /// </summary>
        public float _ReleaseDistance = 2f;
        public float ReleaseDistance
        {
            get { return _ReleaseDistance; }
            set { _ReleaseDistance = value; }
        }

        /// <summary>
        /// Determines if we use the camera forward as the direction of the projectile's launch
        /// </summary>
        public bool _ReleaseFromCameraForward = true;
        public bool ReleaseFromCameraForward
        {
            get { return _ReleaseFromCameraForward; }
            set { _ReleaseFromCameraForward = value; }
        }

        /// <summary>
        /// Determines if we'll consider an expire without a collision a success
        /// </summary>
        public bool _SucceedOnExpire = false;
        public bool SucceedOnExpire
        {
            get { return _SucceedOnExpire; }
            set { _SucceedOnExpire = value; }
        }

        /// <summary>
        /// Area core that was spawned
        /// </summary>
        protected ProjectileCore mProjectileCore = null;

        /// <summary>
        /// Called when the action is first activated
        /// <param name="rPreviousSpellActionState">State of the action prior to this one activating</param>
        public override void Activate(int rPreviousSpellActionState = -1, object rData = null)
        {
            base.Activate(rPreviousSpellActionState, rData);

            if (mInstances != null && mInstances.Count > 0)
            {
                if (_Spell.ShowDebug)
                {
                    for (int i = 0; i < mInstances.Count; i++)
                    {
                        mInstances[i].hideFlags = HideFlags.None;
                    }
                }

                // Set the basics
                mInstances[0].transform.parent = null;

                // Now we want to launch the projectile
                mProjectileCore = mInstances[0].GetComponent<ProjectileCore>();
                if (mProjectileCore != null)
                {
                    mProjectileCore.Prefab = _Prefab;
                    mProjectileCore.Owner = _Spell.Owner;
                    mProjectileCore.Speed = Speed;
                    mProjectileCore.MaxAge = MaxAge;
                    mProjectileCore.MinRange = MinDistance;
                    mProjectileCore.MaxRange = MaxDistance;
                    mProjectileCore.transform.rotation = Spell.Owner.transform.rotation;
                    mProjectileCore.OnReleasedEvent = OnCoreReleased;
                    mProjectileCore.OnImpactEvent = OnImpact;
                    mProjectileCore.OnExpiredEvent = OnExpired;
                    mProjectileCore.Launch(_HorizontalAngle, _VerticalAngle);

                    mProjectileCore.IsHoming = IsHoming;
                    if (mProjectileCore.IsHoming)
                    {
                        GameObject lTarget = GetBestTarget(1, rData, _Spell.Data);
                        if (lTarget != null)
                        {
                            mProjectileCore.Target = lTarget.transform;
                            mProjectileCore.TargetOffset = TargetOffset;
                        }
                    }
                }

                // Determine how we release the spell
                if (_Spell.ReleaseFromCamera && Camera.main != null)
                {
                    Transform lTransform = Camera.main.transform;

                    mInstances[0].transform.position = lTransform.position + (lTransform.forward * _Spell.ReleaseDistance);
                    mInstances[0].transform.rotation = lTransform.rotation;

                    // Apply any rotation adjustment
                    Quaternion lRotation = Quaternion.AngleAxis(_HorizontalAngle, lTransform.up) * Quaternion.AngleAxis(_VerticalAngle, lTransform.right);
                    mInstances[0].transform.rotation = lRotation * lTransform.rotation;

                }
                else if (ReleaseFromCamera && Camera.main != null)
                {
                    Transform lTransform = Camera.main.transform;

                    mInstances[0].transform.position = lTransform.position + (lTransform.forward * _ReleaseDistance);
                    mInstances[0].transform.rotation = lTransform.rotation;

                    // Apply any rotation adjustment
                    Quaternion lRotation = Quaternion.AngleAxis(_HorizontalAngle, lTransform.up) * Quaternion.AngleAxis(_VerticalAngle, lTransform.right);
                    mInstances[0].transform.rotation = lRotation * lTransform.rotation;

                }
                else if (ReleaseFromCameraForward && Camera.main != null)
                {
                    Transform lTransform = Camera.main.transform;

                    // Apply any rotation adjustment
                    Quaternion lRotation = Quaternion.AngleAxis(_HorizontalAngle, lTransform.up) * Quaternion.AngleAxis(_VerticalAngle, lTransform.right);
                    mInstances[0].transform.rotation = lRotation * lTransform.rotation;
                }
            }
            else
            {
                Deactivate();
            }

            // Determine if we're returnning immediately
            if (_DeactivationIndex == EnumSpellActionDeactivation.IMMEDIATELY)
            {
                mIsShuttingDown = true;
                State = EnumSpellActionState.SUCCEEDED;
            }
        }

        /// <summary>
        /// Called when the action is meant to be deactivated
        /// </summary>
        public override void Deactivate()
        {
            mIsShuttingDown = true;
        }

        /// <summary>
        /// Called when the projectile impacts something
        /// </summary>
        /// <param name="rCore">The life core that raised the event</param>
        /// <param name="rUserData">User data returned</param>
        private void OnImpact(ILifeCore rCore, object rUserData = null)
        {
            Combat.CombatHit lHitInfo = (Combat.CombatHit)rUserData;

            // Fill the data with the target information
            SpellData lData = _Spell.Data;

            if (lData.Targets == null) { lData.Targets = new List<GameObject>(); }
            lData.Targets.Add(lHitInfo.Collider.gameObject);

            if (lData.Positions == null) { lData.Positions = new List<Vector3>(); }
            lData.Positions.Add(lHitInfo.Point);

            if (lData.Forwards == null) { lData.Forwards = new List<Vector3>(); }
            if (_Spell.Owner != null) { lData.Forwards.Add(_Spell.Owner.transform.forward); }

            mIsShuttingDown = true;
            State = EnumSpellActionState.SUCCEEDED;
        }

        /// <summary>
        /// Called when the projectile expires without impacting anything
        /// </summary>
        /// <param name="rCore">The life core that raised the event</param>
        /// <param name="rUserData">User data returned</param>
        private void OnExpired(ILifeCore rCore, object rUserData = null)
        {
            mIsShuttingDown = true;

            if (SucceedOnExpire)
            {
                State = EnumSpellActionState.SUCCEEDED;
            }
            else
            {
                State = EnumSpellActionState.FAILED;
            }
        }

        /// <summary>
        /// Called when the core has finished running and is released
        /// </summary>
        /// <param name="rCore">The life core that raised the event</param>
        /// <param name="rUserData">User data returned</param>
        private void OnCoreReleased(ILifeCore rCore, object rUserData = null)
        {
            if (mProjectileCore != null)
            {
                mProjectileCore.Owner = null;
                mProjectileCore.OnReleasedEvent = null;
                mProjectileCore.OnImpactEvent = null;
                mProjectileCore.OnExpiredEvent = null;
                mProjectileCore = null;
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
            mEditorShowDeactivationField = true;
            bool lIsDirty = base.OnInspectorGUI(rTarget);

            NodeEditorStyle.DrawLine(NodeEditorStyle.LineBlue);

            if (EditorHelper.FloatField("Speed", "Units per second that the projectile moves (when no rigidbody is attached).", Speed, rTarget))
            {
                lIsDirty = true;
                Speed = EditorHelper.FieldFloatValue;
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

            if (EditorHelper.BoolField("Is Homing", "Determines if the projectile tracks the target and moves to it.", IsHoming, rTarget))
            {
                lIsDirty = true;
                IsHoming = EditorHelper.FieldBoolValue;
            }

            if (IsHoming)
            {
                if (EditorHelper.Vector3Field("Target Offset", "Offset from the target transform position that the projectile will move towards.", TargetOffset, rTarget))
                {
                    lIsDirty = true;
                    TargetOffset = EditorHelper.FieldVector3Value;
                }
            }

            GUILayout.Space(5f);

            if (EditorHelper.BoolField("Camera Release", "Determines if we use the camera position and camera forward as the direction of the projectile's launch", ReleaseFromCamera, rTarget))
            {
                lIsDirty = true;
                ReleaseFromCamera = EditorHelper.FieldBoolValue;
            }

            if (ReleaseFromCamera)
            {
                if (EditorHelper.FloatField("  Distance", "Distance to release from the camera", ReleaseDistance, rTarget))
                {
                    lIsDirty = true;
                    ReleaseDistance = EditorHelper.FieldFloatValue;
                }
            }
            else
            {
                if (EditorHelper.BoolField("Camera Direction", "Determines if we use ONLY the camera forward as the direction of the projectile's launch", ReleaseFromCameraForward, rTarget))
                {
                    lIsDirty = true;
                    ReleaseFromCameraForward = EditorHelper.FieldBoolValue;
                }
            }

            // Angles
            GUILayout.BeginHorizontal();

            EditorGUILayout.LabelField(new GUIContent("Launch Angles", "Additive angles for the projectile direction."), GUILayout.Width(EditorGUIUtility.labelWidth - 4f));

            if (EditorHelper.FloatField(HorizontalAngle, "Horizontal Angle", rTarget, 0f, 20f))
            {
                lIsDirty = true;
                HorizontalAngle = EditorHelper.FieldFloatValue;
            }

            if (EditorHelper.FloatField(VerticalAngle, "Vertical Angle", rTarget, 0f, 20f))
            {
                lIsDirty = true;
                VerticalAngle = EditorHelper.FieldFloatValue;
            }

            GUILayout.EndHorizontal();

            GUILayout.Space(5f);

            if (EditorHelper.BoolField("Succeed on Expire", "Determines if we'll consider a projectile that expires without a collision a success.", SucceedOnExpire, rTarget))
            {
                lIsDirty = true;
                SucceedOnExpire = EditorHelper.FieldBoolValue;
            }

            return lIsDirty;
        }

#endif

        #endregion
    }
}