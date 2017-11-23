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
    /// Forward facing strafing walk/run that uses Mixamo's magic animations.
    /// </summary>
    [MotionName("PMP - Walk Run Strafe")]
    [MotionDescription("Forward facing strafing walk/run that uses Mixamo's magic animations.")]
    public class PMP_WalkRunStrafe : PMP_MotionBase
    {
        /// <summary>
        /// Trigger values for th emotion
        /// </summary>
        public const int PHASE_UNKNOWN = 0;
        public const int PHASE_START = 32210;
        public const int PHASE_STOP = 32215;

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
        public float _WalkSpeed = 1.488f;
        public virtual float WalkSpeed
        {
            get { return _WalkSpeed; }
            set { _WalkSpeed = value; }
        }

        /// <summary>
        /// Speed (units per second) when running
        /// </summary>
        public float _RunSpeed = 3.229f;
        public virtual float RunSpeed
        {
            get { return _RunSpeed; }
            set { _RunSpeed = value; }
        }

        /// <summary>
        /// Determines if we rotate by ourselves
        /// </summary>
        public bool _RotateWithInput = true;
        public bool RotateWithInput
        {
            get { return _RotateWithInput; }
            set { _RotateWithInput = value; }
        }

        /// <summary>
        /// Determines if we rotate to match the camera
        /// </summary>
        public bool _RotateWithCamera = false;
        public bool RotateWithCamera
        {
            get { return _RotateWithCamera; }
            set { _RotateWithCamera = value; }
        }

        /// <summary>
        /// Desired degrees of rotation per second
        /// </summary>
        public float _RotationSpeed = 270f;
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
        /// Fields to help smooth out the mouse rotation
        /// </summary>
        protected float mYaw = 0f;
        protected float mYawTarget = 0f;
        protected float mYawVelocity = 0f;

        /// <summary>
        /// Used to help manage stopping by adding a delay to when we drop the input
        /// </summary>
        protected float mStopTime = 0f;

        /// <summary>
        /// Input value at the time we started stopping
        /// </summary>
        protected Vector2 mStopInput = Vector2.zero;

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
        /// Determines if the actor rotation should be linked to the camera
        /// </summary>
        protected bool mLinkRotation = false;

        /// <summary>
        /// Default constructor
        /// </summary>
        public PMP_WalkRunStrafe()
            : base()
        {
            _Category = EnumMotionCategories.WALK;

            _Priority = 7;
            _ActionAlias = "Run";

#if UNITY_EDITOR
            if (_EditorAnimatorSMName.Length == 0) { _EditorAnimatorSMName = "PMP_WalkRunStrafe-SM"; }
#endif
        }

        /// <summary>
        /// Controller constructor
        /// </summary>
        /// <param name="rController">Controller the motion belongs to</param>
        public PMP_WalkRunStrafe(MotionController rController)
            : base(rController)
        {
            _Category = EnumMotionCategories.WALK;

            _Priority = 7;
            _ActionAlias = "Run";

#if UNITY_EDITOR
            if (_EditorAnimatorSMName.Length == 0) { _EditorAnimatorSMName = "PMP_WalkRunStrafe-SM"; }
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
        }

        /// <summary>
        /// Tests if this motion should be started. However, the motion
        /// isn't actually started.
        /// </summary>
        /// <returns></returns>
        public override bool TestActivate()
        {
            if (!mIsStartable) { return false; }
            if (!mMotionController.IsGrounded) { return false; }
            if (mActorController.State.Stance != EnumControllerStance.SPELL_CASTING) { return false; }

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
        /// <returns></returns>
        public override bool TestUpdate()
        {
            if (mIsActivatedFrame) { return true; }
            if (!mMotionController.IsGrounded) { return false; }
            if (mActorController.State.Stance != EnumControllerStance.SPELL_CASTING) { return false; }

            // If we're in the idle state with no movement, stop
            if (mAge > 0.2f && mMotionLayer._AnimatorStateID == STATE_IdlePoseOut)
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
            mStopTime = 0f;
            mStopInput = Vector2.zero;
            mLinkRotation = false;

            // Helps with syncronizing from a motion like attack
            float lRunFactor = (IsRunActive ? 1f : 0.5f);
            mInputX.Clear(mMotionController.State.InputX * lRunFactor);
            mInputY.Clear(mMotionController.State.InputY * lRunFactor);

            // Update the max speed based on our animation
            mMotionController.MaxSpeed = (_RunSpeed > 0f ? _RunSpeed : 3.2f);

            // Determine how we'll start our animation
            mMotionController.SetAnimatorMotionPhase(mMotionLayer.AnimatorLayerIndex, PHASE_START, true);

            // Register this motion with the camera
            if (_RotateWithCamera && mMotionController.CameraRig is BaseCameraRig)
            {
                ((BaseCameraRig)mMotionController.CameraRig).OnPostLateUpdate -= OnCameraUpdated;
                ((BaseCameraRig)mMotionController.CameraRig).OnPostLateUpdate += OnCameraUpdated;
            }

            // Finalize the activation
            return base.Activate(rPrevMotion);
        }

        /// <summary>
        /// Raised when we shut the motion down
        /// </summary>
        public override void Deactivate()
        {
            // Register this motion with the camera
            if (mMotionController.CameraRig is BaseCameraRig)
            {
                ((BaseCameraRig)mMotionController.CameraRig).OnPostLateUpdate -= OnCameraUpdated;
            }

            // Continue with the deactivation
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
            rRotation = Quaternion.identity;

            bool lIsRunning = IsRunActive && mMotionController.State.InputMagnitudeTrend.Value > 0.6f;
            if ((lIsRunning && _RunSpeed > 0f) || (!lIsRunning && _WalkSpeed > 0f))
            {
                rMovement = Vector3.zero;

                if (mMotionLayer._AnimatorTransitionID == TRANS_EntryState_MoveTree ||
                    (mMotionLayer._AnimatorStateID == STATE_MoveTree && mMotionLayer._AnimatorTransitionID == 0))
                {
                    rMovement = Vector3.ClampMagnitude(mMotionController.State.InputForward, 1f);
                }

                rMovement = rMovement * ((lIsRunning ? _RunSpeed : _WalkSpeed) * rDeltaTime);
            }
            else
            {
                // Clear out any excess movement from the animations
                if (mMotionLayer._AnimatorTransitionID == TRANS_MoveTree_IdlePoseOut)
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

            // Convert the input to radial so we deal with keyboard and gamepad input the same.
            float lInputX = lState.InputX;
            float lInputY = lState.InputY;
            float lInputMag = lState.InputMagnitudeTrend.Value;
            InputManagerHelper.ConvertToRadialInput(ref lInputX, ref lInputY, ref lInputMag); //, (IsRunActive ? 1f : 0.5f));

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
                if (!(mMotionLayer._AnimatorStateID == STATE_IdlePoseOut))
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
            if (_RotateWithCamera && !(mMotionController.CameraRig is BaseCameraRig))
            {
                OnCameraUpdated(rDeltaTime, rUpdateIndex, null);
            }

            if (!_RotateWithCamera && _RotateWithInput)
            {
                RotateUsingInput(rDeltaTime, ref mRotation);
            }

            // Allow the base class to render debug info
            base.Update(rDeltaTime, rUpdateIndex);
        }

        /// <summary>
        /// Create a rotation velocity that rotates the character based on input
        /// </summary>
        /// <param name="rDeltaTime"></param>
        /// <param name="rAngularVelocity"></param>
        private void RotateUsingInput(float rDeltaTime, ref Quaternion rRotation)
        {
            // If we don't have an input source, stop
            if (mMotionController._InputSource == null) { return; }

            // Determine this frame's rotation
            float lYawDelta = 0f;
            float lYawSmoothing = 0.1f;

            if (mMotionController._InputSource.IsViewingActivated)
            {
                lYawDelta = mMotionController._InputSource.ViewX * mDegreesPer60FPSTick;
            }

            mYawTarget = mYawTarget + lYawDelta;

            // Smooth the rotation
            lYawDelta = (lYawSmoothing <= 0f ? mYawTarget : Mathf.SmoothDampAngle(mYaw, mYawTarget, ref mYawVelocity, lYawSmoothing)) - mYaw;
            mYaw = mYaw + lYawDelta;

            // Use this frame's smoothed rotation
            if (lYawDelta != 0f)
            {
                rRotation = Quaternion.Euler(0f, lYawDelta, 0f);
            }
        }

        /// <summary>
        /// When we want to rotate based on the camera direction, we need to tweak the actor
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
            if (mMotionController._CameraTransform == null) { return; }

            float lToCameraAngle = Vector3Ext.HorizontalAngleTo(mMotionController._Transform.forward, mMotionController._CameraTransform.forward, mMotionController._Transform.up);
            if (!mLinkRotation && Mathf.Abs(lToCameraAngle) <= mDegreesPer60FPSTick * TimeManager.Relative60FPSDeltaTime) { mLinkRotation = true; }

            if (!mLinkRotation)
            {
                float lRotationAngle = Mathf.Abs(lToCameraAngle);
                float lRotationSign = Mathf.Sign(lToCameraAngle);
                lToCameraAngle = lRotationSign * Mathf.Min(mDegreesPer60FPSTick * TimeManager.Relative60FPSDeltaTime, lRotationAngle);
            }

            Quaternion lRotation = Quaternion.AngleAxis(lToCameraAngle, Vector3.up);
            mActorController.Yaw = mActorController.Yaw * lRotation;
            mActorController._Transform.rotation = mActorController.Tilt * mActorController.Yaw;
        }

        // **************************************************************************************************
        // Following properties and function only valid while editing
        // **************************************************************************************************

#if UNITY_EDITOR

        /// <summary>
        /// Allow the constraint to render it's own GUI
        /// </summary>
        /// <returns>Reports if the object's value was changed</returns>
        public override bool OnInspectorGUI()
        {
            bool lIsDirty = false;

            if (EditorHelper.BoolField("Default to Run", "Determines if the default is to run or walk.", DefaultToRun, mMotionController))
            {
                lIsDirty = true;
                DefaultToRun = EditorHelper.FieldBoolValue;
            }

            if (EditorHelper.TextField("Run Action Alias", "Action alias that triggers a run or walk (which ever is opposite the default).", ActionAlias, mMotionController))
            {
                lIsDirty = true;
                ActionAlias = EditorHelper.FieldStringValue;
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

            if (EditorHelper.BoolField("Rotate With Input", "Determines if we rotate based on user input.", RotateWithInput, mMotionController))
            {
                lIsDirty = true;
                RotateWithInput = EditorHelper.FieldBoolValue;
            }

            if (EditorHelper.BoolField("Rotate With Camera", "Determines if we rotate to match the camera.", RotateWithCamera, mMotionController))
            {
                lIsDirty = true;
                RotateWithCamera = EditorHelper.FieldBoolValue;
            }

            if (EditorHelper.FloatField("Rotation Speed", "Degrees per second to rotate the actor.", RotationSpeed, mMotionController))
            {
                lIsDirty = true;
                RotationSpeed = EditorHelper.FieldFloatValue;
            }

            GUILayout.Space(5f);

            if (EditorHelper.FloatField("Stop Delay", "Delay (in seconds) before we process a stop. This gives us time to test for a pivot.", StopDelay, mMotionController))
            {
                lIsDirty = true;
                StopDelay = EditorHelper.FieldFloatValue;
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
        public static int STATE_IdlePoseOut = -1;
        public static int STATE_MoveTree = -1;
        public static int TRANS_AnyState_MoveTree = -1;
        public static int TRANS_EntryState_MoveTree = -1;
        public static int TRANS_IdlePoseOut_MoveTree = -1;
        public static int TRANS_MoveTree_IdlePoseOut = -1;

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
                    if (lStateID == STATE_IdlePoseOut) { return true; }
                    if (lStateID == STATE_MoveTree) { return true; }
                }

                if (lTransitionID == TRANS_AnyState_MoveTree) { return true; }
                if (lTransitionID == TRANS_EntryState_MoveTree) { return true; }
                if (lTransitionID == TRANS_IdlePoseOut_MoveTree) { return true; }
                if (lTransitionID == TRANS_MoveTree_IdlePoseOut) { return true; }
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
            if (rStateID == STATE_IdlePoseOut) { return true; }
            if (rStateID == STATE_MoveTree) { return true; }
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
                if (rStateID == STATE_IdlePoseOut) { return true; }
                if (rStateID == STATE_MoveTree) { return true; }
            }

            if (rTransitionID == TRANS_AnyState_MoveTree) { return true; }
            if (rTransitionID == TRANS_EntryState_MoveTree) { return true; }
            if (rTransitionID == TRANS_IdlePoseOut_MoveTree) { return true; }
            if (rTransitionID == TRANS_MoveTree_IdlePoseOut) { return true; }
            return false;
        }

        /// <summary>
        /// Preprocess any animator data so the motion can use it later
        /// </summary>
        public override void LoadAnimatorData()
        {
            TRANS_AnyState_MoveTree = mMotionController.AddAnimatorName("AnyState -> Base Layer.PMP_WalkRunStrafe-SM.Move Tree");
            TRANS_EntryState_MoveTree = mMotionController.AddAnimatorName("Entry -> Base Layer.PMP_WalkRunStrafe-SM.Move Tree");
            STATE_Start = mMotionController.AddAnimatorName("Base Layer.Start");
            STATE_IdlePoseOut = mMotionController.AddAnimatorName("Base Layer.PMP_WalkRunStrafe-SM.IdlePose Out");
            TRANS_IdlePoseOut_MoveTree = mMotionController.AddAnimatorName("Base Layer.PMP_WalkRunStrafe-SM.IdlePose Out -> Base Layer.PMP_WalkRunStrafe-SM.Move Tree");
            STATE_MoveTree = mMotionController.AddAnimatorName("Base Layer.PMP_WalkRunStrafe-SM.Move Tree");
            TRANS_MoveTree_IdlePoseOut = mMotionController.AddAnimatorName("Base Layer.PMP_WalkRunStrafe-SM.Move Tree -> Base Layer.PMP_WalkRunStrafe-SM.IdlePose Out");
        }

#if UNITY_EDITOR

        private AnimationClip m16672 = null;
        private AnimationClip m19780 = null;
        private AnimationClip m17768 = null;
        private AnimationClip m23934 = null;
        private AnimationClip m25304 = null;
        private AnimationClip m15900 = null;
        private AnimationClip m16640 = null;
        private AnimationClip m15614 = null;
        private AnimationClip m24920 = null;
        private AnimationClip m16328 = null;

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

            UnityEditor.Animations.AnimatorStateMachine lSM_N2165318 = lRootSubStateMachine;
            if (lSM_N2165318 != null)
            {
                for (int i = lSM_N2165318.entryTransitions.Length - 1; i >= 0; i--)
                {
                    lSM_N2165318.RemoveEntryTransition(lSM_N2165318.entryTransitions[i]);
                }

                for (int i = lSM_N2165318.anyStateTransitions.Length - 1; i >= 0; i--)
                {
                    lSM_N2165318.RemoveAnyStateTransition(lSM_N2165318.anyStateTransitions[i]);
                }

                for (int i = lSM_N2165318.states.Length - 1; i >= 0; i--)
                {
                    lSM_N2165318.RemoveState(lSM_N2165318.states[i].state);
                }

                for (int i = lSM_N2165318.stateMachines.Length - 1; i >= 0; i--)
                {
                    lSM_N2165318.RemoveStateMachine(lSM_N2165318.stateMachines[i].stateMachine);
                }
            }
            else
            {
                lSM_N2165318 = lSM_38592.AddStateMachine(_EditorAnimatorSMName, new Vector3(420, 324, 0));
            }

            UnityEditor.Animations.AnimatorState lS_N2165320 = lSM_N2165318.AddState("IdlePose Out", new Vector3(600, 120, 0));
            lS_N2165320.speed = 1f;
            lS_N2165320.motion = m19780;

            UnityEditor.Animations.AnimatorState lS_N2165322 = lSM_N2165318.AddState("Move Tree", new Vector3(312, 120, 0));
            lS_N2165322.speed = 1f;

            UnityEditor.Animations.BlendTree lM_N2165324 = CreateBlendTree("Move Blend Tree", _EditorAnimatorController, mMotionLayer.AnimatorLayerIndex);
            lM_N2165324.blendType = UnityEditor.Animations.BlendTreeType.Simple1D;
            lM_N2165324.blendParameter = "InputMagnitude";
            lM_N2165324.blendParameterY = "InputX";
#if !(UNITY_4_0 || UNITY_4_0_1 || UNITY_4_1 || UNITY_4_2 || UNITY_4_3 || UNITY_4_4 || UNITY_4_5 || UNITY_4_6 || UNITY_5_0 || UNITY_5_1 || UNITY_5_2 || UNITY_5_3)
            lM_N2165324.useAutomaticThresholds = true;
#endif
            lM_N2165324.AddChild(m19780, 0f);

            UnityEditor.Animations.BlendTree lM_N2165328 = CreateBlendTree("WalkTree", _EditorAnimatorController, mMotionLayer.AnimatorLayerIndex);
            lM_N2165328.blendType = UnityEditor.Animations.BlendTreeType.SimpleDirectional2D;
            lM_N2165328.blendParameter = "InputX";
            lM_N2165328.blendParameterY = "InputY";
#if !(UNITY_4_0 || UNITY_4_0_1 || UNITY_4_1 || UNITY_4_2 || UNITY_4_3 || UNITY_4_4 || UNITY_4_5 || UNITY_4_6 || UNITY_5_0 || UNITY_5_1 || UNITY_5_2 || UNITY_5_3)
            lM_N2165328.useAutomaticThresholds = true;
#endif
            lM_N2165328.AddChild(m17768, new Vector2(0f, 0.35f));
            UnityEditor.Animations.ChildMotion[] lM_N2165328_0_Children = lM_N2165328.children;
            lM_N2165328_0_Children[lM_N2165328_0_Children.Length - 1].mirror = false;
            lM_N2165328_0_Children[lM_N2165328_0_Children.Length - 1].timeScale = 1f;
            lM_N2165328.children = lM_N2165328_0_Children;

            lM_N2165328.AddChild(m23934, new Vector2(-0.35f, 0f));
            UnityEditor.Animations.ChildMotion[] lM_N2165328_1_Children = lM_N2165328.children;
            lM_N2165328_1_Children[lM_N2165328_1_Children.Length - 1].mirror = false;
            lM_N2165328_1_Children[lM_N2165328_1_Children.Length - 1].timeScale = 1f;
            lM_N2165328.children = lM_N2165328_1_Children;

            lM_N2165328.AddChild(m25304, new Vector2(0.35f, 0f));
            UnityEditor.Animations.ChildMotion[] lM_N2165328_2_Children = lM_N2165328.children;
            lM_N2165328_2_Children[lM_N2165328_2_Children.Length - 1].mirror = false;
            lM_N2165328_2_Children[lM_N2165328_2_Children.Length - 1].timeScale = 1f;
            lM_N2165328.children = lM_N2165328_2_Children;

            lM_N2165328.AddChild(m15900, new Vector2(0f, -0.35f));
            UnityEditor.Animations.ChildMotion[] lM_N2165328_3_Children = lM_N2165328.children;
            lM_N2165328_3_Children[lM_N2165328_3_Children.Length - 1].mirror = false;
            lM_N2165328_3_Children[lM_N2165328_3_Children.Length - 1].timeScale = 1f;
            lM_N2165328.children = lM_N2165328_3_Children;

            lM_N2165324.AddChild(lM_N2165328, 0.5f);

            UnityEditor.Animations.BlendTree lM_N2165332 = CreateBlendTree("RunTree", _EditorAnimatorController, mMotionLayer.AnimatorLayerIndex);
            lM_N2165332.blendType = UnityEditor.Animations.BlendTreeType.SimpleDirectional2D;
            lM_N2165332.blendParameter = "InputX";
            lM_N2165332.blendParameterY = "InputY";
#if !(UNITY_4_0 || UNITY_4_0_1 || UNITY_4_1 || UNITY_4_2 || UNITY_4_3 || UNITY_4_4 || UNITY_4_5 || UNITY_4_6 || UNITY_5_0 || UNITY_5_1 || UNITY_5_2 || UNITY_5_3)
            lM_N2165332.useAutomaticThresholds = true;
#endif
            lM_N2165332.AddChild(m16640, new Vector2(0f, 0.7f));
            UnityEditor.Animations.ChildMotion[] lM_N2165332_0_Children = lM_N2165332.children;
            lM_N2165332_0_Children[lM_N2165332_0_Children.Length - 1].mirror = false;
            lM_N2165332_0_Children[lM_N2165332_0_Children.Length - 1].timeScale = 1f;
            lM_N2165332.children = lM_N2165332_0_Children;

            lM_N2165332.AddChild(m15614, new Vector2(-0.7f, 0f));
            UnityEditor.Animations.ChildMotion[] lM_N2165332_1_Children = lM_N2165332.children;
            lM_N2165332_1_Children[lM_N2165332_1_Children.Length - 1].mirror = false;
            lM_N2165332_1_Children[lM_N2165332_1_Children.Length - 1].timeScale = 1f;
            lM_N2165332.children = lM_N2165332_1_Children;

            lM_N2165332.AddChild(m24920, new Vector2(0.7f, 0f));
            UnityEditor.Animations.ChildMotion[] lM_N2165332_2_Children = lM_N2165332.children;
            lM_N2165332_2_Children[lM_N2165332_2_Children.Length - 1].mirror = false;
            lM_N2165332_2_Children[lM_N2165332_2_Children.Length - 1].timeScale = 1f;
            lM_N2165332.children = lM_N2165332_2_Children;

            lM_N2165332.AddChild(m16328, new Vector2(0f, -0.7f));
            UnityEditor.Animations.ChildMotion[] lM_N2165332_3_Children = lM_N2165332.children;
            lM_N2165332_3_Children[lM_N2165332_3_Children.Length - 1].mirror = false;
            lM_N2165332_3_Children[lM_N2165332_3_Children.Length - 1].timeScale = 1f;
            lM_N2165332.children = lM_N2165332_3_Children;

            lM_N2165324.AddChild(lM_N2165332, 1f);
            lS_N2165322.motion = lM_N2165324;

            // Create the transition from the any state. Note that 'AnyState' transitions have to be added to the root
            UnityEditor.Animations.AnimatorStateTransition lT_N2165336 = lRootStateMachine.AddAnyStateTransition(lS_N2165322);
            lT_N2165336.hasExitTime = false;
            lT_N2165336.hasFixedDuration = true;
            lT_N2165336.exitTime = 0.9f;
            lT_N2165336.duration = 0.4f;
            lT_N2165336.offset = 0f;
            lT_N2165336.mute = false;
            lT_N2165336.solo = false;
            lT_N2165336.canTransitionToSelf = true;
            lT_N2165336.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            lT_N2165336.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 32210f, "L0MotionPhase");

            UnityEditor.Animations.AnimatorStateTransition lT_N2165338 = lS_N2165320.AddTransition(lS_N2165322);
            lT_N2165338.hasExitTime = false;
            lT_N2165338.hasFixedDuration = true;
            lT_N2165338.exitTime = 0f;
            lT_N2165338.duration = 0.25f;
            lT_N2165338.offset = 0f;
            lT_N2165338.mute = false;
            lT_N2165338.solo = false;
            lT_N2165338.canTransitionToSelf = true;
            lT_N2165338.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            lT_N2165338.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Greater, 0.1f, "InputMagnitude");

            UnityEditor.Animations.AnimatorStateTransition lT_N2165340 = lS_N2165322.AddTransition(lS_N2165320);
            lT_N2165340.hasExitTime = false;
            lT_N2165340.hasFixedDuration = true;
            lT_N2165340.exitTime = 1f;
            lT_N2165340.duration = 0.25f;
            lT_N2165340.offset = 0f;
            lT_N2165340.mute = false;
            lT_N2165340.solo = false;
            lT_N2165340.canTransitionToSelf = true;
            lT_N2165340.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            lT_N2165340.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 32215f, "L0MotionPhase");
            lT_N2165340.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 0f, "L0MotionParameter");

        }

        /// <summary>
        /// Gathers the animations so we can use them when creating the sub-state machine.
        /// </summary>
        public override void FindAnimations()
        {
            m16672 = FindAnimationClip("Assets/ootii/MotionController/Content/Animations/Humanoid/Idling/unity_Idle_IdleToIdlesR.fbx/IdlePose.anim", "IdlePose");
            m19780 = FindAnimationClip("Assets/ootii/MotionControllerPacks/SpellCasting/Content/Animations/Mixamo/Y_Bot@standing_idle.fbx/PMP_IdlePose.anim", "PMP_IdlePose");
            m17768 = FindAnimationClip("Assets/ootii/MotionControllerPacks/SpellCasting/Content/Animations/Mixamo/Y_Bot@Standing_Walk_Forward.fbx/Standing_Walk_Forward.anim", "Standing_Walk_Forward");
            m23934 = FindAnimationClip("Assets/ootii/MotionControllerPacks/SpellCasting/Content/Animations/Mixamo/Y_Bot@Standing_Walk_Left.fbx/Standing_Walk_Left.anim", "Standing_Walk_Left");
            m25304 = FindAnimationClip("Assets/ootii/MotionControllerPacks/SpellCasting/Content/Animations/Mixamo/Y_Bot@Standing_Walk_Right.fbx/Standing_Walk_Right.anim", "Standing_Walk_Right");
            m15900 = FindAnimationClip("Assets/ootii/MotionControllerPacks/SpellCasting/Content/Animations/Mixamo/Y_Bot@Standing_Walk_Back.fbx/Standing_Walk_Back.anim", "Standing_Walk_Back");
            m16640 = FindAnimationClip("Assets/ootii/MotionControllerPacks/SpellCasting/Content/Animations/Mixamo/Y_Bot@Standing_Run_Forward.fbx/Standing_Run_Forward.anim", "Standing_Run_Forward");
            m15614 = FindAnimationClip("Assets/ootii/MotionControllerPacks/SpellCasting/Content/Animations/Mixamo/Y_Bot@Standing_Run_Left.fbx/Standing_Run_Left.anim", "Standing_Run_Left");
            m24920 = FindAnimationClip("Assets/ootii/MotionControllerPacks/SpellCasting/Content/Animations/Mixamo/Y_Bot@Standing_Run_Right.fbx/Standing_Run_Right.anim", "Standing_Run_Right");
            m16328 = FindAnimationClip("Assets/ootii/MotionControllerPacks/SpellCasting/Content/Animations/Mixamo/Y_Bot@Standing_Run_Back.fbx/Standing_Run_Back.anim", "Standing_Run_Back");

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
            m19780 = CreateAnimationField("IdlePose Out.PMP_IdlePose", "Assets/ootii/MotionControllerPacks/SpellCasting/Content/Animations/Mixamo/Y_Bot@standing_idle.fbx/PMP_IdlePose.anim", "PMP_IdlePose", m19780);
            m17768 = CreateAnimationField("Move Tree.Standing_Walk_Forward", "Assets/ootii/MotionControllerPacks/SpellCasting/Content/Animations/Mixamo/Y_Bot@Standing_Walk_Forward.fbx/Standing_Walk_Forward.anim", "Standing_Walk_Forward", m17768);
            m23934 = CreateAnimationField("Move Tree.Standing_Walk_Left", "Assets/ootii/MotionControllerPacks/SpellCasting/Content/Animations/Mixamo/Y_Bot@Standing_Walk_Left.fbx/Standing_Walk_Left.anim", "Standing_Walk_Left", m23934);
            m25304 = CreateAnimationField("Move Tree.Standing_Walk_Right", "Assets/ootii/MotionControllerPacks/SpellCasting/Content/Animations/Mixamo/Y_Bot@Standing_Walk_Right.fbx/Standing_Walk_Right.anim", "Standing_Walk_Right", m25304);
            m15900 = CreateAnimationField("Move Tree.Standing_Walk_Back", "Assets/ootii/MotionControllerPacks/SpellCasting/Content/Animations/Mixamo/Y_Bot@Standing_Walk_Back.fbx/Standing_Walk_Back.anim", "Standing_Walk_Back", m15900);
            m16640 = CreateAnimationField("Move Tree.Standing_Run_Forward", "Assets/ootii/MotionControllerPacks/SpellCasting/Content/Animations/Mixamo/Y_Bot@Standing_Run_Forward.fbx/Standing_Run_Forward.anim", "Standing_Run_Forward", m16640);
            m15614 = CreateAnimationField("Move Tree.Standing_Run_Left", "Assets/ootii/MotionControllerPacks/SpellCasting/Content/Animations/Mixamo/Y_Bot@Standing_Run_Left.fbx/Standing_Run_Left.anim", "Standing_Run_Left", m15614);
            m24920 = CreateAnimationField("Move Tree.Standing_Run_Right", "Assets/ootii/MotionControllerPacks/SpellCasting/Content/Animations/Mixamo/Y_Bot@Standing_Run_Right.fbx/Standing_Run_Right.anim", "Standing_Run_Right", m24920);
            m16328 = CreateAnimationField("Move Tree.Standing_Run_Back", "Assets/ootii/MotionControllerPacks/SpellCasting/Content/Animations/Mixamo/Y_Bot@Standing_Run_Back.fbx/Standing_Run_Back.anim", "Standing_Run_Back", m16328);

            // Add the remaining functionality
            base.OnSettingsGUI();
        }

#endif

        // ************************************ END AUTO GENERATED ************************************
        #endregion
    }
}

