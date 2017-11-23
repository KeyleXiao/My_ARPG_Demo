using UnityEngine;
using com.ootii.Actors.AnimationControllers;
using com.ootii.Actors.Combat;
using com.ootii.Actors.LifeCores;
using com.ootii.Geometry;
using com.ootii.Helpers;
using com.ootii.Timing;

namespace com.ootii.MotionControllerPacks
{
    /// <summary>
    /// Provides some basic functions that we use in most of the PMP motions
    /// </summary>
    public abstract class PMP_MotionBase : MotionControllerMotion
    {
        /// <summary>
        /// Angle applied to the aim to make it look like
        /// we're pointing towards the target. Posative value is right
        /// and negative value is left.
        /// </summary>
        public float _HorizontalAimAngle = -4f;
        public float HorizontalAimAngle
        {
            get { return _HorizontalAimAngle; }
            set { _HorizontalAimAngle = value; }
        }

        /// <summary>
        /// Angle applied to the aim to make it look like
        /// we're pointing towards the target. Posative value is down
        /// and negative value is up.
        /// </summary>
        public float _VerticalAimAngle = -6f;
        public float VerticalAimAngle
        {
            get { return _VerticalAimAngle; }
            set { _VerticalAimAngle = value; }
        }

        /// <summary>
        /// Speed at which the anchor rotates to the target
        /// </summary>
        public float _ToTargetRotationSpeed = 360f;
        public float ToTargetRotationSpeed
        {
            get { return _ToTargetRotationSpeed; }
            set { _ToTargetRotationSpeed = value; }
        }

        /// <summary>
        /// Speed at which the camera rotates to the target
        /// </summary>
        public float _ToTargetCameraRotationSpeed = 720f;
        public float ToTargetCameraRotationSpeed
        {
            get { return _ToTargetCameraRotationSpeed; }
            set { _ToTargetCameraRotationSpeed = value; }
        }

        /// <summary>
        /// Combatant that is the character
        /// </summary>
        protected ICombatant mCombatant = null;

        /// <summary>
        /// Determines if we've reached the forward direction and can stop rotating
        /// </summary>
        protected bool mHasReachedForward = false;

        /// <summary>
        /// Default constructor
        /// </summary>
        public PMP_MotionBase()
            : base()
        {
            _Pack = PMP_Idle.GroupName();
        }

        /// <summary>
        /// Controller constructor
        /// </summary>
        /// <param name="rController">Controller the motion belongs to</param>
        public PMP_MotionBase(MotionController rController)
            : base(rController)
        {
            _Pack = PMP_Idle.GroupName();
        }

        /// <summary>
        /// Awake is called after all objects are initialized so you can safely speak to other objects. This is where
        /// reference can be associated.
        /// </summary>
        public override void Awake()
        {
            base.Awake();

            if (Application.isPlaying)
            {
                // Grab the combatant reference
                mCombatant = mMotionController.gameObject.GetComponent<ICombatant>();
            }
        }

        /// <summary>
        /// Updates the motion over time. This is called by the controller
        /// every update cycle so animations and stages can be updated.
        /// </summary>
        /// <param name="rDeltaTime">Time since the last frame (or fixed update call)</param>
        /// <param name="rUpdateIndex">Index of the update to help manage dynamic/fixed updates. [0: Invalid update, >=1: Valid update]</param>
        public override void Update(float rDeltaTime, int rUpdateIndex)
        {
#if UNITY_EDITOR
            if (ShowDebug)
            {
                if (mCombatant != null && mCombatant.Target != null)
                {
                    float lDistance = Vector3.Distance(mMotionController._Transform.position, mCombatant.Target.position);
                    Graphics.GraphicsManager.DrawText("Target: " + lDistance.ToString("f1"), mCombatant.Target.position + (mCombatant.Target.up * 2f), Color.black);
                }
            }
#endif
        }

        /// <summary>
        /// Forces the camera to stay focused on the target
        /// </summary>
        /// <param name="rTarget">Transform we are rotating to</param>
        protected void RotateCameraToTarget(Transform rTarget, float rSpeed = 0)
        {
            if (rTarget == null) { return; }
            if (mMotionController.CameraRig == null) { return; }

            float lSpeed = (rSpeed > 0f ? rSpeed : _ToTargetCameraRotationSpeed);

            Vector3 lNewPosition = mMotionController._Transform.position + (mMotionController._Transform.rotation * mMotionController.RootMotionMovement);
            Vector3 lForward = (rTarget.position - lNewPosition).normalized;
            mMotionController.CameraRig.SetTargetForward(lForward, lSpeed);
        }

        /// <summary>
        /// Rotate to the specified target's position over time
        /// </summary>
        /// <param name="rTarget">Transform we are rotating to</param>
        /// <param name="rSpeed">Degrees per second to rotate</param>
        /// <param name="rDeltaTime">Current delta time</param>
        /// <param name="rRotation">Resulting delta rotation needed to get to the target</param>
        protected void RotateToTarget(Transform rTarget, float rSpeed, float rDeltaTime, ref Quaternion rRotation)
        {
            Vector3 lNewPosition = mMotionController._Transform.position + (mMotionController._Transform.rotation * mMotionController.RootMotionMovement);
            Vector3 lToTarget = rTarget.position - lNewPosition;

            float lSpeed = (rSpeed > 0f ? rSpeed : _ToTargetRotationSpeed);

            float lToAnchorAngle = Vector3Ext.HorizontalAngleTo(mMotionController._Transform.forward, lToTarget.normalized, mMotionController._Transform.up);
            lToAnchorAngle = Mathf.Sign(lToAnchorAngle) * Mathf.Min(lSpeed * rDeltaTime, Mathf.Abs(lToAnchorAngle));

            rRotation = Quaternion.AngleAxis(lToAnchorAngle, Vector3.up);
        }

        /// <summary>
        /// Rotates the actor to the view over time
        /// </summary>
        protected void RotateToTargetForward(Vector3 rTarget, float rSpeed, ref Quaternion rRotation)
        {
            if (mHasReachedForward) { return; }

            // Grab the angle needed to get to our target forward
            Vector3 lTargetForward = rTarget;
            float lAvatarToCamera = NumberHelper.GetHorizontalAngle(mMotionController._Transform.forward, lTargetForward, mMotionController._Transform.up);
            if (lAvatarToCamera == 0f) { return; }

            float lSpeed = (rSpeed > 0f ? rSpeed : _ToTargetRotationSpeed);

            float lInputFromSign = Mathf.Sign(lAvatarToCamera);
            float lInputFromAngle = Mathf.Abs(lAvatarToCamera);
            float lRotationAngle = (lSpeed / 60f) * TimeManager.Relative60FPSDeltaTime;

            // Establish the link if we're close enough
            if (lInputFromAngle <= lRotationAngle)
            {
                lRotationAngle = lInputFromAngle;
                mHasReachedForward = true;
            }

            // Use the information and AC to determine our final rotation
            rRotation = Quaternion.AngleAxis(lInputFromSign * lRotationAngle, mMotionController._Transform.up);
        }

        /// <summary>
        /// Rotates the body to point towards the cross hairs
        /// </summary>
        protected void RotateChestToTargetForward(Vector3 rTarget, float rWeight)
        {
            if (rWeight == 0f) { return; }

            float lHAngle = 0f;
            float lVAngle = 0f;

            Transform lBody = mMotionController._Transform;
            Transform lCamera = mMotionController._CameraTransform;
            Transform lSpine = mMotionController.Animator.GetBoneTransform(HumanBodyBones.Spine);
            Transform lChest = mMotionController.Animator.GetBoneTransform(HumanBodyBones.Chest);

            // The target direction helps determine how we'll rotate the arm
            Vector3 lTargetDirection = (lCamera != null ? lCamera.forward : rTarget);

            lHAngle = _HorizontalAimAngle * rWeight;
            lVAngle = (_VerticalAimAngle + Vector3Ext.HorizontalAngleTo(lBody.forward, lTargetDirection, lBody.right)) * rWeight;
            lVAngle = Mathf.Clamp(lVAngle, -65f, 65f);

            if (lSpine != null && lChest != null) { lHAngle = lHAngle * 0.5f; }
            if (lSpine != null && lChest != null) { lVAngle = lVAngle * 0.5f; }

            if (lSpine != null)
            {
                lSpine.rotation = Quaternion.AngleAxis(lHAngle, lBody.up) * Quaternion.AngleAxis(lVAngle, lBody.right) * lSpine.rotation;
            }

            if (lChest != null)
            {
                lChest.rotation = Quaternion.AngleAxis(lHAngle, lBody.up) * Quaternion.AngleAxis(lVAngle, lBody.right) * lChest.rotation;
            }
        }

        /// <summary>
        /// Draws out the primary weapon debug info
        /// </summary>
        protected void DrawWeaponDebug(ICombatant rCombatant, IWeaponCore rWeapon, ICombatStyle rAttackStyle, Color rColor)
        {
#if UNITY_EDITOR

            if (rCombatant == null) { return; }
            if (rWeapon == null) { return; }

            if (ShowDebug)
            {
                if (rWeapon.HasColliders)
                {
                    WeaponCore lWeaponCore = rWeapon as WeaponCore;
                    if (lWeaponCore != null)
                    {
                        Graphics.GraphicsManager.DrawCollider(lWeaponCore.Collider as BoxCollider, rColor);
                    }
                }
                else if (rAttackStyle != null)
                {
                    float lMinReach = (rAttackStyle.MinRange > 0f ? rAttackStyle.MinRange : rCombatant.MinMeleeReach + rWeapon.MinRange);
                    float lMaxReach = (rAttackStyle.MaxRange > 0f ? rAttackStyle.MaxRange : rCombatant.MaxMeleeReach + rWeapon.MaxRange);

                    Vector3 lCombatOrigin = rCombatant.CombatOrigin;
                    Graphics.GraphicsManager.DrawSolidFrustum(lCombatOrigin, rCombatant.Transform.rotation * Quaternion.LookRotation(rAttackStyle.Forward, rCombatant.Transform.up), rAttackStyle.HorizontalFOA, rAttackStyle.VerticalFOA, lMinReach, lMaxReach, rColor);
                }
            }
#endif
        }

        /// <summary>
        /// Draws out the primary weapon debug info
        /// </summary>
        protected void DrawWeaponDebug(IWeaponCore rWeapon, ICombatStyle rAttackStyle, Color rColor)
        {
#if UNITY_EDITOR
            DrawWeaponDebug(mCombatant, rWeapon, rAttackStyle, rColor);
#endif
        }

        /// <summary>
        /// Draws out the primary weapon debug info
        /// </summary>
        protected void DrawWeaponDebug(IWeaponCore rWeapon, Vector3 rForward, float rHorizontalFOA, float rVerticalFOA, Color rColor)
        {
#if UNITY_EDITOR

            if (rWeapon == null) { return; }

            if (ShowDebug)
            {
                if (mCombatant != null)
                {
                    if (rWeapon.HasColliders)
                    {
                        WeaponCore lWeaponCore = rWeapon as WeaponCore;
                        if (lWeaponCore != null)
                        {
                            Graphics.GraphicsManager.DrawCollider(lWeaponCore.Collider as BoxCollider, rColor);
                        }
                    }
                    else
                    {
                        float lMinReach = mCombatant.MinMeleeReach + rWeapon.MinRange;
                        float lMaxReach = mCombatant.MaxMeleeReach + rWeapon.MaxRange;

                        Vector3 lCombatOrigin = mCombatant.CombatOrigin;
                        Graphics.GraphicsManager.DrawSolidFrustum(lCombatOrigin, mCombatant.Transform.rotation * Quaternion.LookRotation(rForward, mCombatant.Transform.up), rHorizontalFOA, rVerticalFOA, lMinReach, lMaxReach, rColor);
                        //Graphics.GraphicsManager.DrawText(string.Format("Weapon: {0:f1}/{1:f1}", rWeapon.Health, rWeapon.MaxHealth), rWeapon.gameObject.transform.position + (Vector3.up * 0.4f), Color.black);
                    }
                }
            }
#endif
        }
    }
}
