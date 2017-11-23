using UnityEngine;
using com.ootii.Actors.AnimationControllers;
using com.ootii.Cameras;
using com.ootii.Geometry;
using com.ootii.Helpers;
using com.ootii.Timing;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace com.ootii.MotionControllerPacks
{
    /// <summary>
    /// </summary>
    [MotionName("PMP - Walk Run Pivot")]
    [MotionDescription("Spell casting movement (walk/run) for an adventure game. Uses the Mixamo Pro Magic Pack animations.")]
    public class PMP_WalkRunPivot : PMP_MotionBase, IWalkRunMotion
    {
        /// <summary>
        /// Trigger values for th emotion
        /// </summary>
        public const int PHASE_UNKNOWN = 0;
        public const int PHASE_START = 32200;
        public const int PHASE_STOP = 32201;

        public const int PHASE_START_IDLE_PIVOT = 32205;

        /// <summary>
        /// Determines if we run by default or walk
        /// </summary>
        public bool _DefaultToRun = false;
        public bool DefaultToRun
        {
            get { return _DefaultToRun; }
            set { _DefaultToRun = value; }
        }

        /// <summary>
        /// Determines if the actor should be running based on input
        /// </summary>
        public bool IsRunActive
        {
            get
            {
                if (mMotionController.TargetNormalizedSpeed > 0f && mMotionController.TargetNormalizedSpeed <= 0.5f) { return false; }
                if (mMotionController._InputSource == null) { return _DefaultToRun; }
                return ((_DefaultToRun && !mMotionController._InputSource.IsPressed(_ActionAlias)) || (!_DefaultToRun && mMotionController._InputSource.IsPressed(_ActionAlias)));
            }
        }

        /// <summary>
        /// Speed (units per second) when walking
        /// </summary>
        public float _WalkSpeed = 1.2f;
        public virtual float WalkSpeed
        {
            get { return _WalkSpeed; }
            set { _WalkSpeed = value; }
        }

        /// <summary>
        /// Speed (units per second) when running
        /// </summary>
        public float _RunSpeed = 3.2f;
        public virtual float RunSpeed
        {
            get { return _RunSpeed; }
            set { _RunSpeed = value; }
        }

        /// <summary>
        /// Degrees per second to rotate the actor in order to face the input direction
        /// </summary>
        public float _RotationSpeed = 270f;
        public float RotationSpeed
        {
            get { return _RotationSpeed; }
            set { _RotationSpeed = value; }
        }

        /// <summary>
        /// Determines if we shortcut the motion and start in the loop
        /// </summary>
        private bool mStartInMove = false;
        public bool StartInMove
        {
            get { return mStartInMove; }
            set { mStartInMove = value; }
        }

        /// <summary>
        /// Determines if we shortcut the motion and start in a run
        /// </summary>
        private bool mStartInWalk = false;
        public bool StartInWalk
        {
            get { return mStartInWalk; }

            set
            {
                mStartInWalk = value;
                if (value) { mStartInMove = value; }
            }
        }

        /// <summary>
        /// Determines if we shortcut the motion and start in a run
        /// </summary>
        private bool mStartInRun = false;
        public bool StartInRun
        {
            get { return mStartInRun; }

            set
            {
                mStartInRun = value;
                if (value) { mStartInMove = value; }
            }
        }

        /// <summary>
        /// Multiplier for the frame distance that determines when we unlink the
        /// camera and the actor
        /// </summary>
        public float _LinkFactor = 4f;
        public float LinkFactor
        {
            get { return _LinkFactor; }
            set { _LinkFactor = value; }
        }

        /// <summary>
        /// Time in seconds to keep input active when the input actually ends. This way
        /// we can transition out of the blend tree nicely
        /// </summary>
        public float _StopDelay = 0.15f;
        public virtual float StopDelay
        {
            get { return _StopDelay; }
            set { _StopDelay = value; }
        }

        /// <summary>
        /// Used to help manage stopping by adding a delay to when we drop the input
        /// </summary>
        protected float mStopTime = 0f;

        /// <summary>
        /// Input value at the time we started stopping
        /// </summary>
        protected Vector2 mStopInput = Vector2.zero;

        /// <summary>
        /// Determines if we link the actor rotation to the camera rotation
        /// </summary>
        protected bool mIsLinked = false;

        /// <summary>
        /// Track the angle we have from the input
        /// </summary>
        protected Vector3 mStoredInputForward = Vector3.forward;

        /// <summary>
        /// Speed we'll actually apply to the rotation. This is essencially the
        /// number of degrees per tick assuming we're running at 60 FPS
        /// </summary>
        protected float mDegreesPer60FPSTick = 1f;

        /// <summary>
        /// We use these classes to help smooth the input values so that
        /// movement doesn't drop from 1 to 0 immediately.
        /// </summary>
        protected FloatValue mInputX = new FloatValue(0f, 10);
        protected FloatValue mInputY = new FloatValue(0f, 10);
        protected FloatValue mInputMagnitude = new FloatValue(0f, 15);

        /// <summary>
        /// Default constructor
        /// </summary>
        public PMP_WalkRunPivot()
            : base()
        {
            _Category = EnumMotionCategories.WALK;

            _Priority = 6;
            _ActionAlias = "Run";

#if UNITY_EDITOR
            if (_EditorAnimatorSMName.Length == 0) { _EditorAnimatorSMName = "PMP_WalkRunPivot-SM"; }
#endif
        }

        /// <summary>
        /// Controller constructor
        /// </summary>
        /// <param name="rController">Controller the motion belongs to</param>
        public PMP_WalkRunPivot(MotionController rController)
            : base(rController)
        {
            _Category = EnumMotionCategories.WALK;

            _Priority = 6;
            _ActionAlias = "Run";

#if UNITY_EDITOR
            if (_EditorAnimatorSMName.Length == 0) { _EditorAnimatorSMName = "PMP_WalkRunPivot-SM"; }
#endif
        }

        /// <summary>
        /// Allows for any processing after the motion has been deserialized
        /// </summary>
        public override void Awake()
        {
            base.Awake();

            // Default the speed we'll use to rotate
            mDegreesPer60FPSTick = _RotationSpeed / 60f;
        }

        /// <summary>
        /// Tests if this motion should be started. However, the motion
        /// isn't actually started.
        /// </summary>
        /// <returns></returns>
        public override bool TestActivate()
        {
            if (!mIsStartable)
            {
                return false;
            }

            // If we're not grounded, this is easy
            if (!mMotionController.IsGrounded)
            {
                return false;
            }

            // If we're not in the traversal state, this is easy
            if (mActorController.State.Stance != EnumControllerStance.SPELL_CASTING)
            {
                return false;
            }

            // If we're not actually moving. We use the value here since we'll
            // stop if our value is < 0.4f;
            if (mMotionController.State.InputMagnitudeTrend.Value < 0.4f)
            {
                return false;
            }

            // We're good to move
            return true;
        }

        /// <summary>
        /// Tests if the motion should continue. If it shouldn't, the motion
        /// is typically disabled
        /// </summary>
        /// <returns>Boolean that determines if the motion continues</returns>
        public override bool TestUpdate()
        {
            if (mIsActivatedFrame) { return true; }
            if (!mMotionController.IsGrounded) { return false; }
            if (mActorController.State.Stance != EnumControllerStance.SPELL_CASTING) { return false; }

            // If we're in the idle state with no movement, stop
            if (mAge > 0.2f && mMotionLayer._AnimatorStateID == STATE_IdlePose)
            {
                return false;
            }

            // Ensure we're in the animation
            if (mIsAnimatorActive)
            {
                // One last check to make sure we're in this state
                if (!IsInMotionState)
                {
                    return false;
                }
            }

            // Stay in
            return true;
        }

        /// <summary>
        /// Raised when a motion is being interrupted by another motion
        /// </summary>
        /// <param name="rMotion">Motion doing the interruption</param>
        /// <returns>Boolean determining if it can be interrupted</returns>
        public override bool TestInterruption(MotionControllerMotion rMotion)
        {
            // Since we're dealing with a blend tree, keep the value until the transition completes            
            mMotionController.ForcedInput.x = mInputX.Average;
            mMotionController.ForcedInput.y = mInputY.Average;

            return true;
        }

        /// <summary>
        /// Called to start the specific motion. If the motion
        /// were something like 'jump', this would start the jumping process
        /// </summary>
        /// <param name="rPrevMotion">Motion that this motion is taking over from</param>
        public override bool Activate(MotionControllerMotion rPrevMotion)
        {
            // Store the target forward based on input
            mStoredInputForward = mMotionController.State.InputForward;

            // Register this motion with the camera
            if (mMotionController.CameraRig is BaseCameraRig)
            {
                ((BaseCameraRig)mMotionController.CameraRig).OnPostLateUpdate -= OnCameraUpdated;
                ((BaseCameraRig)mMotionController.CameraRig).OnPostLateUpdate += OnCameraUpdated;
            }

            // Update the max speed based on our animation
            mMotionController.MaxSpeed = (_RunSpeed > 0f ? _RunSpeed : 3.2f);

            // Tell the animator to start your animations
            mMotionController.SetAnimatorMotionPhase(mMotionLayer._AnimatorLayerIndex, PHASE_START, true);

            // Return
            return base.Activate(rPrevMotion);
        }

        /// <summary>
        /// Called to stop the motion. If the motion is stopable. Some motions
        /// like jump cannot be stopped early
        /// </summary>
        public override void Deactivate()
        {
            // Unregister this motion with the camera
            if (mMotionController.CameraRig is BaseCameraRig)
            {
                ((BaseCameraRig)mMotionController.CameraRig).OnPostLateUpdate -= OnCameraUpdated;
            }

            // Finish the deactivation process
            base.Deactivate();
        }

        /// <summary>
        /// Allows the motion to modify the root-motion velocities before they are applied. 
        /// 
        /// NOTE:
        /// Be careful when removing rotations as some transitions will want rotations even 
        /// if the state they are transitioning from don't.
        /// </summary>
        /// <param name="rDeltaTime">Time since the last frame (or fixed update call)</param>
        /// <param name="rUpdateIndex">Index of the update to help manage dynamic/fixed updates. [0: Invalid update, >=1: Valid update]</param>
        /// <param name="rVelocityDelta">Root-motion linear velocity relative to the actor's forward</param>
        /// <param name="rRotationDelta">Root-motion rotational velocity</param>
        /// <returns></returns>
        public override void UpdateRootMotion(float rDeltaTime, int rUpdateIndex, ref Vector3 rMovement, ref Quaternion rRotation)
        {
            rRotation = Quaternion.identity;

            bool lIsRunning = IsRunActive && mMotionController.State.InputMagnitudeTrend.Value > 0.6f;
            if ((lIsRunning && _RunSpeed > 0f) || (!lIsRunning && _WalkSpeed > 0f))
            {
                rMovement = Vector3.zero;

                if (mMotionLayer._AnimatorTransitionID == TRANS_EntryState_MoveTree ||
                    (mMotionLayer._AnimatorStateID == STATE_MoveTree && mMotionLayer._AnimatorTransitionID == 0))
                {
                    if (mMotionController.State.InputForward.sqrMagnitude > 0f)
                    {
                        rMovement = Vector3.forward;
                    }
                }

                rMovement = rMovement * ((lIsRunning ? _RunSpeed : _WalkSpeed) * rDeltaTime);
            }
            else
            {
                // Clear out any excess movement from the animations
                if (mMotionLayer._AnimatorTransitionID == TRANS_MoveTree_IdlePose)
                {
                    rMovement = Vector3.zero;
                }
                // This is an odd case to avoid the character from going backwards before going forward.
                // Unfortunately, the animation with 'center of mass' seems to do this.
                else if (mMotionLayer._AnimatorTransitionID == TRANS_EntryState_MoveTree)
                {
                    if (mMotionController.State.InputX == 0f && mMotionController.State.InputY > 0f)
                    {
                        if (rMovement.z < 0) { rMovement = Vector3.zero; }
                    }
                }

                rMovement.x = 0f;
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
            mRotation = Quaternion.identity;

            // Grab the state info
            MotionState lState = mMotionController.State;

            // Determine if we need to update our target forward
            if (lState.InputForward.sqrMagnitude > 0.01f)
            {
                mStoredInputForward = lState.InputForward;
            }

            // Convert the input to radial so we deal with keyboard and gamepad input the same.
            float lInputX = lState.InputX;
            float lInputY = lState.InputY;
            float lInputMag = lState.InputMagnitudeTrend.Value;
            InputManagerHelper.ConvertToRadialInput(ref lInputX, ref lInputY, ref lInputMag, (IsRunActive ? 1f : 0.5f));

            // Ensure we support the stop delay. This way, we can get out
            // of the blend tree with a nice transition
            if (lState.InputMagnitudeTrend.Value < 0.4f)
            {
                // Only set the timer if it's not set yet
                if (mStopTime == 0f)
                {
                    mStopInput.x = mInputX.Average;
                    mStopInput.y = mInputY.Average;
                    mStopTime = Time.time + _StopDelay;

                    mInputX.Clear(mStopInput.x);
                    mInputY.Clear(mStopInput.y);
                    mInputMagnitude.Clear(Mathf.Sqrt((mInputX.Value * mInputX.Value) + (mInputY.Value * mInputY.Value)));
                }
            }
            // Clear the timer
            else { mStopTime = 0f; }

            // When we're processing normally, update all the input values
            if (mStopTime == 0f)
            {
                mInputX.Add(lInputX);
                mInputY.Add(lInputY);
                mInputMagnitude.Add(lInputMag);
            }
            // If we've reached our stop time, it's time to stop
            else if (Time.time > mStopTime)
            {
                // Determine how we'll stop based on the direction
                if (!(mMotionLayer._AnimatorStateID == STATE_IdlePose))
                {
                    mMotionController.SetAnimatorMotionPhase(mMotionLayer._AnimatorLayerIndex, PHASE_STOP, 0, true);
                }
                // If we're already stopping, we can clear our movement info. We don't want
                // to clear the movement before the transition our our blend tree will drop to idle
                else
                {
                    mStopTime = 0f;
                    mInputX.Clear();
                    mInputY.Clear();
                    mInputMagnitude.Clear();

                    lState.AnimatorStates[mMotionLayer._AnimatorLayerIndex].MotionPhase = 0;
                    lState.AnimatorStates[mMotionLayer._AnimatorLayerIndex].MotionParameter = 0;
                }
            }

            // Modify the input values to add some lag
            lState.InputX = mInputX.Average;
            lState.InputY = mInputY.Average;
            lState.InputMagnitudeTrend.Replace(mInputMagnitude.Average);

            // Finally, set the state value
            mMotionController.State = lState;

            // If we're not dealing with an ootii camera rig, we need to rotate to the camera here
            if (!(mMotionController.CameraRig is BaseCameraRig))
            {
                OnCameraUpdated(rDeltaTime, rUpdateIndex, null);
            }

            // Allow the base class to render debug info
            base.Update(rDeltaTime, rUpdateIndex);
        }

        /// <summary>
        /// When we want to rotate based on the camera direction, we need to tweak the actor
        /// rotation AFTER we process the camera. Otherwise, we can get small stutters during camera rotation. 
        /// 
        /// This is the only way to keep them totally in sync. It also means we can't run any of our AC processing
        /// as the AC already ran. So, we do minimal work here
        /// </summary>
        /// <param name="rDeltaTime"></param>
        /// <param name="rUpdateIndex"></param>
        /// <param name="rCamera"></param>
        protected void OnCameraUpdated(float rDeltaTime, int rUpdateIndex, BaseCameraRig rCamera)
        {
            if (mMotionController._CameraTransform == null) { return; }

            // Grab the angle needed to get to our target forward
            Vector3 lActorInputForward = rCamera._Transform.rotation * mStoredInputForward;
            float lInputFromAvatar = NumberHelper.GetHorizontalAngle(mMotionController._Transform.forward, lActorInputForward);
            if (lInputFromAvatar == 0f) { return; }

            float lInputFromSign = Mathf.Sign(lInputFromAvatar);
            float lInputFromAngle = Mathf.Abs(lInputFromAvatar);
            float lRotationAngle = mDegreesPer60FPSTick * TimeManager.Relative60FPSDeltaTime;

            // Break the link if we have too far to rotate
            if (lInputFromAngle > lRotationAngle * _LinkFactor)
            {
                mIsLinked = false;
            }

            // Establish the link if we're close enough
            if (mIsLinked || lInputFromAngle < lRotationAngle)
            {
                mIsLinked = true;
                lRotationAngle = lInputFromAngle;
            }

            // Use the information and AC to determine our final rotation
            Quaternion lRotation = Quaternion.AngleAxis(lInputFromSign * lRotationAngle, Vector3.up);
            mActorController.Yaw = mActorController.Yaw * lRotation;
            mActorController._Transform.rotation = mActorController.Tilt * mActorController.Yaw;
        }

        // **************************************************************************************************
        // Following properties and function only valid while editing
        // **************************************************************************************************

#if UNITY_EDITOR

        /// <summary>
        /// Allow the motion to render it's own GUI
        /// </summary>
        public override bool OnInspectorGUI()
        {
            bool lIsDirty = false;

            bool lNewDefaultToRun = EditorGUILayout.Toggle(new GUIContent("Default to Run", "Determines if the default is to run or walk."), _DefaultToRun);
            if (lNewDefaultToRun != _DefaultToRun)
            {
                lIsDirty = true;
                DefaultToRun = lNewDefaultToRun;
            }

            string lNewActionAlias = EditorGUILayout.TextField(new GUIContent("Run Action Alias", "Action alias that triggers a run or walk (which ever is opposite the default)."), ActionAlias, GUILayout.MinWidth(30));
            if (lNewActionAlias != ActionAlias)
            {
                lIsDirty = true;
                ActionAlias = lNewActionAlias;
            }

            GUILayout.Space(5f);

            if (EditorHelper.FloatField("Walk Speed", "Speed (units per second) to move when walking. Set to 0 to use root-motion.", WalkSpeed, mMotionController))
            {
                lIsDirty = true;
                WalkSpeed = EditorHelper.FieldFloatValue;
            }

            if (EditorHelper.FloatField("Run Speed", "Speed (units per second) to move when running. Set to 0 to use root-motion.", RunSpeed, mMotionController))
            {
                lIsDirty = true;
                RunSpeed = EditorHelper.FieldFloatValue;
            }

            GUILayout.Space(5f);

            float lNewRotationSpeed = EditorGUILayout.FloatField(new GUIContent("Rotation Speed", "Degrees per second to rotate towards the camera forward (when not pivoting). A value of '0' means rotate instantly."), RotationSpeed);
            if (lNewRotationSpeed != RotationSpeed)
            {
                lIsDirty = true;
                RotationSpeed = lNewRotationSpeed;
            }

            return lIsDirty;
        }

#endif

        #region Auto-Generated
        // ************************************ START AUTO GENERATED ************************************

        /// <summary>
        /// These declarations go inside the class so you can test for which state
        /// and transitions are active. Testing hash values is much faster than strings.
        /// </summary>
        public static int STATE_Start = -1;
        public static int STATE_MoveTree = -1;
        public static int STATE_IdlePose = -1;
        public static int TRANS_AnyState_MoveTree = -1;
        public static int TRANS_EntryState_MoveTree = -1;
        public static int TRANS_MoveTree_IdlePose = -1;
        public static int TRANS_IdlePose_MoveTree = -1;

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
                    if (lStateID == STATE_MoveTree) { return true; }
                    if (lStateID == STATE_IdlePose) { return true; }
                }

                if (lTransitionID == TRANS_AnyState_MoveTree) { return true; }
                if (lTransitionID == TRANS_EntryState_MoveTree) { return true; }
                if (lTransitionID == TRANS_MoveTree_IdlePose) { return true; }
                if (lTransitionID == TRANS_IdlePose_MoveTree) { return true; }
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
            if (rStateID == STATE_MoveTree) { return true; }
            if (rStateID == STATE_IdlePose) { return true; }
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
                if (rStateID == STATE_MoveTree) { return true; }
                if (rStateID == STATE_IdlePose) { return true; }
            }

            if (rTransitionID == TRANS_AnyState_MoveTree) { return true; }
            if (rTransitionID == TRANS_EntryState_MoveTree) { return true; }
            if (rTransitionID == TRANS_MoveTree_IdlePose) { return true; }
            if (rTransitionID == TRANS_IdlePose_MoveTree) { return true; }
            return false;
        }

        /// <summary>
        /// Preprocess any animator data so the motion can use it later
        /// </summary>
        public override void LoadAnimatorData()
        {
            TRANS_AnyState_MoveTree = mMotionController.AddAnimatorName("AnyState -> Base Layer.PMP_WalkRunPivot-SM.Move Tree");
            TRANS_EntryState_MoveTree = mMotionController.AddAnimatorName("Entry -> Base Layer.PMP_WalkRunPivot-SM.Move Tree");
            STATE_Start = mMotionController.AddAnimatorName("Base Layer.Start");
            STATE_MoveTree = mMotionController.AddAnimatorName("Base Layer.PMP_WalkRunPivot-SM.Move Tree");
            TRANS_MoveTree_IdlePose = mMotionController.AddAnimatorName("Base Layer.PMP_WalkRunPivot-SM.Move Tree -> Base Layer.PMP_WalkRunPivot-SM.IdlePose");
            STATE_IdlePose = mMotionController.AddAnimatorName("Base Layer.PMP_WalkRunPivot-SM.IdlePose");
            TRANS_IdlePose_MoveTree = mMotionController.AddAnimatorName("Base Layer.PMP_WalkRunPivot-SM.IdlePose -> Base Layer.PMP_WalkRunPivot-SM.Move Tree");
        }

#if UNITY_EDITOR

        private AnimationClip m16672 = null;
        private AnimationClip m17768 = null;
        private AnimationClip m16640 = null;
        private AnimationClip m19780 = null;

        /// <summary>
        /// Creates the animator substate machine for this motion.
        /// </summary>
        protected override void CreateStateMachine()
        {
            // Grab the root sm for the layer
            UnityEditor.Animations.AnimatorStateMachine lRootStateMachine = _EditorAnimatorController.layers[mMotionLayer.AnimatorLayerIndex].stateMachine;
            UnityEditor.Animations.AnimatorStateMachine lSM_38592 = _EditorAnimatorController.layers[mMotionLayer.AnimatorLayerIndex].stateMachine;
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

            UnityEditor.Animations.AnimatorStateMachine lSM_38604 = lRootSubStateMachine;
            if (lSM_38604 != null)
            {
                for (int i = lSM_38604.entryTransitions.Length - 1; i >= 0; i--)
                {
                    lSM_38604.RemoveEntryTransition(lSM_38604.entryTransitions[i]);
                }

                for (int i = lSM_38604.anyStateTransitions.Length - 1; i >= 0; i--)
                {
                    lSM_38604.RemoveAnyStateTransition(lSM_38604.anyStateTransitions[i]);
                }

                for (int i = lSM_38604.states.Length - 1; i >= 0; i--)
                {
                    lSM_38604.RemoveState(lSM_38604.states[i].state);
                }

                for (int i = lSM_38604.stateMachines.Length - 1; i >= 0; i--)
                {
                    lSM_38604.RemoveStateMachine(lSM_38604.stateMachines[i].stateMachine);
                }
            }
            else
            {
                lSM_38604 = lSM_38592.AddStateMachine(_EditorAnimatorSMName, new Vector3(624, 264, 0));
            }

            UnityEditor.Animations.AnimatorState lS_38960 = lSM_38604.AddState("Move Tree", new Vector3(192, 216, 0));
            lS_38960.speed = 1f;

            UnityEditor.Animations.BlendTree lM_14838 = CreateBlendTree("Move Blend Tree", _EditorAnimatorController, mMotionLayer.AnimatorLayerIndex);
            lM_14838.blendType = UnityEditor.Animations.BlendTreeType.Simple1D;
            lM_14838.blendParameter = "InputMagnitude";
            lM_14838.blendParameterY = "InputX";
#if !(UNITY_4_0 || UNITY_4_0_1 || UNITY_4_1 || UNITY_4_2 || UNITY_4_3 || UNITY_4_4 || UNITY_4_5 || UNITY_4_6 || UNITY_5_0 || UNITY_5_1 || UNITY_5_2 || UNITY_5_3)
            lM_14838.useAutomaticThresholds = false;
#endif
            lM_14838.AddChild(m17768, 0f);
            lM_14838.AddChild(m17768, 0.5f);
            lM_14838.AddChild(m16640, 1f);
            lS_38960.motion = lM_14838;

            UnityEditor.Animations.AnimatorState lS_39156 = lSM_38604.AddState("IdlePose", new Vector3(492, 216, 0));
            lS_39156.speed = 1f;
            lS_39156.motion = m19780;

            // Create the transition from the any state. Note that 'AnyState' transitions have to be added to the root
            UnityEditor.Animations.AnimatorStateTransition lT_38780 = lRootStateMachine.AddAnyStateTransition(lS_38960);
            lT_38780.hasExitTime = false;
            lT_38780.hasFixedDuration = true;
            lT_38780.exitTime = 0.9f;
            lT_38780.duration = 0.2f;
            lT_38780.offset = 0f;
            lT_38780.mute = false;
            lT_38780.solo = false;
            lT_38780.canTransitionToSelf = false;
            lT_38780.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            lT_38780.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 32200f, "L0MotionPhase");
            lT_38780.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 0f, "L0MotionParameter");

            UnityEditor.Animations.AnimatorStateTransition lT_39166 = lS_38960.AddTransition(lS_39156);
            lT_39166.hasExitTime = false;
            lT_39166.hasFixedDuration = true;
            lT_39166.exitTime = 0.7383721f;
            lT_39166.duration = 0.15f;
            lT_39166.offset = 0f;
            lT_39166.mute = false;
            lT_39166.solo = false;
            lT_39166.canTransitionToSelf = true;
            lT_39166.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            lT_39166.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 32201f, "L0MotionPhase");

            UnityEditor.Animations.AnimatorStateTransition lT_39190 = lS_39156.AddTransition(lS_38960);
            lT_39190.hasExitTime = false;
            lT_39190.hasFixedDuration = true;
            lT_39190.exitTime = 0f;
            lT_39190.duration = 0.1f;
            lT_39190.offset = 0f;
            lT_39190.mute = false;
            lT_39190.solo = false;
            lT_39190.canTransitionToSelf = true;
            lT_39190.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            lT_39190.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Greater, 0.2f, "InputMagnitude");

        }

        /// <summary>
        /// Gathers the animations so we can use them when creating the sub-state machine.
        /// </summary>
        public override void FindAnimations()
        {
            m16672 = FindAnimationClip("Assets/ootii/MotionController/Content/Animations/Humanoid/Idling/unity_Idle_IdleToIdlesR.fbx/IdlePose.anim", "IdlePose");
            m17768 = FindAnimationClip("Assets/ootii/MotionControllerPacks/SpellCasting/Content/Animations/Mixamo/Y_Bot@Standing_Walk_Forward.fbx/Standing_Walk_Forward.anim", "Standing_Walk_Forward");
            m16640 = FindAnimationClip("Assets/ootii/MotionControllerPacks/SpellCasting/Content/Animations/Mixamo/Y_Bot@Standing_Run_Forward.fbx/Standing_Run_Forward.anim", "Standing_Run_Forward");
            m19780 = FindAnimationClip("Assets/ootii/MotionControllerPacks/SpellCasting/Content/Animations/Mixamo/Y_Bot@standing_idle.fbx/PMP_IdlePose.anim", "PMP_IdlePose");

            // Add the remaining functionality
            base.FindAnimations();
        }

        /// <summary>
        /// Used to show the settings that allow us to generate the animator setup.
        /// </summary>
        public override void OnSettingsGUI()
        {
            UnityEditor.EditorGUILayout.IntField(new GUIContent("Phase ID", "Phase ID used to transition to the state."), PHASE_START);
            m16672 = CreateAnimationField("Start.IdlePose", "Assets/ootii/MotionController/Content/Animations/Humanoid/Idling/unity_Idle_IdleToIdlesR.fbx/IdlePose.anim", "IdlePose", m16672);
            m17768 = CreateAnimationField("Move Tree.Standing_Walk_Forward", "Assets/ootii/MotionControllerPacks/SpellCasting/Content/Animations/Mixamo/Y_Bot@Standing_Walk_Forward.fbx/Standing_Walk_Forward.anim", "Standing_Walk_Forward", m17768);
            m16640 = CreateAnimationField("Move Tree.Standing_Run_Forward", "Assets/ootii/MotionControllerPacks/SpellCasting/Content/Animations/Mixamo/Y_Bot@Standing_Run_Forward.fbx/Standing_Run_Forward.anim", "Standing_Run_Forward", m16640);
            m19780 = CreateAnimationField("IdlePose.PMP_IdlePose", "Assets/ootii/MotionControllerPacks/SpellCasting/Content/Animations/Mixamo/Y_Bot@standing_idle.fbx/PMP_IdlePose.anim", "PMP_IdlePose", m19780);

            // Add the remaining functionality
            base.OnSettingsGUI();
        }

#endif

        // ************************************ END AUTO GENERATED ************************************
        #endregion
    }
}
