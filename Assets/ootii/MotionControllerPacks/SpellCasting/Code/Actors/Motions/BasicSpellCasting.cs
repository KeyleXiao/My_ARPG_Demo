using System;
using System.Collections.Generic;
using UnityEngine;
using com.ootii.Actors.AnimationControllers;
using com.ootii.Actors.Combat;
using com.ootii.Actors.Magic;
using com.ootii.Actors.LifeCores;
using com.ootii.Data.Serializers;
using com.ootii.Helpers;
using com.ootii.Geometry;
using com.ootii.Messages;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditorInternal;
#endif

namespace com.ootii.MotionControllerPacks
{
    /// <summary>
    /// Basic motion for a sword attack
    /// </summary>
    [MotionName("Basic Spell Casting")]
    [MotionDescription("Basic spell casting using Mixamo's Pro Magic Pack animations.")]
    public class BasicSpellCasting : PMP_MotionBase
    {
        /// <summary>
        /// Preallocates string for the event tests
        /// </summary>
        public static string EVENT_BEGIN_CAST = "begincast";
        public static string EVENT_PAUSE_CAST = "pausecast";
        public static string EVENT_CAST_SPELL = "castspell";
        public static string EVENT_END_CAST = "endcast";

        /// <summary>
        /// Trigger values for th emotion
        /// </summary>
        public const int PHASE_UNKNOWN = 0;
        public const int PHASE_START = 32140;
        public const int PHASE_STANDING_START = 32141;
        public const int PHASE_CANCEL = 32143;
        public const int PHASE_CONTINUE = 32144;
        public const int PHASE_INTERRUPT = 32145;
        public const int PHASE_RECOVER = 32149;

        /// <summary>
        /// Determines if we're using the IsInMotion() function to verify that
        /// the transition in the animator has occurred for this motion.
        /// </summary>
        public override bool VerifyTransition
        {
            get { return false; }
        }

        /// <summary>
        /// Determines if spells can only be cast while in the Spell Casting stance
        /// </summary>
        public bool _RequiresStance = true;
        public bool RequiresStance
        {
            get { return _RequiresStance; }
            set { _RequiresStance = value; }
        }

        /// <summary>
        /// Determines if we'll rotate to the camera's forward during the cast
        /// </summary>
        public bool _RotateToCameraForward = true;
        public bool RotateToCameraForward
        {
            get { return _RotateToCameraForward; }
            set { _RotateToCameraForward = value; }
        }

        /// <summary>
        /// Desired degrees of rotation per second
        /// </summary>
        public float _RotationSpeed = 360f;
        public float RotationSpeed
        {
            get { return _RotationSpeed; }

            set
            {
                _RotationSpeed = value;
                mDegreesPer60FPSTick = _RotationSpeed / 60f;
            }
        }

        /// <summary>
        /// Determines if we release from the camera instead of the bow
        /// </summary>
        public bool _ReleaseFromCamera = false;
        public bool ReleaseFromCamera
        {
            get { return _ReleaseFromCamera; }
            set { _ReleaseFromCamera = value; }
        }

        /// <summary>
        /// Distance from the camera that we release
        /// </summary>
        public float _ReleaseDistance = 3f;
        public float ReleaseDistance
        {
            get { return _ReleaseDistance; }
            set { _ReleaseDistance = value; }
        }

        /// <summary>
        /// Spell that the actor is actually casting (or will cast)
        /// </summary>
        protected Spell mSpellInstance = null;

        [SerializationIgnore]
        public Spell SpellInstance
        {
            get { return mSpellInstance; }
            set { mSpellInstance = value; }
        }

        /// <summary>
        /// Spells that the actor can cast
        /// </summary>
        protected SpellInventory mSpellInventory = null;

        [SerializationIgnore]
        public SpellInventory SpellInventory
        {
            get { return mSpellInventory; }
            set { mSpellInventory = value; }
        }

        /// <summary>
        /// Index of the spell to be cast. -1 Will leave the spell up to the Spell Inventory default.
        /// </summary>
        protected int mSpellIndex = -1;
        public int SpellIndex
        {
            get { return mSpellIndex; }
            set { mSpellIndex = value; }
        }

        /// <summary>
        /// Rotation target we're heading to
        /// </summary>
        protected Vector3 mTargetForward = Vector3.zero;

        /// <summary>
        /// Keeps us from having to re-allocate
        /// </summary>
        protected List<CombatTarget> mCombatTargets = new List<CombatTarget>();

        /// <summary>
        /// Determine if we were in the TRAVERSAL stance
        /// </summary>
        private bool mWasTraversal = true;
        public bool WasTraversal
        {
            get { return mWasTraversal; }
        }

        /// <summary>
        /// Determines if we actually cast the spell
        /// </summary>
        protected bool mHasCast = false;

        /// <summary>
        /// Determines the current IK blending 
        /// </summary>
        protected float mIKWeight = 0f;

        /// <summary>
        /// Determines if the attack has been interrupted or not
        /// </summary>
        protected bool mIsInterrupted = false;

        /// <summary>
        /// Determines if we've responded to the interruption
        /// </summary>
        protected bool mIsInterruptedReported = false;

        // Track the speed of the animator when we paused it
        protected float mStoredAnimatorSpeed = 0f;

        /// <summary>
        /// Speed we'll actually apply to the rotation. This is essencially the
        /// number of degrees per tick assuming we're running at 60 FPS
        /// </summary>
        protected float mDegreesPer60FPSTick = 1f;

        /// <summary>
        /// Default constructor
        /// </summary>
        public BasicSpellCasting()
            : base()
        {
            _Pack = SpellCastingPackDefinition.PackName;
            _Category = EnumMotionCategories.SPELL_CASTING;

            _Priority = 16;
            _ActionAlias = "";

            if (_EditorAnimatorSMName.Length == 0) { _EditorAnimatorSMName = "BasicSpellCasting-SM"; }
        }

        /// <summary>
        /// Controller constructor
        /// </summary>
        /// <param name="rController">Controller the motion belongs to</param>
        public BasicSpellCasting(MotionController rController)
            : base(rController)
        {
            _Pack = SpellCastingPackDefinition.PackName;
            _Category = EnumMotionCategories.SPELL_CASTING;

            _Priority = 16;
            _ActionAlias = "";

            if (_EditorAnimatorSMName.Length == 0) { _EditorAnimatorSMName = "BasicSpellCasting-SM"; }
        }

        /// <summary>
        /// Awake is called after all objects are initialized so you can safely speak to other objects. This is where
        /// reference can be associated.
        /// </summary>
        public override void Awake()
        {
            base.Awake();

            // Default the speed we'll use to rotate
            mDegreesPer60FPSTick = _RotationSpeed / 60f;

            // Grab the spell inventory if it exists
            if (Application.isPlaying)
            {
                mSpellInventory = mMotionController.gameObject.GetComponent<SpellInventory>();
            }
        }

        /// <summary>
        /// Tests if this motion should be started. However, the motion
        /// isn't actually started.
        /// </summary>
        /// <returns></returns>
        public override bool TestActivate()
        {
            if (!mIsStartable) { return false; }

            if (mSpellInventory == null) { return false; }

            if (!mActorController.IsGrounded) { return false; }

            // This is only valid if we're in combat mode
            if (_RequiresStance && mActorController.State.Stance != EnumControllerStance.SPELL_CASTING)
            {
                return false;
            }

            // Check if we've been activated
            if (_ActionAlias.Length > 0 && mMotionController._InputSource != null)
            {
                if (mMotionController._InputSource.IsJustPressed(_ActionAlias))
                {
                    mParameter = 0;
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Tests if the motion should continue. If it shouldn't, the motion
        /// is typically disabled
        /// </summary>
        /// <returns></returns>
        public override bool TestUpdate()
        {
            if (mIsActivatedFrame) { return true; }

            // This is only valid if we're in combat mode
            if (_RequiresStance && mActorController.State.Stance != EnumControllerStance.SPELL_CASTING)
            {
                return false;
            }

            // Get out when we are back at the idle
            if (mMotionLayer._AnimatorStateID == GetStateID("Stand Idle Out")) // STATE_StandIdleOut)
            {
                return false;
            }

            if (mMotionLayer._AnimatorStateID == GetStateID("Spell Idle Out")) // STATE_SpellIdleOut)
            {
                if (mActorController.State.Stance == EnumControllerStance.TRAVERSAL)
                {
                    mMotionController.SetAnimatorMotionPhase(mMotionLayer._AnimatorLayerIndex, PHASE_RECOVER);
                }
                else
                {
                    return false;
                }
            }
            
            //// Ensure we're actually in our animation state
            //if (mIsAnimatorActive)
            //{
            //    if (!IsInMotionState)
            //    {
            //        return false;
            //    }
            //}

            return true;
        }

        /// <summary>
        /// Called to start the specific motion. If the motion
        /// were something like 'jump', this would start the jumping process
        /// </summary>
        /// <param name="rPrevMotion">Motion that this motion is taking over from</param>
        public override bool Activate(MotionControllerMotion rPrevMotion)
        {
            // Ensure we're not holding onto an old spell
            mSpellInstance = null;
            mStoredAnimatorSpeed = 0f;

            // Default the spell
            int lSpellIndex = mSpellIndex;

            // If a spell was specified (through the parameter), use it
            if (mParameter > 0)
            {
                lSpellIndex = mParameter;
            }

            // Allow the spell to be modified before the attack occurs
            MagicMessage lMessage = MagicMessage.Allocate();
            lMessage.ID = MagicMessage.MSG_MAGIC_PRE_CAST;
            lMessage.Caster = mMotionController.gameObject;
            lMessage.SpellIndex = lSpellIndex;
            lMessage.CastingMotion = this;
            lMessage.Data = this;

            IActorCore lActorCore = mMotionController.gameObject.GetComponent<ActorCore>();
            if (lActorCore != null)
            {
                lActorCore.SendMessage(lMessage);
                lSpellIndex = lMessage.SpellIndex;
            }

#if USE_MESSAGE_DISPATCHER || OOTII_MD
            MessageDispatcher.SendMessage(lMessage);
            lSpellIndex = lMessage.SpellIndex;
#endif

            MagicMessage.Release(lMessage);

            // Ensure we know what spell to cast
            if (mSpellInventory != null)
            {
                mSpellInstance = mSpellInventory.InstantiateSpell(lSpellIndex);
            }

            if (mSpellInstance == null) { return false; }

            // Allow the casting
            mIKWeight = 0f;
            mHasCast = false;
            mIsInterrupted = false;
            mIsInterruptedReported = false;
            mHasReachedForward = false;
            mWasTraversal = (mActorController.State.Stance == EnumControllerStance.TRAVERSAL);

            // Rotate towards this target. If there's no camera, we'll 
            // assume we're dealing with an NPC
            if (mMotionController._CameraTransform != null)
            {
                mTargetForward = mMotionController._CameraTransform.forward;
            }

            //// Ensure our combatant is setup
            //if (mCombatant != null)
            //{
            //    if (!mCombatant.OnAttackActivated(this))
            //    {
            //        return false;
            //    }
            //}
            
            // Run the approapriate cast
            int lMotionPhase = (mWasTraversal ? PHASE_STANDING_START : PHASE_START);
            mMotionController.SetAnimatorMotionPhase(mMotionLayer.AnimatorLayerIndex, lMotionPhase, mSpellInstance.CastingStyle, 0, true);

            // Now, activate the motion
            return base.Activate(rPrevMotion);
        }

        /// <summary>
        /// Called to interrupt the motion if it is currently active. This
        /// gives the motion a chance to stop itself how it sees fit. The motion
        /// may simply ignore the call.
        /// </summary>
        /// <param name="rParameter">Any value you wish to pass</param>
        /// <returns>Boolean determining if the motion accepts the interruption. It doesn't mean it will deactivate.</returns>
        public override bool Interrupt(object rParameter)
        {
            if (mIsActive && !mIsInterrupted)
            {
                mIsInterrupted = true;

                return true;
            }

            return false;
        }

        /// <summary>
        /// Raised when we shut the motion down
        /// </summary>
        public override void Deactivate()
        {
            // Reset the animator speed
            if (mStoredAnimatorSpeed > 0f) { mMotionController.Animator.speed = mStoredAnimatorSpeed; }

            // Clear out the spell we cast
            mSpellInstance = null;

            // Don't hold onto our targets
            mCombatTargets.Clear();

            // Continue
            base.Deactivate();
        }

        /// <summary>
        /// Allows the motion to modify the velocity before it is applied.
        /// </summary>
        /// <param name="rDeltaTime">Time since the last frame (or fixed update call)</param>
        /// <param name="rUpdateIndex">Index of the update to help manage dynamic/fixed updates. [0: Invalid update, >=1: Valid update]</param>
        /// <param name="rMovement">Amount of movement caused by root motion this frame</param>
        /// <param name="rRotation">Amount of rotation caused by root motion this frame</param>
        /// <returns></returns>
        public override void UpdateRootMotion(float rDeltaTime, int rUpdateIndex, ref Vector3 rMovement, ref Quaternion rRotation)
        {
            rMovement = Vector3.zero;
            rRotation = Quaternion.identity;
        }

        /// <summary>
        /// Updates the motion over time. This is called by the controller
        /// every update cycle so animations and stages can be updated.
        /// </summary>
        /// <param name="rDeltaTime">Time since the last frame (or fixed update call)</param>
        /// <param name="rUpdateIndex">Index of the update to help manage dynamic/fixed updates. [0: Invalid update, >=1: Valid update]</param>
        public override void Update(float rDeltaTime, int rUpdateIndex)
        {
            mMovement = Vector3.zero;
            mRotation = Quaternion.identity;

            // Handle an interruption
            if (mIsInterrupted)
            {
                if (mMotionLayer._AnimatorStateID != GetStateID("Interrupted") && //STATE_Interrupted &&
                    mMotionLayer._AnimatorTransitionID != GetAnyStateTransitionID("Interrupted") && //(GetAnyStateTransitionID("Interrupted &&
                    mMotionLayer._AnimatorTransitionID != GetEntryTransitionID("Interrupted")) // GetEntryTransitionID("Interrupted)
                {
                    if (!mIsInterruptedReported)
                    {
                        int lMotionParameter = (mWasTraversal ? 1 : 0);
                        mMotionController.SetAnimatorMotionPhase(mMotionLayer.AnimatorLayerIndex, PHASE_INTERRUPT, lMotionParameter, true);

                        if (mSpellInstance != null)
                        {
                            mSpellInstance.Cancel();
                        }

                        mIsInterruptedReported = true;
                    }
                }
            }

            // Determine IK we'll apply
            if (TestIKIn())
            {
                mIKWeight = mMotionLayer._AnimatorTransitionNormalizedTime;
            }
            else if (TestIKOut())
            {
                mIKWeight = 1f - mMotionLayer._AnimatorTransitionNormalizedTime;
            }
            else if (TestIK())
            {
                float lAdjustedTime = Mathf.Clamp(mMotionLayer._AnimatorStateNormalizedTime, 0f, 0.6f) / 0.6f;
                mIKWeight = Mathf.Max(mIKWeight, lAdjustedTime);
            }

            // If we're meant to rotate to the camera, do it
            if (RotateToCameraForward && mMotionController._CameraTransform != null)
            {
                if (TestRotate())
                {
                    mHasReachedForward = false;
                    mTargetForward = mMotionController._CameraTransform.forward;
                }
            }

            // Ensure we face the target if we're meant to
            if (mCombatant != null && mCombatant.IsTargetLocked)
            {
                RotateCameraToTarget(mCombatant.Target, _ToTargetRotationSpeed);
                RotateToTarget(mCombatant.Target, _ToTargetRotationSpeed, rDeltaTime, ref mRotation);
            }
            // Otherwise, rotate towards the camera
            else if (mTargetForward.sqrMagnitude > 0f)
            {
                RotateToTargetForward(mTargetForward, _ToTargetRotationSpeed, ref mRotation);
                RotateChestToTargetForward(mTargetForward, mIKWeight);
            }

            // If we have cast the spell, we need to determine how we'll move back
            if (mHasCast)
            {
                int lMotionParameter = (mWasTraversal ? 1 : 0);
                mMotionController.SetAnimatorMotionParameter(mMotionLayer.AnimatorLayerIndex, lMotionParameter);
            }

            // Allow the base class to render debug info
            base.Update(rDeltaTime, rUpdateIndex);
        }

        /// <summary>
        /// Raised by the animation when an event occurs
        /// </summary>
        public override void OnAnimationEvent(AnimationEvent rEvent)
        {
            if (rEvent == null) { return; }

            // Determine if we are starting the casting
            if (StringHelper.CleanString(rEvent.stringParameter) == EVENT_BEGIN_CAST)
            {
                mSpellInstance.Start();
            }
            // Determine if we're pausing the casting
            else if (StringHelper.CleanString(rEvent.stringParameter) == EVENT_PAUSE_CAST)
            {
                if (mSpellInstance.CastingPause)
                {
                    mStoredAnimatorSpeed = mMotionController.Animator.speed;
                    mMotionController.Animator.speed = 0f;
                }
            }
            // Determine if we actual activate the spell
            else if (StringHelper.CleanString(rEvent.stringParameter) == EVENT_CAST_SPELL)
            {
                mHasCast = true;
                mSpellInstance.Cast(ReleaseFromCamera, ReleaseDistance);
            }
            // Determine if we are ending casting (sometimes this happens at the cast)
            else if (StringHelper.CleanString(rEvent.stringParameter) == EVENT_END_CAST)
            {
                mSpellInstance.End();
            }
        }

        /// <summary>
        /// Raised by the controller when a message is received
        /// </summary>
        public override void OnMessageReceived(IMessage rMessage)
        {
            if (rMessage == null) { return; }
            //if (mActorController.State.Stance != EnumControllerStance.SPELL_CASTING) { return; }

            MagicMessage lMagicMessage = rMessage as MagicMessage;
            if (lMagicMessage != null)
            {
                if (lMagicMessage.ID == MagicMessage.MSG_MAGIC_CAST)
                {
                    mSpellIndex = lMagicMessage.SpellIndex;
                    mMotionController.ActivateMotion(this);

                    lMagicMessage.IsHandled = true;
                    lMagicMessage.Recipient = this;
                }
                else if (lMagicMessage.ID == MagicMessage.MSG_MAGIC_CONTINUE)
                {
                    if (mIsActive)
                    {
                        if (mStoredAnimatorSpeed > 0f) { mMotionController.Animator.speed = mStoredAnimatorSpeed; }

                        mMotionController.SetAnimatorMotionPhase(mMotionLayer.AnimatorLayerIndex, PHASE_CONTINUE, mSpellInstance.CastingStyle, 0, false);

                        lMagicMessage.IsHandled = true;
                        lMagicMessage.Recipient = this;
                    }
                }

                return;
            }

            // If we're not active, the other coditions don't matter
            if (!mIsActive)
            {
                CombatMessage lCombatMessage = rMessage as CombatMessage;
                if (lCombatMessage != null)
                {
                    // Attack message
                    if (lCombatMessage.Attacker == mMotionController.gameObject)
                    {
                        // Call for an attack
                        if (rMessage.ID == CombatMessage.MSG_COMBATANT_ATTACK)
                        {
                            if (lCombatMessage.Defender != null)
                            {
                                mTargetForward = (lCombatMessage.Defender.transform.position - mMotionController._Transform.position).normalized;
                            }
                            else if (lCombatMessage.HitDirection.sqrMagnitude > 0f)
                            {
                                mTargetForward = lCombatMessage.HitDirection;
                            }
                            else
                            {
                                mTargetForward = mMotionController._Transform.forward;
                            }

                            lCombatMessage.IsHandled = true;
                            lCombatMessage.Recipient = this;
                            mMotionController.ActivateMotion(this);
                        }
                    }
                }
            }
            // If we are, we may need to cancel
            else
            {
                MotionMessage lMotionMessage = rMessage as MotionMessage;
                if (lMotionMessage != null)
                {
                    // Continue with the casting
                    if (lMotionMessage.ID == MotionMessage.MSG_MOTION_CONTINUE)
                    {
                        if (mStoredAnimatorSpeed > 0f) { mMotionController.Animator.speed = mStoredAnimatorSpeed; }

                        mMotionController.SetAnimatorMotionPhase(mMotionLayer.AnimatorLayerIndex, PHASE_CONTINUE, mSpellInstance.CastingStyle, 0, false);

                        lMotionMessage.IsHandled = true;
                        lMotionMessage.Recipient = this;
                    }
                    // Cancel the casting
                    else if (lMotionMessage.ID == MotionMessage.MSG_MOTION_DEACTIVATE)
                    {
                        if (mStoredAnimatorSpeed > 0f) { mMotionController.Animator.speed = mStoredAnimatorSpeed; }

                        int lMotionParameter = (mWasTraversal ? 1 : 0);
                        mMotionController.SetAnimatorMotionPhase(mMotionLayer.AnimatorLayerIndex, PHASE_CANCEL, lMotionParameter, false);

                        lMotionMessage.IsHandled = true;
                        lMotionMessage.Recipient = this;
                    }
                }

                CombatMessage lCombatMessage = rMessage as CombatMessage;
                if (lCombatMessage != null)
                {
                    // Attack messages
                    if (lCombatMessage.Defender == mMotionController.gameObject)
                    {
                        // Determine if we're being attacked
                        if (rMessage.ID == CombatMessage.MSG_DEFENDER_ATTACKED)
                        {
                        }
                        // Gives us a chance to respond to the defender's reaction (post attack)
                        else if (rMessage.ID == CombatMessage.MSG_DEFENDER_ATTACKED_BLOCKED)
                        {
                            mIsInterrupted = true;

                            // The damaged and death animation may take over. So, cancel the instance now.
                            if (mSpellInstance != null) { mSpellInstance.Cancel(); }
                        }
                        // Final hit (post attack)
                        else if (rMessage.ID == CombatMessage.MSG_DEFENDER_ATTACKED_PARRIED ||
                                 rMessage.ID == CombatMessage.MSG_DEFENDER_DAMAGED ||
                                 rMessage.ID == CombatMessage.MSG_DEFENDER_KILLED)
                        {
                            mIsInterrupted = true;

                            // The damaged and death animation may take over. So, cancel the instance now.
                            if (mSpellInstance != null) { mSpellInstance.Cancel(); }
                        }
                    }
                    // Defender messages
                    else if (lCombatMessage.Defender == mMotionController.gameObject)
                    {
                    }
                }

                DamageMessage lDamageMessage = rMessage as DamageMessage;
                if (lDamageMessage != null)
                {
                    mIsInterrupted = true;

                    // The damaged and death animation may take over. So, cancel the instance now.
                    if (mSpellInstance != null) { mSpellInstance.Cancel(); }
                }
            }
        }

        /// <summary>
        /// Determine if we are ramping IK up
        /// </summary>
        /// <returns></returns>
        protected virtual bool TestIKIn()
        {
            if (mMotionLayer._AnimatorTransitionID == GetAnyStateTransitionID("1H_Cast_01")) { return true; }
            if (mMotionLayer._AnimatorTransitionID == GetEntryTransitionID("1H_Cast_01")) { return true; }
            if (mMotionLayer._AnimatorTransitionID == GetTransitionID("Stand Idle In", "1H_Cast_01")) { return true; }
            if (mMotionLayer._AnimatorTransitionID == GetAnyStateTransitionID("1H_Cast_02")) { return true; }
            if (mMotionLayer._AnimatorTransitionID == GetEntryTransitionID("1H_Cast_02")) { return true; }
            if (mMotionLayer._AnimatorTransitionID == GetTransitionID("Stand Idle In", "1H_Cast_02")) { return true; }
            if (mMotionLayer._AnimatorTransitionID == GetAnyStateTransitionID("1H_Cast_03")) { return true; }
            if (mMotionLayer._AnimatorTransitionID == GetEntryTransitionID("1H_Cast_03")) { return true; }
            if (mMotionLayer._AnimatorTransitionID == GetTransitionID("Stand Idle In", "1H_Cast_03")) { return true; }
            if (mMotionLayer._AnimatorTransitionID == GetAnyStateTransitionID("1H_Cast_04")) { return true; }
            if (mMotionLayer._AnimatorTransitionID == GetEntryTransitionID("1H_Cast_04")) { return true; }
            if (mMotionLayer._AnimatorTransitionID == GetTransitionID("Stand Idle In", "1H_Cast_04")) { return true; }
            if (mMotionLayer._AnimatorTransitionID == GetAnyStateTransitionID("2H_Cast_01")) { return true; }
            if (mMotionLayer._AnimatorTransitionID == GetEntryTransitionID("2H_Cast_01")) { return true; }
            if (mMotionLayer._AnimatorTransitionID == GetTransitionID("Stand Idle In", "2H_Cast_01")) { return true; }
            if (mMotionLayer._AnimatorTransitionID == GetAnyStateTransitionID("2H_Cast_02")) { return true; }
            if (mMotionLayer._AnimatorTransitionID == GetEntryTransitionID("2H_Cast_02")) { return true; }
            if (mMotionLayer._AnimatorTransitionID == GetTransitionID("Stand Idle In", "2H_Cast_02")) { return true; }
            if (mMotionLayer._AnimatorTransitionID == GetAnyStateTransitionID("2H_Cast_03")) { return true; }
            if (mMotionLayer._AnimatorTransitionID == GetEntryTransitionID("2H_Cast_03")) { return true; }
            if (mMotionLayer._AnimatorTransitionID == GetTransitionID("Stand Idle In", "2H_Cast_03")) { return true; }
            if (mMotionLayer._AnimatorTransitionID == GetAnyStateTransitionID("2H_Cast_04")) { return true; }
            if (mMotionLayer._AnimatorTransitionID == GetEntryTransitionID("2H_Cast_04")) { return true; }
            if (mMotionLayer._AnimatorTransitionID == GetTransitionID("Stand Idle In", "2H_Cast_04")) { return true; }
            if (mMotionLayer._AnimatorTransitionID == GetAnyStateTransitionID("2H_Cast_05")) { return true; }
            if (mMotionLayer._AnimatorTransitionID == GetEntryTransitionID("2H_Cast_05")) { return true; }
            if (mMotionLayer._AnimatorTransitionID == GetTransitionID("Stand Idle In", "2H_Cast_05")) { return true; }
            if (mMotionLayer._AnimatorTransitionID == GetAnyStateTransitionID("2H_Cast_06")) { return true; }
            if (mMotionLayer._AnimatorTransitionID == GetEntryTransitionID("2H_Cast_06")) { return true; }
            if (mMotionLayer._AnimatorTransitionID == GetTransitionID("Stand Idle In", "2H_Cast_06")) { return true; }
            if (mMotionLayer._AnimatorTransitionID == GetAnyStateTransitionID("2H_Cast_07")) { return true; }
            if (mMotionLayer._AnimatorTransitionID == GetEntryTransitionID("2H_Cast_07")) { return true; }
            if (mMotionLayer._AnimatorTransitionID == GetTransitionID("Stand Idle In", "2H_Cast_07")) { return true; }
            if (mMotionLayer._AnimatorTransitionID == GetAnyStateTransitionID("2H_Cast_08")) { return true; }
            if (mMotionLayer._AnimatorTransitionID == GetEntryTransitionID("2H_Cast_08")) { return true; }
            if (mMotionLayer._AnimatorTransitionID == GetTransitionID("Stand Idle In", "2H_Cast_08")) { return true; }
            return false;
        }

        /// <summary>
        /// Determine if we are ramping IK down
        /// </summary>
        /// <returns></returns>
        protected virtual bool TestIKOut()
        {
            if (mMotionLayer._AnimatorTransitionID == GetTransitionID("1H_Cast_01", "Spell Idle Out")) { return true; }
            if (mMotionLayer._AnimatorTransitionID == GetTransitionID("1H_Cast_01", "Stand Idle Transition")) { return true; }
            if (mMotionLayer._AnimatorTransitionID == GetTransitionID("1H_Cast_02", "Spell IdleOut")) { return true; }
            if (mMotionLayer._AnimatorTransitionID == GetTransitionID("1H_Cast_02", "Stand Idle Transition")) { return true; }
            if (mMotionLayer._AnimatorTransitionID == GetTransitionID("1H_Cast_03", "Spell Idle Out")) { return true; }
            if (mMotionLayer._AnimatorTransitionID == GetTransitionID("1H_Cast_03", "Stand Idle Transition")) { return true; }
            if (mMotionLayer._AnimatorTransitionID == GetTransitionID("1H_Cast_04", "Spell Idle Out")) { return true; }
            if (mMotionLayer._AnimatorTransitionID == GetTransitionID("1H_Cast_04", "Stand Idle Transition")) { return true; }
            if (mMotionLayer._AnimatorTransitionID == GetTransitionID("2H_Cast_01", "Spell Idle Out")) { return true; }
            if (mMotionLayer._AnimatorTransitionID == GetTransitionID("2H_Cast_01", "Stand Idle Transition")) { return true; }
            if (mMotionLayer._AnimatorTransitionID == GetTransitionID("2H_Cast_02", "Spell Idle Out")) { return true; }
            if (mMotionLayer._AnimatorTransitionID == GetTransitionID("2H_Cast_02", "Stand Idle Transition")) { return true; }
            if (mMotionLayer._AnimatorTransitionID == GetTransitionID("2H_Cast_03", "Spell Idle Out")) { return true; }
            if (mMotionLayer._AnimatorTransitionID == GetTransitionID("2H_Cast_03", "Stand Idle Transition")) { return true; }
            if (mMotionLayer._AnimatorTransitionID == GetTransitionID("2H_Cast_04", "Spell Idle Out")) { return true; }
            if (mMotionLayer._AnimatorTransitionID == GetTransitionID("2H_Cast_04", "Stand Idle Transition")) { return true; }
            if (mMotionLayer._AnimatorTransitionID == GetTransitionID("2H_Cast_05", "Spell Idle Out")) { return true; }
            if (mMotionLayer._AnimatorTransitionID == GetTransitionID("2H_Cast_05", "Stand Idle Transition")) { return true; }
            if (mMotionLayer._AnimatorTransitionID == GetTransitionID("2H_Cast_06", "Spell Idle Out")) { return true; }
            if (mMotionLayer._AnimatorTransitionID == GetTransitionID("2H_Cast_06", "Stand Idle Transition")) { return true; }
            if (mMotionLayer._AnimatorTransitionID == GetTransitionID("2H_Cast_07", "Spell Idle Out")) { return true; }
            if (mMotionLayer._AnimatorTransitionID == GetTransitionID("2H_Cast_07", "Stand Idle Transition")) { return true; }
            if (mMotionLayer._AnimatorTransitionID == GetTransitionID("2H_Cast_08", "Spell Idle Out")) { return true; }
            if (mMotionLayer._AnimatorTransitionID == GetTransitionID("2H_Cast_08", "Stand Idle Transition")) { return true; }

            return false;
        }

        /// <summary>
        /// Determines if we should be applying IK
        /// </summary>
        /// <returns></returns>
        protected virtual bool TestIK()
        {
            if (mMotionLayer._AnimatorStateID == GetStateID("1H_Cast_01")) { return true; }
            if (mMotionLayer._AnimatorStateID == GetStateID("1H_Cast_02")) { return true; }
            if (mMotionLayer._AnimatorStateID == GetStateID("1H_Cast_03")) { return true; }
            if (mMotionLayer._AnimatorStateID == GetStateID("1H_Cast_04")) { return true; }
            if (mMotionLayer._AnimatorStateID == GetStateID("2H_Cast_01")) { return true; }
            if (mMotionLayer._AnimatorStateID == GetStateID("2H_Cast_02")) { return true; }
            if (mMotionLayer._AnimatorStateID == GetStateID("2H_Cast_03")) { return true; }
            if (mMotionLayer._AnimatorStateID == GetStateID("2H_Cast_04")) { return true; }
            if (mMotionLayer._AnimatorStateID == GetStateID("2H_Cast_05")) { return true; }
            if (mMotionLayer._AnimatorStateID == GetStateID("2H_Cast_06")) { return true; }
            if (mMotionLayer._AnimatorStateID == GetStateID("2H_Cast_07")) { return true; }
            if (mMotionLayer._AnimatorStateID == GetStateID("2H_Cast_08")) { return true; }

            return false;
        }

        /// <summary>
        /// Determine if we should be controlling the rotation
        /// </summary>
        /// <returns></returns>
        protected virtual bool TestRotate()
        {
            if (mMotionLayer._AnimatorStateID == GetStateID("1H_Cast_01_b")) { return true; }
            if (mMotionLayer._AnimatorStateID == GetStateID("1H_Cast_02_b")) { return true; }
            if (mMotionLayer._AnimatorStateID == GetStateID("1H_Cast_03_b")) { return true; }
            if (mMotionLayer._AnimatorStateID == GetStateID("1H_Cast_04_b")) { return true; }
            if (mMotionLayer._AnimatorStateID == GetStateID("2H_Cast_01_b")) { return true; }
            if (mMotionLayer._AnimatorStateID == GetStateID("2H_Cast_02_b")) { return true; }
            if (mMotionLayer._AnimatorStateID == GetStateID("2H_Cast_03_b")) { return true; }
            if (mMotionLayer._AnimatorStateID == GetStateID("2H_Cast_04_b")) { return true; }
            if (mMotionLayer._AnimatorStateID == GetStateID("2H_Cast_05_b")) { return true; }
            if (mMotionLayer._AnimatorStateID == GetStateID("2H_Cast_06_b")) { return true; }
            if (mMotionLayer._AnimatorStateID == GetStateID("2H_Cast_07_b")) { return true; }
            if (mMotionLayer._AnimatorStateID == GetStateID("2H_Cast_08_b")) { return true; }

            return false;
        }

        #region Editor Functions

        // **************************************************************************************************
        // Following properties and function only valid while editing
        // **************************************************************************************************

#if UNITY_EDITOR

        /// <summary>
        /// Reset to default values. Reset is called when the user hits the Reset button in the Inspector's 
        /// context menu or when adding the component the first time. This function is only called in editor mode.
        /// </summary>
        public override void Reset()
        {
        }

        /// <summary>
        /// Allow the constraint to render it's own GUI
        /// </summary>
        /// <returns>Reports if the object's value was changed</returns>
        public override bool OnInspectorGUI()
        {
            bool lIsDirty = false;

            if (EditorHelper.BoolField("Requires Stance", "Determines if spell can only be cast while in the Spell Casting stance.", RequiresStance, mMotionController))
            {
                lIsDirty = true;
                RequiresStance = EditorHelper.FieldBoolValue;
            }

            if (EditorHelper.TextField("Attack Alias", "Action alias that is required to trigger the attack.", ActionAlias, mMotionController))
            {
                lIsDirty = true;
                ActionAlias = EditorHelper.FieldStringValue;
            }

            GUILayout.Space(5f);

            if (EditorHelper.BoolField("Rotate To Camera", "Determines if we'll rotate to face the camera forward.", RotateToCameraForward, mMotionController))
            {
                lIsDirty = true;
                RotateToCameraForward = EditorHelper.FieldBoolValue;
            }

            if (EditorHelper.FloatField("Rotation Speed", "Degrees per second to rotate the actor.", ToTargetRotationSpeed, mMotionController))
            {
                lIsDirty = true;
                ToTargetRotationSpeed = EditorHelper.FieldFloatValue;
            }

            GUILayout.Space(5f);

            if (EditorHelper.BoolField("Release From Camera", "Determines if we release from the camera's position and rotation. This overrides the spell actions.", ReleaseFromCamera, mMotionController))
            {
                lIsDirty = true;
                ReleaseFromCamera = EditorHelper.FieldBoolValue;
            }

            if (ReleaseFromCamera)
            {
                if (EditorHelper.FloatField("Release Distance", "Determines the distance from the camera that we release.", ReleaseDistance, mMotionController))
                {
                    lIsDirty = true;
                    ReleaseDistance = EditorHelper.FieldFloatValue;
                }
            }

            GUILayout.Space(5f);

            EditorHelper.DrawInspectorDescription("IK properties for aiming the right arm", MessageType.None);

            if (EditorHelper.FloatField("Horizontal Angle", "Amount of yaw to add to make the aim look correct. [Pos is right, Neg is left]", HorizontalAimAngle, mMotionController))
            {
                lIsDirty = true;
                HorizontalAimAngle = EditorHelper.FieldFloatValue;
            }

            if (EditorHelper.FloatField("Vertical Angle", "Amount of pitch to add to make the aim look correct. [Pos is down, Neg is up]", VerticalAimAngle, mMotionController))
            {
                lIsDirty = true;
                VerticalAimAngle = EditorHelper.FieldFloatValue;
            }

            return lIsDirty;
        }

#endif

        #endregion

        #region Auto-Generated
        // ************************************ START AUTO GENERATED ************************************

        /// <summary>
        /// Determines if we're using auto-generated code
        /// </summary>
        public override bool HasAutoGeneratedCode
        {
            get { return true; }
        }

#if UNITY_EDITOR

        /// <summary>
        /// New way to create sub-state machines without destroying what exists first.
        /// </summary>
        protected override void CreateStateMachine()
        {
            int rLayerIndex = mMotionLayer._AnimatorLayerIndex;
            MotionController rMotionController = mMotionController;

            SpellCastingPackDefinition.ExtendBasicSpellCasting(rMotionController, rLayerIndex);

            // Run any post processing after creating the state machine
            OnStateMachineCreated();
        }

#endif

        // ************************************ END AUTO GENERATED ************************************
        #endregion
    }
}

//Interrupted,Stand Idle In,Spell Idle Out,Stand Idle Transition,Stand Idle Out,
//1H_Cast_01,1H_Cast_01_a,1H_Cast_01_b,1H_Cast_01_c,
//1H_Cast_02,1H_Cast_02_a,1H_Cast_02_b,1H_Cast_02_c,
//1H_Cast_03,1H_Cast_03_a,1H_Cast_03_b,1H_Cast_03_c,
//1H_Cast_04,1H_Cast_04_a,1H_Cast_04_b,1H_Cast_04_c,
//2H_Cast_01,2H_Cast_01_a,2H_Cast_01_b,2H_Cast_01_c,
//2H_Cast_02,2H_Cast_02_a,2H_Cast_02_b,2H_Cast_02_c,
//2H_Cast_03,2H_Cast_03_a,2H_Cast_03_b,2H_Cast_03_c,
//2H_Cast_04,2H_Cast_04_a,2H_Cast_04_b,2H_Cast_04_c,
//2H_Cast_05,2H_Cast_05_a,2H_Cast_05_b,2H_Cast_05_c,
//2H_Cast_06,2H_Cast_06_a,2H_Cast_06_b,2H_Cast_06_c,
//2H_Cast_07,2H_Cast_07_a,2H_Cast_07_b,2H_Cast_07_c,
//2H_Cast_08,2H_Cast_08_a,2H_Cast_08_b,2H_Cast_08_c
