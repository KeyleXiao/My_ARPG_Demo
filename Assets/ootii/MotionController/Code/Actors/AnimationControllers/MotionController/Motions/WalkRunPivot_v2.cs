using UnityEngine;
using com.ootii.Cameras;
using com.ootii.Geometry;
using com.ootii.Helpers;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace com.ootii.Actors.AnimationControllers
{
    /// <summary>
    /// </summary>
    [MotionName("Walk Run Pivot")]
    [MotionDescription("Standard movement (walk/run) for an adventure game.")]
    public class WalkRunPivot_v2 : MotionControllerMotion, IWalkRunMotion
    {
        /// <summary>
        /// Trigger values for th emotion
        /// </summary>
        public const int PHASE_UNKNOWN = 0;
        public const int PHASE_START = 27130;
        public const int PHASE_END_RUN = 27131;
        public const int PHASE_END_WALK = 27132;
        public const int PHASE_RESUME = 27133;

        public const int PHASE_START_IDLE_PIVOT = 27135;

        /// <summary>
        /// Optional "Form" or "Style" value to test to see if this motion should activate.
        /// </summary>
        public int _FormCondition = 0;
        public int FormCondition
        {
            get { return _FormCondition; }
            set { _FormCondition = value; }
        }

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
        /// Speed (units per second) when walking
        /// </summary>
        public float _WalkSpeed = 0f;
        public virtual float WalkSpeed
        {
            get { return _WalkSpeed; }
            set { _WalkSpeed = value; }
        }

        /// <summary>
        /// Speed (units per second) when running
        /// </summary>
        public float _RunSpeed = 0f;
        public virtual float RunSpeed
        {
            get { return _RunSpeed; }
            set { _RunSpeed = value; }
        }

        /// <summary>
        /// Determines if we rotate to match the camera
        /// </summary>
        public bool _RotateWithCamera = true;
        public bool RotateWithCamera
        {
            get { return _RotateWithCamera; }
            set { _RotateWithCamera = value; }
        }

        /// <summary>
        /// User layer id set for objects that are climbable.
        /// </summary>
        public string _RotateActionAlias = "ActivateRotation";
        public string RotateActionAlias
        {
            get { return _RotateActionAlias; }
            set { _RotateActionAlias = value; }
        }

        /// <summary>
        /// Degrees per second to rotate the actor in order to face the input direction
        /// </summary>
        public float _RotationSpeed = 180f;
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
        /// Determines if we'll use the start transitions when starting from idle
        /// </summary>
        public bool _UseStartTransitions = true;
        public bool UseStartTransitions
        {
            get { return _UseStartTransitions; }
            set { _UseStartTransitions = value; }
        }

        /// <summary>
        /// Determines if we'll use the start transitions when stopping movement
        /// </summary>
        public bool _UseStopTransitions = true;
        public bool UseStopTransitions
        {
            get { return _UseStopTransitions; }
            set { _UseStopTransitions = value; }
        }

        /// <summary>
        /// Determines if the character can pivot while idle
        /// </summary>
        public bool _UseTapToPivot = false;
        public bool UseTapToPivot
        {
            get { return _UseTapToPivot; }
            set { _UseTapToPivot = value; }
        }

        /// <summary>
        /// Determines how long we wait before testing for an idle pivot
        /// </summary>
        public float _TapToPivotDelay = 0.2f;
        public float TapToPivotDelay
        {
            get { return _TapToPivotDelay; }
            set { _TapToPivotDelay = value; }
        }

        /// <summary>
        /// Minimum angle before we use the pivot speed
        /// </summary>
        public float _MinPivotAngle = 20f;
        public float MinPivotAngle
        {
            get { return _MinPivotAngle; }
            set { _MinPivotAngle = value; }
        }

        /// <summary>
        /// Number of samples to use for smoothing
        /// </summary>
        public int _SmoothingSamples = 10;
        public int SmoothingSamples
        {
            get { return _SmoothingSamples; }

            set
            {
                _SmoothingSamples = value;

                mInputX.SampleCount = _SmoothingSamples;
                mInputY.SampleCount = _SmoothingSamples;
                mInputMagnitude.SampleCount = _SmoothingSamples;
            }
        }
        
        /// <summary>
        /// Determines if the actor should be running based on input
        /// </summary>
        public virtual bool IsRunActive
        {
            get
            {
                if (mMotionController.TargetNormalizedSpeed > 0f && mMotionController.TargetNormalizedSpeed <= 0.5f) { return false; }
                if (mMotionController._InputSource == null) { return _DefaultToRun; }
                return ((_DefaultToRun && !mMotionController._InputSource.IsPressed(_ActionAlias)) || (!_DefaultToRun && mMotionController._InputSource.IsPressed(_ActionAlias)));
            }
        }

        /// <summary>
        /// Determine if we're pivoting from an idle
        /// </summary>
        protected bool mStartInPivot = false;

        /// <summary>
        /// Angle of the input from when the motion was activated
        /// </summary>
        protected Vector3 mSavedInputForward = Vector3.zero;

        /// <summary>
        /// Time that has elapsed since there was no input
        /// </summary>
        protected float mNoInputElapsed = 0f;

        /// <summary>
        /// Phase ID we're using to transition out
        /// </summary>
        protected int mExitPhaseID = 0;

        /// <summary>
        /// Frame level rotation test
        /// </summary>
        protected bool mRotateWithCamera = false;

        /// <summary>
        /// Determines if the actor rotation should be linked to the camera
        /// </summary>
        protected bool mLinkRotation = false;

        /// <summary>
        /// We use these classes to help smooth the input values so that
        /// movement doesn't drop from 1 to 0 immediately.
        /// </summary>
        protected FloatValue mInputX = new FloatValue(0f, 10);
        protected FloatValue mInputY = new FloatValue(0f, 10);
        protected FloatValue mInputMagnitude = new FloatValue(0f, 15);

        /// <summary>
        /// Last time we had input activity
        /// </summary>
        protected float mLastTapStartTime = 0f;
        protected float mLastTapInputFromAvatarAngle = 0f;
        protected Vector3 mLastTapInputForward = Vector3.zero;

        /// <summary>
        /// Default constructor
        /// </summary>
        public WalkRunPivot_v2()
            : base()
        {
            _Category = EnumMotionCategories.WALK;

            _Priority = 5;
            _ActionAlias = "Run";

#if UNITY_EDITOR
            if (_EditorAnimatorSMName.Length == 0) { _EditorAnimatorSMName = "WalkRunPivot v2-SM"; }
#endif
        }

        /// <summary>
        /// Controller constructor
        /// </summary>
        /// <param name="rController">Controller the motion belongs to</param>
        public WalkRunPivot_v2(MotionController rController)
            : base(rController)
        {
            _Category = EnumMotionCategories.WALK;

            _Priority = 5;
            _ActionAlias = "Run";

#if UNITY_EDITOR
            if (_EditorAnimatorSMName.Length == 0) { _EditorAnimatorSMName = "WalkRunPivot v2-SM"; }
#endif
        }

        /// <summary>
        /// Awake is called after all objects are initialized so you can safely speak to other objects. This is where
        /// reference can be associated.
        /// </summary>
        public override void Awake()
        {
            base.Awake();

            // Initialize the smoothing variables
            SmoothingSamples = _SmoothingSamples;
        }

        /// <summary>
        /// Tests if this motion should be started. However, the motion
        /// isn't actually started.
        /// </summary>
        /// <returns></returns>
        public override bool TestActivate()
        {
            if (!mIsStartable ||
                !mMotionController.IsGrounded ||
                mMotionController.Stance != EnumControllerStance.TRAVERSAL)
            {
                mStartInPivot = false;
                mLastTapStartTime = 0f;
                return false;
            }

            // Test to see if the form condition matches our current default form
            if (_FormCondition >= 0 && mMotionController.CurrentForm != _FormCondition)
            {
                return false;
            }

            bool lIsPivotable = (_UseTapToPivot && (mLastTapStartTime > 0f || Mathf.Abs(mMotionController.State.InputFromAvatarAngle) > _MinPivotAngle));

            bool lIsIdling = (_UseTapToPivot && mMotionLayer.ActiveMotion != null && mMotionLayer.ActiveMotion.Category == EnumMotionCategories.IDLE);

            // Determine if tapping is enabled
            if (_UseTapToPivot && lIsPivotable && lIsIdling)
            {
                // If there's input, it could be the start of a tap or true movement
                if (mMotionController.State.InputMagnitudeTrend.Value > 0.1f)
                {
                    // Start the timer
                    if (mLastTapStartTime == 0f)
                    {
                        mLastTapStartTime = Time.time;
                        mLastTapInputForward = mMotionController.State.InputForward;
                        mLastTapInputFromAvatarAngle = mMotionController.State.InputFromAvatarAngle;
                        return true;
                    }
                    // Timer has expired. So, we must really be moving
                    else if (mLastTapStartTime + _TapToPivotDelay <= Time.time)
                    {
                        mStartInPivot = false;
                        mLastTapStartTime = 0f;
                        return true;
                    }

                    // Keep waiting
                    return false;
                }
                // No input. So, at the end of a tap or there really is nothing
                else
                {
                    if (mLastTapStartTime > 0f)
                    {
                        mStartInPivot = true;
                        mLastTapStartTime = 0f;
                        return true;
                    }
                }
            }
            // If not, we do normal processing
            else
            {
                mStartInPivot = false;
                mLastTapStartTime = 0f;

                // If there's enough movement, start the motion
                if (mMotionController.State.InputMagnitudeTrend.Value > 0.49f)
                {
                    return true;
                }
            }

            // Don't activate
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
            if (mLastTapStartTime > 0f) { return true; }
            if (!mMotionController.IsGrounded) { return false; }

            // Our idle pose is a good exit
            if (mMotionLayer._AnimatorStateID == STATE_IdlePose)
            {
                return false;
            }

            // Our exit pose for the idle pivots
            if (mMotionLayer._AnimatorStateID == STATE_IdleTurnEndPose)
            {
                if (mMotionController.State.InputMagnitudeTrend.Value < 0.1f)
                {
                    return false;
                }
            }

            // One last check to make sure we're in this state
            if (mIsAnimatorActive && !IsInMotionState)
            {
                return false;
            }

            // If we no longer have input and we're in normal movement, we can stop
            if (mMotionController.State.InputMagnitudeTrend.Average < 0.1f)
            {
                if (mMotionLayer._AnimatorStateID == STATE_MoveTree && mMotionLayer._AnimatorTransitionID == 0)
                {
                    return false;
                }
            }

            // Stay
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
            if (mLastTapStartTime == 0f) { DelayedActivate(); }

            return base.Activate(rPrevMotion);
        }

        /// <summary>
        /// Called to start the specific motion. If the motion
        /// were something like 'jump', this would start the jumping process
        /// </summary>
        /// <param name="rPrevMotion">Motion that this motion is taking over from</param>
        public void DelayedActivate()
        {
            mExitPhaseID = 0;
            mSavedInputForward = mMotionController.State.InputForward;

            // Update the max speed based on our animation
            mMotionController.MaxSpeed = 5.668f;

            // Determine how we start
            if (mStartInPivot)
            {
                mMotionController.State.InputFromAvatarAngle = mLastTapInputFromAvatarAngle;
                mMotionController.SetAnimatorMotionPhase(mMotionLayer.AnimatorLayerIndex, PHASE_START_IDLE_PIVOT, 0, true);
            }
            else if (mStartInMove)
            {
                mStartInMove = false;
                mMotionController.SetAnimatorMotionPhase(mMotionLayer.AnimatorLayerIndex, PHASE_START, 1, true);
            }
            else if (mMotionController._InputSource == null)
            {
                mMotionController.SetAnimatorMotionPhase(mMotionLayer.AnimatorLayerIndex, PHASE_START, (_UseStartTransitions ? 0 : 1), true);
            }
            else
            {
                // Grab the state info
                MotionState lState = mMotionController.State;

                // Convert the input to radial so we deal with keyboard and gamepad input the same.
                float lInputX = lState.InputX;
                float lInputY = lState.InputY;
                float lInputMagnitude = lState.InputMagnitudeTrend.Value;
                InputManagerHelper.ConvertToRadialInput(ref lInputX, ref lInputY, ref lInputMagnitude, (IsRunActive ? 1f : 0.5f));

                // Smooth the input
                if (lInputX != 0f || lInputY < 0f)
                {
                    mInputX.Clear(lInputX);
                    mInputY.Clear(lInputY);
                    mInputMagnitude.Clear(lInputMagnitude);
                }

                // Start the motion
                mMotionController.SetAnimatorMotionPhase(mMotionLayer.AnimatorLayerIndex, PHASE_START, (_UseStartTransitions ? 0 : 1), true);
            }

            // Register this motion with the camera
            if (_RotateWithCamera && mMotionController.CameraRig is BaseCameraRig)
            {
                ((BaseCameraRig)mMotionController.CameraRig).OnPostLateUpdate -= OnCameraUpdated;
                ((BaseCameraRig)mMotionController.CameraRig).OnPostLateUpdate += OnCameraUpdated;
            }
        }

        /// <summary>
        /// Raised when we shut the motion down
        /// </summary>
        public override void Deactivate()
        {
            mLastTapStartTime = 0f;
            mLastTapInputFromAvatarAngle = 0f;

            // Clear out the start
            mStartInPivot = false;
            mStartInRun = false;
            mStartInWalk = false;

            // Register this motion with the camera
            if (mMotionController.CameraRig is BaseCameraRig)
            {
                ((BaseCameraRig)mMotionController.CameraRig).OnPostLateUpdate -= OnCameraUpdated;
            }

            base.Deactivate();
        }

        /// <summary>
        /// Allows the motion to modify the velocity before it is applied. 
        /// 
        /// NOTE:
        /// Be careful when removing rotations
        /// as some transitions will want rotations even if the state they are transitioning from don't.
        /// </summary>
        /// <param name="rDeltaTime">Time since the last frame (or fixed update call)</param>
        /// <param name="rUpdateIndex">Index of the update to help manage dynamic/fixed updates. [0: Invalid update, >=1: Valid update]</param>
        /// <param name="rMovement">Amount of movement caused by root motion this frame</param>
        /// <param name="rRotation">Amount of rotation caused by root motion this frame</param>
        /// <returns></returns>
        public override void UpdateRootMotion(float rDeltaTime, int rUpdateIndex, ref Vector3 rMovement, ref Quaternion rRotation)
        {
            if (mLastTapStartTime > 0f) { return; }

            if (mMotionLayer._AnimatorTransitionID == TRANS_EntryState_MoveTree)
            {
                rRotation = Quaternion.identity;

                rMovement = rMovement.normalized * (mActorController.PrevState.Velocity.magnitude * (Time.smoothDeltaTime > 0f ? Time.smoothDeltaTime : Time.deltaTime));

                rMovement.x = 0f;
                rMovement.y = 0f;
                if (rMovement.z < 0f) { rMovement.z = 0f; }
            }
            else if (mMotionLayer._AnimatorStateID == STATE_MoveTree && mMotionLayer._AnimatorTransitionID == 0)
            {
                rRotation = Quaternion.identity;

                // Override root motion if we're meant to
                float lMovementSpeed = (IsRunActive ? _RunSpeed : _WalkSpeed);
                if (lMovementSpeed > 0f)
                {
                    if (rMovement.sqrMagnitude > 0f)
                    {
                        rMovement = rMovement.normalized * (lMovementSpeed * rDeltaTime);
                    }
                    else
                    {
                        Vector3 lDirection = new Vector3(0f, 0f, 1f);
                        rMovement = lDirection.normalized * (lMovementSpeed * rDeltaTime);
                    }
                }

                rMovement.x = 0f;
                rMovement.y = 0f;
                if (rMovement.z < 0f) { rMovement.z = 0f; }
            }
            else
            {
                if (_UseTapToPivot && IsIdlePivoting())
                {
                    rMovement = Vector3.zero;
                }
                // If we're stopping, add some lag
                else if (IsStopping())
                {
                    rMovement = rMovement * 0.5f;
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
            mMovement = Vector3.zero;
            mRotation = Quaternion.identity;

            if (mLastTapStartTime > 0f)
            {
                UpdateDelayedActivation(rDeltaTime, rUpdateIndex);
            }
            else if (_UseTapToPivot && IsIdlePivoting())
            {
                UpdateIdlePivot(rDeltaTime, rUpdateIndex);
            }
            else
            {
                UpdateMovement(rDeltaTime, rUpdateIndex);
            }
        }

        /// <summary>
        /// Update used when we're delaying activation for possible pivot
        /// </summary>
        private void UpdateDelayedActivation(float rDeltaTime, int rUpdateIndex)
        {
            if (mMotionController.State.InputMagnitudeTrend.Value < 0.1f)
            {
                mStartInPivot = true;
                mLastTapStartTime = 0f;

                DelayedActivate();
            }
            else if (mLastTapStartTime + _TapToPivotDelay < Time.time)
            {
                mStartInPivot = false;
                mLastTapStartTime = 0f;

                DelayedActivate();
            }

            // Update smoothing
            MotionState lState = mMotionController.State;

            // Convert the input to radial so we deal with keyboard and gamepad input the same.
            float lInputMax = (IsRunActive ? 1f : 0.5f);
            float lInputX = Mathf.Clamp(lState.InputX, -lInputMax, lInputMax);
            float lInputY = Mathf.Clamp(lState.InputY, -lInputMax, lInputMax);
            float lInputMagnitude = Mathf.Clamp(lState.InputMagnitudeTrend.Value, 0f, lInputMax);
            InputManagerHelper.ConvertToRadialInput(ref lInputX, ref lInputY, ref lInputMagnitude);

            // Smooth the input
            mInputX.Add(lInputX);
            mInputY.Add(lInputY);
            mInputMagnitude.Add(lInputMagnitude);

            // Modify the input values to add some lag
            mMotionController.State.InputX = mInputX.Average;
            mMotionController.State.InputY = mInputY.Average;
            mMotionController.State.InputMagnitudeTrend.Replace(mInputMagnitude.Average);
        }

        /// <summary>
        /// Update processing for the idle pivot
        /// </summary>
        /// <param name="rDeltaTime"></param>
        /// <param name="rUpdateIndex"></param>
        private void UpdateIdlePivot(float rDeltaTime, int rUpdateIndex)
        {
            int lStateID = mMotionLayer._AnimatorStateID;
            if (lStateID == STATE_IdleTurn180L ||
                lStateID == STATE_IdleTurn90L ||
                lStateID == STATE_IdleTurn20L ||
                lStateID == STATE_IdleTurn20R ||
                lStateID == STATE_IdleTurn90R ||
                lStateID == STATE_IdleTurn180R)
            {
                if (mMotionLayer._AnimatorTransitionID != 0 && mLastTapInputForward.sqrMagnitude > 0f)
                {
                    if (mMotionController._CameraTransform != null)
                    {
                        Vector3 lInputForward = mMotionController._CameraTransform.rotation * mLastTapInputForward;

                        float lAngle = Vector3Ext.HorizontalAngleTo(mMotionController._Transform.forward, lInputForward, mMotionController._Transform.up);
                        mRotation = Quaternion.Euler(0f, lAngle * mMotionLayer._AnimatorTransitionNormalizedTime, 0f);
                    }
                }
            }
        }

        /// <summary>
        /// Update processing for moving
        /// </summary>
        /// <param name="rDeltaTime"></param>
        /// <param name="rUpdateIndex"></param>
        private void UpdateMovement(float rDeltaTime, int rUpdateIndex)
        {
            bool lUpdateSamples = true;

            // Store the last valid input we had
            if (mMotionController.State.InputMagnitudeTrend.Value > 0.4f)
            {
                mExitPhaseID = 0;
                mNoInputElapsed = 0f;
                mSavedInputForward = mMotionController.State.InputForward;

                // If we were stopping, allow us to resume without leaving the motion
                if (IsStopping())
                {
                    mMotionController.SetAnimatorMotionPhase(mMotionLayer._AnimatorLayerIndex, PHASE_RESUME, 0, true);
                }
            }
            else
            {
                mNoInputElapsed = mNoInputElapsed + rDeltaTime;

                if (_UseStopTransitions)
                {
                    lUpdateSamples = false;

                    // If we've passed the delay, we really are stopping
                    if (mNoInputElapsed > 0.2f)
                    {
                        // Determine how we'll stop
                        if (mExitPhaseID == 0)
                        {
                            mExitPhaseID = (mInputMagnitude.Average < 0.6f ? PHASE_END_WALK : PHASE_END_RUN);
                        }

                        // Ensure we actually stop that way
                        if (mExitPhaseID != 0 && mMotionLayer._AnimatorStateID == STATE_MoveTree && mMotionLayer._AnimatorTransitionID == 0)
                        {
                            mMotionController.SetAnimatorMotionPhase(mMotionLayer._AnimatorLayerIndex, mExitPhaseID, 0, true);
                        }
                    }
                }
            }

            // If we need to update the samples... 
            if (lUpdateSamples)
            {
                MotionState lState = mMotionController.State;

                // Convert the input to radial so we deal with keyboard and gamepad input the same.
                float lInputMax = (IsRunActive ? 1f : 0.5f);

                float lInputX = Mathf.Clamp(lState.InputX, -lInputMax, lInputMax);
                float lInputY = Mathf.Clamp(lState.InputY, -lInputMax, lInputMax);
                float lInputMagnitude = Mathf.Clamp(lState.InputMagnitudeTrend.Value, 0f, lInputMax);
                InputManagerHelper.ConvertToRadialInput(ref lInputX, ref lInputY, ref lInputMagnitude);

                // Smooth the input
                mInputX.Add(lInputX);
                mInputY.Add(lInputY);
                mInputMagnitude.Add(lInputMagnitude);
            }

            // Modify the input values to add some lag
            mMotionController.State.InputX = mInputX.Average;
            mMotionController.State.InputY = mInputY.Average;
            mMotionController.State.InputMagnitudeTrend.Replace(mInputMagnitude.Average);

            // We may want to rotate with the camera if we're facing forward
            mRotateWithCamera = false;
            if (_RotateWithCamera && mMotionController._CameraTransform != null)
            {
                float lToCameraAngle = Vector3Ext.HorizontalAngleTo(mMotionController._Transform.forward, mMotionController._CameraTransform.forward, mMotionController._Transform.up);
                mRotateWithCamera = (Mathf.Abs(lToCameraAngle) < _RotationSpeed * rDeltaTime);

                if (mRotateWithCamera && mMotionLayer._AnimatorStateID != STATE_MoveTree) { mRotateWithCamera = false; }
                if (mRotateWithCamera && mMotionLayer._AnimatorTransitionID != 0) { mRotateWithCamera = false; }
                if (mRotateWithCamera && (Mathf.Abs(mMotionController.State.InputX) > 0.05f || mMotionController.State.InputY <= 0f)) { mRotateWithCamera = false; }
                if (mRotateWithCamera && (_RotateActionAlias.Length > 0 && !mMotionController._InputSource.IsPressed(_RotateActionAlias))) { mRotateWithCamera = false; }
            }

            // If we're meant to rotate with the camera (and OnCameraUpdate isn't already attached), do it here
            if (_RotateWithCamera && !(mMotionController.CameraRig is BaseCameraRig))
            {
                OnCameraUpdated(rDeltaTime, rUpdateIndex, null);
            }

            // We only allow input rotation under certain circumstances
            if (mMotionLayer._AnimatorTransitionID == TRANS_EntryState_MoveTree ||
                (mMotionLayer._AnimatorStateID == STATE_MoveTree && mMotionLayer._AnimatorTransitionID == 0) ||

                (mMotionLayer._AnimatorStateID == STATE_IdleToWalk180L && mMotionLayer._AnimatorStateNormalizedTime > 0.7f) ||
                (mMotionLayer._AnimatorStateID == STATE_IdleToWalk90L && mMotionLayer._AnimatorStateNormalizedTime > 0.6f) ||
                (mMotionLayer._AnimatorStateID == STATE_IdleToWalk90R && mMotionLayer._AnimatorStateNormalizedTime > 0.6f) ||
                (mMotionLayer._AnimatorStateID == STATE_IdleToWalk180R && mMotionLayer._AnimatorStateNormalizedTime > 0.7f) ||

                (mMotionLayer._AnimatorStateID == STATE_IdleToRun180L && mMotionLayer._AnimatorStateNormalizedTime > 0.6f) ||
                (mMotionLayer._AnimatorStateID == STATE_IdleToRun90L && mMotionLayer._AnimatorStateNormalizedTime > 0.6f) ||
                (mMotionLayer._AnimatorStateID == STATE_IdleToRun) ||
                (mMotionLayer._AnimatorStateID == STATE_IdleToRun90R && mMotionLayer._AnimatorStateNormalizedTime > 0.6f) ||
                (mMotionLayer._AnimatorStateID == STATE_IdleToRun180R && mMotionLayer._AnimatorStateNormalizedTime > 0.6f)
                )
            {
                // Since we're not rotating with the camera, rotate with input
                if (!mRotateWithCamera)
                {
                    if (mMotionController._CameraTransform != null && mMotionController.State.InputForward.sqrMagnitude == 0f)
                    {
                        RotateToInput(mMotionController._CameraTransform.rotation * mSavedInputForward, rDeltaTime, ref mRotation);
                    }
                    else
                    {
                        RotateToInput(mMotionController.State.InputFromAvatarAngle, rDeltaTime, ref mRotation);
                    }
                }
            }
        }

        /// <summary>
        /// Create a rotation velocity that rotates the character based on input
        /// </summary>
        /// <param name="rInputForward"></param>
        /// <param name="rDeltaTime"></param>
        private void RotateToInput(Vector3 rInputForward, float rDeltaTime, ref Quaternion rRotation)
        {
            float lAngle = Vector3Ext.HorizontalAngleTo(mMotionController._Transform.forward, rInputForward, mMotionController._Transform.up);
            if (lAngle != 0f)
            {
                if (_RotationSpeed > 0f && Mathf.Abs(lAngle) > _RotationSpeed * rDeltaTime)
                {
                    lAngle = Mathf.Sign(lAngle) * _RotationSpeed * rDeltaTime;
                }

                rRotation = Quaternion.Euler(0f, lAngle, 0f);
            }
        }

        /// <summary>
        /// Create a rotation velocity that rotates the character based on input
        /// </summary>
        /// <param name="rInputFromAvatarAngle"></param>
        /// <param name="rDeltaTime"></param>
        private void RotateToInput(float rInputFromAvatarAngle, float rDeltaTime, ref Quaternion rRotation)
        {
            if (rInputFromAvatarAngle != 0f)
            {
                if (_RotationSpeed > 0f && Mathf.Abs(rInputFromAvatarAngle) > _RotationSpeed * rDeltaTime)
                {
                    rInputFromAvatarAngle = Mathf.Sign(rInputFromAvatarAngle) * _RotationSpeed * rDeltaTime;
                }

                rRotation = Quaternion.Euler(0f, rInputFromAvatarAngle, 0f);
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
            if (!mRotateWithCamera)
            {
                mLinkRotation = false;
                return;
            }

            float lToCameraAngle = Vector3Ext.HorizontalAngleTo(mMotionController._Transform.forward, mMotionController._CameraTransform.forward, mMotionController._Transform.up);
            if (!mLinkRotation && Mathf.Abs(lToCameraAngle) <= _RotationSpeed * rDeltaTime) { mLinkRotation = true; }

            if (!mLinkRotation)
            {
                float lRotationAngle = Mathf.Abs(lToCameraAngle);
                float lRotationSign = Mathf.Sign(lToCameraAngle);
                lToCameraAngle = lRotationSign * Mathf.Min(_RotationSpeed * rDeltaTime, lRotationAngle);
            }

            Quaternion lRotation = Quaternion.AngleAxis(lToCameraAngle, Vector3.up);
            mActorController.Yaw = mActorController.Yaw * lRotation;
            mActorController._Transform.rotation = mActorController.Tilt * mActorController.Yaw;
        }

        /// <summary>
        /// Tests if we're in one of the stopping states
        /// </summary>
        /// <returns></returns>
        private bool IsStopping()
        {
            if (!_UseStopTransitions) { return false; }

            int lStateID = mMotionLayer._AnimatorStateID;
            if (lStateID == STATE_RunToIdle_LDown) { return true; }
            if (lStateID == STATE_RunToIdle_RDown) { return true; }
            if (lStateID == STATE_WalkToIdle_LDown) { return true; }
            if (lStateID == STATE_WalkToIdle_RDown) { return true; }

            int lTransitionID = mMotionLayer._AnimatorTransitionID;
            if (lTransitionID == TRANS_MoveTree_RunToIdle_LDown) { return true; }
            if (lTransitionID == TRANS_MoveTree_RunToIdle_RDown) { return true; }
            if (lTransitionID == TRANS_MoveTree_WalkToIdle_LDown) { return true; }
            if (lTransitionID == TRANS_MoveTree_WalkToIdle_RDown) { return true; }

            return false;
        }

        /// <summary>
        /// Tests if we're in one of the pivoting states
        /// </summary>
        /// <returns></returns>
        private bool IsIdlePivoting()
        {
            if (!_UseTapToPivot) { return false; }

            int lStateID = mMotionLayer._AnimatorStateID;
            if (lStateID == STATE_IdleTurn180L) { return true; }
            if (lStateID == STATE_IdleTurn90L) { return true; }
            if (lStateID == STATE_IdleTurn20L) { return true; }
            if (lStateID == STATE_IdleTurn20R) { return true; }
            if (lStateID == STATE_IdleTurn90R) { return true; }
            if (lStateID == STATE_IdleTurn180R) { return true; }

            int lTransitionID = mMotionLayer._AnimatorTransitionID;
            if (lTransitionID == TRANS_EntryState_IdleTurn180L) { return true; }
            if (lTransitionID == TRANS_EntryState_IdleTurn90L) { return true; }
            if (lTransitionID == TRANS_EntryState_IdleTurn20L) { return true; }
            if (lTransitionID == TRANS_EntryState_IdleTurn20R) { return true; }
            if (lTransitionID == TRANS_EntryState_IdleTurn90R) { return true; }
            if (lTransitionID == TRANS_EntryState_IdleTurn180R) { return true; }

            return false;
        }

        #region Editor Functions

        // **************************************************************************************************
        // Following properties and function only valid while editing
        // **************************************************************************************************

#if UNITY_EDITOR

        /// <summary>
        /// Creates input settings in the Unity Input Manager
        /// </summary>
        public override void CreateInputManagerSettings()
        {
            if (!InputManagerHelper.IsDefined(_ActionAlias))
            {
                InputManagerEntry lEntry = new InputManagerEntry();
                lEntry.Name = _ActionAlias;
                lEntry.PositiveButton = "left shift";
                lEntry.Gravity = 1000;
                lEntry.Dead = 0.001f;
                lEntry.Sensitivity = 1000;
                lEntry.Type = InputManagerEntryType.KEY_MOUSE_BUTTON;
                lEntry.Axis = 0;
                lEntry.JoyNum = 0;

                InputManagerHelper.AddEntry(lEntry, true);

#if UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX

                lEntry = new InputManagerEntry();
                lEntry.Name = _ActionAlias;
                lEntry.PositiveButton = "";
                lEntry.Gravity = 1;
                lEntry.Dead = 0.3f;
                lEntry.Sensitivity = 1;
                lEntry.Type = InputManagerEntryType.JOYSTICK_AXIS;
                lEntry.Axis = 5;
                lEntry.JoyNum = 0;

                InputManagerHelper.AddEntry(lEntry, true);

#else

                lEntry = new InputManagerEntry();
                lEntry.Name = _ActionAlias;
                lEntry.PositiveButton = "";
                lEntry.Gravity = 1;
                lEntry.Dead = 0.3f;
                lEntry.Sensitivity = 1;
                lEntry.Type = InputManagerEntryType.JOYSTICK_AXIS;
                lEntry.Axis = 9;
                lEntry.JoyNum = 0;

                InputManagerHelper.AddEntry(lEntry, true);

#endif
            }
        }
        
        /// <summary>
        /// Allow the motion to render it's own GUI
        /// </summary>
        public override bool OnInspectorGUI()
        {
            bool lIsDirty = false;

            if (EditorHelper.IntField("Form Condition", "Optional condition used to only activate this motion if the value matches the current Default Form of the MC. Set to -1 to disable.", FormCondition, mMotionController))
            {
                lIsDirty = true;
                FormCondition = EditorHelper.FieldIntValue;
            }

            GUILayout.Space(5f);

            if (EditorHelper.BoolField("Default to Run", "Determines if the default is to run or walk.", DefaultToRun, mMotionController))
            {
                lIsDirty = true;
                DefaultToRun = EditorHelper.FieldBoolValue;
            }

            if (EditorHelper.TextField("Action Alias", "Action alias that triggers a run or walk (which ever is opposite the default).", ActionAlias, mMotionController))
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

            if (EditorHelper.BoolField("Rotate With Camera", "Determines if we rotate to match the camera.", RotateWithCamera, mMotionController))
            {
                lIsDirty = true;
                RotateWithCamera = EditorHelper.FieldBoolValue;
            }

            if (RotateWithCamera)
            {
                if (EditorHelper.TextField("Rotate Action Alias", "Action alias determines if rotation is activated. This typically matches the input source's View Activator.", RotateActionAlias, mMotionController))
                {
                    lIsDirty = true;
                    RotateActionAlias = EditorHelper.FieldStringValue;
                }
            }

            if (EditorHelper.FloatField("Rotation Speed", "Degrees per second to rotate the actor ('0' means instant rotation).", RotationSpeed, mMotionController))
            {
                lIsDirty = true;
                RotationSpeed = EditorHelper.FieldFloatValue;
            }

            GUILayout.Space(5f);

            if (EditorHelper.BoolField("Use Start Transitions", "Determines if we'll use the start transitions when coming from idle", UseStartTransitions, mMotionController))
            {
                lIsDirty = true;
                UseStartTransitions = EditorHelper.FieldBoolValue;
            }

            if (EditorHelper.BoolField("Use Stop Transitions", "Determines if we'll use the stop transitions when stopping movement", UseStopTransitions, mMotionController))
            {
                lIsDirty = true;
                UseStopTransitions = EditorHelper.FieldBoolValue;
            }

            if (EditorHelper.BoolField("Use Tap to Pivot", "Determines if taping a direction while idle will pivot the character without moving them.", UseTapToPivot, mMotionController))
            {
                lIsDirty = true;
                UseTapToPivot = EditorHelper.FieldBoolValue;
            }

            if (UseTapToPivot)
            {
                EditorGUILayout.BeginHorizontal();

                if (EditorHelper.FloatField("Min Angle", "Sets the minimum angle between the input direction and character direction where we'll do a pivot.", MinPivotAngle, mMotionController))
                {
                    lIsDirty = true;
                    MinPivotAngle = EditorHelper.FieldFloatValue;
                }

                GUILayout.Space(10f);

                EditorGUILayout.LabelField(new GUIContent("Delay", "Delay in seconds before we test if we're NOT pivoting, but moving. In my tests, the average tap took 0.12 to 0.15 seconds."), GUILayout.Width(40f));
                if (EditorHelper.FloatField(TapToPivotDelay, "Delay", mMotionController, 40f))
                {
                    lIsDirty = true;
                    TapToPivotDelay = EditorHelper.FieldFloatValue;
                }

                GUILayout.FlexibleSpace();

                EditorGUILayout.EndHorizontal();
            }

            if (EditorHelper.IntField("Smoothing Samples", "The more samples the smoother movement is, but the less responsive.", SmoothingSamples, mMotionController))
            {
                lIsDirty = true;
                SmoothingSamples = EditorHelper.FieldIntValue;
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
        public int STATE_Start = -1;
        public int STATE_MoveTree = -1;
        public int STATE_IdleToWalk90L = -1;
        public int STATE_IdleToWalk90R = -1;
        public int STATE_IdleToWalk180R = -1;
        public int STATE_IdleToWalk180L = -1;
        public int STATE_IdlePose = -1;
        public int STATE_IdleToRun90L = -1;
        public int STATE_IdleToRun180L = -1;
        public int STATE_IdleToRun90R = -1;
        public int STATE_IdleToRun180R = -1;
        public int STATE_IdleToRun = -1;
        public int STATE_RunPivot180R_LDown = -1;
        public int STATE_WalkPivot180L = -1;
        public int STATE_RunToIdle_LDown = -1;
        public int STATE_WalkToIdle_LDown = -1;
        public int STATE_WalkToIdle_RDown = -1;
        public int STATE_RunToIdle_RDown = -1;
        public int STATE_IdleTurn20R = -1;
        public int STATE_IdleTurn90R = -1;
        public int STATE_IdleTurn180R = -1;
        public int STATE_IdleTurn20L = -1;
        public int STATE_IdleTurn90L = -1;
        public int STATE_IdleTurn180L = -1;
        public int STATE_IdleTurnEndPose = -1;
        public int TRANS_AnyState_IdleToWalk90L = -1;
        public int TRANS_EntryState_IdleToWalk90L = -1;
        public int TRANS_AnyState_IdleToWalk90R = -1;
        public int TRANS_EntryState_IdleToWalk90R = -1;
        public int TRANS_AnyState_IdleToWalk180R = -1;
        public int TRANS_EntryState_IdleToWalk180R = -1;
        public int TRANS_AnyState_MoveTree = -1;
        public int TRANS_EntryState_MoveTree = -1;
        public int TRANS_AnyState_IdleToWalk180L = -1;
        public int TRANS_EntryState_IdleToWalk180L = -1;
        public int TRANS_AnyState_IdleToRun180L = -1;
        public int TRANS_EntryState_IdleToRun180L = -1;
        public int TRANS_AnyState_IdleToRun90L = -1;
        public int TRANS_EntryState_IdleToRun90L = -1;
        public int TRANS_AnyState_IdleToRun90R = -1;
        public int TRANS_EntryState_IdleToRun90R = -1;
        public int TRANS_AnyState_IdleToRun180R = -1;
        public int TRANS_EntryState_IdleToRun180R = -1;
        public int TRANS_AnyState_IdleToRun = -1;
        public int TRANS_EntryState_IdleToRun = -1;
        public int TRANS_AnyState_IdleTurn180L = -1;
        public int TRANS_EntryState_IdleTurn180L = -1;
        public int TRANS_AnyState_IdleTurn90L = -1;
        public int TRANS_EntryState_IdleTurn90L = -1;
        public int TRANS_AnyState_IdleTurn20L = -1;
        public int TRANS_EntryState_IdleTurn20L = -1;
        public int TRANS_AnyState_IdleTurn20R = -1;
        public int TRANS_EntryState_IdleTurn20R = -1;
        public int TRANS_AnyState_IdleTurn90R = -1;
        public int TRANS_EntryState_IdleTurn90R = -1;
        public int TRANS_AnyState_IdleTurn180R = -1;
        public int TRANS_EntryState_IdleTurn180R = -1;
        public int TRANS_MoveTree_RunPivot180R_LDown = -1;
        public int TRANS_MoveTree_WalkPivot180L = -1;
        public int TRANS_MoveTree_RunToIdle_LDown = -1;
        public int TRANS_MoveTree_WalkToIdle_LDown = -1;
        public int TRANS_MoveTree_RunToIdle_RDown = -1;
        public int TRANS_MoveTree_WalkToIdle_RDown = -1;
        public int TRANS_IdleToWalk90L_MoveTree = -1;
        public int TRANS_IdleToWalk90L_IdlePose = -1;
        public int TRANS_IdleToWalk90R_MoveTree = -1;
        public int TRANS_IdleToWalk90R_IdlePose = -1;
        public int TRANS_IdleToWalk180R_MoveTree = -1;
        public int TRANS_IdleToWalk180R_IdlePose = -1;
        public int TRANS_IdleToWalk180L_MoveTree = -1;
        public int TRANS_IdleToWalk180L_IdlePose = -1;
        public int TRANS_IdleToRun90L_MoveTree = -1;
        public int TRANS_IdleToRun90L_IdlePose = -1;
        public int TRANS_IdleToRun180L_MoveTree = -1;
        public int TRANS_IdleToRun180L_IdlePose = -1;
        public int TRANS_IdleToRun90R_MoveTree = -1;
        public int TRANS_IdleToRun90R_IdlePose = -1;
        public int TRANS_IdleToRun180R_MoveTree = -1;
        public int TRANS_IdleToRun180R_IdlePose = -1;
        public int TRANS_IdleToRun_MoveTree = -1;
        public int TRANS_IdleToRun_IdlePose = -1;
        public int TRANS_RunPivot180R_LDown_MoveTree = -1;
        public int TRANS_WalkPivot180L_MoveTree = -1;
        public int TRANS_RunToIdle_LDown_IdlePose = -1;
        public int TRANS_RunToIdle_LDown_MoveTree = -1;
        public int TRANS_WalkToIdle_LDown_MoveTree = -1;
        public int TRANS_WalkToIdle_LDown_IdlePose = -1;
        public int TRANS_WalkToIdle_RDown_MoveTree = -1;
        public int TRANS_WalkToIdle_RDown_IdlePose = -1;
        public int TRANS_RunToIdle_RDown_MoveTree = -1;
        public int TRANS_RunToIdle_RDown_IdlePose = -1;
        public int TRANS_IdleTurn20R_IdleTurnEndPose = -1;
        public int TRANS_IdleTurn90R_IdleTurnEndPose = -1;
        public int TRANS_IdleTurn180R_IdleTurnEndPose = -1;
        public int TRANS_IdleTurn20L_IdleTurnEndPose = -1;
        public int TRANS_IdleTurn90L_IdleTurnEndPose = -1;
        public int TRANS_IdleTurn180L_IdleTurnEndPose = -1;
        public int TRANS_IdleTurnEndPose_MoveTree = -1;

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
                    if (lStateID == STATE_IdleToWalk90L) { return true; }
                    if (lStateID == STATE_IdleToWalk90R) { return true; }
                    if (lStateID == STATE_IdleToWalk180R) { return true; }
                    if (lStateID == STATE_IdleToWalk180L) { return true; }
                    if (lStateID == STATE_IdlePose) { return true; }
                    if (lStateID == STATE_IdleToRun90L) { return true; }
                    if (lStateID == STATE_IdleToRun180L) { return true; }
                    if (lStateID == STATE_IdleToRun90R) { return true; }
                    if (lStateID == STATE_IdleToRun180R) { return true; }
                    if (lStateID == STATE_IdleToRun) { return true; }
                    if (lStateID == STATE_RunPivot180R_LDown) { return true; }
                    if (lStateID == STATE_WalkPivot180L) { return true; }
                    if (lStateID == STATE_RunToIdle_LDown) { return true; }
                    if (lStateID == STATE_WalkToIdle_LDown) { return true; }
                    if (lStateID == STATE_WalkToIdle_RDown) { return true; }
                    if (lStateID == STATE_RunToIdle_RDown) { return true; }
                    if (lStateID == STATE_IdleTurn20R) { return true; }
                    if (lStateID == STATE_IdleTurn90R) { return true; }
                    if (lStateID == STATE_IdleTurn180R) { return true; }
                    if (lStateID == STATE_IdleTurn20L) { return true; }
                    if (lStateID == STATE_IdleTurn90L) { return true; }
                    if (lStateID == STATE_IdleTurn180L) { return true; }
                    if (lStateID == STATE_IdleTurnEndPose) { return true; }
                }

                if (lTransitionID == TRANS_AnyState_IdleToWalk90L) { return true; }
                if (lTransitionID == TRANS_EntryState_IdleToWalk90L) { return true; }
                if (lTransitionID == TRANS_AnyState_IdleToWalk90R) { return true; }
                if (lTransitionID == TRANS_EntryState_IdleToWalk90R) { return true; }
                if (lTransitionID == TRANS_AnyState_IdleToWalk180R) { return true; }
                if (lTransitionID == TRANS_EntryState_IdleToWalk180R) { return true; }
                if (lTransitionID == TRANS_AnyState_MoveTree) { return true; }
                if (lTransitionID == TRANS_EntryState_MoveTree) { return true; }
                if (lTransitionID == TRANS_AnyState_IdleToWalk180L) { return true; }
                if (lTransitionID == TRANS_EntryState_IdleToWalk180L) { return true; }
                if (lTransitionID == TRANS_AnyState_IdleToRun180L) { return true; }
                if (lTransitionID == TRANS_EntryState_IdleToRun180L) { return true; }
                if (lTransitionID == TRANS_AnyState_IdleToRun90L) { return true; }
                if (lTransitionID == TRANS_EntryState_IdleToRun90L) { return true; }
                if (lTransitionID == TRANS_AnyState_IdleToRun90R) { return true; }
                if (lTransitionID == TRANS_EntryState_IdleToRun90R) { return true; }
                if (lTransitionID == TRANS_AnyState_IdleToRun180R) { return true; }
                if (lTransitionID == TRANS_EntryState_IdleToRun180R) { return true; }
                if (lTransitionID == TRANS_AnyState_IdleToRun) { return true; }
                if (lTransitionID == TRANS_EntryState_IdleToRun) { return true; }
                if (lTransitionID == TRANS_AnyState_MoveTree) { return true; }
                if (lTransitionID == TRANS_EntryState_MoveTree) { return true; }
                if (lTransitionID == TRANS_AnyState_MoveTree) { return true; }
                if (lTransitionID == TRANS_EntryState_MoveTree) { return true; }
                if (lTransitionID == TRANS_AnyState_IdleTurn180L) { return true; }
                if (lTransitionID == TRANS_EntryState_IdleTurn180L) { return true; }
                if (lTransitionID == TRANS_AnyState_IdleTurn90L) { return true; }
                if (lTransitionID == TRANS_EntryState_IdleTurn90L) { return true; }
                if (lTransitionID == TRANS_AnyState_IdleTurn20L) { return true; }
                if (lTransitionID == TRANS_EntryState_IdleTurn20L) { return true; }
                if (lTransitionID == TRANS_AnyState_IdleTurn20R) { return true; }
                if (lTransitionID == TRANS_EntryState_IdleTurn20R) { return true; }
                if (lTransitionID == TRANS_AnyState_IdleTurn90R) { return true; }
                if (lTransitionID == TRANS_EntryState_IdleTurn90R) { return true; }
                if (lTransitionID == TRANS_AnyState_IdleTurn180R) { return true; }
                if (lTransitionID == TRANS_EntryState_IdleTurn180R) { return true; }
                if (lTransitionID == TRANS_MoveTree_RunPivot180R_LDown) { return true; }
                if (lTransitionID == TRANS_MoveTree_RunPivot180R_LDown) { return true; }
                if (lTransitionID == TRANS_MoveTree_WalkPivot180L) { return true; }
                if (lTransitionID == TRANS_MoveTree_WalkPivot180L) { return true; }
                if (lTransitionID == TRANS_MoveTree_RunToIdle_LDown) { return true; }
                if (lTransitionID == TRANS_MoveTree_WalkToIdle_LDown) { return true; }
                if (lTransitionID == TRANS_MoveTree_RunToIdle_RDown) { return true; }
                if (lTransitionID == TRANS_MoveTree_WalkToIdle_RDown) { return true; }
                if (lTransitionID == TRANS_MoveTree_RunToIdle_RDown) { return true; }
                if (lTransitionID == TRANS_MoveTree_RunToIdle_LDown) { return true; }
                if (lTransitionID == TRANS_MoveTree_WalkToIdle_RDown) { return true; }
                if (lTransitionID == TRANS_MoveTree_WalkToIdle_LDown) { return true; }
                if (lTransitionID == TRANS_IdleToWalk90L_MoveTree) { return true; }
                if (lTransitionID == TRANS_IdleToWalk90L_IdlePose) { return true; }
                if (lTransitionID == TRANS_IdleToWalk90R_MoveTree) { return true; }
                if (lTransitionID == TRANS_IdleToWalk90R_IdlePose) { return true; }
                if (lTransitionID == TRANS_IdleToWalk180R_MoveTree) { return true; }
                if (lTransitionID == TRANS_IdleToWalk180R_IdlePose) { return true; }
                if (lTransitionID == TRANS_IdleToWalk180L_MoveTree) { return true; }
                if (lTransitionID == TRANS_IdleToWalk180L_IdlePose) { return true; }
                if (lTransitionID == TRANS_IdleToRun90L_MoveTree) { return true; }
                if (lTransitionID == TRANS_IdleToRun90L_IdlePose) { return true; }
                if (lTransitionID == TRANS_IdleToRun180L_MoveTree) { return true; }
                if (lTransitionID == TRANS_IdleToRun180L_IdlePose) { return true; }
                if (lTransitionID == TRANS_IdleToRun90R_MoveTree) { return true; }
                if (lTransitionID == TRANS_IdleToRun90R_IdlePose) { return true; }
                if (lTransitionID == TRANS_IdleToRun180R_MoveTree) { return true; }
                if (lTransitionID == TRANS_IdleToRun180R_IdlePose) { return true; }
                if (lTransitionID == TRANS_IdleToRun_MoveTree) { return true; }
                if (lTransitionID == TRANS_IdleToRun_IdlePose) { return true; }
                if (lTransitionID == TRANS_RunPivot180R_LDown_MoveTree) { return true; }
                if (lTransitionID == TRANS_WalkPivot180L_MoveTree) { return true; }
                if (lTransitionID == TRANS_RunToIdle_LDown_IdlePose) { return true; }
                if (lTransitionID == TRANS_RunToIdle_LDown_MoveTree) { return true; }
                if (lTransitionID == TRANS_WalkToIdle_LDown_MoveTree) { return true; }
                if (lTransitionID == TRANS_WalkToIdle_LDown_IdlePose) { return true; }
                if (lTransitionID == TRANS_WalkToIdle_RDown_MoveTree) { return true; }
                if (lTransitionID == TRANS_WalkToIdle_RDown_IdlePose) { return true; }
                if (lTransitionID == TRANS_RunToIdle_RDown_MoveTree) { return true; }
                if (lTransitionID == TRANS_RunToIdle_RDown_IdlePose) { return true; }
                if (lTransitionID == TRANS_IdleTurn20R_IdleTurnEndPose) { return true; }
                if (lTransitionID == TRANS_IdleTurn90R_IdleTurnEndPose) { return true; }
                if (lTransitionID == TRANS_IdleTurn180R_IdleTurnEndPose) { return true; }
                if (lTransitionID == TRANS_IdleTurn20L_IdleTurnEndPose) { return true; }
                if (lTransitionID == TRANS_IdleTurn90L_IdleTurnEndPose) { return true; }
                if (lTransitionID == TRANS_IdleTurn180L_IdleTurnEndPose) { return true; }
                if (lTransitionID == TRANS_IdleTurnEndPose_MoveTree) { return true; }
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
            if (rStateID == STATE_IdleToWalk90L) { return true; }
            if (rStateID == STATE_IdleToWalk90R) { return true; }
            if (rStateID == STATE_IdleToWalk180R) { return true; }
            if (rStateID == STATE_IdleToWalk180L) { return true; }
            if (rStateID == STATE_IdlePose) { return true; }
            if (rStateID == STATE_IdleToRun90L) { return true; }
            if (rStateID == STATE_IdleToRun180L) { return true; }
            if (rStateID == STATE_IdleToRun90R) { return true; }
            if (rStateID == STATE_IdleToRun180R) { return true; }
            if (rStateID == STATE_IdleToRun) { return true; }
            if (rStateID == STATE_RunPivot180R_LDown) { return true; }
            if (rStateID == STATE_WalkPivot180L) { return true; }
            if (rStateID == STATE_RunToIdle_LDown) { return true; }
            if (rStateID == STATE_WalkToIdle_LDown) { return true; }
            if (rStateID == STATE_WalkToIdle_RDown) { return true; }
            if (rStateID == STATE_RunToIdle_RDown) { return true; }
            if (rStateID == STATE_IdleTurn20R) { return true; }
            if (rStateID == STATE_IdleTurn90R) { return true; }
            if (rStateID == STATE_IdleTurn180R) { return true; }
            if (rStateID == STATE_IdleTurn20L) { return true; }
            if (rStateID == STATE_IdleTurn90L) { return true; }
            if (rStateID == STATE_IdleTurn180L) { return true; }
            if (rStateID == STATE_IdleTurnEndPose) { return true; }
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
                if (rStateID == STATE_IdleToWalk90L) { return true; }
                if (rStateID == STATE_IdleToWalk90R) { return true; }
                if (rStateID == STATE_IdleToWalk180R) { return true; }
                if (rStateID == STATE_IdleToWalk180L) { return true; }
                if (rStateID == STATE_IdlePose) { return true; }
                if (rStateID == STATE_IdleToRun90L) { return true; }
                if (rStateID == STATE_IdleToRun180L) { return true; }
                if (rStateID == STATE_IdleToRun90R) { return true; }
                if (rStateID == STATE_IdleToRun180R) { return true; }
                if (rStateID == STATE_IdleToRun) { return true; }
                if (rStateID == STATE_RunPivot180R_LDown) { return true; }
                if (rStateID == STATE_WalkPivot180L) { return true; }
                if (rStateID == STATE_RunToIdle_LDown) { return true; }
                if (rStateID == STATE_WalkToIdle_LDown) { return true; }
                if (rStateID == STATE_WalkToIdle_RDown) { return true; }
                if (rStateID == STATE_RunToIdle_RDown) { return true; }
                if (rStateID == STATE_IdleTurn20R) { return true; }
                if (rStateID == STATE_IdleTurn90R) { return true; }
                if (rStateID == STATE_IdleTurn180R) { return true; }
                if (rStateID == STATE_IdleTurn20L) { return true; }
                if (rStateID == STATE_IdleTurn90L) { return true; }
                if (rStateID == STATE_IdleTurn180L) { return true; }
                if (rStateID == STATE_IdleTurnEndPose) { return true; }
            }

            if (rTransitionID == TRANS_AnyState_IdleToWalk90L) { return true; }
            if (rTransitionID == TRANS_EntryState_IdleToWalk90L) { return true; }
            if (rTransitionID == TRANS_AnyState_IdleToWalk90R) { return true; }
            if (rTransitionID == TRANS_EntryState_IdleToWalk90R) { return true; }
            if (rTransitionID == TRANS_AnyState_IdleToWalk180R) { return true; }
            if (rTransitionID == TRANS_EntryState_IdleToWalk180R) { return true; }
            if (rTransitionID == TRANS_AnyState_MoveTree) { return true; }
            if (rTransitionID == TRANS_EntryState_MoveTree) { return true; }
            if (rTransitionID == TRANS_AnyState_IdleToWalk180L) { return true; }
            if (rTransitionID == TRANS_EntryState_IdleToWalk180L) { return true; }
            if (rTransitionID == TRANS_AnyState_IdleToRun180L) { return true; }
            if (rTransitionID == TRANS_EntryState_IdleToRun180L) { return true; }
            if (rTransitionID == TRANS_AnyState_IdleToRun90L) { return true; }
            if (rTransitionID == TRANS_EntryState_IdleToRun90L) { return true; }
            if (rTransitionID == TRANS_AnyState_IdleToRun90R) { return true; }
            if (rTransitionID == TRANS_EntryState_IdleToRun90R) { return true; }
            if (rTransitionID == TRANS_AnyState_IdleToRun180R) { return true; }
            if (rTransitionID == TRANS_EntryState_IdleToRun180R) { return true; }
            if (rTransitionID == TRANS_AnyState_IdleToRun) { return true; }
            if (rTransitionID == TRANS_EntryState_IdleToRun) { return true; }
            if (rTransitionID == TRANS_AnyState_MoveTree) { return true; }
            if (rTransitionID == TRANS_EntryState_MoveTree) { return true; }
            if (rTransitionID == TRANS_AnyState_MoveTree) { return true; }
            if (rTransitionID == TRANS_EntryState_MoveTree) { return true; }
            if (rTransitionID == TRANS_AnyState_IdleTurn180L) { return true; }
            if (rTransitionID == TRANS_EntryState_IdleTurn180L) { return true; }
            if (rTransitionID == TRANS_AnyState_IdleTurn90L) { return true; }
            if (rTransitionID == TRANS_EntryState_IdleTurn90L) { return true; }
            if (rTransitionID == TRANS_AnyState_IdleTurn20L) { return true; }
            if (rTransitionID == TRANS_EntryState_IdleTurn20L) { return true; }
            if (rTransitionID == TRANS_AnyState_IdleTurn20R) { return true; }
            if (rTransitionID == TRANS_EntryState_IdleTurn20R) { return true; }
            if (rTransitionID == TRANS_AnyState_IdleTurn90R) { return true; }
            if (rTransitionID == TRANS_EntryState_IdleTurn90R) { return true; }
            if (rTransitionID == TRANS_AnyState_IdleTurn180R) { return true; }
            if (rTransitionID == TRANS_EntryState_IdleTurn180R) { return true; }
            if (rTransitionID == TRANS_MoveTree_RunPivot180R_LDown) { return true; }
            if (rTransitionID == TRANS_MoveTree_RunPivot180R_LDown) { return true; }
            if (rTransitionID == TRANS_MoveTree_WalkPivot180L) { return true; }
            if (rTransitionID == TRANS_MoveTree_WalkPivot180L) { return true; }
            if (rTransitionID == TRANS_MoveTree_RunToIdle_LDown) { return true; }
            if (rTransitionID == TRANS_MoveTree_WalkToIdle_LDown) { return true; }
            if (rTransitionID == TRANS_MoveTree_RunToIdle_RDown) { return true; }
            if (rTransitionID == TRANS_MoveTree_WalkToIdle_RDown) { return true; }
            if (rTransitionID == TRANS_MoveTree_RunToIdle_RDown) { return true; }
            if (rTransitionID == TRANS_MoveTree_RunToIdle_LDown) { return true; }
            if (rTransitionID == TRANS_MoveTree_WalkToIdle_RDown) { return true; }
            if (rTransitionID == TRANS_MoveTree_WalkToIdle_LDown) { return true; }
            if (rTransitionID == TRANS_IdleToWalk90L_MoveTree) { return true; }
            if (rTransitionID == TRANS_IdleToWalk90L_IdlePose) { return true; }
            if (rTransitionID == TRANS_IdleToWalk90R_MoveTree) { return true; }
            if (rTransitionID == TRANS_IdleToWalk90R_IdlePose) { return true; }
            if (rTransitionID == TRANS_IdleToWalk180R_MoveTree) { return true; }
            if (rTransitionID == TRANS_IdleToWalk180R_IdlePose) { return true; }
            if (rTransitionID == TRANS_IdleToWalk180L_MoveTree) { return true; }
            if (rTransitionID == TRANS_IdleToWalk180L_IdlePose) { return true; }
            if (rTransitionID == TRANS_IdleToRun90L_MoveTree) { return true; }
            if (rTransitionID == TRANS_IdleToRun90L_IdlePose) { return true; }
            if (rTransitionID == TRANS_IdleToRun180L_MoveTree) { return true; }
            if (rTransitionID == TRANS_IdleToRun180L_IdlePose) { return true; }
            if (rTransitionID == TRANS_IdleToRun90R_MoveTree) { return true; }
            if (rTransitionID == TRANS_IdleToRun90R_IdlePose) { return true; }
            if (rTransitionID == TRANS_IdleToRun180R_MoveTree) { return true; }
            if (rTransitionID == TRANS_IdleToRun180R_IdlePose) { return true; }
            if (rTransitionID == TRANS_IdleToRun_MoveTree) { return true; }
            if (rTransitionID == TRANS_IdleToRun_IdlePose) { return true; }
            if (rTransitionID == TRANS_RunPivot180R_LDown_MoveTree) { return true; }
            if (rTransitionID == TRANS_WalkPivot180L_MoveTree) { return true; }
            if (rTransitionID == TRANS_RunToIdle_LDown_IdlePose) { return true; }
            if (rTransitionID == TRANS_RunToIdle_LDown_MoveTree) { return true; }
            if (rTransitionID == TRANS_WalkToIdle_LDown_MoveTree) { return true; }
            if (rTransitionID == TRANS_WalkToIdle_LDown_IdlePose) { return true; }
            if (rTransitionID == TRANS_WalkToIdle_RDown_MoveTree) { return true; }
            if (rTransitionID == TRANS_WalkToIdle_RDown_IdlePose) { return true; }
            if (rTransitionID == TRANS_RunToIdle_RDown_MoveTree) { return true; }
            if (rTransitionID == TRANS_RunToIdle_RDown_IdlePose) { return true; }
            if (rTransitionID == TRANS_IdleTurn20R_IdleTurnEndPose) { return true; }
            if (rTransitionID == TRANS_IdleTurn90R_IdleTurnEndPose) { return true; }
            if (rTransitionID == TRANS_IdleTurn180R_IdleTurnEndPose) { return true; }
            if (rTransitionID == TRANS_IdleTurn20L_IdleTurnEndPose) { return true; }
            if (rTransitionID == TRANS_IdleTurn90L_IdleTurnEndPose) { return true; }
            if (rTransitionID == TRANS_IdleTurn180L_IdleTurnEndPose) { return true; }
            if (rTransitionID == TRANS_IdleTurnEndPose_MoveTree) { return true; }
            return false;
        }

        /// <summary>
        /// Preprocess any animator data so the motion can use it later
        /// </summary>
        public override void LoadAnimatorData()
        {
            string lLayer = mMotionController.Animator.GetLayerName(mMotionLayer._AnimatorLayerIndex);
            TRANS_AnyState_IdleToWalk90L = mMotionController.AddAnimatorName("AnyState -> " + lLayer + ".WalkRunPivot v2-SM.IdleToWalk90L");
            TRANS_EntryState_IdleToWalk90L = mMotionController.AddAnimatorName("Entry -> " + lLayer + ".WalkRunPivot v2-SM.IdleToWalk90L");
            TRANS_AnyState_IdleToWalk90R = mMotionController.AddAnimatorName("AnyState -> " + lLayer + ".WalkRunPivot v2-SM.IdleToWalk90R");
            TRANS_EntryState_IdleToWalk90R = mMotionController.AddAnimatorName("Entry -> " + lLayer + ".WalkRunPivot v2-SM.IdleToWalk90R");
            TRANS_AnyState_IdleToWalk180R = mMotionController.AddAnimatorName("AnyState -> " + lLayer + ".WalkRunPivot v2-SM.IdleToWalk180R");
            TRANS_EntryState_IdleToWalk180R = mMotionController.AddAnimatorName("Entry -> " + lLayer + ".WalkRunPivot v2-SM.IdleToWalk180R");
            TRANS_AnyState_MoveTree = mMotionController.AddAnimatorName("AnyState -> " + lLayer + ".WalkRunPivot v2-SM.Move Tree");
            TRANS_EntryState_MoveTree = mMotionController.AddAnimatorName("Entry -> " + lLayer + ".WalkRunPivot v2-SM.Move Tree");
            TRANS_AnyState_IdleToWalk180L = mMotionController.AddAnimatorName("AnyState -> " + lLayer + ".WalkRunPivot v2-SM.IdleToWalk180L");
            TRANS_EntryState_IdleToWalk180L = mMotionController.AddAnimatorName("Entry -> " + lLayer + ".WalkRunPivot v2-SM.IdleToWalk180L");
            TRANS_AnyState_IdleToRun180L = mMotionController.AddAnimatorName("AnyState -> " + lLayer + ".WalkRunPivot v2-SM.IdleToRun180L");
            TRANS_EntryState_IdleToRun180L = mMotionController.AddAnimatorName("Entry -> " + lLayer + ".WalkRunPivot v2-SM.IdleToRun180L");
            TRANS_AnyState_IdleToRun90L = mMotionController.AddAnimatorName("AnyState -> " + lLayer + ".WalkRunPivot v2-SM.IdleToRun90L");
            TRANS_EntryState_IdleToRun90L = mMotionController.AddAnimatorName("Entry -> " + lLayer + ".WalkRunPivot v2-SM.IdleToRun90L");
            TRANS_AnyState_IdleToRun90R = mMotionController.AddAnimatorName("AnyState -> " + lLayer + ".WalkRunPivot v2-SM.IdleToRun90R");
            TRANS_EntryState_IdleToRun90R = mMotionController.AddAnimatorName("Entry -> " + lLayer + ".WalkRunPivot v2-SM.IdleToRun90R");
            TRANS_AnyState_IdleToRun180R = mMotionController.AddAnimatorName("AnyState -> " + lLayer + ".WalkRunPivot v2-SM.IdleToRun180R");
            TRANS_EntryState_IdleToRun180R = mMotionController.AddAnimatorName("Entry -> " + lLayer + ".WalkRunPivot v2-SM.IdleToRun180R");
            TRANS_AnyState_IdleToRun = mMotionController.AddAnimatorName("AnyState -> " + lLayer + ".WalkRunPivot v2-SM.IdleToRun");
            TRANS_EntryState_IdleToRun = mMotionController.AddAnimatorName("Entry -> " + lLayer + ".WalkRunPivot v2-SM.IdleToRun");
            TRANS_AnyState_MoveTree = mMotionController.AddAnimatorName("AnyState -> " + lLayer + ".WalkRunPivot v2-SM.Move Tree");
            TRANS_EntryState_MoveTree = mMotionController.AddAnimatorName("Entry -> " + lLayer + ".WalkRunPivot v2-SM.Move Tree");
            TRANS_AnyState_MoveTree = mMotionController.AddAnimatorName("AnyState -> " + lLayer + ".WalkRunPivot v2-SM.Move Tree");
            TRANS_EntryState_MoveTree = mMotionController.AddAnimatorName("Entry -> " + lLayer + ".WalkRunPivot v2-SM.Move Tree");
            TRANS_AnyState_IdleTurn180L = mMotionController.AddAnimatorName("AnyState -> " + lLayer + ".WalkRunPivot v2-SM.IdleTurn180L");
            TRANS_EntryState_IdleTurn180L = mMotionController.AddAnimatorName("Entry -> " + lLayer + ".WalkRunPivot v2-SM.IdleTurn180L");
            TRANS_AnyState_IdleTurn90L = mMotionController.AddAnimatorName("AnyState -> " + lLayer + ".WalkRunPivot v2-SM.IdleTurn90L");
            TRANS_EntryState_IdleTurn90L = mMotionController.AddAnimatorName("Entry -> " + lLayer + ".WalkRunPivot v2-SM.IdleTurn90L");
            TRANS_AnyState_IdleTurn20L = mMotionController.AddAnimatorName("AnyState -> " + lLayer + ".WalkRunPivot v2-SM.IdleTurn20L");
            TRANS_EntryState_IdleTurn20L = mMotionController.AddAnimatorName("Entry -> " + lLayer + ".WalkRunPivot v2-SM.IdleTurn20L");
            TRANS_AnyState_IdleTurn20R = mMotionController.AddAnimatorName("AnyState -> " + lLayer + ".WalkRunPivot v2-SM.IdleTurn20R");
            TRANS_EntryState_IdleTurn20R = mMotionController.AddAnimatorName("Entry -> " + lLayer + ".WalkRunPivot v2-SM.IdleTurn20R");
            TRANS_AnyState_IdleTurn90R = mMotionController.AddAnimatorName("AnyState -> " + lLayer + ".WalkRunPivot v2-SM.IdleTurn90R");
            TRANS_EntryState_IdleTurn90R = mMotionController.AddAnimatorName("Entry -> " + lLayer + ".WalkRunPivot v2-SM.IdleTurn90R");
            TRANS_AnyState_IdleTurn180R = mMotionController.AddAnimatorName("AnyState -> " + lLayer + ".WalkRunPivot v2-SM.IdleTurn180R");
            TRANS_EntryState_IdleTurn180R = mMotionController.AddAnimatorName("Entry -> " + lLayer + ".WalkRunPivot v2-SM.IdleTurn180R");
            STATE_Start = mMotionController.AddAnimatorName("" + lLayer + ".Start");
            STATE_MoveTree = mMotionController.AddAnimatorName("" + lLayer + ".WalkRunPivot v2-SM.Move Tree");
            TRANS_MoveTree_RunPivot180R_LDown = mMotionController.AddAnimatorName("" + lLayer + ".WalkRunPivot v2-SM.Move Tree -> " + lLayer + ".WalkRunPivot v2-SM.RunPivot180R_LDown");
            TRANS_MoveTree_RunPivot180R_LDown = mMotionController.AddAnimatorName("" + lLayer + ".WalkRunPivot v2-SM.Move Tree -> " + lLayer + ".WalkRunPivot v2-SM.RunPivot180R_LDown");
            TRANS_MoveTree_WalkPivot180L = mMotionController.AddAnimatorName("" + lLayer + ".WalkRunPivot v2-SM.Move Tree -> " + lLayer + ".WalkRunPivot v2-SM.WalkPivot180L");
            TRANS_MoveTree_WalkPivot180L = mMotionController.AddAnimatorName("" + lLayer + ".WalkRunPivot v2-SM.Move Tree -> " + lLayer + ".WalkRunPivot v2-SM.WalkPivot180L");
            TRANS_MoveTree_RunToIdle_LDown = mMotionController.AddAnimatorName("" + lLayer + ".WalkRunPivot v2-SM.Move Tree -> " + lLayer + ".WalkRunPivot v2-SM.RunToIdle_LDown");
            TRANS_MoveTree_WalkToIdle_LDown = mMotionController.AddAnimatorName("" + lLayer + ".WalkRunPivot v2-SM.Move Tree -> " + lLayer + ".WalkRunPivot v2-SM.WalkToIdle_LDown");
            TRANS_MoveTree_RunToIdle_RDown = mMotionController.AddAnimatorName("" + lLayer + ".WalkRunPivot v2-SM.Move Tree -> " + lLayer + ".WalkRunPivot v2-SM.RunToIdle_RDown");
            TRANS_MoveTree_WalkToIdle_RDown = mMotionController.AddAnimatorName("" + lLayer + ".WalkRunPivot v2-SM.Move Tree -> " + lLayer + ".WalkRunPivot v2-SM.WalkToIdle_RDown");
            TRANS_MoveTree_RunToIdle_RDown = mMotionController.AddAnimatorName("" + lLayer + ".WalkRunPivot v2-SM.Move Tree -> " + lLayer + ".WalkRunPivot v2-SM.RunToIdle_RDown");
            TRANS_MoveTree_RunToIdle_LDown = mMotionController.AddAnimatorName("" + lLayer + ".WalkRunPivot v2-SM.Move Tree -> " + lLayer + ".WalkRunPivot v2-SM.RunToIdle_LDown");
            TRANS_MoveTree_WalkToIdle_RDown = mMotionController.AddAnimatorName("" + lLayer + ".WalkRunPivot v2-SM.Move Tree -> " + lLayer + ".WalkRunPivot v2-SM.WalkToIdle_RDown");
            TRANS_MoveTree_WalkToIdle_LDown = mMotionController.AddAnimatorName("" + lLayer + ".WalkRunPivot v2-SM.Move Tree -> " + lLayer + ".WalkRunPivot v2-SM.WalkToIdle_LDown");
            STATE_IdleToWalk90L = mMotionController.AddAnimatorName("" + lLayer + ".WalkRunPivot v2-SM.IdleToWalk90L");
            TRANS_IdleToWalk90L_MoveTree = mMotionController.AddAnimatorName("" + lLayer + ".WalkRunPivot v2-SM.IdleToWalk90L -> " + lLayer + ".WalkRunPivot v2-SM.Move Tree");
            TRANS_IdleToWalk90L_IdlePose = mMotionController.AddAnimatorName("" + lLayer + ".WalkRunPivot v2-SM.IdleToWalk90L -> " + lLayer + ".WalkRunPivot v2-SM.IdlePose");
            STATE_IdleToWalk90R = mMotionController.AddAnimatorName("" + lLayer + ".WalkRunPivot v2-SM.IdleToWalk90R");
            TRANS_IdleToWalk90R_MoveTree = mMotionController.AddAnimatorName("" + lLayer + ".WalkRunPivot v2-SM.IdleToWalk90R -> " + lLayer + ".WalkRunPivot v2-SM.Move Tree");
            TRANS_IdleToWalk90R_IdlePose = mMotionController.AddAnimatorName("" + lLayer + ".WalkRunPivot v2-SM.IdleToWalk90R -> " + lLayer + ".WalkRunPivot v2-SM.IdlePose");
            STATE_IdleToWalk180R = mMotionController.AddAnimatorName("" + lLayer + ".WalkRunPivot v2-SM.IdleToWalk180R");
            TRANS_IdleToWalk180R_MoveTree = mMotionController.AddAnimatorName("" + lLayer + ".WalkRunPivot v2-SM.IdleToWalk180R -> " + lLayer + ".WalkRunPivot v2-SM.Move Tree");
            TRANS_IdleToWalk180R_IdlePose = mMotionController.AddAnimatorName("" + lLayer + ".WalkRunPivot v2-SM.IdleToWalk180R -> " + lLayer + ".WalkRunPivot v2-SM.IdlePose");
            STATE_IdleToWalk180L = mMotionController.AddAnimatorName("" + lLayer + ".WalkRunPivot v2-SM.IdleToWalk180L");
            TRANS_IdleToWalk180L_MoveTree = mMotionController.AddAnimatorName("" + lLayer + ".WalkRunPivot v2-SM.IdleToWalk180L -> " + lLayer + ".WalkRunPivot v2-SM.Move Tree");
            TRANS_IdleToWalk180L_IdlePose = mMotionController.AddAnimatorName("" + lLayer + ".WalkRunPivot v2-SM.IdleToWalk180L -> " + lLayer + ".WalkRunPivot v2-SM.IdlePose");
            STATE_IdlePose = mMotionController.AddAnimatorName("" + lLayer + ".WalkRunPivot v2-SM.IdlePose");
            STATE_IdleToRun90L = mMotionController.AddAnimatorName("" + lLayer + ".WalkRunPivot v2-SM.IdleToRun90L");
            TRANS_IdleToRun90L_MoveTree = mMotionController.AddAnimatorName("" + lLayer + ".WalkRunPivot v2-SM.IdleToRun90L -> " + lLayer + ".WalkRunPivot v2-SM.Move Tree");
            TRANS_IdleToRun90L_IdlePose = mMotionController.AddAnimatorName("" + lLayer + ".WalkRunPivot v2-SM.IdleToRun90L -> " + lLayer + ".WalkRunPivot v2-SM.IdlePose");
            STATE_IdleToRun180L = mMotionController.AddAnimatorName("" + lLayer + ".WalkRunPivot v2-SM.IdleToRun180L");
            TRANS_IdleToRun180L_MoveTree = mMotionController.AddAnimatorName("" + lLayer + ".WalkRunPivot v2-SM.IdleToRun180L -> " + lLayer + ".WalkRunPivot v2-SM.Move Tree");
            TRANS_IdleToRun180L_IdlePose = mMotionController.AddAnimatorName("" + lLayer + ".WalkRunPivot v2-SM.IdleToRun180L -> " + lLayer + ".WalkRunPivot v2-SM.IdlePose");
            STATE_IdleToRun90R = mMotionController.AddAnimatorName("" + lLayer + ".WalkRunPivot v2-SM.IdleToRun90R");
            TRANS_IdleToRun90R_MoveTree = mMotionController.AddAnimatorName("" + lLayer + ".WalkRunPivot v2-SM.IdleToRun90R -> " + lLayer + ".WalkRunPivot v2-SM.Move Tree");
            TRANS_IdleToRun90R_IdlePose = mMotionController.AddAnimatorName("" + lLayer + ".WalkRunPivot v2-SM.IdleToRun90R -> " + lLayer + ".WalkRunPivot v2-SM.IdlePose");
            STATE_IdleToRun180R = mMotionController.AddAnimatorName("" + lLayer + ".WalkRunPivot v2-SM.IdleToRun180R");
            TRANS_IdleToRun180R_MoveTree = mMotionController.AddAnimatorName("" + lLayer + ".WalkRunPivot v2-SM.IdleToRun180R -> " + lLayer + ".WalkRunPivot v2-SM.Move Tree");
            TRANS_IdleToRun180R_IdlePose = mMotionController.AddAnimatorName("" + lLayer + ".WalkRunPivot v2-SM.IdleToRun180R -> " + lLayer + ".WalkRunPivot v2-SM.IdlePose");
            STATE_IdleToRun = mMotionController.AddAnimatorName("" + lLayer + ".WalkRunPivot v2-SM.IdleToRun");
            TRANS_IdleToRun_MoveTree = mMotionController.AddAnimatorName("" + lLayer + ".WalkRunPivot v2-SM.IdleToRun -> " + lLayer + ".WalkRunPivot v2-SM.Move Tree");
            TRANS_IdleToRun_IdlePose = mMotionController.AddAnimatorName("" + lLayer + ".WalkRunPivot v2-SM.IdleToRun -> " + lLayer + ".WalkRunPivot v2-SM.IdlePose");
            STATE_RunPivot180R_LDown = mMotionController.AddAnimatorName("" + lLayer + ".WalkRunPivot v2-SM.RunPivot180R_LDown");
            TRANS_RunPivot180R_LDown_MoveTree = mMotionController.AddAnimatorName("" + lLayer + ".WalkRunPivot v2-SM.RunPivot180R_LDown -> " + lLayer + ".WalkRunPivot v2-SM.Move Tree");
            STATE_WalkPivot180L = mMotionController.AddAnimatorName("" + lLayer + ".WalkRunPivot v2-SM.WalkPivot180L");
            TRANS_WalkPivot180L_MoveTree = mMotionController.AddAnimatorName("" + lLayer + ".WalkRunPivot v2-SM.WalkPivot180L -> " + lLayer + ".WalkRunPivot v2-SM.Move Tree");
            STATE_RunToIdle_LDown = mMotionController.AddAnimatorName("" + lLayer + ".WalkRunPivot v2-SM.RunToIdle_LDown");
            TRANS_RunToIdle_LDown_IdlePose = mMotionController.AddAnimatorName("" + lLayer + ".WalkRunPivot v2-SM.RunToIdle_LDown -> " + lLayer + ".WalkRunPivot v2-SM.IdlePose");
            TRANS_RunToIdle_LDown_MoveTree = mMotionController.AddAnimatorName("" + lLayer + ".WalkRunPivot v2-SM.RunToIdle_LDown -> " + lLayer + ".WalkRunPivot v2-SM.Move Tree");
            STATE_WalkToIdle_LDown = mMotionController.AddAnimatorName("" + lLayer + ".WalkRunPivot v2-SM.WalkToIdle_LDown");
            TRANS_WalkToIdle_LDown_MoveTree = mMotionController.AddAnimatorName("" + lLayer + ".WalkRunPivot v2-SM.WalkToIdle_LDown -> " + lLayer + ".WalkRunPivot v2-SM.Move Tree");
            TRANS_WalkToIdle_LDown_IdlePose = mMotionController.AddAnimatorName("" + lLayer + ".WalkRunPivot v2-SM.WalkToIdle_LDown -> " + lLayer + ".WalkRunPivot v2-SM.IdlePose");
            STATE_WalkToIdle_RDown = mMotionController.AddAnimatorName("" + lLayer + ".WalkRunPivot v2-SM.WalkToIdle_RDown");
            TRANS_WalkToIdle_RDown_MoveTree = mMotionController.AddAnimatorName("" + lLayer + ".WalkRunPivot v2-SM.WalkToIdle_RDown -> " + lLayer + ".WalkRunPivot v2-SM.Move Tree");
            TRANS_WalkToIdle_RDown_IdlePose = mMotionController.AddAnimatorName("" + lLayer + ".WalkRunPivot v2-SM.WalkToIdle_RDown -> " + lLayer + ".WalkRunPivot v2-SM.IdlePose");
            STATE_RunToIdle_RDown = mMotionController.AddAnimatorName("" + lLayer + ".WalkRunPivot v2-SM.RunToIdle_RDown");
            TRANS_RunToIdle_RDown_MoveTree = mMotionController.AddAnimatorName("" + lLayer + ".WalkRunPivot v2-SM.RunToIdle_RDown -> " + lLayer + ".WalkRunPivot v2-SM.Move Tree");
            TRANS_RunToIdle_RDown_IdlePose = mMotionController.AddAnimatorName("" + lLayer + ".WalkRunPivot v2-SM.RunToIdle_RDown -> " + lLayer + ".WalkRunPivot v2-SM.IdlePose");
            STATE_IdleTurn20R = mMotionController.AddAnimatorName("" + lLayer + ".WalkRunPivot v2-SM.IdleTurn20R");
            TRANS_IdleTurn20R_IdleTurnEndPose = mMotionController.AddAnimatorName("" + lLayer + ".WalkRunPivot v2-SM.IdleTurn20R -> " + lLayer + ".WalkRunPivot v2-SM.IdleTurnEndPose");
            STATE_IdleTurn90R = mMotionController.AddAnimatorName("" + lLayer + ".WalkRunPivot v2-SM.IdleTurn90R");
            TRANS_IdleTurn90R_IdleTurnEndPose = mMotionController.AddAnimatorName("" + lLayer + ".WalkRunPivot v2-SM.IdleTurn90R -> " + lLayer + ".WalkRunPivot v2-SM.IdleTurnEndPose");
            STATE_IdleTurn180R = mMotionController.AddAnimatorName("" + lLayer + ".WalkRunPivot v2-SM.IdleTurn180R");
            TRANS_IdleTurn180R_IdleTurnEndPose = mMotionController.AddAnimatorName("" + lLayer + ".WalkRunPivot v2-SM.IdleTurn180R -> " + lLayer + ".WalkRunPivot v2-SM.IdleTurnEndPose");
            STATE_IdleTurn20L = mMotionController.AddAnimatorName("" + lLayer + ".WalkRunPivot v2-SM.IdleTurn20L");
            TRANS_IdleTurn20L_IdleTurnEndPose = mMotionController.AddAnimatorName("" + lLayer + ".WalkRunPivot v2-SM.IdleTurn20L -> " + lLayer + ".WalkRunPivot v2-SM.IdleTurnEndPose");
            STATE_IdleTurn90L = mMotionController.AddAnimatorName("" + lLayer + ".WalkRunPivot v2-SM.IdleTurn90L");
            TRANS_IdleTurn90L_IdleTurnEndPose = mMotionController.AddAnimatorName("" + lLayer + ".WalkRunPivot v2-SM.IdleTurn90L -> " + lLayer + ".WalkRunPivot v2-SM.IdleTurnEndPose");
            STATE_IdleTurn180L = mMotionController.AddAnimatorName("" + lLayer + ".WalkRunPivot v2-SM.IdleTurn180L");
            TRANS_IdleTurn180L_IdleTurnEndPose = mMotionController.AddAnimatorName("" + lLayer + ".WalkRunPivot v2-SM.IdleTurn180L -> " + lLayer + ".WalkRunPivot v2-SM.IdleTurnEndPose");
            STATE_IdleTurnEndPose = mMotionController.AddAnimatorName("" + lLayer + ".WalkRunPivot v2-SM.IdleTurnEndPose");
            TRANS_IdleTurnEndPose_MoveTree = mMotionController.AddAnimatorName("" + lLayer + ".WalkRunPivot v2-SM.IdleTurnEndPose -> " + lLayer + ".WalkRunPivot v2-SM.Move Tree");
        }

#if UNITY_EDITOR

        private AnimationClip m15958 = null;
        private AnimationClip m22266 = null;
        private AnimationClip m15916 = null;
        private AnimationClip m25052 = null;
        private AnimationClip m25054 = null;
        private AnimationClip m25058 = null;
        private AnimationClip m25056 = null;
        private AnimationClip m21058 = null;
        private AnimationClip m21062 = null;
        private AnimationClip m21060 = null;
        private AnimationClip m21064 = null;
        private AnimationClip m15914 = null;
        private AnimationClip m20888 = null;
        private AnimationClip m25062 = null;
        private AnimationClip m20206 = null;
        private AnimationClip m25066 = null;
        private AnimationClip m25068 = null;
        private AnimationClip m23202 = null;
        private AnimationClip m15968 = null;
        private AnimationClip m15972 = null;
        private AnimationClip m15966 = null;
        private AnimationClip m15970 = null;

        /// <summary>
        /// Creates the animator substate machine for this motion.
        /// </summary>
        protected override void CreateStateMachine()
        {
            // Grab the root sm for the layer
            UnityEditor.Animations.AnimatorStateMachine lRootStateMachine = _EditorAnimatorController.layers[mMotionLayer.AnimatorLayerIndex].stateMachine;
            UnityEditor.Animations.AnimatorStateMachine lSM_60916 = _EditorAnimatorController.layers[mMotionLayer.AnimatorLayerIndex].stateMachine;
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

            UnityEditor.Animations.AnimatorStateMachine lSM_61478 = lRootSubStateMachine;
            if (lSM_61478 != null)
            {
                for (int i = lSM_61478.entryTransitions.Length - 1; i >= 0; i--)
                {
                    lSM_61478.RemoveEntryTransition(lSM_61478.entryTransitions[i]);
                }

                for (int i = lSM_61478.anyStateTransitions.Length - 1; i >= 0; i--)
                {
                    lSM_61478.RemoveAnyStateTransition(lSM_61478.anyStateTransitions[i]);
                }

                for (int i = lSM_61478.states.Length - 1; i >= 0; i--)
                {
                    lSM_61478.RemoveState(lSM_61478.states[i].state);
                }

                for (int i = lSM_61478.stateMachines.Length - 1; i >= 0; i--)
                {
                    lSM_61478.RemoveStateMachine(lSM_61478.stateMachines[i].stateMachine);
                }
            }
            else
            {
                lSM_61478 = lSM_60916.AddStateMachine(_EditorAnimatorSMName, new Vector3(624, -756, 0));
            }

            UnityEditor.Animations.AnimatorState lS_53778 = lSM_61478.AddState("Move Tree", new Vector3(240, 372, 0));
            lS_53778.speed = 1f;

            UnityEditor.Animations.BlendTree lM_37332 = CreateBlendTree("Move Blend Tree", _EditorAnimatorController, mMotionLayer.AnimatorLayerIndex);
            lM_37332.blendType = UnityEditor.Animations.BlendTreeType.Simple1D;
            lM_37332.blendParameter = "InputMagnitude";
            lM_37332.blendParameterY = "InputX";
#if !(UNITY_4_0 || UNITY_4_0_1 || UNITY_4_1 || UNITY_4_2 || UNITY_4_3 || UNITY_4_4 || UNITY_4_5 || UNITY_4_6 || UNITY_5_0 || UNITY_5_1 || UNITY_5_2 || UNITY_5_3)
            lM_37332.useAutomaticThresholds = true;
#endif
            lM_37332.AddChild(m15958, 0f);
            lM_37332.AddChild(m22266, 0.5f);
            lM_37332.AddChild(m15916, 1f);
            lS_53778.motion = lM_37332;

            UnityEditor.Animations.AnimatorState lS_57330 = lSM_61478.AddState("IdleToWalk90L", new Vector3(-180, 204, 0));
            lS_57330.speed = 1.7f;
            lS_57330.motion = m25052;

            UnityEditor.Animations.AnimatorState lS_55704 = lSM_61478.AddState("IdleToWalk90R", new Vector3(-180, 264, 0));
            lS_55704.speed = 1.7f;
            lS_55704.motion = m25054;

            UnityEditor.Animations.AnimatorState lS_53872 = lSM_61478.AddState("IdleToWalk180R", new Vector3(-180, 324, 0));
            lS_53872.speed = 1.7f;
            lS_53872.motion = m25058;

            UnityEditor.Animations.AnimatorState lS_56980 = lSM_61478.AddState("IdleToWalk180L", new Vector3(-180, 144, 0));
            lS_56980.speed = 1.7f;
            lS_56980.motion = m25056;

            UnityEditor.Animations.AnimatorState lS_60176 = lSM_61478.AddState("IdlePose", new Vector3(132, 216, 0));
            lS_60176.speed = 1f;
            lS_60176.motion = m15958;

            UnityEditor.Animations.AnimatorState lS_54262 = lSM_61478.AddState("IdleToRun90L", new Vector3(-168, 492, 0));
            lS_54262.speed = 1.5f;
            lS_54262.motion = m21058;

            UnityEditor.Animations.AnimatorState lS_54502 = lSM_61478.AddState("IdleToRun180L", new Vector3(-168, 432, 0));
            lS_54502.speed = 1.3f;
            lS_54502.motion = m21062;

            UnityEditor.Animations.AnimatorState lS_55312 = lSM_61478.AddState("IdleToRun90R", new Vector3(-168, 612, 0));
            lS_55312.speed = 1.5f;
            lS_55312.motion = m21060;

            UnityEditor.Animations.AnimatorState lS_58064 = lSM_61478.AddState("IdleToRun180R", new Vector3(-168, 672, 0));
            lS_58064.speed = 1.3f;
            lS_58064.motion = m21064;

            UnityEditor.Animations.AnimatorState lS_55270 = lSM_61478.AddState("IdleToRun", new Vector3(-168, 552, 0));
            lS_55270.speed = 2f;
            lS_55270.motion = m15914;

            UnityEditor.Animations.AnimatorState lS_56848 = lSM_61478.AddState("RunPivot180R_LDown", new Vector3(144, 564, 0));
            lS_56848.speed = 1.2f;
            lS_56848.motion = m20888;

            UnityEditor.Animations.AnimatorState lS_57898 = lSM_61478.AddState("WalkPivot180L", new Vector3(360, 564, 0));
            lS_57898.speed = 1.5f;
            lS_57898.motion = m25062;

            UnityEditor.Animations.AnimatorState lS_57848 = lSM_61478.AddState("RunToIdle_LDown", new Vector3(576, 336, 0));
            lS_57848.speed = 1f;
            lS_57848.motion = m20206;

            UnityEditor.Animations.AnimatorState lS_58420 = lSM_61478.AddState("WalkToIdle_LDown", new Vector3(576, 492, 0));
            lS_58420.speed = 1f;
            lS_58420.motion = m25066;

            UnityEditor.Animations.AnimatorState lS_57344 = lSM_61478.AddState("WalkToIdle_RDown", new Vector3(576, 420, 0));
            lS_57344.speed = 1f;
            lS_57344.motion = m25068;

            UnityEditor.Animations.AnimatorState lS_54074 = lSM_61478.AddState("RunToIdle_RDown", new Vector3(576, 264, 0));
            lS_54074.speed = 1f;
            lS_54074.motion = m23202;

            UnityEditor.Animations.AnimatorState lS_60548 = lSM_61478.AddState("IdleTurn20R", new Vector3(-720, 408, 0));
            lS_60548.speed = 1f;
            lS_60548.motion = m15968;

            UnityEditor.Animations.AnimatorState lS_55512 = lSM_61478.AddState("IdleTurn90R", new Vector3(-720, 468, 0));
            lS_55512.speed = 1.6f;
            lS_55512.motion = m15968;

            UnityEditor.Animations.AnimatorState lS_59512 = lSM_61478.AddState("IdleTurn180R", new Vector3(-720, 528, 0));
            lS_59512.speed = 1.4f;
            lS_59512.motion = m15972;

            UnityEditor.Animations.AnimatorState lS_55416 = lSM_61478.AddState("IdleTurn20L", new Vector3(-720, 348, 0));
            lS_55416.speed = 1f;
            lS_55416.motion = m15966;

            UnityEditor.Animations.AnimatorState lS_58368 = lSM_61478.AddState("IdleTurn90L", new Vector3(-720, 288, 0));
            lS_58368.speed = 1.6f;
            lS_58368.motion = m15966;

            UnityEditor.Animations.AnimatorState lS_56666 = lSM_61478.AddState("IdleTurn180L", new Vector3(-720, 228, 0));
            lS_56666.speed = 1.4f;
            lS_56666.motion = m15970;

            UnityEditor.Animations.AnimatorState lS_54788 = lSM_61478.AddState("IdleTurnEndPose", new Vector3(-984, 372, 0));
            lS_54788.speed = 1f;
            lS_54788.motion = m15958;

            // Create the transition from the any state. Note that 'AnyState' transitions have to be added to the root
            UnityEditor.Animations.AnimatorStateTransition lT_48510 = lRootStateMachine.AddAnyStateTransition(lS_57330);
            lT_48510.hasExitTime = false;
            lT_48510.hasFixedDuration = true;
            lT_48510.exitTime = 0.9f;
            lT_48510.duration = 0.1f;
            lT_48510.offset = 0f;
            lT_48510.mute = false;
            lT_48510.solo = false;
            lT_48510.canTransitionToSelf = true;
            lT_48510.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            lT_48510.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 27130f, "L" + mMotionLayer._AnimatorLayerIndex + "MotionPhase");
            lT_48510.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 0f, "L" + mMotionLayer._AnimatorLayerIndex + "MotionParameter");
            lT_48510.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Less, -20f, "InputAngleFromAvatar");
            lT_48510.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Greater, -160f, "InputAngleFromAvatar");
            lT_48510.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Less, 0.6f, "InputMagnitude");

            // Create the transition from the any state. Note that 'AnyState' transitions have to be added to the root
            UnityEditor.Animations.AnimatorStateTransition lT_47392 = lRootStateMachine.AddAnyStateTransition(lS_55704);
            lT_47392.hasExitTime = false;
            lT_47392.hasFixedDuration = true;
            lT_47392.exitTime = 0.9f;
            lT_47392.duration = 0.1f;
            lT_47392.offset = 0f;
            lT_47392.mute = false;
            lT_47392.solo = false;
            lT_47392.canTransitionToSelf = true;
            lT_47392.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            lT_47392.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 27130f, "L" + mMotionLayer._AnimatorLayerIndex + "MotionPhase");
            lT_47392.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 0f, "L" + mMotionLayer._AnimatorLayerIndex + "MotionParameter");
            lT_47392.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Greater, 20f, "InputAngleFromAvatar");
            lT_47392.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Less, 160f, "InputAngleFromAvatar");
            lT_47392.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Less, 0.6f, "InputMagnitude");

            // Create the transition from the any state. Note that 'AnyState' transitions have to be added to the root
            UnityEditor.Animations.AnimatorStateTransition lT_39848 = lRootStateMachine.AddAnyStateTransition(lS_53872);
            lT_39848.hasExitTime = false;
            lT_39848.hasFixedDuration = true;
            lT_39848.exitTime = 0.9f;
            lT_39848.duration = 0.1f;
            lT_39848.offset = 0f;
            lT_39848.mute = false;
            lT_39848.solo = false;
            lT_39848.canTransitionToSelf = true;
            lT_39848.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            lT_39848.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 27130f, "L" + mMotionLayer._AnimatorLayerIndex + "MotionPhase");
            lT_39848.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 0f, "L" + mMotionLayer._AnimatorLayerIndex + "MotionParameter");
            lT_39848.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Greater, 160f, "InputAngleFromAvatar");
            lT_39848.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Less, 0.6f, "InputMagnitude");

            // Create the transition from the any state. Note that 'AnyState' transitions have to be added to the root
            UnityEditor.Animations.AnimatorStateTransition lT_38716 = lRootStateMachine.AddAnyStateTransition(lS_53778);
            lT_38716.hasExitTime = false;
            lT_38716.hasFixedDuration = true;
            lT_38716.exitTime = 0.9f;
            lT_38716.duration = 0.1f;
            lT_38716.offset = 0f;
            lT_38716.mute = false;
            lT_38716.solo = false;
            lT_38716.canTransitionToSelf = true;
            lT_38716.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            lT_38716.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 27130f, "L" + mMotionLayer._AnimatorLayerIndex + "MotionPhase");
            lT_38716.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Greater, -20f, "InputAngleFromAvatar");
            lT_38716.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Less, 20f, "InputAngleFromAvatar");
            lT_38716.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Less, 0.6f, "InputMagnitude");

            // Create the transition from the any state. Note that 'AnyState' transitions have to be added to the root
            UnityEditor.Animations.AnimatorStateTransition lT_52498 = lRootStateMachine.AddAnyStateTransition(lS_56980);
            lT_52498.hasExitTime = false;
            lT_52498.hasFixedDuration = true;
            lT_52498.exitTime = 0.9f;
            lT_52498.duration = 0.1f;
            lT_52498.offset = 0f;
            lT_52498.mute = false;
            lT_52498.solo = false;
            lT_52498.canTransitionToSelf = true;
            lT_52498.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            lT_52498.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 27130f, "L" + mMotionLayer._AnimatorLayerIndex + "MotionPhase");
            lT_52498.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 0f, "L" + mMotionLayer._AnimatorLayerIndex + "MotionParameter");
            lT_52498.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Less, -160f, "InputAngleFromAvatar");
            lT_52498.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Less, 0.6f, "InputMagnitude");

            // Create the transition from the any state. Note that 'AnyState' transitions have to be added to the root
            UnityEditor.Animations.AnimatorStateTransition lT_52796 = lRootStateMachine.AddAnyStateTransition(lS_54502);
            lT_52796.hasExitTime = false;
            lT_52796.hasFixedDuration = true;
            lT_52796.exitTime = 0.9f;
            lT_52796.duration = 0.1f;
            lT_52796.offset = 0f;
            lT_52796.mute = false;
            lT_52796.solo = false;
            lT_52796.canTransitionToSelf = true;
            lT_52796.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            lT_52796.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 27130f, "L" + mMotionLayer._AnimatorLayerIndex + "MotionPhase");
            lT_52796.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 0f, "L" + mMotionLayer._AnimatorLayerIndex + "MotionParameter");
            lT_52796.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Less, -160f, "InputAngleFromAvatar");
            lT_52796.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Greater, 0.6f, "InputMagnitude");

            // Create the transition from the any state. Note that 'AnyState' transitions have to be added to the root
            UnityEditor.Animations.AnimatorStateTransition lT_53156 = lRootStateMachine.AddAnyStateTransition(lS_54262);
            lT_53156.hasExitTime = false;
            lT_53156.hasFixedDuration = true;
            lT_53156.exitTime = 0.9f;
            lT_53156.duration = 0.1f;
            lT_53156.offset = 0f;
            lT_53156.mute = false;
            lT_53156.solo = false;
            lT_53156.canTransitionToSelf = true;
            lT_53156.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            lT_53156.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 27130f, "L" + mMotionLayer._AnimatorLayerIndex + "MotionPhase");
            lT_53156.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 0f, "L" + mMotionLayer._AnimatorLayerIndex + "MotionParameter");
            lT_53156.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Less, -20f, "InputAngleFromAvatar");
            lT_53156.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Greater, -160f, "InputAngleFromAvatar");
            lT_53156.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Greater, 0.6f, "InputMagnitude");

            // Create the transition from the any state. Note that 'AnyState' transitions have to be added to the root
            UnityEditor.Animations.AnimatorStateTransition lT_40430 = lRootStateMachine.AddAnyStateTransition(lS_55312);
            lT_40430.hasExitTime = false;
            lT_40430.hasFixedDuration = true;
            lT_40430.exitTime = 0.9f;
            lT_40430.duration = 0.1f;
            lT_40430.offset = 0f;
            lT_40430.mute = false;
            lT_40430.solo = false;
            lT_40430.canTransitionToSelf = true;
            lT_40430.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            lT_40430.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 27130f, "L" + mMotionLayer._AnimatorLayerIndex + "MotionPhase");
            lT_40430.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 0f, "L" + mMotionLayer._AnimatorLayerIndex + "MotionParameter");
            lT_40430.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Greater, 20f, "InputAngleFromAvatar");
            lT_40430.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Less, 160f, "InputAngleFromAvatar");
            lT_40430.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Greater, 0.6f, "InputMagnitude");

            // Create the transition from the any state. Note that 'AnyState' transitions have to be added to the root
            UnityEditor.Animations.AnimatorStateTransition lT_49428 = lRootStateMachine.AddAnyStateTransition(lS_58064);
            lT_49428.hasExitTime = false;
            lT_49428.hasFixedDuration = true;
            lT_49428.exitTime = 0.9f;
            lT_49428.duration = 0.1f;
            lT_49428.offset = 0f;
            lT_49428.mute = false;
            lT_49428.solo = false;
            lT_49428.canTransitionToSelf = true;
            lT_49428.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            lT_49428.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 27130f, "L" + mMotionLayer._AnimatorLayerIndex + "MotionPhase");
            lT_49428.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 0f, "L" + mMotionLayer._AnimatorLayerIndex + "MotionParameter");
            lT_49428.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Greater, 160f, "InputAngleFromAvatar");
            lT_49428.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Greater, 0.6f, "InputMagnitude");

            // Create the transition from the any state. Note that 'AnyState' transitions have to be added to the root
            UnityEditor.Animations.AnimatorStateTransition lT_47900 = lRootStateMachine.AddAnyStateTransition(lS_55270);
            lT_47900.hasExitTime = false;
            lT_47900.hasFixedDuration = true;
            lT_47900.exitTime = 0.9f;
            lT_47900.duration = 0.1f;
            lT_47900.offset = 0f;
            lT_47900.mute = false;
            lT_47900.solo = false;
            lT_47900.canTransitionToSelf = true;
            lT_47900.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            lT_47900.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 27130f, "L" + mMotionLayer._AnimatorLayerIndex + "MotionPhase");
            lT_47900.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 0f, "L" + mMotionLayer._AnimatorLayerIndex + "MotionParameter");
            lT_47900.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Greater, -20f, "InputAngleFromAvatar");
            lT_47900.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Less, 20f, "InputAngleFromAvatar");
            lT_47900.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Greater, 0.6f, "InputMagnitude");

            // Create the transition from the any state. Note that 'AnyState' transitions have to be added to the root
            UnityEditor.Animations.AnimatorStateTransition lT_46304 = lRootStateMachine.AddAnyStateTransition(lS_53778);
            lT_46304.hasExitTime = false;
            lT_46304.hasFixedDuration = true;
            lT_46304.exitTime = 0.9f;
            lT_46304.duration = 0.1f;
            lT_46304.offset = 0.5f;
            lT_46304.mute = false;
            lT_46304.solo = false;
            lT_46304.canTransitionToSelf = true;
            lT_46304.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            lT_46304.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 27130f, "L" + mMotionLayer._AnimatorLayerIndex + "MotionPhase");
            lT_46304.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 2f, "L" + mMotionLayer._AnimatorLayerIndex + "MotionParameter");

            // Create the transition from the any state. Note that 'AnyState' transitions have to be added to the root
            UnityEditor.Animations.AnimatorStateTransition lT_53252 = lRootStateMachine.AddAnyStateTransition(lS_53778);
            lT_53252.hasExitTime = false;
            lT_53252.hasFixedDuration = true;
            lT_53252.exitTime = 0.9f;
            lT_53252.duration = 0.2f;
            lT_53252.offset = 0f;
            lT_53252.mute = false;
            lT_53252.solo = false;
            lT_53252.canTransitionToSelf = true;
            lT_53252.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            lT_53252.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 27130f, "L" + mMotionLayer._AnimatorLayerIndex + "MotionPhase");
            lT_53252.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 1f, "L" + mMotionLayer._AnimatorLayerIndex + "MotionParameter");

            // Create the transition from the any state. Note that 'AnyState' transitions have to be added to the root
            UnityEditor.Animations.AnimatorStateTransition lT_42670 = lRootStateMachine.AddAnyStateTransition(lS_56666);
            lT_42670.hasExitTime = false;
            lT_42670.hasFixedDuration = true;
            lT_42670.exitTime = 0.9f;
            lT_42670.duration = 0.05f;
            lT_42670.offset = 0.2228713f;
            lT_42670.mute = false;
            lT_42670.solo = false;
            lT_42670.canTransitionToSelf = true;
            lT_42670.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            lT_42670.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 27135f, "L" + mMotionLayer._AnimatorLayerIndex + "MotionPhase");
            lT_42670.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Less, -135f, "InputAngleFromAvatar");

            // Create the transition from the any state. Note that 'AnyState' transitions have to be added to the root
            UnityEditor.Animations.AnimatorStateTransition lT_51152 = lRootStateMachine.AddAnyStateTransition(lS_58368);
            lT_51152.hasExitTime = false;
            lT_51152.hasFixedDuration = true;
            lT_51152.exitTime = 0.9f;
            lT_51152.duration = 0.05f;
            lT_51152.offset = 0.1442637f;
            lT_51152.mute = false;
            lT_51152.solo = false;
            lT_51152.canTransitionToSelf = true;
            lT_51152.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            lT_51152.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 27135f, "L" + mMotionLayer._AnimatorLayerIndex + "MotionPhase");
            lT_51152.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Less, -45f, "InputAngleFromAvatar");
            lT_51152.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Greater, -135f, "InputAngleFromAvatar");

            // Create the transition from the any state. Note that 'AnyState' transitions have to be added to the root
            UnityEditor.Animations.AnimatorStateTransition lT_40364 = lRootStateMachine.AddAnyStateTransition(lS_55416);
            lT_40364.hasExitTime = false;
            lT_40364.hasFixedDuration = true;
            lT_40364.exitTime = 0.9f;
            lT_40364.duration = 0.05f;
            lT_40364.offset = 0.1442637f;
            lT_40364.mute = false;
            lT_40364.solo = false;
            lT_40364.canTransitionToSelf = true;
            lT_40364.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            lT_40364.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 27135f, "L" + mMotionLayer._AnimatorLayerIndex + "MotionPhase");
            lT_40364.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Less, 0f, "InputAngleFromAvatar");
            lT_40364.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Greater, -45f, "InputAngleFromAvatar");

            // Create the transition from the any state. Note that 'AnyState' transitions have to be added to the root
            UnityEditor.Animations.AnimatorStateTransition lT_42336 = lRootStateMachine.AddAnyStateTransition(lS_60548);
            lT_42336.hasExitTime = false;
            lT_42336.hasFixedDuration = true;
            lT_42336.exitTime = 0.9f;
            lT_42336.duration = 0.05f;
            lT_42336.offset = 0.2277291f;
            lT_42336.mute = false;
            lT_42336.solo = false;
            lT_42336.canTransitionToSelf = true;
            lT_42336.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            lT_42336.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 27135f, "L" + mMotionLayer._AnimatorLayerIndex + "MotionPhase");
            lT_42336.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Greater, 0f, "InputAngleFromAvatar");
            lT_42336.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Less, 45f, "InputAngleFromAvatar");

            // Create the transition from the any state. Note that 'AnyState' transitions have to be added to the root
            UnityEditor.Animations.AnimatorStateTransition lT_52062 = lRootStateMachine.AddAnyStateTransition(lS_55512);
            lT_52062.hasExitTime = false;
            lT_52062.hasFixedDuration = true;
            lT_52062.exitTime = 0.8999999f;
            lT_52062.duration = 0.05000001f;
            lT_52062.offset = 0.2277291f;
            lT_52062.mute = false;
            lT_52062.solo = false;
            lT_52062.canTransitionToSelf = true;
            lT_52062.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            lT_52062.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 27135f, "L" + mMotionLayer._AnimatorLayerIndex + "MotionPhase");
            lT_52062.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Greater, 45f, "InputAngleFromAvatar");
            lT_52062.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Less, 135f, "InputAngleFromAvatar");

            // Create the transition from the any state. Note that 'AnyState' transitions have to be added to the root
            UnityEditor.Animations.AnimatorStateTransition lT_48298 = lRootStateMachine.AddAnyStateTransition(lS_59512);
            lT_48298.hasExitTime = false;
            lT_48298.hasFixedDuration = true;
            lT_48298.exitTime = 0.9f;
            lT_48298.duration = 0.05f;
            lT_48298.offset = 0.2689505f;
            lT_48298.mute = false;
            lT_48298.solo = false;
            lT_48298.canTransitionToSelf = true;
            lT_48298.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            lT_48298.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 27135f, "L" + mMotionLayer._AnimatorLayerIndex + "MotionPhase");
            lT_48298.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Greater, 135f, "InputAngleFromAvatar");

            UnityEditor.Animations.AnimatorStateTransition lT_40206 = lS_53778.AddTransition(lS_56848);
            lT_40206.hasExitTime = false;
            lT_40206.hasFixedDuration = true;
            lT_40206.exitTime = 0.5481927f;
            lT_40206.duration = 0.1f;
            lT_40206.offset = 0f;
            lT_40206.mute = false;
            lT_40206.solo = false;
            lT_40206.canTransitionToSelf = true;
            lT_40206.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            lT_40206.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Greater, 160f, "InputAngleFromAvatar");
            lT_40206.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Greater, 0.6f, "InputMagnitude");

            UnityEditor.Animations.AnimatorStateTransition lT_48706 = lS_53778.AddTransition(lS_56848);
            lT_48706.hasExitTime = false;
            lT_48706.hasFixedDuration = true;
            lT_48706.exitTime = 0.5481927f;
            lT_48706.duration = 0.1f;
            lT_48706.offset = 0f;
            lT_48706.mute = false;
            lT_48706.solo = false;
            lT_48706.canTransitionToSelf = true;
            lT_48706.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            lT_48706.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Less, -160f, "InputAngleFromAvatar");
            lT_48706.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Greater, 0.6f, "InputMagnitude");

            UnityEditor.Animations.AnimatorStateTransition lT_47672 = lS_53778.AddTransition(lS_57898);
            lT_47672.hasExitTime = false;
            lT_47672.hasFixedDuration = true;
            lT_47672.exitTime = 0.5481927f;
            lT_47672.duration = 0.1f;
            lT_47672.offset = 0f;
            lT_47672.mute = false;
            lT_47672.solo = false;
            lT_47672.canTransitionToSelf = true;
            lT_47672.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            lT_47672.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Greater, 160f, "InputAngleFromAvatar");
            lT_47672.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Greater, 0.2f, "InputMagnitude");
            lT_47672.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Less, 0.6f, "InputMagnitude");

            UnityEditor.Animations.AnimatorStateTransition lT_45182 = lS_53778.AddTransition(lS_57898);
            lT_45182.hasExitTime = false;
            lT_45182.hasFixedDuration = true;
            lT_45182.exitTime = 0.5481927f;
            lT_45182.duration = 0.1f;
            lT_45182.offset = 0f;
            lT_45182.mute = false;
            lT_45182.solo = false;
            lT_45182.canTransitionToSelf = true;
            lT_45182.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            lT_45182.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Less, -160f, "InputAngleFromAvatar");
            lT_45182.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Greater, 0.2f, "InputMagnitude");
            lT_45182.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Less, 0.6f, "InputMagnitude");

            UnityEditor.Animations.AnimatorStateTransition lT_52126 = lS_53778.AddTransition(lS_57848);
            lT_52126.hasExitTime = true;
            lT_52126.hasFixedDuration = true;
            lT_52126.exitTime = 0.5f;
            lT_52126.duration = 0.2f;
            lT_52126.offset = 0.3595567f;
            lT_52126.mute = false;
            lT_52126.solo = false;
            lT_52126.canTransitionToSelf = true;
            lT_52126.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            lT_52126.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 27131f, "L" + mMotionLayer._AnimatorLayerIndex + "MotionPhase");

            UnityEditor.Animations.AnimatorStateTransition lT_50248 = lS_53778.AddTransition(lS_58420);
            lT_50248.hasExitTime = true;
            lT_50248.hasFixedDuration = true;
            lT_50248.exitTime = 0.5f;
            lT_50248.duration = 0.2f;
            lT_50248.offset = 0.5352634f;
            lT_50248.mute = false;
            lT_50248.solo = false;
            lT_50248.canTransitionToSelf = true;
            lT_50248.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            lT_50248.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 27132f, "L" + mMotionLayer._AnimatorLayerIndex + "MotionPhase");

            UnityEditor.Animations.AnimatorStateTransition lT_43448 = lS_53778.AddTransition(lS_54074);
            lT_43448.hasExitTime = true;
            lT_43448.hasFixedDuration = true;
            lT_43448.exitTime = 1f;
            lT_43448.duration = 0.2f;
            lT_43448.offset = 0f;
            lT_43448.mute = false;
            lT_43448.solo = false;
            lT_43448.canTransitionToSelf = true;
            lT_43448.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            lT_43448.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 27131f, "L" + mMotionLayer._AnimatorLayerIndex + "MotionPhase");

            UnityEditor.Animations.AnimatorStateTransition lT_51686 = lS_53778.AddTransition(lS_57344);
            lT_51686.hasExitTime = true;
            lT_51686.hasFixedDuration = true;
            lT_51686.exitTime = 1f;
            lT_51686.duration = 0.2f;
            lT_51686.offset = 0.4974566f;
            lT_51686.mute = false;
            lT_51686.solo = false;
            lT_51686.canTransitionToSelf = true;
            lT_51686.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            lT_51686.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 27132f, "L" + mMotionLayer._AnimatorLayerIndex + "MotionPhase");

            UnityEditor.Animations.AnimatorStateTransition lT_48986 = lS_53778.AddTransition(lS_54074);
            lT_48986.hasExitTime = true;
            lT_48986.hasFixedDuration = true;
            lT_48986.exitTime = 0.25f;
            lT_48986.duration = 0.2f;
            lT_48986.offset = 0.1060333f;
            lT_48986.mute = false;
            lT_48986.solo = false;
            lT_48986.canTransitionToSelf = true;
            lT_48986.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            lT_48986.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 27131f, "L" + mMotionLayer._AnimatorLayerIndex + "MotionPhase");

            UnityEditor.Animations.AnimatorStateTransition lT_41956 = lS_53778.AddTransition(lS_57848);
            lT_41956.hasExitTime = true;
            lT_41956.hasFixedDuration = true;
            lT_41956.exitTime = 0.75f;
            lT_41956.duration = 0.2f;
            lT_41956.offset = 0.4174516f;
            lT_41956.mute = false;
            lT_41956.solo = false;
            lT_41956.canTransitionToSelf = true;
            lT_41956.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            lT_41956.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 27131f, "L" + mMotionLayer._AnimatorLayerIndex + "MotionPhase");

            UnityEditor.Animations.AnimatorStateTransition lT_48756 = lS_53778.AddTransition(lS_57344);
            lT_48756.hasExitTime = true;
            lT_48756.hasFixedDuration = true;
            lT_48756.exitTime = 0.75f;
            lT_48756.duration = 0.2f;
            lT_48756.offset = 0.256667f;
            lT_48756.mute = false;
            lT_48756.solo = false;
            lT_48756.canTransitionToSelf = true;
            lT_48756.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            lT_48756.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 27132f, "L" + mMotionLayer._AnimatorLayerIndex + "MotionPhase");

            UnityEditor.Animations.AnimatorStateTransition lT_52606 = lS_53778.AddTransition(lS_58420);
            lT_52606.hasExitTime = true;
            lT_52606.hasFixedDuration = true;
            lT_52606.exitTime = 0.25f;
            lT_52606.duration = 0.2f;
            lT_52606.offset = 0.2689477f;
            lT_52606.mute = false;
            lT_52606.solo = false;
            lT_52606.canTransitionToSelf = true;
            lT_52606.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            lT_52606.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 27132f, "L" + mMotionLayer._AnimatorLayerIndex + "MotionPhase");

            UnityEditor.Animations.AnimatorStateTransition lT_44314 = lS_57330.AddTransition(lS_53778);
            lT_44314.hasExitTime = true;
            lT_44314.hasFixedDuration = true;
            lT_44314.exitTime = 0.75f;
            lT_44314.duration = 0.15f;
            lT_44314.offset = 0.0963606f;
            lT_44314.mute = false;
            lT_44314.solo = false;
            lT_44314.canTransitionToSelf = true;
            lT_44314.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            lT_44314.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Greater, 0.4f, "InputMagnitude");

            UnityEditor.Animations.AnimatorStateTransition lT_51096 = lS_57330.AddTransition(lS_60176);
            lT_51096.hasExitTime = true;
            lT_51096.hasFixedDuration = true;
            lT_51096.exitTime = 0.8404255f;
            lT_51096.duration = 0.25f;
            lT_51096.offset = 0f;
            lT_51096.mute = false;
            lT_51096.solo = false;
            lT_51096.canTransitionToSelf = true;
            lT_51096.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            lT_51096.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Less, 0.4f, "InputMagnitude");

            UnityEditor.Animations.AnimatorStateTransition lT_45840 = lS_55704.AddTransition(lS_53778);
            lT_45840.hasExitTime = true;
            lT_45840.hasFixedDuration = true;
            lT_45840.exitTime = 0.75f;
            lT_45840.duration = 0.15f;
            lT_45840.offset = 0.6026077f;
            lT_45840.mute = false;
            lT_45840.solo = false;
            lT_45840.canTransitionToSelf = true;
            lT_45840.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            lT_45840.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Greater, 0.4f, "InputMagnitude");

            UnityEditor.Animations.AnimatorStateTransition lT_38888 = lS_55704.AddTransition(lS_60176);
            lT_38888.hasExitTime = true;
            lT_38888.hasFixedDuration = true;
            lT_38888.exitTime = 0.7916668f;
            lT_38888.duration = 0.25f;
            lT_38888.offset = 0f;
            lT_38888.mute = false;
            lT_38888.solo = false;
            lT_38888.canTransitionToSelf = true;
            lT_38888.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            lT_38888.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Less, 0.4f, "InputMagnitude");

            UnityEditor.Animations.AnimatorStateTransition lT_42990 = lS_53872.AddTransition(lS_53778);
            lT_42990.hasExitTime = true;
            lT_42990.hasFixedDuration = true;
            lT_42990.exitTime = 0.8846154f;
            lT_42990.duration = 0.25f;
            lT_42990.offset = 0.8864383f;
            lT_42990.mute = false;
            lT_42990.solo = false;
            lT_42990.canTransitionToSelf = true;
            lT_42990.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            lT_42990.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Greater, 0.4f, "InputMagnitude");

            UnityEditor.Animations.AnimatorStateTransition lT_48686 = lS_53872.AddTransition(lS_60176);
            lT_48686.hasExitTime = true;
            lT_48686.hasFixedDuration = true;
            lT_48686.exitTime = 0.8584907f;
            lT_48686.duration = 0.25f;
            lT_48686.offset = 0f;
            lT_48686.mute = false;
            lT_48686.solo = false;
            lT_48686.canTransitionToSelf = true;
            lT_48686.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            lT_48686.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Less, 0.4f, "InputMagnitude");

            UnityEditor.Animations.AnimatorStateTransition lT_38302 = lS_56980.AddTransition(lS_53778);
            lT_38302.hasExitTime = true;
            lT_38302.hasFixedDuration = true;
            lT_38302.exitTime = 0.9074074f;
            lT_38302.duration = 0.25f;
            lT_38302.offset = 0.3468954f;
            lT_38302.mute = false;
            lT_38302.solo = false;
            lT_38302.canTransitionToSelf = true;
            lT_38302.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            lT_38302.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Greater, 0.4f, "InputMagnitude");

            UnityEditor.Animations.AnimatorStateTransition lT_44856 = lS_56980.AddTransition(lS_60176);
            lT_44856.hasExitTime = true;
            lT_44856.hasFixedDuration = true;
            lT_44856.exitTime = 0.8584907f;
            lT_44856.duration = 0.25f;
            lT_44856.offset = 0f;
            lT_44856.mute = false;
            lT_44856.solo = false;
            lT_44856.canTransitionToSelf = true;
            lT_44856.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            lT_44856.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Less, 0.4f, "InputMagnitude");

            UnityEditor.Animations.AnimatorStateTransition lT_39370 = lS_54262.AddTransition(lS_53778);
            lT_39370.hasExitTime = true;
            lT_39370.hasFixedDuration = true;
            lT_39370.exitTime = 0.7222224f;
            lT_39370.duration = 0.25f;
            lT_39370.offset = 0f;
            lT_39370.mute = false;
            lT_39370.solo = false;
            lT_39370.canTransitionToSelf = true;
            lT_39370.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;

            UnityEditor.Animations.AnimatorStateTransition lT_51476 = lS_54262.AddTransition(lS_60176);
            lT_51476.hasExitTime = true;
            lT_51476.hasFixedDuration = true;
            lT_51476.exitTime = 0.7794119f;
            lT_51476.duration = 0.25f;
            lT_51476.offset = 0f;
            lT_51476.mute = false;
            lT_51476.solo = false;
            lT_51476.canTransitionToSelf = true;
            lT_51476.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            lT_51476.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Less, 0.4f, "InputMagnitude");

            UnityEditor.Animations.AnimatorStateTransition lT_42542 = lS_54502.AddTransition(lS_53778);
            lT_42542.hasExitTime = true;
            lT_42542.hasFixedDuration = true;
            lT_42542.exitTime = 0.7580653f;
            lT_42542.duration = 0.25f;
            lT_42542.offset = 0f;
            lT_42542.mute = false;
            lT_42542.solo = false;
            lT_42542.canTransitionToSelf = true;
            lT_42542.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;

            UnityEditor.Animations.AnimatorStateTransition lT_50646 = lS_54502.AddTransition(lS_60176);
            lT_50646.hasExitTime = true;
            lT_50646.hasFixedDuration = true;
            lT_50646.exitTime = 0.8125004f;
            lT_50646.duration = 0.25f;
            lT_50646.offset = 0f;
            lT_50646.mute = false;
            lT_50646.solo = false;
            lT_50646.canTransitionToSelf = true;
            lT_50646.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            lT_50646.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Less, 0.4f, "InputMagnitude");

            UnityEditor.Animations.AnimatorStateTransition lT_45838 = lS_55312.AddTransition(lS_53778);
            lT_45838.hasExitTime = true;
            lT_45838.hasFixedDuration = true;
            lT_45838.exitTime = 0.7580646f;
            lT_45838.duration = 0.25f;
            lT_45838.offset = 0.5379788f;
            lT_45838.mute = false;
            lT_45838.solo = false;
            lT_45838.canTransitionToSelf = true;
            lT_45838.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;

            UnityEditor.Animations.AnimatorStateTransition lT_52388 = lS_55312.AddTransition(lS_60176);
            lT_52388.hasExitTime = true;
            lT_52388.hasFixedDuration = true;
            lT_52388.exitTime = 0.7794119f;
            lT_52388.duration = 0.25f;
            lT_52388.offset = 0f;
            lT_52388.mute = false;
            lT_52388.solo = false;
            lT_52388.canTransitionToSelf = true;
            lT_52388.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            lT_52388.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Less, 0.4f, "InputMagnitude");

            UnityEditor.Animations.AnimatorStateTransition lT_50110 = lS_58064.AddTransition(lS_53778);
            lT_50110.hasExitTime = true;
            lT_50110.hasFixedDuration = true;
            lT_50110.exitTime = 0.8255816f;
            lT_50110.duration = 0.25f;
            lT_50110.offset = 0.5181294f;
            lT_50110.mute = false;
            lT_50110.solo = false;
            lT_50110.canTransitionToSelf = true;
            lT_50110.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;

            UnityEditor.Animations.AnimatorStateTransition lT_50046 = lS_58064.AddTransition(lS_60176);
            lT_50046.hasExitTime = true;
            lT_50046.hasFixedDuration = true;
            lT_50046.exitTime = 0.8125004f;
            lT_50046.duration = 0.25f;
            lT_50046.offset = 0f;
            lT_50046.mute = false;
            lT_50046.solo = false;
            lT_50046.canTransitionToSelf = true;
            lT_50046.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            lT_50046.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Less, 0.4f, "InputMagnitude");

            UnityEditor.Animations.AnimatorStateTransition lT_45648 = lS_55270.AddTransition(lS_53778);
            lT_45648.hasExitTime = true;
            lT_45648.hasFixedDuration = true;
            lT_45648.exitTime = 0.6182807f;
            lT_45648.duration = 0.25f;
            lT_45648.offset = 0.02634108f;
            lT_45648.mute = false;
            lT_45648.solo = false;
            lT_45648.canTransitionToSelf = true;
            lT_45648.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;

            UnityEditor.Animations.AnimatorStateTransition lT_45336 = lS_55270.AddTransition(lS_60176);
            lT_45336.hasExitTime = true;
            lT_45336.hasFixedDuration = true;
            lT_45336.exitTime = 0.6250002f;
            lT_45336.duration = 0.25f;
            lT_45336.offset = 0f;
            lT_45336.mute = false;
            lT_45336.solo = false;
            lT_45336.canTransitionToSelf = true;
            lT_45336.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            lT_45336.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Less, 0.4f, "InputMagnitude");

            UnityEditor.Animations.AnimatorStateTransition lT_43338 = lS_56848.AddTransition(lS_53778);
            lT_43338.hasExitTime = true;
            lT_43338.hasFixedDuration = true;
            lT_43338.exitTime = 0.8469388f;
            lT_43338.duration = 0.25f;
            lT_43338.offset = 0f;
            lT_43338.mute = false;
            lT_43338.solo = false;
            lT_43338.canTransitionToSelf = true;
            lT_43338.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;

            UnityEditor.Animations.AnimatorStateTransition lT_43006 = lS_57898.AddTransition(lS_53778);
            lT_43006.hasExitTime = true;
            lT_43006.hasFixedDuration = true;
            lT_43006.exitTime = 0.8636364f;
            lT_43006.duration = 0.25f;
            lT_43006.offset = 0.8593867f;
            lT_43006.mute = false;
            lT_43006.solo = false;
            lT_43006.canTransitionToSelf = true;
            lT_43006.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;

            UnityEditor.Animations.AnimatorStateTransition lT_48278 = lS_57848.AddTransition(lS_60176);
            lT_48278.hasExitTime = true;
            lT_48278.hasFixedDuration = true;
            lT_48278.exitTime = 0.7f;
            lT_48278.duration = 0.2f;
            lT_48278.offset = 0f;
            lT_48278.mute = false;
            lT_48278.solo = false;
            lT_48278.canTransitionToSelf = true;
            lT_48278.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;

            UnityEditor.Animations.AnimatorStateTransition lT_43670 = lS_57848.AddTransition(lS_53778);
            lT_43670.hasExitTime = false;
            lT_43670.hasFixedDuration = true;
            lT_43670.exitTime = 0.8684211f;
            lT_43670.duration = 0.25f;
            lT_43670.offset = 0f;
            lT_43670.mute = false;
            lT_43670.solo = false;
            lT_43670.canTransitionToSelf = true;
            lT_43670.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            lT_43670.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 27133f, "L" + mMotionLayer._AnimatorLayerIndex + "MotionPhase");

            UnityEditor.Animations.AnimatorStateTransition lT_42258 = lS_58420.AddTransition(lS_53778);
            lT_42258.hasExitTime = false;
            lT_42258.hasFixedDuration = true;
            lT_42258.exitTime = 0.75f;
            lT_42258.duration = 0.25f;
            lT_42258.offset = 0f;
            lT_42258.mute = false;
            lT_42258.solo = false;
            lT_42258.canTransitionToSelf = true;
            lT_42258.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            lT_42258.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 27133f, "L" + mMotionLayer._AnimatorLayerIndex + "MotionPhase");

            UnityEditor.Animations.AnimatorStateTransition lT_50200 = lS_58420.AddTransition(lS_60176);
            lT_50200.hasExitTime = true;
            lT_50200.hasFixedDuration = true;
            lT_50200.exitTime = 0.8f;
            lT_50200.duration = 0.2f;
            lT_50200.offset = 0f;
            lT_50200.mute = false;
            lT_50200.solo = false;
            lT_50200.canTransitionToSelf = true;
            lT_50200.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;

            UnityEditor.Animations.AnimatorStateTransition lT_45910 = lS_57344.AddTransition(lS_53778);
            lT_45910.hasExitTime = false;
            lT_45910.hasFixedDuration = true;
            lT_45910.exitTime = 0.75f;
            lT_45910.duration = 0.25f;
            lT_45910.offset = 0f;
            lT_45910.mute = false;
            lT_45910.solo = false;
            lT_45910.canTransitionToSelf = true;
            lT_45910.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            lT_45910.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 27133f, "L" + mMotionLayer._AnimatorLayerIndex + "MotionPhase");

            UnityEditor.Animations.AnimatorStateTransition lT_37946 = lS_57344.AddTransition(lS_60176);
            lT_37946.hasExitTime = true;
            lT_37946.hasFixedDuration = true;
            lT_37946.exitTime = 0.8f;
            lT_37946.duration = 0.2f;
            lT_37946.offset = 0f;
            lT_37946.mute = false;
            lT_37946.solo = false;
            lT_37946.canTransitionToSelf = true;
            lT_37946.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;

            UnityEditor.Animations.AnimatorStateTransition lT_51316 = lS_54074.AddTransition(lS_53778);
            lT_51316.hasExitTime = false;
            lT_51316.hasFixedDuration = true;
            lT_51316.exitTime = 0.8170732f;
            lT_51316.duration = 0.25f;
            lT_51316.offset = 0f;
            lT_51316.mute = false;
            lT_51316.solo = false;
            lT_51316.canTransitionToSelf = true;
            lT_51316.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            lT_51316.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 27133f, "L" + mMotionLayer._AnimatorLayerIndex + "MotionPhase");

            UnityEditor.Animations.AnimatorStateTransition lT_40198 = lS_54074.AddTransition(lS_60176);
            lT_40198.hasExitTime = true;
            lT_40198.hasFixedDuration = true;
            lT_40198.exitTime = 0.5021765f;
            lT_40198.duration = 0.1999999f;
            lT_40198.offset = 0.04457206f;
            lT_40198.mute = false;
            lT_40198.solo = false;
            lT_40198.canTransitionToSelf = true;
            lT_40198.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;

            UnityEditor.Animations.AnimatorStateTransition lT_48638 = lS_60548.AddTransition(lS_54788);
            lT_48638.hasExitTime = true;
            lT_48638.hasFixedDuration = true;
            lT_48638.exitTime = 0.3138752f;
            lT_48638.duration = 0.15f;
            lT_48638.offset = 0f;
            lT_48638.mute = false;
            lT_48638.solo = false;
            lT_48638.canTransitionToSelf = true;
            lT_48638.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;

            UnityEditor.Animations.AnimatorStateTransition lT_44442 = lS_55512.AddTransition(lS_54788);
            lT_44442.hasExitTime = true;
            lT_44442.hasFixedDuration = true;
            lT_44442.exitTime = 0.5643811f;
            lT_44442.duration = 0.15f;
            lT_44442.offset = 0f;
            lT_44442.mute = false;
            lT_44442.solo = false;
            lT_44442.canTransitionToSelf = true;
            lT_44442.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;

            UnityEditor.Animations.AnimatorStateTransition lT_46438 = lS_59512.AddTransition(lS_54788);
            lT_46438.hasExitTime = true;
            lT_46438.hasFixedDuration = true;
            lT_46438.exitTime = 0.7016318f;
            lT_46438.duration = 0.15f;
            lT_46438.offset = 0f;
            lT_46438.mute = false;
            lT_46438.solo = false;
            lT_46438.canTransitionToSelf = true;
            lT_46438.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;

            UnityEditor.Animations.AnimatorStateTransition lT_53692 = lS_55416.AddTransition(lS_54788);
            lT_53692.hasExitTime = true;
            lT_53692.hasFixedDuration = true;
            lT_53692.exitTime = 0.2468245f;
            lT_53692.duration = 0.15f;
            lT_53692.offset = 0f;
            lT_53692.mute = false;
            lT_53692.solo = false;
            lT_53692.canTransitionToSelf = true;
            lT_53692.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;

            UnityEditor.Animations.AnimatorStateTransition lT_46694 = lS_58368.AddTransition(lS_54788);
            lT_46694.hasExitTime = true;
            lT_46694.hasFixedDuration = true;
            lT_46694.exitTime = 0.5180793f;
            lT_46694.duration = 0.15f;
            lT_46694.offset = 0f;
            lT_46694.mute = false;
            lT_46694.solo = false;
            lT_46694.canTransitionToSelf = true;
            lT_46694.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;

            UnityEditor.Animations.AnimatorStateTransition lT_41664 = lS_56666.AddTransition(lS_54788);
            lT_41664.hasExitTime = true;
            lT_41664.hasFixedDuration = true;
            lT_41664.exitTime = 0.6774405f;
            lT_41664.duration = 0.15f;
            lT_41664.offset = 0f;
            lT_41664.mute = false;
            lT_41664.solo = false;
            lT_41664.canTransitionToSelf = true;
            lT_41664.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;

            UnityEditor.Animations.AnimatorStateTransition lT_45322 = lS_54788.AddTransition(lS_53778);
            lT_45322.hasExitTime = false;
            lT_45322.hasFixedDuration = true;
            lT_45322.exitTime = 0f;
            lT_45322.duration = 0.1f;
            lT_45322.offset = 0f;
            lT_45322.mute = false;
            lT_45322.solo = false;
            lT_45322.canTransitionToSelf = true;
            lT_45322.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            lT_45322.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Greater, 0.4f, "InputMagnitude");

        }

        /// <summary>
        /// Gathers the animations so we can use them when creating the sub-state machine.
        /// </summary>
        public override void FindAnimations()
        {
            m15958 = FindAnimationClip("Assets/ootii/MotionController/Content/Animations/Humanoid/Idling/unity_Idle_IdleToIdlesR.fbx/IdlePose.anim", "IdlePose");
            m22266 = FindAnimationClip("Assets/ootii/MotionController/Content/Animations/Humanoid/Walking/unity_WalkFWD_v2.fbx/WalkForward.anim", "WalkForward");
            m15916 = FindAnimationClip("Assets/ootii/MotionController/Content/Animations/Humanoid/Running/RunForward_v2.fbx/RunForward.anim", "RunForward");
            m25052 = FindAnimationClip("Assets/ootii/MotionController/Content/Animations/Humanoid/Walking/unity_Idle2walk_v2.fbx/IdleToWalk90L.anim", "IdleToWalk90L");
            m25054 = FindAnimationClip("Assets/ootii/MotionController/Content/Animations/Humanoid/Walking/unity_Idle2walk_v2.fbx/IdleToWalk90R.anim", "IdleToWalk90R");
            m25058 = FindAnimationClip("Assets/ootii/MotionController/Content/Animations/Humanoid/Walking/unity_Idle2walk_v2.fbx/IdleToWalk180R.anim", "IdleToWalk180R");
            m25056 = FindAnimationClip("Assets/ootii/MotionController/Content/Animations/Humanoid/Walking/unity_Idle2walk_v2.fbx/IdleToWalk180L.anim", "IdleToWalk180L");
            m21058 = FindAnimationClip("Assets/ootii/MotionController/Content/Animations/Humanoid/Running/unity_Idle2Run_v2.fbx/IdleToRun90L.anim", "IdleToRun90L");
            m21062 = FindAnimationClip("Assets/ootii/MotionController/Content/Animations/Humanoid/Running/unity_Idle2Run_v2.fbx/IdleToRun180L.anim", "IdleToRun180L");
            m21060 = FindAnimationClip("Assets/ootii/MotionController/Content/Animations/Humanoid/Running/unity_Idle2Run_v2.fbx/IdleToRun90R.anim", "IdleToRun90R");
            m21064 = FindAnimationClip("Assets/ootii/MotionController/Content/Animations/Humanoid/Running/unity_Idle2Run_v2.fbx/IdleToRun180R.anim", "IdleToRun180R");
            m15914 = FindAnimationClip("Assets/ootii/MotionController/Content/Animations/Humanoid/Running/RunForward_v2.fbx/IdleToRun.anim", "IdleToRun");
            m20888 = FindAnimationClip("Assets/ootii/MotionController/Content/Animations/Humanoid/Running/unity_PlantNTurn180_Run_R_1.fbx/RunPivot180R_LDown.anim", "RunPivot180R_LDown");
            m25062 = FindAnimationClip("Assets/ootii/MotionController/Content/Animations/Humanoid/Walking/unity_Idle2walk_v2.fbx/WalkPivot180L.anim", "WalkPivot180L");
            m20206 = FindAnimationClip("Assets/ootii/MotionController/Content/Animations/Humanoid/Running/unity_PlantNTurn180_Run_R_2.fbx/RunToIdle_LDown.anim", "RunToIdle_LDown");
            m25066 = FindAnimationClip("Assets/ootii/MotionController/Content/Animations/Humanoid/Walking/unity_Idle2walk_v2.fbx/WalkToIdle_LDown.anim", "WalkToIdle_LDown");
            m25068 = FindAnimationClip("Assets/ootii/MotionController/Content/Animations/Humanoid/Walking/unity_Idle2walk_v2.fbx/WalkToIdle_RDown.anim", "WalkToIdle_RDown");
            m23202 = FindAnimationClip("Assets/ootii/MotionController/Content/Animations/Humanoid/Running/unity_HalfSteps2Idle_PasingLongStepTOIdle.fbx/RunToIdle_RDown.anim", "RunToIdle_RDown");
            m15968 = FindAnimationClip("Assets/ootii/MotionController/Content/Animations/Humanoid/Idling/unity_Idle_IdleToIdlesR.fbx/IdleTurn90R.anim", "IdleTurn90R");
            m15972 = FindAnimationClip("Assets/ootii/MotionController/Content/Animations/Humanoid/Idling/unity_Idle_IdleToIdlesR.fbx/IdleTurn180R.anim", "IdleTurn180R");
            m15966 = FindAnimationClip("Assets/ootii/MotionController/Content/Animations/Humanoid/Idling/unity_Idle_IdleToIdlesR.fbx/IdleTurn90L.anim", "IdleTurn90L");
            m15970 = FindAnimationClip("Assets/ootii/MotionController/Content/Animations/Humanoid/Idling/unity_Idle_IdleToIdlesR.fbx/IdleTurn180L.anim", "IdleTurn180L");

            // Add the remaining functionality
            base.FindAnimations();
        }

        /// <summary>
        /// Used to show the settings that allow us to generate the animator setup.
        /// </summary>
        public override void OnSettingsGUI()
        {
            UnityEditor.EditorGUILayout.IntField(new GUIContent("Phase ID", "Phase ID used to transition to the state."), PHASE_START);
            m15958 = CreateAnimationField("Start.IdlePose", "Assets/ootii/MotionController/Content/Animations/Humanoid/Idling/unity_Idle_IdleToIdlesR.fbx/IdlePose.anim", "IdlePose", m15958);
            m22266 = CreateAnimationField("Move Tree.WalkForward", "Assets/ootii/MotionController/Content/Animations/Humanoid/Walking/unity_WalkFWD_v2.fbx/WalkForward.anim", "WalkForward", m22266);
            m15916 = CreateAnimationField("Move Tree.RunForward", "Assets/ootii/MotionController/Content/Animations/Humanoid/Running/RunForward_v2.fbx/RunForward.anim", "RunForward", m15916);
            m25052 = CreateAnimationField("IdleToWalk90L", "Assets/ootii/MotionController/Content/Animations/Humanoid/Walking/unity_Idle2walk_v2.fbx/IdleToWalk90L.anim", "IdleToWalk90L", m25052);
            m25054 = CreateAnimationField("IdleToWalk90R", "Assets/ootii/MotionController/Content/Animations/Humanoid/Walking/unity_Idle2walk_v2.fbx/IdleToWalk90R.anim", "IdleToWalk90R", m25054);
            m25058 = CreateAnimationField("IdleToWalk180R", "Assets/ootii/MotionController/Content/Animations/Humanoid/Walking/unity_Idle2walk_v2.fbx/IdleToWalk180R.anim", "IdleToWalk180R", m25058);
            m25056 = CreateAnimationField("IdleToWalk180L", "Assets/ootii/MotionController/Content/Animations/Humanoid/Walking/unity_Idle2walk_v2.fbx/IdleToWalk180L.anim", "IdleToWalk180L", m25056);
            m21058 = CreateAnimationField("IdleToRun90L", "Assets/ootii/MotionController/Content/Animations/Humanoid/Running/unity_Idle2Run_v2.fbx/IdleToRun90L.anim", "IdleToRun90L", m21058);
            m21062 = CreateAnimationField("IdleToRun180L", "Assets/ootii/MotionController/Content/Animations/Humanoid/Running/unity_Idle2Run_v2.fbx/IdleToRun180L.anim", "IdleToRun180L", m21062);
            m21060 = CreateAnimationField("IdleToRun90R", "Assets/ootii/MotionController/Content/Animations/Humanoid/Running/unity_Idle2Run_v2.fbx/IdleToRun90R.anim", "IdleToRun90R", m21060);
            m21064 = CreateAnimationField("IdleToRun180R", "Assets/ootii/MotionController/Content/Animations/Humanoid/Running/unity_Idle2Run_v2.fbx/IdleToRun180R.anim", "IdleToRun180R", m21064);
            m15914 = CreateAnimationField("IdleToRun", "Assets/ootii/MotionController/Content/Animations/Humanoid/Running/RunForward_v2.fbx/IdleToRun.anim", "IdleToRun", m15914);
            m20888 = CreateAnimationField("RunPivot180R_LDown", "Assets/ootii/MotionController/Content/Animations/Humanoid/Running/unity_PlantNTurn180_Run_R_1.fbx/RunPivot180R_LDown.anim", "RunPivot180R_LDown", m20888);
            m25062 = CreateAnimationField("WalkPivot180L", "Assets/ootii/MotionController/Content/Animations/Humanoid/Walking/unity_Idle2walk_v2.fbx/WalkPivot180L.anim", "WalkPivot180L", m25062);
            m20206 = CreateAnimationField("RunToIdle_LDown", "Assets/ootii/MotionController/Content/Animations/Humanoid/Running/unity_PlantNTurn180_Run_R_2.fbx/RunToIdle_LDown.anim", "RunToIdle_LDown", m20206);
            m25066 = CreateAnimationField("WalkToIdle_LDown", "Assets/ootii/MotionController/Content/Animations/Humanoid/Walking/unity_Idle2walk_v2.fbx/WalkToIdle_LDown.anim", "WalkToIdle_LDown", m25066);
            m25068 = CreateAnimationField("WalkToIdle_RDown", "Assets/ootii/MotionController/Content/Animations/Humanoid/Walking/unity_Idle2walk_v2.fbx/WalkToIdle_RDown.anim", "WalkToIdle_RDown", m25068);
            m23202 = CreateAnimationField("RunToIdle_RDown", "Assets/ootii/MotionController/Content/Animations/Humanoid/Running/unity_HalfSteps2Idle_PasingLongStepTOIdle.fbx/RunToIdle_RDown.anim", "RunToIdle_RDown", m23202);
            m15968 = CreateAnimationField("IdleTurn20R.IdleTurn90R", "Assets/ootii/MotionController/Content/Animations/Humanoid/Idling/unity_Idle_IdleToIdlesR.fbx/IdleTurn90R.anim", "IdleTurn90R", m15968);
            m15972 = CreateAnimationField("IdleTurn180R", "Assets/ootii/MotionController/Content/Animations/Humanoid/Idling/unity_Idle_IdleToIdlesR.fbx/IdleTurn180R.anim", "IdleTurn180R", m15972);
            m15966 = CreateAnimationField("IdleTurn20L.IdleTurn90L", "Assets/ootii/MotionController/Content/Animations/Humanoid/Idling/unity_Idle_IdleToIdlesR.fbx/IdleTurn90L.anim", "IdleTurn90L", m15966);
            m15970 = CreateAnimationField("IdleTurn180L", "Assets/ootii/MotionController/Content/Animations/Humanoid/Idling/unity_Idle_IdleToIdlesR.fbx/IdleTurn180L.anim", "IdleTurn180L", m15970);

            // Add the remaining functionality
            base.OnSettingsGUI();
        }

#endif

        // ************************************ END AUTO GENERATED ************************************
        #endregion
    }
}
