using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using com.ootii.Actors.AnimationControllers;
using com.ootii.Actors.LifeCores;
using com.ootii.Cameras;
using com.ootii.Input;
using com.ootii.Geometry;
using com.ootii.Graphics;
using com.ootii.Helpers;
using com.ootii.Messages;

namespace com.ootii.Actors.Combat
{
    /// <summary>
    /// Delegates to help customize the combatant
    /// </summary>
    public delegate void CombatantTransformEvent(Combatant rCombatant, Transform rTransform);
    public delegate bool CombatantMotionEvent(Combatant rCombatant, MotionControllerMotion rMotion);
    public delegate void CombatantMessageEvent(Combatant rCombatant, CombatMessage rMessage);

    /// <summary>
    /// Provides combat based information about a specific actor.
    /// </summary>
    public class Combatant : MonoBehaviour, ICombatant
    {
        /// <summary>
        /// This transform. We cache this so we're not actually doing a Get everytime we access 'transform'
        /// </summary>
        [NonSerialized]
        public Transform _Transform = null;
        public Transform Transform
        {
            get { return _Transform; }
        }

        /// <summary>
        /// Transform that our combat origin starts from
        /// </summary>
        public Transform _CombatTransform = null;
        public Transform CombatTransform
        {
            get { return _CombatTransform; }
            set { _CombatTransform = value; }
        }

        /// <summary>
        /// Determines if the transform is used to get the height only
        /// </summary>
        public bool _CombatTransformHeightOnly = true;
        public bool CombatTransformHeightOnly
        {
            get { return _CombatTransformHeightOnly; }
            set { _CombatTransformHeightOnly = value; }
        }

        /// <summary>
        /// The CombatOffset represents the "core" center of the character. Typically this
        /// would be the the chest or shoulders of the character as that's where punching,
        /// swinging, etc. originates.
        /// </summary>
        public Vector3 _CombatOffset = new Vector3(0f, 0f, 0f);
        public Vector3 CombatOffset
        {
            get { return _CombatOffset; }
            set { _CombatOffset = value; }
        }

        /// <summary>
        /// Vector where combat originates from. Typically this is the shoulders or chest area.
        /// </summary>
        public Vector3 CombatOrigin
        {
            get
            {
                Vector3 lOffset = _CombatOffset;
                Transform lTransform = transform;

                if (_CombatTransform != null)
                {
                    if (_CombatTransformHeightOnly)
                    {
                        Vector3 lLocalPosition = lTransform.InverseTransformPoint(_CombatTransform.position);
                        lOffset.y = lOffset.y + lLocalPosition.y;
                    }
                    else
                    {
                        lTransform = _CombatTransform;
                    }
                }

                return lTransform.position + (lTransform.rotation * lOffset);
            }
        }

        /// <summary>
        /// Minimum distance the combatant can reach for melee combat.
        /// </summary>
        public float _MinMeleeReach = 0.1f;
        public float MinMeleeReach
        {
            get { return _MinMeleeReach; }
            set { _MinMeleeReach = value; }
        }

        /// <summary>
        /// Maximum distance the combatant can reach for melee combat (not including weapon length).
        /// </summary>
        public float _MaxMeleeReach = 0.75f;
        public float MaxMeleeReach
        {
            get { return _MaxMeleeReach; }
            set { _MaxMeleeReach = value; }
        }

        /// <summary>
        /// Target the combatant is focusing on
        /// </summary>
        public Transform _Target = null;
        public Transform Target
        {
            get { return _Target; }

            set
            {
                if (_Target != value)
                {
                    if (value == null)
                    {
                        if (_Target != null) { OnTargetUnlocked(_Target); }

                        _Target = null;
                        IsTargetLocked = false;
                    }
                    else
                    {
                        _Target = value;
                        IsTargetLocked = true;

                        OnTargetLocked(_Target);
                    }
                }
            }
        }

        /// <summary>
        /// Determines if the combatant is able to lock onto a target
        /// </summary>
        public bool _IsLockingEnabled = false;
        public bool IsLockingEnabled
        {
            get { return _IsLockingEnabled; }
            set { _IsLockingEnabled = value; }
        }

        /// <summary>
        /// Determines if we're locking onto the combatant
        /// </summary>
        public bool _IsTargetLocked = false;
        public bool IsTargetLocked
        {
            get { return (_IsLockingEnabled && _IsTargetLocked && _Target != null); }
            set { _IsTargetLocked = value; }
        }

        /// <summary>
        /// Locked icon to display on the target
        /// </summary>
        public Texture _TargetLockedIcon = null;
        public Texture TargetLockedIcon
        {
            get { return _TargetLockedIcon; }
            set { _TargetLockedIcon = value; }
        }

        /// <summary>
        /// Action alias to lock onto a target
        /// </summary>
        public string _ToggleCombatantLockAlias = "Combat Lock";
        public string ToggleCombatantLockAlias
        {
            get { return _ToggleCombatantLockAlias; }
            set { _ToggleCombatantLockAlias = value; }
        }

        /// <summary>
        /// The maximum distance we'll check for a lock
        /// </summary>
        public float _MaxLockDistance = 10f;
        public float MaxLockDistance
        {
            get { return _MaxLockDistance; }
            set { _MaxLockDistance = value; }
        }

        /// <summary>
        /// Determines if the lock requires a combatant
        /// </summary>
        public bool _LockRequiresCombatant = true;
        public bool LockRequiresCombatant
        {
            get { return _LockRequiresCombatant; }
            set { _LockRequiresCombatant = value; }
        }

        /// <summary>
        /// Camera mode/motor to activate when locking 
        /// </summary>
        public int _LockCameraMode = -1;
        public int LockCameraMode
        {
            get { return _LockCameraMode; }
            set { _LockCameraMode = value; }
        }

        /// <summary>
        /// Camera mode/motor to activate when unlocking
        /// </summary>
        public int _UnlockCameraMode = -1;
        public int UnlockCameraMode
        {
            get { return _UnlockCameraMode; }
            set { _UnlockCameraMode = value; }
        }

        /// <summary>
        /// Determines if we force the actor to rotate to the target
        /// </summary>
        public bool _ForceActorRotation = false;
        public bool ForceActorRotation
        {
            get { return _ForceActorRotation; }
            set { _ForceActorRotation = value; }
        }

        /// <summary>
        /// Determines if we force the ootii camera to rotate to the target
        /// </summary>
        public bool _ForceCameraRotation = false;
        public bool ForceCameraRotation
        {
            get { return _ForceCameraRotation; }
            set { _ForceCameraRotation = value; }
        }

        /// <summary>
        /// Attack style that is currently ready (or in use).
        /// </summary>
        protected ICombatStyle mCombatStyle = null;
        public ICombatStyle CombatStyle
        {
            get { return mCombatStyle; }
            set { mCombatStyle = value; }
        }

        /// <summary>
        /// Primary weapon being used. We track it here to make it easier for
        /// others to access it
        /// </summary>
        protected IWeaponCore mPrimaryWeapon = null;
        public IWeaponCore PrimaryWeapon
        {
            get { return mPrimaryWeapon; }
            set { mPrimaryWeapon = value; }
        }

        /// <summary>
        /// Secondary weapon being used. We track it here to make it easier for
        /// others to access it
        /// </summary>
        protected IWeaponCore mSecondaryWeapon = null;
        public IWeaponCore SecondaryWeapon
        {
            get { return mSecondaryWeapon; }
            set { mSecondaryWeapon = value; }
        }

        /// <summary>
        /// Comma delimited string of stance IDs that the targeting will work for. An empty string means all.
        /// </summary>
        public string _ActorStances = "11,1,2,8";
        public string ActorStances
        {
            get { return _ActorStances; }

            set
            {
                _ActorStances = value;

                if (_ActorStances.Length == 0)
                {
                    if (mActorStances != null)
                    {
                        mActorStances.Clear();
                    }
                }
                else
                {
                    if (mActorStances == null) { mActorStances = new List<int>(); }
                    mActorStances.Clear();

                    int lState = 0;
                    string[] lStates = _ActorStances.Split(',');
                    for (int i = 0; i < lStates.Length; i++)
                    {
                        if (int.TryParse(lStates[i], out lState))
                        {
                            if (!mActorStances.Contains(lState))
                            {
                                mActorStances.Add(lState);
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Determines if we'll render debug information
        /// </summary>
        public bool _ShowDebug = false;
        public bool ShowDebug
        {
            get { return _ShowDebug; }
            set { _ShowDebug = value; }
        }

        /// <summary>
        /// Event for when a target is locked on
        /// </summary>
        public MessageEvent TargetLockedEvent = null;

        /// <summary>
        /// Event for when a target is unlocked
        /// </summary>
        public MessageEvent TargetUnlockedEvent = null;

        /// <summary>
        /// Callback for when the attack is first initiated
        /// </summary>
        [NonSerialized]
        public CombatantMotionEvent AttackActivated = null;

        /// <summary>
        /// Callback for before the defender responds to the attack
        /// </summary>
        [NonSerialized]
        public CombatantMessageEvent PreAttack = null;

        /// <summary>
        /// Callback for after the defender responds to the attack
        /// </summary>
        [NonSerialized]
        public CombatantMessageEvent PostAttack = null;

        /// <summary>
        /// Callback for when the defender is attacked
        /// </summary>
        [NonSerialized]
        public CombatantMessageEvent Attacked = null;

        /// <summary>
        /// Callback for when a target is locked
        /// </summary>
        [NonSerialized]
        public CombatantTransformEvent TargetLocked = null;

        /// <summary>
        /// Callback for when the target is unlocked event
        /// </summary>
        [NonSerialized]
        public CombatantTransformEvent TargetUnlocked = null;

        /// <summary>
        /// Actor Controller the combatant is tied to.
        /// </summary>
        protected ActorController mActorController = null;

        /// <summary>
        /// Motion Controller the combatant is tied to.
        /// </summary>
        protected MotionController mMotionController = null;

        /// <summary>
        /// Input source associated with the character.
        /// </summary>
        protected IInputSource mInputSource = null;

        /// <summary>
        /// Initial list of prospects that is close to the combatant
        /// </summary>
        protected List<GameObject> mMeleeProspects = new List<GameObject>();

        /// <summary>
        /// Time in seconds to delay before gathering new prospects
        /// </summary>
        protected float mMeleeProspectDelay = 0.25f;

        /// <summary>
        /// Used to track the last time we've gathered prospects
        /// </summary>
        protected float mMeleeProspectTime = 0f;

        /// <summary>
        /// Actor stances we'll check to see if we can transition
        /// </summary>
        protected List<int> mActorStances = null;

        /// <summary>
        /// Determines if the character's rotation is locked to the direction
        /// </summary>
        protected bool mIsRotationLocked = false;

        /// <summary>
        /// Once the objects are instanciated, awake is called before start. Use it
        /// to setup references to other objects
        /// </summary>
        protected void Start()
        {
            _Transform = transform;

            ActorStances = _ActorStances;

            mActorController = gameObject.GetComponent<ActorController>();

            mMotionController = gameObject.GetComponent<MotionController>();
            if (mMotionController != null) { mInputSource = mMotionController._InputSource; }

            // Register this combatant with the camera
            if (mMotionController != null && mMotionController.CameraRig is BaseCameraRig)
            {
                ((BaseCameraRig)mMotionController.CameraRig).OnPostLateUpdate -= OnCameraUpdated;
                ((BaseCameraRig)mMotionController.CameraRig).OnPostLateUpdate += OnCameraUpdated;
            }

            if (ShowDebug) { CombatManager.ShowDebug = true; }
        }

        /// <summary>
        /// This function is called when the behaviour becomes disabled () or inactive.
        /// </summary>
        protected void OnDisable()
        {
            // Unregister this combatant with the camera
            if (mMotionController != null && mMotionController.CameraRig is BaseCameraRig)
            {
                ((BaseCameraRig)mMotionController.CameraRig).OnPostLateUpdate -= OnCameraUpdated;
            }
        }

        /// <summary>
        /// Update is called every frame, if the MonoBehaviour is enabled.
        /// </summary>
        protected virtual void Update()
        {
            if (mMotionController == null) { return; }

            if (mInputSource != null)
            {
                if (_IsLockingEnabled && _ToggleCombatantLockAlias.Length > 0)
                {
                    if (mInputSource.IsJustPressed(_ToggleCombatantLockAlias))
                    {
                        if (IsTargetLocked)
                        {
                            Target = null;
                        }
                        else
                        {
                            if (mActorStances == null || mActorStances.Count == 0 || mActorStances.Contains(mMotionController.Stance))
                            {
                                Target = FindTarget();
                            }
                        }
                    }
                }
            }

            // Unlock the target if our stance isn't valid
            if (_IsLockingEnabled && IsTargetLocked)
            {
                // Ensure our target is alive and able to be targeted
                if (_Target != null)
                {
                    ActorCore lTargetActorCore = _Target.GetComponent<ActorCore>();
                    if (lTargetActorCore != null && !lTargetActorCore.IsAlive)
                    {
                        IsTargetLocked = false;
                        OnTargetUnlocked(_Target);
                    }
                }

                // Ensure we're in a stance where targeting is valid
                if (mActorStances != null && mActorStances.Count > 0 && !mActorStances.Contains(mMotionController.Stance))
                {
                    IsTargetLocked = false;
                    OnTargetUnlocked(_Target);
                }

                // Finally, force the rotations as needed
                if (IsTargetLocked)
                {
                    if (_ForceActorRotation) { RotateActorToTarget(_Target, 360f); }
                    if (_ForceCameraRotation && mMotionController.CameraRig == null) { RotateCameraToTarget(_Target, 360f); }
                }
            }
        }

        /// <summary>
        /// Renders any UI to the screen
        /// </summary>
        protected virtual void OnGUI()
        {
            if (_IsLockingEnabled && IsTargetLocked)
            {
                DrawTargetIcon();
            }
        }

        /// <summary>
        /// Attempt to find a target that we can focus on. This approach uses a spiralling raycast
        /// </summary>
        /// <returns></returns>
        public virtual Transform FindTarget(string rTag = null)
        {
            if (mMotionController == null) { return null; }

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

            Transform lTarget = null;

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
                Vector3 lDirection = (mMotionController.CameraTransform.TransformPoint(lPosition) - mMotionController.CameraTransform.position).normalized;
                if (RaycastExt.SafeRaycast(mMotionController.CameraTransform.position, lDirection, out lHitInfo, _MaxLockDistance, -1, _Transform))
                {
                    // Grab the gameobject this collider belongs to
                    GameObject lGameObject = lHitInfo.collider.gameObject;

                    // Don't count the ignore
                    if (lGameObject.transform == mMotionController.CameraTransform) { continue; }
                    if (lHitInfo.collider is TerrainCollider) { continue; }

                    // Determine if the combatant has the appropriate tag
                    if (rTag != null && rTag.Length > 0)
                    {
                        if (lGameObject.CompareTag(rTag))
                        {
                            lTarget = lGameObject.transform;
                            break;
                        }
                    }

                    // We only care about combatants we'll enage with
                    ICombatant lCombatant = lGameObject.GetComponent<ICombatant>();
                    if (lCombatant != null)
                    {
                        lTarget = lGameObject.transform;
                        break;
                    }

                    // We can do a catch-all if a combatant isn't required
                    if (lTarget == null && !_LockRequiresCombatant)
                    {
                        lTarget = lGameObject.transform;
                    }
                }

                // Increment the spiral
                lAngle += lDegreesPerStep;
                lRadius = Mathf.Min(lRadius + lRadiusPerStep, lMaxRadius);

                //lColor.r = lColor.r - lColorPerStep;
                //lColor.g = lColor.g - lColorPerStep;
            }

            // Return the target hit
            return lTarget;
        }

        /// <summary>
        /// Grab all the combat targets that could be affected by the style and weapon
        /// </summary>
        /// <param name="rStyle">AttackStyle that defines the field-of-attack</param>
        /// <param name="rWeapon">Weapon that is currently being used</param>
        /// <param name="rCombatTargets">List of CombatTargets we will fill with the results</param>
        /// <param name="rIgnore">Combatant to ignore. Typically this is the character asking for the query.</param>
        /// <returns></returns>
        public virtual int QueryCombatTargets(AttackStyle rAttackStyle, IWeaponCore rWeapon, List<CombatTarget> rCombatTargets, Transform rIgnore)
        {
            IWeaponCore lWeapon = (rWeapon != null ? rWeapon : mPrimaryWeapon);

            CombatFilter lFilter = new CombatFilter(rAttackStyle);
            lFilter.MinDistance = (rAttackStyle.MinRange > 0f ? rAttackStyle.MinRange : _MinMeleeReach + lWeapon.MinRange);
            lFilter.MaxDistance = (rAttackStyle.MaxRange > 0f ? rAttackStyle.MaxRange : _MaxMeleeReach + lWeapon.MaxRange);

            rCombatTargets.Clear();
            int lTargetCount = CombatManager.QueryCombatTargets(_Transform, CombatOrigin, lFilter, rCombatTargets, rIgnore);

            return lTargetCount;
        }

        /// <summary>
        /// Grab all the combat targets that could be affected by the style and the primary weapon
        /// </summary>
        /// <param name="rStyle">AttackStyle that defines the field-of-attack</param>
        /// <param name="rCombatTargets">List of CombatTargets we will fill with the results</param>
        /// <returns></returns>
        public virtual int QueryCombatTargets(AttackStyle rStyle, List<CombatTarget> rCombatTargets)
        {
            return QueryCombatTargets(rStyle, null, rCombatTargets, _Transform);
        }

        /// <summary>
        /// Allows the combatant a chance to modify the motion before it is fully activated.
        /// </summary>
        /// <param name="rMotion">Motion that is being activated</param>
        /// <returns>Boolean used to determine if the motion should continue activation.</returns>
        public virtual bool OnAttackActivated(MotionControllerMotion rMotion)
        {
            //com.ootii.Utilities.Debug.Log.FileWrite(_Transform.name + ".OnAttackActivated(" + rMotion.GetType().Name + ")");

            if (AttackActivated != null) { return AttackActivated(this, rMotion); }

            return true;
        }

        /// <summary>
        /// Occurs prior to the defender receiving the attack notification. This
        /// allows the attacker to augment the attack if needed.
        /// </summary>
        /// <param name="rRound"></param>
        public virtual void OnPreAttack(CombatMessage rMessage)
        {
            //com.ootii.Utilities.Debug.Log.FileWrite(_Transform.name + ".OnPreAttack()");

            if (PreAttack != null) { PreAttack(this, rMessage); }

            if (mMotionController != null) { mMotionController.SendMessage(rMessage); }
        }

        /// <summary>
        /// Occurs after the defender receives the attack notification. This
        /// allows the attacker to react to the defender if needed.
        /// </summary>
        /// <param name="rRound"></param>
        public virtual void OnPostAttack(CombatMessage rMessage)
        {
            //com.ootii.Utilities.Debug.Log.FileWrite(_Transform.name + ".OnPostAttack()");

            if (PostAttack != null) { PostAttack(this, rMessage); }

            if (mMotionController != null) { mMotionController.SendMessage(rMessage); }

            // If the defender was killed, release the target
            if (_Target != null && rMessage.ID == CombatMessage.MSG_DEFENDER_KILLED)
            {
                IsTargetLocked = false;
                OnTargetUnlocked(_Target);
            }
        }

        /// <summary>
        /// Notifies the defender that they are attacked.
        /// </summary>
        /// <param name="rRound"></param>
        public virtual void OnAttacked(CombatMessage rMessage)
        {
            //com.ootii.Utilities.Debug.Log.FileWrite(_Transform.name + ".OnAttacked()");

            if (Attacked != null) { Attacked(this, rMessage); }

            if (rMessage.ID != CombatMessage.MSG_DEFENDER_ATTACKED) { return; }

            ActorCore lDefenderCore = gameObject.GetComponent<ActorCore>();
            if (lDefenderCore != null)
            {
                lDefenderCore.SendMessage(rMessage);
            }
            else
            {
                // Check if this defender is blocking, parrying, etc
                if (mMotionController != null) { mMotionController.SendMessage(rMessage); }

                // Determine if we're continuing with the attack and apply damage
                if (rMessage.ID == CombatMessage.MSG_DEFENDER_ATTACKED)
                {
                    IDamageable lDamageable = gameObject.GetComponent<IDamageable>();
                    if (lDamageable != null)
                    {
                        lDamageable.OnDamaged(rMessage);
                    }
                    else
                    {
                        rMessage.ID = CombatMessage.MSG_DEFENDER_DAMAGED;
                        if (mMotionController != null) { mMotionController.SendMessage(rMessage); }
                    }
                }

                // Disable this combatant
                if (rMessage.ID == CombatMessage.MSG_DEFENDER_KILLED)
                {
                    this.enabled = false;
                }
            }
        }

        /// <summary>
        /// Raised when we lock onto a target
        /// </summary>
        protected virtual void OnTargetLocked(Transform rTransform)
        {
            if (_LockCameraMode >= 0)
            {
                if (mMotionController != null && mMotionController.CameraRig != null)
                {
                    mMotionController.CameraRig.Mode = _LockCameraMode;
                }
            }

            if (TargetLocked != null) { TargetLocked(this, rTransform); }

            // Send the message
            CombatMessage lMessage = CombatMessage.Allocate();
            lMessage.ID = EnumMessageID.MSG_COMBAT_ATTACKER_TARGET_LOCKED;
            lMessage.Attacker = gameObject;
            lMessage.Defender = _Target.gameObject;

            if (TargetLockedEvent != null)
            {
                TargetLockedEvent.Invoke(lMessage);
            }

#if USE_MESSAGE_DISPATCHER || OOTII_MD
            MessageDispatcher.SendMessage(lMessage);
#endif

            CombatMessage.Release(lMessage);
        }

        /// <summary>
        /// Raised when we release the lock on a target
        /// </summary>
        protected virtual void OnTargetUnlocked(Transform rTransform)
        {
            if (_UnlockCameraMode >= 0)
            {
                if (mMotionController != null && mMotionController.CameraRig != null)
                {
                    mMotionController.CameraRig.Mode = _UnlockCameraMode;
                }
            }

            if (TargetUnlocked != null) { TargetUnlocked(this, rTransform); }

            // Send the message
            CombatMessage lMessage = CombatMessage.Allocate();
            lMessage.ID = EnumMessageID.MSG_COMBAT_ATTACKER_TARGET_UNLOCKED;
            lMessage.Attacker = gameObject;
            lMessage.Defender = rTransform.gameObject;

            if (TargetUnlockedEvent != null)
            {
                TargetUnlockedEvent.Invoke(lMessage);
            }

#if USE_MESSAGE_DISPATCHER || OOTII_MD
            MessageDispatcher.SendMessage(lMessage);
#endif

            CombatMessage.Release(lMessage);
        }

        /// <summary>
        /// Create a rotation velocity that rotates the character based on input
        /// </summary>
        /// <param name="rInputFromAvatarAngle"></param>
        /// <param name="rDeltaTime"></param>
        protected virtual void RotateActorToTarget(Transform rTarget, float rSpeed)
        {
            // Get the forward looking direction
            Vector3 lForward = (rTarget.position - mActorController._Transform.position).normalized;

            // We do the inverse tilt so we calculate the rotation in "natural up" space vs. "actor up" space. 
            Quaternion lInvTilt = QuaternionExt.FromToRotation(mActorController._Transform.up, Vector3.up);

            // Character's forward direction of the actor in "natural up"
            Vector3 lActorForward = lInvTilt * mActorController._Transform.forward;

            // Target forward in "natural up"
            Vector3 lTargetForward = lInvTilt * lForward;

            // Ensure we don't exceed our rotation speed
            float lActorToTargetAngle = NumberHelper.GetHorizontalAngle(lActorForward, lTargetForward);
            if (rSpeed > 0f && Mathf.Abs(lActorToTargetAngle) > rSpeed * Time.deltaTime)
            {
                lActorToTargetAngle = Mathf.Sign(lActorToTargetAngle) * rSpeed * Time.deltaTime;
            }

            // Add the rotation to our character
            Quaternion lRotation = Quaternion.AngleAxis(lActorToTargetAngle, Vector3.up);

            if (mActorController._UseTransformPosition && mActorController._UseTransformRotation)
            {
                _Transform.rotation = _Transform.rotation * lRotation;
            }
            else
            {
                mActorController.Rotate(lRotation, Quaternion.identity);
            }
        }

        /// <summary>
        /// Forces the camera to stay focused on the target
        /// </summary>
        /// <param name="rTarget">Transform we are rotating to</param>
        protected void RotateCameraToTarget(Transform rTarget, float rSpeed = 0)
        {
            if (rTarget == null) { return; }
            if (mMotionController == null) { return; }

            float lSpeed = (rSpeed > 0f ? rSpeed : 360f);

            if (mMotionController.CameraRig != null)
            {
                Vector3 lTargetPosition = rTarget.position;

                Combatant lTargetCombatant = rTarget.GetComponent<Combatant>();
                if (lTargetCombatant != null) { lTargetPosition = lTargetCombatant.CombatOrigin; }

                Vector3 lForward = (lTargetPosition - mMotionController.CameraRig.Transform.position).normalized;
                mMotionController.CameraRig.SetTargetForward(lForward, lSpeed);
            }
            else if (mMotionController._CameraTransform != null)
            {
                Vector3 lNewPosition = mMotionController._Transform.position + (mMotionController._Transform.rotation * mMotionController.RootMotionMovement);
                Vector3 lForward = (rTarget.position - lNewPosition).normalized;
                mMotionController._CameraTransform.rotation = Quaternion.LookRotation(lForward, mMotionController._CameraTransform.up);
            }
        }

        /// <summary>
        /// When we want to rotate based on the camera direction (which input does), we need to tweak the actor
        /// rotation AFTER we process the camera. Otherwise, we can get small stutters during camera rotation. 
        /// 
        /// This is the only way to keep them totally in sync. It also means we can't run any of our AC processing
        /// as the AC already ran. So, we do minimal work here
        /// </summary>
        /// <param name="rDeltaTime"></param>
        /// <param name="rUpdateCount"></param>
        /// <param name="rCamera"></param>
        private void OnCameraUpdated(float rDeltaTime, int rUpdateIndex, BaseCameraRig rCamera)
        {
            if (!_ForceCameraRotation) { return; }
            if (!IsTargetLocked) { return; }

            float lSpeed = 360f;
            Vector3 lTargetPosition = _Target.position;

            Combatant lTargetCombatant = _Target.GetComponent<Combatant>();
            if (lTargetCombatant != null) { lTargetPosition = lTargetCombatant.CombatOrigin; }

            Vector3 lForward = (lTargetPosition - mMotionController.CameraRig.Transform.position).normalized;
            mMotionController.CameraRig.SetTargetForward(lForward, lSpeed);
        }

        /// <summary>
        /// Renders the combat icon to the screen
        /// </summary>
        private void DrawTargetIcon()
        {
            if (!_IsLockingEnabled || !IsTargetLocked) { return; }
            if (_TargetLockedIcon == null) { return; }

            Vector3 lPosition = Vector3.zero;

            Combatant lCombatant = _Target.GetComponent<Combatant>();
            if (lCombatant != null)
            {
                lPosition = lCombatant.CombatOrigin;
            }
            else
            {
                lPosition = _Target.position + (_Target.up * 1.6f);
            }

            GraphicsManager.DrawTexture(_TargetLockedIcon, lPosition, 32f, 32f);
        }

        #region Editor Functions

        // **************************************************************************************************
        // Following properties and function only valid while editing
        // **************************************************************************************************

#if UNITY_EDITOR

        /// <summary>
        /// Show the events in the editor
        /// </summary>
        public bool EditorShowEvents = false;

        /// <summary>
        /// Reset is called when the user hits the Reset button in the Inspector's context menu or when adding the component the first time. This function is only called in editor mode.
        /// </summary>
        void Reset()
        {
            if (_CombatTransform == null)
            {
                Animator lAnimator = gameObject.GetComponent<Animator>();
                if (lAnimator != null)
                {
                    _CombatTransform = lAnimator.GetBoneTransform(HumanBodyBones.Chest);
                    _CombatOffset = new Vector3(0f, 0.25f, 0f);
                }
            }

            if (_CombatTransform == null)
            {
                _CombatOffset = new Vector3(0f, 1.4f, 0f);
            }
        }

#endif

        #endregion

    }
}
