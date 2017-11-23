using System;
using System.Collections.Generic;
using UnityEngine;
using com.ootii.Actors.Combat;
using com.ootii.Geometry;
using com.ootii.Messages;

namespace com.ootii.Actors.LifeCores
{
    /// <summary>
    /// Determines the capabilities of the weapon and provides access to
    /// core specific functionality.
    /// </summary>
    public class WeaponCore : ItemCore, IWeaponCore
    {
        /// <summary>
        /// Owner that we don't want to collide with
        /// </summary>
        public override GameObject Owner
        {
            get { return mOwner; }

            set
            {
                Collider[] lColliders = gameObject.GetComponents<Collider>();

                if (mOwner != null)
                {
                    Collider[] lOwnerColliders = mOwner.GetComponents<Collider>();
                    for (int i = 0; i < lColliders.Length; i++)
                    {
                        for (int j = 0; j < lOwnerColliders.Length; j++)
                        {
                            UnityEngine.Physics.IgnoreCollision(lColliders[i], lOwnerColliders[j], false);
                        }
                    }
                }

                if (value != null)
                {
                    Collider[] lOwnerColliders = value.GetComponents<Collider>();
                    for (int i = 0; i < lColliders.Length; i++)
                    {
                        for (int j = 0; j < lOwnerColliders.Length; j++)
                        {
                            UnityEngine.Physics.IgnoreCollision(lColliders[i], lOwnerColliders[j], true);
                        }
                    }
                }

                mOwner = value;
            }
        }

        /// <summary>
        /// Determine if we're actually swinging
        /// </summary>
        protected bool mIsActive = false;
        public virtual bool IsActive
        {
            get { return mIsActive; }

            set
            {
                mIsActive = value;

                //mAge = 0f;
                mImpactCount = 0;
                //mPositions.Clear();

                Rigidbody lRigidbody = gameObject.GetComponent<Rigidbody>();
                if (lRigidbody != null) { lRigidbody.detectCollisions = mIsActive; }

                // Enable or disable the colliders
                Collider[] lColliders = gameObject.GetComponents<Collider>();
                for (int i = 0; i < lColliders.Length; i++)
                {
                    lColliders[i].enabled = mIsActive;
                }
            }
        }

        /// <summary>
        /// Determines if we're dealing with colliders or not
        /// </summary>
        protected bool mHasColliders = false;
        public bool HasColliders
        {
            get { return mHasColliders; }
        }

        /// <summary>
        /// Collider used by the item
        /// </summary>
        public Collider Collider
        {
            get
            {
                if (mHasColliders)
                {
                    return gameObject.GetComponent<Collider>();
                }

                return null;
            }
        }

        /// <summary>
        /// Minimum range the weapon can apply damage
        /// </summary>
        public float _MinRange = 0f;
        public virtual float MinRange
        {
            get { return _MinRange; }
            set { _MinRange = value; }
        }

        /// <summary>
        /// Maximum range the weapon can apply damage
        /// </summary>
        public float _MaxRange = 1f;
        public virtual float MaxRange
        {
            get { return _MaxRange; }
            set { _MaxRange = value; }
        }

        /// <summary>
        /// Min damage to use on impact
        /// </summary>
        public float _MinDamage = 50f;
        public virtual float MinDamage
        {
            get { return _MinDamage; }
            set { _MinDamage = value; }
        }

        /// <summary>
        /// Max damage to use on impact
        /// </summary>
        public float _MaxDamage = 50f;
        public virtual float MaxDamage
        {
            get { return _MaxDamage; }
            set { _MaxDamage = value; }
        }

        /// <summary>
        /// Min force multiplier to use on impact
        /// </summary>
        public float _MinImpactPower = 1f;
        public virtual float MinImpactPower
        {
            get { return _MinImpactPower; }
            set { _MinImpactPower = value; }
        }

        /// <summary>
        /// Max force multiplier to use on impact
        /// </summary>
        public float _MaxImpactPower = 1f;
        public virtual float MaxImpactPower
        {
            get { return _MaxImpactPower; }
            set { _MaxImpactPower = value; }
        }

        /// <summary>
        /// Attack style being used for the current attack
        /// </summary>
        protected ICombatStyle mAttackStyle = null;
        public virtual ICombatStyle AttackStyle
        {
            get { return mAttackStyle; }
            set { mAttackStyle = value; }
        }

        /// <summary>
        /// Track the last hit that occurred
        /// </summary>
        protected CombatHit mLastHit = CombatHit.EMPTY;
        public CombatHit LastHit
        {
            get { return mLastHit; }
            set { mLastHit = value; }
        }

        /// <summary>
        /// Raised when the weapon actually impacts something
        /// </summary>
        [NonSerialized]
        public LifeCoreDelegate OnImpactEvent = null;

        /// <summary>
        /// Cache the transform for easier access
        /// </summary>
        protected Transform mTransform = null;

        // Used to track the movement of the weapon
        protected Vector3 mLastPosition;

        /// <summary>
        /// Number of impacts that occured this activation
        /// </summary>
        protected int mImpactCount = 0;

        /// <summary>
        /// Called before any updates are called
        /// </summary>
        protected virtual void Start()
        {
            mTransform = gameObject.transform;
            mLastPosition = mTransform.position;

            Collider lCollider = gameObject.GetComponent<Collider>();
            mHasColliders = (lCollider != null);

            // Ensure we're not detecting collisions yet
            IsActive = false;
        }

        /// <summary>
        /// As the projectile moves, check if it collides with anything
        /// </summary>
        protected virtual void LateUpdate()
        {
            if (mIsActive)
            {
                // Track the position over time
                mLastPosition = mTransform.position;
            }
        }

        /// <summary>
        /// Amount of damage to apply this attack
        /// </summary>
        /// <param name="rPercent">Percentage of damage between min and max (use 0 to 1).</param>
        /// <param name="rMultiplier">Multiplier applied to the final damage.</param>
        /// <returns>Amount of damage this attack</returns>
        public virtual float GetAttackDamage(float rPercent = 1f, float rMultiplier = 1f)
        {
            return (_MinDamage + ((_MaxDamage - _MinDamage) * rPercent)) * rMultiplier;
        }

        /// <summary>
        /// Amount of impulse to apply this attack
        /// </summary>
        /// <param name="rPercent">Percentage of impulse between min and max (use 0 to 1).</param>
        /// <param name="rMultiplier">Multiplier applied to the final impulse.</param>
        /// <returns>Amount of impulse this attack</returns>
        public virtual float GetAttackImpactPower(float rPercent = 1f, float rMultiplier = 1f)
        {
            return (_MinImpactPower + ((_MaxImpactPower - _MinImpactPower) * rPercent)) * rMultiplier;
        }

        /// <summary>
        /// Test each of the combatants to determine if an impact occured
        /// </summary>
        /// <param name="rCombatTargets">Targets who we may be impacting</param>
        /// <param name="rAttackStyle">ICombatStyle that details the combat style being used.</param>
        /// <returns>The number of impacts that occurred</returns>
        public virtual int TestImpact(List<CombatTarget> rCombatTargets, ICombatStyle rAttackStyle = null)
        {
            mImpactCount = 0;

            float lMaxReach = 0f;

            if (mOwner != null)
            {
                ICombatant lCombatant = mOwner.GetComponent<ICombatant>();
                if (lCombatant != null) { lMaxReach = lCombatant.MaxMeleeReach; }
            }

            for (int i = 0; i < rCombatTargets.Count; i++)
            {
                CombatTarget lTarget = rCombatTargets[i];
                if (lTarget == CombatTarget.EMPTY) { continue; }

                float lDistance = Vector3.Distance(lTarget.ClosestPoint, mTransform.position);
                if (lDistance > _MaxRange + lMaxReach) { continue; }

                Vector3 lVector = (mTransform.position - mLastPosition).normalized;
                if (lVector.sqrMagnitude == 0 && mOwner != null) { lVector = mOwner.transform.forward; }

                mLastHit.Collider = lTarget.Collider;
                mLastHit.Point = lTarget.ClosestPoint;
                mLastHit.Normal = -lVector;
                mLastHit.Vector = lVector;
                mLastHit.Distance = lTarget.Distance;
                mLastHit.Index = mImpactCount;

                OnImpact(mLastHit, rAttackStyle);
            }

            return mImpactCount;
        }

        /// <summary>
        /// Raised when the impact occurs
        /// </summary>
        /// <param name="rHitInfo">CombatHit structure detailing the hit information.</param>
        /// <param name="rAttackStyle">ICombatStyle that details the combat style being used.</param>
        protected virtual void OnImpact(CombatHit rHitInfo, ICombatStyle rAttackStyle = null)
        {
            // If we get here, there's an impact
            mImpactCount++;

            // Extract out information about the hit
            Transform lHitTransform = GetClosestTransform(rHitInfo.Point, rHitInfo.Collider.transform);
            Vector3 lHitDirection = Quaternion.Inverse(lHitTransform.rotation) * (rHitInfo.Point - lHitTransform.position).normalized;

            // Put together the combat info. This will will be modified over time
            CombatMessage lMessage = CombatMessage.Allocate();
            lMessage.Attacker = mOwner;
            lMessage.Defender = rHitInfo.Collider.gameObject;
            lMessage.Weapon = this;
            lMessage.Damage = GetAttackDamage(1f, (rAttackStyle != null ? rAttackStyle.DamageModifier : 1f));
            lMessage.ImpactPower = GetAttackImpactPower();
            lMessage.HitPoint = rHitInfo.Point;
            lMessage.HitDirection = lHitDirection;
            lMessage.HitVector = rHitInfo.Vector;
            lMessage.HitTransform = lHitTransform;

            // Grab cores for processing
            ActorCore lAttackerCore = (mOwner != null ? mOwner.GetComponent<ActorCore>() : null);
            ActorCore lDefenderCore = rHitInfo.Collider.gameObject.GetComponent<ActorCore>();

            // Pre-Attack
            lMessage.ID = CombatMessage.MSG_ATTACKER_ATTACKED;

            if (lAttackerCore != null)
            {
                lAttackerCore.SendMessage(lMessage);
            }

#if USE_MESSAGE_DISPATCHER || OOTII_MD
            MessageDispatcher.SendMessage(lMessage);
#endif

            // Attack Defender
            lMessage.ID = CombatMessage.MSG_DEFENDER_ATTACKED;

            if (lDefenderCore != null)
            {
                ICombatant lDefenderCombatant = rHitInfo.Collider.gameObject.GetComponent<ICombatant>();
                if (lDefenderCombatant != null)
                {
                    lMessage.HitDirection = Quaternion.Inverse(lDefenderCore.Transform.rotation) * (rHitInfo.Point - lDefenderCombatant.CombatOrigin).normalized;
                }

                lDefenderCore.SendMessage(lMessage);

#if USE_MESSAGE_DISPATCHER || OOTII_MD
                MessageDispatcher.SendMessage(lMessage);
#endif
            }
            else
            {
                lMessage.HitDirection = Quaternion.Inverse(lHitTransform.rotation) * (rHitInfo.Point - lHitTransform.position).normalized;

                IDamageable lDefenderDamageable = rHitInfo.Collider.gameObject.GetComponent<IDamageable>();
                if (lDefenderDamageable != null)
                {
                    lDefenderDamageable.OnDamaged(lMessage);
                }

                Rigidbody lRigidBody = rHitInfo.Collider.gameObject.GetComponent<Rigidbody>();
                if (lRigidBody != null)
                {
                    lRigidBody.AddForceAtPosition(rHitInfo.Vector * lMessage.ImpactPower, rHitInfo.Point, ForceMode.Impulse);
                }
            }

            // Attacker response
            if (lAttackerCore != null)
            {
                lAttackerCore.SendMessage(lMessage);

#if USE_MESSAGE_DISPATCHER || OOTII_MD
                MessageDispatcher.SendMessage(lMessage);
#endif
            }

            // Finish up any impact processing (like sound)
            OnImpactComplete(lMessage);

            // Release the combatant to the pool
            CombatMessage.Release(lMessage);
        }

        /// <summary>
        /// Raised when the weapon hit is verified. This way the weapon can adjust the combat message, play sounds, etc.
        /// </summary>
        /// <param name="rCombatMessage">Message that contains information about the hit</param>
        public virtual void OnImpactComplete(CombatMessage rCombatMessage)
        {
            //com.ootii.Utilities.Debug.Log.FileWrite((mOwner != null ? mOwner.name + "." : "") + "WeaponCore.OnHit()");
        }

        /// <summary>
        /// Starts the recursive function for the closest transform to the specified point
        /// </summary>
        /// <param name="rPosition">Reference point for for the closest transform</param>
        /// <param name="rCollider">Transform that represents the collision</param>
        /// <returns></returns>
        public virtual Transform GetClosestTransform(Vector3 rPosition, Transform rCollider)
        {
            // Find the anchor's root transform
            Transform lActorTransform = rCollider;
            //while (lActorTransform.parent != null) { lActorTransform = lActorTransform.parent; }

            // Grab the closest body transform
            float lMinDistance = float.MaxValue;
            Transform lMinTransform = lActorTransform;
            GetClosestTransform(rPosition, lActorTransform, ref lMinDistance, ref lMinTransform);

            // Return it
            return lMinTransform;
        }

        /// <summary>
        /// Find the closes transform to the hit position. This is what we'll attach the projectile to
        /// </summary>
        /// <param name="rPosition">Hit position</param>
        /// <param name="rTransform">Transform to be tested</param>
        /// <param name="rMinDistance">Current min distance between the hit position and closest transform</param>
        /// <param name="rMinTransform">Closest transform</param>
        protected virtual void GetClosestTransform(Vector3 rPosition, Transform rTransform, ref float rMinDistance, ref Transform rMinTransform)
        {
            // Limit what we'll connect to
            if (rTransform.name.Contains("connector")) { return; }
            if (rTransform.gameObject.GetComponent<IWeaponCore>() != null) { return; }

            // If this transform is closer to the hit position, use it
            float lDistance = Vector3.Distance(rPosition, rTransform.position);
            if (lDistance < rMinDistance)
            {
                rMinDistance = lDistance;
                rMinTransform = rTransform;
            }

            // Check if any child transform is closer to the hit position
            for (int i = 0; i < rTransform.childCount; i++)
            {
                GetClosestTransform(rPosition, rTransform.GetChild(i), ref rMinDistance, ref rMinTransform);
            }
        }

        /// <summary>
        /// Determines if the "descendant" transform is a child (or grand child)
        /// of the "parent" transform.
        /// </summary>
        /// <param name="rParent"></param>
        /// <param name="rTest"></param>
        /// <returns></returns>
        protected bool IsDescendant(Transform rParent, Transform rDescendant)
        {
            if (rParent == null) { return false; }

            Transform lDescendantParent = rDescendant;
            while (lDescendantParent != null)
            {
                if (lDescendantParent == rParent) { return true; }
                lDescendantParent = lDescendantParent.parent;
            }

            return false;
        }

        /// <summary>
        /// Capture Unity's collision event. We use triggers since IsKinematic Rigidbodies don't
        /// raise collisions... only triggers.
        /// </summary>
        protected virtual void OnTriggerEnter(Collider rCollider)
        {
            //com.ootii.Utilities.Debug.Log.FileWrite(transform.name + ".OnTriggerEnter(" + rCollider.name + ")");

            if (!mIsActive) { return; }

            // Ensure we're not hitting ourselves
            if (mOwner != null)
            {
                if (rCollider.gameObject == mOwner) { return; }

                IWeaponCore lWeaponCore = rCollider.gameObject.GetComponent<WeaponCore>();
                if (lWeaponCore != null && lWeaponCore.Owner == mOwner) { return; }

                if (IsDescendant(mOwner.transform, rCollider.transform)) { return; }
            }

            Vector3 lClosestPoint = GeometryExt.ClosestPoint(mLastPosition, rCollider);
            if (lClosestPoint != Vector3Ext.Null)
            {
                Vector3 lVector = (mTransform.position - mLastPosition).normalized;
                if (lVector.sqrMagnitude == 0 && mOwner != null) { lVector = mOwner.transform.forward; }

                mLastHit.Collider = rCollider;
                mLastHit.Point = lClosestPoint;
                mLastHit.Normal = -lVector;
                mLastHit.Vector = lVector;
                mLastHit.Distance = 0f;
                mLastHit.Index = mImpactCount;
                OnImpact(mLastHit, mAttackStyle);
            }
        }

        /// <summary>
        /// Capture Unity's collision event
        /// </summary>
        protected virtual void OnTriggerStay(Collider rCollider)
        {
            //com.ootii.Utilities.Debug.Log.FileWrite(transform.name + ".OnTriggerStay(" + rCollider.name + ")");

            if (!mIsActive) { return; }
        }

        /// <summary>
        /// Capture Unity's collision event
        /// </summary>
        protected virtual void OnTriggerExit(Collider rCollider)
        {
            //com.ootii.Utilities.Debug.Log.FileWrite(transform.name + ".OnTriggerExit(" + rCollider.name + ")");

            if (!mIsActive) { return; }
        }
    }
}
