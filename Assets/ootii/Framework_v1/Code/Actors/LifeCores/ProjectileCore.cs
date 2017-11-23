//#define OOTII_DEBUG
using System;
using System.Collections.Generic;
using UnityEngine;
using com.ootii.Actors.Combat;
using com.ootii.Collections;
using com.ootii.Geometry;
using com.ootii.Helpers;

#if OOTII_DEBUG
using com.ootii.Graphics;
#endif

namespace com.ootii.Actors.LifeCores
{
    /// <summary>
    /// A life cycle is the heart-beat of the projectile. This code is used to manage
    /// the movement of the projectile and the impact.
    /// </summary>
    public class ProjectileCore : ItemCore, IProjectileCore
    {
        /// <summary>
        /// Floating point error constant
        /// </summary>
        public const float EPSILON = 0.001f;

        // Color names that could be used in materials
        private static string[] MATERIAL_COLORS = new string[] { "_Color", "_MainColor", "_TintColor", "_EmissionColor", "_BorderColor", "_ReflectColor", "_RimColor", "_CoreColor" };

        /// <summary>
        /// Cache the transform for use
        /// </summary>
        [NonSerialized]
        public Transform _Transform = null;
        public Transform Transform
        {
            get { return _Transform; }
            set { _Transform = value; }
        }

        /// <summary>
        /// Determine if we're actually launched
        /// </summary>
        protected bool mIsActive = false;
        public bool IsActive
        {
            get { return mIsActive; }
            set { mIsActive = value; }
        }

        /// <summary>
        /// Determines if we're dealing with colliders or not
        /// </summary>
        public virtual bool HasColliders
        {
            get { return false; }
        }

        /// <summary>
        /// Maximum age to live before the impact
        /// </summary>
        public float _MaxAge = 5f;
        public float MaxAge
        {
            get { return _MaxAge; }
            set { _MaxAge = value; }
        }

        /// <summary>
        /// Max age to live after the impact
        /// </summary>
        public float _MaxImpactAge = 0f;
        public float MaxImpactAge
        {
            get { return _MaxImpactAge; }
            set { _MaxImpactAge = value; }
        }

        /// <summary>
        /// Age of the life core so we know when to expire it
        /// </summary>
        protected float mAge = 0f;
        public virtual float Age
        {
            get { return mAge; }
            set { mAge = value; }
        }

        /// <summary>
        /// Minimum Distance the projectile can succeed
        /// </summary>
        public float _MinRange = 0f;
        public float MinRange
        {
            get { return _MinRange; }
            set { _MinRange = value; }
        }

        /// <summary>
        /// Maximum Distance the projectile can succeed
        /// </summary>
        public float _MaxRange = 5f;
        public float MaxRange
        {
            get { return _MaxRange; }
            set { _MaxRange = value; }
        }

        /// <summary>
        /// Min damage to use on impact
        /// </summary>
        public float _MinDamage = 50f;
        public float MinDamage
        {
            get { return _MinDamage; }
            set { _MinDamage = value; }
        }

        /// <summary>
        /// Max damage to use on impact
        /// </summary>
        public float _MaxDamage = 50f;
        public float MaxDamage
        {
            get { return _MaxDamage; }
            set { _MaxDamage = value; }
        }

        /// <summary>
        /// Min force multiplier to use on impact
        /// </summary>
        public float _MinImpactPower = 1f;
        public float MinImpactPower
        {
            get { return _MinImpactPower; }
            set { _MinImpactPower = value; }
        }

        /// <summary>
        /// Max force multiplier to use on impact
        /// </summary>
        public float _MaxImpactPower = 1f;
        public float MaxImpactPower
        {
            get { return _MaxImpactPower; }
            set { _MaxImpactPower = value; }
        }

        /// <summary>
        /// Determines if the projectile is a homing projectile that will
        /// move towards the target
        /// </summary>
        protected bool _IsHoming = false;
        public bool IsHoming
        {
            get { return _IsHoming; }
            set { _IsHoming = value; }
        }

        /// <summary>
        /// Speed that the projectile moves (when not using a rigidbody)
        /// </summary>
        public float _Speed = 0f;
        public float Speed
        {
            get { return _Speed; }
            set { _Speed = value; }
        }

        /// <summary>
        /// Target a homing projectile moves towards.
        /// </summary>
        protected Transform mTarget = null;
        public Transform Target
        {
            get { return mTarget; }
            set { mTarget = value; }
        }

        /// <summary>
        /// Offset from the target transform's position
        /// </summary>
        protected Vector3 mTargetOffset = Vector3.zero;
        public Vector3 TargetOffset
        {
            get { return mTargetOffset; }
            set { mTargetOffset = value; }
        }

        /// <summary>
        /// Determines if we use raycasting for collision texts (instead of colliders)
        /// </summary>
        public bool _UseRaycast = true;
        public bool UseRaycast
        {
            get { return _UseRaycast; }
            set { _UseRaycast = value; }
        }

        /// <summary>
        /// Determines if the projectile will stick around after impact
        /// </summary>
        public bool _EmbedOnImpact = false;
        public bool EmbedOnImpact
        {
            get { return _EmbedOnImpact; }
            set { _EmbedOnImpact = value; }
        }

        /// <summary>
        /// Amount to embed the arrow after impact
        /// </summary>
        public float _EmbedDistance = 0.1f;
        public float EmbedDistance
        {
            get { return _EmbedDistance; }
            set { _EmbedDistance = value; }
        }

        /// <summary>
        /// GameObject that holds launch effects
        /// </summary>
        public GameObject _LaunchRoot = null;
        public GameObject LaunchRoot
        {
            get { return _LaunchRoot; }
            set { _LaunchRoot = value; }
        }

        /// <summary>
        /// GameObject that holds fly effects
        /// </summary>
        public GameObject _FlyRoot = null;
        public GameObject FlyRoot
        {
            get { return _FlyRoot; }
            set { _FlyRoot = value; }
        }

        /// <summary>
        /// GameObject that holds impact effects
        /// </summary>
        public GameObject _ImpactRoot = null;
        public GameObject ImpactRoot
        {
            get { return _ImpactRoot; }
            set { _ImpactRoot = value; }
        }

        /// <summary>
        /// Speed at which we change the projector's alpha
        /// </summary>
        public float _AudioFadeInSpeed = 0f;
        public float AudioFadeInSpeed
        {
            get { return _AudioFadeInSpeed; }
            set { _AudioFadeInSpeed = value; }
        }

        /// <summary>
        /// Speed at which we change the projector's alpha
        /// </summary>
        public float _AudioFadeOutSpeed = 1f;
        public float AudioFadeOutSpeed
        {
            get { return _AudioFadeOutSpeed; }
            set { _AudioFadeOutSpeed = value; }
        }

        /// <summary>
        /// Speed at which we change the projector's alpha
        /// </summary>
        public float _LightFadeInSpeed = 0f;
        public float LightFadeInSpeed
        {
            get { return _LightFadeInSpeed; }
            set { _LightFadeInSpeed = value; }
        }

        /// <summary>
        /// Speed at which we change the projector's alpha
        /// </summary>
        public float _LightFadeOutSpeed = 1f;
        public float LightFadeOutSpeed
        {
            get { return _LightFadeOutSpeed; }
            set { _LightFadeOutSpeed = value; }
        }

        /// <summary>
        /// Speed at which we change the projector's alpha
        /// </summary>
        public float _ProjectorFadeInSpeed = 0f;
        public float ProjectorFadeInSpeed
        {
            get { return _ProjectorFadeInSpeed; }
            set { _ProjectorFadeInSpeed = value; }
        }

        /// <summary>
        /// Speed at which we change the projector's alpha
        /// </summary>
        public float _ProjectorFadeOutSpeed = 1f;
        public float ProjectorFadeOutSpeed
        {
            get { return _ProjectorFadeOutSpeed; }
            set { _ProjectorFadeOutSpeed = value; }
        }

        /// <summary>
        /// Callback to be notified when the particle is destroyed
        /// </summary>
        [NonSerialized]
        public LifeCoreDelegate OnReleasedEvent = null;

        /// <summary>
        /// Event for when a collider enters the trigger area
        /// </summary>
        [NonSerialized]
        public LifeCoreDelegate OnImpactEvent = null;

        /// <summary>
        /// Event for when a collider stays in the trigger area
        /// </summary>
        [NonSerialized]
        public LifeCoreDelegate OnExpiredEvent = null;

        // Track the instance of the launch effects
        protected GameObject mLaunchInstance = null;
        public GameObject LaunchInstance
        {
            get { return mLaunchInstance; }
        }

        // Track the instance of the flight effects
        protected GameObject mFlyInstance = null;
        public GameObject FlyInstance
        {
            get { return mFlyInstance; }
        }

        // Track the instance of the impact effects
        protected GameObject mImpactInstance = null;
        public GameObject ImpactInstance
        {
            get { return mImpactInstance; }
        }

        // Rigidbody associated with the projectile
        protected Rigidbody mRigidbody = null;

        // Determines if the projectile has reported its expire
        protected bool mHasExpired = false;

        // Determines if we're shutting down
        protected bool mIsShuttingDown = false;

        // Store the initial position at launch
        protected Vector3 mLaunchPosition = Vector3.zero;

        // Store the path so we determine the trajectory
        protected Vector3 mLastPosition = Vector3.zero;

        // Number of impacts that occured this activation
        protected int mImpactCount = 0;

        // Track the last hit that occurred
        protected CombatHit mLastImpact = CombatHit.EMPTY;

        /// <summary>
        /// Awake is called when the script instance is being loaded.
        /// </summary>
        public virtual void Awake()
        {
            _Transform = gameObject.transform;

            // Extract the rigidbody
            mRigidbody = gameObject.GetComponent<Rigidbody>();

            // Add twice so we can create a trajectory from the start
            mLastPosition = _Transform.position;
        }

        /// <summary>
        /// Sends the projectile flying
        /// </summary>
        public virtual void Launch(float rHAngle = 0f, float rVAngle = 0f, float rForce = 0f)
        {
            mAge = 0f;
            mIsActive = true;
            mHasExpired = false;
            mIsShuttingDown = false;

            mImpactCount = 0;
            mLastImpact = CombatHit.EMPTY;

            // Apply any rotation adjustment
            Quaternion lRotation = Quaternion.AngleAxis(rHAngle, _Transform.up) * Quaternion.AngleAxis(rVAngle, _Transform.right);
            _Transform.rotation = lRotation * _Transform.rotation;

            // Set the launch particles
            mLaunchPosition = _Transform.position;

            if (_LaunchRoot != null)
            {
                mLaunchInstance = GameObject.Instantiate(_LaunchRoot);
                mLaunchInstance.transform.position = _Transform.position;
                mLaunchInstance.transform.rotation = _Transform.rotation;

                StartEffects(mLaunchInstance);
            }

            if (_FlyRoot != null)
            {
                mFlyInstance = GameObject.Instantiate(_FlyRoot);
                mFlyInstance.transform.parent = _Transform;
                mFlyInstance.transform.localRotation = Quaternion.identity;
                mFlyInstance.transform.localPosition = Vector3.zero;

                StartEffects(mFlyInstance);
            }

            // Add twice so we can create a trajectory from the start
            mLastPosition = _Transform.position;

            // If we're dealing with a rigidbody, add the force
            if (mRigidbody != null)
            {
                mRigidbody.useGravity = true;
                mRigidbody.isKinematic = false;

                if (rForce == 0f) { rForce = _Speed; }

                Vector3 lForce = _Transform.forward * rForce;
                mRigidbody.AddForce(lForce);
            }
        }

        /// <summary>
        /// Tells the projectile to terminate itself. However, we want to allow the
        /// particles to finish out before we actually release.
        /// </summary>
        public virtual void Stop()
        {
            if (mIsShuttingDown) { return; }
            mIsShuttingDown = true;

            mIsActive = false;
        }

        /// <summary>
        /// Releases the game object back to the pool (if allocated) or simply destroys if it not.
        /// </summary>
        public virtual void Release()
        {
            if (OnReleasedEvent != null) { OnReleasedEvent(this, null); }

            if (mLaunchInstance != null) { GameObject.Destroy(mLaunchInstance); }
            if (mFlyInstance != null) { GameObject.Destroy(mFlyInstance); }
            if (mImpactInstance != null) { GameObject.Destroy(mImpactInstance); }

            Transform lParent = gameObject.transform.parent;
            if (lParent != null && lParent.name.Contains(" connector", StringComparison.OrdinalIgnoreCase))
            {
                GameObject.Destroy(lParent.gameObject);
            }
            else
            {
                GameObject.Destroy(gameObject);
            }

            mAge = 0f;
            mIsActive = false;
            mHasExpired = false;
            mIsShuttingDown = false;
            OnReleasedEvent = null;
            mTarget = null;
        }

        /// <summary>
        /// As the projectile moves, check if it collides with anything
        /// </summary>
        protected virtual void Update()
        {
            mAge = mAge + Time.deltaTime;

            // Arrow is flying
            if (mIsActive)
            {
                Vector3 lMovement = Vector3.zero;
                Vector3 lMovementDirection = Vector3.zero;
                float lMovementDistance = 0f;

                // If we're moving based on velocity...
                if (mRigidbody == null)
                {
                    // If homing, we need to adjust the direction of movement
                    if (_IsHoming && mTarget != null)
                    {
                        Vector3 lTargetPosition = mTarget.position + (mTarget.rotation * mTargetOffset);

                        lMovementDirection = (lTargetPosition - _Transform.position).normalized;
                        _Transform.forward = lMovement;
                    }
                    // Otherwise, we just move forward
                    else
                    {
                        lMovementDirection = _Transform.forward;
                    }

                    lMovementDistance = _Speed * Time.deltaTime;
                    lMovement = lMovementDirection * lMovementDistance;
                    transform.position = _Transform.position + lMovement;
                }
                // We must be moving based on physics...
                else
                {
                    lMovement = _Transform.position - mLastPosition;
                    lMovementDistance = lMovement.magnitude;
                    lMovementDirection = (lMovementDistance > 0 ? lMovement.normalized : _Transform.forward);

                    // Ensure our rotation fits our trajectory
                    _Transform.forward = lMovementDirection;
                }

                // Check if we're predicting (or have) an impact
                if (_UseRaycast || mRigidbody == null)
                {
                    TestImpact(mLastPosition, lMovementDirection, lMovementDistance);
                }

                // Store our final info
                mLastPosition = _Transform.position;
            }

            // Determine if the arrow has expired
            if (mImpactCount == 0)
            {
                if (!mHasExpired && (_MaxAge > 0f && mAge > _MaxAge))
                {
                    OnExpired();
                    mHasExpired = true;
                }

                float lDistance = Vector3.Distance(_Transform.position, mLaunchPosition);
                if (!mHasExpired && _MaxRange > 0f && lDistance >= _MaxRange)
                {
                    OnExpired();
                    mHasExpired = true;
                }
            }
            else
            {
                if (!mHasExpired && (mAge > _MaxImpactAge))
                {
                    OnExpired();
                    mHasExpired = true;
                }
            }

            // Get rid of the arrow if we exceed the max age
            if (mImpactCount > 0 || mHasExpired)
            {
                Stop();
            }

            // If we we're shuttind down, test if we can release
            bool lAreLaunchParticlesAlive = UpdateEffects(mLaunchInstance, mIsShuttingDown);
            bool lAreFlyParticlesAlive = UpdateEffects(mFlyInstance, mIsShuttingDown);
            bool lAreImpactParticlesAlive = UpdateEffects(mImpactInstance, mIsShuttingDown);

            if (mIsShuttingDown && !lAreLaunchParticlesAlive && !lAreFlyParticlesAlive && !lAreImpactParticlesAlive)
            {
                if (mAge > _MaxImpactAge)
                {
                    Release();
                }
            }
        }

        /// <summary>
        /// Test if we have an impact. If we have a prediction factor an impact may not occur, but we 
        /// will store the prediction.
        /// </summary>
        /// <param name="rMovementDirection"></param>
        /// <param name="rMovementDistance"></param>
        /// <param name="rPrecictFactor"></param>
        /// <returns></returns>
        protected virtual bool TestImpact(Vector3 rStartPosition, Vector3 rMovementDirection, float rMovementDistance, float rPredictFactor = 2f)
        {
#if OOTII_DEBUG

            Debug.DrawLine(rStartPosition, rStartPosition + (rMovementDirection * Mathf.Max(rMovementDistance * rPredictFactor, 0.1f)), Color.red);

#endif

            // Test for a hit
            RaycastHit lRaycastHit;
            if (RaycastExt.SafeRaycast(rStartPosition, rMovementDirection, out lRaycastHit, Mathf.Max(rMovementDistance * rPredictFactor, 0.1f), -1, _Transform))
            {
                // Ensure we're not hitting another projectile
                if (lRaycastHit.collider.gameObject.GetComponent<IProjectileCore>() != null) { return false; }

                // This is really a predictive hit ans we extended the Distance a bit
                Collider lHitCollider = lRaycastHit.collider;
                Vector3 lHitPoint = lRaycastHit.point + (rMovementDirection * _EmbedDistance);
                Transform lHitTransform = GetClosestTransform(lHitPoint, lHitCollider.transform);

                Vector3 lHitPosition = lHitTransform.InverseTransformPoint(lHitPoint);
                float lDistance = Vector3.Distance(lHitPosition, mLaunchPosition);
                if (lDistance < _MinRange)
                {
                    mHasExpired = true;
                    //mSpellAction.OnFailure();

                    return false;
                }

#if OOTII_DEBUG

                mWorldHitPoint = lHitPoint;
                GraphicsManager.DrawLine(rStartPosition, rStartPosition + (rMovementDirection * Mathf.Max(rMovementDistance * rPredictFactor, 0.1f)), Color.red);

#endif

                // If we're within the actual Distance, we will consider this a real hit
                if (lRaycastHit.distance - rMovementDistance <= 0f)
                {
                    mLastImpact.Collider = lHitCollider;
                    mLastImpact.Point = lHitPoint;
                    mLastImpact.Normal = lRaycastHit.normal;
                    mLastImpact.Vector = rMovementDirection;
                    mLastImpact.Distance = lRaycastHit.distance;
                    mLastImpact.Index = mImpactCount;

                    OnImpact(mLastImpact);

                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Raised when the impact occurs
        /// </summary>
        /// <param name="rHitInfo">RaycastHit data about the hit itself</param>
        /// <param name="rClosestTransform">Closets transform to the hit position</param>
        /// <param name="rLocalPosition">Position of the hit relative to the closest transform</param>
        /// <param name="rLocalForward">Direction the hit came from relative to the closest transform</param>
        protected virtual void OnImpact(CombatHit rHitInfo)
        {
            mAge = 0f;
            mIsActive = false;

            mImpactCount++;

            ClearPhysics();

            // Stop the flight particles
            StopEffects(mLaunchInstance);
            StopEffects(mFlyInstance);

            // Play the particles
            if (_ImpactRoot != null)
            {
                mImpactInstance = GameObject.Instantiate(_ImpactRoot);
                mImpactInstance.transform.position = rHitInfo.Point;
                mImpactInstance.transform.rotation = Quaternion.LookRotation(rHitInfo.Normal, Vector3.up);

                StartEffects(mImpactInstance);
            }

            //// Combatant that is attacking
            //ICombatant lDefender = null;

            //// Determine who we're colliding with
            //lDefender = rHitInfo.Collider.gameObject.GetComponent<ICombatant>();
            //IDamageable lHitActorCore = rHitInfo.Collider.gameObject.GetComponent<IDamageable>();

            //if (lDefender == null)
            //{
            //    IWeaponCore lWeaponCore = rHitInfo.Collider.gameObject.GetComponent<IWeaponCore>();
            //    if (lWeaponCore != null)
            //    {
            //        lDefender = lWeaponCore.Owner.GetComponent<ICombatant>();
            //        if (lHitActorCore == null) { lHitActorCore = lWeaponCore.Owner.GetComponent<IDamageable>(); }
            //    }
            //}

            // Save the hit information
            Transform lHitTransform = GetClosestTransform(rHitInfo.Point, rHitInfo.Collider.transform);
            //Vector3 lCombatCenter = lHitTransform.position;
            //Vector3 lHitDirection = Vector3.zero;

            //if (lDefender != null)
            //{
            //    //lHitDirection = Quaternion.Inverse(lDefender.Transform.rotation) * (rHitInfo.Point - lDefender.CombatOrigin).normalized;

            //}
            //else
            //{
            //    //lHitDirection = Quaternion.Inverse(lHitTransform.rotation) * (rHitInfo.Point - lCombatCenter).normalized;
            //}

            // Determine if the projectile sticks around after impact
            if (_EmbedOnImpact)
            {
                // To compensate for scaling, we use a "connector". This way,
                // the projectile doesn't scale even if the target does
                Vector3 lLocalScale = lHitTransform.lossyScale;
                lLocalScale.x = 1f / Mathf.Max(Mathf.Abs(lLocalScale.x), 0.0001f);
                lLocalScale.y = 1f / Mathf.Max(Mathf.Abs(lLocalScale.y), 0.0001f);
                lLocalScale.z = 1f / Mathf.Max(Mathf.Abs(lLocalScale.z), 0.0001f);

                GameObject lConnector = new GameObject();
                lConnector.name = name + " connector";
                lConnector.transform.parent = lHitTransform;
                lConnector.transform.localScale = lLocalScale;
                lConnector.transform.localPosition = lHitTransform.InverseTransformPoint(rHitInfo.Point);
                lConnector.transform.localRotation = Quaternion.identity;

                _Transform.parent = lConnector.transform;
                _Transform.localScale = Vector3.one;
                _Transform.localPosition = Vector3.zero;
                _Transform.forward = rHitInfo.Vector;
            }

            // Raise the impact event 
            //mSpellAction.OnSuccess();
            if (OnImpactEvent != null) { OnImpactEvent(this, rHitInfo); }
        }

        /// <summary>
        /// Start the effects based on the fade out speed
        /// </summary>
        /// <param name="rInstance"></param>
        protected virtual void StartEffects(GameObject rInstance)
        {
            if (rInstance == null) { return; }

            // Ensure particles are running
            ParticleSystem[] lParticleSystems = rInstance.GetComponentsInChildren<ParticleSystem>();
            for (int i = 0; i < lParticleSystems.Length; i++)
            {
                if (!lParticleSystems[i].IsAlive(true))
                {
                    lParticleSystems[i].Play(true);
                }
            }

            // Check if sounds are alive
            AudioSource[] lAudioSources = rInstance.GetComponentsInChildren<AudioSource>();
            for (int i = 0; i < lAudioSources.Length; i++)
            {
                if (!lAudioSources[i].isPlaying && AudioFadeInSpeed <= 0f)
                {
                    lAudioSources[i].Play();
                }
            }

            // Check if lights are alive
            Light[] lLights = rInstance.GetComponentsInChildren<Light>();
            for (int i = 0; i < lLights.Length; i++)
            {
                if (lLights[i].intensity == 0f && LightFadeInSpeed <= 0f)
                {
                    lLights[i].intensity = 1f;
                }
            }

            // Ensure projectors are running
            Projector[] lProjectors = rInstance.GetComponentsInChildren<Projector>();
            for (int i = 0; i < lProjectors.Length; i++)
            {
                if (lProjectors[i].material.HasProperty("_Alpha"))
                {
                    float lAlpha = lProjectors[i].material.GetFloat("_Alpha");
                    if (lAlpha == 0f && ProjectorFadeOutSpeed <= 0f)
                    {
                        lProjectors[i].material.SetFloat("_Alpha", 1f);
                    }
                }

                Material lMaterial = lProjectors[i].material;
                for (int j = 0; j < MATERIAL_COLORS.Length; j++)
                {
                    if (lMaterial.HasProperty(MATERIAL_COLORS[j]))
                    {
                        Color lColor = lMaterial.GetColor(MATERIAL_COLORS[j]);
                        if (lColor.a == 0f && ProjectorFadeOutSpeed <= 0f)
                        {
                            lColor.a = 1f;
                            lProjectors[i].material.SetColor(MATERIAL_COLORS[j], lColor);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Stops the effects based on the fade out speed
        /// </summary>
        /// <param name="rInstance"></param>
        protected virtual void StopEffects(GameObject rInstance)
        {
            if (rInstance == null) { return; }

            // Ensure particles are running
            ParticleSystem[] lParticleSystems = rInstance.GetComponentsInChildren<ParticleSystem>();
            for (int i = 0; i < lParticleSystems.Length; i++)
            {
                if (lParticleSystems[i].IsAlive(true))
                {
#if UNITY_5_0 || UNITY_5_1 || UNITY_5_2 || UNITY_5_3 || UNITY_5_4
                    lParticleSystems[i].Stop(true);
#else
                    lParticleSystems[i].Stop(true, ParticleSystemStopBehavior.StopEmitting);
#endif
                }
            }

            // Check if sounds are alive
            AudioSource[] lAudioSources = rInstance.GetComponentsInChildren<AudioSource>();
            for (int i = 0; i < lAudioSources.Length; i++)
            {
                if (lAudioSources[i].isPlaying && AudioFadeOutSpeed <= 0f)
                {
                    lAudioSources[i].Stop();
                }
            }

            // Check if lights are alive
            Light[] lLights = rInstance.GetComponentsInChildren<Light>();
            for (int i = 0; i < lLights.Length; i++)
            {
                if (lLights[i].intensity > 0f && LightFadeOutSpeed <= 0f)
                {
                    lLights[i].intensity = 0f;
                }
            }

            // Ensure projectors are running
            Projector[] lProjectors = rInstance.GetComponentsInChildren<Projector>();
            for (int i = 0; i < lProjectors.Length; i++)
            {
                if (lProjectors[i].material.HasProperty("_Alpha"))
                {
                    float lAlpha = lProjectors[i].material.GetFloat("_Alpha");
                    if (lAlpha > 0f && ProjectorFadeOutSpeed <= 0f)
                    {
                        lProjectors[i].material.SetFloat("_Alpha", 0f);
                    }
                }

                Material lMaterial = lProjectors[i].material;
                for (int j = 0; j < MATERIAL_COLORS.Length; j++)
                {
                    if (lMaterial.HasProperty(MATERIAL_COLORS[j]))
                    {
                        Color lColor = lMaterial.GetColor(MATERIAL_COLORS[j]);
                        if (lColor.a > 0f && ProjectorFadeOutSpeed <= 0f)
                        {
                            lColor.a = 0f;
                            lProjectors[i].material.SetColor(MATERIAL_COLORS[j], lColor);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Update the effects and fade them in and out as needed
        /// </summary>
        /// <param name="rInstance">Instance we're processing</param>
        /// <param name="rShutDown">Determines if we're shutting down or not</param>
        /// <returns></returns>
        protected bool UpdateEffects(GameObject rInstance, bool rShutDown)
        {
            if (rInstance == null) { return false; }

            bool lIsAlive = false;

            if (!rShutDown)
            {
                // Ensure particles are running
                ParticleSystem[] lParticleSystems = rInstance.GetComponentsInChildren<ParticleSystem>();
                for (int i = 0; i < lParticleSystems.Length; i++)
                {
                    if (lParticleSystems[i].IsAlive(true))
                    {
                        lIsAlive = true;
                    }
                }

                // Check if sounds are alive
                if (AudioFadeInSpeed > 0f)
                {
                    AudioSource[] lAudioSources = rInstance.GetComponentsInChildren<AudioSource>();
                    for (int i = 0; i < lAudioSources.Length; i++)
                    {
                        if (lAudioSources[i].isPlaying)
                        {
                            lIsAlive = true;

                            if (lAudioSources[i].volume < 1f)
                            {
                                lAudioSources[i].volume = Mathf.Clamp01(lAudioSources[i].volume - (AudioFadeInSpeed * Time.deltaTime));
                            }
                        }
                    }
                }

                // Check if lights are alive
                if (LightFadeInSpeed > 0f)
                {
                    Light[] lLights = rInstance.GetComponentsInChildren<Light>();
                    for (int i = 0; i < lLights.Length; i++)
                    {
                        lIsAlive = true;

                        if (lLights[i].intensity < 1f)
                        {
                            lLights[i].intensity = Mathf.Clamp01(lLights[i].intensity + (LightFadeInSpeed * Time.deltaTime));
                        }
                    }
                }

                // Ensure projectors are running
                if (ProjectorFadeInSpeed > 0f)
                {
                    Projector[] lProjectors = rInstance.GetComponentsInChildren<Projector>();
                    for (int i = 0; i < lProjectors.Length; i++)
                    {
                        if (lProjectors[i].material.HasProperty("_Alpha"))
                        {
                            float lAlpha = lProjectors[i].material.GetFloat("_Alpha");
                            if (lAlpha < 1f)
                            {
                                lAlpha = Mathf.Clamp01(lAlpha + (ProjectorFadeInSpeed * Time.deltaTime));
                                lProjectors[i].material.SetFloat("_Alpha", lAlpha);
                            }
                        }

                        Material lMaterial = lProjectors[i].material;
                        for (int j = 0; j < MATERIAL_COLORS.Length; j++)
                        {
                            if (lMaterial.HasProperty(MATERIAL_COLORS[j]))
                            {
                                lIsAlive = true;

                                Color lColor = lMaterial.GetColor(MATERIAL_COLORS[j]);
                                if (lColor.a < 1f)
                                {
                                    lColor.a = Mathf.Clamp01(lColor.a + (ProjectorFadeInSpeed * Time.deltaTime));
                                    lProjectors[i].material.SetColor(MATERIAL_COLORS[j], lColor);
                                }
                            }
                        }
                    }
                }
            }
            else
            {
                // Ensure particles are running
                ParticleSystem[] lParticleSystems = rInstance.GetComponentsInChildren<ParticleSystem>();
                for (int i = 0; i < lParticleSystems.Length; i++)
                {
                    if (lParticleSystems[i].IsAlive(true))
                    {
                        lIsAlive = true;
                    }
                }

                // Check if sounds are alive
                AudioSource[] lAudioSources = rInstance.GetComponentsInChildren<AudioSource>();
                for (int i = 0; i < lAudioSources.Length; i++)
                {
                    if (lAudioSources[i].isPlaying && lAudioSources[i].volume > 0f)
                    {
                        if (AudioFadeOutSpeed <= 0f)
                        {
                            lAudioSources[i].volume = 0f;
                        }
                        else
                        {
                            lAudioSources[i].volume = Mathf.Clamp01(lAudioSources[i].volume - (AudioFadeOutSpeed * Time.deltaTime));
                        }

                        if (lAudioSources[i].volume > 0f) { lIsAlive = true; }
                    }
                }

                // Check if lights are alive
                Light[] lLights = rInstance.GetComponentsInChildren<Light>();
                for (int i = 0; i < lLights.Length; i++)
                {
                    if (lLights[i].intensity > 0f)
                    {
                        if (LightFadeOutSpeed <= 0f)
                        {
                            lLights[i].intensity = 0f;
                        }
                        else
                        {
                            lLights[i].intensity = Mathf.Clamp01(lLights[i].intensity - (LightFadeOutSpeed * Time.deltaTime));
                        }

                        if (lLights[i].intensity > 0f) { lIsAlive = true; }
                    }
                }

                // Ensure projectors are running
                Projector[] lProjectors = rInstance.GetComponentsInChildren<Projector>();
                for (int i = 0; i < lProjectors.Length; i++)
                {
                    if (lProjectors[i].material.HasProperty("_Alpha"))
                    {
                        float lAlpha = lProjectors[i].material.GetFloat("_Alpha");
                        if (lAlpha > 0f)
                        {
                            if (ProjectorFadeOutSpeed <= 0f)
                            {
                                lAlpha = 0f;
                                lProjectors[i].material.SetFloat("_Alpha", lAlpha);
                            }
                            else
                            {
                                lAlpha = Mathf.Clamp01(lAlpha - (ProjectorFadeOutSpeed * Time.deltaTime));
                                lProjectors[i].material.SetFloat("_Alpha", lAlpha);
                            }

                            if (lAlpha > 0f) { lIsAlive = true; }
                        }
                    }

                    Material lMaterial = lProjectors[i].material;
                    for (int j = 0; j < MATERIAL_COLORS.Length; j++)
                    {
                        if (lMaterial.HasProperty(MATERIAL_COLORS[j]))
                        {
                            Color lColor = lMaterial.GetColor(MATERIAL_COLORS[j]);
                            if (lColor.a > 0f)
                            {
                                if (ProjectorFadeOutSpeed <= 0f)
                                {
                                    lColor.a = 0f;
                                    lProjectors[i].material.SetColor(MATERIAL_COLORS[j], lColor);
                                }
                                else
                                {
                                    lColor.a = Mathf.Clamp01(lColor.a - (ProjectorFadeOutSpeed * Time.deltaTime));
                                    lProjectors[i].material.SetColor(MATERIAL_COLORS[j], lColor);
                                }

                                if (lColor.a > 0f) { lIsAlive = true; }
                            }
                        }
                    }
                }
            }

            return lIsAlive;
        }

        /// <summary>
        /// Raised when the projectile expires without an impact
        /// </summary>
        protected virtual void OnExpired()
        {
            StopEffects(mLaunchInstance);
            StopEffects(mFlyInstance);

            if (OnExpiredEvent != null) { OnExpiredEvent(this, null); }
        }

        /// <summary>
        /// Clear the physics values associated with the projectile
        /// </summary>
        protected virtual void ClearPhysics()
        {
            if (mRigidbody != null)
            {
                mRigidbody.velocity = Vector3.zero;
                mRigidbody.isKinematic = true;
                mRigidbody.useGravity = false;
                mRigidbody.constraints = RigidbodyConstraints.FreezeAll;
            }

            Collider lProjectileCollider = gameObject.GetComponent<Collider>();
            if (lProjectileCollider != null) { lProjectileCollider.enabled = false; }
        }

        /// <summary>
        /// Starts the recursive function for the closest transform to the specified point
        /// </summary>
        /// <param name="rPosition">Reference point for for the closest transform</param>
        /// <param name="rCollider">Transform that represents the collision</param>
        /// <returns></returns>
        protected virtual Transform GetClosestTransform(Vector3 rPosition, Transform rCollider)
        {
            // Find the anchor's root transform
            Transform lActorTransform = rCollider;

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
            if (!rTransform.gameObject.activeInHierarchy) { return; }
            if (rTransform.name.Contains(" connector", StringComparison.OrdinalIgnoreCase)) { return; }
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
        /// Test each of the combatants to determine if an impact occured
        /// </summary>
        /// <param name="rCombatant">Combatant that is holding the weapon</param>
        /// <param name="rCombatTargets">Targets who we may be impacting</param>
        /// <returns>The number of impacts that occurred</returns>
        public int TestImpact(ICombatant rCombatant, List<CombatTarget> rCombatTargets) { return 0; }

        /// <summary>
        /// Raised when the weapon actually hits. This is typically used for playing sounds or effects.
        /// </summary>
        /// <param name="rCombatMessage">Message that contains information about the hit</param>
        public void OnImpactComplete(CombatMessage rCombatMessage) { }
    }
}
