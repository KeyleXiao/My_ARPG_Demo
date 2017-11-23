using System;
using UnityEngine;
using com.ootii.Actors.AnimationControllers;
using com.ootii.Actors.Inventory;
using com.ootii.Data.Serializers;
using com.ootii.Helpers;

namespace com.ootii.MotionControllerPacks
{
    /// <summary>
    /// Sheathes the sword and shield and moves into the idle pose.
    /// </summary>
    [MotionName("PMP - Store Spell")]
    [MotionDescription("Stores the spell using Mixamo animations.")]
    public class PMP_StoreSpell : PMP_MotionBase, IEquipStoreMotion
    {
        /// <summary>
        /// Preallocates string for the event tests
        /// </summary>
        public static string EVENT_STORE = "store";

        /// <summary>
        /// Trigger values for th emotion
        /// </summary>
        public const int PHASE_UNKNOWN = 0;
        public const int PHASE_START = 32115;

        /// <summary>
        /// GameObject that owns the IInventorySource we really want
        /// </summary>
        public GameObject _InventorySourceOwner = null;
        public GameObject InventorySourceOwner
        {
            get { return _InventorySourceOwner; }
            set { _InventorySourceOwner = value; }
        }

        /// <summary>
        /// Defines the source of our inventory items.
        /// </summary>
        [NonSerialized]
        protected IInventorySource mInventorySource = null;
        public IInventorySource InventorySource
        {
            get { return mInventorySource; }
            set { mInventorySource = value; }
        }

        /// <summary>
        /// ID of the 'right hand' slot the sword is held in
        /// </summary>
        public string _SlotID = "RIGHT_HAND";
        public string SlotID
        {
            get { return _SlotID; }
            set { _SlotID = value; }
        }

        /// <summary>
        /// Slot ID that is will hold the item. 
        /// This overrides any properties for one activation.
        /// </summary>
        [NonSerialized]
        public string _OverrideSlotID = null;

        [SerializationIgnore]
        public string OverrideSlotID
        {
            get { return _OverrideSlotID; }
            set { _OverrideSlotID = value; }
        }

        /// <summary>
        /// Item ID that is going to be unsheathed. 
        /// This overrides any properties for one activation.
        /// </summary>
        [NonSerialized]
        public string _OverrideItemID = null;

        [SerializationIgnore]
        public string OverrideItemID
        {
            get { return _OverrideItemID; }
            set { _OverrideItemID = value; }
        }

        /// <summary>
        /// Determines if the weapon is currently equipped
        /// </summary>
        private bool mIsEquipped = false;

        /// <summary>
        /// Default constructor
        /// </summary>
        public PMP_StoreSpell()
            : base()
        {
            _Pack = PMP_Idle.GroupName();
            _Category = EnumMotionCategories.EQUIP_STORE;

            _Priority = 8f;
            _ActionAlias = "Spell Equip";

#if UNITY_EDITOR
            if (_EditorAnimatorSMName.Length == 0) { _EditorAnimatorSMName = "PMP_EquipStore-SM"; }
#endif
        }

        /// <summary>
        /// Controller constructor
        /// </summary>
        /// <param name="rController">Controller the motion belongs to</param>
        public PMP_StoreSpell(MotionController rController)
            : base(rController)
        {
            _Pack = PMP_Idle.GroupName();
            _Category = EnumMotionCategories.EQUIP_STORE;

            _Priority = 8f;
            _ActionAlias = "Spell Equip";

#if UNITY_EDITOR
            if (_EditorAnimatorSMName.Length == 0) { _EditorAnimatorSMName = "PMP_EquipStore-SM"; }
#endif
        }

        /// <summary>
        /// Awake is called after all objects are initialized so you can safely speak to other objects. This is where
        /// reference can be associated.
        /// </summary>
        public override void Awake()
        {
            base.Awake();

            // Object that will provide access to attributes
            if (_InventorySourceOwner != null)
            {
                mInventorySource = InterfaceHelper.GetComponent<IInventorySource>(_InventorySourceOwner);
            }

            // If the input source is still null, see if we can grab a local input source
            if (mInventorySource == null && mMotionController != null)
            {
                mInventorySource = InterfaceHelper.GetComponent<IInventorySource>(mMotionController.gameObject);
                if (mInventorySource != null) { _InventorySourceOwner = mMotionController.gameObject; }
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
            if (!mActorController.IsGrounded) { return false; }
            if (mMotionController._InputSource == null) { return false; }
            if (mMotionLayer._AnimatorTransitionID != 0) { return false; }

            // Since we're using BasicInventory, it can 
            if (mInventorySource != null && !mInventorySource.AllowMotionSelfActivation) { return false; }

            // If we're already in a melee stance, we can stop
            if (mActorController.State.Stance == EnumControllerStance.SPELL_CASTING)
            {
                if (mMotionController._InputSource.IsJustPressed(_ActionAlias))
                {
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
            // Check if it's time to exit. 
            if (mMotionLayer._AnimatorStateID == STATE_SpellIdleOut || mMotionLayer._AnimatorStateID == STATE_StandIdleOut)
            {
                return false;
            }
            // If we're not in our motion state and no transition is happening, we may need to get out
            else if (!IsInMotionState)
            { 
                if (mMotionController.State.AnimatorStates[mMotionLayer._AnimatorLayerIndex].MotionPhase == 0 &&
                    mMotionLayer._AnimatorTransitionID == 0)
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
            if (rMotion.Category != EnumMotionCategories.DEATH)
            {
                if (mIsEquipped)
                {
                    mIsEquipped = false;
                    StoreItem();
                }
            }

            return base.TestInterruption(rMotion);
        }

        /// <summary>
        /// Called to start the specific motion. If the motion
        /// were something like 'jump', this would start the jumping process
        /// </summary>
        /// <param name="rPrevMotion">Motion that this motion is taking over from</param>
        public override bool Activate(MotionControllerMotion rPrevMotion)
        {
            mIsEquipped = true;

            // Trigger the animation
            mMotionController.SetAnimatorMotionPhase(mMotionLayer.AnimatorLayerIndex, PHASE_START, true);
            return base.Activate(rPrevMotion);
        }

        /// <summary>
        /// Raised when we shut the motion down
        /// </summary>
        public override void Deactivate()
        {
            // Reset the state
            if (mActorController.State.Stance == EnumControllerStance.SPELL_CASTING)
            {
                mActorController.State.Stance = EnumControllerStance.TRAVERSAL;
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
            // When someone else is forcing an activation, we can get into a race condition with 
            // the idle from the previous equip. So, we'll keep forcing the phase ID until we're back here
            if (mIsEquipped && (mInventorySource != null && !mInventorySource.AllowMotionSelfActivation) && !IsInMotionState)
            {
                mMotionController.SetAnimatorMotionPhase(mMotionLayer.AnimatorLayerIndex, PHASE_START, true);
            }
        }

        /// <summary>
        /// Raised by the animation when an event occurs
        /// </summary>
        public override void OnAnimationEvent(AnimationEvent rEvent)
        {
            if (rEvent == null) { return; }

            if (rEvent.stringParameter.Length == 0 || StringHelper.CleanString(rEvent.stringParameter) == EVENT_STORE)
            {
                if (mIsEquipped)
                {
                    mIsEquipped = false;
                    StoreItem();
                }
            }
        }

        /// <summary>
        /// Create the item to unsheathe
        /// </summary>
        /// <returns></returns>
        private void StoreItem()
        {
            string lSlotID = "";
            if (OverrideSlotID.Length > 0)
            {
                lSlotID = OverrideSlotID;
            }
            else
            {
                lSlotID = SlotID;
            }

            if (mInventorySource != null)
            {
                if (mCombatant != null)
                {
                    if (mCombatant.PrimaryWeapon != null) { mCombatant.PrimaryWeapon.Owner = null; }
                    mCombatant.PrimaryWeapon = null;
                }

                mInventorySource.StoreItem(lSlotID);
            }
            
            // Remove a body sphere we may have added
            mMotionController._ActorController.RemoveBodyShape("Combatant Shape");
        }

        #region Editor Functions

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

            if (EditorHelper.TextField("Action Alias", "Action alias that is used to trigger sheathing and unsheating the sword.", ActionAlias, mMotionController))
            {
                lIsDirty = true;
                ActionAlias = EditorHelper.FieldStringValue;
            }

            GUILayout.Space(5f);

            GameObject lNewAttributeSourceOwner = EditorHelper.InterfaceOwnerField<IInventorySource>(new GUIContent("Inventory Source", "Inventory source we'll use for accessing items and slots."), InventorySourceOwner, true);
            if (lNewAttributeSourceOwner != InventorySourceOwner)
            {
                lIsDirty = true;
                InventorySourceOwner = lNewAttributeSourceOwner;
            }

            if (EditorHelper.TextField("Slot ID", "ID of the 'right hand' slot that the sword is held in.", SlotID, mMotionController))
            {
                lIsDirty = true;
                SlotID = EditorHelper.FieldStringValue;
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
        public static int STATE_StandIdleOut = -1;
        public static int STATE_StoreSpell = -1;
        public static int STATE_EquipSpell = -1;
        public static int TRANS_AnyState_EquipSpell = -1;
        public static int TRANS_EntryState_EquipSpell = -1;
        public static int TRANS_AnyState_StoreSpell = -1;
        public static int TRANS_EntryState_StoreSpell = -1;
        public static int TRANS_StoreSpell_StandIdleOut = -1;
        public static int TRANS_EquipSpell_SpellIdleOut = -1;

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
                    if (lStateID == STATE_StandIdleOut) { return true; }
                    if (lStateID == STATE_StoreSpell) { return true; }
                    if (lStateID == STATE_EquipSpell) { return true; }
                }

                if (lTransitionID == TRANS_AnyState_EquipSpell) { return true; }
                if (lTransitionID == TRANS_EntryState_EquipSpell) { return true; }
                if (lTransitionID == TRANS_AnyState_StoreSpell) { return true; }
                if (lTransitionID == TRANS_EntryState_StoreSpell) { return true; }
                if (lTransitionID == TRANS_StoreSpell_StandIdleOut) { return true; }
                if (lTransitionID == TRANS_EquipSpell_SpellIdleOut) { return true; }
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
            if (rStateID == STATE_StandIdleOut) { return true; }
            if (rStateID == STATE_StoreSpell) { return true; }
            if (rStateID == STATE_EquipSpell) { return true; }
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
                if (rStateID == STATE_StandIdleOut) { return true; }
                if (rStateID == STATE_StoreSpell) { return true; }
                if (rStateID == STATE_EquipSpell) { return true; }
            }

            if (rTransitionID == TRANS_AnyState_EquipSpell) { return true; }
            if (rTransitionID == TRANS_EntryState_EquipSpell) { return true; }
            if (rTransitionID == TRANS_AnyState_StoreSpell) { return true; }
            if (rTransitionID == TRANS_EntryState_StoreSpell) { return true; }
            if (rTransitionID == TRANS_StoreSpell_StandIdleOut) { return true; }
            if (rTransitionID == TRANS_EquipSpell_SpellIdleOut) { return true; }
            return false;
        }

        /// <summary>
        /// Preprocess any animator data so the motion can use it later
        /// </summary>
        public override void LoadAnimatorData()
        {
            TRANS_AnyState_EquipSpell = mMotionController.AddAnimatorName("AnyState -> Base Layer.PMP_EquipStore-SM.Equip Spell");
            TRANS_EntryState_EquipSpell = mMotionController.AddAnimatorName("Entry -> Base Layer.PMP_EquipStore-SM.Equip Spell");
            TRANS_AnyState_StoreSpell = mMotionController.AddAnimatorName("AnyState -> Base Layer.PMP_EquipStore-SM.Store Spell");
            TRANS_EntryState_StoreSpell = mMotionController.AddAnimatorName("Entry -> Base Layer.PMP_EquipStore-SM.Store Spell");
            STATE_Start = mMotionController.AddAnimatorName("Base Layer.Start");
            STATE_SpellIdleOut = mMotionController.AddAnimatorName("Base Layer.PMP_EquipStore-SM.Spell Idle Out");
            STATE_StandIdleOut = mMotionController.AddAnimatorName("Base Layer.PMP_EquipStore-SM.Stand Idle Out");
            STATE_StoreSpell = mMotionController.AddAnimatorName("Base Layer.PMP_EquipStore-SM.Store Spell");
            TRANS_StoreSpell_StandIdleOut = mMotionController.AddAnimatorName("Base Layer.PMP_EquipStore-SM.Store Spell -> Base Layer.PMP_EquipStore-SM.Stand Idle Out");
            STATE_EquipSpell = mMotionController.AddAnimatorName("Base Layer.PMP_EquipStore-SM.Equip Spell");
            TRANS_EquipSpell_SpellIdleOut = mMotionController.AddAnimatorName("Base Layer.PMP_EquipStore-SM.Equip Spell -> Base Layer.PMP_EquipStore-SM.Spell Idle Out");
        }

#if UNITY_EDITOR

        private AnimationClip m17212 = null;
        private AnimationClip m20620 = null;
        private AnimationClip m19754 = null;

        /// <summary>
        /// Creates the animator substate machine for this motion.
        /// </summary>
        protected override void CreateStateMachine()
        {
            // Grab the root sm for the layer
            UnityEditor.Animations.AnimatorStateMachine lRootStateMachine = _EditorAnimatorController.layers[mMotionLayer.AnimatorLayerIndex].stateMachine;
            UnityEditor.Animations.AnimatorStateMachine lSM_41656 = _EditorAnimatorController.layers[mMotionLayer.AnimatorLayerIndex].stateMachine;
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

            UnityEditor.Animations.AnimatorStateMachine lSM_41686 = lRootSubStateMachine;
            if (lSM_41686 != null)
            {
                for (int i = lSM_41686.entryTransitions.Length - 1; i >= 0; i--)
                {
                    lSM_41686.RemoveEntryTransition(lSM_41686.entryTransitions[i]);
                }

                for (int i = lSM_41686.anyStateTransitions.Length - 1; i >= 0; i--)
                {
                    lSM_41686.RemoveAnyStateTransition(lSM_41686.anyStateTransitions[i]);
                }

                for (int i = lSM_41686.states.Length - 1; i >= 0; i--)
                {
                    lSM_41686.RemoveState(lSM_41686.states[i].state);
                }

                for (int i = lSM_41686.stateMachines.Length - 1; i >= 0; i--)
                {
                    lSM_41686.RemoveStateMachine(lSM_41686.stateMachines[i].stateMachine);
                }
            }
            else
            {
                lSM_41686 = lSM_41656.AddStateMachine(_EditorAnimatorSMName, new Vector3(192, 324, 0));
            }

            UnityEditor.Animations.AnimatorState lS_42706 = lSM_41686.AddState("Spell Idle Out", new Vector3(576, 48, 0));
            lS_42706.speed = 1f;
            lS_42706.motion = m20620;

            UnityEditor.Animations.AnimatorState lS_42708 = lSM_41686.AddState("Stand Idle Out", new Vector3(576, 120, 0));
            lS_42708.speed = 1f;
            lS_42708.motion = m17212;

            UnityEditor.Animations.AnimatorState lS_41992 = lSM_41686.AddState("Store Spell", new Vector3(312, 120, 0));
            lS_41992.speed = -1.1f;
            lS_41992.motion = m19754;

            UnityEditor.Animations.AnimatorState lS_41990 = lSM_41686.AddState("Equip Spell", new Vector3(312, 48, 0));
            lS_41990.speed = 1.1f;
            lS_41990.motion = m19754;

            // Create the transition from the any state. Note that 'AnyState' transitions have to be added to the root
            UnityEditor.Animations.AnimatorStateTransition lT_41758 = lRootStateMachine.AddAnyStateTransition(lS_41990);
            lT_41758.hasExitTime = false;
            lT_41758.hasFixedDuration = true;
            lT_41758.exitTime = 0.9f;
            lT_41758.duration = 0.1f;
            lT_41758.offset = 0f;
            lT_41758.mute = false;
            lT_41758.solo = false;
            lT_41758.canTransitionToSelf = true;
            lT_41758.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            lT_41758.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 32110f, "L0MotionPhase");

            // Create the transition from the any state. Note that 'AnyState' transitions have to be added to the root
            UnityEditor.Animations.AnimatorStateTransition lT_41760 = lRootStateMachine.AddAnyStateTransition(lS_41992);
            lT_41760.hasExitTime = false;
            lT_41760.hasFixedDuration = true;
            lT_41760.exitTime = 0.9f;
            lT_41760.duration = 0.2f;
            lT_41760.offset = 0f;
            lT_41760.mute = false;
            lT_41760.solo = false;
            lT_41760.canTransitionToSelf = true;
            lT_41760.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            lT_41760.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 32115f, "L0MotionPhase");

            UnityEditor.Animations.AnimatorStateTransition lT_42710 = lS_41992.AddTransition(lS_42708);
            lT_42710.hasExitTime = true;
            lT_42710.hasFixedDuration = true;
            lT_42710.exitTime = 0.8857439f;
            lT_42710.duration = 0.1033962f;
            lT_42710.offset = 1.507076f;
            lT_42710.mute = false;
            lT_42710.solo = false;
            lT_42710.canTransitionToSelf = true;
            lT_42710.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;

            UnityEditor.Animations.AnimatorStateTransition lT_42712 = lS_41990.AddTransition(lS_42706);
            lT_42712.hasExitTime = true;
            lT_42712.hasFixedDuration = true;
            lT_42712.exitTime = 0.5385581f;
            lT_42712.duration = 0.3215042f;
            lT_42712.offset = 0f;
            lT_42712.mute = false;
            lT_42712.solo = false;
            lT_42712.canTransitionToSelf = true;
            lT_42712.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;

        }

        /// <summary>
        /// Gathers the animations so we can use them when creating the sub-state machine.
        /// </summary>
        public override void FindAnimations()
        {
            m17212 = FindAnimationClip("Assets/ootii/MotionController/Content/Animations/Humanoid/Idling/unity_Idle_IdleToIdlesR.fbx/IdlePose.anim", "IdlePose");
            m20620 = FindAnimationClip("Assets/ootii/MotionControllerPacks/SpellCasting/Content/Animations/Mixamo/Y_Bot@standing_idle.fbx/PMP_IdlePose.anim", "PMP_IdlePose");
            m19754 = FindAnimationClip("Assets/ootii/MotionController/Content/Animations/Humanoid/Idling/IdleToReady.fbx/IdleToReady.anim", "IdleToReady");

            // Add the remaining functionality
            base.FindAnimations();
        }

        /// <summary>
        /// Used to show the settings that allow us to generate the animator setup.
        /// </summary>
        public override void OnSettingsGUI()
        {
            UnityEditor.EditorGUILayout.IntField(new GUIContent("Phase ID", "Phase ID used to transition to the state."), PHASE_START);
            m17212 = CreateAnimationField("Start.IdlePose", "Assets/ootii/MotionController/Content/Animations/Humanoid/Idling/unity_Idle_IdleToIdlesR.fbx/IdlePose.anim", "IdlePose", m17212);
            m20620 = CreateAnimationField("Spell Idle Out.PMP_IdlePose", "Assets/ootii/MotionControllerPacks/SpellCasting/Content/Animations/Mixamo/Y_Bot@standing_idle.fbx/PMP_IdlePose.anim", "PMP_IdlePose", m20620);
            m19754 = CreateAnimationField("Store Spell.IdleToReady", "Assets/ootii/MotionController/Content/Animations/Humanoid/Idling/IdleToReady.fbx/IdleToReady.anim", "IdleToReady", m19754);

            // Add the remaining functionality
            base.OnSettingsGUI();
        }

#endif

        // ************************************ END AUTO GENERATED ************************************
        #endregion
    }
}
