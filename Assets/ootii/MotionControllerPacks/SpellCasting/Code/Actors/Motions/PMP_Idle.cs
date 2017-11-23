using UnityEngine;
using com.ootii.Actors.AnimationControllers;
using com.ootii.Actors.Combat;
using com.ootii.Actors.Magic;
using com.ootii.Cameras;
using com.ootii.Helpers;
using com.ootii.Geometry;
using com.ootii.Timing;

#if UNITY_EDITOR
using UnityEditor;
using com.ootii.Actors.Attributes;
using com.ootii.Actors.Inventory;
using com.ootii.Actors.LifeCores;
#endif

namespace com.ootii.MotionControllerPacks
{
    /// <summary>
    /// Very basic idle while magic is ready. There is no rotations.
    /// </summary>
    [MotionName("PMP - Idle")]
    [MotionDescription("Standard idle motion while magic is ready.")]
    public class PMP_Idle : PMP_MotionBase
    {
        // Enum values for the motion
        public const int PHASE_UNKNOWN = 0;
        public const int PHASE_START = 32100;

        /// <summary>
        /// Determines if we rotate by ourselves
        /// </summary>
        public bool _RotateWithInput = false;
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
        /// Determines if use the pivot animations while idle
        /// </summary>
        public bool _UsePivotAnimations = true;
        public bool UsePivotAnimations
        {
            get { return _UsePivotAnimations; }
            set { _UsePivotAnimations = value; }
        }

        /// <summary>
        /// Speed we'll actually apply to the rotation. This is essencially the
        /// number of degrees per tick assuming we're running at 60 FPS
        /// </summary>
        protected float mDegreesPer60FPSTick = 1f;

        /// <summary>
        /// Fields to help smooth out the mouse rotation
        /// </summary>
        protected float mYaw = 0f;
        protected float mYawTarget = 0f;
        protected float mYawVelocity = 0f;

        /// <summary>
        /// Determines if the actor rotation should be linked to the camera
        /// </summary>
        protected bool mLinkRotation = false;

        /// <summary>
        /// Default constructor
        /// </summary>
        public PMP_Idle()
            : base()
        {
            _Category = EnumMotionCategories.IDLE;
            _ActionAlias = "Spell Casting Stance";

            _Priority = 1;

#if UNITY_EDITOR
            if (_EditorAnimatorSMName.Length == 0) { _EditorAnimatorSMName = "PMP_Idle-SM"; }
#endif
        }

        /// <summary>
        /// Controller constructor
        /// </summary>
        /// <param name="rController">Controller the motion belongs to</param>
        public PMP_Idle(MotionController rController)
            : base(rController)
        {
            _Category = EnumMotionCategories.IDLE;
            _ActionAlias = "Spell Casting Stance";

            _Priority = 1;

#if UNITY_EDITOR
            if (_EditorAnimatorSMName.Length == 0) { _EditorAnimatorSMName = "PMP_Idle-SM"; }
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
            if (!mIsStartable) { return false; }
            if (!mMotionController.IsGrounded) { return false; }

            // If we're not in spell casting mode, we may get in
            if (mActorController.State.Stance != EnumControllerStance.SPELL_CASTING)
            {
                if (_ActionAlias.Length > 0 && mMotionController._InputSource != null)
                {
                    if (mMotionController._InputSource.IsJustPressed(_ActionAlias))
                    {
                        return true;
                    }
                }

                // Exit
                return false;
            }

            // This is a catch all. If there are no motions found to match
            // the controller's state, we default to this motion.
            if (mMotionLayer.ActiveMotion == null)
            {
                return true;
            }

            // Return the final result
            return false;
        }

        /// <summary>
        /// Tests if the motion should continue. If it shouldn't, the motion
        /// is typically disabled
        /// </summary>
        /// <returns>Boolean that determines if the motion continues</returns>
        public override bool TestUpdate()
        {
            // Ensure we're in the animation
            if (mIsAnimatorActive)
            {
                // If we're not in spell casting mode, get out
                if (mActorController.State.Stance != EnumControllerStance.SPELL_CASTING)
                {
                    return false;
                }
                // If we are in, we may still get out
                else
                {
                    if (_ActionAlias.Length > 0 && mMotionController._InputSource != null)
                    {
                        if (mMotionController._InputSource.IsJustPressed(_ActionAlias))
                        {
                            mActorController.State.Stance = EnumControllerStance.TRAVERSAL;
                            return false;
                        }
                    }
                }

                // Ensure we're in a valid animation
                if (!IsInMotionState)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Raised when a motion is being interrupted by another motion
        /// </summary>
        /// <param name="rMotion">Motion doing the interruption</param>
        /// <returns>Boolean determining if it can be interrupted</returns>
        public override bool TestInterruption(MotionControllerMotion rMotion)
        {
            return true;
        }

        /// <summary>
        /// Called to start the specific motion. If the motion
        /// were something like 'jump', this would start the jumping process
        /// </summary>
        /// <param name="rPrevMotion">Motion that this motion is taking over from</param>
        public override bool Activate(MotionControllerMotion rPrevMotion)
        {
            mLinkRotation = false;

            mActorController.State.Stance = EnumControllerStance.SPELL_CASTING;

            // Tell the animator to start your animations
            if (mMotionLayer._AnimatorStateID != STATE_IdlePose)
            {
                mMotionController.SetAnimatorMotionPhase(mMotionLayer._AnimatorLayerIndex, PHASE_START, true);
            }

            // Register this motion with the camera
            if (_RotateWithCamera && mMotionController.CameraRig is BaseCameraRig)
            {
                ((BaseCameraRig)mMotionController.CameraRig).OnPostLateUpdate -= OnCameraUpdated;
                ((BaseCameraRig)mMotionController.CameraRig).OnPostLateUpdate += OnCameraUpdated;
            }

            // Return
            return base.Activate(rPrevMotion);
        }

        /// <summary>
        /// Called to stop the motion. If the motion is stopable. Some motions
        /// like jump cannot be stopped early
        /// </summary>
        public override void Deactivate()
        {
            // Register this motion with the camera
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
            if (mCombatant != null && mCombatant.IsTargetLocked)
            {
                RotateCameraToTarget(mCombatant.Target, _ToTargetCameraRotationSpeed);
                RotateToTarget(mCombatant.Target, _ToTargetRotationSpeed, rDeltaTime, ref mRotation);
            }
            else
            {
                if (!_RotateWithCamera && _RotateWithInput)
                {
                    RotateUsingInput(rDeltaTime, ref mRotation);
                }

                if (_UsePivotAnimations)
                {
                    int lParameter = 0;
                    if (mMotionController.State.InputMagnitudeTrend.Value > 0f)
                    {
                        if (mYawVelocity > 10f)
                        {
                            lParameter = 1;
                        }
                        else if (mYawVelocity < -10f)
                        {
                            lParameter = -1;
                        }
                    }

                    mMotionController.SetAnimatorMotionParameter(mMotionLayer._AnimatorLayerIndex, lParameter);
                }
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
        private void OnCameraUpdated(float rDeltaTime, int rUpdateCount, BaseCameraRig rCamera)
        {
            if (mMotionController._CameraTransform == null) { return; }

            float lToCameraAngle = Vector3Ext.HorizontalAngleTo(mMotionController._Transform.forward, mMotionController._CameraTransform.forward, mMotionController._Transform.up);
            float lRotationAngle = Mathf.Abs(lToCameraAngle);
            float lRotationSign = Mathf.Sign(lToCameraAngle);

            if (!mLinkRotation && lRotationAngle <= (_RotationSpeed / 60f) * TimeManager.Relative60FPSDeltaTime) { mLinkRotation = true; }

            // Record the velocity for our idle pivoting
            if (lRotationAngle < 1f)
            {
                float lVelocitySign = Mathf.Sign(mYawVelocity);
                mYawVelocity = mYawVelocity - (lVelocitySign * rDeltaTime * 10f);

                if (Mathf.Sign(mYawVelocity) != lVelocitySign) { mYawVelocity = 0f; }
            }
            else
            {
                mYawVelocity = lRotationSign * 12f;
            }

            // If we're not linked, rotate smoothly
            if (!mLinkRotation)
            {
                lToCameraAngle = lRotationSign * Mathf.Min((_RotationSpeed / 60f) * TimeManager.Relative60FPSDeltaTime, lRotationAngle);
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

            if (EditorHelper.TextField("Action Alias", "Action alias that has us enter and leave the spell casting stance", ActionAlias, mMotionController))
            {
                lIsDirty = true;
                ActionAlias = EditorHelper.FieldStringValue;
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

            if (EditorHelper.BoolField("Use Pivot Animations", "Determines if use animations while pivoting.", UsePivotAnimations, mMotionController))
            {
                lIsDirty = true;
                UsePivotAnimations = EditorHelper.FieldBoolValue;
            }

            //GUILayout.Space(5f);

            //if (EditorHelper.FloatField("To Target Rotation Speed", "Degrees per second to rotate the actor to the target.", ToTargetRotationSpeed, mMotionController))
            //{
            //    lIsDirty = true;
            //    ToTargetRotationSpeed = EditorHelper.FieldFloatValue;
            //}

            //if (EditorHelper.FloatField("To Target Camera Speed", "Degrees per second to rotate the camera to the target.", ToTargetCameraRotationSpeed, mMotionController))
            //{
            //    lIsDirty = true;
            //    ToTargetCameraRotationSpeed = EditorHelper.FieldFloatValue;
            //}

            return lIsDirty;
        }

#endif

        #region Pack Methods

        /// <summary>
        /// Name of the group these motions belong to
        /// </summary>
        public static string GroupName()
        {
            return "Spell Casting";
        }

#if UNITY_EDITOR

        public static bool sCreateSubStateMachines = true;

        public static bool sCreateInputAliases = true;

        public static bool sCreateInventory = true;

        public static bool sCreateAttributes = true;

        public static bool sCreateSpellInventory = true;

        public static bool sCreateCore = true;

        public static bool sCreateMotions = true;

        /// <summary>
        /// Determines if this class represents a starting point for a pack
        /// </summary>
        /// <returns></returns>
        public static string RegisterPack()
        {
            return GroupName();
        }

        /// <summary>
        /// Draws the inspector for the pack
        /// </summary>
        /// <returns></returns>
        public static bool OnPackInspector(MotionController rMotionController)
        {
            EditorHelper.DrawSmallTitle(GroupName());
            EditorHelper.DrawLink("Mixamo Pro Magic Pack Animations", "http://www.ootii.com/Unity/MotionPacks/SpellCasting/SpellCastingUsersGuide.pdf");

            GUILayout.Space(5f);

            EditorGUILayout.LabelField("See included documentation:", EditorHelper.SmallBoldLabel);
            EditorGUILayout.LabelField("1. Download and import animations.", EditorHelper.SmallLabel);
            EditorGUILayout.LabelField("2. Unzip and replace animation meta files.", EditorHelper.SmallLabel);
            EditorGUILayout.LabelField("3. Select options and create motions.", EditorHelper.SmallLabel);

            EditorHelper.DrawLine();

            EditorHelper.BoolField("Create Mecanim States", "Determines if we create/override the existing sub-state machine", sCreateSubStateMachines);
            sCreateSubStateMachines = EditorHelper.FieldBoolValue;

            EditorHelper.BoolField("Create Input Aliases", "Determines if we create input aliases", sCreateInputAliases);
            sCreateInputAliases = EditorHelper.FieldBoolValue;

            EditorHelper.BoolField("Create Inventory", "Determines if we create/override the existing inventory", sCreateInventory);
            sCreateInventory = EditorHelper.FieldBoolValue;

            EditorHelper.BoolField("Create Attributes", "Determines if we create/override the existing attributes", sCreateAttributes);
            sCreateAttributes = EditorHelper.FieldBoolValue;

            EditorHelper.BoolField("Create Spell Inventory", "Create the spell inventory for the caster", sCreateSpellInventory);
            sCreateSpellInventory = EditorHelper.FieldBoolValue;

            EditorHelper.BoolField("Create Combatant", "Determines if we create/override the existing core", sCreateCore);
            sCreateCore = EditorHelper.FieldBoolValue;

            EditorHelper.BoolField("Create Motions", "Determines if we create the archery motions", sCreateMotions);
            sCreateMotions = EditorHelper.FieldBoolValue;

            GUILayout.Space(5f);

            if (GUILayout.Button(new GUIContent("Setup Pack", "Create and setup the motion pack."), EditorStyles.miniButton))
            {
                if (sCreateInventory)
                {
                    BasicInventory lInventory = rMotionController.gameObject.GetComponent<BasicInventory>();
                    if (lInventory == null) { lInventory = rMotionController.gameObject.AddComponent<BasicInventory>(); }

                    BasicInventoryItem lItem = lInventory.GetInventoryItem("Spell_01");
                    if (lItem != null) { lInventory.Items.Remove(lItem); }

                    lInventory.Items.Add(new BasicInventoryItem());
                    lInventory.Items[lInventory.Items.Count - 1].ID = "Spell_01";
                    lInventory.Items[lInventory.Items.Count - 1].EquipMotion = "PMP_EquipSpell";
                    lInventory.Items[lInventory.Items.Count - 1].StoreMotion = "PMP_StoreSpell";

                    BasicInventorySlot lSlot = lInventory.GetInventorySlot("RIGHT_HAND");
                    if (lSlot == null)
                    {
                        lInventory.Slots.Add(new BasicInventorySlot());
                        lInventory.Slots[lInventory.Slots.Count - 1].ID = "RIGHT_HAND";
                        lInventory.Slots[lInventory.Slots.Count - 1].ItemID = "";
                    }

                    if (lInventory.GetInventorySlot("LEFT_HAND") == null)
                    {
                        lInventory.Slots.Add(new BasicInventorySlot());
                        lInventory.Slots[lInventory.Slots.Count - 1].ID = "LEFT_HAND";
                        lInventory.Slots[lInventory.Slots.Count - 1].ItemID = "";
                    }

                    lSlot = lInventory.GetInventorySlot("LEFT_LOWER_ARM");
                    if (lSlot == null)
                    {
                        lInventory.Slots.Add(new BasicInventorySlot());
                        lInventory.Slots[lInventory.Slots.Count - 1].ID = "LEFT_LOWER_ARM";
                        lInventory.Slots[lInventory.Slots.Count - 1].ItemID = "";
                    }

                    if (lInventory.GetInventorySlot("READY_PROJECTILE") == null)
                    {
                        lInventory.Slots.Add(new BasicInventorySlot());
                        lInventory.Slots[lInventory.Slots.Count - 1].ID = "READY_PROJECTILE";
                        lInventory.Slots[lInventory.Slots.Count - 1].ItemID = "";
                    }

                    BasicInventorySet lWeaponSet = lInventory.GetWeaponSet("Spell Casting");
                    if (lWeaponSet != null) { lInventory.WeaponSets.Remove(lWeaponSet); }

                    lWeaponSet = new BasicInventorySet();
                    lWeaponSet.ID = "Spell Casting";

                    BasicInventorySetItem lWeaponSetItem = new BasicInventorySetItem();
                    lWeaponSetItem.ItemID = "";
                    lWeaponSetItem.SlotID = "LEFT_HAND";
                    lWeaponSetItem.Instantiate = true;
                    lWeaponSet.Items.Add(lWeaponSetItem);

                    lWeaponSetItem = new BasicInventorySetItem();
                    lWeaponSetItem.ItemID = "";
                    lWeaponSetItem.SlotID = "READY_PROJECTILE";
                    lWeaponSetItem.Instantiate = false;
                    lWeaponSet.Items.Add(lWeaponSetItem);

                    lWeaponSetItem = new BasicInventorySetItem();
                    lWeaponSetItem.ItemID = "Spell_01";
                    lWeaponSetItem.SlotID = "RIGHT_HAND";
                    lWeaponSetItem.Instantiate = false;
                    lWeaponSet.Items.Add(lWeaponSetItem);

                    lWeaponSetItem = new BasicInventorySetItem();
                    lWeaponSetItem.ItemID = "";
                    lWeaponSetItem.SlotID = "LEFT_LOWER_ARM";
                    lWeaponSetItem.Instantiate = false;
                    lWeaponSet.Items.Add(lWeaponSetItem);

                    if (lInventory.WeaponSets.Count == 0)
                    {
                        BasicInventorySet lFirstWeaponSet = new BasicInventorySet();
                        lFirstWeaponSet.ID = "Sword and Shield";

                        lInventory.WeaponSets.Add(lFirstWeaponSet);
                    }

                    if (lInventory.WeaponSets.Count == 1)
                    {
                        BasicInventorySet lSecondWeaponSet = new BasicInventorySet();
                        lSecondWeaponSet.ID = "Longbow";

                        lInventory.WeaponSets.Add(lSecondWeaponSet);
                    }

                    lInventory.WeaponSets.Insert(2, lWeaponSet);
                }

                if (sCreateAttributes)
                {
                    BasicAttributes lAttributes = rMotionController.gameObject.GetComponent<BasicAttributes>();
                    if (lAttributes == null) { lAttributes = rMotionController.gameObject.AddComponent<BasicAttributes>(); }

                    BasicAttribute lAttribute = lAttributes.GetAttribute("Health");
                    if (lAttribute != null) { lAttributes.Items.Remove(lAttribute); }

                    lAttributes.Items.Add(new BasicAttribute());
                    lAttributes.Items[lAttributes.Items.Count - 1].ID = "Health";
                    lAttributes.Items[lAttributes.Items.Count - 1].SetValue<float>(100f);
                }

                if (sCreateSpellInventory)
                {
                    SpellInventory lAttributes = rMotionController.gameObject.GetComponent<SpellInventory>();
                    if (lAttributes == null) { lAttributes = rMotionController.gameObject.AddComponent<SpellInventory>(); }
                }

                if (sCreateCore)
                {
                    Combatant lCombatant = rMotionController.gameObject.GetComponent<Combatant>();
                    if (lCombatant == null) { lCombatant = rMotionController.gameObject.AddComponent<Combatant>(); }

                    if (rMotionController._ActorController == null || !rMotionController._ActorController.UseTransformPosition)
                    {
                        lCombatant.IsLockingEnabled = true;
                        lCombatant.TargetLockedIcon = AssetDatabase.LoadAssetAtPath<Texture>("Assets/ootii/Framework_v1/Content/Textures/UI/TargetIcon_2.png");
                    }

                    ActorCore lCore = rMotionController.gameObject.GetComponent<ActorCore>();
                    if (lCore == null) { lCore = rMotionController.gameObject.AddComponent<ActorCore>(); }

                    lCore.IsAlive = true;
                }

                if (sCreateInputAliases)
                {
                    // Sheathe
                    if (!InputManagerHelper.IsDefined("Spell Casting Equip"))
                    {
                        InputManagerEntry lEntry = new InputManagerEntry();
                        lEntry.Name = "Spell Casting Equip";
                        lEntry.PositiveButton = "3"; // "3" key
                        lEntry.Gravity = 1000;
                        lEntry.Dead = 0.001f;
                        lEntry.Sensitivity = 1000;
                        lEntry.Type = InputManagerEntryType.KEY_MOUSE_BUTTON;
                        lEntry.Axis = 0;
                        lEntry.JoyNum = 0;
                        InputManagerHelper.AddEntry(lEntry, true);

                        lEntry = new InputManagerEntry();
                        lEntry.Name = "Spell Casting Equip";
                        lEntry.Gravity = 1000;
                        lEntry.Dead = 0.001f;
                        lEntry.Sensitivity = 1000;
                        lEntry.JoyNum = 0;

#if UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
                        lEntry.PositiveButton = "joystick button 8";
                        lEntry.Type = InputManagerEntryType.KEY_MOUSE_BUTTON; // D-pad Y
                        lEntry.Axis = 0;
#else
                        lEntry.PositiveButton = "";
                        lEntry.Type = InputManagerEntryType.JOYSTICK_AXIS; // D-pad Y
                        lEntry.Axis = 7;
#endif

                        InputManagerHelper.AddEntry(lEntry, true);

                    }

                    // Fire
                    if (!InputManagerHelper.IsDefined("Spell Casting Cast"))
                    {
                        InputManagerEntry lEntry = new InputManagerEntry();
                        lEntry.Name = "Spell Casting Cast";
                        lEntry.PositiveButton = "left ctrl";
                        lEntry.AltPositiveButton = "mouse 0"; // Left mouse button
                        lEntry.Gravity = 1000;
                        lEntry.Dead = 0.001f;
                        lEntry.Sensitivity = 1000;
                        lEntry.Type = InputManagerEntryType.KEY_MOUSE_BUTTON;
                        lEntry.Axis = 0;
                        lEntry.JoyNum = 0;
                        InputManagerHelper.AddEntry(lEntry, true);

                        lEntry = new InputManagerEntry();
                        lEntry.Name = "Spell Casting Cast";

#if UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
                        lEntry.PositiveButton = "joystick button 16"; // Green A
#else

                        lEntry.PositiveButton = "joystick button 0"; // Green A
#endif

                        lEntry.Gravity = 1000;
                        lEntry.Dead = 0.001f;
                        lEntry.Sensitivity = 1000;
                        lEntry.Type = InputManagerEntryType.KEY_MOUSE_BUTTON;
                        lEntry.Axis = 0;
                        lEntry.JoyNum = 0;
                        InputManagerHelper.AddEntry(lEntry, true);
                    }

                    // Continue
                    if (!InputManagerHelper.IsDefined("Spell Casting Continue"))
                    {
                        InputManagerEntry lEntry = new InputManagerEntry();
                        lEntry.Name = "Spell Casting Continue";
                        lEntry.PositiveButton = "left ctrl";
                        lEntry.AltPositiveButton = "mouse 0"; // Left mouse button
                        lEntry.Gravity = 1000;
                        lEntry.Dead = 0.001f;
                        lEntry.Sensitivity = 1000;
                        lEntry.Type = InputManagerEntryType.KEY_MOUSE_BUTTON;
                        lEntry.Axis = 0;
                        lEntry.JoyNum = 0;
                        InputManagerHelper.AddEntry(lEntry, true);

                        lEntry = new InputManagerEntry();
                        lEntry.Name = "Spell Casting Continue";

#if UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
                        lEntry.PositiveButton = "joystick button 16"; // Green A
#else

                        lEntry.PositiveButton = "joystick button 0"; // Green A
#endif

                        lEntry.Gravity = 1000;
                        lEntry.Dead = 0.001f;
                        lEntry.Sensitivity = 1000;
                        lEntry.Type = InputManagerEntryType.KEY_MOUSE_BUTTON;
                        lEntry.Axis = 0;
                        lEntry.JoyNum = 0;
                        InputManagerHelper.AddEntry(lEntry, true);
                    }

                    // Cancel
                    if (!InputManagerHelper.IsDefined("Spell Casting Cancel"))
                    {
                        InputManagerEntry lEntry = new InputManagerEntry();
                        lEntry.Name = "Spell Casting Cancel";
                        lEntry.PositiveButton = "escape";
                        lEntry.Gravity = 1000;
                        lEntry.Dead = 0.001f;
                        lEntry.Sensitivity = 1000;
                        lEntry.Type = InputManagerEntryType.KEY_MOUSE_BUTTON;
                        lEntry.Axis = 0;
                        lEntry.JoyNum = 0;
                        InputManagerHelper.AddEntry(lEntry, true);

                        lEntry = new InputManagerEntry();
                        lEntry.Name = "Spell Casting Cancel";

#if UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
                        lEntry.PositiveButton = "joystick button 19"; // Yellow Y
#else

                        lEntry.PositiveButton = "joystick button 3"; // Yellow Y
#endif

                        lEntry.Gravity = 1000;
                        lEntry.Dead = 0.001f;
                        lEntry.Sensitivity = 1000;
                        lEntry.Type = InputManagerEntryType.KEY_MOUSE_BUTTON;
                        lEntry.Axis = 0;
                        lEntry.JoyNum = 0;
                        InputManagerHelper.AddEntry(lEntry, true);
                    }

                    // Move Up
                    if (!InputManagerHelper.IsDefined("Move Up"))
                    {
                        InputManagerEntry lEntry = new InputManagerEntry();
                        lEntry.Name = "Move Up";
                        lEntry.PositiveButton = "e";
                        lEntry.Gravity = 1000;
                        lEntry.Dead = 0.001f;
                        lEntry.Sensitivity = 1000;
                        lEntry.Type = InputManagerEntryType.KEY_MOUSE_BUTTON;
                        lEntry.Axis = 0;
                        lEntry.JoyNum = 0;

                        InputManagerHelper.AddEntry(lEntry, true);
                    }

                    // Move down
                    if (!InputManagerHelper.IsDefined("Move Down"))
                    {
                        InputManagerEntry lEntry = new InputManagerEntry();
                        lEntry.Name = "Move Down";
                        lEntry.PositiveButton = "q";
                        lEntry.Gravity = 1000;
                        lEntry.Dead = 0.001f;
                        lEntry.Sensitivity = 1000;
                        lEntry.Type = InputManagerEntryType.KEY_MOUSE_BUTTON;
                        lEntry.Axis = 0;
                        lEntry.JoyNum = 0;

                        InputManagerHelper.AddEntry(lEntry, true);
                    }
                }

                if (sCreateMotions || sCreateSubStateMachines)
                {
                    IBaseCameraRig lCameraRig = rMotionController.CameraRig;
                    if (lCameraRig == null) { lCameraRig = rMotionController.ExtractCameraRig(rMotionController.CameraTransform); }

                    if (rMotionController.MotionLayers.Count == 0)
                    {
                        MotionControllerLayer lMotionLayer = new MotionControllerLayer();
                        rMotionController.MotionLayers.Add(lMotionLayer);
                    }

                    PMP_Idle lIdle = rMotionController.GetMotion<PMP_Idle>();
                    if (lIdle == null) { lIdle = rMotionController.CreateMotion<PMP_Idle>(0); }

                    PMP_EquipSpell lEquip = rMotionController.GetMotion<PMP_EquipSpell>(0);
                    if (lEquip == null) { lEquip = rMotionController.CreateMotion<PMP_EquipSpell>(0); }

                    PMP_StoreSpell lStore = rMotionController.GetMotion<PMP_StoreSpell>(0);
                    if (lStore == null) { lStore = rMotionController.CreateMotion<PMP_StoreSpell>(0); }

                    PMP_WalkRunPivot lPivot = rMotionController.GetMotion<PMP_WalkRunPivot>(0);
                    if (lPivot == null) { lPivot = rMotionController.CreateMotion<PMP_WalkRunPivot>(0); }

                    PMP_WalkRunStrafe lStrafe = rMotionController.GetMotion<PMP_WalkRunStrafe>(0);
                    if (lStrafe == null) { lStrafe = rMotionController.CreateMotion<PMP_WalkRunStrafe>(0); }

                    PMP_BasicSpellCastings lCast = rMotionController.GetMotion<PMP_BasicSpellCastings>(0);
                    if (lCast == null) { lCast = rMotionController.CreateMotion<PMP_BasicSpellCastings>(0); }

                    Cower lCower = rMotionController.GetMotion<Cower>(0);
                    if (lCower == null) { lCower = rMotionController.CreateMotion<Cower>(0); }

                    Death lDeath = rMotionController.GetMotion<Death>(0);
                    if (lDeath == null) { lDeath = rMotionController.CreateMotion<Death>(0); }

                    Damaged lDamaged = rMotionController.GetMotion<Damaged>(0);
                    if (lDamaged == null) { lDamaged = rMotionController.CreateMotion<Damaged>(0); }

                    Frozen lFrozen = rMotionController.GetMotion<Frozen>(0);
                    if (lFrozen == null) { lFrozen = rMotionController.CreateMotion<Frozen>(0); }

                    KnockedDown lKnockedDown = rMotionController.GetMotion<KnockedDown>(0);
                    if (lKnockedDown == null) { lKnockedDown = rMotionController.CreateMotion<KnockedDown>(0); }

                    Levitate lLevitate = rMotionController.GetMotion<Levitate>(0);
                    if (lLevitate == null) { lLevitate = rMotionController.CreateMotion<Levitate>(0); }

                    PushedBack lPushedBack = rMotionController.GetMotion<PushedBack>(0);
                    if (lPushedBack == null) { lPushedBack = rMotionController.CreateMotion<PushedBack>(0); }

                    Sleep lSleep = rMotionController.GetMotion<Sleep>(0);
                    if (lSleep == null) { lSleep = rMotionController.CreateMotion<Sleep>(0); }

                    Stunned lStunned = rMotionController.GetMotion<Stunned>(0);
                    if (lStunned == null) { lStunned = rMotionController.CreateMotion<Stunned>(0); }

                    if (sCreateSubStateMachines)
                    {
                        Animator lAnimator = rMotionController.Animator;
                        if (lAnimator == null) { lAnimator = rMotionController.gameObject.GetComponent<Animator>(); }

                        if (lAnimator != null)
                        {
                            UnityEditor.Animations.AnimatorController lAnimatorController = lAnimator.runtimeAnimatorController as UnityEditor.Animations.AnimatorController;

                            lIdle.CreateStateMachine(lAnimatorController);
                            lEquip.CreateStateMachine(lAnimatorController);
                            lPivot.CreateStateMachine(lAnimatorController);
                            lStrafe.CreateStateMachine(lAnimatorController);
                            lCast.CreateStateMachine(lAnimatorController);
                            lDeath.CreateStateMachine(lAnimatorController);
                            lLevitate.CreateStateMachine(lAnimatorController);
                        }
                    }
                }

                EditorUtility.DisplayDialog("Motion Pack: " + GroupName(), "Motion pack imported.", "ok");

                return true;
            }

            return false;
        }

#endif

        #endregion

        #region Auto-Generated
        // ************************************ START AUTO GENERATED ************************************

        /// <summary>
        /// These declarations go inside the class so you can test for which state
        /// and transitions are active. Testing hash values is much faster than strings.
        /// </summary>
        public static int STATE_IdlePose = -1;
        public static int TRANS_AnyState_IdlePose = -1;
        public static int TRANS_EntryState_IdlePose = -1;

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

                if (lStateID == STATE_IdlePose) { return true; }
                if (lTransitionID == TRANS_AnyState_IdlePose) { return true; }
                if (lTransitionID == TRANS_EntryState_IdlePose) { return true; }
                return false;
            }
        }

        /// <summary>
        /// Used to determine if the actor is in one of the states for this motion
        /// </summary>
        /// <returns></returns>
        public override bool IsMotionState(int rStateID)
        {
            if (rStateID == STATE_IdlePose) { return true; }
            return false;
        }

        /// <summary>
        /// Used to determine if the actor is in one of the states for this motion
        /// </summary>
        /// <returns></returns>
        public override bool IsMotionState(int rStateID, int rTransitionID)
        {
            if (rStateID == STATE_IdlePose) { return true; }
            if (rTransitionID == TRANS_AnyState_IdlePose) { return true; }
            if (rTransitionID == TRANS_EntryState_IdlePose) { return true; }
            return false;
        }

        /// <summary>
        /// Preprocess any animator data so the motion can use it later
        /// </summary>
        public override void LoadAnimatorData()
        {
            TRANS_AnyState_IdlePose = mMotionController.AddAnimatorName("AnyState -> Base Layer.PMP_Idle-SM.IdlePose");
            TRANS_EntryState_IdlePose = mMotionController.AddAnimatorName("Entry -> Base Layer.PMP_Idle-SM.IdlePose");
            STATE_IdlePose = mMotionController.AddAnimatorName("Base Layer.PMP_Idle-SM.IdlePose");
        }

#if UNITY_EDITOR

        private AnimationClip m68716 = null;

        /// <summary>
        /// Creates the animator substate machine for this motion.
        /// </summary>
        protected override void CreateStateMachine()
        {
            // Grab the root sm for the layer
            UnityEditor.Animations.AnimatorStateMachine lRootStateMachine = _EditorAnimatorController.layers[mMotionLayer.AnimatorLayerIndex].stateMachine;
            UnityEditor.Animations.AnimatorStateMachine lSM_47316 = _EditorAnimatorController.layers[mMotionLayer.AnimatorLayerIndex].stateMachine;
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

            UnityEditor.Animations.AnimatorStateMachine lSM_N10000 = lRootSubStateMachine;
            if (lSM_N10000 != null)
            {
                for (int i = lSM_N10000.entryTransitions.Length - 1; i >= 0; i--)
                {
                    lSM_N10000.RemoveEntryTransition(lSM_N10000.entryTransitions[i]);
                }

                for (int i = lSM_N10000.anyStateTransitions.Length - 1; i >= 0; i--)
                {
                    lSM_N10000.RemoveAnyStateTransition(lSM_N10000.anyStateTransitions[i]);
                }

                for (int i = lSM_N10000.states.Length - 1; i >= 0; i--)
                {
                    lSM_N10000.RemoveState(lSM_N10000.states[i].state);
                }

                for (int i = lSM_N10000.stateMachines.Length - 1; i >= 0; i--)
                {
                    lSM_N10000.RemoveStateMachine(lSM_N10000.stateMachines[i].stateMachine);
                }
            }
            else
            {
                lSM_N10000 = lSM_47316.AddStateMachine(_EditorAnimatorSMName, new Vector3(192, 264, 0));
            }

            UnityEditor.Animations.AnimatorState lS_N9998 = lSM_N10000.AddState("IdlePose", new Vector3(252, 108, 0));
            lS_N9998.speed = 1f;
            lS_N9998.motion = m68716;

            // Create the transition from the any state. Note that 'AnyState' transitions have to be added to the root
            UnityEditor.Animations.AnimatorStateTransition lT_N120354 = lRootStateMachine.AddAnyStateTransition(lS_N9998);
            lT_N120354.hasExitTime = false;
            lT_N120354.hasFixedDuration = true;
            lT_N120354.exitTime = 0.9f;
            lT_N120354.duration = 0.1f;
            lT_N120354.offset = 0f;
            lT_N120354.mute = false;
            lT_N120354.solo = false;
            lT_N120354.canTransitionToSelf = true;
            lT_N120354.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            lT_N120354.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 32100f, "L0MotionPhase");

        }

        /// <summary>
        /// Gathers the animations so we can use them when creating the sub-state machine.
        /// </summary>
        public override void FindAnimations()
        {
            m68716 = FindAnimationClip("Assets/ootii/MotionControllerPacks/SpellCasting/Content/Animations/Mixamo/Y_Bot@standing_idle.fbx/PMP_IdlePose.anim", "PMP_IdlePose");

            // Add the remaining functionality
            base.FindAnimations();
        }

        /// <summary>
        /// Used to show the settings that allow us to generate the animator setup.
        /// </summary>
        public override void OnSettingsGUI()
        {
            UnityEditor.EditorGUILayout.IntField(new GUIContent("Phase ID", "Phase ID used to transition to the state."), PHASE_START);
            m68716 = CreateAnimationField("IdlePose.PMP_IdlePose", "Assets/ootii/MotionControllerPacks/SpellCasting/Content/Animations/Mixamo/Y_Bot@standing_idle.fbx/PMP_IdlePose.anim", "PMP_IdlePose", m68716);

            // Add the remaining functionality
            base.OnSettingsGUI();
        }

#endif

        // ************************************ END AUTO GENERATED ************************************
        #endregion
    }
}
