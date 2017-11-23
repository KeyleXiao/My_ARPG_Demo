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
    [MotionName("PMP - Basic Spell Castings")]
    [MotionDescription("Basic spell casting using Mixamo's Pro Magic Pack animations.")]
    public class PMP_BasicSpellCastings : PMP_MotionBase
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
        public PMP_BasicSpellCastings()
            : base()
        {
            _Pack = PMP_Idle.GroupName();
            _Category = EnumMotionCategories.SPELL_CASTING;

            _Priority = 16;
            _ActionAlias = "";

#if UNITY_EDITOR
            if (_EditorAnimatorSMName.Length == 0) { _EditorAnimatorSMName = "PMP_BasicSpellCastings-SM"; }
#endif
        }

        /// <summary>
        /// Controller constructor
        /// </summary>
        /// <param name="rController">Controller the motion belongs to</param>
        public PMP_BasicSpellCastings(MotionController rController)
            : base(rController)
        {
            _Pack = PMP_Idle.GroupName();
            _Category = EnumMotionCategories.SPELL_CASTING;

            _Priority = 16;
            _ActionAlias = "";

#if UNITY_EDITOR
            if (_EditorAnimatorSMName.Length == 0) { _EditorAnimatorSMName = "PMP_BasicSpellCastings-SM"; }
#endif
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
            if (mMotionLayer._AnimatorStateID == STATE_StandIdleOut)
            {
                return false;
            }

            if (mMotionLayer._AnimatorStateID == STATE_SpellIdleOut)
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
            
            // Ensure we're actually in our animation state
            if (mIsAnimatorActive)
            {
                if (!IsInMotionState)
                {
                    return false;
                }
            }

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
            IActorCore lActorCore = mMotionController.gameObject.GetComponent<ActorCore>();
            if (lActorCore != null)
            {
                MagicMessage lMagicMessage = MagicMessage.Allocate();
                lMagicMessage.ID = MagicMessage.MSG_MAGIC_PRE_CAST;
                lMagicMessage.Caster = mMotionController.gameObject;
                lMagicMessage.SpellIndex = lSpellIndex;
                lMagicMessage.CastingMotion = this;
                lMagicMessage.Data = this;

                lActorCore.SendMessage(lMagicMessage);

                lSpellIndex = lMagicMessage.SpellIndex;

                MagicMessage.Release(lMagicMessage);
            }

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
            mMotionController.SetAnimatorMotionPhase(mMotionLayer.AnimatorLayerIndex, lMotionPhase, mSpellInstance.CastingStyle, true);

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
                if (mMotionLayer._AnimatorStateID != STATE_Interrupted &&
                    mMotionLayer._AnimatorTransitionID != TRANS_AnyState_Interrupted &&
                    mMotionLayer._AnimatorTransitionID != TRANS_EntryState_Interrupted)
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

                        mMotionController.SetAnimatorMotionPhase(mMotionLayer.AnimatorLayerIndex, PHASE_CONTINUE, mSpellInstance.CastingStyle, false);

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

                        mMotionController.SetAnimatorMotionPhase(mMotionLayer.AnimatorLayerIndex, PHASE_CONTINUE, mSpellInstance.CastingStyle, false);

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
        protected bool TestIKIn()
        {
            if (mMotionLayer._AnimatorTransitionID == TRANS_AnyState_1H_Cast_01) { return true; }
            if (mMotionLayer._AnimatorTransitionID == TRANS_EntryState_1H_Cast_01) { return true; }
            if (mMotionLayer._AnimatorTransitionID == TRANS_StandIdleIn_1H_Cast_01) { return true; }
            if (mMotionLayer._AnimatorTransitionID == TRANS_AnyState_1H_Cast_02) { return true; }
            if (mMotionLayer._AnimatorTransitionID == TRANS_EntryState_1H_Cast_02) { return true; }
            if (mMotionLayer._AnimatorTransitionID == TRANS_StandIdleIn_1H_Cast_02) { return true; }
            if (mMotionLayer._AnimatorTransitionID == TRANS_AnyState_1H_Cast_03) { return true; }
            if (mMotionLayer._AnimatorTransitionID == TRANS_EntryState_1H_Cast_03) { return true; }
            if (mMotionLayer._AnimatorTransitionID == TRANS_StandIdleIn_1H_Cast_03) { return true; }
            if (mMotionLayer._AnimatorTransitionID == TRANS_AnyState_1H_Cast_04) { return true; }
            if (mMotionLayer._AnimatorTransitionID == TRANS_EntryState_1H_Cast_04) { return true; }
            if (mMotionLayer._AnimatorTransitionID == TRANS_StandIdleIn_1H_Cast_04) { return true; }
            if (mMotionLayer._AnimatorTransitionID == TRANS_AnyState_2H_Cast_01) { return true; }
            if (mMotionLayer._AnimatorTransitionID == TRANS_EntryState_2H_Cast_01) { return true; }
            if (mMotionLayer._AnimatorTransitionID == TRANS_StandIdleIn_2H_Cast_01) { return true; }
            if (mMotionLayer._AnimatorTransitionID == TRANS_AnyState_2H_Cast_02) { return true; }
            if (mMotionLayer._AnimatorTransitionID == TRANS_EntryState_2H_Cast_02) { return true; }
            if (mMotionLayer._AnimatorTransitionID == TRANS_StandIdleIn_2H_Cast_02) { return true; }
            if (mMotionLayer._AnimatorTransitionID == TRANS_AnyState_2H_Cast_03) { return true; }
            if (mMotionLayer._AnimatorTransitionID == TRANS_EntryState_2H_Cast_03) { return true; }
            if (mMotionLayer._AnimatorTransitionID == TRANS_StandIdleIn_2H_Cast_03) { return true; }
            if (mMotionLayer._AnimatorTransitionID == TRANS_AnyState_2H_Cast_04) { return true; }
            if (mMotionLayer._AnimatorTransitionID == TRANS_EntryState_2H_Cast_04) { return true; }
            if (mMotionLayer._AnimatorTransitionID == TRANS_StandIdleIn_2H_Cast_04) { return true; }
            if (mMotionLayer._AnimatorTransitionID == TRANS_AnyState_2H_Cast_05) { return true; }
            if (mMotionLayer._AnimatorTransitionID == TRANS_EntryState_2H_Cast_05) { return true; }
            if (mMotionLayer._AnimatorTransitionID == TRANS_StandIdleIn_2H_Cast_05) { return true; }
            if (mMotionLayer._AnimatorTransitionID == TRANS_AnyState_2H_Cast_06) { return true; }
            if (mMotionLayer._AnimatorTransitionID == TRANS_EntryState_2H_Cast_06) { return true; }
            if (mMotionLayer._AnimatorTransitionID == TRANS_StandIdleIn_2H_Cast_06) { return true; }
            if (mMotionLayer._AnimatorTransitionID == TRANS_AnyState_2H_Cast_07) { return true; }
            if (mMotionLayer._AnimatorTransitionID == TRANS_EntryState_2H_Cast_07) { return true; }
            if (mMotionLayer._AnimatorTransitionID == TRANS_StandIdleIn_2H_Cast_07) { return true; }
            if (mMotionLayer._AnimatorTransitionID == TRANS_AnyState_2H_Cast_08) { return true; }
            if (mMotionLayer._AnimatorTransitionID == TRANS_EntryState_2H_Cast_08) { return true; }
            if (mMotionLayer._AnimatorTransitionID == TRANS_StandIdleIn_2H_Cast_08) { return true; }
            return false;
        }

        /// <summary>
        /// Determine if we are ramping IK down
        /// </summary>
        /// <returns></returns>
        protected bool TestIKOut()
        {
            if (mMotionLayer._AnimatorTransitionID == TRANS_1H_Cast_01_SpellIdleOut) { return true; }
            if (mMotionLayer._AnimatorTransitionID == TRANS_1H_Cast_01_StandIdleTransition) { return true; }
            if (mMotionLayer._AnimatorTransitionID == TRANS_1H_Cast_02_SpellIdleOut) { return true; }
            if (mMotionLayer._AnimatorTransitionID == TRANS_1H_Cast_02_StandIdleTransition) { return true; }
            if (mMotionLayer._AnimatorTransitionID == TRANS_1H_Cast_03_SpellIdleOut) { return true; }
            if (mMotionLayer._AnimatorTransitionID == TRANS_1H_Cast_03_StandIdleTransition) { return true; }
            if (mMotionLayer._AnimatorTransitionID == TRANS_1H_Cast_04_SpellIdleOut) { return true; }
            if (mMotionLayer._AnimatorTransitionID == TRANS_1H_Cast_04_StandIdleTransition) { return true; }
            if (mMotionLayer._AnimatorTransitionID == TRANS_2H_Cast_01_SpellIdleOut) { return true; }
            if (mMotionLayer._AnimatorTransitionID == TRANS_2H_Cast_01_StandIdleTransition) { return true; }
            if (mMotionLayer._AnimatorTransitionID == TRANS_2H_Cast_02_SpellIdleOut) { return true; }
            if (mMotionLayer._AnimatorTransitionID == TRANS_2H_Cast_02_StandIdleTransition) { return true; }
            if (mMotionLayer._AnimatorTransitionID == TRANS_2H_Cast_03_SpellIdleOut) { return true; }
            if (mMotionLayer._AnimatorTransitionID == TRANS_2H_Cast_03_StandIdleTransition) { return true; }
            if (mMotionLayer._AnimatorTransitionID == TRANS_2H_Cast_04_SpellIdleOut) { return true; }
            if (mMotionLayer._AnimatorTransitionID == TRANS_2H_Cast_04_StandIdleTransition) { return true; }
            if (mMotionLayer._AnimatorTransitionID == TRANS_2H_Cast_05_SpellIdleOut) { return true; }
            if (mMotionLayer._AnimatorTransitionID == TRANS_2H_Cast_05_StandIdleTransition) { return true; }
            if (mMotionLayer._AnimatorTransitionID == TRANS_2H_Cast_06_SpellIdleOut) { return true; }
            if (mMotionLayer._AnimatorTransitionID == TRANS_2H_Cast_06_StandIdleTransition) { return true; }
            if (mMotionLayer._AnimatorTransitionID == TRANS_2H_Cast_07_SpellIdleOut) { return true; }
            if (mMotionLayer._AnimatorTransitionID == TRANS_2H_Cast_07_StandIdleTransition) { return true; }
            if (mMotionLayer._AnimatorTransitionID == TRANS_2H_Cast_08_SpellIdleOut) { return true; }
            if (mMotionLayer._AnimatorTransitionID == TRANS_2H_Cast_08_StandIdleTransition) { return true; }

            return false;
        }

        /// <summary>
        /// Determines if we should be applying IK
        /// </summary>
        /// <returns></returns>
        protected bool TestIK()
        {
            if (mMotionLayer._AnimatorStateID == STATE_1H_Cast_01) { return true; }
            if (mMotionLayer._AnimatorStateID == STATE_1H_Cast_02) { return true; }
            if (mMotionLayer._AnimatorStateID == STATE_1H_Cast_03) { return true; }
            if (mMotionLayer._AnimatorStateID == STATE_1H_Cast_04) { return true; }
            if (mMotionLayer._AnimatorStateID == STATE_2H_Cast_01) { return true; }
            if (mMotionLayer._AnimatorStateID == STATE_2H_Cast_02) { return true; }
            if (mMotionLayer._AnimatorStateID == STATE_2H_Cast_03) { return true; }
            if (mMotionLayer._AnimatorStateID == STATE_2H_Cast_04) { return true; }
            if (mMotionLayer._AnimatorStateID == STATE_2H_Cast_05) { return true; }
            if (mMotionLayer._AnimatorStateID == STATE_2H_Cast_06) { return true; }
            if (mMotionLayer._AnimatorStateID == STATE_2H_Cast_07) { return true; }
            if (mMotionLayer._AnimatorStateID == STATE_2H_Cast_08) { return true; }

            return false;
        }

        /// <summary>
        /// Determine if we should be controlling the rotation
        /// </summary>
        /// <returns></returns>
        protected bool TestRotate()
        {
            if (mMotionLayer._AnimatorStateID == STATE_1H_Cast_01_b) { return true; }
            if (mMotionLayer._AnimatorStateID == STATE_1H_Cast_02_b) { return true; }
            if (mMotionLayer._AnimatorStateID == STATE_1H_Cast_03_b) { return true; }
            if (mMotionLayer._AnimatorStateID == STATE_1H_Cast_04_b) { return true; }
            if (mMotionLayer._AnimatorStateID == STATE_2H_Cast_01_b) { return true; }
            if (mMotionLayer._AnimatorStateID == STATE_2H_Cast_02_b) { return true; }
            if (mMotionLayer._AnimatorStateID == STATE_2H_Cast_03_b) { return true; }
            if (mMotionLayer._AnimatorStateID == STATE_2H_Cast_04_b) { return true; }
            if (mMotionLayer._AnimatorStateID == STATE_2H_Cast_05_b) { return true; }
            if (mMotionLayer._AnimatorStateID == STATE_2H_Cast_06_b) { return true; }
            if (mMotionLayer._AnimatorStateID == STATE_2H_Cast_07_b) { return true; }
            if (mMotionLayer._AnimatorStateID == STATE_2H_Cast_08_b) { return true; }

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
        /// These declarations go inside the class so you can test for which state
        /// and transitions are active. Testing hash values is much faster than strings.
        /// </summary>
        public static int STATE_Start = -1;
        public static int STATE_SpellIdleOut = -1;
        public static int STATE_1H_Cast_01 = -1;
        public static int STATE_StandIdleIn = -1;
        public static int STATE_StandIdleTransition = -1;
        public static int STATE_StandIdleOut = -1;
        public static int STATE_2H_Cast_01_a = -1;
        public static int STATE_2H_Cast_01_c = -1;
        public static int STATE_2H_Cast_01_b = -1;
        public static int STATE_2H_Cast_01 = -1;
        public static int STATE_1H_Cast_01_a = -1;
        public static int STATE_1H_Cast_01_b = -1;
        public static int STATE_1H_Cast_01_c = -1;
        public static int STATE_Interrupted = -1;
        public static int STATE_1H_Cast_02 = -1;
        public static int STATE_1H_Cast_02_a = -1;
        public static int STATE_1H_Cast_02_b = -1;
        public static int STATE_1H_Cast_02_c = -1;
        public static int STATE_2H_Cast_02 = -1;
        public static int STATE_2H_Cast_02_a = -1;
        public static int STATE_2H_Cast_02_b = -1;
        public static int STATE_2H_Cast_02_c = -1;
        public static int STATE_2H_Cast_03 = -1;
        public static int STATE_2H_Cast_03_a = -1;
        public static int STATE_2H_Cast_03_b = -1;
        public static int STATE_2H_Cast_03_c = -1;
        public static int STATE_2H_Cast_04 = -1;
        public static int STATE_2H_Cast_04_a = -1;
        public static int STATE_2H_Cast_04_b = -1;
        public static int STATE_2H_Cast_04_c = -1;
        public static int STATE_2H_Cast_05 = -1;
        public static int STATE_2H_Cast_05_a = -1;
        public static int STATE_2H_Cast_05_b = -1;
        public static int STATE_2H_Cast_05_c = -1;
        public static int STATE_2H_Cast_06 = -1;
        public static int STATE_2H_Cast_06_a = -1;
        public static int STATE_2H_Cast_06_b = -1;
        public static int STATE_2H_Cast_06_c = -1;
        public static int STATE_2H_Cast_07 = -1;
        public static int STATE_2H_Cast_07_a = -1;
        public static int STATE_2H_Cast_07_b = -1;
        public static int STATE_2H_Cast_07_c = -1;
        public static int STATE_2H_Cast_08 = -1;
        public static int STATE_2H_Cast_08_a = -1;
        public static int STATE_2H_Cast_08_b = -1;
        public static int STATE_2H_Cast_08_c = -1;
        public static int STATE_1H_Cast_03 = -1;
        public static int STATE_1H_Cast_03_a = -1;
        public static int STATE_1H_Cast_03_b = -1;
        public static int STATE_1H_Cast_03_c = -1;
        public static int STATE_1H_Cast_04 = -1;
        public static int STATE_1H_Cast_04_a = -1;
        public static int STATE_1H_Cast_04_b = -1;
        public static int STATE_1H_Cast_04_c = -1;
        public static int TRANS_AnyState_StandIdleIn = -1;
        public static int TRANS_EntryState_StandIdleIn = -1;
        public static int TRANS_AnyState_1H_Cast_01 = -1;
        public static int TRANS_EntryState_1H_Cast_01 = -1;
        public static int TRANS_AnyState_2H_Cast_01_a = -1;
        public static int TRANS_EntryState_2H_Cast_01_a = -1;
        public static int TRANS_AnyState_2H_Cast_01 = -1;
        public static int TRANS_EntryState_2H_Cast_01 = -1;
        public static int TRANS_AnyState_1H_Cast_01_a = -1;
        public static int TRANS_EntryState_1H_Cast_01_a = -1;
        public static int TRANS_AnyState_Interrupted = -1;
        public static int TRANS_EntryState_Interrupted = -1;
        public static int TRANS_AnyState_1H_Cast_02 = -1;
        public static int TRANS_EntryState_1H_Cast_02 = -1;
        public static int TRANS_AnyState_1H_Cast_02_a = -1;
        public static int TRANS_EntryState_1H_Cast_02_a = -1;
        public static int TRANS_AnyState_2H_Cast_02 = -1;
        public static int TRANS_EntryState_2H_Cast_02 = -1;
        public static int TRANS_AnyState_1H_Cast_03 = -1;
        public static int TRANS_EntryState_1H_Cast_03 = -1;
        public static int TRANS_AnyState_1H_Cast_03_a = -1;
        public static int TRANS_EntryState_1H_Cast_03_a = -1;
        public static int TRANS_AnyState_1H_Cast_04 = -1;
        public static int TRANS_EntryState_1H_Cast_04 = -1;
        public static int TRANS_AnyState_1H_Cast_04_a = -1;
        public static int TRANS_EntryState_1H_Cast_04_a = -1;
        public static int TRANS_AnyState_2H_Cast_02_a = -1;
        public static int TRANS_EntryState_2H_Cast_02_a = -1;
        public static int TRANS_AnyState_2H_Cast_03 = -1;
        public static int TRANS_EntryState_2H_Cast_03 = -1;
        public static int TRANS_AnyState_2H_Cast_03_a = -1;
        public static int TRANS_EntryState_2H_Cast_03_a = -1;
        public static int TRANS_AnyState_2H_Cast_04 = -1;
        public static int TRANS_EntryState_2H_Cast_04 = -1;
        public static int TRANS_AnyState_2H_Cast_04_a = -1;
        public static int TRANS_EntryState_2H_Cast_04_a = -1;
        public static int TRANS_AnyState_2H_Cast_05 = -1;
        public static int TRANS_EntryState_2H_Cast_05 = -1;
        public static int TRANS_AnyState_2H_Cast_05_a = -1;
        public static int TRANS_EntryState_2H_Cast_05_a = -1;
        public static int TRANS_AnyState_2H_Cast_06 = -1;
        public static int TRANS_EntryState_2H_Cast_06 = -1;
        public static int TRANS_AnyState_2H_Cast_06_a = -1;
        public static int TRANS_EntryState_2H_Cast_06_a = -1;
        public static int TRANS_AnyState_2H_Cast_07 = -1;
        public static int TRANS_EntryState_2H_Cast_07 = -1;
        public static int TRANS_AnyState_2H_Cast_07_a = -1;
        public static int TRANS_EntryState_2H_Cast_07_a = -1;
        public static int TRANS_AnyState_2H_Cast_08 = -1;
        public static int TRANS_EntryState_2H_Cast_08 = -1;
        public static int TRANS_AnyState_2H_Cast_08_a = -1;
        public static int TRANS_EntryState_2H_Cast_08_a = -1;
        public static int TRANS_1H_Cast_01_SpellIdleOut = -1;
        public static int TRANS_1H_Cast_01_StandIdleTransition = -1;
        public static int TRANS_StandIdleIn_1H_Cast_01 = -1;
        public static int TRANS_StandIdleIn_2H_Cast_01_a = -1;
        public static int TRANS_StandIdleIn_2H_Cast_01 = -1;
        public static int TRANS_StandIdleIn_1H_Cast_01_a = -1;
        public static int TRANS_StandIdleIn_1H_Cast_02 = -1;
        public static int TRANS_StandIdleIn_1H_Cast_02_a = -1;
        public static int TRANS_StandIdleIn_2H_Cast_02 = -1;
        public static int TRANS_StandIdleIn_1H_Cast_03 = -1;
        public static int TRANS_StandIdleIn_1H_Cast_03_a = -1;
        public static int TRANS_StandIdleIn_1H_Cast_04 = -1;
        public static int TRANS_StandIdleIn_1H_Cast_04_a = -1;
        public static int TRANS_StandIdleIn_2H_Cast_02_a = -1;
        public static int TRANS_StandIdleIn_2H_Cast_03 = -1;
        public static int TRANS_StandIdleIn_2H_Cast_03_a = -1;
        public static int TRANS_StandIdleIn_2H_Cast_04 = -1;
        public static int TRANS_StandIdleIn_2H_Cast_04_a = -1;
        public static int TRANS_StandIdleIn_2H_Cast_05 = -1;
        public static int TRANS_StandIdleIn_2H_Cast_05_a = -1;
        public static int TRANS_StandIdleIn_2H_Cast_06 = -1;
        public static int TRANS_StandIdleIn_2H_Cast_06_a = -1;
        public static int TRANS_StandIdleIn_2H_Cast_07 = -1;
        public static int TRANS_StandIdleIn_2H_Cast_07_a = -1;
        public static int TRANS_StandIdleIn_2H_Cast_08 = -1;
        public static int TRANS_StandIdleIn_2H_Cast_08_a = -1;
        public static int TRANS_StandIdleTransition_StandIdleOut = -1;
        public static int TRANS_2H_Cast_01_a_2H_Cast_01_b = -1;
        public static int TRANS_2H_Cast_01_c_SpellIdleOut = -1;
        public static int TRANS_2H_Cast_01_c_StandIdleTransition = -1;
        public static int TRANS_2H_Cast_01_b_2H_Cast_01_c = -1;
        public static int TRANS_2H_Cast_01_b_StandIdleTransition = -1;
        public static int TRANS_2H_Cast_01_b_SpellIdleOut = -1;
        public static int TRANS_2H_Cast_01_SpellIdleOut = -1;
        public static int TRANS_2H_Cast_01_StandIdleTransition = -1;
        public static int TRANS_1H_Cast_01_a_1H_Cast_01_b = -1;
        public static int TRANS_1H_Cast_01_b_1H_Cast_01_c = -1;
        public static int TRANS_1H_Cast_01_b_SpellIdleOut = -1;
        public static int TRANS_1H_Cast_01_b_StandIdleTransition = -1;
        public static int TRANS_1H_Cast_01_c_SpellIdleOut = -1;
        public static int TRANS_1H_Cast_01_c_StandIdleTransition = -1;
        public static int TRANS_Interrupted_SpellIdleOut = -1;
        public static int TRANS_Interrupted_StandIdleTransition = -1;
        public static int TRANS_1H_Cast_02_SpellIdleOut = -1;
        public static int TRANS_1H_Cast_02_StandIdleTransition = -1;
        public static int TRANS_1H_Cast_02_a_1H_Cast_02_b = -1;
        public static int TRANS_1H_Cast_02_b_1H_Cast_02_c = -1;
        public static int TRANS_1H_Cast_02_b_SpellIdleOut = -1;
        public static int TRANS_1H_Cast_02_b_StandIdleTransition = -1;
        public static int TRANS_1H_Cast_02_c_SpellIdleOut = -1;
        public static int TRANS_1H_Cast_02_c_StandIdleTransition = -1;
        public static int TRANS_2H_Cast_02_SpellIdleOut = -1;
        public static int TRANS_2H_Cast_02_StandIdleTransition = -1;
        public static int TRANS_2H_Cast_02_a_2H_Cast_02_b = -1;
        public static int TRANS_2H_Cast_02_b_2H_Cast_02_c = -1;
        public static int TRANS_2H_Cast_02_b_SpellIdleOut = -1;
        public static int TRANS_2H_Cast_02_b_StandIdleTransition = -1;
        public static int TRANS_2H_Cast_02_c_SpellIdleOut = -1;
        public static int TRANS_2H_Cast_02_c_StandIdleTransition = -1;
        public static int TRANS_2H_Cast_03_SpellIdleOut = -1;
        public static int TRANS_2H_Cast_03_StandIdleTransition = -1;
        public static int TRANS_2H_Cast_03_a_2H_Cast_03_b = -1;
        public static int TRANS_2H_Cast_03_b_2H_Cast_03_c = -1;
        public static int TRANS_2H_Cast_03_b_SpellIdleOut = -1;
        public static int TRANS_2H_Cast_03_b_StandIdleTransition = -1;
        public static int TRANS_2H_Cast_03_c_SpellIdleOut = -1;
        public static int TRANS_2H_Cast_03_c_StandIdleTransition = -1;
        public static int TRANS_2H_Cast_04_SpellIdleOut = -1;
        public static int TRANS_2H_Cast_04_StandIdleTransition = -1;
        public static int TRANS_2H_Cast_04_a_2H_Cast_04_b = -1;
        public static int TRANS_2H_Cast_04_b_2H_Cast_04_c = -1;
        public static int TRANS_2H_Cast_04_b_SpellIdleOut = -1;
        public static int TRANS_2H_Cast_04_b_StandIdleTransition = -1;
        public static int TRANS_2H_Cast_04_c_SpellIdleOut = -1;
        public static int TRANS_2H_Cast_04_c_StandIdleTransition = -1;
        public static int TRANS_2H_Cast_05_SpellIdleOut = -1;
        public static int TRANS_2H_Cast_05_StandIdleTransition = -1;
        public static int TRANS_2H_Cast_05_a_2H_Cast_05_b = -1;
        public static int TRANS_2H_Cast_05_b_2H_Cast_05_c = -1;
        public static int TRANS_2H_Cast_05_b_SpellIdleOut = -1;
        public static int TRANS_2H_Cast_05_b_StandIdleTransition = -1;
        public static int TRANS_2H_Cast_05_c_SpellIdleOut = -1;
        public static int TRANS_2H_Cast_05_c_StandIdleTransition = -1;
        public static int TRANS_2H_Cast_06_SpellIdleOut = -1;
        public static int TRANS_2H_Cast_06_StandIdleTransition = -1;
        public static int TRANS_2H_Cast_06_a_2H_Cast_06_b = -1;
        public static int TRANS_2H_Cast_06_b_2H_Cast_06_c = -1;
        public static int TRANS_2H_Cast_06_b_SpellIdleOut = -1;
        public static int TRANS_2H_Cast_06_b_StandIdleTransition = -1;
        public static int TRANS_2H_Cast_06_c_SpellIdleOut = -1;
        public static int TRANS_2H_Cast_06_c_StandIdleTransition = -1;
        public static int TRANS_2H_Cast_07_SpellIdleOut = -1;
        public static int TRANS_2H_Cast_07_StandIdleTransition = -1;
        public static int TRANS_2H_Cast_07_a_2H_Cast_07_b = -1;
        public static int TRANS_2H_Cast_07_b_2H_Cast_07_c = -1;
        public static int TRANS_2H_Cast_07_b_SpellIdleOut = -1;
        public static int TRANS_2H_Cast_07_b_StandIdleTransition = -1;
        public static int TRANS_2H_Cast_07_c_SpellIdleOut = -1;
        public static int TRANS_2H_Cast_07_c_StandIdleTransition = -1;
        public static int TRANS_2H_Cast_08_SpellIdleOut = -1;
        public static int TRANS_2H_Cast_08_StandIdleTransition = -1;
        public static int TRANS_2H_Cast_08_a_2H_Cast_08_b = -1;
        public static int TRANS_2H_Cast_08_b_2H_Cast_08_c = -1;
        public static int TRANS_2H_Cast_08_b_SpellIdleOut = -1;
        public static int TRANS_2H_Cast_08_b_StandIdleTransition = -1;
        public static int TRANS_2H_Cast_08_c_SpellIdleOut = -1;
        public static int TRANS_2H_Cast_08_c_StandIdleTransition = -1;
        public static int TRANS_1H_Cast_03_SpellIdleOut = -1;
        public static int TRANS_1H_Cast_03_StandIdleTransition = -1;
        public static int TRANS_1H_Cast_03_a_1H_Cast_03_b = -1;
        public static int TRANS_1H_Cast_03_b_1H_Cast_03_c = -1;
        public static int TRANS_1H_Cast_03_b_SpellIdleOut = -1;
        public static int TRANS_1H_Cast_03_b_StandIdleTransition = -1;
        public static int TRANS_1H_Cast_03_c_SpellIdleOut = -1;
        public static int TRANS_1H_Cast_03_c_StandIdleTransition = -1;
        public static int TRANS_1H_Cast_04_SpellIdleOut = -1;
        public static int TRANS_1H_Cast_04_StandIdleTransition = -1;
        public static int TRANS_1H_Cast_04_a_1H_Cast_04_b = -1;
        public static int TRANS_1H_Cast_04_b_1H_Cast_04_c = -1;
        public static int TRANS_1H_Cast_04_b_SpellIdleOut = -1;
        public static int TRANS_1H_Cast_04_b_StandIdleTransition = -1;
        public static int TRANS_1H_Cast_04_c_SpellIdleOut = -1;
        public static int TRANS_1H_Cast_04_c_StandIdleTransition = -1;

        /// <summary>
        /// Determines if we're using auto-generated code
        /// </summary>
        public override bool HasAutoGeneratedCode
        {
            get { return true; }
        }

        /// <summary>
        /// Used to determine if the actor is in one of the states for this motion
        /// </summary>
        /// <returns></returns>
        public override bool IsInMotionState
        {
            get
            {
                int lStateID = mMotionLayer._AnimatorStateID;
                int lTransitionID = mMotionLayer._AnimatorTransitionID;

                if (lTransitionID == 0)
                {
                    if (lStateID == STATE_Start) { return true; }
                    if (lStateID == STATE_SpellIdleOut) { return true; }
                    if (lStateID == STATE_1H_Cast_01) { return true; }
                    if (lStateID == STATE_StandIdleIn) { return true; }
                    if (lStateID == STATE_StandIdleTransition) { return true; }
                    if (lStateID == STATE_StandIdleOut) { return true; }
                    if (lStateID == STATE_2H_Cast_01_a) { return true; }
                    if (lStateID == STATE_2H_Cast_01_c) { return true; }
                    if (lStateID == STATE_2H_Cast_01_b) { return true; }
                    if (lStateID == STATE_2H_Cast_01) { return true; }
                    if (lStateID == STATE_1H_Cast_01_a) { return true; }
                    if (lStateID == STATE_1H_Cast_01_b) { return true; }
                    if (lStateID == STATE_1H_Cast_01_c) { return true; }
                    if (lStateID == STATE_Interrupted) { return true; }
                    if (lStateID == STATE_1H_Cast_02) { return true; }
                    if (lStateID == STATE_1H_Cast_02_a) { return true; }
                    if (lStateID == STATE_1H_Cast_02_b) { return true; }
                    if (lStateID == STATE_1H_Cast_02_c) { return true; }
                    if (lStateID == STATE_2H_Cast_02) { return true; }
                    if (lStateID == STATE_2H_Cast_02_a) { return true; }
                    if (lStateID == STATE_2H_Cast_02_b) { return true; }
                    if (lStateID == STATE_2H_Cast_02_c) { return true; }
                    if (lStateID == STATE_2H_Cast_03) { return true; }
                    if (lStateID == STATE_2H_Cast_03_a) { return true; }
                    if (lStateID == STATE_2H_Cast_03_b) { return true; }
                    if (lStateID == STATE_2H_Cast_03_c) { return true; }
                    if (lStateID == STATE_2H_Cast_04) { return true; }
                    if (lStateID == STATE_2H_Cast_04_a) { return true; }
                    if (lStateID == STATE_2H_Cast_04_b) { return true; }
                    if (lStateID == STATE_2H_Cast_04_c) { return true; }
                    if (lStateID == STATE_2H_Cast_05) { return true; }
                    if (lStateID == STATE_2H_Cast_05_a) { return true; }
                    if (lStateID == STATE_2H_Cast_05_b) { return true; }
                    if (lStateID == STATE_2H_Cast_05_c) { return true; }
                    if (lStateID == STATE_2H_Cast_06) { return true; }
                    if (lStateID == STATE_2H_Cast_06_a) { return true; }
                    if (lStateID == STATE_2H_Cast_06_b) { return true; }
                    if (lStateID == STATE_2H_Cast_06_c) { return true; }
                    if (lStateID == STATE_2H_Cast_07) { return true; }
                    if (lStateID == STATE_2H_Cast_07_a) { return true; }
                    if (lStateID == STATE_2H_Cast_07_b) { return true; }
                    if (lStateID == STATE_2H_Cast_07_c) { return true; }
                    if (lStateID == STATE_2H_Cast_08) { return true; }
                    if (lStateID == STATE_2H_Cast_08_a) { return true; }
                    if (lStateID == STATE_2H_Cast_08_b) { return true; }
                    if (lStateID == STATE_2H_Cast_08_c) { return true; }
                    if (lStateID == STATE_1H_Cast_03) { return true; }
                    if (lStateID == STATE_1H_Cast_03_a) { return true; }
                    if (lStateID == STATE_1H_Cast_03_b) { return true; }
                    if (lStateID == STATE_1H_Cast_03_c) { return true; }
                    if (lStateID == STATE_1H_Cast_04) { return true; }
                    if (lStateID == STATE_1H_Cast_04_a) { return true; }
                    if (lStateID == STATE_1H_Cast_04_b) { return true; }
                    if (lStateID == STATE_1H_Cast_04_c) { return true; }
                }

                if (lTransitionID == TRANS_AnyState_StandIdleIn) { return true; }
                if (lTransitionID == TRANS_EntryState_StandIdleIn) { return true; }
                if (lTransitionID == TRANS_AnyState_1H_Cast_01) { return true; }
                if (lTransitionID == TRANS_EntryState_1H_Cast_01) { return true; }
                if (lTransitionID == TRANS_AnyState_2H_Cast_01_a) { return true; }
                if (lTransitionID == TRANS_EntryState_2H_Cast_01_a) { return true; }
                if (lTransitionID == TRANS_AnyState_2H_Cast_01) { return true; }
                if (lTransitionID == TRANS_EntryState_2H_Cast_01) { return true; }
                if (lTransitionID == TRANS_AnyState_1H_Cast_01_a) { return true; }
                if (lTransitionID == TRANS_EntryState_1H_Cast_01_a) { return true; }
                if (lTransitionID == TRANS_AnyState_Interrupted) { return true; }
                if (lTransitionID == TRANS_EntryState_Interrupted) { return true; }
                if (lTransitionID == TRANS_AnyState_1H_Cast_02) { return true; }
                if (lTransitionID == TRANS_EntryState_1H_Cast_02) { return true; }
                if (lTransitionID == TRANS_AnyState_1H_Cast_02_a) { return true; }
                if (lTransitionID == TRANS_EntryState_1H_Cast_02_a) { return true; }
                if (lTransitionID == TRANS_AnyState_2H_Cast_02) { return true; }
                if (lTransitionID == TRANS_EntryState_2H_Cast_02) { return true; }
                if (lTransitionID == TRANS_AnyState_1H_Cast_03) { return true; }
                if (lTransitionID == TRANS_EntryState_1H_Cast_03) { return true; }
                if (lTransitionID == TRANS_AnyState_1H_Cast_03_a) { return true; }
                if (lTransitionID == TRANS_EntryState_1H_Cast_03_a) { return true; }
                if (lTransitionID == TRANS_AnyState_1H_Cast_04) { return true; }
                if (lTransitionID == TRANS_EntryState_1H_Cast_04) { return true; }
                if (lTransitionID == TRANS_AnyState_1H_Cast_04_a) { return true; }
                if (lTransitionID == TRANS_EntryState_1H_Cast_04_a) { return true; }
                if (lTransitionID == TRANS_AnyState_2H_Cast_02_a) { return true; }
                if (lTransitionID == TRANS_EntryState_2H_Cast_02_a) { return true; }
                if (lTransitionID == TRANS_AnyState_2H_Cast_03) { return true; }
                if (lTransitionID == TRANS_EntryState_2H_Cast_03) { return true; }
                if (lTransitionID == TRANS_AnyState_2H_Cast_03_a) { return true; }
                if (lTransitionID == TRANS_EntryState_2H_Cast_03_a) { return true; }
                if (lTransitionID == TRANS_AnyState_2H_Cast_04) { return true; }
                if (lTransitionID == TRANS_EntryState_2H_Cast_04) { return true; }
                if (lTransitionID == TRANS_AnyState_2H_Cast_04_a) { return true; }
                if (lTransitionID == TRANS_EntryState_2H_Cast_04_a) { return true; }
                if (lTransitionID == TRANS_AnyState_2H_Cast_05) { return true; }
                if (lTransitionID == TRANS_EntryState_2H_Cast_05) { return true; }
                if (lTransitionID == TRANS_AnyState_2H_Cast_05_a) { return true; }
                if (lTransitionID == TRANS_EntryState_2H_Cast_05_a) { return true; }
                if (lTransitionID == TRANS_AnyState_2H_Cast_06) { return true; }
                if (lTransitionID == TRANS_EntryState_2H_Cast_06) { return true; }
                if (lTransitionID == TRANS_AnyState_2H_Cast_06_a) { return true; }
                if (lTransitionID == TRANS_EntryState_2H_Cast_06_a) { return true; }
                if (lTransitionID == TRANS_AnyState_2H_Cast_07) { return true; }
                if (lTransitionID == TRANS_EntryState_2H_Cast_07) { return true; }
                if (lTransitionID == TRANS_AnyState_2H_Cast_07_a) { return true; }
                if (lTransitionID == TRANS_EntryState_2H_Cast_07_a) { return true; }
                if (lTransitionID == TRANS_AnyState_2H_Cast_08) { return true; }
                if (lTransitionID == TRANS_EntryState_2H_Cast_08) { return true; }
                if (lTransitionID == TRANS_AnyState_2H_Cast_08_a) { return true; }
                if (lTransitionID == TRANS_EntryState_2H_Cast_08_a) { return true; }
                if (lTransitionID == TRANS_1H_Cast_01_SpellIdleOut) { return true; }
                if (lTransitionID == TRANS_1H_Cast_01_StandIdleTransition) { return true; }
                if (lTransitionID == TRANS_StandIdleIn_1H_Cast_01) { return true; }
                if (lTransitionID == TRANS_StandIdleIn_2H_Cast_01_a) { return true; }
                if (lTransitionID == TRANS_StandIdleIn_2H_Cast_01) { return true; }
                if (lTransitionID == TRANS_StandIdleIn_1H_Cast_01_a) { return true; }
                if (lTransitionID == TRANS_StandIdleIn_1H_Cast_02) { return true; }
                if (lTransitionID == TRANS_StandIdleIn_1H_Cast_02_a) { return true; }
                if (lTransitionID == TRANS_StandIdleIn_2H_Cast_02) { return true; }
                if (lTransitionID == TRANS_StandIdleIn_1H_Cast_03) { return true; }
                if (lTransitionID == TRANS_StandIdleIn_1H_Cast_03_a) { return true; }
                if (lTransitionID == TRANS_StandIdleIn_1H_Cast_04) { return true; }
                if (lTransitionID == TRANS_StandIdleIn_1H_Cast_04_a) { return true; }
                if (lTransitionID == TRANS_StandIdleIn_2H_Cast_02_a) { return true; }
                if (lTransitionID == TRANS_StandIdleIn_2H_Cast_03) { return true; }
                if (lTransitionID == TRANS_StandIdleIn_2H_Cast_03_a) { return true; }
                if (lTransitionID == TRANS_StandIdleIn_2H_Cast_04) { return true; }
                if (lTransitionID == TRANS_StandIdleIn_2H_Cast_04_a) { return true; }
                if (lTransitionID == TRANS_StandIdleIn_2H_Cast_05) { return true; }
                if (lTransitionID == TRANS_StandIdleIn_2H_Cast_05_a) { return true; }
                if (lTransitionID == TRANS_StandIdleIn_2H_Cast_06) { return true; }
                if (lTransitionID == TRANS_StandIdleIn_2H_Cast_06_a) { return true; }
                if (lTransitionID == TRANS_StandIdleIn_2H_Cast_07) { return true; }
                if (lTransitionID == TRANS_StandIdleIn_2H_Cast_07_a) { return true; }
                if (lTransitionID == TRANS_StandIdleIn_2H_Cast_08) { return true; }
                if (lTransitionID == TRANS_StandIdleIn_2H_Cast_08_a) { return true; }
                if (lTransitionID == TRANS_StandIdleTransition_StandIdleOut) { return true; }
                if (lTransitionID == TRANS_2H_Cast_01_a_2H_Cast_01_b) { return true; }
                if (lTransitionID == TRANS_2H_Cast_01_c_SpellIdleOut) { return true; }
                if (lTransitionID == TRANS_2H_Cast_01_c_StandIdleTransition) { return true; }
                if (lTransitionID == TRANS_2H_Cast_01_b_2H_Cast_01_c) { return true; }
                if (lTransitionID == TRANS_2H_Cast_01_b_StandIdleTransition) { return true; }
                if (lTransitionID == TRANS_2H_Cast_01_b_SpellIdleOut) { return true; }
                if (lTransitionID == TRANS_2H_Cast_01_SpellIdleOut) { return true; }
                if (lTransitionID == TRANS_2H_Cast_01_StandIdleTransition) { return true; }
                if (lTransitionID == TRANS_1H_Cast_01_a_1H_Cast_01_b) { return true; }
                if (lTransitionID == TRANS_1H_Cast_01_b_1H_Cast_01_c) { return true; }
                if (lTransitionID == TRANS_1H_Cast_01_b_SpellIdleOut) { return true; }
                if (lTransitionID == TRANS_1H_Cast_01_b_StandIdleTransition) { return true; }
                if (lTransitionID == TRANS_1H_Cast_01_c_SpellIdleOut) { return true; }
                if (lTransitionID == TRANS_1H_Cast_01_c_StandIdleTransition) { return true; }
                if (lTransitionID == TRANS_Interrupted_SpellIdleOut) { return true; }
                if (lTransitionID == TRANS_Interrupted_StandIdleTransition) { return true; }
                if (lTransitionID == TRANS_1H_Cast_02_SpellIdleOut) { return true; }
                if (lTransitionID == TRANS_1H_Cast_02_StandIdleTransition) { return true; }
                if (lTransitionID == TRANS_1H_Cast_02_a_1H_Cast_02_b) { return true; }
                if (lTransitionID == TRANS_1H_Cast_02_b_1H_Cast_02_c) { return true; }
                if (lTransitionID == TRANS_1H_Cast_02_b_SpellIdleOut) { return true; }
                if (lTransitionID == TRANS_1H_Cast_02_b_StandIdleTransition) { return true; }
                if (lTransitionID == TRANS_1H_Cast_02_c_SpellIdleOut) { return true; }
                if (lTransitionID == TRANS_1H_Cast_02_c_StandIdleTransition) { return true; }
                if (lTransitionID == TRANS_2H_Cast_02_SpellIdleOut) { return true; }
                if (lTransitionID == TRANS_2H_Cast_02_StandIdleTransition) { return true; }
                if (lTransitionID == TRANS_2H_Cast_02_a_2H_Cast_02_b) { return true; }
                if (lTransitionID == TRANS_2H_Cast_02_b_2H_Cast_02_c) { return true; }
                if (lTransitionID == TRANS_2H_Cast_02_b_SpellIdleOut) { return true; }
                if (lTransitionID == TRANS_2H_Cast_02_b_StandIdleTransition) { return true; }
                if (lTransitionID == TRANS_2H_Cast_02_c_SpellIdleOut) { return true; }
                if (lTransitionID == TRANS_2H_Cast_02_c_StandIdleTransition) { return true; }
                if (lTransitionID == TRANS_2H_Cast_03_SpellIdleOut) { return true; }
                if (lTransitionID == TRANS_2H_Cast_03_StandIdleTransition) { return true; }
                if (lTransitionID == TRANS_2H_Cast_03_a_2H_Cast_03_b) { return true; }
                if (lTransitionID == TRANS_2H_Cast_03_b_2H_Cast_03_c) { return true; }
                if (lTransitionID == TRANS_2H_Cast_03_b_SpellIdleOut) { return true; }
                if (lTransitionID == TRANS_2H_Cast_03_b_StandIdleTransition) { return true; }
                if (lTransitionID == TRANS_2H_Cast_03_c_SpellIdleOut) { return true; }
                if (lTransitionID == TRANS_2H_Cast_03_c_StandIdleTransition) { return true; }
                if (lTransitionID == TRANS_2H_Cast_04_SpellIdleOut) { return true; }
                if (lTransitionID == TRANS_2H_Cast_04_StandIdleTransition) { return true; }
                if (lTransitionID == TRANS_2H_Cast_04_a_2H_Cast_04_b) { return true; }
                if (lTransitionID == TRANS_2H_Cast_04_b_2H_Cast_04_c) { return true; }
                if (lTransitionID == TRANS_2H_Cast_04_b_SpellIdleOut) { return true; }
                if (lTransitionID == TRANS_2H_Cast_04_b_StandIdleTransition) { return true; }
                if (lTransitionID == TRANS_2H_Cast_04_c_SpellIdleOut) { return true; }
                if (lTransitionID == TRANS_2H_Cast_04_c_StandIdleTransition) { return true; }
                if (lTransitionID == TRANS_2H_Cast_05_SpellIdleOut) { return true; }
                if (lTransitionID == TRANS_2H_Cast_05_StandIdleTransition) { return true; }
                if (lTransitionID == TRANS_2H_Cast_05_a_2H_Cast_05_b) { return true; }
                if (lTransitionID == TRANS_2H_Cast_05_b_2H_Cast_05_c) { return true; }
                if (lTransitionID == TRANS_2H_Cast_05_b_SpellIdleOut) { return true; }
                if (lTransitionID == TRANS_2H_Cast_05_b_StandIdleTransition) { return true; }
                if (lTransitionID == TRANS_2H_Cast_05_c_SpellIdleOut) { return true; }
                if (lTransitionID == TRANS_2H_Cast_05_c_StandIdleTransition) { return true; }
                if (lTransitionID == TRANS_2H_Cast_06_SpellIdleOut) { return true; }
                if (lTransitionID == TRANS_2H_Cast_06_StandIdleTransition) { return true; }
                if (lTransitionID == TRANS_2H_Cast_06_a_2H_Cast_06_b) { return true; }
                if (lTransitionID == TRANS_2H_Cast_06_b_2H_Cast_06_c) { return true; }
                if (lTransitionID == TRANS_2H_Cast_06_b_SpellIdleOut) { return true; }
                if (lTransitionID == TRANS_2H_Cast_06_b_StandIdleTransition) { return true; }
                if (lTransitionID == TRANS_2H_Cast_06_c_SpellIdleOut) { return true; }
                if (lTransitionID == TRANS_2H_Cast_06_c_StandIdleTransition) { return true; }
                if (lTransitionID == TRANS_2H_Cast_07_SpellIdleOut) { return true; }
                if (lTransitionID == TRANS_2H_Cast_07_StandIdleTransition) { return true; }
                if (lTransitionID == TRANS_2H_Cast_07_a_2H_Cast_07_b) { return true; }
                if (lTransitionID == TRANS_2H_Cast_07_b_2H_Cast_07_c) { return true; }
                if (lTransitionID == TRANS_2H_Cast_07_b_SpellIdleOut) { return true; }
                if (lTransitionID == TRANS_2H_Cast_07_b_StandIdleTransition) { return true; }
                if (lTransitionID == TRANS_2H_Cast_07_c_SpellIdleOut) { return true; }
                if (lTransitionID == TRANS_2H_Cast_07_c_StandIdleTransition) { return true; }
                if (lTransitionID == TRANS_2H_Cast_08_SpellIdleOut) { return true; }
                if (lTransitionID == TRANS_2H_Cast_08_StandIdleTransition) { return true; }
                if (lTransitionID == TRANS_2H_Cast_08_a_2H_Cast_08_b) { return true; }
                if (lTransitionID == TRANS_2H_Cast_08_b_2H_Cast_08_c) { return true; }
                if (lTransitionID == TRANS_2H_Cast_08_b_SpellIdleOut) { return true; }
                if (lTransitionID == TRANS_2H_Cast_08_b_StandIdleTransition) { return true; }
                if (lTransitionID == TRANS_2H_Cast_08_c_SpellIdleOut) { return true; }
                if (lTransitionID == TRANS_2H_Cast_08_c_StandIdleTransition) { return true; }
                if (lTransitionID == TRANS_1H_Cast_03_SpellIdleOut) { return true; }
                if (lTransitionID == TRANS_1H_Cast_03_StandIdleTransition) { return true; }
                if (lTransitionID == TRANS_1H_Cast_03_a_1H_Cast_03_b) { return true; }
                if (lTransitionID == TRANS_1H_Cast_03_b_1H_Cast_03_c) { return true; }
                if (lTransitionID == TRANS_1H_Cast_03_b_SpellIdleOut) { return true; }
                if (lTransitionID == TRANS_1H_Cast_03_b_StandIdleTransition) { return true; }
                if (lTransitionID == TRANS_1H_Cast_03_c_SpellIdleOut) { return true; }
                if (lTransitionID == TRANS_1H_Cast_03_c_StandIdleTransition) { return true; }
                if (lTransitionID == TRANS_1H_Cast_04_SpellIdleOut) { return true; }
                if (lTransitionID == TRANS_1H_Cast_04_StandIdleTransition) { return true; }
                if (lTransitionID == TRANS_1H_Cast_04_a_1H_Cast_04_b) { return true; }
                if (lTransitionID == TRANS_1H_Cast_04_b_1H_Cast_04_c) { return true; }
                if (lTransitionID == TRANS_1H_Cast_04_b_SpellIdleOut) { return true; }
                if (lTransitionID == TRANS_1H_Cast_04_b_StandIdleTransition) { return true; }
                if (lTransitionID == TRANS_1H_Cast_04_c_SpellIdleOut) { return true; }
                if (lTransitionID == TRANS_1H_Cast_04_c_StandIdleTransition) { return true; }
                return false;
            }
        }

        /// <summary>
        /// Used to determine if the actor is in one of the states for this motion
        /// </summary>
        /// <returns></returns>
        public override bool IsMotionState(int rStateID)
        {
            if (rStateID == STATE_Start) { return true; }
            if (rStateID == STATE_SpellIdleOut) { return true; }
            if (rStateID == STATE_1H_Cast_01) { return true; }
            if (rStateID == STATE_StandIdleIn) { return true; }
            if (rStateID == STATE_StandIdleTransition) { return true; }
            if (rStateID == STATE_StandIdleOut) { return true; }
            if (rStateID == STATE_2H_Cast_01_a) { return true; }
            if (rStateID == STATE_2H_Cast_01_c) { return true; }
            if (rStateID == STATE_2H_Cast_01_b) { return true; }
            if (rStateID == STATE_2H_Cast_01) { return true; }
            if (rStateID == STATE_1H_Cast_01_a) { return true; }
            if (rStateID == STATE_1H_Cast_01_b) { return true; }
            if (rStateID == STATE_1H_Cast_01_c) { return true; }
            if (rStateID == STATE_Interrupted) { return true; }
            if (rStateID == STATE_1H_Cast_02) { return true; }
            if (rStateID == STATE_1H_Cast_02_a) { return true; }
            if (rStateID == STATE_1H_Cast_02_b) { return true; }
            if (rStateID == STATE_1H_Cast_02_c) { return true; }
            if (rStateID == STATE_2H_Cast_02) { return true; }
            if (rStateID == STATE_2H_Cast_02_a) { return true; }
            if (rStateID == STATE_2H_Cast_02_b) { return true; }
            if (rStateID == STATE_2H_Cast_02_c) { return true; }
            if (rStateID == STATE_2H_Cast_03) { return true; }
            if (rStateID == STATE_2H_Cast_03_a) { return true; }
            if (rStateID == STATE_2H_Cast_03_b) { return true; }
            if (rStateID == STATE_2H_Cast_03_c) { return true; }
            if (rStateID == STATE_2H_Cast_04) { return true; }
            if (rStateID == STATE_2H_Cast_04_a) { return true; }
            if (rStateID == STATE_2H_Cast_04_b) { return true; }
            if (rStateID == STATE_2H_Cast_04_c) { return true; }
            if (rStateID == STATE_2H_Cast_05) { return true; }
            if (rStateID == STATE_2H_Cast_05_a) { return true; }
            if (rStateID == STATE_2H_Cast_05_b) { return true; }
            if (rStateID == STATE_2H_Cast_05_c) { return true; }
            if (rStateID == STATE_2H_Cast_06) { return true; }
            if (rStateID == STATE_2H_Cast_06_a) { return true; }
            if (rStateID == STATE_2H_Cast_06_b) { return true; }
            if (rStateID == STATE_2H_Cast_06_c) { return true; }
            if (rStateID == STATE_2H_Cast_07) { return true; }
            if (rStateID == STATE_2H_Cast_07_a) { return true; }
            if (rStateID == STATE_2H_Cast_07_b) { return true; }
            if (rStateID == STATE_2H_Cast_07_c) { return true; }
            if (rStateID == STATE_2H_Cast_08) { return true; }
            if (rStateID == STATE_2H_Cast_08_a) { return true; }
            if (rStateID == STATE_2H_Cast_08_b) { return true; }
            if (rStateID == STATE_2H_Cast_08_c) { return true; }
            if (rStateID == STATE_1H_Cast_03) { return true; }
            if (rStateID == STATE_1H_Cast_03_a) { return true; }
            if (rStateID == STATE_1H_Cast_03_b) { return true; }
            if (rStateID == STATE_1H_Cast_03_c) { return true; }
            if (rStateID == STATE_1H_Cast_04) { return true; }
            if (rStateID == STATE_1H_Cast_04_a) { return true; }
            if (rStateID == STATE_1H_Cast_04_b) { return true; }
            if (rStateID == STATE_1H_Cast_04_c) { return true; }
            return false;
        }

        /// <summary>
        /// Used to determine if the actor is in one of the states for this motion
        /// </summary>
        /// <returns></returns>
        public override bool IsMotionState(int rStateID, int rTransitionID)
        {
            if (rTransitionID == 0)
            {
                if (rStateID == STATE_Start) { return true; }
                if (rStateID == STATE_SpellIdleOut) { return true; }
                if (rStateID == STATE_1H_Cast_01) { return true; }
                if (rStateID == STATE_StandIdleIn) { return true; }
                if (rStateID == STATE_StandIdleTransition) { return true; }
                if (rStateID == STATE_StandIdleOut) { return true; }
                if (rStateID == STATE_2H_Cast_01_a) { return true; }
                if (rStateID == STATE_2H_Cast_01_c) { return true; }
                if (rStateID == STATE_2H_Cast_01_b) { return true; }
                if (rStateID == STATE_2H_Cast_01) { return true; }
                if (rStateID == STATE_1H_Cast_01_a) { return true; }
                if (rStateID == STATE_1H_Cast_01_b) { return true; }
                if (rStateID == STATE_1H_Cast_01_c) { return true; }
                if (rStateID == STATE_Interrupted) { return true; }
                if (rStateID == STATE_1H_Cast_02) { return true; }
                if (rStateID == STATE_1H_Cast_02_a) { return true; }
                if (rStateID == STATE_1H_Cast_02_b) { return true; }
                if (rStateID == STATE_1H_Cast_02_c) { return true; }
                if (rStateID == STATE_2H_Cast_02) { return true; }
                if (rStateID == STATE_2H_Cast_02_a) { return true; }
                if (rStateID == STATE_2H_Cast_02_b) { return true; }
                if (rStateID == STATE_2H_Cast_02_c) { return true; }
                if (rStateID == STATE_2H_Cast_03) { return true; }
                if (rStateID == STATE_2H_Cast_03_a) { return true; }
                if (rStateID == STATE_2H_Cast_03_b) { return true; }
                if (rStateID == STATE_2H_Cast_03_c) { return true; }
                if (rStateID == STATE_2H_Cast_04) { return true; }
                if (rStateID == STATE_2H_Cast_04_a) { return true; }
                if (rStateID == STATE_2H_Cast_04_b) { return true; }
                if (rStateID == STATE_2H_Cast_04_c) { return true; }
                if (rStateID == STATE_2H_Cast_05) { return true; }
                if (rStateID == STATE_2H_Cast_05_a) { return true; }
                if (rStateID == STATE_2H_Cast_05_b) { return true; }
                if (rStateID == STATE_2H_Cast_05_c) { return true; }
                if (rStateID == STATE_2H_Cast_06) { return true; }
                if (rStateID == STATE_2H_Cast_06_a) { return true; }
                if (rStateID == STATE_2H_Cast_06_b) { return true; }
                if (rStateID == STATE_2H_Cast_06_c) { return true; }
                if (rStateID == STATE_2H_Cast_07) { return true; }
                if (rStateID == STATE_2H_Cast_07_a) { return true; }
                if (rStateID == STATE_2H_Cast_07_b) { return true; }
                if (rStateID == STATE_2H_Cast_07_c) { return true; }
                if (rStateID == STATE_2H_Cast_08) { return true; }
                if (rStateID == STATE_2H_Cast_08_a) { return true; }
                if (rStateID == STATE_2H_Cast_08_b) { return true; }
                if (rStateID == STATE_2H_Cast_08_c) { return true; }
                if (rStateID == STATE_1H_Cast_03) { return true; }
                if (rStateID == STATE_1H_Cast_03_a) { return true; }
                if (rStateID == STATE_1H_Cast_03_b) { return true; }
                if (rStateID == STATE_1H_Cast_03_c) { return true; }
                if (rStateID == STATE_1H_Cast_04) { return true; }
                if (rStateID == STATE_1H_Cast_04_a) { return true; }
                if (rStateID == STATE_1H_Cast_04_b) { return true; }
                if (rStateID == STATE_1H_Cast_04_c) { return true; }
            }

            if (rTransitionID == TRANS_AnyState_StandIdleIn) { return true; }
            if (rTransitionID == TRANS_EntryState_StandIdleIn) { return true; }
            if (rTransitionID == TRANS_AnyState_1H_Cast_01) { return true; }
            if (rTransitionID == TRANS_EntryState_1H_Cast_01) { return true; }
            if (rTransitionID == TRANS_AnyState_2H_Cast_01_a) { return true; }
            if (rTransitionID == TRANS_EntryState_2H_Cast_01_a) { return true; }
            if (rTransitionID == TRANS_AnyState_2H_Cast_01) { return true; }
            if (rTransitionID == TRANS_EntryState_2H_Cast_01) { return true; }
            if (rTransitionID == TRANS_AnyState_1H_Cast_01_a) { return true; }
            if (rTransitionID == TRANS_EntryState_1H_Cast_01_a) { return true; }
            if (rTransitionID == TRANS_AnyState_Interrupted) { return true; }
            if (rTransitionID == TRANS_EntryState_Interrupted) { return true; }
            if (rTransitionID == TRANS_AnyState_1H_Cast_02) { return true; }
            if (rTransitionID == TRANS_EntryState_1H_Cast_02) { return true; }
            if (rTransitionID == TRANS_AnyState_1H_Cast_02_a) { return true; }
            if (rTransitionID == TRANS_EntryState_1H_Cast_02_a) { return true; }
            if (rTransitionID == TRANS_AnyState_2H_Cast_02) { return true; }
            if (rTransitionID == TRANS_EntryState_2H_Cast_02) { return true; }
            if (rTransitionID == TRANS_AnyState_1H_Cast_03) { return true; }
            if (rTransitionID == TRANS_EntryState_1H_Cast_03) { return true; }
            if (rTransitionID == TRANS_AnyState_1H_Cast_03_a) { return true; }
            if (rTransitionID == TRANS_EntryState_1H_Cast_03_a) { return true; }
            if (rTransitionID == TRANS_AnyState_1H_Cast_04) { return true; }
            if (rTransitionID == TRANS_EntryState_1H_Cast_04) { return true; }
            if (rTransitionID == TRANS_AnyState_1H_Cast_04_a) { return true; }
            if (rTransitionID == TRANS_EntryState_1H_Cast_04_a) { return true; }
            if (rTransitionID == TRANS_AnyState_2H_Cast_02_a) { return true; }
            if (rTransitionID == TRANS_EntryState_2H_Cast_02_a) { return true; }
            if (rTransitionID == TRANS_AnyState_2H_Cast_03) { return true; }
            if (rTransitionID == TRANS_EntryState_2H_Cast_03) { return true; }
            if (rTransitionID == TRANS_AnyState_2H_Cast_03_a) { return true; }
            if (rTransitionID == TRANS_EntryState_2H_Cast_03_a) { return true; }
            if (rTransitionID == TRANS_AnyState_2H_Cast_04) { return true; }
            if (rTransitionID == TRANS_EntryState_2H_Cast_04) { return true; }
            if (rTransitionID == TRANS_AnyState_2H_Cast_04_a) { return true; }
            if (rTransitionID == TRANS_EntryState_2H_Cast_04_a) { return true; }
            if (rTransitionID == TRANS_AnyState_2H_Cast_05) { return true; }
            if (rTransitionID == TRANS_EntryState_2H_Cast_05) { return true; }
            if (rTransitionID == TRANS_AnyState_2H_Cast_05_a) { return true; }
            if (rTransitionID == TRANS_EntryState_2H_Cast_05_a) { return true; }
            if (rTransitionID == TRANS_AnyState_2H_Cast_06) { return true; }
            if (rTransitionID == TRANS_EntryState_2H_Cast_06) { return true; }
            if (rTransitionID == TRANS_AnyState_2H_Cast_06_a) { return true; }
            if (rTransitionID == TRANS_EntryState_2H_Cast_06_a) { return true; }
            if (rTransitionID == TRANS_AnyState_2H_Cast_07) { return true; }
            if (rTransitionID == TRANS_EntryState_2H_Cast_07) { return true; }
            if (rTransitionID == TRANS_AnyState_2H_Cast_07_a) { return true; }
            if (rTransitionID == TRANS_EntryState_2H_Cast_07_a) { return true; }
            if (rTransitionID == TRANS_AnyState_2H_Cast_08) { return true; }
            if (rTransitionID == TRANS_EntryState_2H_Cast_08) { return true; }
            if (rTransitionID == TRANS_AnyState_2H_Cast_08_a) { return true; }
            if (rTransitionID == TRANS_EntryState_2H_Cast_08_a) { return true; }
            if (rTransitionID == TRANS_1H_Cast_01_SpellIdleOut) { return true; }
            if (rTransitionID == TRANS_1H_Cast_01_StandIdleTransition) { return true; }
            if (rTransitionID == TRANS_StandIdleIn_1H_Cast_01) { return true; }
            if (rTransitionID == TRANS_StandIdleIn_2H_Cast_01_a) { return true; }
            if (rTransitionID == TRANS_StandIdleIn_2H_Cast_01) { return true; }
            if (rTransitionID == TRANS_StandIdleIn_1H_Cast_01_a) { return true; }
            if (rTransitionID == TRANS_StandIdleIn_1H_Cast_02) { return true; }
            if (rTransitionID == TRANS_StandIdleIn_1H_Cast_02_a) { return true; }
            if (rTransitionID == TRANS_StandIdleIn_2H_Cast_02) { return true; }
            if (rTransitionID == TRANS_StandIdleIn_1H_Cast_03) { return true; }
            if (rTransitionID == TRANS_StandIdleIn_1H_Cast_03_a) { return true; }
            if (rTransitionID == TRANS_StandIdleIn_1H_Cast_04) { return true; }
            if (rTransitionID == TRANS_StandIdleIn_1H_Cast_04_a) { return true; }
            if (rTransitionID == TRANS_StandIdleIn_2H_Cast_02_a) { return true; }
            if (rTransitionID == TRANS_StandIdleIn_2H_Cast_03) { return true; }
            if (rTransitionID == TRANS_StandIdleIn_2H_Cast_03_a) { return true; }
            if (rTransitionID == TRANS_StandIdleIn_2H_Cast_04) { return true; }
            if (rTransitionID == TRANS_StandIdleIn_2H_Cast_04_a) { return true; }
            if (rTransitionID == TRANS_StandIdleIn_2H_Cast_05) { return true; }
            if (rTransitionID == TRANS_StandIdleIn_2H_Cast_05_a) { return true; }
            if (rTransitionID == TRANS_StandIdleIn_2H_Cast_06) { return true; }
            if (rTransitionID == TRANS_StandIdleIn_2H_Cast_06_a) { return true; }
            if (rTransitionID == TRANS_StandIdleIn_2H_Cast_07) { return true; }
            if (rTransitionID == TRANS_StandIdleIn_2H_Cast_07_a) { return true; }
            if (rTransitionID == TRANS_StandIdleIn_2H_Cast_08) { return true; }
            if (rTransitionID == TRANS_StandIdleIn_2H_Cast_08_a) { return true; }
            if (rTransitionID == TRANS_StandIdleTransition_StandIdleOut) { return true; }
            if (rTransitionID == TRANS_2H_Cast_01_a_2H_Cast_01_b) { return true; }
            if (rTransitionID == TRANS_2H_Cast_01_c_SpellIdleOut) { return true; }
            if (rTransitionID == TRANS_2H_Cast_01_c_StandIdleTransition) { return true; }
            if (rTransitionID == TRANS_2H_Cast_01_b_2H_Cast_01_c) { return true; }
            if (rTransitionID == TRANS_2H_Cast_01_b_StandIdleTransition) { return true; }
            if (rTransitionID == TRANS_2H_Cast_01_b_SpellIdleOut) { return true; }
            if (rTransitionID == TRANS_2H_Cast_01_SpellIdleOut) { return true; }
            if (rTransitionID == TRANS_2H_Cast_01_StandIdleTransition) { return true; }
            if (rTransitionID == TRANS_1H_Cast_01_a_1H_Cast_01_b) { return true; }
            if (rTransitionID == TRANS_1H_Cast_01_b_1H_Cast_01_c) { return true; }
            if (rTransitionID == TRANS_1H_Cast_01_b_SpellIdleOut) { return true; }
            if (rTransitionID == TRANS_1H_Cast_01_b_StandIdleTransition) { return true; }
            if (rTransitionID == TRANS_1H_Cast_01_c_SpellIdleOut) { return true; }
            if (rTransitionID == TRANS_1H_Cast_01_c_StandIdleTransition) { return true; }
            if (rTransitionID == TRANS_Interrupted_SpellIdleOut) { return true; }
            if (rTransitionID == TRANS_Interrupted_StandIdleTransition) { return true; }
            if (rTransitionID == TRANS_1H_Cast_02_SpellIdleOut) { return true; }
            if (rTransitionID == TRANS_1H_Cast_02_StandIdleTransition) { return true; }
            if (rTransitionID == TRANS_1H_Cast_02_a_1H_Cast_02_b) { return true; }
            if (rTransitionID == TRANS_1H_Cast_02_b_1H_Cast_02_c) { return true; }
            if (rTransitionID == TRANS_1H_Cast_02_b_SpellIdleOut) { return true; }
            if (rTransitionID == TRANS_1H_Cast_02_b_StandIdleTransition) { return true; }
            if (rTransitionID == TRANS_1H_Cast_02_c_SpellIdleOut) { return true; }
            if (rTransitionID == TRANS_1H_Cast_02_c_StandIdleTransition) { return true; }
            if (rTransitionID == TRANS_2H_Cast_02_SpellIdleOut) { return true; }
            if (rTransitionID == TRANS_2H_Cast_02_StandIdleTransition) { return true; }
            if (rTransitionID == TRANS_2H_Cast_02_a_2H_Cast_02_b) { return true; }
            if (rTransitionID == TRANS_2H_Cast_02_b_2H_Cast_02_c) { return true; }
            if (rTransitionID == TRANS_2H_Cast_02_b_SpellIdleOut) { return true; }
            if (rTransitionID == TRANS_2H_Cast_02_b_StandIdleTransition) { return true; }
            if (rTransitionID == TRANS_2H_Cast_02_c_SpellIdleOut) { return true; }
            if (rTransitionID == TRANS_2H_Cast_02_c_StandIdleTransition) { return true; }
            if (rTransitionID == TRANS_2H_Cast_03_SpellIdleOut) { return true; }
            if (rTransitionID == TRANS_2H_Cast_03_StandIdleTransition) { return true; }
            if (rTransitionID == TRANS_2H_Cast_03_a_2H_Cast_03_b) { return true; }
            if (rTransitionID == TRANS_2H_Cast_03_b_2H_Cast_03_c) { return true; }
            if (rTransitionID == TRANS_2H_Cast_03_b_SpellIdleOut) { return true; }
            if (rTransitionID == TRANS_2H_Cast_03_b_StandIdleTransition) { return true; }
            if (rTransitionID == TRANS_2H_Cast_03_c_SpellIdleOut) { return true; }
            if (rTransitionID == TRANS_2H_Cast_03_c_StandIdleTransition) { return true; }
            if (rTransitionID == TRANS_2H_Cast_04_SpellIdleOut) { return true; }
            if (rTransitionID == TRANS_2H_Cast_04_StandIdleTransition) { return true; }
            if (rTransitionID == TRANS_2H_Cast_04_a_2H_Cast_04_b) { return true; }
            if (rTransitionID == TRANS_2H_Cast_04_b_2H_Cast_04_c) { return true; }
            if (rTransitionID == TRANS_2H_Cast_04_b_SpellIdleOut) { return true; }
            if (rTransitionID == TRANS_2H_Cast_04_b_StandIdleTransition) { return true; }
            if (rTransitionID == TRANS_2H_Cast_04_c_SpellIdleOut) { return true; }
            if (rTransitionID == TRANS_2H_Cast_04_c_StandIdleTransition) { return true; }
            if (rTransitionID == TRANS_2H_Cast_05_SpellIdleOut) { return true; }
            if (rTransitionID == TRANS_2H_Cast_05_StandIdleTransition) { return true; }
            if (rTransitionID == TRANS_2H_Cast_05_a_2H_Cast_05_b) { return true; }
            if (rTransitionID == TRANS_2H_Cast_05_b_2H_Cast_05_c) { return true; }
            if (rTransitionID == TRANS_2H_Cast_05_b_SpellIdleOut) { return true; }
            if (rTransitionID == TRANS_2H_Cast_05_b_StandIdleTransition) { return true; }
            if (rTransitionID == TRANS_2H_Cast_05_c_SpellIdleOut) { return true; }
            if (rTransitionID == TRANS_2H_Cast_05_c_StandIdleTransition) { return true; }
            if (rTransitionID == TRANS_2H_Cast_06_SpellIdleOut) { return true; }
            if (rTransitionID == TRANS_2H_Cast_06_StandIdleTransition) { return true; }
            if (rTransitionID == TRANS_2H_Cast_06_a_2H_Cast_06_b) { return true; }
            if (rTransitionID == TRANS_2H_Cast_06_b_2H_Cast_06_c) { return true; }
            if (rTransitionID == TRANS_2H_Cast_06_b_SpellIdleOut) { return true; }
            if (rTransitionID == TRANS_2H_Cast_06_b_StandIdleTransition) { return true; }
            if (rTransitionID == TRANS_2H_Cast_06_c_SpellIdleOut) { return true; }
            if (rTransitionID == TRANS_2H_Cast_06_c_StandIdleTransition) { return true; }
            if (rTransitionID == TRANS_2H_Cast_07_SpellIdleOut) { return true; }
            if (rTransitionID == TRANS_2H_Cast_07_StandIdleTransition) { return true; }
            if (rTransitionID == TRANS_2H_Cast_07_a_2H_Cast_07_b) { return true; }
            if (rTransitionID == TRANS_2H_Cast_07_b_2H_Cast_07_c) { return true; }
            if (rTransitionID == TRANS_2H_Cast_07_b_SpellIdleOut) { return true; }
            if (rTransitionID == TRANS_2H_Cast_07_b_StandIdleTransition) { return true; }
            if (rTransitionID == TRANS_2H_Cast_07_c_SpellIdleOut) { return true; }
            if (rTransitionID == TRANS_2H_Cast_07_c_StandIdleTransition) { return true; }
            if (rTransitionID == TRANS_2H_Cast_08_SpellIdleOut) { return true; }
            if (rTransitionID == TRANS_2H_Cast_08_StandIdleTransition) { return true; }
            if (rTransitionID == TRANS_2H_Cast_08_a_2H_Cast_08_b) { return true; }
            if (rTransitionID == TRANS_2H_Cast_08_b_2H_Cast_08_c) { return true; }
            if (rTransitionID == TRANS_2H_Cast_08_b_SpellIdleOut) { return true; }
            if (rTransitionID == TRANS_2H_Cast_08_b_StandIdleTransition) { return true; }
            if (rTransitionID == TRANS_2H_Cast_08_c_SpellIdleOut) { return true; }
            if (rTransitionID == TRANS_2H_Cast_08_c_StandIdleTransition) { return true; }
            if (rTransitionID == TRANS_1H_Cast_03_SpellIdleOut) { return true; }
            if (rTransitionID == TRANS_1H_Cast_03_StandIdleTransition) { return true; }
            if (rTransitionID == TRANS_1H_Cast_03_a_1H_Cast_03_b) { return true; }
            if (rTransitionID == TRANS_1H_Cast_03_b_1H_Cast_03_c) { return true; }
            if (rTransitionID == TRANS_1H_Cast_03_b_SpellIdleOut) { return true; }
            if (rTransitionID == TRANS_1H_Cast_03_b_StandIdleTransition) { return true; }
            if (rTransitionID == TRANS_1H_Cast_03_c_SpellIdleOut) { return true; }
            if (rTransitionID == TRANS_1H_Cast_03_c_StandIdleTransition) { return true; }
            if (rTransitionID == TRANS_1H_Cast_04_SpellIdleOut) { return true; }
            if (rTransitionID == TRANS_1H_Cast_04_StandIdleTransition) { return true; }
            if (rTransitionID == TRANS_1H_Cast_04_a_1H_Cast_04_b) { return true; }
            if (rTransitionID == TRANS_1H_Cast_04_b_1H_Cast_04_c) { return true; }
            if (rTransitionID == TRANS_1H_Cast_04_b_SpellIdleOut) { return true; }
            if (rTransitionID == TRANS_1H_Cast_04_b_StandIdleTransition) { return true; }
            if (rTransitionID == TRANS_1H_Cast_04_c_SpellIdleOut) { return true; }
            if (rTransitionID == TRANS_1H_Cast_04_c_StandIdleTransition) { return true; }
            return false;
        }

        /// <summary>
        /// Preprocess any animator data so the motion can use it later
        /// </summary>
        public override void LoadAnimatorData()
        {
            TRANS_AnyState_StandIdleIn = mMotionController.AddAnimatorName("AnyState -> Base Layer.PMP_BasicSpellCastings-SM.Stand Idle In");
            TRANS_EntryState_StandIdleIn = mMotionController.AddAnimatorName("Entry -> Base Layer.PMP_BasicSpellCastings-SM.Stand Idle In");
            TRANS_AnyState_1H_Cast_01 = mMotionController.AddAnimatorName("AnyState -> Base Layer.PMP_BasicSpellCastings-SM.1H_Cast_01");
            TRANS_EntryState_1H_Cast_01 = mMotionController.AddAnimatorName("Entry -> Base Layer.PMP_BasicSpellCastings-SM.1H_Cast_01");
            TRANS_AnyState_2H_Cast_01_a = mMotionController.AddAnimatorName("AnyState -> Base Layer.PMP_BasicSpellCastings-SM.2H_Cast_01_a");
            TRANS_EntryState_2H_Cast_01_a = mMotionController.AddAnimatorName("Entry -> Base Layer.PMP_BasicSpellCastings-SM.2H_Cast_01_a");
            TRANS_AnyState_2H_Cast_01 = mMotionController.AddAnimatorName("AnyState -> Base Layer.PMP_BasicSpellCastings-SM.2H_Cast_01");
            TRANS_EntryState_2H_Cast_01 = mMotionController.AddAnimatorName("Entry -> Base Layer.PMP_BasicSpellCastings-SM.2H_Cast_01");
            TRANS_AnyState_1H_Cast_01_a = mMotionController.AddAnimatorName("AnyState -> Base Layer.PMP_BasicSpellCastings-SM.1H_Cast_01_a");
            TRANS_EntryState_1H_Cast_01_a = mMotionController.AddAnimatorName("Entry -> Base Layer.PMP_BasicSpellCastings-SM.1H_Cast_01_a");
            TRANS_AnyState_Interrupted = mMotionController.AddAnimatorName("AnyState -> Base Layer.PMP_BasicSpellCastings-SM.Interrupted");
            TRANS_EntryState_Interrupted = mMotionController.AddAnimatorName("Entry -> Base Layer.PMP_BasicSpellCastings-SM.Interrupted");
            TRANS_AnyState_1H_Cast_02 = mMotionController.AddAnimatorName("AnyState -> Base Layer.PMP_BasicSpellCastings-SM.1H_Cast_02");
            TRANS_EntryState_1H_Cast_02 = mMotionController.AddAnimatorName("Entry -> Base Layer.PMP_BasicSpellCastings-SM.1H_Cast_02");
            TRANS_AnyState_1H_Cast_02_a = mMotionController.AddAnimatorName("AnyState -> Base Layer.PMP_BasicSpellCastings-SM.1H_Cast_02_a");
            TRANS_EntryState_1H_Cast_02_a = mMotionController.AddAnimatorName("Entry -> Base Layer.PMP_BasicSpellCastings-SM.1H_Cast_02_a");
            TRANS_AnyState_2H_Cast_02 = mMotionController.AddAnimatorName("AnyState -> Base Layer.PMP_BasicSpellCastings-SM.2H_Cast_02");
            TRANS_EntryState_2H_Cast_02 = mMotionController.AddAnimatorName("Entry -> Base Layer.PMP_BasicSpellCastings-SM.2H_Cast_02");
            TRANS_AnyState_1H_Cast_03 = mMotionController.AddAnimatorName("AnyState -> Base Layer.PMP_BasicSpellCastings-SM.1H_Cast_03");
            TRANS_EntryState_1H_Cast_03 = mMotionController.AddAnimatorName("Entry -> Base Layer.PMP_BasicSpellCastings-SM.1H_Cast_03");
            TRANS_AnyState_1H_Cast_03_a = mMotionController.AddAnimatorName("AnyState -> Base Layer.PMP_BasicSpellCastings-SM.1H_Cast_03_a");
            TRANS_EntryState_1H_Cast_03_a = mMotionController.AddAnimatorName("Entry -> Base Layer.PMP_BasicSpellCastings-SM.1H_Cast_03_a");
            TRANS_AnyState_1H_Cast_04 = mMotionController.AddAnimatorName("AnyState -> Base Layer.PMP_BasicSpellCastings-SM.1H_Cast_04");
            TRANS_EntryState_1H_Cast_04 = mMotionController.AddAnimatorName("Entry -> Base Layer.PMP_BasicSpellCastings-SM.1H_Cast_04");
            TRANS_AnyState_1H_Cast_04_a = mMotionController.AddAnimatorName("AnyState -> Base Layer.PMP_BasicSpellCastings-SM.1H_Cast_04_a");
            TRANS_EntryState_1H_Cast_04_a = mMotionController.AddAnimatorName("Entry -> Base Layer.PMP_BasicSpellCastings-SM.1H_Cast_04_a");
            TRANS_AnyState_2H_Cast_02_a = mMotionController.AddAnimatorName("AnyState -> Base Layer.PMP_BasicSpellCastings-SM.2H_Cast_02_a");
            TRANS_EntryState_2H_Cast_02_a = mMotionController.AddAnimatorName("Entry -> Base Layer.PMP_BasicSpellCastings-SM.2H_Cast_02_a");
            TRANS_AnyState_2H_Cast_03 = mMotionController.AddAnimatorName("AnyState -> Base Layer.PMP_BasicSpellCastings-SM.2H_Cast_03");
            TRANS_EntryState_2H_Cast_03 = mMotionController.AddAnimatorName("Entry -> Base Layer.PMP_BasicSpellCastings-SM.2H_Cast_03");
            TRANS_AnyState_2H_Cast_03_a = mMotionController.AddAnimatorName("AnyState -> Base Layer.PMP_BasicSpellCastings-SM.2H_Cast_03_a");
            TRANS_EntryState_2H_Cast_03_a = mMotionController.AddAnimatorName("Entry -> Base Layer.PMP_BasicSpellCastings-SM.2H_Cast_03_a");
            TRANS_AnyState_2H_Cast_04 = mMotionController.AddAnimatorName("AnyState -> Base Layer.PMP_BasicSpellCastings-SM.2H_Cast_04");
            TRANS_EntryState_2H_Cast_04 = mMotionController.AddAnimatorName("Entry -> Base Layer.PMP_BasicSpellCastings-SM.2H_Cast_04");
            TRANS_AnyState_2H_Cast_04_a = mMotionController.AddAnimatorName("AnyState -> Base Layer.PMP_BasicSpellCastings-SM.2H_Cast_04_a");
            TRANS_EntryState_2H_Cast_04_a = mMotionController.AddAnimatorName("Entry -> Base Layer.PMP_BasicSpellCastings-SM.2H_Cast_04_a");
            TRANS_AnyState_2H_Cast_05 = mMotionController.AddAnimatorName("AnyState -> Base Layer.PMP_BasicSpellCastings-SM.2H_Cast_05");
            TRANS_EntryState_2H_Cast_05 = mMotionController.AddAnimatorName("Entry -> Base Layer.PMP_BasicSpellCastings-SM.2H_Cast_05");
            TRANS_AnyState_2H_Cast_05_a = mMotionController.AddAnimatorName("AnyState -> Base Layer.PMP_BasicSpellCastings-SM.2H_Cast_05_a");
            TRANS_EntryState_2H_Cast_05_a = mMotionController.AddAnimatorName("Entry -> Base Layer.PMP_BasicSpellCastings-SM.2H_Cast_05_a");
            TRANS_AnyState_2H_Cast_06 = mMotionController.AddAnimatorName("AnyState -> Base Layer.PMP_BasicSpellCastings-SM.2H_Cast_06");
            TRANS_EntryState_2H_Cast_06 = mMotionController.AddAnimatorName("Entry -> Base Layer.PMP_BasicSpellCastings-SM.2H_Cast_06");
            TRANS_AnyState_2H_Cast_06_a = mMotionController.AddAnimatorName("AnyState -> Base Layer.PMP_BasicSpellCastings-SM.2H_Cast_06_a");
            TRANS_EntryState_2H_Cast_06_a = mMotionController.AddAnimatorName("Entry -> Base Layer.PMP_BasicSpellCastings-SM.2H_Cast_06_a");
            TRANS_AnyState_2H_Cast_07 = mMotionController.AddAnimatorName("AnyState -> Base Layer.PMP_BasicSpellCastings-SM.2H_Cast_07");
            TRANS_EntryState_2H_Cast_07 = mMotionController.AddAnimatorName("Entry -> Base Layer.PMP_BasicSpellCastings-SM.2H_Cast_07");
            TRANS_AnyState_2H_Cast_07_a = mMotionController.AddAnimatorName("AnyState -> Base Layer.PMP_BasicSpellCastings-SM.2H_Cast_07_a");
            TRANS_EntryState_2H_Cast_07_a = mMotionController.AddAnimatorName("Entry -> Base Layer.PMP_BasicSpellCastings-SM.2H_Cast_07_a");
            TRANS_AnyState_2H_Cast_08 = mMotionController.AddAnimatorName("AnyState -> Base Layer.PMP_BasicSpellCastings-SM.2H_Cast_08");
            TRANS_EntryState_2H_Cast_08 = mMotionController.AddAnimatorName("Entry -> Base Layer.PMP_BasicSpellCastings-SM.2H_Cast_08");
            TRANS_AnyState_2H_Cast_08_a = mMotionController.AddAnimatorName("AnyState -> Base Layer.PMP_BasicSpellCastings-SM.2H_Cast_08_a");
            TRANS_EntryState_2H_Cast_08_a = mMotionController.AddAnimatorName("Entry -> Base Layer.PMP_BasicSpellCastings-SM.2H_Cast_08_a");
            STATE_Start = mMotionController.AddAnimatorName("Base Layer.Start");
            STATE_SpellIdleOut = mMotionController.AddAnimatorName("Base Layer.PMP_BasicSpellCastings-SM.Spell Idle Out");
            STATE_1H_Cast_01 = mMotionController.AddAnimatorName("Base Layer.PMP_BasicSpellCastings-SM.1H_Cast_01");
            TRANS_1H_Cast_01_SpellIdleOut = mMotionController.AddAnimatorName("Base Layer.PMP_BasicSpellCastings-SM.1H_Cast_01 -> Base Layer.PMP_BasicSpellCastings-SM.Spell Idle Out");
            TRANS_1H_Cast_01_StandIdleTransition = mMotionController.AddAnimatorName("Base Layer.PMP_BasicSpellCastings-SM.1H_Cast_01 -> Base Layer.PMP_BasicSpellCastings-SM.Stand Idle Transition");
            STATE_StandIdleIn = mMotionController.AddAnimatorName("Base Layer.PMP_BasicSpellCastings-SM.Stand Idle In");
            TRANS_StandIdleIn_1H_Cast_01 = mMotionController.AddAnimatorName("Base Layer.PMP_BasicSpellCastings-SM.Stand Idle In -> Base Layer.PMP_BasicSpellCastings-SM.1H_Cast_01");
            TRANS_StandIdleIn_2H_Cast_01_a = mMotionController.AddAnimatorName("Base Layer.PMP_BasicSpellCastings-SM.Stand Idle In -> Base Layer.PMP_BasicSpellCastings-SM.2H_Cast_01_a");
            TRANS_StandIdleIn_2H_Cast_01 = mMotionController.AddAnimatorName("Base Layer.PMP_BasicSpellCastings-SM.Stand Idle In -> Base Layer.PMP_BasicSpellCastings-SM.2H_Cast_01");
            TRANS_StandIdleIn_1H_Cast_01_a = mMotionController.AddAnimatorName("Base Layer.PMP_BasicSpellCastings-SM.Stand Idle In -> Base Layer.PMP_BasicSpellCastings-SM.1H_Cast_01_a");
            TRANS_StandIdleIn_1H_Cast_02 = mMotionController.AddAnimatorName("Base Layer.PMP_BasicSpellCastings-SM.Stand Idle In -> Base Layer.PMP_BasicSpellCastings-SM.1H_Cast_02");
            TRANS_StandIdleIn_1H_Cast_02_a = mMotionController.AddAnimatorName("Base Layer.PMP_BasicSpellCastings-SM.Stand Idle In -> Base Layer.PMP_BasicSpellCastings-SM.1H_Cast_02_a");
            TRANS_StandIdleIn_2H_Cast_02 = mMotionController.AddAnimatorName("Base Layer.PMP_BasicSpellCastings-SM.Stand Idle In -> Base Layer.PMP_BasicSpellCastings-SM.2H_Cast_02");
            TRANS_StandIdleIn_1H_Cast_03 = mMotionController.AddAnimatorName("Base Layer.PMP_BasicSpellCastings-SM.Stand Idle In -> Base Layer.PMP_BasicSpellCastings-SM.1H_Cast_03");
            TRANS_StandIdleIn_1H_Cast_03_a = mMotionController.AddAnimatorName("Base Layer.PMP_BasicSpellCastings-SM.Stand Idle In -> Base Layer.PMP_BasicSpellCastings-SM.1H_Cast_03_a");
            TRANS_StandIdleIn_1H_Cast_04 = mMotionController.AddAnimatorName("Base Layer.PMP_BasicSpellCastings-SM.Stand Idle In -> Base Layer.PMP_BasicSpellCastings-SM.1H_Cast_04");
            TRANS_StandIdleIn_1H_Cast_04_a = mMotionController.AddAnimatorName("Base Layer.PMP_BasicSpellCastings-SM.Stand Idle In -> Base Layer.PMP_BasicSpellCastings-SM.1H_Cast_04_a");
            TRANS_StandIdleIn_2H_Cast_02_a = mMotionController.AddAnimatorName("Base Layer.PMP_BasicSpellCastings-SM.Stand Idle In -> Base Layer.PMP_BasicSpellCastings-SM.2H_Cast_02_a");
            TRANS_StandIdleIn_2H_Cast_03 = mMotionController.AddAnimatorName("Base Layer.PMP_BasicSpellCastings-SM.Stand Idle In -> Base Layer.PMP_BasicSpellCastings-SM.2H_Cast_03");
            TRANS_StandIdleIn_2H_Cast_03_a = mMotionController.AddAnimatorName("Base Layer.PMP_BasicSpellCastings-SM.Stand Idle In -> Base Layer.PMP_BasicSpellCastings-SM.2H_Cast_03_a");
            TRANS_StandIdleIn_2H_Cast_04 = mMotionController.AddAnimatorName("Base Layer.PMP_BasicSpellCastings-SM.Stand Idle In -> Base Layer.PMP_BasicSpellCastings-SM.2H_Cast_04");
            TRANS_StandIdleIn_2H_Cast_04_a = mMotionController.AddAnimatorName("Base Layer.PMP_BasicSpellCastings-SM.Stand Idle In -> Base Layer.PMP_BasicSpellCastings-SM.2H_Cast_04_a");
            TRANS_StandIdleIn_2H_Cast_05 = mMotionController.AddAnimatorName("Base Layer.PMP_BasicSpellCastings-SM.Stand Idle In -> Base Layer.PMP_BasicSpellCastings-SM.2H_Cast_05");
            TRANS_StandIdleIn_2H_Cast_05_a = mMotionController.AddAnimatorName("Base Layer.PMP_BasicSpellCastings-SM.Stand Idle In -> Base Layer.PMP_BasicSpellCastings-SM.2H_Cast_05_a");
            TRANS_StandIdleIn_2H_Cast_06 = mMotionController.AddAnimatorName("Base Layer.PMP_BasicSpellCastings-SM.Stand Idle In -> Base Layer.PMP_BasicSpellCastings-SM.2H_Cast_06");
            TRANS_StandIdleIn_2H_Cast_06_a = mMotionController.AddAnimatorName("Base Layer.PMP_BasicSpellCastings-SM.Stand Idle In -> Base Layer.PMP_BasicSpellCastings-SM.2H_Cast_06_a");
            TRANS_StandIdleIn_2H_Cast_07 = mMotionController.AddAnimatorName("Base Layer.PMP_BasicSpellCastings-SM.Stand Idle In -> Base Layer.PMP_BasicSpellCastings-SM.2H_Cast_07");
            TRANS_StandIdleIn_2H_Cast_07_a = mMotionController.AddAnimatorName("Base Layer.PMP_BasicSpellCastings-SM.Stand Idle In -> Base Layer.PMP_BasicSpellCastings-SM.2H_Cast_07_a");
            TRANS_StandIdleIn_2H_Cast_08 = mMotionController.AddAnimatorName("Base Layer.PMP_BasicSpellCastings-SM.Stand Idle In -> Base Layer.PMP_BasicSpellCastings-SM.2H_Cast_08");
            TRANS_StandIdleIn_2H_Cast_08_a = mMotionController.AddAnimatorName("Base Layer.PMP_BasicSpellCastings-SM.Stand Idle In -> Base Layer.PMP_BasicSpellCastings-SM.2H_Cast_08_a");
            STATE_StandIdleTransition = mMotionController.AddAnimatorName("Base Layer.PMP_BasicSpellCastings-SM.Stand Idle Transition");
            TRANS_StandIdleTransition_StandIdleOut = mMotionController.AddAnimatorName("Base Layer.PMP_BasicSpellCastings-SM.Stand Idle Transition -> Base Layer.PMP_BasicSpellCastings-SM.Stand Idle Out");
            STATE_StandIdleOut = mMotionController.AddAnimatorName("Base Layer.PMP_BasicSpellCastings-SM.Stand Idle Out");
            STATE_2H_Cast_01_a = mMotionController.AddAnimatorName("Base Layer.PMP_BasicSpellCastings-SM.2H_Cast_01_a");
            TRANS_2H_Cast_01_a_2H_Cast_01_b = mMotionController.AddAnimatorName("Base Layer.PMP_BasicSpellCastings-SM.2H_Cast_01_a -> Base Layer.PMP_BasicSpellCastings-SM.2H_Cast_01_b");
            STATE_2H_Cast_01_c = mMotionController.AddAnimatorName("Base Layer.PMP_BasicSpellCastings-SM.2H_Cast_01_c");
            TRANS_2H_Cast_01_c_SpellIdleOut = mMotionController.AddAnimatorName("Base Layer.PMP_BasicSpellCastings-SM.2H_Cast_01_c -> Base Layer.PMP_BasicSpellCastings-SM.Spell Idle Out");
            TRANS_2H_Cast_01_c_StandIdleTransition = mMotionController.AddAnimatorName("Base Layer.PMP_BasicSpellCastings-SM.2H_Cast_01_c -> Base Layer.PMP_BasicSpellCastings-SM.Stand Idle Transition");
            STATE_2H_Cast_01_b = mMotionController.AddAnimatorName("Base Layer.PMP_BasicSpellCastings-SM.2H_Cast_01_b");
            TRANS_2H_Cast_01_b_2H_Cast_01_c = mMotionController.AddAnimatorName("Base Layer.PMP_BasicSpellCastings-SM.2H_Cast_01_b -> Base Layer.PMP_BasicSpellCastings-SM.2H_Cast_01_c");
            TRANS_2H_Cast_01_b_StandIdleTransition = mMotionController.AddAnimatorName("Base Layer.PMP_BasicSpellCastings-SM.2H_Cast_01_b -> Base Layer.PMP_BasicSpellCastings-SM.Stand Idle Transition");
            TRANS_2H_Cast_01_b_SpellIdleOut = mMotionController.AddAnimatorName("Base Layer.PMP_BasicSpellCastings-SM.2H_Cast_01_b -> Base Layer.PMP_BasicSpellCastings-SM.Spell Idle Out");
            STATE_2H_Cast_01 = mMotionController.AddAnimatorName("Base Layer.PMP_BasicSpellCastings-SM.2H_Cast_01");
            TRANS_2H_Cast_01_SpellIdleOut = mMotionController.AddAnimatorName("Base Layer.PMP_BasicSpellCastings-SM.2H_Cast_01 -> Base Layer.PMP_BasicSpellCastings-SM.Spell Idle Out");
            TRANS_2H_Cast_01_StandIdleTransition = mMotionController.AddAnimatorName("Base Layer.PMP_BasicSpellCastings-SM.2H_Cast_01 -> Base Layer.PMP_BasicSpellCastings-SM.Stand Idle Transition");
            STATE_1H_Cast_01_a = mMotionController.AddAnimatorName("Base Layer.PMP_BasicSpellCastings-SM.1H_Cast_01_a");
            TRANS_1H_Cast_01_a_1H_Cast_01_b = mMotionController.AddAnimatorName("Base Layer.PMP_BasicSpellCastings-SM.1H_Cast_01_a -> Base Layer.PMP_BasicSpellCastings-SM.1H_Cast_01_b");
            STATE_1H_Cast_01_b = mMotionController.AddAnimatorName("Base Layer.PMP_BasicSpellCastings-SM.1H_Cast_01_b");
            TRANS_1H_Cast_01_b_1H_Cast_01_c = mMotionController.AddAnimatorName("Base Layer.PMP_BasicSpellCastings-SM.1H_Cast_01_b -> Base Layer.PMP_BasicSpellCastings-SM.1H_Cast_01_c");
            TRANS_1H_Cast_01_b_SpellIdleOut = mMotionController.AddAnimatorName("Base Layer.PMP_BasicSpellCastings-SM.1H_Cast_01_b -> Base Layer.PMP_BasicSpellCastings-SM.Spell Idle Out");
            TRANS_1H_Cast_01_b_StandIdleTransition = mMotionController.AddAnimatorName("Base Layer.PMP_BasicSpellCastings-SM.1H_Cast_01_b -> Base Layer.PMP_BasicSpellCastings-SM.Stand Idle Transition");
            STATE_1H_Cast_01_c = mMotionController.AddAnimatorName("Base Layer.PMP_BasicSpellCastings-SM.1H_Cast_01_c");
            TRANS_1H_Cast_01_c_SpellIdleOut = mMotionController.AddAnimatorName("Base Layer.PMP_BasicSpellCastings-SM.1H_Cast_01_c -> Base Layer.PMP_BasicSpellCastings-SM.Spell Idle Out");
            TRANS_1H_Cast_01_c_StandIdleTransition = mMotionController.AddAnimatorName("Base Layer.PMP_BasicSpellCastings-SM.1H_Cast_01_c -> Base Layer.PMP_BasicSpellCastings-SM.Stand Idle Transition");
            STATE_Interrupted = mMotionController.AddAnimatorName("Base Layer.PMP_BasicSpellCastings-SM.Interrupted");
            TRANS_Interrupted_SpellIdleOut = mMotionController.AddAnimatorName("Base Layer.PMP_BasicSpellCastings-SM.Interrupted -> Base Layer.PMP_BasicSpellCastings-SM.Spell Idle Out");
            TRANS_Interrupted_StandIdleTransition = mMotionController.AddAnimatorName("Base Layer.PMP_BasicSpellCastings-SM.Interrupted -> Base Layer.PMP_BasicSpellCastings-SM.Stand Idle Transition");
            STATE_1H_Cast_02 = mMotionController.AddAnimatorName("Base Layer.PMP_BasicSpellCastings-SM.1H_Cast_02");
            TRANS_1H_Cast_02_SpellIdleOut = mMotionController.AddAnimatorName("Base Layer.PMP_BasicSpellCastings-SM.1H_Cast_02 -> Base Layer.PMP_BasicSpellCastings-SM.Spell Idle Out");
            TRANS_1H_Cast_02_StandIdleTransition = mMotionController.AddAnimatorName("Base Layer.PMP_BasicSpellCastings-SM.1H_Cast_02 -> Base Layer.PMP_BasicSpellCastings-SM.Stand Idle Transition");
            STATE_1H_Cast_02_a = mMotionController.AddAnimatorName("Base Layer.PMP_BasicSpellCastings-SM.1H_Cast_02_a");
            TRANS_1H_Cast_02_a_1H_Cast_02_b = mMotionController.AddAnimatorName("Base Layer.PMP_BasicSpellCastings-SM.1H_Cast_02_a -> Base Layer.PMP_BasicSpellCastings-SM.1H_Cast_02_b");
            STATE_1H_Cast_02_b = mMotionController.AddAnimatorName("Base Layer.PMP_BasicSpellCastings-SM.1H_Cast_02_b");
            TRANS_1H_Cast_02_b_1H_Cast_02_c = mMotionController.AddAnimatorName("Base Layer.PMP_BasicSpellCastings-SM.1H_Cast_02_b -> Base Layer.PMP_BasicSpellCastings-SM.1H_Cast_02_c");
            TRANS_1H_Cast_02_b_SpellIdleOut = mMotionController.AddAnimatorName("Base Layer.PMP_BasicSpellCastings-SM.1H_Cast_02_b -> Base Layer.PMP_BasicSpellCastings-SM.Spell Idle Out");
            TRANS_1H_Cast_02_b_StandIdleTransition = mMotionController.AddAnimatorName("Base Layer.PMP_BasicSpellCastings-SM.1H_Cast_02_b -> Base Layer.PMP_BasicSpellCastings-SM.Stand Idle Transition");
            STATE_1H_Cast_02_c = mMotionController.AddAnimatorName("Base Layer.PMP_BasicSpellCastings-SM.1H_Cast_02_c");
            TRANS_1H_Cast_02_c_SpellIdleOut = mMotionController.AddAnimatorName("Base Layer.PMP_BasicSpellCastings-SM.1H_Cast_02_c -> Base Layer.PMP_BasicSpellCastings-SM.Spell Idle Out");
            TRANS_1H_Cast_02_c_StandIdleTransition = mMotionController.AddAnimatorName("Base Layer.PMP_BasicSpellCastings-SM.1H_Cast_02_c -> Base Layer.PMP_BasicSpellCastings-SM.Stand Idle Transition");
            STATE_2H_Cast_02 = mMotionController.AddAnimatorName("Base Layer.PMP_BasicSpellCastings-SM.2H_Cast_02");
            TRANS_2H_Cast_02_SpellIdleOut = mMotionController.AddAnimatorName("Base Layer.PMP_BasicSpellCastings-SM.2H_Cast_02 -> Base Layer.PMP_BasicSpellCastings-SM.Spell Idle Out");
            TRANS_2H_Cast_02_StandIdleTransition = mMotionController.AddAnimatorName("Base Layer.PMP_BasicSpellCastings-SM.2H_Cast_02 -> Base Layer.PMP_BasicSpellCastings-SM.Stand Idle Transition");
            STATE_2H_Cast_02_a = mMotionController.AddAnimatorName("Base Layer.PMP_BasicSpellCastings-SM.2H_Cast_02_a");
            TRANS_2H_Cast_02_a_2H_Cast_02_b = mMotionController.AddAnimatorName("Base Layer.PMP_BasicSpellCastings-SM.2H_Cast_02_a -> Base Layer.PMP_BasicSpellCastings-SM.2H_Cast_02_b");
            STATE_2H_Cast_02_b = mMotionController.AddAnimatorName("Base Layer.PMP_BasicSpellCastings-SM.2H_Cast_02_b");
            TRANS_2H_Cast_02_b_2H_Cast_02_c = mMotionController.AddAnimatorName("Base Layer.PMP_BasicSpellCastings-SM.2H_Cast_02_b -> Base Layer.PMP_BasicSpellCastings-SM.2H_Cast_02_c");
            TRANS_2H_Cast_02_b_SpellIdleOut = mMotionController.AddAnimatorName("Base Layer.PMP_BasicSpellCastings-SM.2H_Cast_02_b -> Base Layer.PMP_BasicSpellCastings-SM.Spell Idle Out");
            TRANS_2H_Cast_02_b_StandIdleTransition = mMotionController.AddAnimatorName("Base Layer.PMP_BasicSpellCastings-SM.2H_Cast_02_b -> Base Layer.PMP_BasicSpellCastings-SM.Stand Idle Transition");
            STATE_2H_Cast_02_c = mMotionController.AddAnimatorName("Base Layer.PMP_BasicSpellCastings-SM.2H_Cast_02_c");
            TRANS_2H_Cast_02_c_SpellIdleOut = mMotionController.AddAnimatorName("Base Layer.PMP_BasicSpellCastings-SM.2H_Cast_02_c -> Base Layer.PMP_BasicSpellCastings-SM.Spell Idle Out");
            TRANS_2H_Cast_02_c_StandIdleTransition = mMotionController.AddAnimatorName("Base Layer.PMP_BasicSpellCastings-SM.2H_Cast_02_c -> Base Layer.PMP_BasicSpellCastings-SM.Stand Idle Transition");
            STATE_2H_Cast_03 = mMotionController.AddAnimatorName("Base Layer.PMP_BasicSpellCastings-SM.2H_Cast_03");
            TRANS_2H_Cast_03_SpellIdleOut = mMotionController.AddAnimatorName("Base Layer.PMP_BasicSpellCastings-SM.2H_Cast_03 -> Base Layer.PMP_BasicSpellCastings-SM.Spell Idle Out");
            TRANS_2H_Cast_03_StandIdleTransition = mMotionController.AddAnimatorName("Base Layer.PMP_BasicSpellCastings-SM.2H_Cast_03 -> Base Layer.PMP_BasicSpellCastings-SM.Stand Idle Transition");
            STATE_2H_Cast_03_a = mMotionController.AddAnimatorName("Base Layer.PMP_BasicSpellCastings-SM.2H_Cast_03_a");
            TRANS_2H_Cast_03_a_2H_Cast_03_b = mMotionController.AddAnimatorName("Base Layer.PMP_BasicSpellCastings-SM.2H_Cast_03_a -> Base Layer.PMP_BasicSpellCastings-SM.2H_Cast_03_b");
            STATE_2H_Cast_03_b = mMotionController.AddAnimatorName("Base Layer.PMP_BasicSpellCastings-SM.2H_Cast_03_b");
            TRANS_2H_Cast_03_b_2H_Cast_03_c = mMotionController.AddAnimatorName("Base Layer.PMP_BasicSpellCastings-SM.2H_Cast_03_b -> Base Layer.PMP_BasicSpellCastings-SM.2H_Cast_03_c");
            TRANS_2H_Cast_03_b_SpellIdleOut = mMotionController.AddAnimatorName("Base Layer.PMP_BasicSpellCastings-SM.2H_Cast_03_b -> Base Layer.PMP_BasicSpellCastings-SM.Spell Idle Out");
            TRANS_2H_Cast_03_b_StandIdleTransition = mMotionController.AddAnimatorName("Base Layer.PMP_BasicSpellCastings-SM.2H_Cast_03_b -> Base Layer.PMP_BasicSpellCastings-SM.Stand Idle Transition");
            STATE_2H_Cast_03_c = mMotionController.AddAnimatorName("Base Layer.PMP_BasicSpellCastings-SM.2H_Cast_03_c");
            TRANS_2H_Cast_03_c_SpellIdleOut = mMotionController.AddAnimatorName("Base Layer.PMP_BasicSpellCastings-SM.2H_Cast_03_c -> Base Layer.PMP_BasicSpellCastings-SM.Spell Idle Out");
            TRANS_2H_Cast_03_c_StandIdleTransition = mMotionController.AddAnimatorName("Base Layer.PMP_BasicSpellCastings-SM.2H_Cast_03_c -> Base Layer.PMP_BasicSpellCastings-SM.Stand Idle Transition");
            STATE_2H_Cast_04 = mMotionController.AddAnimatorName("Base Layer.PMP_BasicSpellCastings-SM.2H_Cast_04");
            TRANS_2H_Cast_04_SpellIdleOut = mMotionController.AddAnimatorName("Base Layer.PMP_BasicSpellCastings-SM.2H_Cast_04 -> Base Layer.PMP_BasicSpellCastings-SM.Spell Idle Out");
            TRANS_2H_Cast_04_StandIdleTransition = mMotionController.AddAnimatorName("Base Layer.PMP_BasicSpellCastings-SM.2H_Cast_04 -> Base Layer.PMP_BasicSpellCastings-SM.Stand Idle Transition");
            STATE_2H_Cast_04_a = mMotionController.AddAnimatorName("Base Layer.PMP_BasicSpellCastings-SM.2H_Cast_04_a");
            TRANS_2H_Cast_04_a_2H_Cast_04_b = mMotionController.AddAnimatorName("Base Layer.PMP_BasicSpellCastings-SM.2H_Cast_04_a -> Base Layer.PMP_BasicSpellCastings-SM.2H_Cast_04_b");
            STATE_2H_Cast_04_b = mMotionController.AddAnimatorName("Base Layer.PMP_BasicSpellCastings-SM.2H_Cast_04_b");
            TRANS_2H_Cast_04_b_2H_Cast_04_c = mMotionController.AddAnimatorName("Base Layer.PMP_BasicSpellCastings-SM.2H_Cast_04_b -> Base Layer.PMP_BasicSpellCastings-SM.2H_Cast_04_c");
            TRANS_2H_Cast_04_b_SpellIdleOut = mMotionController.AddAnimatorName("Base Layer.PMP_BasicSpellCastings-SM.2H_Cast_04_b -> Base Layer.PMP_BasicSpellCastings-SM.Spell Idle Out");
            TRANS_2H_Cast_04_b_StandIdleTransition = mMotionController.AddAnimatorName("Base Layer.PMP_BasicSpellCastings-SM.2H_Cast_04_b -> Base Layer.PMP_BasicSpellCastings-SM.Stand Idle Transition");
            STATE_2H_Cast_04_c = mMotionController.AddAnimatorName("Base Layer.PMP_BasicSpellCastings-SM.2H_Cast_04_c");
            TRANS_2H_Cast_04_c_SpellIdleOut = mMotionController.AddAnimatorName("Base Layer.PMP_BasicSpellCastings-SM.2H_Cast_04_c -> Base Layer.PMP_BasicSpellCastings-SM.Spell Idle Out");
            TRANS_2H_Cast_04_c_StandIdleTransition = mMotionController.AddAnimatorName("Base Layer.PMP_BasicSpellCastings-SM.2H_Cast_04_c -> Base Layer.PMP_BasicSpellCastings-SM.Stand Idle Transition");
            STATE_2H_Cast_05 = mMotionController.AddAnimatorName("Base Layer.PMP_BasicSpellCastings-SM.2H_Cast_05");
            TRANS_2H_Cast_05_SpellIdleOut = mMotionController.AddAnimatorName("Base Layer.PMP_BasicSpellCastings-SM.2H_Cast_05 -> Base Layer.PMP_BasicSpellCastings-SM.Spell Idle Out");
            TRANS_2H_Cast_05_StandIdleTransition = mMotionController.AddAnimatorName("Base Layer.PMP_BasicSpellCastings-SM.2H_Cast_05 -> Base Layer.PMP_BasicSpellCastings-SM.Stand Idle Transition");
            STATE_2H_Cast_05_a = mMotionController.AddAnimatorName("Base Layer.PMP_BasicSpellCastings-SM.2H_Cast_05_a");
            TRANS_2H_Cast_05_a_2H_Cast_05_b = mMotionController.AddAnimatorName("Base Layer.PMP_BasicSpellCastings-SM.2H_Cast_05_a -> Base Layer.PMP_BasicSpellCastings-SM.2H_Cast_05_b");
            STATE_2H_Cast_05_b = mMotionController.AddAnimatorName("Base Layer.PMP_BasicSpellCastings-SM.2H_Cast_05_b");
            TRANS_2H_Cast_05_b_2H_Cast_05_c = mMotionController.AddAnimatorName("Base Layer.PMP_BasicSpellCastings-SM.2H_Cast_05_b -> Base Layer.PMP_BasicSpellCastings-SM.2H_Cast_05_c");
            TRANS_2H_Cast_05_b_SpellIdleOut = mMotionController.AddAnimatorName("Base Layer.PMP_BasicSpellCastings-SM.2H_Cast_05_b -> Base Layer.PMP_BasicSpellCastings-SM.Spell Idle Out");
            TRANS_2H_Cast_05_b_StandIdleTransition = mMotionController.AddAnimatorName("Base Layer.PMP_BasicSpellCastings-SM.2H_Cast_05_b -> Base Layer.PMP_BasicSpellCastings-SM.Stand Idle Transition");
            STATE_2H_Cast_05_c = mMotionController.AddAnimatorName("Base Layer.PMP_BasicSpellCastings-SM.2H_Cast_05_c");
            TRANS_2H_Cast_05_c_SpellIdleOut = mMotionController.AddAnimatorName("Base Layer.PMP_BasicSpellCastings-SM.2H_Cast_05_c -> Base Layer.PMP_BasicSpellCastings-SM.Spell Idle Out");
            TRANS_2H_Cast_05_c_StandIdleTransition = mMotionController.AddAnimatorName("Base Layer.PMP_BasicSpellCastings-SM.2H_Cast_05_c -> Base Layer.PMP_BasicSpellCastings-SM.Stand Idle Transition");
            STATE_2H_Cast_06 = mMotionController.AddAnimatorName("Base Layer.PMP_BasicSpellCastings-SM.2H_Cast_06");
            TRANS_2H_Cast_06_SpellIdleOut = mMotionController.AddAnimatorName("Base Layer.PMP_BasicSpellCastings-SM.2H_Cast_06 -> Base Layer.PMP_BasicSpellCastings-SM.Spell Idle Out");
            TRANS_2H_Cast_06_StandIdleTransition = mMotionController.AddAnimatorName("Base Layer.PMP_BasicSpellCastings-SM.2H_Cast_06 -> Base Layer.PMP_BasicSpellCastings-SM.Stand Idle Transition");
            STATE_2H_Cast_06_a = mMotionController.AddAnimatorName("Base Layer.PMP_BasicSpellCastings-SM.2H_Cast_06_a");
            TRANS_2H_Cast_06_a_2H_Cast_06_b = mMotionController.AddAnimatorName("Base Layer.PMP_BasicSpellCastings-SM.2H_Cast_06_a -> Base Layer.PMP_BasicSpellCastings-SM.2H_Cast_06_b");
            STATE_2H_Cast_06_b = mMotionController.AddAnimatorName("Base Layer.PMP_BasicSpellCastings-SM.2H_Cast_06_b");
            TRANS_2H_Cast_06_b_2H_Cast_06_c = mMotionController.AddAnimatorName("Base Layer.PMP_BasicSpellCastings-SM.2H_Cast_06_b -> Base Layer.PMP_BasicSpellCastings-SM.2H_Cast_06_c");
            TRANS_2H_Cast_06_b_SpellIdleOut = mMotionController.AddAnimatorName("Base Layer.PMP_BasicSpellCastings-SM.2H_Cast_06_b -> Base Layer.PMP_BasicSpellCastings-SM.Spell Idle Out");
            TRANS_2H_Cast_06_b_StandIdleTransition = mMotionController.AddAnimatorName("Base Layer.PMP_BasicSpellCastings-SM.2H_Cast_06_b -> Base Layer.PMP_BasicSpellCastings-SM.Stand Idle Transition");
            STATE_2H_Cast_06_c = mMotionController.AddAnimatorName("Base Layer.PMP_BasicSpellCastings-SM.2H_Cast_06_c");
            TRANS_2H_Cast_06_c_SpellIdleOut = mMotionController.AddAnimatorName("Base Layer.PMP_BasicSpellCastings-SM.2H_Cast_06_c -> Base Layer.PMP_BasicSpellCastings-SM.Spell Idle Out");
            TRANS_2H_Cast_06_c_StandIdleTransition = mMotionController.AddAnimatorName("Base Layer.PMP_BasicSpellCastings-SM.2H_Cast_06_c -> Base Layer.PMP_BasicSpellCastings-SM.Stand Idle Transition");
            STATE_2H_Cast_07 = mMotionController.AddAnimatorName("Base Layer.PMP_BasicSpellCastings-SM.2H_Cast_07");
            TRANS_2H_Cast_07_SpellIdleOut = mMotionController.AddAnimatorName("Base Layer.PMP_BasicSpellCastings-SM.2H_Cast_07 -> Base Layer.PMP_BasicSpellCastings-SM.Spell Idle Out");
            TRANS_2H_Cast_07_StandIdleTransition = mMotionController.AddAnimatorName("Base Layer.PMP_BasicSpellCastings-SM.2H_Cast_07 -> Base Layer.PMP_BasicSpellCastings-SM.Stand Idle Transition");
            STATE_2H_Cast_07_a = mMotionController.AddAnimatorName("Base Layer.PMP_BasicSpellCastings-SM.2H_Cast_07_a");
            TRANS_2H_Cast_07_a_2H_Cast_07_b = mMotionController.AddAnimatorName("Base Layer.PMP_BasicSpellCastings-SM.2H_Cast_07_a -> Base Layer.PMP_BasicSpellCastings-SM.2H_Cast_07_b");
            STATE_2H_Cast_07_b = mMotionController.AddAnimatorName("Base Layer.PMP_BasicSpellCastings-SM.2H_Cast_07_b");
            TRANS_2H_Cast_07_b_2H_Cast_07_c = mMotionController.AddAnimatorName("Base Layer.PMP_BasicSpellCastings-SM.2H_Cast_07_b -> Base Layer.PMP_BasicSpellCastings-SM.2H_Cast_07_c");
            TRANS_2H_Cast_07_b_SpellIdleOut = mMotionController.AddAnimatorName("Base Layer.PMP_BasicSpellCastings-SM.2H_Cast_07_b -> Base Layer.PMP_BasicSpellCastings-SM.Spell Idle Out");
            TRANS_2H_Cast_07_b_StandIdleTransition = mMotionController.AddAnimatorName("Base Layer.PMP_BasicSpellCastings-SM.2H_Cast_07_b -> Base Layer.PMP_BasicSpellCastings-SM.Stand Idle Transition");
            STATE_2H_Cast_07_c = mMotionController.AddAnimatorName("Base Layer.PMP_BasicSpellCastings-SM.2H_Cast_07_c");
            TRANS_2H_Cast_07_c_SpellIdleOut = mMotionController.AddAnimatorName("Base Layer.PMP_BasicSpellCastings-SM.2H_Cast_07_c -> Base Layer.PMP_BasicSpellCastings-SM.Spell Idle Out");
            TRANS_2H_Cast_07_c_StandIdleTransition = mMotionController.AddAnimatorName("Base Layer.PMP_BasicSpellCastings-SM.2H_Cast_07_c -> Base Layer.PMP_BasicSpellCastings-SM.Stand Idle Transition");
            STATE_2H_Cast_08 = mMotionController.AddAnimatorName("Base Layer.PMP_BasicSpellCastings-SM.2H_Cast_08");
            TRANS_2H_Cast_08_SpellIdleOut = mMotionController.AddAnimatorName("Base Layer.PMP_BasicSpellCastings-SM.2H_Cast_08 -> Base Layer.PMP_BasicSpellCastings-SM.Spell Idle Out");
            TRANS_2H_Cast_08_StandIdleTransition = mMotionController.AddAnimatorName("Base Layer.PMP_BasicSpellCastings-SM.2H_Cast_08 -> Base Layer.PMP_BasicSpellCastings-SM.Stand Idle Transition");
            STATE_2H_Cast_08_a = mMotionController.AddAnimatorName("Base Layer.PMP_BasicSpellCastings-SM.2H_Cast_08_a");
            TRANS_2H_Cast_08_a_2H_Cast_08_b = mMotionController.AddAnimatorName("Base Layer.PMP_BasicSpellCastings-SM.2H_Cast_08_a -> Base Layer.PMP_BasicSpellCastings-SM.2H_Cast_08_b");
            STATE_2H_Cast_08_b = mMotionController.AddAnimatorName("Base Layer.PMP_BasicSpellCastings-SM.2H_Cast_08_b");
            TRANS_2H_Cast_08_b_2H_Cast_08_c = mMotionController.AddAnimatorName("Base Layer.PMP_BasicSpellCastings-SM.2H_Cast_08_b -> Base Layer.PMP_BasicSpellCastings-SM.2H_Cast_08_c");
            TRANS_2H_Cast_08_b_SpellIdleOut = mMotionController.AddAnimatorName("Base Layer.PMP_BasicSpellCastings-SM.2H_Cast_08_b -> Base Layer.PMP_BasicSpellCastings-SM.Spell Idle Out");
            TRANS_2H_Cast_08_b_StandIdleTransition = mMotionController.AddAnimatorName("Base Layer.PMP_BasicSpellCastings-SM.2H_Cast_08_b -> Base Layer.PMP_BasicSpellCastings-SM.Stand Idle Transition");
            STATE_2H_Cast_08_c = mMotionController.AddAnimatorName("Base Layer.PMP_BasicSpellCastings-SM.2H_Cast_08_c");
            TRANS_2H_Cast_08_c_SpellIdleOut = mMotionController.AddAnimatorName("Base Layer.PMP_BasicSpellCastings-SM.2H_Cast_08_c -> Base Layer.PMP_BasicSpellCastings-SM.Spell Idle Out");
            TRANS_2H_Cast_08_c_StandIdleTransition = mMotionController.AddAnimatorName("Base Layer.PMP_BasicSpellCastings-SM.2H_Cast_08_c -> Base Layer.PMP_BasicSpellCastings-SM.Stand Idle Transition");
            STATE_1H_Cast_03 = mMotionController.AddAnimatorName("Base Layer.PMP_BasicSpellCastings-SM.1H_Cast_03");
            TRANS_1H_Cast_03_SpellIdleOut = mMotionController.AddAnimatorName("Base Layer.PMP_BasicSpellCastings-SM.1H_Cast_03 -> Base Layer.PMP_BasicSpellCastings-SM.Spell Idle Out");
            TRANS_1H_Cast_03_StandIdleTransition = mMotionController.AddAnimatorName("Base Layer.PMP_BasicSpellCastings-SM.1H_Cast_03 -> Base Layer.PMP_BasicSpellCastings-SM.Stand Idle Transition");
            STATE_1H_Cast_03_a = mMotionController.AddAnimatorName("Base Layer.PMP_BasicSpellCastings-SM.1H_Cast_03_a");
            TRANS_1H_Cast_03_a_1H_Cast_03_b = mMotionController.AddAnimatorName("Base Layer.PMP_BasicSpellCastings-SM.1H_Cast_03_a -> Base Layer.PMP_BasicSpellCastings-SM.1H_Cast_03_b");
            STATE_1H_Cast_03_b = mMotionController.AddAnimatorName("Base Layer.PMP_BasicSpellCastings-SM.1H_Cast_03_b");
            TRANS_1H_Cast_03_b_1H_Cast_03_c = mMotionController.AddAnimatorName("Base Layer.PMP_BasicSpellCastings-SM.1H_Cast_03_b -> Base Layer.PMP_BasicSpellCastings-SM.1H_Cast_03_c");
            TRANS_1H_Cast_03_b_SpellIdleOut = mMotionController.AddAnimatorName("Base Layer.PMP_BasicSpellCastings-SM.1H_Cast_03_b -> Base Layer.PMP_BasicSpellCastings-SM.Spell Idle Out");
            TRANS_1H_Cast_03_b_StandIdleTransition = mMotionController.AddAnimatorName("Base Layer.PMP_BasicSpellCastings-SM.1H_Cast_03_b -> Base Layer.PMP_BasicSpellCastings-SM.Stand Idle Transition");
            STATE_1H_Cast_03_c = mMotionController.AddAnimatorName("Base Layer.PMP_BasicSpellCastings-SM.1H_Cast_03_c");
            TRANS_1H_Cast_03_c_SpellIdleOut = mMotionController.AddAnimatorName("Base Layer.PMP_BasicSpellCastings-SM.1H_Cast_03_c -> Base Layer.PMP_BasicSpellCastings-SM.Spell Idle Out");
            TRANS_1H_Cast_03_c_StandIdleTransition = mMotionController.AddAnimatorName("Base Layer.PMP_BasicSpellCastings-SM.1H_Cast_03_c -> Base Layer.PMP_BasicSpellCastings-SM.Stand Idle Transition");
            STATE_1H_Cast_04 = mMotionController.AddAnimatorName("Base Layer.PMP_BasicSpellCastings-SM.1H_Cast_04");
            TRANS_1H_Cast_04_SpellIdleOut = mMotionController.AddAnimatorName("Base Layer.PMP_BasicSpellCastings-SM.1H_Cast_04 -> Base Layer.PMP_BasicSpellCastings-SM.Spell Idle Out");
            TRANS_1H_Cast_04_StandIdleTransition = mMotionController.AddAnimatorName("Base Layer.PMP_BasicSpellCastings-SM.1H_Cast_04 -> Base Layer.PMP_BasicSpellCastings-SM.Stand Idle Transition");
            STATE_1H_Cast_04_a = mMotionController.AddAnimatorName("Base Layer.PMP_BasicSpellCastings-SM.1H_Cast_04_a");
            TRANS_1H_Cast_04_a_1H_Cast_04_b = mMotionController.AddAnimatorName("Base Layer.PMP_BasicSpellCastings-SM.1H_Cast_04_a -> Base Layer.PMP_BasicSpellCastings-SM.1H_Cast_04_b");
            STATE_1H_Cast_04_b = mMotionController.AddAnimatorName("Base Layer.PMP_BasicSpellCastings-SM.1H_Cast_04_b");
            TRANS_1H_Cast_04_b_1H_Cast_04_c = mMotionController.AddAnimatorName("Base Layer.PMP_BasicSpellCastings-SM.1H_Cast_04_b -> Base Layer.PMP_BasicSpellCastings-SM.1H_Cast_04_c");
            TRANS_1H_Cast_04_b_SpellIdleOut = mMotionController.AddAnimatorName("Base Layer.PMP_BasicSpellCastings-SM.1H_Cast_04_b -> Base Layer.PMP_BasicSpellCastings-SM.Spell Idle Out");
            TRANS_1H_Cast_04_b_StandIdleTransition = mMotionController.AddAnimatorName("Base Layer.PMP_BasicSpellCastings-SM.1H_Cast_04_b -> Base Layer.PMP_BasicSpellCastings-SM.Stand Idle Transition");
            STATE_1H_Cast_04_c = mMotionController.AddAnimatorName("Base Layer.PMP_BasicSpellCastings-SM.1H_Cast_04_c");
            TRANS_1H_Cast_04_c_SpellIdleOut = mMotionController.AddAnimatorName("Base Layer.PMP_BasicSpellCastings-SM.1H_Cast_04_c -> Base Layer.PMP_BasicSpellCastings-SM.Spell Idle Out");
            TRANS_1H_Cast_04_c_StandIdleTransition = mMotionController.AddAnimatorName("Base Layer.PMP_BasicSpellCastings-SM.1H_Cast_04_c -> Base Layer.PMP_BasicSpellCastings-SM.Stand Idle Transition");
        }

#if UNITY_EDITOR

        private AnimationClip m17094 = null;
        private AnimationClip m20438 = null;
        private AnimationClip m30130 = null;
        private AnimationClip m19590 = null;
        private AnimationClip m16546 = null;
        private AnimationClip m16550 = null;
        private AnimationClip m16548 = null;
        private AnimationClip m16544 = null;
        private AnimationClip m30124 = null;
        private AnimationClip m30126 = null;
        private AnimationClip m30128 = null;
        private AnimationClip m31788 = null;
        private AnimationClip m23758 = null;
        private AnimationClip m186464 = null;
        private AnimationClip m186466 = null;
        private AnimationClip m186468 = null;
        private AnimationClip m22246 = null;
        private AnimationClip m241242 = null;
        private AnimationClip m241244 = null;
        private AnimationClip m241246 = null;
        private AnimationClip m242640 = null;
        private AnimationClip m242642 = null;
        private AnimationClip m242644 = null;
        private AnimationClip m242646 = null;
        private AnimationClip m242650 = null;
        private AnimationClip m242652 = null;
        private AnimationClip m242654 = null;
        private AnimationClip m242656 = null;
        private AnimationClip m242660 = null;
        private AnimationClip m242662 = null;
        private AnimationClip m242664 = null;
        private AnimationClip m242666 = null;
        private AnimationClip m242670 = null;
        private AnimationClip m242672 = null;
        private AnimationClip m242674 = null;
        private AnimationClip m242676 = null;
        private AnimationClip m242680 = null;
        private AnimationClip m242682 = null;
        private AnimationClip m242684 = null;
        private AnimationClip m15810 = null;
        private AnimationClip m242690 = null;
        private AnimationClip m242692 = null;
        private AnimationClip m242694 = null;
        private AnimationClip m242696 = null;
        private AnimationClip m242620 = null;
        private AnimationClip m242622 = null;
        private AnimationClip m242624 = null;
        private AnimationClip m242626 = null;
        private AnimationClip m242630 = null;
        private AnimationClip m242632 = null;
        private AnimationClip m242634 = null;
        private AnimationClip m242636 = null;

        /// <summary>
        /// Creates the animator substate machine for this motion.
        /// </summary>
        protected override void CreateStateMachine()
        {
            // Grab the root sm for the layer
            UnityEditor.Animations.AnimatorStateMachine lRootStateMachine = _EditorAnimatorController.layers[mMotionLayer.AnimatorLayerIndex].stateMachine;
            UnityEditor.Animations.AnimatorStateMachine lSM_40198 = _EditorAnimatorController.layers[mMotionLayer.AnimatorLayerIndex].stateMachine;
            UnityEditor.Animations.AnimatorStateMachine lRootSubStateMachine = null;

            // If we find the sm with our name, remove it
            for (int i = 0; i < lRootStateMachine.stateMachines.Length; i++)
            {
                // Look for a sm with the matching name
                if (lRootStateMachine.stateMachines[i].stateMachine.name == _EditorAnimatorSMName)
                {
                    lRootSubStateMachine = lRootStateMachine.stateMachines[i].stateMachine;

                    // Allow the user to stop before we remove the sm
                    if (!UnityEditor.EditorUtility.DisplayDialog("Motion Controller", _EditorAnimatorSMName + " already exists. Delete and recreate it?", "Yes", "No"))
                    {
                        return;
                    }

                    // Remove the sm
                    //lRootStateMachine.RemoveStateMachine(lRootStateMachine.stateMachines[i].stateMachine);
                    break;
                }
            }

            UnityEditor.Animations.AnimatorStateMachine lSM_40226 = lRootSubStateMachine;
            if (lSM_40226 != null)
            {
                for (int i = lSM_40226.entryTransitions.Length - 1; i >= 0; i--)
                {
                    lSM_40226.RemoveEntryTransition(lSM_40226.entryTransitions[i]);
                }

                for (int i = lSM_40226.anyStateTransitions.Length - 1; i >= 0; i--)
                {
                    lSM_40226.RemoveAnyStateTransition(lSM_40226.anyStateTransitions[i]);
                }

                for (int i = lSM_40226.states.Length - 1; i >= 0; i--)
                {
                    lSM_40226.RemoveState(lSM_40226.states[i].state);
                }

                for (int i = lSM_40226.stateMachines.Length - 1; i >= 0; i--)
                {
                    lSM_40226.RemoveStateMachine(lSM_40226.stateMachines[i].stateMachine);
                }
            }
            else
            {
                lSM_40226 = lSM_40198.AddStateMachine(_EditorAnimatorSMName, new Vector3(636, 264, 0));
            }

            UnityEditor.Animations.AnimatorState lS_40864 = lSM_40226.AddState("Spell Idle Out", new Vector3(1416, 132, 0));
            lS_40864.speed = 0.3f;
            lS_40864.motion = m20438;

            UnityEditor.Animations.AnimatorState lS_40494 = lSM_40226.AddState("1H_Cast_01", new Vector3(648, -216, 0));
            lS_40494.speed = 1.2f;
            lS_40494.motion = m30130;

            UnityEditor.Animations.AnimatorState lS_40488 = lSM_40226.AddState("Stand Idle In", new Vector3(300, 144, 0));
            lS_40488.speed = 1.4f;
            lS_40488.motion = m19590;

            UnityEditor.Animations.AnimatorState lS_40866 = lSM_40226.AddState("Stand Idle Transition", new Vector3(1428, 276, 0));
            lS_40866.speed = -1.4f;
            lS_40866.motion = m19590;

            UnityEditor.Animations.AnimatorState lS_40868 = lSM_40226.AddState("Stand Idle Out", new Vector3(1656, 276, 0));
            lS_40868.speed = 1f;
            lS_40868.motion = m17094;

            UnityEditor.Animations.AnimatorState lS_40496 = lSM_40226.AddState("2H_Cast_01_a", new Vector3(648, 444, 0));
            lS_40496.speed = 1f;
            lS_40496.motion = m16546;

            UnityEditor.Animations.AnimatorState lS_40870 = lSM_40226.AddState("2H_Cast_01_c", new Vector3(1104, 444, 0));
            lS_40870.speed = 1f;
            lS_40870.motion = m16550;

            UnityEditor.Animations.AnimatorState lS_40872 = lSM_40226.AddState("2H_Cast_01_b", new Vector3(876, 444, 0));
            lS_40872.speed = 0.5f;
            lS_40872.motion = m16548;

            UnityEditor.Animations.AnimatorState lS_40504 = lSM_40226.AddState("2H_Cast_01", new Vector3(648, 396, 0));
            lS_40504.speed = 0.6f;
            lS_40504.motion = m16544;

            UnityEditor.Animations.AnimatorState lS_40506 = lSM_40226.AddState("1H_Cast_01_a", new Vector3(648, -168, 0));
            lS_40506.speed = 1f;
            lS_40506.motion = m30124;

            UnityEditor.Animations.AnimatorState lS_40874 = lSM_40226.AddState("1H_Cast_01_b", new Vector3(876, -168, 0));
            lS_40874.speed = 1f;
            lS_40874.motion = m30126;

            UnityEditor.Animations.AnimatorState lS_40876 = lSM_40226.AddState("1H_Cast_01_c", new Vector3(1104, -168, 0));
            lS_40876.speed = 1f;
            lS_40876.motion = m30128;

            UnityEditor.Animations.AnimatorState lS_N29072 = lSM_40226.AddState("Interrupted", new Vector3(648, -312, 0));
            lS_N29072.speed = 1f;
            lS_N29072.motion = m31788;

            UnityEditor.Animations.AnimatorState lS_N454678 = lSM_40226.AddState("1H_Cast_02", new Vector3(648, -84, 0));
            lS_N454678.speed = 1f;
            lS_N454678.motion = m23758;

            UnityEditor.Animations.AnimatorState lS_N498894 = lSM_40226.AddState("1H_Cast_02_a", new Vector3(648, -36, 0));
            lS_N498894.speed = 1f;
            lS_N498894.motion = m186464;

            UnityEditor.Animations.AnimatorState lS_N499114 = lSM_40226.AddState("1H_Cast_02_b", new Vector3(876, -36, 0));
            lS_N499114.speed = 1f;
            lS_N499114.motion = m186466;

            UnityEditor.Animations.AnimatorState lS_N499298 = lSM_40226.AddState("1H_Cast_02_c", new Vector3(1104, -36, 0));
            lS_N499298.speed = 1f;
            lS_N499298.motion = m186468;

            UnityEditor.Animations.AnimatorState lS_N564636 = lSM_40226.AddState("2H_Cast_02", new Vector3(648, 528, 0));
            lS_N564636.speed = 1f;
            lS_N564636.motion = m22246;

            UnityEditor.Animations.AnimatorState lS_N1020548 = lSM_40226.AddState("2H_Cast_02_a", new Vector3(648, 576, 0));
            lS_N1020548.speed = 1f;
            lS_N1020548.motion = m241242;

            UnityEditor.Animations.AnimatorState lS_N1020718 = lSM_40226.AddState("2H_Cast_02_b", new Vector3(876, 576, 0));
            lS_N1020718.speed = 1f;
            lS_N1020718.motion = m241244;

            UnityEditor.Animations.AnimatorState lS_N1021296 = lSM_40226.AddState("2H_Cast_02_c", new Vector3(1104, 576, 0));
            lS_N1021296.speed = 1f;
            lS_N1021296.motion = m241246;

            UnityEditor.Animations.AnimatorState lS_N1021434 = lSM_40226.AddState("2H_Cast_03", new Vector3(648, 672, 0));
            lS_N1021434.speed = 1f;
            lS_N1021434.motion = m242640;

            UnityEditor.Animations.AnimatorState lS_N1021608 = lSM_40226.AddState("2H_Cast_03_a", new Vector3(648, 720, 0));
            lS_N1021608.speed = 1f;
            lS_N1021608.motion = m242642;

            UnityEditor.Animations.AnimatorState lS_N1021800 = lSM_40226.AddState("2H_Cast_03_b", new Vector3(876, 720, 0));
            lS_N1021800.speed = 1f;
            lS_N1021800.motion = m242644;

            UnityEditor.Animations.AnimatorState lS_N1022012 = lSM_40226.AddState("2H_Cast_03_c", new Vector3(1104, 720, 0));
            lS_N1022012.speed = 1f;
            lS_N1022012.motion = m242646;

            UnityEditor.Animations.AnimatorState lS_N1022170 = lSM_40226.AddState("2H_Cast_04", new Vector3(648, 804, 0));
            lS_N1022170.speed = 1f;
            lS_N1022170.motion = m242650;

            UnityEditor.Animations.AnimatorState lS_N1022354 = lSM_40226.AddState("2H_Cast_04_a", new Vector3(648, 852, 0));
            lS_N1022354.speed = 1f;
            lS_N1022354.motion = m242652;

            UnityEditor.Animations.AnimatorState lS_N1022574 = lSM_40226.AddState("2H_Cast_04_b", new Vector3(876, 852, 0));
            lS_N1022574.speed = 1f;
            lS_N1022574.motion = m242654;

            UnityEditor.Animations.AnimatorState lS_N1022762 = lSM_40226.AddState("2H_Cast_04_c", new Vector3(1104, 852, 0));
            lS_N1022762.speed = 1f;
            lS_N1022762.motion = m242656;

            UnityEditor.Animations.AnimatorState lS_N1022932 = lSM_40226.AddState("2H_Cast_05", new Vector3(648, 948, 0));
            lS_N1022932.speed = 1f;
            lS_N1022932.motion = m242660;

            UnityEditor.Animations.AnimatorState lS_N1023122 = lSM_40226.AddState("2H_Cast_05_a", new Vector3(648, 996, 0));
            lS_N1023122.speed = 1f;
            lS_N1023122.motion = m242662;

            UnityEditor.Animations.AnimatorState lS_N1023314 = lSM_40226.AddState("2H_Cast_05_b", new Vector3(876, 996, 0));
            lS_N1023314.speed = 1f;
            lS_N1023314.motion = m242664;

            UnityEditor.Animations.AnimatorState lS_N1023542 = lSM_40226.AddState("2H_Cast_05_c", new Vector3(1104, 996, 0));
            lS_N1023542.speed = 1f;
            lS_N1023542.motion = m242666;

            UnityEditor.Animations.AnimatorState lS_N1023716 = lSM_40226.AddState("2H_Cast_06", new Vector3(648, 1092, 0));
            lS_N1023716.speed = 1f;
            lS_N1023716.motion = m242670;

            UnityEditor.Animations.AnimatorState lS_N1023916 = lSM_40226.AddState("2H_Cast_06_a", new Vector3(648, 1140, 0));
            lS_N1023916.speed = 1f;
            lS_N1023916.motion = m242672;

            UnityEditor.Animations.AnimatorState lS_N1024118 = lSM_40226.AddState("2H_Cast_06_b", new Vector3(876, 1140, 0));
            lS_N1024118.speed = 1f;
            lS_N1024118.motion = m242674;

            UnityEditor.Animations.AnimatorState lS_N1024356 = lSM_40226.AddState("2H_Cast_06_c", new Vector3(1104, 1140, 0));
            lS_N1024356.speed = 1f;
            lS_N1024356.motion = m242676;

            UnityEditor.Animations.AnimatorState lS_N1024532 = lSM_40226.AddState("2H_Cast_07", new Vector3(648, 1236, 0));
            lS_N1024532.speed = 1f;
            lS_N1024532.motion = m242680;

            UnityEditor.Animations.AnimatorState lS_N1024740 = lSM_40226.AddState("2H_Cast_07_a", new Vector3(648, 1284, 0));
            lS_N1024740.speed = 1f;
            lS_N1024740.motion = m242682;

            UnityEditor.Animations.AnimatorState lS_N1024984 = lSM_40226.AddState("2H_Cast_07_b", new Vector3(876, 1284, 0));
            lS_N1024984.speed = 1f;
            lS_N1024984.motion = m242684;

            UnityEditor.Animations.AnimatorState lS_N1025196 = lSM_40226.AddState("2H_Cast_07_c", new Vector3(1104, 1284, 0));
            lS_N1025196.speed = 1f;
            lS_N1025196.motion = m15810;

            UnityEditor.Animations.AnimatorState lS_N1025392 = lSM_40226.AddState("2H_Cast_08", new Vector3(648, 1368, 0));
            lS_N1025392.speed = 1f;
            lS_N1025392.motion = m242690;

            UnityEditor.Animations.AnimatorState lS_N1025642 = lSM_40226.AddState("2H_Cast_08_a", new Vector3(648, 1416, 0));
            lS_N1025642.speed = 1f;
            lS_N1025642.motion = m242692;

            UnityEditor.Animations.AnimatorState lS_N1025928 = lSM_40226.AddState("2H_Cast_08_b", new Vector3(876, 1416, 0));
            lS_N1025928.speed = 1f;
            lS_N1025928.motion = m242694;

            UnityEditor.Animations.AnimatorState lS_N1026150 = lSM_40226.AddState("2H_Cast_08_c", new Vector3(1104, 1416, 0));
            lS_N1026150.speed = 1f;
            lS_N1026150.motion = m242696;

            UnityEditor.Animations.AnimatorState lS_N1026512 = lSM_40226.AddState("1H_Cast_03", new Vector3(648, 48, 0));
            lS_N1026512.speed = 1f;
            lS_N1026512.motion = m242620;

            UnityEditor.Animations.AnimatorState lS_N1026734 = lSM_40226.AddState("1H_Cast_03_a", new Vector3(648, 96, 0));
            lS_N1026734.speed = 1f;
            lS_N1026734.motion = m242622;

            UnityEditor.Animations.AnimatorState lS_N1026958 = lSM_40226.AddState("1H_Cast_03_b", new Vector3(876, 96, 0));
            lS_N1026958.speed = 1f;
            lS_N1026958.motion = m242624;

            UnityEditor.Animations.AnimatorState lS_N1027218 = lSM_40226.AddState("1H_Cast_03_c", new Vector3(1104, 96, 0));
            lS_N1027218.speed = 1f;
            lS_N1027218.motion = m242626;

            UnityEditor.Animations.AnimatorState lS_N1028980 = lSM_40226.AddState("1H_Cast_04", new Vector3(648, 180, 0));
            lS_N1028980.speed = 1f;
            lS_N1028980.motion = m242630;

            UnityEditor.Animations.AnimatorState lS_N1029194 = lSM_40226.AddState("1H_Cast_04_a", new Vector3(648, 228, 0));
            lS_N1029194.speed = 1f;
            lS_N1029194.motion = m242632;

            UnityEditor.Animations.AnimatorState lS_N1029464 = lSM_40226.AddState("1H_Cast_04_b", new Vector3(876, 228, 0));
            lS_N1029464.speed = 1f;
            lS_N1029464.motion = m242634;

            UnityEditor.Animations.AnimatorState lS_N1029702 = lSM_40226.AddState("1H_Cast_04_c", new Vector3(1104, 228, 0));
            lS_N1029702.speed = 1f;
            lS_N1029702.motion = m242636;

            // Create the transition from the any state. Note that 'AnyState' transitions have to be added to the root
            UnityEditor.Animations.AnimatorStateTransition lT_40298 = lRootStateMachine.AddAnyStateTransition(lS_40488);
            lT_40298.hasExitTime = false;
            lT_40298.hasFixedDuration = true;
            lT_40298.exitTime = 0.9f;
            lT_40298.duration = 0.1f;
            lT_40298.offset = 0f;
            lT_40298.mute = false;
            lT_40298.solo = false;
            lT_40298.canTransitionToSelf = true;
            lT_40298.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            lT_40298.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 32141f, "L0MotionPhase");

            // Create the transition from the any state. Note that 'AnyState' transitions have to be added to the root
            UnityEditor.Animations.AnimatorStateTransition lT_40304 = lRootStateMachine.AddAnyStateTransition(lS_40494);
            lT_40304.hasExitTime = false;
            lT_40304.hasFixedDuration = true;
            lT_40304.exitTime = 0.9f;
            lT_40304.duration = 0.2f;
            lT_40304.offset = 0f;
            lT_40304.mute = false;
            lT_40304.solo = false;
            lT_40304.canTransitionToSelf = true;
            lT_40304.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            lT_40304.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 32140f, "L0MotionPhase");
            lT_40304.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 0f, "L0MotionParameter");

            // Create the transition from the any state. Note that 'AnyState' transitions have to be added to the root
            UnityEditor.Animations.AnimatorStateTransition lT_40306 = lRootStateMachine.AddAnyStateTransition(lS_40496);
            lT_40306.hasExitTime = false;
            lT_40306.hasFixedDuration = true;
            lT_40306.exitTime = 0.9f;
            lT_40306.duration = 0.1f;
            lT_40306.offset = 0f;
            lT_40306.mute = false;
            lT_40306.solo = false;
            lT_40306.canTransitionToSelf = true;
            lT_40306.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            lT_40306.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 32140f, "L0MotionPhase");
            lT_40306.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 3f, "L0MotionParameter");

            // Create the transition from the any state. Note that 'AnyState' transitions have to be added to the root
            UnityEditor.Animations.AnimatorStateTransition lT_40314 = lRootStateMachine.AddAnyStateTransition(lS_40504);
            lT_40314.hasExitTime = false;
            lT_40314.hasFixedDuration = true;
            lT_40314.exitTime = 0.9f;
            lT_40314.duration = 0.2f;
            lT_40314.offset = 0f;
            lT_40314.mute = false;
            lT_40314.solo = false;
            lT_40314.canTransitionToSelf = true;
            lT_40314.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            lT_40314.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 32140f, "L0MotionPhase");
            lT_40314.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 2f, "L0MotionParameter");

            // Create the transition from the any state. Note that 'AnyState' transitions have to be added to the root
            UnityEditor.Animations.AnimatorStateTransition lT_40316 = lRootStateMachine.AddAnyStateTransition(lS_40506);
            lT_40316.hasExitTime = false;
            lT_40316.hasFixedDuration = true;
            lT_40316.exitTime = 0.9f;
            lT_40316.duration = 0.1f;
            lT_40316.offset = 0f;
            lT_40316.mute = false;
            lT_40316.solo = false;
            lT_40316.canTransitionToSelf = true;
            lT_40316.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            lT_40316.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 32140f, "L0MotionPhase");
            lT_40316.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 1f, "L0MotionParameter");

            // Create the transition from the any state. Note that 'AnyState' transitions have to be added to the root
            UnityEditor.Animations.AnimatorStateTransition lT_N29390 = lRootStateMachine.AddAnyStateTransition(lS_N29072);
            lT_N29390.hasExitTime = false;
            lT_N29390.hasFixedDuration = true;
            lT_N29390.exitTime = 0.75f;
            lT_N29390.duration = 0.25f;
            lT_N29390.offset = 0f;
            lT_N29390.mute = false;
            lT_N29390.solo = false;
            lT_N29390.canTransitionToSelf = true;
            lT_N29390.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            lT_N29390.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 32145f, "L0MotionPhase");

            // Create the transition from the any state. Note that 'AnyState' transitions have to be added to the root
            UnityEditor.Animations.AnimatorStateTransition lT_N455294 = lRootStateMachine.AddAnyStateTransition(lS_N454678);
            lT_N455294.hasExitTime = false;
            lT_N455294.hasFixedDuration = true;
            lT_N455294.exitTime = 0.75f;
            lT_N455294.duration = 0.25f;
            lT_N455294.offset = 0f;
            lT_N455294.mute = false;
            lT_N455294.solo = false;
            lT_N455294.canTransitionToSelf = true;
            lT_N455294.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            lT_N455294.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 32140f, "L0MotionPhase");
            lT_N455294.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 4f, "L0MotionParameter");

            // Create the transition from the any state. Note that 'AnyState' transitions have to be added to the root
            UnityEditor.Animations.AnimatorStateTransition lT_N504214 = lRootStateMachine.AddAnyStateTransition(lS_N498894);
            lT_N504214.hasExitTime = false;
            lT_N504214.hasFixedDuration = true;
            lT_N504214.exitTime = 0.75f;
            lT_N504214.duration = 0.25f;
            lT_N504214.offset = 0f;
            lT_N504214.mute = false;
            lT_N504214.solo = false;
            lT_N504214.canTransitionToSelf = true;
            lT_N504214.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            lT_N504214.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 32140f, "L0MotionPhase");
            lT_N504214.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 5f, "L0MotionParameter");

            // Create the transition from the any state. Note that 'AnyState' transitions have to be added to the root
            UnityEditor.Animations.AnimatorStateTransition lT_N576106 = lRootStateMachine.AddAnyStateTransition(lS_N564636);
            lT_N576106.hasExitTime = false;
            lT_N576106.hasFixedDuration = true;
            lT_N576106.exitTime = 0.75f;
            lT_N576106.duration = 0.25f;
            lT_N576106.offset = 0f;
            lT_N576106.mute = false;
            lT_N576106.solo = false;
            lT_N576106.canTransitionToSelf = true;
            lT_N576106.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            lT_N576106.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 32140f, "L0MotionPhase");
            lT_N576106.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 10f, "L0MotionParameter");

            // Create the transition from the any state. Note that 'AnyState' transitions have to be added to the root
            UnityEditor.Animations.AnimatorStateTransition lT_N1551246 = lRootStateMachine.AddAnyStateTransition(lS_N1026512);
            lT_N1551246.hasExitTime = false;
            lT_N1551246.hasFixedDuration = true;
            lT_N1551246.exitTime = 0.75f;
            lT_N1551246.duration = 0.25f;
            lT_N1551246.offset = 0f;
            lT_N1551246.mute = false;
            lT_N1551246.solo = false;
            lT_N1551246.canTransitionToSelf = true;
            lT_N1551246.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            lT_N1551246.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 32140f, "L0MotionPhase");
            lT_N1551246.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 6f, "L0MotionParameter");

            // Create the transition from the any state. Note that 'AnyState' transitions have to be added to the root
            UnityEditor.Animations.AnimatorStateTransition lT_N1551478 = lRootStateMachine.AddAnyStateTransition(lS_N1026734);
            lT_N1551478.hasExitTime = false;
            lT_N1551478.hasFixedDuration = true;
            lT_N1551478.exitTime = 0.75f;
            lT_N1551478.duration = 0.25f;
            lT_N1551478.offset = 0f;
            lT_N1551478.mute = false;
            lT_N1551478.solo = false;
            lT_N1551478.canTransitionToSelf = true;
            lT_N1551478.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            lT_N1551478.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 32140f, "L0MotionPhase");
            lT_N1551478.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 7f, "L0MotionParameter");

            // Create the transition from the any state. Note that 'AnyState' transitions have to be added to the root
            UnityEditor.Animations.AnimatorStateTransition lT_N1551710 = lRootStateMachine.AddAnyStateTransition(lS_N1028980);
            lT_N1551710.hasExitTime = false;
            lT_N1551710.hasFixedDuration = true;
            lT_N1551710.exitTime = 0.75f;
            lT_N1551710.duration = 0.25f;
            lT_N1551710.offset = 0f;
            lT_N1551710.mute = false;
            lT_N1551710.solo = false;
            lT_N1551710.canTransitionToSelf = true;
            lT_N1551710.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            lT_N1551710.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 32140f, "L0MotionPhase");
            lT_N1551710.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 8f, "L0MotionParameter");

            // Create the transition from the any state. Note that 'AnyState' transitions have to be added to the root
            UnityEditor.Animations.AnimatorStateTransition lT_N1551942 = lRootStateMachine.AddAnyStateTransition(lS_N1029194);
            lT_N1551942.hasExitTime = false;
            lT_N1551942.hasFixedDuration = true;
            lT_N1551942.exitTime = 0.75f;
            lT_N1551942.duration = 0.25f;
            lT_N1551942.offset = 0f;
            lT_N1551942.mute = false;
            lT_N1551942.solo = false;
            lT_N1551942.canTransitionToSelf = true;
            lT_N1551942.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            lT_N1551942.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 32140f, "L0MotionPhase");
            lT_N1551942.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 9f, "L0MotionParameter");

            // Create the transition from the any state. Note that 'AnyState' transitions have to be added to the root
            UnityEditor.Animations.AnimatorStateTransition lT_N1554376 = lRootStateMachine.AddAnyStateTransition(lS_N1020548);
            lT_N1554376.hasExitTime = false;
            lT_N1554376.hasFixedDuration = true;
            lT_N1554376.exitTime = 0.75f;
            lT_N1554376.duration = 0.25f;
            lT_N1554376.offset = 0f;
            lT_N1554376.mute = false;
            lT_N1554376.solo = false;
            lT_N1554376.canTransitionToSelf = true;
            lT_N1554376.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            lT_N1554376.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 32140f, "L0MotionPhase");
            lT_N1554376.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 11f, "L0MotionParameter");

            // Create the transition from the any state. Note that 'AnyState' transitions have to be added to the root
            UnityEditor.Animations.AnimatorStateTransition lT_N1554610 = lRootStateMachine.AddAnyStateTransition(lS_N1021434);
            lT_N1554610.hasExitTime = false;
            lT_N1554610.hasFixedDuration = true;
            lT_N1554610.exitTime = 0.75f;
            lT_N1554610.duration = 0.25f;
            lT_N1554610.offset = 0f;
            lT_N1554610.mute = false;
            lT_N1554610.solo = false;
            lT_N1554610.canTransitionToSelf = true;
            lT_N1554610.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            lT_N1554610.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 32140f, "L0MotionPhase");
            lT_N1554610.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 12f, "L0MotionParameter");

            // Create the transition from the any state. Note that 'AnyState' transitions have to be added to the root
            UnityEditor.Animations.AnimatorStateTransition lT_N1554842 = lRootStateMachine.AddAnyStateTransition(lS_N1021608);
            lT_N1554842.hasExitTime = false;
            lT_N1554842.hasFixedDuration = true;
            lT_N1554842.exitTime = 0.75f;
            lT_N1554842.duration = 0.25f;
            lT_N1554842.offset = 0f;
            lT_N1554842.mute = false;
            lT_N1554842.solo = false;
            lT_N1554842.canTransitionToSelf = true;
            lT_N1554842.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            lT_N1554842.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 32140f, "L0MotionPhase");
            lT_N1554842.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 13f, "L0MotionParameter");

            // Create the transition from the any state. Note that 'AnyState' transitions have to be added to the root
            UnityEditor.Animations.AnimatorStateTransition lT_N1555082 = lRootStateMachine.AddAnyStateTransition(lS_N1022170);
            lT_N1555082.hasExitTime = false;
            lT_N1555082.hasFixedDuration = true;
            lT_N1555082.exitTime = 0.75f;
            lT_N1555082.duration = 0.25f;
            lT_N1555082.offset = 0f;
            lT_N1555082.mute = false;
            lT_N1555082.solo = false;
            lT_N1555082.canTransitionToSelf = true;
            lT_N1555082.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            lT_N1555082.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 32140f, "L0MotionPhase");
            lT_N1555082.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 14f, "L0MotionParameter");

            // Create the transition from the any state. Note that 'AnyState' transitions have to be added to the root
            UnityEditor.Animations.AnimatorStateTransition lT_N1555314 = lRootStateMachine.AddAnyStateTransition(lS_N1022354);
            lT_N1555314.hasExitTime = false;
            lT_N1555314.hasFixedDuration = true;
            lT_N1555314.exitTime = 0.75f;
            lT_N1555314.duration = 0.25f;
            lT_N1555314.offset = 0f;
            lT_N1555314.mute = false;
            lT_N1555314.solo = false;
            lT_N1555314.canTransitionToSelf = true;
            lT_N1555314.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            lT_N1555314.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 32140f, "L0MotionPhase");
            lT_N1555314.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 15f, "L0MotionParameter");

            // Create the transition from the any state. Note that 'AnyState' transitions have to be added to the root
            UnityEditor.Animations.AnimatorStateTransition lT_N1556410 = lRootStateMachine.AddAnyStateTransition(lS_N1022932);
            lT_N1556410.hasExitTime = false;
            lT_N1556410.hasFixedDuration = true;
            lT_N1556410.exitTime = 0.75f;
            lT_N1556410.duration = 0.25f;
            lT_N1556410.offset = 0f;
            lT_N1556410.mute = false;
            lT_N1556410.solo = false;
            lT_N1556410.canTransitionToSelf = true;
            lT_N1556410.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            lT_N1556410.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 32140f, "L0MotionPhase");
            lT_N1556410.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 16f, "L0MotionParameter");

            // Create the transition from the any state. Note that 'AnyState' transitions have to be added to the root
            UnityEditor.Animations.AnimatorStateTransition lT_N1556656 = lRootStateMachine.AddAnyStateTransition(lS_N1023122);
            lT_N1556656.hasExitTime = false;
            lT_N1556656.hasFixedDuration = true;
            lT_N1556656.exitTime = 0.75f;
            lT_N1556656.duration = 0.25f;
            lT_N1556656.offset = 0f;
            lT_N1556656.mute = false;
            lT_N1556656.solo = false;
            lT_N1556656.canTransitionToSelf = true;
            lT_N1556656.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            lT_N1556656.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 32140f, "L0MotionPhase");
            lT_N1556656.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 17f, "L0MotionParameter");

            // Create the transition from the any state. Note that 'AnyState' transitions have to be added to the root
            UnityEditor.Animations.AnimatorStateTransition lT_N1556890 = lRootStateMachine.AddAnyStateTransition(lS_N1023716);
            lT_N1556890.hasExitTime = false;
            lT_N1556890.hasFixedDuration = true;
            lT_N1556890.exitTime = 0.75f;
            lT_N1556890.duration = 0.25f;
            lT_N1556890.offset = 0f;
            lT_N1556890.mute = false;
            lT_N1556890.solo = false;
            lT_N1556890.canTransitionToSelf = true;
            lT_N1556890.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            lT_N1556890.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 32140f, "L0MotionPhase");
            lT_N1556890.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 18f, "L0MotionParameter");

            // Create the transition from the any state. Note that 'AnyState' transitions have to be added to the root
            UnityEditor.Animations.AnimatorStateTransition lT_N1557122 = lRootStateMachine.AddAnyStateTransition(lS_N1023916);
            lT_N1557122.hasExitTime = false;
            lT_N1557122.hasFixedDuration = true;
            lT_N1557122.exitTime = 0.75f;
            lT_N1557122.duration = 0.25f;
            lT_N1557122.offset = 0f;
            lT_N1557122.mute = false;
            lT_N1557122.solo = false;
            lT_N1557122.canTransitionToSelf = true;
            lT_N1557122.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            lT_N1557122.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 32140f, "L0MotionPhase");
            lT_N1557122.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 19f, "L0MotionParameter");

            // Create the transition from the any state. Note that 'AnyState' transitions have to be added to the root
            UnityEditor.Animations.AnimatorStateTransition lT_N1557368 = lRootStateMachine.AddAnyStateTransition(lS_N1024532);
            lT_N1557368.hasExitTime = false;
            lT_N1557368.hasFixedDuration = true;
            lT_N1557368.exitTime = 0.75f;
            lT_N1557368.duration = 0.25f;
            lT_N1557368.offset = 0f;
            lT_N1557368.mute = false;
            lT_N1557368.solo = false;
            lT_N1557368.canTransitionToSelf = true;
            lT_N1557368.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            lT_N1557368.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 32140f, "L0MotionPhase");
            lT_N1557368.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 20f, "L0MotionParameter");

            // Create the transition from the any state. Note that 'AnyState' transitions have to be added to the root
            UnityEditor.Animations.AnimatorStateTransition lT_N1557600 = lRootStateMachine.AddAnyStateTransition(lS_N1024740);
            lT_N1557600.hasExitTime = false;
            lT_N1557600.hasFixedDuration = true;
            lT_N1557600.exitTime = 0.75f;
            lT_N1557600.duration = 0.25f;
            lT_N1557600.offset = 0f;
            lT_N1557600.mute = false;
            lT_N1557600.solo = false;
            lT_N1557600.canTransitionToSelf = true;
            lT_N1557600.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            lT_N1557600.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 32140f, "L0MotionPhase");
            lT_N1557600.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 21f, "L0MotionParameter");

            // Create the transition from the any state. Note that 'AnyState' transitions have to be added to the root
            UnityEditor.Animations.AnimatorStateTransition lT_N1557832 = lRootStateMachine.AddAnyStateTransition(lS_N1025392);
            lT_N1557832.hasExitTime = false;
            lT_N1557832.hasFixedDuration = true;
            lT_N1557832.exitTime = 0.75f;
            lT_N1557832.duration = 0.25f;
            lT_N1557832.offset = 0f;
            lT_N1557832.mute = false;
            lT_N1557832.solo = false;
            lT_N1557832.canTransitionToSelf = true;
            lT_N1557832.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            lT_N1557832.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 32140f, "L0MotionPhase");
            lT_N1557832.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 22f, "L0MotionParameter");

            // Create the transition from the any state. Note that 'AnyState' transitions have to be added to the root
            UnityEditor.Animations.AnimatorStateTransition lT_N1558064 = lRootStateMachine.AddAnyStateTransition(lS_N1025642);
            lT_N1558064.hasExitTime = false;
            lT_N1558064.hasFixedDuration = true;
            lT_N1558064.exitTime = 0.75f;
            lT_N1558064.duration = 0.25f;
            lT_N1558064.offset = 0f;
            lT_N1558064.mute = false;
            lT_N1558064.solo = false;
            lT_N1558064.canTransitionToSelf = true;
            lT_N1558064.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            lT_N1558064.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 32140f, "L0MotionPhase");
            lT_N1558064.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 23f, "L0MotionParameter");

            UnityEditor.Animations.AnimatorStateTransition lT_40878 = lS_40494.AddTransition(lS_40864);
            lT_40878.hasExitTime = true;
            lT_40878.hasFixedDuration = true;
            lT_40878.exitTime = 0.9f;
            lT_40878.duration = 0.15f;
            lT_40878.offset = 0f;
            lT_40878.mute = false;
            lT_40878.solo = false;
            lT_40878.canTransitionToSelf = true;
            lT_40878.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            lT_40878.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 0f, "L0MotionParameter");

            UnityEditor.Animations.AnimatorStateTransition lT_40880 = lS_40494.AddTransition(lS_40866);
            lT_40880.hasExitTime = true;
            lT_40880.hasFixedDuration = true;
            lT_40880.exitTime = 0.6975827f;
            lT_40880.duration = 0.25f;
            lT_40880.offset = 0f;
            lT_40880.mute = false;
            lT_40880.solo = false;
            lT_40880.canTransitionToSelf = true;
            lT_40880.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            lT_40880.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 1f, "L0MotionParameter");

            UnityEditor.Animations.AnimatorStateTransition lT_40882 = lS_40488.AddTransition(lS_40494);
            lT_40882.hasExitTime = true;
            lT_40882.hasFixedDuration = true;
            lT_40882.exitTime = 0.3048472f;
            lT_40882.duration = 0.2500001f;
            lT_40882.offset = 0f;
            lT_40882.mute = false;
            lT_40882.solo = false;
            lT_40882.canTransitionToSelf = true;
            lT_40882.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            lT_40882.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 0f, "L0MotionParameter");

            UnityEditor.Animations.AnimatorStateTransition lT_40884 = lS_40488.AddTransition(lS_40496);
            lT_40884.hasExitTime = true;
            lT_40884.hasFixedDuration = true;
            lT_40884.exitTime = 0.5648073f;
            lT_40884.duration = 0.1000001f;
            lT_40884.offset = 0f;
            lT_40884.mute = false;
            lT_40884.solo = false;
            lT_40884.canTransitionToSelf = true;
            lT_40884.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            lT_40884.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 3f, "L0MotionParameter");

            UnityEditor.Animations.AnimatorStateTransition lT_40886 = lS_40488.AddTransition(lS_40504);
            lT_40886.hasExitTime = true;
            lT_40886.hasFixedDuration = true;
            lT_40886.exitTime = 0.3f;
            lT_40886.duration = 0.25f;
            lT_40886.offset = 0f;
            lT_40886.mute = false;
            lT_40886.solo = false;
            lT_40886.canTransitionToSelf = true;
            lT_40886.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            lT_40886.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 2f, "L0MotionParameter");

            UnityEditor.Animations.AnimatorStateTransition lT_40888 = lS_40488.AddTransition(lS_40506);
            lT_40888.hasExitTime = true;
            lT_40888.hasFixedDuration = true;
            lT_40888.exitTime = 0.3048472f;
            lT_40888.duration = 0.1000001f;
            lT_40888.offset = 0f;
            lT_40888.mute = false;
            lT_40888.solo = false;
            lT_40888.canTransitionToSelf = true;
            lT_40888.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            lT_40888.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 1f, "L0MotionParameter");

            UnityEditor.Animations.AnimatorStateTransition lT_N499578 = lS_40488.AddTransition(lS_N454678);
            lT_N499578.hasExitTime = true;
            lT_N499578.hasFixedDuration = true;
            lT_N499578.exitTime = 0.7115385f;
            lT_N499578.duration = 0.25f;
            lT_N499578.offset = 0f;
            lT_N499578.mute = false;
            lT_N499578.solo = false;
            lT_N499578.canTransitionToSelf = true;
            lT_N499578.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            lT_N499578.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 4f, "L0MotionParameter");

            UnityEditor.Animations.AnimatorStateTransition lT_N504720 = lS_40488.AddTransition(lS_N498894);
            lT_N504720.hasExitTime = true;
            lT_N504720.hasFixedDuration = true;
            lT_N504720.exitTime = 0.7115385f;
            lT_N504720.duration = 0.25f;
            lT_N504720.offset = 0f;
            lT_N504720.mute = false;
            lT_N504720.solo = false;
            lT_N504720.canTransitionToSelf = true;
            lT_N504720.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            lT_N504720.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 5f, "L0MotionParameter");

            UnityEditor.Animations.AnimatorStateTransition lT_N577512 = lS_40488.AddTransition(lS_N564636);
            lT_N577512.hasExitTime = true;
            lT_N577512.hasFixedDuration = true;
            lT_N577512.exitTime = 0.7115385f;
            lT_N577512.duration = 0.25f;
            lT_N577512.offset = 0f;
            lT_N577512.mute = false;
            lT_N577512.solo = false;
            lT_N577512.canTransitionToSelf = true;
            lT_N577512.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            lT_N577512.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 10f, "L0MotionParameter");

            UnityEditor.Animations.AnimatorStateTransition lT_N1428472 = lS_40488.AddTransition(lS_N1026512);
            lT_N1428472.hasExitTime = true;
            lT_N1428472.hasFixedDuration = true;
            lT_N1428472.exitTime = 0.7115385f;
            lT_N1428472.duration = 0.25f;
            lT_N1428472.offset = 0f;
            lT_N1428472.mute = false;
            lT_N1428472.solo = false;
            lT_N1428472.canTransitionToSelf = true;
            lT_N1428472.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            lT_N1428472.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 6f, "L0MotionParameter");

            UnityEditor.Animations.AnimatorStateTransition lT_N1428704 = lS_40488.AddTransition(lS_N1026734);
            lT_N1428704.hasExitTime = true;
            lT_N1428704.hasFixedDuration = true;
            lT_N1428704.exitTime = 0.7115385f;
            lT_N1428704.duration = 0.25f;
            lT_N1428704.offset = 0f;
            lT_N1428704.mute = false;
            lT_N1428704.solo = false;
            lT_N1428704.canTransitionToSelf = true;
            lT_N1428704.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            lT_N1428704.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 7f, "L0MotionParameter");

            UnityEditor.Animations.AnimatorStateTransition lT_N1428936 = lS_40488.AddTransition(lS_N1028980);
            lT_N1428936.hasExitTime = true;
            lT_N1428936.hasFixedDuration = true;
            lT_N1428936.exitTime = 0.7115385f;
            lT_N1428936.duration = 0.25f;
            lT_N1428936.offset = 0f;
            lT_N1428936.mute = false;
            lT_N1428936.solo = false;
            lT_N1428936.canTransitionToSelf = true;
            lT_N1428936.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            lT_N1428936.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 8f, "L0MotionParameter");

            UnityEditor.Animations.AnimatorStateTransition lT_N1429168 = lS_40488.AddTransition(lS_N1029194);
            lT_N1429168.hasExitTime = true;
            lT_N1429168.hasFixedDuration = true;
            lT_N1429168.exitTime = 0.7115385f;
            lT_N1429168.duration = 0.25f;
            lT_N1429168.offset = 0f;
            lT_N1429168.mute = false;
            lT_N1429168.solo = false;
            lT_N1429168.canTransitionToSelf = true;
            lT_N1429168.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            lT_N1429168.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 9f, "L0MotionParameter");

            UnityEditor.Animations.AnimatorStateTransition lT_N1452288 = lS_40488.AddTransition(lS_N1020548);
            lT_N1452288.hasExitTime = true;
            lT_N1452288.hasFixedDuration = true;
            lT_N1452288.exitTime = 0.7115385f;
            lT_N1452288.duration = 0.25f;
            lT_N1452288.offset = 0f;
            lT_N1452288.mute = false;
            lT_N1452288.solo = false;
            lT_N1452288.canTransitionToSelf = true;
            lT_N1452288.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            lT_N1452288.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 11f, "L0MotionParameter");

            UnityEditor.Animations.AnimatorStateTransition lT_N1457592 = lS_40488.AddTransition(lS_N1021434);
            lT_N1457592.hasExitTime = true;
            lT_N1457592.hasFixedDuration = true;
            lT_N1457592.exitTime = 0.7115385f;
            lT_N1457592.duration = 0.25f;
            lT_N1457592.offset = 0f;
            lT_N1457592.mute = false;
            lT_N1457592.solo = false;
            lT_N1457592.canTransitionToSelf = true;
            lT_N1457592.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            lT_N1457592.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 12f, "L0MotionParameter");

            UnityEditor.Animations.AnimatorStateTransition lT_N1457834 = lS_40488.AddTransition(lS_N1021608);
            lT_N1457834.hasExitTime = true;
            lT_N1457834.hasFixedDuration = true;
            lT_N1457834.exitTime = 0.7115385f;
            lT_N1457834.duration = 0.25f;
            lT_N1457834.offset = 0f;
            lT_N1457834.mute = false;
            lT_N1457834.solo = false;
            lT_N1457834.canTransitionToSelf = true;
            lT_N1457834.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            lT_N1457834.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 13f, "L0MotionParameter");

            UnityEditor.Animations.AnimatorStateTransition lT_N1458074 = lS_40488.AddTransition(lS_N1022170);
            lT_N1458074.hasExitTime = true;
            lT_N1458074.hasFixedDuration = true;
            lT_N1458074.exitTime = 0.7115385f;
            lT_N1458074.duration = 0.25f;
            lT_N1458074.offset = 0f;
            lT_N1458074.mute = false;
            lT_N1458074.solo = false;
            lT_N1458074.canTransitionToSelf = true;
            lT_N1458074.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            lT_N1458074.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 14f, "L0MotionParameter");

            UnityEditor.Animations.AnimatorStateTransition lT_N1458306 = lS_40488.AddTransition(lS_N1022354);
            lT_N1458306.hasExitTime = true;
            lT_N1458306.hasFixedDuration = true;
            lT_N1458306.exitTime = 0.7115385f;
            lT_N1458306.duration = 0.25f;
            lT_N1458306.offset = 0f;
            lT_N1458306.mute = false;
            lT_N1458306.solo = false;
            lT_N1458306.canTransitionToSelf = true;
            lT_N1458306.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            lT_N1458306.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 15f, "L0MotionParameter");

            UnityEditor.Animations.AnimatorStateTransition lT_N1458540 = lS_40488.AddTransition(lS_N1022932);
            lT_N1458540.hasExitTime = true;
            lT_N1458540.hasFixedDuration = true;
            lT_N1458540.exitTime = 0.7115385f;
            lT_N1458540.duration = 0.25f;
            lT_N1458540.offset = 0f;
            lT_N1458540.mute = false;
            lT_N1458540.solo = false;
            lT_N1458540.canTransitionToSelf = true;
            lT_N1458540.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            lT_N1458540.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 16f, "L0MotionParameter");

            UnityEditor.Animations.AnimatorStateTransition lT_N1458778 = lS_40488.AddTransition(lS_N1023122);
            lT_N1458778.hasExitTime = true;
            lT_N1458778.hasFixedDuration = true;
            lT_N1458778.exitTime = 0.7115385f;
            lT_N1458778.duration = 0.25f;
            lT_N1458778.offset = 0f;
            lT_N1458778.mute = false;
            lT_N1458778.solo = false;
            lT_N1458778.canTransitionToSelf = true;
            lT_N1458778.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            lT_N1458778.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 17f, "L0MotionParameter");

            UnityEditor.Animations.AnimatorStateTransition lT_N1459012 = lS_40488.AddTransition(lS_N1023716);
            lT_N1459012.hasExitTime = true;
            lT_N1459012.hasFixedDuration = true;
            lT_N1459012.exitTime = 0.7115385f;
            lT_N1459012.duration = 0.25f;
            lT_N1459012.offset = 0f;
            lT_N1459012.mute = false;
            lT_N1459012.solo = false;
            lT_N1459012.canTransitionToSelf = true;
            lT_N1459012.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            lT_N1459012.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 18f, "L0MotionParameter");

            UnityEditor.Animations.AnimatorStateTransition lT_N1459250 = lS_40488.AddTransition(lS_N1023916);
            lT_N1459250.hasExitTime = true;
            lT_N1459250.hasFixedDuration = true;
            lT_N1459250.exitTime = 0.7115385f;
            lT_N1459250.duration = 0.25f;
            lT_N1459250.offset = 0f;
            lT_N1459250.mute = false;
            lT_N1459250.solo = false;
            lT_N1459250.canTransitionToSelf = true;
            lT_N1459250.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            lT_N1459250.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 19f, "L0MotionParameter");

            UnityEditor.Animations.AnimatorStateTransition lT_N1459490 = lS_40488.AddTransition(lS_N1024532);
            lT_N1459490.hasExitTime = true;
            lT_N1459490.hasFixedDuration = true;
            lT_N1459490.exitTime = 0.7115385f;
            lT_N1459490.duration = 0.25f;
            lT_N1459490.offset = 0f;
            lT_N1459490.mute = false;
            lT_N1459490.solo = false;
            lT_N1459490.canTransitionToSelf = true;
            lT_N1459490.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            lT_N1459490.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 20f, "L0MotionParameter");

            UnityEditor.Animations.AnimatorStateTransition lT_N1459722 = lS_40488.AddTransition(lS_N1024740);
            lT_N1459722.hasExitTime = true;
            lT_N1459722.hasFixedDuration = true;
            lT_N1459722.exitTime = 0.7115385f;
            lT_N1459722.duration = 0.25f;
            lT_N1459722.offset = 0f;
            lT_N1459722.mute = false;
            lT_N1459722.solo = false;
            lT_N1459722.canTransitionToSelf = true;
            lT_N1459722.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            lT_N1459722.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 21f, "L0MotionParameter");

            UnityEditor.Animations.AnimatorStateTransition lT_N1459954 = lS_40488.AddTransition(lS_N1025392);
            lT_N1459954.hasExitTime = true;
            lT_N1459954.hasFixedDuration = true;
            lT_N1459954.exitTime = 0.7115385f;
            lT_N1459954.duration = 0.25f;
            lT_N1459954.offset = 0f;
            lT_N1459954.mute = false;
            lT_N1459954.solo = false;
            lT_N1459954.canTransitionToSelf = true;
            lT_N1459954.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            lT_N1459954.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 22f, "L0MotionParameter");

            UnityEditor.Animations.AnimatorStateTransition lT_N1460194 = lS_40488.AddTransition(lS_N1025642);
            lT_N1460194.hasExitTime = true;
            lT_N1460194.hasFixedDuration = true;
            lT_N1460194.exitTime = 0.7115385f;
            lT_N1460194.duration = 0.25f;
            lT_N1460194.offset = 0f;
            lT_N1460194.mute = false;
            lT_N1460194.solo = false;
            lT_N1460194.canTransitionToSelf = true;
            lT_N1460194.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            lT_N1460194.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 23f, "L0MotionParameter");

            UnityEditor.Animations.AnimatorStateTransition lT_40890 = lS_40866.AddTransition(lS_40868);
            lT_40890.hasExitTime = true;
            lT_40890.hasFixedDuration = true;
            lT_40890.exitTime = 0.7692308f;
            lT_40890.duration = 0.25f;
            lT_40890.offset = 0f;
            lT_40890.mute = false;
            lT_40890.solo = false;
            lT_40890.canTransitionToSelf = true;
            lT_40890.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;

            UnityEditor.Animations.AnimatorStateTransition lT_40892 = lS_40496.AddTransition(lS_40872);
            lT_40892.hasExitTime = true;
            lT_40892.hasFixedDuration = true;
            lT_40892.exitTime = 0.8910829f;
            lT_40892.duration = 0.09076428f;
            lT_40892.offset = 0f;
            lT_40892.mute = false;
            lT_40892.solo = false;
            lT_40892.canTransitionToSelf = true;
            lT_40892.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;

            UnityEditor.Animations.AnimatorStateTransition lT_40894 = lS_40870.AddTransition(lS_40864);
            lT_40894.hasExitTime = true;
            lT_40894.hasFixedDuration = true;
            lT_40894.exitTime = 0.8591989f;
            lT_40894.duration = 0.1548803f;
            lT_40894.offset = 17.12114f;
            lT_40894.mute = false;
            lT_40894.solo = false;
            lT_40894.canTransitionToSelf = true;
            lT_40894.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            lT_40894.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 0f, "L0MotionParameter");

            UnityEditor.Animations.AnimatorStateTransition lT_40896 = lS_40870.AddTransition(lS_40866);
            lT_40896.hasExitTime = true;
            lT_40896.hasFixedDuration = true;
            lT_40896.exitTime = 0.7727273f;
            lT_40896.duration = 0.25f;
            lT_40896.offset = 0f;
            lT_40896.mute = false;
            lT_40896.solo = false;
            lT_40896.canTransitionToSelf = true;
            lT_40896.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            lT_40896.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 1f, "L0MotionParameter");

            UnityEditor.Animations.AnimatorStateTransition lT_40898 = lS_40872.AddTransition(lS_40870);
            lT_40898.hasExitTime = false;
            lT_40898.hasFixedDuration = true;
            lT_40898.exitTime = 0f;
            lT_40898.duration = 0.1f;
            lT_40898.offset = 0f;
            lT_40898.mute = false;
            lT_40898.solo = false;
            lT_40898.canTransitionToSelf = true;
            lT_40898.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            lT_40898.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 32144f, "L0MotionPhase");

            UnityEditor.Animations.AnimatorStateTransition lT_40900 = lS_40872.AddTransition(lS_40866);
            lT_40900.hasExitTime = false;
            lT_40900.hasFixedDuration = true;
            lT_40900.exitTime = 0f;
            lT_40900.duration = 0.15f;
            lT_40900.offset = 0f;
            lT_40900.mute = false;
            lT_40900.solo = false;
            lT_40900.canTransitionToSelf = true;
            lT_40900.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            lT_40900.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 32143f, "L0MotionPhase");
            lT_40900.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 1f, "L0MotionParameter");

            UnityEditor.Animations.AnimatorStateTransition lT_40902 = lS_40872.AddTransition(lS_40864);
            lT_40902.hasExitTime = false;
            lT_40902.hasFixedDuration = true;
            lT_40902.exitTime = 0f;
            lT_40902.duration = 0.15f;
            lT_40902.offset = 0f;
            lT_40902.mute = false;
            lT_40902.solo = false;
            lT_40902.canTransitionToSelf = true;
            lT_40902.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            lT_40902.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 32143f, "L0MotionPhase");
            lT_40902.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 0f, "L0MotionParameter");

            UnityEditor.Animations.AnimatorStateTransition lT_40904 = lS_40504.AddTransition(lS_40864);
            lT_40904.hasExitTime = true;
            lT_40904.hasFixedDuration = true;
            lT_40904.exitTime = 1f;
            lT_40904.duration = 0f;
            lT_40904.offset = 0f;
            lT_40904.mute = false;
            lT_40904.solo = false;
            lT_40904.canTransitionToSelf = true;
            lT_40904.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            lT_40904.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 0f, "L0MotionParameter");

            UnityEditor.Animations.AnimatorStateTransition lT_40906 = lS_40504.AddTransition(lS_40866);
            lT_40906.hasExitTime = true;
            lT_40906.hasFixedDuration = true;
            lT_40906.exitTime = 0.8846154f;
            lT_40906.duration = 0.25f;
            lT_40906.offset = 0f;
            lT_40906.mute = false;
            lT_40906.solo = false;
            lT_40906.canTransitionToSelf = true;
            lT_40906.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            lT_40906.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 1f, "L0MotionParameter");

            UnityEditor.Animations.AnimatorStateTransition lT_40908 = lS_40506.AddTransition(lS_40874);
            lT_40908.hasExitTime = true;
            lT_40908.hasFixedDuration = true;
            lT_40908.exitTime = 0.9f;
            lT_40908.duration = 0.1f;
            lT_40908.offset = 0f;
            lT_40908.mute = false;
            lT_40908.solo = false;
            lT_40908.canTransitionToSelf = true;
            lT_40908.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;

            UnityEditor.Animations.AnimatorStateTransition lT_40910 = lS_40874.AddTransition(lS_40876);
            lT_40910.hasExitTime = true;
            lT_40910.hasFixedDuration = true;
            lT_40910.exitTime = 0f;
            lT_40910.duration = 0.25f;
            lT_40910.offset = 0f;
            lT_40910.mute = false;
            lT_40910.solo = false;
            lT_40910.canTransitionToSelf = true;
            lT_40910.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            lT_40910.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 32144f, "L0MotionPhase");

            UnityEditor.Animations.AnimatorStateTransition lT_40912 = lS_40874.AddTransition(lS_40864);
            lT_40912.hasExitTime = false;
            lT_40912.hasFixedDuration = true;
            lT_40912.exitTime = 0f;
            lT_40912.duration = 0.25f;
            lT_40912.offset = 0f;
            lT_40912.mute = false;
            lT_40912.solo = false;
            lT_40912.canTransitionToSelf = true;
            lT_40912.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            lT_40912.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 32143f, "L0MotionPhase");
            lT_40912.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 0f, "L0MotionParameter");

            UnityEditor.Animations.AnimatorStateTransition lT_40914 = lS_40874.AddTransition(lS_40866);
            lT_40914.hasExitTime = false;
            lT_40914.hasFixedDuration = true;
            lT_40914.exitTime = 0f;
            lT_40914.duration = 0.25f;
            lT_40914.offset = 0f;
            lT_40914.mute = false;
            lT_40914.solo = false;
            lT_40914.canTransitionToSelf = true;
            lT_40914.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            lT_40914.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 32143f, "L0MotionPhase");
            lT_40914.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 1f, "L0MotionParameter");

            UnityEditor.Animations.AnimatorStateTransition lT_40916 = lS_40876.AddTransition(lS_40864);
            lT_40916.hasExitTime = true;
            lT_40916.hasFixedDuration = true;
            lT_40916.exitTime = 0.88f;
            lT_40916.duration = 0.25f;
            lT_40916.offset = 0f;
            lT_40916.mute = false;
            lT_40916.solo = false;
            lT_40916.canTransitionToSelf = true;
            lT_40916.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            lT_40916.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 0f, "L0MotionParameter");

            UnityEditor.Animations.AnimatorStateTransition lT_40918 = lS_40876.AddTransition(lS_40866);
            lT_40918.hasExitTime = true;
            lT_40918.hasFixedDuration = true;
            lT_40918.exitTime = 0.88f;
            lT_40918.duration = 0.25f;
            lT_40918.offset = 0f;
            lT_40918.mute = false;
            lT_40918.solo = false;
            lT_40918.canTransitionToSelf = true;
            lT_40918.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            lT_40918.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 1f, "L0MotionParameter");

            UnityEditor.Animations.AnimatorStateTransition lT_N29472 = lS_N29072.AddTransition(lS_40864);
            lT_N29472.hasExitTime = true;
            lT_N29472.hasFixedDuration = true;
            lT_N29472.exitTime = 0.6393927f;
            lT_N29472.duration = 0.25f;
            lT_N29472.offset = 0f;
            lT_N29472.mute = false;
            lT_N29472.solo = false;
            lT_N29472.canTransitionToSelf = true;
            lT_N29472.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            lT_N29472.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 0f, "L0MotionParameter");

            UnityEditor.Animations.AnimatorStateTransition lT_N36290 = lS_N29072.AddTransition(lS_40866);
            lT_N36290.hasExitTime = true;
            lT_N36290.hasFixedDuration = true;
            lT_N36290.exitTime = 0.5381515f;
            lT_N36290.duration = 0.2499999f;
            lT_N36290.offset = 0f;
            lT_N36290.mute = false;
            lT_N36290.solo = false;
            lT_N36290.canTransitionToSelf = true;
            lT_N36290.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            lT_N36290.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 1f, "L0MotionParameter");

            UnityEditor.Animations.AnimatorStateTransition lT_N455404 = lS_N454678.AddTransition(lS_40864);
            lT_N455404.hasExitTime = true;
            lT_N455404.hasFixedDuration = true;
            lT_N455404.exitTime = 0.8088763f;
            lT_N455404.duration = 0.2499999f;
            lT_N455404.offset = 0f;
            lT_N455404.mute = false;
            lT_N455404.solo = false;
            lT_N455404.canTransitionToSelf = true;
            lT_N455404.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            lT_N455404.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 0f, "L0MotionParameter");

            UnityEditor.Animations.AnimatorStateTransition lT_N455488 = lS_N454678.AddTransition(lS_40866);
            lT_N455488.hasExitTime = true;
            lT_N455488.hasFixedDuration = true;
            lT_N455488.exitTime = 0.6674351f;
            lT_N455488.duration = 0.2499999f;
            lT_N455488.offset = 0f;
            lT_N455488.mute = false;
            lT_N455488.solo = false;
            lT_N455488.canTransitionToSelf = true;
            lT_N455488.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            lT_N455488.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 1f, "L0MotionParameter");

            UnityEditor.Animations.AnimatorStateTransition lT_N504312 = lS_N498894.AddTransition(lS_N499114);
            lT_N504312.hasExitTime = true;
            lT_N504312.hasFixedDuration = true;
            lT_N504312.exitTime = 0.2105264f;
            lT_N504312.duration = 0.25f;
            lT_N504312.offset = 0f;
            lT_N504312.mute = false;
            lT_N504312.solo = false;
            lT_N504312.canTransitionToSelf = true;
            lT_N504312.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;

            UnityEditor.Animations.AnimatorStateTransition lT_N504412 = lS_N499114.AddTransition(lS_N499298);
            lT_N504412.hasExitTime = true;
            lT_N504412.hasFixedDuration = true;
            lT_N504412.exitTime = 0f;
            lT_N504412.duration = 0.25f;
            lT_N504412.offset = 0f;
            lT_N504412.mute = false;
            lT_N504412.solo = false;
            lT_N504412.canTransitionToSelf = true;
            lT_N504412.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            lT_N504412.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 32144f, "L0MotionPhase");

            UnityEditor.Animations.AnimatorStateTransition lT_N1293130 = lS_N499114.AddTransition(lS_40864);
            lT_N1293130.hasExitTime = false;
            lT_N1293130.hasFixedDuration = true;
            lT_N1293130.exitTime = 0f;
            lT_N1293130.duration = 0.25f;
            lT_N1293130.offset = 0f;
            lT_N1293130.mute = false;
            lT_N1293130.solo = false;
            lT_N1293130.canTransitionToSelf = true;
            lT_N1293130.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            lT_N1293130.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 32143f, "L0MotionPhase");
            lT_N1293130.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 0f, "L0MotionParameter");

            UnityEditor.Animations.AnimatorStateTransition lT_N1293362 = lS_N499114.AddTransition(lS_40866);
            lT_N1293362.hasExitTime = false;
            lT_N1293362.hasFixedDuration = true;
            lT_N1293362.exitTime = 0f;
            lT_N1293362.duration = 0.25f;
            lT_N1293362.offset = 0f;
            lT_N1293362.mute = false;
            lT_N1293362.solo = false;
            lT_N1293362.canTransitionToSelf = true;
            lT_N1293362.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            lT_N1293362.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 32143f, "L0MotionPhase");
            lT_N1293362.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 1f, "L0MotionParameter");

            UnityEditor.Animations.AnimatorStateTransition lT_N504512 = lS_N499298.AddTransition(lS_40864);
            lT_N504512.hasExitTime = true;
            lT_N504512.hasFixedDuration = true;
            lT_N504512.exitTime = 0.8717949f;
            lT_N504512.duration = 0.25f;
            lT_N504512.offset = 0f;
            lT_N504512.mute = false;
            lT_N504512.solo = false;
            lT_N504512.canTransitionToSelf = true;
            lT_N504512.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            lT_N504512.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 0f, "L0MotionParameter");

            UnityEditor.Animations.AnimatorStateTransition lT_N504608 = lS_N499298.AddTransition(lS_40866);
            lT_N504608.hasExitTime = true;
            lT_N504608.hasFixedDuration = true;
            lT_N504608.exitTime = 0.8717949f;
            lT_N504608.duration = 0.25f;
            lT_N504608.offset = 0f;
            lT_N504608.mute = false;
            lT_N504608.solo = false;
            lT_N504608.canTransitionToSelf = true;
            lT_N504608.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            lT_N504608.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 1f, "L0MotionParameter");

            UnityEditor.Animations.AnimatorStateTransition lT_N576358 = lS_N564636.AddTransition(lS_40864);
            lT_N576358.hasExitTime = true;
            lT_N576358.hasFixedDuration = true;
            lT_N576358.exitTime = 0.9152542f;
            lT_N576358.duration = 0.25f;
            lT_N576358.offset = 0f;
            lT_N576358.mute = false;
            lT_N576358.solo = false;
            lT_N576358.canTransitionToSelf = true;
            lT_N576358.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            lT_N576358.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 0f, "L0MotionParameter");

            UnityEditor.Animations.AnimatorStateTransition lT_N576458 = lS_N564636.AddTransition(lS_40866);
            lT_N576458.hasExitTime = true;
            lT_N576458.hasFixedDuration = true;
            lT_N576458.exitTime = 0.6196442f;
            lT_N576458.duration = 0.25f;
            lT_N576458.offset = 0f;
            lT_N576458.mute = false;
            lT_N576458.solo = false;
            lT_N576458.canTransitionToSelf = true;
            lT_N576458.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            lT_N576458.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 1f, "L0MotionParameter");

            UnityEditor.Animations.AnimatorStateTransition lT_N1031590 = lS_N1020548.AddTransition(lS_N1020718);
            lT_N1031590.hasExitTime = true;
            lT_N1031590.hasFixedDuration = true;
            lT_N1031590.exitTime = 0.9152542f;
            lT_N1031590.duration = 0.25f;
            lT_N1031590.offset = 0f;
            lT_N1031590.mute = false;
            lT_N1031590.solo = false;
            lT_N1031590.canTransitionToSelf = true;
            lT_N1031590.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;

            UnityEditor.Animations.AnimatorStateTransition lT_N1123278 = lS_N1020718.AddTransition(lS_N1021296);
            lT_N1123278.hasExitTime = true;
            lT_N1123278.hasFixedDuration = true;
            lT_N1123278.exitTime = 0.9152542f;
            lT_N1123278.duration = 0.25f;
            lT_N1123278.offset = 0f;
            lT_N1123278.mute = false;
            lT_N1123278.solo = false;
            lT_N1123278.canTransitionToSelf = true;
            lT_N1123278.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            lT_N1123278.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 32144f, "L0MotionPhase");

            UnityEditor.Animations.AnimatorStateTransition lT_N1162202 = lS_N1020718.AddTransition(lS_40864);
            lT_N1162202.hasExitTime = false;
            lT_N1162202.hasFixedDuration = true;
            lT_N1162202.exitTime = 0.9152542f;
            lT_N1162202.duration = 0.25f;
            lT_N1162202.offset = 0f;
            lT_N1162202.mute = false;
            lT_N1162202.solo = false;
            lT_N1162202.canTransitionToSelf = true;
            lT_N1162202.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            lT_N1162202.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 32143f, "L0MotionPhase");
            lT_N1162202.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 0f, "L0MotionParameter");

            UnityEditor.Animations.AnimatorStateTransition lT_N1162434 = lS_N1020718.AddTransition(lS_40866);
            lT_N1162434.hasExitTime = false;
            lT_N1162434.hasFixedDuration = true;
            lT_N1162434.exitTime = 0.9152542f;
            lT_N1162434.duration = 0.25f;
            lT_N1162434.offset = 0f;
            lT_N1162434.mute = false;
            lT_N1162434.solo = false;
            lT_N1162434.canTransitionToSelf = true;
            lT_N1162434.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            lT_N1162434.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 32143f, "L0MotionPhase");
            lT_N1162434.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 1f, "L0MotionParameter");

            UnityEditor.Animations.AnimatorStateTransition lT_N1086710 = lS_N1021296.AddTransition(lS_40864);
            lT_N1086710.hasExitTime = true;
            lT_N1086710.hasFixedDuration = true;
            lT_N1086710.exitTime = 0.9152542f;
            lT_N1086710.duration = 0.25f;
            lT_N1086710.offset = 0f;
            lT_N1086710.mute = false;
            lT_N1086710.solo = false;
            lT_N1086710.canTransitionToSelf = true;
            lT_N1086710.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            lT_N1086710.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 0f, "L0MotionParameter");

            UnityEditor.Animations.AnimatorStateTransition lT_N1090318 = lS_N1021296.AddTransition(lS_40866);
            lT_N1090318.hasExitTime = true;
            lT_N1090318.hasFixedDuration = true;
            lT_N1090318.exitTime = 0.9152542f;
            lT_N1090318.duration = 0.25f;
            lT_N1090318.offset = 0f;
            lT_N1090318.mute = false;
            lT_N1090318.solo = false;
            lT_N1090318.canTransitionToSelf = true;
            lT_N1090318.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            lT_N1090318.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 1f, "L0MotionParameter");

            UnityEditor.Animations.AnimatorStateTransition lT_N1364624 = lS_N1021434.AddTransition(lS_40864);
            lT_N1364624.hasExitTime = true;
            lT_N1364624.hasFixedDuration = true;
            lT_N1364624.exitTime = 0.9230769f;
            lT_N1364624.duration = 0.25f;
            lT_N1364624.offset = 0f;
            lT_N1364624.mute = false;
            lT_N1364624.solo = false;
            lT_N1364624.canTransitionToSelf = true;
            lT_N1364624.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            lT_N1364624.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 0f, "L0MotionParameter");

            UnityEditor.Animations.AnimatorStateTransition lT_N1364856 = lS_N1021434.AddTransition(lS_40866);
            lT_N1364856.hasExitTime = true;
            lT_N1364856.hasFixedDuration = true;
            lT_N1364856.exitTime = 0.9230769f;
            lT_N1364856.duration = 0.25f;
            lT_N1364856.offset = 0f;
            lT_N1364856.mute = false;
            lT_N1364856.solo = false;
            lT_N1364856.canTransitionToSelf = true;
            lT_N1364856.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            lT_N1364856.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 1f, "L0MotionParameter");

            UnityEditor.Animations.AnimatorStateTransition lT_N1031352 = lS_N1021608.AddTransition(lS_N1021800);
            lT_N1031352.hasExitTime = true;
            lT_N1031352.hasFixedDuration = true;
            lT_N1031352.exitTime = 0.9230769f;
            lT_N1031352.duration = 0.25f;
            lT_N1031352.offset = 0f;
            lT_N1031352.mute = false;
            lT_N1031352.solo = false;
            lT_N1031352.canTransitionToSelf = true;
            lT_N1031352.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;

            UnityEditor.Animations.AnimatorStateTransition lT_N1127746 = lS_N1021800.AddTransition(lS_N1022012);
            lT_N1127746.hasExitTime = true;
            lT_N1127746.hasFixedDuration = true;
            lT_N1127746.exitTime = 0.9230769f;
            lT_N1127746.duration = 0.25f;
            lT_N1127746.offset = 0f;
            lT_N1127746.mute = false;
            lT_N1127746.solo = false;
            lT_N1127746.canTransitionToSelf = true;
            lT_N1127746.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            lT_N1127746.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 32144f, "L0MotionPhase");

            UnityEditor.Animations.AnimatorStateTransition lT_N1191718 = lS_N1021800.AddTransition(lS_40864);
            lT_N1191718.hasExitTime = false;
            lT_N1191718.hasFixedDuration = true;
            lT_N1191718.exitTime = 0.9230769f;
            lT_N1191718.duration = 0.25f;
            lT_N1191718.offset = 0f;
            lT_N1191718.mute = false;
            lT_N1191718.solo = false;
            lT_N1191718.canTransitionToSelf = true;
            lT_N1191718.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            lT_N1191718.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 32143f, "L0MotionPhase");
            lT_N1191718.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 0f, "L0MotionParameter");

            UnityEditor.Animations.AnimatorStateTransition lT_N1191950 = lS_N1021800.AddTransition(lS_40866);
            lT_N1191950.hasExitTime = false;
            lT_N1191950.hasFixedDuration = true;
            lT_N1191950.exitTime = 0.9230769f;
            lT_N1191950.duration = 0.25f;
            lT_N1191950.offset = 0f;
            lT_N1191950.mute = false;
            lT_N1191950.solo = false;
            lT_N1191950.canTransitionToSelf = true;
            lT_N1191950.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            lT_N1191950.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 32143f, "L0MotionPhase");
            lT_N1191950.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 1f, "L0MotionParameter");

            UnityEditor.Animations.AnimatorStateTransition lT_N1078646 = lS_N1022012.AddTransition(lS_40864);
            lT_N1078646.hasExitTime = true;
            lT_N1078646.hasFixedDuration = true;
            lT_N1078646.exitTime = 0.9230769f;
            lT_N1078646.duration = 0.25f;
            lT_N1078646.offset = 0f;
            lT_N1078646.mute = false;
            lT_N1078646.solo = false;
            lT_N1078646.canTransitionToSelf = true;
            lT_N1078646.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            lT_N1078646.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 0f, "L0MotionParameter");

            UnityEditor.Animations.AnimatorStateTransition lT_N1082254 = lS_N1022012.AddTransition(lS_40866);
            lT_N1082254.hasExitTime = true;
            lT_N1082254.hasFixedDuration = true;
            lT_N1082254.exitTime = 0.9230769f;
            lT_N1082254.duration = 0.25f;
            lT_N1082254.offset = 0f;
            lT_N1082254.mute = false;
            lT_N1082254.solo = false;
            lT_N1082254.canTransitionToSelf = true;
            lT_N1082254.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            lT_N1082254.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 1f, "L0MotionParameter");

            UnityEditor.Animations.AnimatorStateTransition lT_N1365094 = lS_N1022170.AddTransition(lS_40864);
            lT_N1365094.hasExitTime = true;
            lT_N1365094.hasFixedDuration = true;
            lT_N1365094.exitTime = 0.9074074f;
            lT_N1365094.duration = 0.25f;
            lT_N1365094.offset = 0f;
            lT_N1365094.mute = false;
            lT_N1365094.solo = false;
            lT_N1365094.canTransitionToSelf = true;
            lT_N1365094.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            lT_N1365094.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 0f, "L0MotionParameter");

            UnityEditor.Animations.AnimatorStateTransition lT_N1365326 = lS_N1022170.AddTransition(lS_40866);
            lT_N1365326.hasExitTime = true;
            lT_N1365326.hasFixedDuration = true;
            lT_N1365326.exitTime = 0.9074074f;
            lT_N1365326.duration = 0.25f;
            lT_N1365326.offset = 0f;
            lT_N1365326.mute = false;
            lT_N1365326.solo = false;
            lT_N1365326.canTransitionToSelf = true;
            lT_N1365326.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            lT_N1365326.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 1f, "L0MotionParameter");

            UnityEditor.Animations.AnimatorStateTransition lT_N1031114 = lS_N1022354.AddTransition(lS_N1022574);
            lT_N1031114.hasExitTime = true;
            lT_N1031114.hasFixedDuration = true;
            lT_N1031114.exitTime = 0.9074074f;
            lT_N1031114.duration = 0.25f;
            lT_N1031114.offset = 0f;
            lT_N1031114.mute = false;
            lT_N1031114.solo = false;
            lT_N1031114.canTransitionToSelf = true;
            lT_N1031114.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;

            UnityEditor.Animations.AnimatorStateTransition lT_N1132208 = lS_N1022574.AddTransition(lS_N1022762);
            lT_N1132208.hasExitTime = true;
            lT_N1132208.hasFixedDuration = true;
            lT_N1132208.exitTime = 0.9074074f;
            lT_N1132208.duration = 0.25f;
            lT_N1132208.offset = 0f;
            lT_N1132208.mute = false;
            lT_N1132208.solo = false;
            lT_N1132208.canTransitionToSelf = true;
            lT_N1132208.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            lT_N1132208.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 32144f, "L0MotionPhase");

            UnityEditor.Animations.AnimatorStateTransition lT_N1208456 = lS_N1022574.AddTransition(lS_40864);
            lT_N1208456.hasExitTime = false;
            lT_N1208456.hasFixedDuration = true;
            lT_N1208456.exitTime = 0.9074074f;
            lT_N1208456.duration = 0.25f;
            lT_N1208456.offset = 0f;
            lT_N1208456.mute = false;
            lT_N1208456.solo = false;
            lT_N1208456.canTransitionToSelf = true;
            lT_N1208456.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            lT_N1208456.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 32143f, "L0MotionPhase");
            lT_N1208456.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 0f, "L0MotionParameter");

            UnityEditor.Animations.AnimatorStateTransition lT_N1208688 = lS_N1022574.AddTransition(lS_40866);
            lT_N1208688.hasExitTime = false;
            lT_N1208688.hasFixedDuration = true;
            lT_N1208688.exitTime = 0.9074074f;
            lT_N1208688.duration = 0.25f;
            lT_N1208688.offset = 0f;
            lT_N1208688.mute = false;
            lT_N1208688.solo = false;
            lT_N1208688.canTransitionToSelf = true;
            lT_N1208688.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            lT_N1208688.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 32143f, "L0MotionPhase");
            lT_N1208688.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 1f, "L0MotionParameter");

            UnityEditor.Animations.AnimatorStateTransition lT_N1069740 = lS_N1022762.AddTransition(lS_40864);
            lT_N1069740.hasExitTime = true;
            lT_N1069740.hasFixedDuration = true;
            lT_N1069740.exitTime = 0.9074074f;
            lT_N1069740.duration = 0.25f;
            lT_N1069740.offset = 0f;
            lT_N1069740.mute = false;
            lT_N1069740.solo = false;
            lT_N1069740.canTransitionToSelf = true;
            lT_N1069740.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            lT_N1069740.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 0f, "L0MotionParameter");

            UnityEditor.Animations.AnimatorStateTransition lT_N1073348 = lS_N1022762.AddTransition(lS_40866);
            lT_N1073348.hasExitTime = true;
            lT_N1073348.hasFixedDuration = true;
            lT_N1073348.exitTime = 0.9074074f;
            lT_N1073348.duration = 0.25f;
            lT_N1073348.offset = 0f;
            lT_N1073348.mute = false;
            lT_N1073348.solo = false;
            lT_N1073348.canTransitionToSelf = true;
            lT_N1073348.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            lT_N1073348.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 1f, "L0MotionParameter");

            UnityEditor.Animations.AnimatorStateTransition lT_N1365574 = lS_N1022932.AddTransition(lS_40864);
            lT_N1365574.hasExitTime = true;
            lT_N1365574.hasFixedDuration = true;
            lT_N1365574.exitTime = 0.9050633f;
            lT_N1365574.duration = 0.25f;
            lT_N1365574.offset = 0f;
            lT_N1365574.mute = false;
            lT_N1365574.solo = false;
            lT_N1365574.canTransitionToSelf = true;
            lT_N1365574.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            lT_N1365574.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 0f, "L0MotionParameter");

            UnityEditor.Animations.AnimatorStateTransition lT_N1365806 = lS_N1022932.AddTransition(lS_40866);
            lT_N1365806.hasExitTime = true;
            lT_N1365806.hasFixedDuration = true;
            lT_N1365806.exitTime = 0.9050633f;
            lT_N1365806.duration = 0.25f;
            lT_N1365806.offset = 0f;
            lT_N1365806.mute = false;
            lT_N1365806.solo = false;
            lT_N1365806.canTransitionToSelf = true;
            lT_N1365806.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            lT_N1365806.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 1f, "L0MotionParameter");

            UnityEditor.Animations.AnimatorStateTransition lT_N1030876 = lS_N1023122.AddTransition(lS_N1023314);
            lT_N1030876.hasExitTime = true;
            lT_N1030876.hasFixedDuration = true;
            lT_N1030876.exitTime = 0.9050633f;
            lT_N1030876.duration = 0.25f;
            lT_N1030876.offset = 0f;
            lT_N1030876.mute = false;
            lT_N1030876.solo = false;
            lT_N1030876.canTransitionToSelf = true;
            lT_N1030876.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;

            UnityEditor.Animations.AnimatorStateTransition lT_N1136680 = lS_N1023314.AddTransition(lS_N1023542);
            lT_N1136680.hasExitTime = true;
            lT_N1136680.hasFixedDuration = true;
            lT_N1136680.exitTime = 0.9050633f;
            lT_N1136680.duration = 0.25f;
            lT_N1136680.offset = 0f;
            lT_N1136680.mute = false;
            lT_N1136680.solo = false;
            lT_N1136680.canTransitionToSelf = true;
            lT_N1136680.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            lT_N1136680.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 32144f, "L0MotionPhase");

            UnityEditor.Animations.AnimatorStateTransition lT_N1225196 = lS_N1023314.AddTransition(lS_40864);
            lT_N1225196.hasExitTime = false;
            lT_N1225196.hasFixedDuration = true;
            lT_N1225196.exitTime = 0.9050633f;
            lT_N1225196.duration = 0.25f;
            lT_N1225196.offset = 0f;
            lT_N1225196.mute = false;
            lT_N1225196.solo = false;
            lT_N1225196.canTransitionToSelf = true;
            lT_N1225196.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            lT_N1225196.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 32143f, "L0MotionPhase");
            lT_N1225196.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 0f, "L0MotionParameter");

            UnityEditor.Animations.AnimatorStateTransition lT_N1225430 = lS_N1023314.AddTransition(lS_40866);
            lT_N1225430.hasExitTime = false;
            lT_N1225430.hasFixedDuration = true;
            lT_N1225430.exitTime = 0.9050633f;
            lT_N1225430.duration = 0.25f;
            lT_N1225430.offset = 0f;
            lT_N1225430.mute = false;
            lT_N1225430.solo = false;
            lT_N1225430.canTransitionToSelf = true;
            lT_N1225430.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            lT_N1225430.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 32143f, "L0MotionPhase");
            lT_N1225430.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 1f, "L0MotionParameter");

            UnityEditor.Animations.AnimatorStateTransition lT_N1061668 = lS_N1023542.AddTransition(lS_40864);
            lT_N1061668.hasExitTime = true;
            lT_N1061668.hasFixedDuration = true;
            lT_N1061668.exitTime = 0.9050633f;
            lT_N1061668.duration = 0.25f;
            lT_N1061668.offset = 0f;
            lT_N1061668.mute = false;
            lT_N1061668.solo = false;
            lT_N1061668.canTransitionToSelf = true;
            lT_N1061668.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            lT_N1061668.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 0f, "L0MotionParameter");

            UnityEditor.Animations.AnimatorStateTransition lT_N1065290 = lS_N1023542.AddTransition(lS_40866);
            lT_N1065290.hasExitTime = true;
            lT_N1065290.hasFixedDuration = true;
            lT_N1065290.exitTime = 0.9050633f;
            lT_N1065290.duration = 0.25f;
            lT_N1065290.offset = 0f;
            lT_N1065290.mute = false;
            lT_N1065290.solo = false;
            lT_N1065290.canTransitionToSelf = true;
            lT_N1065290.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            lT_N1065290.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 1f, "L0MotionParameter");

            UnityEditor.Animations.AnimatorStateTransition lT_N1366058 = lS_N1023716.AddTransition(lS_40864);
            lT_N1366058.hasExitTime = true;
            lT_N1366058.hasFixedDuration = true;
            lT_N1366058.exitTime = 0.9418604f;
            lT_N1366058.duration = 0.25f;
            lT_N1366058.offset = 0f;
            lT_N1366058.mute = false;
            lT_N1366058.solo = false;
            lT_N1366058.canTransitionToSelf = true;
            lT_N1366058.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            lT_N1366058.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 0f, "L0MotionParameter");

            UnityEditor.Animations.AnimatorStateTransition lT_N1366290 = lS_N1023716.AddTransition(lS_40866);
            lT_N1366290.hasExitTime = true;
            lT_N1366290.hasFixedDuration = true;
            lT_N1366290.exitTime = 0.9418604f;
            lT_N1366290.duration = 0.25f;
            lT_N1366290.offset = 0f;
            lT_N1366290.mute = false;
            lT_N1366290.solo = false;
            lT_N1366290.canTransitionToSelf = true;
            lT_N1366290.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            lT_N1366290.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 1f, "L0MotionParameter");

            UnityEditor.Animations.AnimatorStateTransition lT_N1155940 = lS_N1023916.AddTransition(lS_N1024118);
            lT_N1155940.hasExitTime = true;
            lT_N1155940.hasFixedDuration = true;
            lT_N1155940.exitTime = 0.9418604f;
            lT_N1155940.duration = 0.25f;
            lT_N1155940.offset = 0f;
            lT_N1155940.mute = false;
            lT_N1155940.solo = false;
            lT_N1155940.canTransitionToSelf = true;
            lT_N1155940.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;

            UnityEditor.Animations.AnimatorStateTransition lT_N1141142 = lS_N1024118.AddTransition(lS_N1024356);
            lT_N1141142.hasExitTime = true;
            lT_N1141142.hasFixedDuration = true;
            lT_N1141142.exitTime = 0.9418604f;
            lT_N1141142.duration = 0.25f;
            lT_N1141142.offset = 0f;
            lT_N1141142.mute = false;
            lT_N1141142.solo = false;
            lT_N1141142.canTransitionToSelf = true;
            lT_N1141142.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            lT_N1141142.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 32144f, "L0MotionPhase");

            UnityEditor.Animations.AnimatorStateTransition lT_N1241956 = lS_N1024118.AddTransition(lS_40864);
            lT_N1241956.hasExitTime = false;
            lT_N1241956.hasFixedDuration = true;
            lT_N1241956.exitTime = 0.9418604f;
            lT_N1241956.duration = 0.25f;
            lT_N1241956.offset = 0f;
            lT_N1241956.mute = false;
            lT_N1241956.solo = false;
            lT_N1241956.canTransitionToSelf = true;
            lT_N1241956.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            lT_N1241956.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 32143f, "L0MotionPhase");
            lT_N1241956.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 0f, "L0MotionParameter");

            UnityEditor.Animations.AnimatorStateTransition lT_N1242188 = lS_N1024118.AddTransition(lS_40866);
            lT_N1242188.hasExitTime = false;
            lT_N1242188.hasFixedDuration = true;
            lT_N1242188.exitTime = 0.9418604f;
            lT_N1242188.duration = 0.25f;
            lT_N1242188.offset = 0f;
            lT_N1242188.mute = false;
            lT_N1242188.solo = false;
            lT_N1242188.canTransitionToSelf = true;
            lT_N1242188.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            lT_N1242188.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 32143f, "L0MotionPhase");
            lT_N1242188.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 1f, "L0MotionParameter");

            UnityEditor.Animations.AnimatorStateTransition lT_N1053594 = lS_N1024356.AddTransition(lS_40864);
            lT_N1053594.hasExitTime = true;
            lT_N1053594.hasFixedDuration = true;
            lT_N1053594.exitTime = 0.9418604f;
            lT_N1053594.duration = 0.25f;
            lT_N1053594.offset = 0f;
            lT_N1053594.mute = false;
            lT_N1053594.solo = false;
            lT_N1053594.canTransitionToSelf = true;
            lT_N1053594.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            lT_N1053594.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 0f, "L0MotionParameter");

            UnityEditor.Animations.AnimatorStateTransition lT_N1057212 = lS_N1024356.AddTransition(lS_40866);
            lT_N1057212.hasExitTime = true;
            lT_N1057212.hasFixedDuration = true;
            lT_N1057212.exitTime = 0.9418604f;
            lT_N1057212.duration = 0.25f;
            lT_N1057212.offset = 0f;
            lT_N1057212.mute = false;
            lT_N1057212.solo = false;
            lT_N1057212.canTransitionToSelf = true;
            lT_N1057212.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            lT_N1057212.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 1f, "L0MotionParameter");

            UnityEditor.Animations.AnimatorStateTransition lT_N1366538 = lS_N1024532.AddTransition(lS_40864);
            lT_N1366538.hasExitTime = true;
            lT_N1366538.hasFixedDuration = true;
            lT_N1366538.exitTime = 0.9246231f;
            lT_N1366538.duration = 0.25f;
            lT_N1366538.offset = 0f;
            lT_N1366538.mute = false;
            lT_N1366538.solo = false;
            lT_N1366538.canTransitionToSelf = true;
            lT_N1366538.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            lT_N1366538.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 0f, "L0MotionParameter");

            UnityEditor.Animations.AnimatorStateTransition lT_N1366776 = lS_N1024532.AddTransition(lS_40866);
            lT_N1366776.hasExitTime = true;
            lT_N1366776.hasFixedDuration = true;
            lT_N1366776.exitTime = 0.9246231f;
            lT_N1366776.duration = 0.25f;
            lT_N1366776.offset = 0f;
            lT_N1366776.mute = false;
            lT_N1366776.solo = false;
            lT_N1366776.canTransitionToSelf = true;
            lT_N1366776.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            lT_N1366776.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 1f, "L0MotionParameter");

            UnityEditor.Animations.AnimatorStateTransition lT_N1155702 = lS_N1024740.AddTransition(lS_N1024984);
            lT_N1155702.hasExitTime = true;
            lT_N1155702.hasFixedDuration = true;
            lT_N1155702.exitTime = 0.9246231f;
            lT_N1155702.duration = 0.25f;
            lT_N1155702.offset = 0f;
            lT_N1155702.mute = false;
            lT_N1155702.solo = false;
            lT_N1155702.canTransitionToSelf = true;
            lT_N1155702.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;

            UnityEditor.Animations.AnimatorStateTransition lT_N1141380 = lS_N1024984.AddTransition(lS_N1025196);
            lT_N1141380.hasExitTime = true;
            lT_N1141380.hasFixedDuration = true;
            lT_N1141380.exitTime = 0.9246231f;
            lT_N1141380.duration = 0.25f;
            lT_N1141380.offset = 0f;
            lT_N1141380.mute = false;
            lT_N1141380.solo = false;
            lT_N1141380.canTransitionToSelf = true;
            lT_N1141380.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            lT_N1141380.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 32144f, "L0MotionPhase");

            UnityEditor.Animations.AnimatorStateTransition lT_N1258720 = lS_N1024984.AddTransition(lS_40864);
            lT_N1258720.hasExitTime = false;
            lT_N1258720.hasFixedDuration = true;
            lT_N1258720.exitTime = 0.9246231f;
            lT_N1258720.duration = 0.25f;
            lT_N1258720.offset = 0f;
            lT_N1258720.mute = false;
            lT_N1258720.solo = false;
            lT_N1258720.canTransitionToSelf = true;
            lT_N1258720.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            lT_N1258720.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 32143f, "L0MotionPhase");
            lT_N1258720.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 0f, "L0MotionParameter");

            UnityEditor.Animations.AnimatorStateTransition lT_N1258952 = lS_N1024984.AddTransition(lS_40866);
            lT_N1258952.hasExitTime = false;
            lT_N1258952.hasFixedDuration = true;
            lT_N1258952.exitTime = 0.9246231f;
            lT_N1258952.duration = 0.25f;
            lT_N1258952.offset = 0f;
            lT_N1258952.mute = false;
            lT_N1258952.solo = false;
            lT_N1258952.canTransitionToSelf = true;
            lT_N1258952.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            lT_N1258952.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 32143f, "L0MotionPhase");
            lT_N1258952.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 1f, "L0MotionParameter");

            UnityEditor.Animations.AnimatorStateTransition lT_N1045532 = lS_N1025196.AddTransition(lS_40864);
            lT_N1045532.hasExitTime = true;
            lT_N1045532.hasFixedDuration = true;
            lT_N1045532.exitTime = 0.9246231f;
            lT_N1045532.duration = 0.25f;
            lT_N1045532.offset = 0f;
            lT_N1045532.mute = false;
            lT_N1045532.solo = false;
            lT_N1045532.canTransitionToSelf = true;
            lT_N1045532.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            lT_N1045532.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 0f, "L0MotionParameter");

            UnityEditor.Animations.AnimatorStateTransition lT_N1049140 = lS_N1025196.AddTransition(lS_40866);
            lT_N1049140.hasExitTime = true;
            lT_N1049140.hasFixedDuration = true;
            lT_N1049140.exitTime = 0.9246231f;
            lT_N1049140.duration = 0.25f;
            lT_N1049140.offset = 0f;
            lT_N1049140.mute = false;
            lT_N1049140.solo = false;
            lT_N1049140.canTransitionToSelf = true;
            lT_N1049140.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            lT_N1049140.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 1f, "L0MotionParameter");

            UnityEditor.Animations.AnimatorStateTransition lT_N1367014 = lS_N1025392.AddTransition(lS_40864);
            lT_N1367014.hasExitTime = true;
            lT_N1367014.hasFixedDuration = true;
            lT_N1367014.exitTime = 0.9292453f;
            lT_N1367014.duration = 0.25f;
            lT_N1367014.offset = 0f;
            lT_N1367014.mute = false;
            lT_N1367014.solo = false;
            lT_N1367014.canTransitionToSelf = true;
            lT_N1367014.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            lT_N1367014.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 0f, "L0MotionParameter");

            UnityEditor.Animations.AnimatorStateTransition lT_N1367246 = lS_N1025392.AddTransition(lS_40866);
            lT_N1367246.hasExitTime = true;
            lT_N1367246.hasFixedDuration = true;
            lT_N1367246.exitTime = 0.9292453f;
            lT_N1367246.duration = 0.25f;
            lT_N1367246.offset = 0f;
            lT_N1367246.mute = false;
            lT_N1367246.solo = false;
            lT_N1367246.canTransitionToSelf = true;
            lT_N1367246.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            lT_N1367246.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 1f, "L0MotionParameter");

            UnityEditor.Animations.AnimatorStateTransition lT_N1155464 = lS_N1025642.AddTransition(lS_N1025928);
            lT_N1155464.hasExitTime = true;
            lT_N1155464.hasFixedDuration = true;
            lT_N1155464.exitTime = 0.9292453f;
            lT_N1155464.duration = 0.25f;
            lT_N1155464.offset = 0f;
            lT_N1155464.mute = false;
            lT_N1155464.solo = false;
            lT_N1155464.canTransitionToSelf = true;
            lT_N1155464.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;

            UnityEditor.Animations.AnimatorStateTransition lT_N1141618 = lS_N1025928.AddTransition(lS_N1026150);
            lT_N1141618.hasExitTime = true;
            lT_N1141618.hasFixedDuration = true;
            lT_N1141618.exitTime = 0.9292453f;
            lT_N1141618.duration = 0.25f;
            lT_N1141618.offset = 0f;
            lT_N1141618.mute = false;
            lT_N1141618.solo = false;
            lT_N1141618.canTransitionToSelf = true;
            lT_N1141618.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            lT_N1141618.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 32144f, "L0MotionPhase");

            UnityEditor.Animations.AnimatorStateTransition lT_N1275482 = lS_N1025928.AddTransition(lS_40864);
            lT_N1275482.hasExitTime = false;
            lT_N1275482.hasFixedDuration = true;
            lT_N1275482.exitTime = 0.9292453f;
            lT_N1275482.duration = 0.25f;
            lT_N1275482.offset = 0f;
            lT_N1275482.mute = false;
            lT_N1275482.solo = false;
            lT_N1275482.canTransitionToSelf = true;
            lT_N1275482.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            lT_N1275482.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 32143f, "L0MotionPhase");
            lT_N1275482.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 0f, "L0MotionParameter");

            UnityEditor.Animations.AnimatorStateTransition lT_N1275716 = lS_N1025928.AddTransition(lS_40866);
            lT_N1275716.hasExitTime = false;
            lT_N1275716.hasFixedDuration = true;
            lT_N1275716.exitTime = 0.9292453f;
            lT_N1275716.duration = 0.25f;
            lT_N1275716.offset = 0f;
            lT_N1275716.mute = false;
            lT_N1275716.solo = false;
            lT_N1275716.canTransitionToSelf = true;
            lT_N1275716.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            lT_N1275716.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 32143f, "L0MotionPhase");
            lT_N1275716.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 1f, "L0MotionParameter");

            UnityEditor.Animations.AnimatorStateTransition lT_N1034062 = lS_N1026150.AddTransition(lS_40864);
            lT_N1034062.hasExitTime = true;
            lT_N1034062.hasFixedDuration = true;
            lT_N1034062.exitTime = 0.9292453f;
            lT_N1034062.duration = 0.25f;
            lT_N1034062.offset = 0f;
            lT_N1034062.mute = false;
            lT_N1034062.solo = false;
            lT_N1034062.canTransitionToSelf = true;
            lT_N1034062.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            lT_N1034062.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 0f, "L0MotionParameter");

            UnityEditor.Animations.AnimatorStateTransition lT_N1041076 = lS_N1026150.AddTransition(lS_40866);
            lT_N1041076.hasExitTime = true;
            lT_N1041076.hasFixedDuration = true;
            lT_N1041076.exitTime = 0.9292453f;
            lT_N1041076.duration = 0.25f;
            lT_N1041076.offset = 0f;
            lT_N1041076.mute = false;
            lT_N1041076.solo = false;
            lT_N1041076.canTransitionToSelf = true;
            lT_N1041076.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            lT_N1041076.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 1f, "L0MotionParameter");

            UnityEditor.Animations.AnimatorStateTransition lT_N1345032 = lS_N1026512.AddTransition(lS_40864);
            lT_N1345032.hasExitTime = true;
            lT_N1345032.hasFixedDuration = true;
            lT_N1345032.exitTime = 0.8863636f;
            lT_N1345032.duration = 0.25f;
            lT_N1345032.offset = 0f;
            lT_N1345032.mute = false;
            lT_N1345032.solo = false;
            lT_N1345032.canTransitionToSelf = true;
            lT_N1345032.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            lT_N1345032.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 0f, "L0MotionParameter");

            UnityEditor.Animations.AnimatorStateTransition lT_N1345264 = lS_N1026512.AddTransition(lS_40866);
            lT_N1345264.hasExitTime = true;
            lT_N1345264.hasFixedDuration = true;
            lT_N1345264.exitTime = 0.8863636f;
            lT_N1345264.duration = 0.25f;
            lT_N1345264.offset = 0f;
            lT_N1345264.mute = false;
            lT_N1345264.solo = false;
            lT_N1345264.canTransitionToSelf = true;
            lT_N1345264.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            lT_N1345264.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 1f, "L0MotionParameter");

            UnityEditor.Animations.AnimatorStateTransition lT_N1032064 = lS_N1026734.AddTransition(lS_N1026958);
            lT_N1032064.hasExitTime = true;
            lT_N1032064.hasFixedDuration = true;
            lT_N1032064.exitTime = 0.8863636f;
            lT_N1032064.duration = 0.25f;
            lT_N1032064.offset = 0f;
            lT_N1032064.mute = false;
            lT_N1032064.solo = false;
            lT_N1032064.canTransitionToSelf = true;
            lT_N1032064.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;

            UnityEditor.Animations.AnimatorStateTransition lT_N1113480 = lS_N1026958.AddTransition(lS_N1027218);
            lT_N1113480.hasExitTime = true;
            lT_N1113480.hasFixedDuration = true;
            lT_N1113480.exitTime = 0.8863636f;
            lT_N1113480.duration = 0.25f;
            lT_N1113480.offset = 0f;
            lT_N1113480.mute = false;
            lT_N1113480.solo = false;
            lT_N1113480.canTransitionToSelf = true;
            lT_N1113480.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            lT_N1113480.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 32144f, "L0MotionPhase");

            UnityEditor.Animations.AnimatorStateTransition lT_N1309866 = lS_N1026958.AddTransition(lS_40864);
            lT_N1309866.hasExitTime = false;
            lT_N1309866.hasFixedDuration = true;
            lT_N1309866.exitTime = 0.8863636f;
            lT_N1309866.duration = 0.25f;
            lT_N1309866.offset = 0f;
            lT_N1309866.mute = false;
            lT_N1309866.solo = false;
            lT_N1309866.canTransitionToSelf = true;
            lT_N1309866.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            lT_N1309866.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 32143f, "L0MotionPhase");
            lT_N1309866.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 0f, "L0MotionPhase");

            UnityEditor.Animations.AnimatorStateTransition lT_N1310098 = lS_N1026958.AddTransition(lS_40866);
            lT_N1310098.hasExitTime = false;
            lT_N1310098.hasFixedDuration = true;
            lT_N1310098.exitTime = 0.8863636f;
            lT_N1310098.duration = 0.25f;
            lT_N1310098.offset = 0f;
            lT_N1310098.mute = false;
            lT_N1310098.solo = false;
            lT_N1310098.canTransitionToSelf = true;
            lT_N1310098.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            lT_N1310098.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 32143f, "L0MotionPhase");
            lT_N1310098.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 1f, "L0MotionPhase");

            UnityEditor.Animations.AnimatorStateTransition lT_N1102850 = lS_N1027218.AddTransition(lS_40864);
            lT_N1102850.hasExitTime = true;
            lT_N1102850.hasFixedDuration = true;
            lT_N1102850.exitTime = 0.8863636f;
            lT_N1102850.duration = 0.25f;
            lT_N1102850.offset = 0f;
            lT_N1102850.mute = false;
            lT_N1102850.solo = false;
            lT_N1102850.canTransitionToSelf = true;
            lT_N1102850.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            lT_N1102850.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 0f, "L0MotionParameter");

            UnityEditor.Animations.AnimatorStateTransition lT_N1106470 = lS_N1027218.AddTransition(lS_40866);
            lT_N1106470.hasExitTime = true;
            lT_N1106470.hasFixedDuration = true;
            lT_N1106470.exitTime = 0.8863636f;
            lT_N1106470.duration = 0.25f;
            lT_N1106470.offset = 0f;
            lT_N1106470.mute = false;
            lT_N1106470.solo = false;
            lT_N1106470.canTransitionToSelf = true;
            lT_N1106470.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            lT_N1106470.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 1f, "L0MotionParameter");

            UnityEditor.Animations.AnimatorStateTransition lT_N1353110 = lS_N1028980.AddTransition(lS_40864);
            lT_N1353110.hasExitTime = true;
            lT_N1353110.hasFixedDuration = true;
            lT_N1353110.exitTime = 0.890511f;
            lT_N1353110.duration = 0.25f;
            lT_N1353110.offset = 0f;
            lT_N1353110.mute = false;
            lT_N1353110.solo = false;
            lT_N1353110.canTransitionToSelf = true;
            lT_N1353110.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            lT_N1353110.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 0f, "L0MotionParameter");

            UnityEditor.Animations.AnimatorStateTransition lT_N1353342 = lS_N1028980.AddTransition(lS_40866);
            lT_N1353342.hasExitTime = true;
            lT_N1353342.hasFixedDuration = true;
            lT_N1353342.exitTime = 0.890511f;
            lT_N1353342.duration = 0.25f;
            lT_N1353342.offset = 0f;
            lT_N1353342.mute = false;
            lT_N1353342.solo = false;
            lT_N1353342.canTransitionToSelf = true;
            lT_N1353342.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            lT_N1353342.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 1f, "L0MotionParameter");

            UnityEditor.Animations.AnimatorStateTransition lT_N1031826 = lS_N1029194.AddTransition(lS_N1029464);
            lT_N1031826.hasExitTime = true;
            lT_N1031826.hasFixedDuration = true;
            lT_N1031826.exitTime = 0.890511f;
            lT_N1031826.duration = 0.25f;
            lT_N1031826.offset = 0f;
            lT_N1031826.mute = false;
            lT_N1031826.solo = false;
            lT_N1031826.canTransitionToSelf = true;
            lT_N1031826.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;

            UnityEditor.Animations.AnimatorStateTransition lT_N1117934 = lS_N1029464.AddTransition(lS_N1029702);
            lT_N1117934.hasExitTime = true;
            lT_N1117934.hasFixedDuration = true;
            lT_N1117934.exitTime = 0.890511f;
            lT_N1117934.duration = 0.25f;
            lT_N1117934.offset = 0f;
            lT_N1117934.mute = false;
            lT_N1117934.solo = false;
            lT_N1117934.canTransitionToSelf = true;
            lT_N1117934.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            lT_N1117934.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 32144f, "L0MotionPhase");

            UnityEditor.Animations.AnimatorStateTransition lT_N1326578 = lS_N1029464.AddTransition(lS_40864);
            lT_N1326578.hasExitTime = true;
            lT_N1326578.hasFixedDuration = true;
            lT_N1326578.exitTime = 0.890511f;
            lT_N1326578.duration = 0.25f;
            lT_N1326578.offset = 0f;
            lT_N1326578.mute = false;
            lT_N1326578.solo = false;
            lT_N1326578.canTransitionToSelf = true;
            lT_N1326578.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            lT_N1326578.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 32143f, "L0MotionPhase");
            lT_N1326578.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 0f, "L0MotionParameter");

            UnityEditor.Animations.AnimatorStateTransition lT_N1326810 = lS_N1029464.AddTransition(lS_40866);
            lT_N1326810.hasExitTime = true;
            lT_N1326810.hasFixedDuration = true;
            lT_N1326810.exitTime = 0.890511f;
            lT_N1326810.duration = 0.25f;
            lT_N1326810.offset = 0f;
            lT_N1326810.mute = false;
            lT_N1326810.solo = false;
            lT_N1326810.canTransitionToSelf = true;
            lT_N1326810.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            lT_N1326810.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 32143f, "L0MotionPhase");
            lT_N1326810.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 1f, "L0MotionParameter");

            UnityEditor.Animations.AnimatorStateTransition lT_N1094780 = lS_N1029702.AddTransition(lS_40864);
            lT_N1094780.hasExitTime = true;
            lT_N1094780.hasFixedDuration = true;
            lT_N1094780.exitTime = 0.890511f;
            lT_N1094780.duration = 0.25f;
            lT_N1094780.offset = 0f;
            lT_N1094780.mute = false;
            lT_N1094780.solo = false;
            lT_N1094780.canTransitionToSelf = true;
            lT_N1094780.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            lT_N1094780.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 0f, "L0MotionParameter");

            UnityEditor.Animations.AnimatorStateTransition lT_N1098402 = lS_N1029702.AddTransition(lS_40866);
            lT_N1098402.hasExitTime = true;
            lT_N1098402.hasFixedDuration = true;
            lT_N1098402.exitTime = 0.890511f;
            lT_N1098402.duration = 0.25f;
            lT_N1098402.offset = 0f;
            lT_N1098402.mute = false;
            lT_N1098402.solo = false;
            lT_N1098402.canTransitionToSelf = true;
            lT_N1098402.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            lT_N1098402.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 1f, "L0MotionParameter");

        }

        /// <summary>
        /// Gathers the animations so we can use them when creating the sub-state machine.
        /// </summary>
        public override void FindAnimations()
        {
            m17094 = FindAnimationClip("Assets/ootii/MotionController/Content/Animations/Humanoid/Idling/unity_Idle_IdleToIdlesR.fbx/IdlePose.anim", "IdlePose");
            m20438 = FindAnimationClip("Assets/ootii/MotionControllerPacks/SpellCasting/Content/Animations/Mixamo/Y_Bot@standing_idle.fbx/PMP_IdlePose.anim", "PMP_IdlePose");
            m30130 = FindAnimationClip("Assets/ootii/MotionControllerPacks/SpellCasting/Content/Animations/Mixamo/Y_Bot@Standing_1H_Magic_Attack_01.fbx/Standing_1H_Magic_Attack_01.anim", "Standing_1H_Magic_Attack_01");
            m19590 = FindAnimationClip("Assets/ootii/MotionController/Content/Animations/Humanoid/Idling/IdleToReady.fbx/IdleToReady.anim", "IdleToReady");
            m16546 = FindAnimationClip("Assets/ootii/MotionControllerPacks/SpellCasting/Content/Animations/Mixamo/Y_Bot@Standing_2H_Cast_Spell_01.fbx/2H_Cast_01_a.anim", "2H_Cast_01_a");
            m16550 = FindAnimationClip("Assets/ootii/MotionControllerPacks/SpellCasting/Content/Animations/Mixamo/Y_Bot@Standing_2H_Cast_Spell_01.fbx/2H_Cast_01_c.anim", "2H_Cast_01_c");
            m16548 = FindAnimationClip("Assets/ootii/MotionControllerPacks/SpellCasting/Content/Animations/Mixamo/Y_Bot@Standing_2H_Cast_Spell_01.fbx/2H_Cast_01_b.anim", "2H_Cast_01_b");
            m16544 = FindAnimationClip("Assets/ootii/MotionControllerPacks/SpellCasting/Content/Animations/Mixamo/Y_Bot@Standing_2H_Cast_Spell_01.fbx/2H_Cast_01.anim", "2H_Cast_01");
            m30124 = FindAnimationClip("Assets/ootii/MotionControllerPacks/SpellCasting/Content/Animations/Mixamo/Y_Bot@Standing_1H_Magic_Attack_01.fbx/1H_Cast_01_a.anim", "1H_Cast_01_a");
            m30126 = FindAnimationClip("Assets/ootii/MotionControllerPacks/SpellCasting/Content/Animations/Mixamo/Y_Bot@Standing_1H_Magic_Attack_01.fbx/1H_Cast_01_b.anim", "1H_Cast_01_b");
            m30128 = FindAnimationClip("Assets/ootii/MotionControllerPacks/SpellCasting/Content/Animations/Mixamo/Y_Bot@Standing_1H_Magic_Attack_01.fbx/1H_Cast_01_c.anim", "1H_Cast_01_c");
            m31788 = FindAnimationClip("Assets/ootii/MotionControllerPacks/SpellCasting/Content/Animations/Mixamo/Y_Bot@Standing_React_Small_From_Back.fbx/Standing_React_Small_From_Back.anim", "Standing_React_Small_From_Back");
            m23758 = FindAnimationClip("Assets/ootii/MotionControllerPacks/SpellCasting/Content/Animations/Mixamo/Y_Bot@standing_1H_cast_spell_01.fbx/standing_1H_cast_spell_01.anim", "standing_1H_cast_spell_01");
            m186464 = FindAnimationClip("Assets/ootii/MotionControllerPacks/SpellCasting/Content/Animations/Mixamo/Y_Bot@standing_1H_cast_spell_01.fbx/1H_Cast_02_a.anim", "1H_Cast_02_a");
            m186466 = FindAnimationClip("Assets/ootii/MotionControllerPacks/SpellCasting/Content/Animations/Mixamo/Y_Bot@standing_1H_cast_spell_01.fbx/1H_Cast_02_b.anim", "1H_Cast_02_b");
            m186468 = FindAnimationClip("Assets/ootii/MotionControllerPacks/SpellCasting/Content/Animations/Mixamo/Y_Bot@standing_1H_cast_spell_01.fbx/1H_Cast_02_c.anim", "1H_Cast_02_c");
            m22246 = FindAnimationClip("Assets/ootii/MotionControllerPacks/SpellCasting/Content/Animations/Mixamo/Y_Bot@Standing_2H_Magic_Area_Attack_01.fbx/Standing_2H_Magic_Area_Attack_01.anim", "Standing_2H_Magic_Area_Attack_01");
            m241242 = FindAnimationClip("Assets/ootii/MotionControllerPacks/SpellCasting/Content/Animations/Mixamo/Y_Bot@Standing_2H_Magic_Area_Attack_01.fbx/2H_Cast_02_a.anim", "2H_Cast_02_a");
            m241244 = FindAnimationClip("Assets/ootii/MotionControllerPacks/SpellCasting/Content/Animations/Mixamo/Y_Bot@Standing_2H_Magic_Area_Attack_01.fbx/2H_Cast_02_b.anim", "2H_Cast_02_b");
            m241246 = FindAnimationClip("Assets/ootii/MotionControllerPacks/SpellCasting/Content/Animations/Mixamo/Y_Bot@Standing_2H_Magic_Area_Attack_01.fbx/2H_Cast_02_c.anim", "2H_Cast_02_c");
            m242640 = FindAnimationClip("Assets/ootii/MotionControllerPacks/SpellCasting/Content/Animations/Mixamo/Y_Bot@Standing_2H_Magic_Area_Attack_02.fbx/2H_Cast_03.anim", "2H_Cast_03");
            m242642 = FindAnimationClip("Assets/ootii/MotionControllerPacks/SpellCasting/Content/Animations/Mixamo/Y_Bot@Standing_2H_Magic_Area_Attack_02.fbx/2H_Cast_03_a.anim", "2H_Cast_03_a");
            m242644 = FindAnimationClip("Assets/ootii/MotionControllerPacks/SpellCasting/Content/Animations/Mixamo/Y_Bot@Standing_2H_Magic_Area_Attack_02.fbx/2H_Cast_03_b.anim", "2H_Cast_03_b");
            m242646 = FindAnimationClip("Assets/ootii/MotionControllerPacks/SpellCasting/Content/Animations/Mixamo/Y_Bot@Standing_2H_Magic_Area_Attack_02.fbx/2H_Cast_03_c.anim", "2H_Cast_03_c");
            m242650 = FindAnimationClip("Assets/ootii/MotionControllerPacks/SpellCasting/Content/Animations/Mixamo/Y_Bot@Standing_2H_Magic_Attack_01.fbx/2H_Cast_04.anim", "2H_Cast_04");
            m242652 = FindAnimationClip("Assets/ootii/MotionControllerPacks/SpellCasting/Content/Animations/Mixamo/Y_Bot@Standing_2H_Magic_Attack_01.fbx/2H_Cast_04_a.anim", "2H_Cast_04_a");
            m242654 = FindAnimationClip("Assets/ootii/MotionControllerPacks/SpellCasting/Content/Animations/Mixamo/Y_Bot@Standing_2H_Magic_Attack_01.fbx/2H_Cast_04_b.anim", "2H_Cast_04_b");
            m242656 = FindAnimationClip("Assets/ootii/MotionControllerPacks/SpellCasting/Content/Animations/Mixamo/Y_Bot@Standing_2H_Magic_Attack_01.fbx/2H_Cast_04_c.anim", "2H_Cast_04_c");
            m242660 = FindAnimationClip("Assets/ootii/MotionControllerPacks/SpellCasting/Content/Animations/Mixamo/Y_Bot@Standing_2H_Magic_Attack_02.fbx/2H_Cast_05.anim", "2H_Cast_05");
            m242662 = FindAnimationClip("Assets/ootii/MotionControllerPacks/SpellCasting/Content/Animations/Mixamo/Y_Bot@Standing_2H_Magic_Attack_02.fbx/2H_Cast_05_a.anim", "2H_Cast_05_a");
            m242664 = FindAnimationClip("Assets/ootii/MotionControllerPacks/SpellCasting/Content/Animations/Mixamo/Y_Bot@Standing_2H_Magic_Attack_02.fbx/2H_Cast_05_b.anim", "2H_Cast_05_b");
            m242666 = FindAnimationClip("Assets/ootii/MotionControllerPacks/SpellCasting/Content/Animations/Mixamo/Y_Bot@Standing_2H_Magic_Attack_02.fbx/2H_Cast_05_c.anim", "2H_Cast_05_c");
            m242670 = FindAnimationClip("Assets/ootii/MotionControllerPacks/SpellCasting/Content/Animations/Mixamo/Y_Bot@Standing_2H_Magic_Attack_03.fbx/2H_Cast_06.anim", "2H_Cast_06");
            m242672 = FindAnimationClip("Assets/ootii/MotionControllerPacks/SpellCasting/Content/Animations/Mixamo/Y_Bot@Standing_2H_Magic_Attack_03.fbx/2H_Cast_06_a.anim", "2H_Cast_06_a");
            m242674 = FindAnimationClip("Assets/ootii/MotionControllerPacks/SpellCasting/Content/Animations/Mixamo/Y_Bot@Standing_2H_Magic_Attack_03.fbx/2H_Cast_06_b.anim", "2H_Cast_06_b");
            m242676 = FindAnimationClip("Assets/ootii/MotionControllerPacks/SpellCasting/Content/Animations/Mixamo/Y_Bot@Standing_2H_Magic_Attack_03.fbx/2H_Cast_06_c.anim", "2H_Cast_06_c");
            m242680 = FindAnimationClip("Assets/ootii/MotionControllerPacks/SpellCasting/Content/Animations/Mixamo/Y_Bot@Standing_2H_Magic_Attack_04.fbx/2H_Cast_07.anim", "2H_Cast_07");
            m242682 = FindAnimationClip("Assets/ootii/MotionControllerPacks/SpellCasting/Content/Animations/Mixamo/Y_Bot@Standing_2H_Magic_Attack_04.fbx/2H_Cast_07_a.anim", "2H_Cast_07_a");
            m242684 = FindAnimationClip("Assets/ootii/MotionControllerPacks/SpellCasting/Content/Animations/Mixamo/Y_Bot@Standing_2H_Magic_Attack_04.fbx/2H_Cast_07_b.anim", "2H_Cast_07_b");
            m15810 = FindAnimationClip("Assets/ootii/MotionControllerPacks/SpellCasting/Content/Animations/Mixamo/Y_Bot@Standing_2H_Magic_Attack_04.fbx/2H_Cast_07_c.anim", "2H_Cast_07_c");
            m242690 = FindAnimationClip("Assets/ootii/MotionControllerPacks/SpellCasting/Content/Animations/Mixamo/Y_Bot@Standing_2H_Magic_Attack_05.fbx/2H_Cast_08.anim", "2H_Cast_08");
            m242692 = FindAnimationClip("Assets/ootii/MotionControllerPacks/SpellCasting/Content/Animations/Mixamo/Y_Bot@Standing_2H_Magic_Attack_05.fbx/2H_Cast_08_a.anim", "2H_Cast_08_a");
            m242694 = FindAnimationClip("Assets/ootii/MotionControllerPacks/SpellCasting/Content/Animations/Mixamo/Y_Bot@Standing_2H_Magic_Attack_05.fbx/2H_Cast_08_b.anim", "2H_Cast_08_b");
            m242696 = FindAnimationClip("Assets/ootii/MotionControllerPacks/SpellCasting/Content/Animations/Mixamo/Y_Bot@Standing_2H_Magic_Attack_05.fbx/2H_Cast_08_c.anim", "2H_Cast_08_c");
            m242620 = FindAnimationClip("Assets/ootii/MotionControllerPacks/SpellCasting/Content/Animations/Mixamo/Y_Bot@Standing_1H_Magic_Attack_02.fbx/1H_Cast_03.anim", "1H_Cast_03");
            m242622 = FindAnimationClip("Assets/ootii/MotionControllerPacks/SpellCasting/Content/Animations/Mixamo/Y_Bot@Standing_1H_Magic_Attack_02.fbx/1H_Cast_03_a.anim", "1H_Cast_03_a");
            m242624 = FindAnimationClip("Assets/ootii/MotionControllerPacks/SpellCasting/Content/Animations/Mixamo/Y_Bot@Standing_1H_Magic_Attack_02.fbx/1H_Cast_03_b.anim", "1H_Cast_03_b");
            m242626 = FindAnimationClip("Assets/ootii/MotionControllerPacks/SpellCasting/Content/Animations/Mixamo/Y_Bot@Standing_1H_Magic_Attack_02.fbx/1H_Cast_03_c.anim", "1H_Cast_03_c");
            m242630 = FindAnimationClip("Assets/ootii/MotionControllerPacks/SpellCasting/Content/Animations/Mixamo/Y_Bot@Standing_1H_Magic_Attack_03.fbx/1H_Cast_04.anim", "1H_Cast_04");
            m242632 = FindAnimationClip("Assets/ootii/MotionControllerPacks/SpellCasting/Content/Animations/Mixamo/Y_Bot@Standing_1H_Magic_Attack_03.fbx/1H_Cast_04_a.anim", "1H_Cast_04_a");
            m242634 = FindAnimationClip("Assets/ootii/MotionControllerPacks/SpellCasting/Content/Animations/Mixamo/Y_Bot@Standing_1H_Magic_Attack_03.fbx/1H_Cast_04_b.anim", "1H_Cast_04_b");
            m242636 = FindAnimationClip("Assets/ootii/MotionControllerPacks/SpellCasting/Content/Animations/Mixamo/Y_Bot@Standing_1H_Magic_Attack_03.fbx/1H_Cast_04_c.anim", "1H_Cast_04_c");

            // Add the remaining functionality
            base.FindAnimations();
        }

        /// <summary>
        /// Used to show the settings that allow us to generate the animator setup.
        /// </summary>
        public override void OnSettingsGUI()
        {
            UnityEditor.EditorGUILayout.IntField(new GUIContent("Phase ID", "Phase ID used to transition to the state."), PHASE_START);
            m17094 = CreateAnimationField("Start.IdlePose", "Assets/ootii/MotionController/Content/Animations/Humanoid/Idling/unity_Idle_IdleToIdlesR.fbx/IdlePose.anim", "IdlePose", m17094);
            m20438 = CreateAnimationField("Spell Idle Out.PMP_IdlePose", "Assets/ootii/MotionControllerPacks/SpellCasting/Content/Animations/Mixamo/Y_Bot@standing_idle.fbx/PMP_IdlePose.anim", "PMP_IdlePose", m20438);
            m30130 = CreateAnimationField("1H_Cast_01.Standing_1H_Magic_Attack_01", "Assets/ootii/MotionControllerPacks/SpellCasting/Content/Animations/Mixamo/Y_Bot@Standing_1H_Magic_Attack_01.fbx/Standing_1H_Magic_Attack_01.anim", "Standing_1H_Magic_Attack_01", m30130);
            m19590 = CreateAnimationField("Stand Idle In.IdleToReady", "Assets/ootii/MotionController/Content/Animations/Humanoid/Idling/IdleToReady.fbx/IdleToReady.anim", "IdleToReady", m19590);
            m16546 = CreateAnimationField("2H_Cast_01_a", "Assets/ootii/MotionControllerPacks/SpellCasting/Content/Animations/Mixamo/Y_Bot@Standing_2H_Cast_Spell_01.fbx/2H_Cast_01_a.anim", "2H_Cast_01_a", m16546);
            m16550 = CreateAnimationField("2H_Cast_01_c", "Assets/ootii/MotionControllerPacks/SpellCasting/Content/Animations/Mixamo/Y_Bot@Standing_2H_Cast_Spell_01.fbx/2H_Cast_01_c.anim", "2H_Cast_01_c", m16550);
            m16548 = CreateAnimationField("2H_Cast_01_b", "Assets/ootii/MotionControllerPacks/SpellCasting/Content/Animations/Mixamo/Y_Bot@Standing_2H_Cast_Spell_01.fbx/2H_Cast_01_b.anim", "2H_Cast_01_b", m16548);
            m16544 = CreateAnimationField("2H_Cast_01", "Assets/ootii/MotionControllerPacks/SpellCasting/Content/Animations/Mixamo/Y_Bot@Standing_2H_Cast_Spell_01.fbx/2H_Cast_01.anim", "2H_Cast_01", m16544);
            m30124 = CreateAnimationField("1H_Cast_01_a", "Assets/ootii/MotionControllerPacks/SpellCasting/Content/Animations/Mixamo/Y_Bot@Standing_1H_Magic_Attack_01.fbx/1H_Cast_01_a.anim", "1H_Cast_01_a", m30124);
            m30126 = CreateAnimationField("1H_Cast_01_b", "Assets/ootii/MotionControllerPacks/SpellCasting/Content/Animations/Mixamo/Y_Bot@Standing_1H_Magic_Attack_01.fbx/1H_Cast_01_b.anim", "1H_Cast_01_b", m30126);
            m30128 = CreateAnimationField("1H_Cast_01_c", "Assets/ootii/MotionControllerPacks/SpellCasting/Content/Animations/Mixamo/Y_Bot@Standing_1H_Magic_Attack_01.fbx/1H_Cast_01_c.anim", "1H_Cast_01_c", m30128);
            m31788 = CreateAnimationField("Interrupted.Standing_React_Small_From_Back", "Assets/ootii/MotionControllerPacks/SpellCasting/Content/Animations/Mixamo/Y_Bot@Standing_React_Small_From_Back.fbx/Standing_React_Small_From_Back.anim", "Standing_React_Small_From_Back", m31788);
            m23758 = CreateAnimationField("1H_Cast_02.standing_1H_cast_spell_01", "Assets/ootii/MotionControllerPacks/SpellCasting/Content/Animations/Mixamo/Y_Bot@standing_1H_cast_spell_01.fbx/standing_1H_cast_spell_01.anim", "standing_1H_cast_spell_01", m23758);
            m186464 = CreateAnimationField("1H_Cast_02_a", "Assets/ootii/MotionControllerPacks/SpellCasting/Content/Animations/Mixamo/Y_Bot@standing_1H_cast_spell_01.fbx/1H_Cast_02_a.anim", "1H_Cast_02_a", m186464);
            m186466 = CreateAnimationField("1H_Cast_02_b", "Assets/ootii/MotionControllerPacks/SpellCasting/Content/Animations/Mixamo/Y_Bot@standing_1H_cast_spell_01.fbx/1H_Cast_02_b.anim", "1H_Cast_02_b", m186466);
            m186468 = CreateAnimationField("1H_Cast_02_c", "Assets/ootii/MotionControllerPacks/SpellCasting/Content/Animations/Mixamo/Y_Bot@standing_1H_cast_spell_01.fbx/1H_Cast_02_c.anim", "1H_Cast_02_c", m186468);
            m22246 = CreateAnimationField("2H_Cast_02.Standing_2H_Magic_Area_Attack_01", "Assets/ootii/MotionControllerPacks/SpellCasting/Content/Animations/Mixamo/Y_Bot@Standing_2H_Magic_Area_Attack_01.fbx/Standing_2H_Magic_Area_Attack_01.anim", "Standing_2H_Magic_Area_Attack_01", m22246);
            m241242 = CreateAnimationField("2H_Cast_02_a", "Assets/ootii/MotionControllerPacks/SpellCasting/Content/Animations/Mixamo/Y_Bot@Standing_2H_Magic_Area_Attack_01.fbx/2H_Cast_02_a.anim", "2H_Cast_02_a", m241242);
            m241244 = CreateAnimationField("2H_Cast_02_b", "Assets/ootii/MotionControllerPacks/SpellCasting/Content/Animations/Mixamo/Y_Bot@Standing_2H_Magic_Area_Attack_01.fbx/2H_Cast_02_b.anim", "2H_Cast_02_b", m241244);
            m241246 = CreateAnimationField("2H_Cast_02_c", "Assets/ootii/MotionControllerPacks/SpellCasting/Content/Animations/Mixamo/Y_Bot@Standing_2H_Magic_Area_Attack_01.fbx/2H_Cast_02_c.anim", "2H_Cast_02_c", m241246);
            m242640 = CreateAnimationField("2H_Cast_03", "Assets/ootii/MotionControllerPacks/SpellCasting/Content/Animations/Mixamo/Y_Bot@Standing_2H_Magic_Area_Attack_02.fbx/2H_Cast_03.anim", "2H_Cast_03", m242640);
            m242642 = CreateAnimationField("2H_Cast_03_a", "Assets/ootii/MotionControllerPacks/SpellCasting/Content/Animations/Mixamo/Y_Bot@Standing_2H_Magic_Area_Attack_02.fbx/2H_Cast_03_a.anim", "2H_Cast_03_a", m242642);
            m242644 = CreateAnimationField("2H_Cast_03_b", "Assets/ootii/MotionControllerPacks/SpellCasting/Content/Animations/Mixamo/Y_Bot@Standing_2H_Magic_Area_Attack_02.fbx/2H_Cast_03_b.anim", "2H_Cast_03_b", m242644);
            m242646 = CreateAnimationField("2H_Cast_03_c", "Assets/ootii/MotionControllerPacks/SpellCasting/Content/Animations/Mixamo/Y_Bot@Standing_2H_Magic_Area_Attack_02.fbx/2H_Cast_03_c.anim", "2H_Cast_03_c", m242646);
            m242650 = CreateAnimationField("2H_Cast_04", "Assets/ootii/MotionControllerPacks/SpellCasting/Content/Animations/Mixamo/Y_Bot@Standing_2H_Magic_Attack_01.fbx/2H_Cast_04.anim", "2H_Cast_04", m242650);
            m242652 = CreateAnimationField("2H_Cast_04_a", "Assets/ootii/MotionControllerPacks/SpellCasting/Content/Animations/Mixamo/Y_Bot@Standing_2H_Magic_Attack_01.fbx/2H_Cast_04_a.anim", "2H_Cast_04_a", m242652);
            m242654 = CreateAnimationField("2H_Cast_04_b", "Assets/ootii/MotionControllerPacks/SpellCasting/Content/Animations/Mixamo/Y_Bot@Standing_2H_Magic_Attack_01.fbx/2H_Cast_04_b.anim", "2H_Cast_04_b", m242654);
            m242656 = CreateAnimationField("2H_Cast_04_c", "Assets/ootii/MotionControllerPacks/SpellCasting/Content/Animations/Mixamo/Y_Bot@Standing_2H_Magic_Attack_01.fbx/2H_Cast_04_c.anim", "2H_Cast_04_c", m242656);
            m242660 = CreateAnimationField("2H_Cast_05", "Assets/ootii/MotionControllerPacks/SpellCasting/Content/Animations/Mixamo/Y_Bot@Standing_2H_Magic_Attack_02.fbx/2H_Cast_05.anim", "2H_Cast_05", m242660);
            m242662 = CreateAnimationField("2H_Cast_05_a", "Assets/ootii/MotionControllerPacks/SpellCasting/Content/Animations/Mixamo/Y_Bot@Standing_2H_Magic_Attack_02.fbx/2H_Cast_05_a.anim", "2H_Cast_05_a", m242662);
            m242664 = CreateAnimationField("2H_Cast_05_b", "Assets/ootii/MotionControllerPacks/SpellCasting/Content/Animations/Mixamo/Y_Bot@Standing_2H_Magic_Attack_02.fbx/2H_Cast_05_b.anim", "2H_Cast_05_b", m242664);
            m242666 = CreateAnimationField("2H_Cast_05_c", "Assets/ootii/MotionControllerPacks/SpellCasting/Content/Animations/Mixamo/Y_Bot@Standing_2H_Magic_Attack_02.fbx/2H_Cast_05_c.anim", "2H_Cast_05_c", m242666);
            m242670 = CreateAnimationField("2H_Cast_06", "Assets/ootii/MotionControllerPacks/SpellCasting/Content/Animations/Mixamo/Y_Bot@Standing_2H_Magic_Attack_03.fbx/2H_Cast_06.anim", "2H_Cast_06", m242670);
            m242672 = CreateAnimationField("2H_Cast_06_a", "Assets/ootii/MotionControllerPacks/SpellCasting/Content/Animations/Mixamo/Y_Bot@Standing_2H_Magic_Attack_03.fbx/2H_Cast_06_a.anim", "2H_Cast_06_a", m242672);
            m242674 = CreateAnimationField("2H_Cast_06_b", "Assets/ootii/MotionControllerPacks/SpellCasting/Content/Animations/Mixamo/Y_Bot@Standing_2H_Magic_Attack_03.fbx/2H_Cast_06_b.anim", "2H_Cast_06_b", m242674);
            m242676 = CreateAnimationField("2H_Cast_06_c", "Assets/ootii/MotionControllerPacks/SpellCasting/Content/Animations/Mixamo/Y_Bot@Standing_2H_Magic_Attack_03.fbx/2H_Cast_06_c.anim", "2H_Cast_06_c", m242676);
            m242680 = CreateAnimationField("2H_Cast_07", "Assets/ootii/MotionControllerPacks/SpellCasting/Content/Animations/Mixamo/Y_Bot@Standing_2H_Magic_Attack_04.fbx/2H_Cast_07.anim", "2H_Cast_07", m242680);
            m242682 = CreateAnimationField("2H_Cast_07_a", "Assets/ootii/MotionControllerPacks/SpellCasting/Content/Animations/Mixamo/Y_Bot@Standing_2H_Magic_Attack_04.fbx/2H_Cast_07_a.anim", "2H_Cast_07_a", m242682);
            m242684 = CreateAnimationField("2H_Cast_07_b", "Assets/ootii/MotionControllerPacks/SpellCasting/Content/Animations/Mixamo/Y_Bot@Standing_2H_Magic_Attack_04.fbx/2H_Cast_07_b.anim", "2H_Cast_07_b", m242684);
            m15810 = CreateAnimationField("2H_Cast_07_c", "Assets/ootii/MotionControllerPacks/SpellCasting/Content/Animations/Mixamo/Y_Bot@Standing_2H_Magic_Attack_04.fbx/2H_Cast_07_c.anim", "2H_Cast_07_c", m15810);
            m242690 = CreateAnimationField("2H_Cast_08", "Assets/ootii/MotionControllerPacks/SpellCasting/Content/Animations/Mixamo/Y_Bot@Standing_2H_Magic_Attack_05.fbx/2H_Cast_08.anim", "2H_Cast_08", m242690);
            m242692 = CreateAnimationField("2H_Cast_08_a", "Assets/ootii/MotionControllerPacks/SpellCasting/Content/Animations/Mixamo/Y_Bot@Standing_2H_Magic_Attack_05.fbx/2H_Cast_08_a.anim", "2H_Cast_08_a", m242692);
            m242694 = CreateAnimationField("2H_Cast_08_b", "Assets/ootii/MotionControllerPacks/SpellCasting/Content/Animations/Mixamo/Y_Bot@Standing_2H_Magic_Attack_05.fbx/2H_Cast_08_b.anim", "2H_Cast_08_b", m242694);
            m242696 = CreateAnimationField("2H_Cast_08_c", "Assets/ootii/MotionControllerPacks/SpellCasting/Content/Animations/Mixamo/Y_Bot@Standing_2H_Magic_Attack_05.fbx/2H_Cast_08_c.anim", "2H_Cast_08_c", m242696);
            m242620 = CreateAnimationField("1H_Cast_03", "Assets/ootii/MotionControllerPacks/SpellCasting/Content/Animations/Mixamo/Y_Bot@Standing_1H_Magic_Attack_02.fbx/1H_Cast_03.anim", "1H_Cast_03", m242620);
            m242622 = CreateAnimationField("1H_Cast_03_a", "Assets/ootii/MotionControllerPacks/SpellCasting/Content/Animations/Mixamo/Y_Bot@Standing_1H_Magic_Attack_02.fbx/1H_Cast_03_a.anim", "1H_Cast_03_a", m242622);
            m242624 = CreateAnimationField("1H_Cast_03_b", "Assets/ootii/MotionControllerPacks/SpellCasting/Content/Animations/Mixamo/Y_Bot@Standing_1H_Magic_Attack_02.fbx/1H_Cast_03_b.anim", "1H_Cast_03_b", m242624);
            m242626 = CreateAnimationField("1H_Cast_03_c", "Assets/ootii/MotionControllerPacks/SpellCasting/Content/Animations/Mixamo/Y_Bot@Standing_1H_Magic_Attack_02.fbx/1H_Cast_03_c.anim", "1H_Cast_03_c", m242626);
            m242630 = CreateAnimationField("1H_Cast_04", "Assets/ootii/MotionControllerPacks/SpellCasting/Content/Animations/Mixamo/Y_Bot@Standing_1H_Magic_Attack_03.fbx/1H_Cast_04.anim", "1H_Cast_04", m242630);
            m242632 = CreateAnimationField("1H_Cast_04_a", "Assets/ootii/MotionControllerPacks/SpellCasting/Content/Animations/Mixamo/Y_Bot@Standing_1H_Magic_Attack_03.fbx/1H_Cast_04_a.anim", "1H_Cast_04_a", m242632);
            m242634 = CreateAnimationField("1H_Cast_04_b", "Assets/ootii/MotionControllerPacks/SpellCasting/Content/Animations/Mixamo/Y_Bot@Standing_1H_Magic_Attack_03.fbx/1H_Cast_04_b.anim", "1H_Cast_04_b", m242634);
            m242636 = CreateAnimationField("1H_Cast_04_c", "Assets/ootii/MotionControllerPacks/SpellCasting/Content/Animations/Mixamo/Y_Bot@Standing_1H_Magic_Attack_03.fbx/1H_Cast_04_c.anim", "1H_Cast_04_c", m242636);

            // Add the remaining functionality
            base.OnSettingsGUI();
        }

#endif

        // ************************************ END AUTO GENERATED ************************************
        #endregion
    }
}
