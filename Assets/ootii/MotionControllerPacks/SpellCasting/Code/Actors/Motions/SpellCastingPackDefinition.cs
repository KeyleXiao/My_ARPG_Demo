using UnityEngine;
using com.ootii.Actors.Attributes;
using com.ootii.Actors.Combat;
using com.ootii.Actors.Inventory;
using com.ootii.Actors.LifeCores;
using com.ootii.Actors.Magic;
using com.ootii.Cameras;
using com.ootii.Helpers;
using com.ootii.MotionControllerPacks;
using com.ootii.Reactors;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace com.ootii.Actors.AnimationControllers
{
    /// <summary>
    /// Contains information about the motion pack that will be used
    /// when setting up the motions
    /// </summary>
    public class SpellCastingPackDefinition : MotionPackDefinition
    {
        /// <summary>
        /// Defines the friendly name of the motion pack
        /// </summary>
        public new static string PackName
        {
            get { return "Spell Casting"; }
        }

#if UNITY_EDITOR

        //private static string[] mMovementStyles = new string[] { "Adventure", "Shooter" };

        private static int mAnimatorLayer = 0;

        //private static bool mExtendSubStateMachines = true;

        private static bool mCreateSubStateMachines = true;

        private static bool mCreateInputAliases = true;

        private static bool mCreateInventory = true;

        private static bool mCreateSpellInventory = true;

        private static bool mCreateAttributes = true;

        private static bool mCreateCore = true;

        private static bool mCreateMotions = true;

        /// <summary>
        /// Draws the inspector for the pack
        /// </summary>
        /// <returns></returns>
        public new static bool OnPackInspector(MotionController rMotionController)
        {
            EditorHelper.DrawSmallTitle("Spell Casting");
            EditorHelper.DrawLink("See the latest User's Guide", "www.ootii.com/Unity/MotionControllerPacks/SpellCasting/SpellCasting_UserGuide.pdf");
            EditorGUILayout.LabelField("1. Download and import animations.", EditorHelper.SmallLabel);
            EditorGUILayout.LabelField("2. Unzip and replace animation meta files.", EditorHelper.SmallLabel);
            EditorGUILayout.LabelField("3. Select options and create motions.", EditorHelper.SmallLabel);

            EditorGUILayout.BeginVertical(EditorHelper.Box);

            EditorHelper.IntField("Animator Layer", "Layer onwhich we'll create SOME of the sub-state machines and motions. Some motions (ie movement motions) should always be on the first layer.", mAnimatorLayer);
            mAnimatorLayer = EditorHelper.FieldIntValue;

            EditorHelper.BoolField("Create Mecanim", "Determines if we create/override the existing sub-state machine", mCreateSubStateMachines);
            mCreateSubStateMachines = EditorHelper.FieldBoolValue;

            EditorHelper.BoolField("Create Motions", "Determines if we create the motions", mCreateMotions);
            mCreateMotions = EditorHelper.FieldBoolValue;

            EditorGUILayout.EndVertical();

            EditorGUILayout.BeginVertical(EditorHelper.Box);

            EditorHelper.BoolField("Create Input Aliases", "Determines if we create input aliases", mCreateInputAliases);
            mCreateInputAliases = EditorHelper.FieldBoolValue;

            EditorHelper.BoolField("Create Inventory", "Determines if we create/override the existing inventory", mCreateInventory);
            mCreateInventory = EditorHelper.FieldBoolValue;

            EditorHelper.BoolField("Create Spell Inventory", "Create the spell inventory for the caster", mCreateSpellInventory);
            mCreateSpellInventory = EditorHelper.FieldBoolValue;

            EditorHelper.BoolField("Create Attributes", "Determines if we create/override the existing attributes", mCreateAttributes);
            mCreateAttributes = EditorHelper.FieldBoolValue;

            EditorGUILayout.EndVertical();

            if (GUILayout.Button(new GUIContent("Setup Pack", "Create and setup the motion pack."), EditorStyles.miniButton))
            {
                if (mCreateInventory)
                {
                    BasicInventory lInventory = rMotionController.gameObject.GetComponent<BasicInventory>();
                    if (lInventory == null) { lInventory = rMotionController.gameObject.AddComponent<BasicInventory>(); }

                    BasicInventoryItem lItem = lInventory.GetInventoryItem("Spell_01");
                    if (lItem != null) { lInventory.Items.Remove(lItem); }

                    lInventory.Items.Add(new BasicInventoryItem());
                    lInventory.Items[lInventory.Items.Count - 1].ID = "Spell_01";
                    lInventory.Items[lInventory.Items.Count - 1].EquipMotion = "BasicItemEquip";
                    lInventory.Items[lInventory.Items.Count - 1].EquipStyle = 300;
                    lInventory.Items[lInventory.Items.Count - 1].StoreMotion = "BasicItemStore";
                    lInventory.Items[lInventory.Items.Count - 1].StoreStyle = 300;

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
                    lWeaponSet.Stance = 8;
                    lWeaponSet.DefaultForm = 300;

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

                if (mCreateAttributes)
                {
                    BasicAttributes lAttributes = rMotionController.gameObject.GetComponent<BasicAttributes>();
                    if (lAttributes == null) { lAttributes = rMotionController.gameObject.AddComponent<BasicAttributes>(); }

                    BasicAttribute lAttribute = lAttributes.GetAttribute("Health");
                    if (lAttribute != null) { lAttributes.Items.Remove(lAttribute); }

                    lAttributes.Items.Add(new BasicAttribute());
                    lAttributes.Items[lAttributes.Items.Count - 1].ID = "Health";
                    lAttributes.Items[lAttributes.Items.Count - 1].SetValue<float>(100f);
                }

                if (mCreateSpellInventory)
                {
                    SpellInventory lSpells = rMotionController.gameObject.GetComponent<SpellInventory>();
                    if (lSpells == null) { lSpells = rMotionController.gameObject.AddComponent<SpellInventory>(); }
                }

                if (mCreateCore)
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

                    if (!lCore.StateExists("Stance")) { lCore.SetStateValue("Stance", 0); }
                    if (!lCore.StateExists("Default Form")) { lCore.SetStateValue("Default Form", 0); }
                    if (!lCore.StateExists("Current Form")) { lCore.SetStateValue("Current Form", 0); }

                    if (lCore.GetReactor<BasicAttackedReactor>() == null) { lCore.AddReactor(new BasicAttackedReactor()); }
                    if (lCore.GetReactor<BasicDamagedReactor>() == null) { lCore.AddReactor(new BasicDamagedReactor()); }
                    if (lCore.GetReactor<BasicKilledReactor>() == null) { lCore.AddReactor(new BasicKilledReactor()); }

                    lCore.IsAlive = true;
                }

                if (mCreateInputAliases)
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

                if (mCreateSubStateMachines)
                {
                    Animator lAnimator = rMotionController.Animator;
                    if (lAnimator == null) { lAnimator = rMotionController.gameObject.GetComponent<Animator>(); }

                    if (lAnimator != null)
                    {
                        UnityEditor.Animations.AnimatorController lAnimatorController = lAnimator.runtimeAnimatorController as UnityEditor.Animations.AnimatorController;
                        MotionPackDefinition.SetupAnimatorController(lAnimatorController);

                        if (mAnimatorLayer > 0) { ExtendEmptyMotion(rMotionController, 1); }
                        if (mAnimatorLayer > 0) { ExtendEmptyMotion(rMotionController, 2); }

                        ExtendBasicIdle(rMotionController, 0);
                        ExtendBasicJump(rMotionController, 0);
                        ExtendBasicWalkRunPivot(rMotionController, 0);
                        ExtendBasicWalkRunStrafe(rMotionController, 0);
                        ExtendBasicEquipStore(rMotionController, mAnimatorLayer);
                        ExtendBasicSpellCasting(rMotionController, mAnimatorLayer);
                        ExtendBasicDamaged(rMotionController, 0);
                        ExtendBasicDeath(rMotionController, 0);
                    }
                }

                if (mCreateMotions)
                {
                    IBaseCameraRig lCameraRig = rMotionController.CameraRig;
                    if (lCameraRig == null) { lCameraRig = rMotionController.ExtractCameraRig(rMotionController.CameraTransform); }

                    BasicIdle lIdle = rMotionController.GetMotion<BasicIdle>(0, true);
                    if (lIdle == null) { lIdle = rMotionController.CreateMotion<BasicIdle>(0); }

                    BasicWalkRunPivot lPivot = rMotionController.GetMotion<BasicWalkRunPivot>(0, true);
                    if (lPivot == null) { lPivot = rMotionController.CreateMotion<BasicWalkRunPivot>(0); }

                    BasicWalkRunStrafe lStrafe = rMotionController.GetMotion<BasicWalkRunStrafe>(0, true);
                    if (lStrafe == null) { lStrafe = rMotionController.CreateMotion<BasicWalkRunStrafe>(0); }

                    if (mAnimatorLayer > 0)
                    {
                        Empty lEmpty = rMotionController.GetMotion<Empty>(1, true);
                        if (lEmpty == null) { lEmpty = rMotionController.CreateMotion<Empty>(1); }
                    }

                    if (mAnimatorLayer > 0)
                    {
                        Empty lEmpty = rMotionController.GetMotion<Empty>(2, true);
                        if (lEmpty == null) { lEmpty = rMotionController.CreateMotion<Empty>(2); }
                    }

                    BasicItemEquip lEquip = rMotionController.GetMotion<BasicItemEquip>(mAnimatorLayer, true);
                    if (lEquip == null) { lEquip = rMotionController.CreateMotion<BasicItemEquip>(mAnimatorLayer); }

                    BasicItemStore lStore = rMotionController.GetMotion<BasicItemStore>(mAnimatorLayer, true);
                    if (lStore == null) { lStore = rMotionController.CreateMotion<BasicItemStore>(mAnimatorLayer); }

                    BasicSpellCasting lCasting = rMotionController.GetMotion<BasicSpellCasting>(mAnimatorLayer, true);
                    if (lCasting == null) { lCasting = rMotionController.CreateMotion<BasicSpellCasting>(mAnimatorLayer); }

                    lIdle.RotateWithInput = false;
                    lIdle.RotateWithCamera = false;
                    rMotionController.SerializeMotion(lIdle);

                    lPivot.IsEnabled = true;
                    rMotionController.SerializeMotion(lPivot);

                    lStrafe.IsEnabled = true;
                    lStrafe.RequireTarget = true;
                    lStrafe.RotateWithInput = false;
                    lStrafe.RotateWithCamera = false;
                    rMotionController.SerializeMotion(lStrafe);
                }                

                EditorUtility.DisplayDialog("Motion Pack: Spell Casting", "Motion pack imported. ", "ok");

                return true;
            }

            return false;
        }

        /// <summary>
        /// New way to create sub-state machines without destroying what exists first.
        /// </summary>
        public static void ExtendEmptyMotion(MotionController rMotionController, int rLayerIndex)
        {
            UnityEditor.Animations.AnimatorController lController = null;

            Animator lAnimator = rMotionController.Animator;
            if (lAnimator == null) { lAnimator = rMotionController.gameObject.GetComponent<Animator>(); }
            if (lAnimator != null) { lController = lAnimator.runtimeAnimatorController as UnityEditor.Animations.AnimatorController; }
            if (lController == null) { return; }

            while (lController.layers.Length <= rLayerIndex)
            {
                UnityEditor.Animations.AnimatorControllerLayer lNewLayer = new UnityEditor.Animations.AnimatorControllerLayer();
                lNewLayer.name = "Layer " + (lController.layers.Length + 1);
                lNewLayer.stateMachine = new UnityEditor.Animations.AnimatorStateMachine();
                lController.AddLayer(lNewLayer);
            }

            UnityEditor.Animations.AnimatorControllerLayer lLayer = lController.layers[rLayerIndex];

            UnityEditor.Animations.AnimatorStateMachine lLayerStateMachine = lLayer.stateMachine;

            UnityEditor.Animations.AnimatorStateMachine lSSM_N22342 = MotionControllerMotion.EditorFindSSM(lLayerStateMachine, "Empty-SM");
            if (lSSM_N22342 == null) { lSSM_N22342 = lLayerStateMachine.AddStateMachine("Empty-SM", new Vector3(192, -480, 0)); }

            UnityEditor.Animations.AnimatorState lState_N22344 = MotionControllerMotion.EditorFindState(lSSM_N22342, "EmptyPose");
            if (lState_N22344 == null) { lState_N22344 = lSSM_N22342.AddState("EmptyPose", new Vector3(312, 84, 0)); }
            lState_N22344.speed = 1f;
            lState_N22344.mirror = false;
            lState_N22344.tag = "";

            UnityEditor.Animations.AnimatorStateTransition lAnyTransition_N22346 = MotionControllerMotion.EditorFindAnyStateTransition(lLayerStateMachine, lState_N22344, 0);
            if (lAnyTransition_N22346 == null) { lAnyTransition_N22346 = lLayerStateMachine.AddAnyStateTransition(lState_N22344); }
            lAnyTransition_N22346.isExit = false;
            lAnyTransition_N22346.hasExitTime = false;
            lAnyTransition_N22346.hasFixedDuration = true;
            lAnyTransition_N22346.exitTime = 0.75f;
            lAnyTransition_N22346.duration = 0.15f;
            lAnyTransition_N22346.offset = 0f;
            lAnyTransition_N22346.mute = false;
            lAnyTransition_N22346.solo = false;
            lAnyTransition_N22346.canTransitionToSelf = false;
            lAnyTransition_N22346.orderedInterruption = false;
            lAnyTransition_N22346.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)2;
            for (int i = lAnyTransition_N22346.conditions.Length - 1; i >= 0; i--) { lAnyTransition_N22346.RemoveCondition(lAnyTransition_N22346.conditions[i]); }
            lAnyTransition_N22346.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 3010f, "L" + rLayerIndex + "MotionPhase");
            lAnyTransition_N22346.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 0f, "L" + rLayerIndex + "MotionForm");
            lAnyTransition_N22346.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 0f, "L" + rLayerIndex + "MotionParameter");

            UnityEditor.Animations.AnimatorStateTransition lAnyTransition_N22348 = MotionControllerMotion.EditorFindAnyStateTransition(lLayerStateMachine, lState_N22344, 1);
            if (lAnyTransition_N22348 == null) { lAnyTransition_N22348 = lLayerStateMachine.AddAnyStateTransition(lState_N22344); }
            lAnyTransition_N22348.isExit = false;
            lAnyTransition_N22348.hasExitTime = false;
            lAnyTransition_N22348.hasFixedDuration = true;
            lAnyTransition_N22348.exitTime = 0.75f;
            lAnyTransition_N22348.duration = 0f;
            lAnyTransition_N22348.offset = 0f;
            lAnyTransition_N22348.mute = false;
            lAnyTransition_N22348.solo = false;
            lAnyTransition_N22348.canTransitionToSelf = false;
            lAnyTransition_N22348.orderedInterruption = false;
            lAnyTransition_N22348.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)2;
            for (int i = lAnyTransition_N22348.conditions.Length - 1; i >= 0; i--) { lAnyTransition_N22348.RemoveCondition(lAnyTransition_N22348.conditions[i]); }
            lAnyTransition_N22348.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 3010f, "L" + rLayerIndex + "MotionPhase");
            lAnyTransition_N22348.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 0f, "L" + rLayerIndex + "MotionForm");
            lAnyTransition_N22348.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 1f, "L" + rLayerIndex + "MotionParameter");
        }

        /// <summary>
        /// New way to create sub-state machines without destroying what exists first.
        /// </summary>
        public static void ExtendBasicIdle(MotionController rMotionController, int rLayerIndex)
        {
            UnityEditor.Animations.AnimatorController lController = null;

            Animator lAnimator = rMotionController.Animator;
            if (lAnimator == null) { lAnimator = rMotionController.gameObject.GetComponent<Animator>(); }
            if (lAnimator != null) { lController = lAnimator.runtimeAnimatorController as UnityEditor.Animations.AnimatorController; }
            if (lController == null) { return; }

            while (lController.layers.Length <= rLayerIndex)
            {
                UnityEditor.Animations.AnimatorControllerLayer lNewLayer = new UnityEditor.Animations.AnimatorControllerLayer();
                lNewLayer.name = "Layer " + (lController.layers.Length + 1);
                lNewLayer.stateMachine = new UnityEditor.Animations.AnimatorStateMachine();
                lController.AddLayer(lNewLayer);
            }

            UnityEditor.Animations.AnimatorControllerLayer lLayer = lController.layers[rLayerIndex];

            UnityEditor.Animations.AnimatorStateMachine lLayerStateMachine = lLayer.stateMachine;

            UnityEditor.Animations.AnimatorStateMachine lSSM_37922 = MotionControllerMotion.EditorFindSSM(lLayerStateMachine, "BasicIdle-SM");
            if (lSSM_37922 == null) { lSSM_37922 = lLayerStateMachine.AddStateMachine("BasicIdle-SM", new Vector3(192, -1056, 0)); }

            UnityEditor.Animations.AnimatorState lState_38398 = MotionControllerMotion.EditorFindState(lSSM_37922, "Unarmed Idle Pose");
            if (lState_38398 == null) { lState_38398 = lSSM_37922.AddState("Unarmed Idle Pose", new Vector3(312, 84, 0)); }
            lState_38398.speed = 1f;
            lState_38398.mirror = false;
            lState_38398.tag = "";
            lState_38398.motion = MotionControllerMotion.EditorFindAnimationClip("Assets/ootii/MotionController/Content/Animations/Humanoid/Idling/unity_Idle_IdleToIdlesR.fbx", "IdlePose");

            UnityEditor.Animations.AnimatorState lState_38438 = MotionControllerMotion.EditorFindState(lSSM_37922, "SpellCasting Idle Pose");
            if (lState_38438 == null) { lState_38438 = lSSM_37922.AddState("SpellCasting Idle Pose", new Vector3(312, 264, 0)); }
            lState_38438.speed = 1f;
            lState_38438.mirror = false;
            lState_38438.tag = "";
            lState_38438.motion = MotionControllerMotion.EditorFindAnimationClip("Assets/ootii/MotionControllerPacks/SpellCasting/Content/Animations/Mixamo/standing idle.fbx", "PMP_IdlePose");

            UnityEditor.Animations.AnimatorStateTransition lAnyTransition_38106 = MotionControllerMotion.EditorFindAnyStateTransition(lLayerStateMachine, lState_38398, 0);
            if (lAnyTransition_38106 == null) { lAnyTransition_38106 = lLayerStateMachine.AddAnyStateTransition(lState_38398); }
            lAnyTransition_38106.isExit = false;
            lAnyTransition_38106.hasExitTime = false;
            lAnyTransition_38106.hasFixedDuration = true;
            lAnyTransition_38106.exitTime = 0.75f;
            lAnyTransition_38106.duration = 0.1f;
            lAnyTransition_38106.offset = 0f;
            lAnyTransition_38106.mute = false;
            lAnyTransition_38106.solo = false;
            lAnyTransition_38106.canTransitionToSelf = true;
            lAnyTransition_38106.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            for (int i = lAnyTransition_38106.conditions.Length - 1; i >= 0; i--) { lAnyTransition_38106.RemoveCondition(lAnyTransition_38106.conditions[i]); }
            lAnyTransition_38106.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 3000f, "L" + rLayerIndex + "MotionPhase");
            lAnyTransition_38106.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 0f, "L" + rLayerIndex + "MotionForm");
            lAnyTransition_38106.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 0f, "L" + rLayerIndex + "MotionParameter");

            UnityEditor.Animations.AnimatorStateTransition lAnyTransition_38108 = MotionControllerMotion.EditorFindAnyStateTransition(lLayerStateMachine, lState_38398, 1);
            if (lAnyTransition_38108 == null) { lAnyTransition_38108 = lLayerStateMachine.AddAnyStateTransition(lState_38398); }
            lAnyTransition_38108.isExit = false;
            lAnyTransition_38108.hasExitTime = false;
            lAnyTransition_38108.hasFixedDuration = true;
            lAnyTransition_38108.exitTime = 0.75f;
            lAnyTransition_38108.duration = 0f;
            lAnyTransition_38108.offset = 0f;
            lAnyTransition_38108.mute = false;
            lAnyTransition_38108.solo = false;
            lAnyTransition_38108.canTransitionToSelf = true;
            lAnyTransition_38108.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            for (int i = lAnyTransition_38108.conditions.Length - 1; i >= 0; i--) { lAnyTransition_38108.RemoveCondition(lAnyTransition_38108.conditions[i]); }
            lAnyTransition_38108.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 3000f, "L" + rLayerIndex + "MotionPhase");
            lAnyTransition_38108.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 0f, "L" + rLayerIndex + "MotionForm");
            lAnyTransition_38108.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 1f, "L" + rLayerIndex + "MotionParameter");

            UnityEditor.Animations.AnimatorStateTransition lAnyTransition_38152 = MotionControllerMotion.EditorFindAnyStateTransition(lLayerStateMachine, lState_38438, 0);
            if (lAnyTransition_38152 == null) { lAnyTransition_38152 = lLayerStateMachine.AddAnyStateTransition(lState_38438); }
            lAnyTransition_38152.isExit = false;
            lAnyTransition_38152.hasExitTime = false;
            lAnyTransition_38152.hasFixedDuration = true;
            lAnyTransition_38152.exitTime = 0.75f;
            lAnyTransition_38152.duration = 0.1f;
            lAnyTransition_38152.offset = 0f;
            lAnyTransition_38152.mute = false;
            lAnyTransition_38152.solo = false;
            lAnyTransition_38152.canTransitionToSelf = true;
            lAnyTransition_38152.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            for (int i = lAnyTransition_38152.conditions.Length - 1; i >= 0; i--) { lAnyTransition_38152.RemoveCondition(lAnyTransition_38152.conditions[i]); }
            lAnyTransition_38152.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 3000f, "L" + rLayerIndex + "MotionPhase");
            lAnyTransition_38152.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 300f, "L" + rLayerIndex + "MotionForm");
            lAnyTransition_38152.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 0f, "L" + rLayerIndex + "MotionParameter");

            UnityEditor.Animations.AnimatorStateTransition lAnyTransition_38154 = MotionControllerMotion.EditorFindAnyStateTransition(lLayerStateMachine, lState_38438, 1);
            if (lAnyTransition_38154 == null) { lAnyTransition_38154 = lLayerStateMachine.AddAnyStateTransition(lState_38438); }
            lAnyTransition_38154.isExit = false;
            lAnyTransition_38154.hasExitTime = false;
            lAnyTransition_38154.hasFixedDuration = true;
            lAnyTransition_38154.exitTime = 0.75f;
            lAnyTransition_38154.duration = 0f;
            lAnyTransition_38154.offset = 0f;
            lAnyTransition_38154.mute = false;
            lAnyTransition_38154.solo = false;
            lAnyTransition_38154.canTransitionToSelf = true;
            lAnyTransition_38154.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            for (int i = lAnyTransition_38154.conditions.Length - 1; i >= 0; i--) { lAnyTransition_38154.RemoveCondition(lAnyTransition_38154.conditions[i]); }
            lAnyTransition_38154.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 3000f, "L" + rLayerIndex + "MotionPhase");
            lAnyTransition_38154.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 300f, "L" + rLayerIndex + "MotionForm");
            lAnyTransition_38154.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 1f, "L" + rLayerIndex + "MotionParameter");
        }

        /// <summary>
        /// New way to create sub-state machines without destroying what exists first.
        /// </summary>
        public static void ExtendBasicJump(MotionController rMotionController, int rLayerIndex)
        {
            UnityEditor.Animations.AnimatorController lController = null;

            Animator lAnimator = rMotionController.Animator;
            if (lAnimator == null) { lAnimator = rMotionController.gameObject.GetComponent<Animator>(); }
            if (lAnimator != null) { lController = lAnimator.runtimeAnimatorController as UnityEditor.Animations.AnimatorController; }
            if (lController == null) { return; }

            while (lController.layers.Length <= rLayerIndex)
            {
                UnityEditor.Animations.AnimatorControllerLayer lNewLayer = new UnityEditor.Animations.AnimatorControllerLayer();
                lNewLayer.name = "Layer " + (lController.layers.Length + 1);
                lNewLayer.stateMachine = new UnityEditor.Animations.AnimatorStateMachine();
                lController.AddLayer(lNewLayer);
            }

            UnityEditor.Animations.AnimatorControllerLayer lLayer = lController.layers[rLayerIndex];

            UnityEditor.Animations.AnimatorStateMachine lLayerStateMachine = lLayer.stateMachine;

            UnityEditor.Animations.AnimatorStateMachine lSSM_31068 = MotionControllerMotion.EditorFindSSM(lLayerStateMachine, "BasicJump-SM");
            if (lSSM_31068 == null) { lSSM_31068 = lLayerStateMachine.AddStateMachine("BasicJump-SM", new Vector3(192, -864, 0)); }

            UnityEditor.Animations.AnimatorState lState_31422 = MotionControllerMotion.EditorFindState(lSSM_31068, "Unarmed Jump");
            if (lState_31422 == null) { lState_31422 = lSSM_31068.AddState("Unarmed Jump", new Vector3(360, -60, 0)); }
            lState_31422.speed = 1.1f;
            lState_31422.mirror = false;
            lState_31422.tag = "";
            lState_31422.motion = MotionControllerMotion.EditorFindAnimationClip("Assets/ootii/MotionController/Content/Animations/Humanoid/Jumping/ootii_StandingJump.fbx", "StandingJump");

            UnityEditor.Animations.AnimatorState lState_32404 = MotionControllerMotion.EditorFindState(lSSM_31068, "IdlePose");
            if (lState_32404 == null) { lState_32404 = lSSM_31068.AddState("IdlePose", new Vector3(600, -60, 0)); }
            lState_32404.speed = 1f;
            lState_32404.mirror = false;
            lState_32404.tag = "Exit";
            lState_32404.motion = MotionControllerMotion.EditorFindAnimationClip("Assets/ootii/MotionController/Content/Animations/Humanoid/Idling/unity_Idle_IdleToIdlesR.fbx", "IdlePose");

            UnityEditor.Animations.AnimatorState lState_N198638 = MotionControllerMotion.EditorFindState(lSSM_31068, "SpellCasting Jump");
            if (lState_N198638 == null) { lState_N198638 = lSSM_31068.AddState("SpellCasting Jump", new Vector3(360, 156, 0)); }
            lState_N198638.speed = 1.2f;
            lState_N198638.mirror = false;
            lState_N198638.tag = "";
            lState_N198638.motion = MotionControllerMotion.EditorFindAnimationClip("Assets/ootii/MotionControllerPacks/SpellCasting/Content/Animations/Mixamo/Standing Jump.fbx", "mixamo.com");

            UnityEditor.Animations.AnimatorState lState_N200918 = MotionControllerMotion.EditorFindState(lSSM_31068, "PMP_IdlePose");
            if (lState_N200918 == null) { lState_N200918 = lSSM_31068.AddState("PMP_IdlePose", new Vector3(600, 156, 0)); }
            lState_N200918.speed = 1f;
            lState_N200918.mirror = false;
            lState_N200918.tag = "Exit";
            lState_N200918.motion = MotionControllerMotion.EditorFindAnimationClip("Assets/ootii/MotionControllerPacks/SpellCasting/Content/Animations/Mixamo/standing idle.fbx", "PMP_IdlePose");

            UnityEditor.Animations.AnimatorStateTransition lAnyTransition_31250 = MotionControllerMotion.EditorFindAnyStateTransition(lLayerStateMachine, lState_31422, 0);
            if (lAnyTransition_31250 == null) { lAnyTransition_31250 = lLayerStateMachine.AddAnyStateTransition(lState_31422); }
            lAnyTransition_31250.isExit = false;
            lAnyTransition_31250.hasExitTime = false;
            lAnyTransition_31250.hasFixedDuration = true;
            lAnyTransition_31250.exitTime = 0.75f;
            lAnyTransition_31250.duration = 0.25f;
            lAnyTransition_31250.offset = 0f;
            lAnyTransition_31250.mute = false;
            lAnyTransition_31250.solo = false;
            lAnyTransition_31250.canTransitionToSelf = true;
            lAnyTransition_31250.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            for (int i = lAnyTransition_31250.conditions.Length - 1; i >= 0; i--) { lAnyTransition_31250.RemoveCondition(lAnyTransition_31250.conditions[i]); }
            lAnyTransition_31250.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 3400f, "L" + rLayerIndex + "MotionPhase");
            lAnyTransition_31250.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 0f, "L" + rLayerIndex + "MotionForm");
            lAnyTransition_31250.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 0f, "L" + rLayerIndex + "MotionParameter");

            UnityEditor.Animations.AnimatorStateTransition lAnyTransition_N224582 = MotionControllerMotion.EditorFindAnyStateTransition(lLayerStateMachine, lState_N198638, 0);
            if (lAnyTransition_N224582 == null) { lAnyTransition_N224582 = lLayerStateMachine.AddAnyStateTransition(lState_N198638); }
            lAnyTransition_N224582.isExit = false;
            lAnyTransition_N224582.hasExitTime = false;
            lAnyTransition_N224582.hasFixedDuration = true;
            lAnyTransition_N224582.exitTime = 0.75f;
            lAnyTransition_N224582.duration = 0.25f;
            lAnyTransition_N224582.offset = 0f;
            lAnyTransition_N224582.mute = false;
            lAnyTransition_N224582.solo = false;
            lAnyTransition_N224582.canTransitionToSelf = true;
            lAnyTransition_N224582.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            for (int i = lAnyTransition_N224582.conditions.Length - 1; i >= 0; i--) { lAnyTransition_N224582.RemoveCondition(lAnyTransition_N224582.conditions[i]); }
            lAnyTransition_N224582.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 3400f, "L" + rLayerIndex + "MotionPhase");
            lAnyTransition_N224582.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 300f, "L" + rLayerIndex + "MotionForm");
            lAnyTransition_N224582.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 0f, "L" + rLayerIndex + "MotionParameter");

            UnityEditor.Animations.AnimatorStateTransition lTransition_32406 = MotionControllerMotion.EditorFindTransition(lState_31422, lState_32404, 0);
            if (lTransition_32406 == null) { lTransition_32406 = lState_31422.AddTransition(lState_32404); }
            lTransition_32406.isExit = false;
            lTransition_32406.hasExitTime = true;
            lTransition_32406.hasFixedDuration = true;
            lTransition_32406.exitTime = 0.7643284f;
            lTransition_32406.duration = 0.25f;
            lTransition_32406.offset = 0f;
            lTransition_32406.mute = false;
            lTransition_32406.solo = false;
            lTransition_32406.canTransitionToSelf = true;
            lTransition_32406.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            for (int i = lTransition_32406.conditions.Length - 1; i >= 0; i--) { lTransition_32406.RemoveCondition(lTransition_32406.conditions[i]); }

            UnityEditor.Animations.AnimatorStateTransition lTransition_N201684 = MotionControllerMotion.EditorFindTransition(lState_N198638, lState_N200918, 0);
            if (lTransition_N201684 == null) { lTransition_N201684 = lState_N198638.AddTransition(lState_N200918); }
            lTransition_N201684.isExit = false;
            lTransition_N201684.hasExitTime = true;
            lTransition_N201684.hasFixedDuration = false;
            lTransition_N201684.exitTime = 0.7920555f;
            lTransition_N201684.duration = 0.04999993f;
            lTransition_N201684.offset = 0f;
            lTransition_N201684.mute = false;
            lTransition_N201684.solo = false;
            lTransition_N201684.canTransitionToSelf = true;
            lTransition_N201684.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            for (int i = lTransition_N201684.conditions.Length - 1; i >= 0; i--) { lTransition_N201684.RemoveCondition(lTransition_N201684.conditions[i]); }
        }

        /// <summary>
        /// New way to create sub-state machines without destroying what exists first.
        /// </summary>
        public static void ExtendBasicWalkRunPivot(MotionController rMotionController, int rLayerIndex)
        {
            UnityEditor.Animations.AnimatorController lController = null;

            Animator lAnimator = rMotionController.Animator;
            if (lAnimator == null) { lAnimator = rMotionController.gameObject.GetComponent<Animator>(); }
            if (lAnimator != null) { lController = lAnimator.runtimeAnimatorController as UnityEditor.Animations.AnimatorController; }
            if (lController == null) { return; }

            while (lController.layers.Length <= rLayerIndex)
            {
                UnityEditor.Animations.AnimatorControllerLayer lNewLayer = new UnityEditor.Animations.AnimatorControllerLayer();
                lNewLayer.name = "Layer " + (lController.layers.Length + 1);
                lNewLayer.stateMachine = new UnityEditor.Animations.AnimatorStateMachine();
                lController.AddLayer(lNewLayer);
            }

            UnityEditor.Animations.AnimatorControllerLayer lLayer = lController.layers[rLayerIndex];

            UnityEditor.Animations.AnimatorStateMachine lLayerStateMachine = lLayer.stateMachine;

            UnityEditor.Animations.AnimatorStateMachine lSSM_37924 = MotionControllerMotion.EditorFindSSM(lLayerStateMachine, "BasicWalkRunPivot-SM");
            if (lSSM_37924 == null) { lSSM_37924 = lLayerStateMachine.AddStateMachine("BasicWalkRunPivot-SM", new Vector3(408, -1056, 0)); }

            UnityEditor.Animations.AnimatorState lState_38400 = MotionControllerMotion.EditorFindState(lSSM_37924, "Unarmed BlendTree");
            if (lState_38400 == null) { lState_38400 = lSSM_37924.AddState("Unarmed BlendTree", new Vector3(312, 72, 0)); }
            lState_38400.speed = 1f;
            lState_38400.mirror = false;
            lState_38400.tag = "";

            UnityEditor.Animations.BlendTree lM_25576 = MotionControllerMotion.EditorCreateBlendTree("Blend Tree", lController, rLayerIndex);
            lM_25576.blendType = UnityEditor.Animations.BlendTreeType.Simple1D;
            lM_25576.blendParameter = "InputMagnitude";
            lM_25576.blendParameterY = "InputX";
#if !(UNITY_4_0 || UNITY_4_0_1 || UNITY_4_1 || UNITY_4_2 || UNITY_4_3 || UNITY_4_4 || UNITY_4_5 || UNITY_4_6 || UNITY_5_0 || UNITY_5_1 || UNITY_5_2 || UNITY_5_3)
            lM_25576.useAutomaticThresholds = false;
#endif
            lM_25576.AddChild(MotionControllerMotion.EditorFindAnimationClip("Assets/ootii/MotionController/Content/Animations/Humanoid/Idling/unity_Idle_IdleToIdlesR.fbx", "IdlePose"), 0f);
            lM_25576.AddChild(MotionControllerMotion.EditorFindAnimationClip("Assets/ootii/MotionController/Content/Animations/Humanoid/Walking/unity_WalkFWD_v2.fbx", "WalkForward"), 0.5f);
            lM_25576.AddChild(MotionControllerMotion.EditorFindAnimationClip("Assets/ootii/MotionController/Content/Animations/Humanoid/Running/RunForward_v2.fbx", "RunForward"), 1f);
            lState_38400.motion = lM_25576;

            UnityEditor.Animations.AnimatorState lState_38444 = MotionControllerMotion.EditorFindState(lSSM_37924, "Spell BlendTree");
            if (lState_38444 == null) { lState_38444 = lSSM_37924.AddState("Spell BlendTree", new Vector3(312, 252, 0)); }
            lState_38444.speed = 1f;
            lState_38444.mirror = false;
            lState_38444.tag = "";

            UnityEditor.Animations.BlendTree lM_39498 = MotionControllerMotion.EditorCreateBlendTree("Pivot Blend Tree", lController, rLayerIndex);
            lM_39498.blendType = UnityEditor.Animations.BlendTreeType.Simple1D;
            lM_39498.blendParameter = "InputMagnitude";
            lM_39498.blendParameterY = "InputX";
#if !(UNITY_4_0 || UNITY_4_0_1 || UNITY_4_1 || UNITY_4_2 || UNITY_4_3 || UNITY_4_4 || UNITY_4_5 || UNITY_4_6 || UNITY_5_0 || UNITY_5_1 || UNITY_5_2 || UNITY_5_3)
            lM_39498.useAutomaticThresholds = true;
#endif
            lM_39498.AddChild(MotionControllerMotion.EditorFindAnimationClip("Assets/ootii/MotionControllerPacks/SpellCasting/Content/Animations/Mixamo/standing idle.fbx", "standing idle"), 0f);
            lM_39498.AddChild(MotionControllerMotion.EditorFindAnimationClip("Assets/ootii/MotionControllerPacks/SpellCasting/Content/Animations/Mixamo/Standing Walk Forward.fbx", "Standing Walk Forward"), 0.5f);
            lM_39498.AddChild(MotionControllerMotion.EditorFindAnimationClip("Assets/ootii/MotionControllerPacks/SpellCasting/Content/Animations/Mixamo/Standing Run Forward.fbx", "Standing Run Forward"), 1f);
            lState_38444.motion = lM_39498;

            UnityEditor.Animations.AnimatorStateTransition lAnyTransition_38110 = MotionControllerMotion.EditorFindAnyStateTransition(lLayerStateMachine, lState_38400, 0);
            if (lAnyTransition_38110 == null) { lAnyTransition_38110 = lLayerStateMachine.AddAnyStateTransition(lState_38400); }
            lAnyTransition_38110.isExit = false;
            lAnyTransition_38110.hasExitTime = false;
            lAnyTransition_38110.hasFixedDuration = true;
            lAnyTransition_38110.exitTime = 0.75f;
            lAnyTransition_38110.duration = 0.25f;
            lAnyTransition_38110.offset = 0f;
            lAnyTransition_38110.mute = false;
            lAnyTransition_38110.solo = false;
            lAnyTransition_38110.canTransitionToSelf = true;
            lAnyTransition_38110.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            for (int i = lAnyTransition_38110.conditions.Length - 1; i >= 0; i--) { lAnyTransition_38110.RemoveCondition(lAnyTransition_38110.conditions[i]); }
            lAnyTransition_38110.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 3050f, "L" + rLayerIndex + "MotionPhase");
            lAnyTransition_38110.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 0f, "L" + rLayerIndex + "MotionForm");

            UnityEditor.Animations.AnimatorStateTransition lAnyTransition_38160 = MotionControllerMotion.EditorFindAnyStateTransition(lLayerStateMachine, lState_38444, 0);
            if (lAnyTransition_38160 == null) { lAnyTransition_38160 = lLayerStateMachine.AddAnyStateTransition(lState_38444); }
            lAnyTransition_38160.isExit = false;
            lAnyTransition_38160.hasExitTime = false;
            lAnyTransition_38160.hasFixedDuration = true;
            lAnyTransition_38160.exitTime = 0.75f;
            lAnyTransition_38160.duration = 0.25f;
            lAnyTransition_38160.offset = 0f;
            lAnyTransition_38160.mute = false;
            lAnyTransition_38160.solo = false;
            lAnyTransition_38160.canTransitionToSelf = true;
            lAnyTransition_38160.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            for (int i = lAnyTransition_38160.conditions.Length - 1; i >= 0; i--) { lAnyTransition_38160.RemoveCondition(lAnyTransition_38160.conditions[i]); }
            lAnyTransition_38160.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 3050f, "L" + rLayerIndex + "MotionPhase");
            lAnyTransition_38160.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 300f, "L" + rLayerIndex + "MotionForm");
        }

        /// <summary>
        /// New way to create sub-state machines without destroying what exists first.
        /// </summary>
        public static void ExtendBasicWalkRunStrafe(MotionController rMotionController, int rLayerIndex)
        {
            UnityEditor.Animations.AnimatorController lController = null;

            Animator lAnimator = rMotionController.Animator;
            if (lAnimator == null) { lAnimator = rMotionController.gameObject.GetComponent<Animator>(); }
            if (lAnimator != null) { lController = lAnimator.runtimeAnimatorController as UnityEditor.Animations.AnimatorController; }
            if (lController == null) { return; }

            while (lController.layers.Length <= rLayerIndex)
            {
                UnityEditor.Animations.AnimatorControllerLayer lNewLayer = new UnityEditor.Animations.AnimatorControllerLayer();
                lNewLayer.name = "Layer " + (lController.layers.Length + 1);
                lNewLayer.stateMachine = new UnityEditor.Animations.AnimatorStateMachine();
                lController.AddLayer(lNewLayer);
            }

            UnityEditor.Animations.AnimatorControllerLayer lLayer = lController.layers[rLayerIndex];

            UnityEditor.Animations.AnimatorStateMachine lLayerStateMachine = lLayer.stateMachine;

            UnityEditor.Animations.AnimatorStateMachine lSSM_37926 = MotionControllerMotion.EditorFindSSM(lLayerStateMachine, "BasicWalkRunStrafe-SM");
            if (lSSM_37926 == null) { lSSM_37926 = lLayerStateMachine.AddStateMachine("BasicWalkRunStrafe-SM", new Vector3(408, -1008, 0)); }

            UnityEditor.Animations.AnimatorState lState_38402 = MotionControllerMotion.EditorFindState(lSSM_37926, "Unarmed BlendTree");
            if (lState_38402 == null) { lState_38402 = lSSM_37926.AddState("Unarmed BlendTree", new Vector3(336, 24, 0)); }
            lState_38402.speed = 1f;
            lState_38402.mirror = false;
            lState_38402.tag = "";

            UnityEditor.Animations.BlendTree lM_25600 = MotionControllerMotion.EditorCreateBlendTree("Move Blend Tree", lController, rLayerIndex);
            lM_25600.blendType = UnityEditor.Animations.BlendTreeType.Simple1D;
            lM_25600.blendParameter = "InputMagnitude";
            lM_25600.blendParameterY = "InputX";
#if !(UNITY_4_0 || UNITY_4_0_1 || UNITY_4_1 || UNITY_4_2 || UNITY_4_3 || UNITY_4_4 || UNITY_4_5 || UNITY_4_6 || UNITY_5_0 || UNITY_5_1 || UNITY_5_2 || UNITY_5_3)
            lM_25600.useAutomaticThresholds = false;
#endif
            lM_25600.AddChild(MotionControllerMotion.EditorFindAnimationClip("Assets/ootii/MotionController/Content/Animations/Humanoid/Idling/unity_Idle_IdleToIdlesR.fbx", "IdlePose"), 0f);

            UnityEditor.Animations.BlendTree lM_25694 = MotionControllerMotion.EditorCreateBlendTree("WalkTree", lController, rLayerIndex);
            lM_25694.blendType = UnityEditor.Animations.BlendTreeType.SimpleDirectional2D;
            lM_25694.blendParameter = "InputX";
            lM_25694.blendParameterY = "InputY";
#if !(UNITY_4_0 || UNITY_4_0_1 || UNITY_4_1 || UNITY_4_2 || UNITY_4_3 || UNITY_4_4 || UNITY_4_5 || UNITY_4_6 || UNITY_5_0 || UNITY_5_1 || UNITY_5_2 || UNITY_5_3)
            lM_25694.useAutomaticThresholds = true;
#endif
            lM_25694.AddChild(MotionControllerMotion.EditorFindAnimationClip("Assets/ootii/MotionController/Content/Animations/Humanoid/Walking/unity_WalkFWD_v2.fbx", "WalkForward"), new Vector2(0f, 0.35f));
            UnityEditor.Animations.ChildMotion[] lM_25694_0_Children = lM_25694.children;
            lM_25694_0_Children[lM_25694_0_Children.Length - 1].mirror = false;
            lM_25694_0_Children[lM_25694_0_Children.Length - 1].timeScale = 1.1f;
            lM_25694.children = lM_25694_0_Children;

            lM_25694.AddChild(MotionControllerMotion.EditorFindAnimationClip("Assets/ootii/MotionController/Content/Animations/Humanoid/Walking/unity_SWalk_v2.fbx", "SWalkForwardRight"), new Vector2(0.35f, 0.35f));
            UnityEditor.Animations.ChildMotion[] lM_25694_1_Children = lM_25694.children;
            lM_25694_1_Children[lM_25694_1_Children.Length - 1].mirror = false;
            lM_25694_1_Children[lM_25694_1_Children.Length - 1].timeScale = 1.2f;
            lM_25694.children = lM_25694_1_Children;

            lM_25694.AddChild(MotionControllerMotion.EditorFindAnimationClip("Assets/ootii/MotionController/Content/Animations/Humanoid/Walking/unity_SWalk_v2.fbx", "SWalkForwardLeft"), new Vector2(-0.35f, 0.35f));
            UnityEditor.Animations.ChildMotion[] lM_25694_2_Children = lM_25694.children;
            lM_25694_2_Children[lM_25694_2_Children.Length - 1].mirror = false;
            lM_25694_2_Children[lM_25694_2_Children.Length - 1].timeScale = 1.2f;
            lM_25694.children = lM_25694_2_Children;

            lM_25694.AddChild(MotionControllerMotion.EditorFindAnimationClip("Assets/ootii/MotionController/Content/Animations/Humanoid/Walking/unity_SWalk_v2.fbx", "SWalkLeft"), new Vector2(-0.35f, 0f));
            UnityEditor.Animations.ChildMotion[] lM_25694_3_Children = lM_25694.children;
            lM_25694_3_Children[lM_25694_3_Children.Length - 1].mirror = false;
            lM_25694_3_Children[lM_25694_3_Children.Length - 1].timeScale = 1.2f;
            lM_25694.children = lM_25694_3_Children;

            lM_25694.AddChild(MotionControllerMotion.EditorFindAnimationClip("Assets/ootii/MotionController/Content/Animations/Humanoid/Walking/unity_SWalk_v2.fbx", "SWalkRight"), new Vector2(0.35f, 0f));
            UnityEditor.Animations.ChildMotion[] lM_25694_4_Children = lM_25694.children;
            lM_25694_4_Children[lM_25694_4_Children.Length - 1].mirror = false;
            lM_25694_4_Children[lM_25694_4_Children.Length - 1].timeScale = 1.2f;
            lM_25694.children = lM_25694_4_Children;

            lM_25694.AddChild(MotionControllerMotion.EditorFindAnimationClip("Assets/ootii/MotionController/Content/Animations/Humanoid/Walking/unity_Idle2Strafe_AllAngles.fbx", "WalkStrafeBackwardsLeft"), new Vector2(-0.35f, -0.35f));
            UnityEditor.Animations.ChildMotion[] lM_25694_5_Children = lM_25694.children;
            lM_25694_5_Children[lM_25694_5_Children.Length - 1].mirror = false;
            lM_25694_5_Children[lM_25694_5_Children.Length - 1].timeScale = 1.1f;
            lM_25694.children = lM_25694_5_Children;

            lM_25694.AddChild(MotionControllerMotion.EditorFindAnimationClip("Assets/ootii/MotionController/Content/Animations/Humanoid/Walking/unity_Idle2Strafe_AllAngles.fbx", "WalkStrafeBackwardsRight"), new Vector2(0.35f, -0.35f));
            UnityEditor.Animations.ChildMotion[] lM_25694_6_Children = lM_25694.children;
            lM_25694_6_Children[lM_25694_6_Children.Length - 1].mirror = false;
            lM_25694_6_Children[lM_25694_6_Children.Length - 1].timeScale = 1.1f;
            lM_25694.children = lM_25694_6_Children;

            lM_25694.AddChild(MotionControllerMotion.EditorFindAnimationClip("Assets/ootii/MotionController/Content/Animations/Humanoid/Walking/unity_BWalk.fbx", "WalkBackwards"), new Vector2(0f, -0.35f));
            UnityEditor.Animations.ChildMotion[] lM_25694_7_Children = lM_25694.children;
            lM_25694_7_Children[lM_25694_7_Children.Length - 1].mirror = false;
            lM_25694_7_Children[lM_25694_7_Children.Length - 1].timeScale = 1f;
            lM_25694.children = lM_25694_7_Children;

            lM_25600.AddChild(lM_25694, 0.5f);

            UnityEditor.Animations.BlendTree lM_25630 = MotionControllerMotion.EditorCreateBlendTree("RunTree", lController, rLayerIndex);
            lM_25630.blendType = UnityEditor.Animations.BlendTreeType.SimpleDirectional2D;
            lM_25630.blendParameter = "InputX";
            lM_25630.blendParameterY = "InputY";
#if !(UNITY_4_0 || UNITY_4_0_1 || UNITY_4_1 || UNITY_4_2 || UNITY_4_3 || UNITY_4_4 || UNITY_4_5 || UNITY_4_6 || UNITY_5_0 || UNITY_5_1 || UNITY_5_2 || UNITY_5_3)
            lM_25630.useAutomaticThresholds = true;
#endif
            lM_25630.AddChild(MotionControllerMotion.EditorFindAnimationClip("Assets/ootii/MotionController/Content/Animations/Humanoid/Running/RunForward_v2.fbx", "RunForward"), new Vector2(0f, 0.7f));
            UnityEditor.Animations.ChildMotion[] lM_25630_0_Children = lM_25630.children;
            lM_25630_0_Children[lM_25630_0_Children.Length - 1].mirror = false;
            lM_25630_0_Children[lM_25630_0_Children.Length - 1].timeScale = 1f;
            lM_25630.children = lM_25630_0_Children;

            lM_25630.AddChild(MotionControllerMotion.EditorFindAnimationClip("Assets/ootii/MotionController/Content/Animations/Humanoid/Running/RunStrafe.fbx", "RunStrafeForwardRight"), new Vector2(0.7f, 0.7f));
            UnityEditor.Animations.ChildMotion[] lM_25630_1_Children = lM_25630.children;
            lM_25630_1_Children[lM_25630_1_Children.Length - 1].mirror = false;
            lM_25630_1_Children[lM_25630_1_Children.Length - 1].timeScale = 1.1f;
            lM_25630.children = lM_25630_1_Children;

            lM_25630.AddChild(MotionControllerMotion.EditorFindAnimationClip("Assets/ootii/MotionController/Content/Animations/Humanoid/Running/RunStrafe.fbx", "RunStrafeForwardLeft"), new Vector2(-0.7f, 0.7f));
            UnityEditor.Animations.ChildMotion[] lM_25630_2_Children = lM_25630.children;
            lM_25630_2_Children[lM_25630_2_Children.Length - 1].mirror = false;
            lM_25630_2_Children[lM_25630_2_Children.Length - 1].timeScale = 1.1f;
            lM_25630.children = lM_25630_2_Children;

            lM_25630.AddChild(MotionControllerMotion.EditorFindAnimationClip("Assets/ootii/MotionController/Content/Animations/Humanoid/Running/RunStrafe.fbx", "RunStrafeLeft"), new Vector2(-0.7f, 0f));
            UnityEditor.Animations.ChildMotion[] lM_25630_3_Children = lM_25630.children;
            lM_25630_3_Children[lM_25630_3_Children.Length - 1].mirror = false;
            lM_25630_3_Children[lM_25630_3_Children.Length - 1].timeScale = 1f;
            lM_25630.children = lM_25630_3_Children;

            lM_25630.AddChild(MotionControllerMotion.EditorFindAnimationClip("Assets/ootii/MotionController/Content/Animations/Humanoid/Running/RunStrafe.fbx", "RunStrafeRight"), new Vector2(0.7f, 0f));
            UnityEditor.Animations.ChildMotion[] lM_25630_4_Children = lM_25630.children;
            lM_25630_4_Children[lM_25630_4_Children.Length - 1].mirror = false;
            lM_25630_4_Children[lM_25630_4_Children.Length - 1].timeScale = 1f;
            lM_25630.children = lM_25630_4_Children;

            lM_25630.AddChild(MotionControllerMotion.EditorFindAnimationClip("Assets/ootii/MotionController/Content/Animations/Humanoid/Running/RunStrafe.fbx", "RunStrafeBackwardLeft"), new Vector2(-0.7f, -0.7f));
            UnityEditor.Animations.ChildMotion[] lM_25630_5_Children = lM_25630.children;
            lM_25630_5_Children[lM_25630_5_Children.Length - 1].mirror = false;
            lM_25630_5_Children[lM_25630_5_Children.Length - 1].timeScale = 1.1f;
            lM_25630.children = lM_25630_5_Children;

            lM_25630.AddChild(MotionControllerMotion.EditorFindAnimationClip("Assets/ootii/MotionController/Content/Animations/Humanoid/Running/RunStrafe.fbx", "RunStrafeBackwardRight"), new Vector2(0.7f, -0.7f));
            UnityEditor.Animations.ChildMotion[] lM_25630_6_Children = lM_25630.children;
            lM_25630_6_Children[lM_25630_6_Children.Length - 1].mirror = false;
            lM_25630_6_Children[lM_25630_6_Children.Length - 1].timeScale = 1.1f;
            lM_25630.children = lM_25630_6_Children;

            lM_25630.AddChild(MotionControllerMotion.EditorFindAnimationClip("Assets/ootii/MotionController/Content/Animations/Humanoid/Running/RunBackward.fbx", "RunBackwards"), new Vector2(0f, -0.7f));
            UnityEditor.Animations.ChildMotion[] lM_25630_7_Children = lM_25630.children;
            lM_25630_7_Children[lM_25630_7_Children.Length - 1].mirror = false;
            lM_25630_7_Children[lM_25630_7_Children.Length - 1].timeScale = 1f;
            lM_25630.children = lM_25630_7_Children;

            lM_25600.AddChild(lM_25630, 1f);
            lState_38402.motion = lM_25600;

            UnityEditor.Animations.AnimatorState lState_38446 = MotionControllerMotion.EditorFindState(lSSM_37926, "Spell BlendTree");
            if (lState_38446 == null) { lState_38446 = lSSM_37926.AddState("Spell BlendTree", new Vector3(336, 240, 0)); }
            lState_38446.speed = 1f;
            lState_38446.mirror = false;
            lState_38446.tag = "";

            UnityEditor.Animations.BlendTree lM_39500 = MotionControllerMotion.EditorCreateBlendTree("Move Blend Tree", lController, rLayerIndex);
            lM_39500.blendType = UnityEditor.Animations.BlendTreeType.Simple1D;
            lM_39500.blendParameter = "InputMagnitude";
            lM_39500.blendParameterY = "InputX";
#if !(UNITY_4_0 || UNITY_4_0_1 || UNITY_4_1 || UNITY_4_2 || UNITY_4_3 || UNITY_4_4 || UNITY_4_5 || UNITY_4_6 || UNITY_5_0 || UNITY_5_1 || UNITY_5_2 || UNITY_5_3)
            lM_39500.useAutomaticThresholds = true;
#endif
            lM_39500.AddChild(MotionControllerMotion.EditorFindAnimationClip("Assets/ootii/MotionControllerPacks/SpellCasting/Content/Animations/Mixamo/standing idle.fbx", "PMP_IdlePose"), 0f);

            UnityEditor.Animations.BlendTree lM_39502 = MotionControllerMotion.EditorCreateBlendTree("WalkTree", lController, rLayerIndex);
            lM_39502.blendType = UnityEditor.Animations.BlendTreeType.SimpleDirectional2D;
            lM_39502.blendParameter = "InputX";
            lM_39502.blendParameterY = "InputY";
#if !(UNITY_4_0 || UNITY_4_0_1 || UNITY_4_1 || UNITY_4_2 || UNITY_4_3 || UNITY_4_4 || UNITY_4_5 || UNITY_4_6 || UNITY_5_0 || UNITY_5_1 || UNITY_5_2 || UNITY_5_3)
            lM_39502.useAutomaticThresholds = true;
#endif
            lM_39502.AddChild(MotionControllerMotion.EditorFindAnimationClip("Assets/ootii/MotionControllerPacks/SpellCasting/Content/Animations/Mixamo/Standing Walk Forward.fbx", "Standing Walk Forward"), new Vector2(0f, 0.35f));
            UnityEditor.Animations.ChildMotion[] lM_39502_0_Children = lM_39502.children;
            lM_39502_0_Children[lM_39502_0_Children.Length - 1].mirror = false;
            lM_39502_0_Children[lM_39502_0_Children.Length - 1].timeScale = 1f;
            lM_39502.children = lM_39502_0_Children;

            lM_39502.AddChild(MotionControllerMotion.EditorFindAnimationClip("Assets/ootii/MotionControllerPacks/SpellCasting/Content/Animations/Mixamo/Standing Walk Left.fbx", "Standing Walk Left"), new Vector2(-0.35f, 0f));
            UnityEditor.Animations.ChildMotion[] lM_39502_1_Children = lM_39502.children;
            lM_39502_1_Children[lM_39502_1_Children.Length - 1].mirror = false;
            lM_39502_1_Children[lM_39502_1_Children.Length - 1].timeScale = 1f;
            lM_39502.children = lM_39502_1_Children;

            lM_39502.AddChild(MotionControllerMotion.EditorFindAnimationClip("Assets/ootii/MotionControllerPacks/SpellCasting/Content/Animations/Mixamo/Standing Walk Right.fbx", "Standing Walk Right"), new Vector2(0.35f, 0f));
            UnityEditor.Animations.ChildMotion[] lM_39502_2_Children = lM_39502.children;
            lM_39502_2_Children[lM_39502_2_Children.Length - 1].mirror = false;
            lM_39502_2_Children[lM_39502_2_Children.Length - 1].timeScale = 1f;
            lM_39502.children = lM_39502_2_Children;

            lM_39502.AddChild(MotionControllerMotion.EditorFindAnimationClip("Assets/ootii/MotionControllerPacks/SpellCasting/Content/Animations/Mixamo/Standing Walk Back.fbx", "Standing Walk Back"), new Vector2(0f, -0.35f));
            UnityEditor.Animations.ChildMotion[] lM_39502_3_Children = lM_39502.children;
            lM_39502_3_Children[lM_39502_3_Children.Length - 1].mirror = false;
            lM_39502_3_Children[lM_39502_3_Children.Length - 1].timeScale = 1f;
            lM_39502.children = lM_39502_3_Children;

            lM_39500.AddChild(lM_39502, 0.5f);

            UnityEditor.Animations.BlendTree lM_39504 = MotionControllerMotion.EditorCreateBlendTree("RunTree", lController, rLayerIndex);
            lM_39504.blendType = UnityEditor.Animations.BlendTreeType.SimpleDirectional2D;
            lM_39504.blendParameter = "InputX";
            lM_39504.blendParameterY = "InputY";
#if !(UNITY_4_0 || UNITY_4_0_1 || UNITY_4_1 || UNITY_4_2 || UNITY_4_3 || UNITY_4_4 || UNITY_4_5 || UNITY_4_6 || UNITY_5_0 || UNITY_5_1 || UNITY_5_2 || UNITY_5_3)
            lM_39504.useAutomaticThresholds = true;
#endif
            lM_39504.AddChild(MotionControllerMotion.EditorFindAnimationClip("Assets/ootii/MotionControllerPacks/SpellCasting/Content/Animations/Mixamo/Standing Run Forward.fbx", "Standing Run Forward"), new Vector2(0f, 0.7f));
            UnityEditor.Animations.ChildMotion[] lM_39504_0_Children = lM_39504.children;
            lM_39504_0_Children[lM_39504_0_Children.Length - 1].mirror = false;
            lM_39504_0_Children[lM_39504_0_Children.Length - 1].timeScale = 1f;
            lM_39504.children = lM_39504_0_Children;

            lM_39504.AddChild(MotionControllerMotion.EditorFindAnimationClip("Assets/ootii/MotionControllerPacks/SpellCasting/Content/Animations/Mixamo/Standing Run Left.fbx", "Standing Run Left"), new Vector2(-0.7f, 0f));
            UnityEditor.Animations.ChildMotion[] lM_39504_1_Children = lM_39504.children;
            lM_39504_1_Children[lM_39504_1_Children.Length - 1].mirror = false;
            lM_39504_1_Children[lM_39504_1_Children.Length - 1].timeScale = 1f;
            lM_39504.children = lM_39504_1_Children;

            lM_39504.AddChild(MotionControllerMotion.EditorFindAnimationClip("Assets/ootii/MotionControllerPacks/SpellCasting/Content/Animations/Mixamo/Standing Run Right.fbx", "Standing Run Right"), new Vector2(0.7f, 0f));
            UnityEditor.Animations.ChildMotion[] lM_39504_2_Children = lM_39504.children;
            lM_39504_2_Children[lM_39504_2_Children.Length - 1].mirror = false;
            lM_39504_2_Children[lM_39504_2_Children.Length - 1].timeScale = 1f;
            lM_39504.children = lM_39504_2_Children;

            lM_39504.AddChild(MotionControllerMotion.EditorFindAnimationClip("Assets/ootii/MotionControllerPacks/SpellCasting/Content/Animations/Mixamo/Standing Run Back.fbx", "Standing Run Back"), new Vector2(0f, -0.7f));
            UnityEditor.Animations.ChildMotion[] lM_39504_3_Children = lM_39504.children;
            lM_39504_3_Children[lM_39504_3_Children.Length - 1].mirror = false;
            lM_39504_3_Children[lM_39504_3_Children.Length - 1].timeScale = 1f;
            lM_39504.children = lM_39504_3_Children;

            lM_39500.AddChild(lM_39504, 1f);
            lState_38446.motion = lM_39500;

            UnityEditor.Animations.AnimatorStateTransition lAnyTransition_38112 = MotionControllerMotion.EditorFindAnyStateTransition(lLayerStateMachine, lState_38402, 0);
            if (lAnyTransition_38112 == null) { lAnyTransition_38112 = lLayerStateMachine.AddAnyStateTransition(lState_38402); }
            lAnyTransition_38112.isExit = false;
            lAnyTransition_38112.hasExitTime = false;
            lAnyTransition_38112.hasFixedDuration = true;
            lAnyTransition_38112.exitTime = 0.9f;
            lAnyTransition_38112.duration = 0.2f;
            lAnyTransition_38112.offset = 0f;
            lAnyTransition_38112.mute = false;
            lAnyTransition_38112.solo = false;
            lAnyTransition_38112.canTransitionToSelf = true;
            lAnyTransition_38112.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            for (int i = lAnyTransition_38112.conditions.Length - 1; i >= 0; i--) { lAnyTransition_38112.RemoveCondition(lAnyTransition_38112.conditions[i]); }
            lAnyTransition_38112.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 3100f, "L" + rLayerIndex + "MotionPhase");
            lAnyTransition_38112.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 0f, "L" + rLayerIndex + "MotionForm");

            UnityEditor.Animations.AnimatorStateTransition lAnyTransition_38162 = MotionControllerMotion.EditorFindAnyStateTransition(lLayerStateMachine, lState_38446, 0);
            if (lAnyTransition_38162 == null) { lAnyTransition_38162 = lLayerStateMachine.AddAnyStateTransition(lState_38446); }
            lAnyTransition_38162.isExit = false;
            lAnyTransition_38162.hasExitTime = false;
            lAnyTransition_38162.hasFixedDuration = true;
            lAnyTransition_38162.exitTime = 0.75f;
            lAnyTransition_38162.duration = 0.25f;
            lAnyTransition_38162.offset = 0f;
            lAnyTransition_38162.mute = false;
            lAnyTransition_38162.solo = false;
            lAnyTransition_38162.canTransitionToSelf = true;
            lAnyTransition_38162.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            for (int i = lAnyTransition_38162.conditions.Length - 1; i >= 0; i--) { lAnyTransition_38162.RemoveCondition(lAnyTransition_38162.conditions[i]); }
            lAnyTransition_38162.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 3100f, "L" + rLayerIndex + "MotionPhase");
            lAnyTransition_38162.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 300f, "L" + rLayerIndex + "MotionForm");
        }

        /// <summary>
        /// New way to create sub-state machines without destroying what exists first.
        /// </summary>
        public static void ExtendBasicEquipStore(MotionController rMotionController, int rLayerIndex)
        {
            UnityEditor.Animations.AnimatorController lController = null;

            Animator lAnimator = rMotionController.Animator;
            if (lAnimator == null) { lAnimator = rMotionController.gameObject.GetComponent<Animator>(); }
            if (lAnimator != null) { lController = lAnimator.runtimeAnimatorController as UnityEditor.Animations.AnimatorController; }
            if (lController == null) { return; }

            while (lController.layers.Length <= rLayerIndex)
            {
                UnityEditor.Animations.AnimatorControllerLayer lNewLayer = new UnityEditor.Animations.AnimatorControllerLayer();
                lNewLayer.name = "Layer " + (lController.layers.Length + 1);
                lNewLayer.stateMachine = new UnityEditor.Animations.AnimatorStateMachine();
                lController.AddLayer(lNewLayer);
            }

            UnityEditor.Animations.AnimatorControllerLayer lLayer = lController.layers[rLayerIndex];

            UnityEditor.Animations.AnimatorStateMachine lLayerStateMachine = lLayer.stateMachine;

            UnityEditor.Animations.AnimatorStateMachine lSSM_37928 = MotionControllerMotion.EditorFindSSM(lLayerStateMachine, "BasicEquipStore-SM");
            if (lSSM_37928 == null) { lSSM_37928 = lLayerStateMachine.AddStateMachine("BasicEquipStore-SM", new Vector3(192, -1008, 0)); }

            UnityEditor.Animations.AnimatorState lState_38440 = MotionControllerMotion.EditorFindState(lSSM_37928, "Equip Spell");
            if (lState_38440 == null) { lState_38440 = lSSM_37928.AddState("Equip Spell", new Vector3(300, 408, 0)); }
            lState_38440.speed = 1.1f;
            lState_38440.mirror = false;
            lState_38440.tag = "";
            lState_38440.motion = MotionControllerMotion.EditorFindAnimationClip("Assets/ootii/MotionController/Content/Animations/Humanoid/Idling/IdleToReady.fbx", "IdleToReady");

            UnityEditor.Animations.AnimatorState lState_38442 = MotionControllerMotion.EditorFindState(lSSM_37928, "Store Spell");
            if (lState_38442 == null) { lState_38442 = lSSM_37928.AddState("Store Spell", new Vector3(300, 456, 0)); }
            lState_38442.speed = -1.1f;
            lState_38442.mirror = false;
            lState_38442.tag = "";
            lState_38442.motion = MotionControllerMotion.EditorFindAnimationClip("Assets/ootii/MotionController/Content/Animations/Humanoid/Idling/IdleToReady.fbx", "IdleToReady");

            UnityEditor.Animations.AnimatorState lState_39522 = MotionControllerMotion.EditorFindState(lSSM_37928, "EquipSpellIdlePoseExit");
            if (lState_39522 == null) { lState_39522 = lSSM_37928.AddState("EquipSpellIdlePoseExit", new Vector3(552, 408, 0)); }
            lState_39522.speed = 1f;
            lState_39522.mirror = false;
            lState_39522.tag = "Exit";
            lState_39522.motion = MotionControllerMotion.EditorFindAnimationClip("Assets/ootii/MotionControllerPacks/SpellCasting/Content/Animations/Mixamo/standing idle.fbx", "PMP_IdlePose");

            UnityEditor.Animations.AnimatorState lState_39524 = MotionControllerMotion.EditorFindState(lSSM_37928, "StoreSpellIdlePoseExit");
            if (lState_39524 == null) { lState_39524 = lSSM_37928.AddState("StoreSpellIdlePoseExit", new Vector3(552, 456, 0)); }
            lState_39524.speed = 1f;
            lState_39524.mirror = false;
            lState_39524.tag = "Exit";
            lState_39524.motion = MotionControllerMotion.EditorFindAnimationClip("Assets/ootii/MotionController/Content/Animations/Humanoid/Idling/unity_Idle_IdleToIdlesR.fbx", "IdlePose");

            UnityEditor.Animations.AnimatorStateTransition lAnyTransition_38156 = MotionControllerMotion.EditorFindAnyStateTransition(lLayerStateMachine, lState_38440, 0);
            if (lAnyTransition_38156 == null) { lAnyTransition_38156 = lLayerStateMachine.AddAnyStateTransition(lState_38440); }
            lAnyTransition_38156.isExit = false;
            lAnyTransition_38156.hasExitTime = false;
            lAnyTransition_38156.hasFixedDuration = true;
            lAnyTransition_38156.exitTime = 0.75f;
            lAnyTransition_38156.duration = 0.25f;
            lAnyTransition_38156.offset = 0f;
            lAnyTransition_38156.mute = false;
            lAnyTransition_38156.solo = false;
            lAnyTransition_38156.canTransitionToSelf = true;
            lAnyTransition_38156.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            for (int i = lAnyTransition_38156.conditions.Length - 1; i >= 0; i--) { lAnyTransition_38156.RemoveCondition(lAnyTransition_38156.conditions[i]); }
            lAnyTransition_38156.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 3150f, "L" + rLayerIndex + "MotionPhase");
            lAnyTransition_38156.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 300f, "L" + rLayerIndex + "MotionForm");

            UnityEditor.Animations.AnimatorStateTransition lAnyTransition_38158 = MotionControllerMotion.EditorFindAnyStateTransition(lLayerStateMachine, lState_38442, 0);
            if (lAnyTransition_38158 == null) { lAnyTransition_38158 = lLayerStateMachine.AddAnyStateTransition(lState_38442); }
            lAnyTransition_38158.isExit = false;
            lAnyTransition_38158.hasExitTime = false;
            lAnyTransition_38158.hasFixedDuration = true;
            lAnyTransition_38158.exitTime = 0.75f;
            lAnyTransition_38158.duration = 0.25f;
            lAnyTransition_38158.offset = 0f;
            lAnyTransition_38158.mute = false;
            lAnyTransition_38158.solo = false;
            lAnyTransition_38158.canTransitionToSelf = true;
            lAnyTransition_38158.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            for (int i = lAnyTransition_38158.conditions.Length - 1; i >= 0; i--) { lAnyTransition_38158.RemoveCondition(lAnyTransition_38158.conditions[i]); }
            lAnyTransition_38158.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 3155f, "L" + rLayerIndex + "MotionPhase");
            lAnyTransition_38158.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 300f, "L" + rLayerIndex + "MotionForm");

            UnityEditor.Animations.AnimatorStateTransition lTransition_39542 = MotionControllerMotion.EditorFindTransition(lState_38440, lState_39522, 0);
            if (lTransition_39542 == null) { lTransition_39542 = lState_38440.AddTransition(lState_39522); }
            lTransition_39542.isExit = false;
            lTransition_39542.hasExitTime = true;
            lTransition_39542.hasFixedDuration = true;
            lTransition_39542.exitTime = 0.9f;
            lTransition_39542.duration = 0.1f;
            lTransition_39542.offset = 0f;
            lTransition_39542.mute = false;
            lTransition_39542.solo = false;
            lTransition_39542.canTransitionToSelf = true;
            lTransition_39542.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            for (int i = lTransition_39542.conditions.Length - 1; i >= 0; i--) { lTransition_39542.RemoveCondition(lTransition_39542.conditions[i]); }

            UnityEditor.Animations.AnimatorStateTransition lTransition_39544 = MotionControllerMotion.EditorFindTransition(lState_38442, lState_39524, 0);
            if (lTransition_39544 == null) { lTransition_39544 = lState_38442.AddTransition(lState_39524); }
            lTransition_39544.isExit = false;
            lTransition_39544.hasExitTime = true;
            lTransition_39544.hasFixedDuration = true;
            lTransition_39544.exitTime = 0.9f;
            lTransition_39544.duration = 0.1f;
            lTransition_39544.offset = 0f;
            lTransition_39544.mute = false;
            lTransition_39544.solo = false;
            lTransition_39544.canTransitionToSelf = true;
            lTransition_39544.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            for (int i = lTransition_39544.conditions.Length - 1; i >= 0; i--) { lTransition_39544.RemoveCondition(lTransition_39544.conditions[i]); }

        }

        /// <summary>
        /// New way to create sub-state machines without destroying what exists first.
        /// </summary>
        public static void ExtendBasicSpellCasting(MotionController rMotionController, int rLayerIndex)
        {
            UnityEditor.Animations.AnimatorController lController = null;

            Animator lAnimator = rMotionController.Animator;
            if (lAnimator == null) { lAnimator = rMotionController.gameObject.GetComponent<Animator>(); }
            if (lAnimator != null) { lController = lAnimator.runtimeAnimatorController as UnityEditor.Animations.AnimatorController; }
            if (lController == null) { return; }

            while (lController.layers.Length <= rLayerIndex)
            {
                UnityEditor.Animations.AnimatorControllerLayer lNewLayer = new UnityEditor.Animations.AnimatorControllerLayer();
                lNewLayer.name = "Layer " + (lController.layers.Length + 1);
                lNewLayer.stateMachine = new UnityEditor.Animations.AnimatorStateMachine();
                lController.AddLayer(lNewLayer);
            }

            UnityEditor.Animations.AnimatorControllerLayer lLayer = lController.layers[rLayerIndex];

            UnityEditor.Animations.AnimatorStateMachine lLayerStateMachine = lLayer.stateMachine;

            UnityEditor.Animations.AnimatorStateMachine lSSM_37934 = MotionControllerMotion.EditorFindSSM(lLayerStateMachine, "BasicSpellCasting-SM");
            if (lSSM_37934 == null) { lSSM_37934 = lLayerStateMachine.AddStateMachine("BasicSpellCasting-SM", new Vector3(840, -1008, 0)); }

            UnityEditor.Animations.AnimatorState lState_39612 = MotionControllerMotion.EditorFindState(lSSM_37934, "Spell Idle Out");
            if (lState_39612 == null) { lState_39612 = lSSM_37934.AddState("Spell Idle Out", new Vector3(1416, 132, 0)); }
            lState_39612.speed = 0.3f;
            lState_39612.mirror = false;
            lState_39612.tag = "";
            lState_39612.motion = MotionControllerMotion.EditorFindAnimationClip("Assets/ootii/MotionControllerPacks/SpellCasting/Content/Animations/Mixamo/standing idle.fbx", "PMP_IdlePose");

            UnityEditor.Animations.AnimatorState lState_38450 = MotionControllerMotion.EditorFindState(lSSM_37934, "1H_Cast_01");
            if (lState_38450 == null) { lState_38450 = lSSM_37934.AddState("1H_Cast_01", new Vector3(648, -216, 0)); }
            lState_38450.speed = 1.2f;
            lState_38450.mirror = false;
            lState_38450.tag = "";
            lState_38450.motion = MotionControllerMotion.EditorFindAnimationClip("Assets/ootii/MotionControllerPacks/SpellCasting/Content/Animations/Mixamo/Standing 1H Magic Attack 01.fbx", "Standing 1H Magic Attack 01");

            UnityEditor.Animations.AnimatorState lState_38448 = MotionControllerMotion.EditorFindState(lSSM_37934, "Stand Idle In");
            if (lState_38448 == null) { lState_38448 = lSSM_37934.AddState("Stand Idle In", new Vector3(300, 144, 0)); }
            lState_38448.speed = 1.4f;
            lState_38448.mirror = false;
            lState_38448.tag = "";
            lState_38448.motion = MotionControllerMotion.EditorFindAnimationClip("Assets/ootii/MotionController/Content/Animations/Humanoid/Idling/IdleToReady.fbx", "IdleToReady");

            UnityEditor.Animations.AnimatorState lState_39614 = MotionControllerMotion.EditorFindState(lSSM_37934, "Stand Idle Transition");
            if (lState_39614 == null) { lState_39614 = lSSM_37934.AddState("Stand Idle Transition", new Vector3(1428, 276, 0)); }
            lState_39614.speed = -1.4f;
            lState_39614.mirror = false;
            lState_39614.tag = "";
            lState_39614.motion = MotionControllerMotion.EditorFindAnimationClip("Assets/ootii/MotionController/Content/Animations/Humanoid/Idling/IdleToReady.fbx", "IdleToReady");

            UnityEditor.Animations.AnimatorState lState_39616 = MotionControllerMotion.EditorFindState(lSSM_37934, "Stand Idle Out");
            if (lState_39616 == null) { lState_39616 = lSSM_37934.AddState("Stand Idle Out", new Vector3(1656, 276, 0)); }
            lState_39616.speed = 1f;
            lState_39616.mirror = false;
            lState_39616.tag = "";
            lState_39616.motion = MotionControllerMotion.EditorFindAnimationClip("Assets/ootii/MotionController/Content/Animations/Humanoid/Idling/unity_Idle_IdleToIdlesR.fbx", "IdlePose");

            UnityEditor.Animations.AnimatorState lState_38452 = MotionControllerMotion.EditorFindState(lSSM_37934, "2H_Cast_01_a");
            if (lState_38452 == null) { lState_38452 = lSSM_37934.AddState("2H_Cast_01_a", new Vector3(648, 444, 0)); }
            lState_38452.speed = 1f;
            lState_38452.mirror = false;
            lState_38452.tag = "";
            lState_38452.motion = MotionControllerMotion.EditorFindAnimationClip("Assets/ootii/MotionControllerPacks/SpellCasting/Content/Animations/Mixamo/Standing 2H Cast Spell 01.fbx", "2H_Cast_01_a");

            UnityEditor.Animations.AnimatorState lState_39618 = MotionControllerMotion.EditorFindState(lSSM_37934, "2H_Cast_01_c");
            if (lState_39618 == null) { lState_39618 = lSSM_37934.AddState("2H_Cast_01_c", new Vector3(1104, 444, 0)); }
            lState_39618.speed = 1f;
            lState_39618.mirror = false;
            lState_39618.tag = "";
            lState_39618.motion = MotionControllerMotion.EditorFindAnimationClip("Assets/ootii/MotionControllerPacks/SpellCasting/Content/Animations/Mixamo/Standing 2H Cast Spell 01.fbx", "2H_Cast_01_c");

            UnityEditor.Animations.AnimatorState lState_39620 = MotionControllerMotion.EditorFindState(lSSM_37934, "2H_Cast_01_b");
            if (lState_39620 == null) { lState_39620 = lSSM_37934.AddState("2H_Cast_01_b", new Vector3(876, 444, 0)); }
            lState_39620.speed = 0.5f;
            lState_39620.mirror = false;
            lState_39620.tag = "";
            lState_39620.motion = MotionControllerMotion.EditorFindAnimationClip("Assets/ootii/MotionControllerPacks/SpellCasting/Content/Animations/Mixamo/Standing 2H Cast Spell 01.fbx", "2H_Cast_01_b");

            UnityEditor.Animations.AnimatorState lState_38454 = MotionControllerMotion.EditorFindState(lSSM_37934, "2H_Cast_01");
            if (lState_38454 == null) { lState_38454 = lSSM_37934.AddState("2H_Cast_01", new Vector3(648, 396, 0)); }
            lState_38454.speed = 0.6f;
            lState_38454.mirror = false;
            lState_38454.tag = "";
            lState_38454.motion = MotionControllerMotion.EditorFindAnimationClip("Assets/ootii/MotionControllerPacks/SpellCasting/Content/Animations/Mixamo/Standing 2H Cast Spell 01.fbx", "2H_Cast_01");

            UnityEditor.Animations.AnimatorState lState_38456 = MotionControllerMotion.EditorFindState(lSSM_37934, "1H_Cast_01_a");
            if (lState_38456 == null) { lState_38456 = lSSM_37934.AddState("1H_Cast_01_a", new Vector3(648, -168, 0)); }
            lState_38456.speed = 1f;
            lState_38456.mirror = false;
            lState_38456.tag = "";
            lState_38456.motion = MotionControllerMotion.EditorFindAnimationClip("Assets/ootii/MotionControllerPacks/SpellCasting/Content/Animations/Mixamo/Standing 1H Magic Attack 01.fbx", "1H_Cast_01_a");

            UnityEditor.Animations.AnimatorState lState_39622 = MotionControllerMotion.EditorFindState(lSSM_37934, "1H_Cast_01_b");
            if (lState_39622 == null) { lState_39622 = lSSM_37934.AddState("1H_Cast_01_b", new Vector3(876, -168, 0)); }
            lState_39622.speed = 1f;
            lState_39622.mirror = false;
            lState_39622.tag = "";
            lState_39622.motion = MotionControllerMotion.EditorFindAnimationClip("Assets/ootii/MotionControllerPacks/SpellCasting/Content/Animations/Mixamo/Standing 1H Magic Attack 01.fbx", "1H_Cast_01_b");

            UnityEditor.Animations.AnimatorState lState_39624 = MotionControllerMotion.EditorFindState(lSSM_37934, "1H_Cast_01_c");
            if (lState_39624 == null) { lState_39624 = lSSM_37934.AddState("1H_Cast_01_c", new Vector3(1104, -168, 0)); }
            lState_39624.speed = 1f;
            lState_39624.mirror = false;
            lState_39624.tag = "";
            lState_39624.motion = MotionControllerMotion.EditorFindAnimationClip("Assets/ootii/MotionControllerPacks/SpellCasting/Content/Animations/Mixamo/Standing 1H Magic Attack 01.fbx", "1H_Cast_01_c");

            UnityEditor.Animations.AnimatorState lState_38458 = MotionControllerMotion.EditorFindState(lSSM_37934, "Interrupted");
            if (lState_38458 == null) { lState_38458 = lSSM_37934.AddState("Interrupted", new Vector3(648, -312, 0)); }
            lState_38458.speed = 1f;
            lState_38458.mirror = false;
            lState_38458.tag = "";
            lState_38458.motion = MotionControllerMotion.EditorFindAnimationClip("Assets/ootii/MotionControllerPacks/SpellCasting/Content/Animations/Mixamo/Standing React Small From Back.fbx", "Standing React Small From Back");

            UnityEditor.Animations.AnimatorState lState_38460 = MotionControllerMotion.EditorFindState(lSSM_37934, "1H_Cast_02");
            if (lState_38460 == null) { lState_38460 = lSSM_37934.AddState("1H_Cast_02", new Vector3(648, -84, 0)); }
            lState_38460.speed = 1f;
            lState_38460.mirror = false;
            lState_38460.tag = "";
            lState_38460.motion = MotionControllerMotion.EditorFindAnimationClip("Assets/ootii/MotionControllerPacks/SpellCasting/Content/Animations/Mixamo/standing 1H cast spell 01.fbx", "standing 1H cast spell 01");

            UnityEditor.Animations.AnimatorState lState_38462 = MotionControllerMotion.EditorFindState(lSSM_37934, "1H_Cast_02_a");
            if (lState_38462 == null) { lState_38462 = lSSM_37934.AddState("1H_Cast_02_a", new Vector3(648, -36, 0)); }
            lState_38462.speed = 1f;
            lState_38462.mirror = false;
            lState_38462.tag = "";
            lState_38462.motion = MotionControllerMotion.EditorFindAnimationClip("Assets/ootii/MotionControllerPacks/SpellCasting/Content/Animations/Mixamo/standing 1H cast spell 01.fbx", "1H_Cast_02_a");

            UnityEditor.Animations.AnimatorState lState_39626 = MotionControllerMotion.EditorFindState(lSSM_37934, "1H_Cast_02_b");
            if (lState_39626 == null) { lState_39626 = lSSM_37934.AddState("1H_Cast_02_b", new Vector3(876, -36, 0)); }
            lState_39626.speed = 1f;
            lState_39626.mirror = false;
            lState_39626.tag = "";
            lState_39626.motion = MotionControllerMotion.EditorFindAnimationClip("Assets/ootii/MotionControllerPacks/SpellCasting/Content/Animations/Mixamo/standing 1H cast spell 01.fbx", "1H_Cast_02_b");

            UnityEditor.Animations.AnimatorState lState_39628 = MotionControllerMotion.EditorFindState(lSSM_37934, "1H_Cast_02_c");
            if (lState_39628 == null) { lState_39628 = lSSM_37934.AddState("1H_Cast_02_c", new Vector3(1104, -36, 0)); }
            lState_39628.speed = 1f;
            lState_39628.mirror = false;
            lState_39628.tag = "";
            lState_39628.motion = MotionControllerMotion.EditorFindAnimationClip("Assets/ootii/MotionControllerPacks/SpellCasting/Content/Animations/Mixamo/standing 1H cast spell 01.fbx", "1H_Cast_02_c");

            UnityEditor.Animations.AnimatorState lState_38464 = MotionControllerMotion.EditorFindState(lSSM_37934, "2H_Cast_02");
            if (lState_38464 == null) { lState_38464 = lSSM_37934.AddState("2H_Cast_02", new Vector3(648, 528, 0)); }
            lState_38464.speed = 1f;
            lState_38464.mirror = false;
            lState_38464.tag = "";
            lState_38464.motion = MotionControllerMotion.EditorFindAnimationClip("Assets/ootii/MotionControllerPacks/SpellCasting/Content/Animations/Mixamo/Standing 2H Magic Area Attack 01.fbx", "Standing 2H Magic Area Attack 01");

            UnityEditor.Animations.AnimatorState lState_38474 = MotionControllerMotion.EditorFindState(lSSM_37934, "2H_Cast_02_a");
            if (lState_38474 == null) { lState_38474 = lSSM_37934.AddState("2H_Cast_02_a", new Vector3(648, 576, 0)); }
            lState_38474.speed = 1f;
            lState_38474.mirror = false;
            lState_38474.tag = "";
            lState_38474.motion = MotionControllerMotion.EditorFindAnimationClip("Assets/ootii/MotionControllerPacks/SpellCasting/Content/Animations/Mixamo/Standing 2H Magic Area Attack 01.fbx", "2H_Cast_02_a");

            UnityEditor.Animations.AnimatorState lState_39630 = MotionControllerMotion.EditorFindState(lSSM_37934, "2H_Cast_02_b");
            if (lState_39630 == null) { lState_39630 = lSSM_37934.AddState("2H_Cast_02_b", new Vector3(876, 576, 0)); }
            lState_39630.speed = 1f;
            lState_39630.mirror = false;
            lState_39630.tag = "";
            lState_39630.motion = MotionControllerMotion.EditorFindAnimationClip("Assets/ootii/MotionControllerPacks/SpellCasting/Content/Animations/Mixamo/Standing 2H Magic Area Attack 01.fbx", "2H_Cast_02_b");

            UnityEditor.Animations.AnimatorState lState_39632 = MotionControllerMotion.EditorFindState(lSSM_37934, "2H_Cast_02_c");
            if (lState_39632 == null) { lState_39632 = lSSM_37934.AddState("2H_Cast_02_c", new Vector3(1104, 576, 0)); }
            lState_39632.speed = 1f;
            lState_39632.mirror = false;
            lState_39632.tag = "";
            lState_39632.motion = MotionControllerMotion.EditorFindAnimationClip("Assets/ootii/MotionControllerPacks/SpellCasting/Content/Animations/Mixamo/Standing 2H Magic Area Attack 01.fbx", "2H_Cast_02_c");

            UnityEditor.Animations.AnimatorState lState_38476 = MotionControllerMotion.EditorFindState(lSSM_37934, "2H_Cast_03");
            if (lState_38476 == null) { lState_38476 = lSSM_37934.AddState("2H_Cast_03", new Vector3(648, 672, 0)); }
            lState_38476.speed = 1f;
            lState_38476.mirror = false;
            lState_38476.tag = "";
            lState_38476.motion = MotionControllerMotion.EditorFindAnimationClip("Assets/ootii/MotionControllerPacks/SpellCasting/Content/Animations/Mixamo/Standing 2H Magic Area Attack 02.fbx", "2H_Cast_03");

            UnityEditor.Animations.AnimatorState lState_38478 = MotionControllerMotion.EditorFindState(lSSM_37934, "2H_Cast_03_a");
            if (lState_38478 == null) { lState_38478 = lSSM_37934.AddState("2H_Cast_03_a", new Vector3(648, 720, 0)); }
            lState_38478.speed = 1f;
            lState_38478.mirror = false;
            lState_38478.tag = "";
            lState_38478.motion = MotionControllerMotion.EditorFindAnimationClip("Assets/ootii/MotionControllerPacks/SpellCasting/Content/Animations/Mixamo/Standing 2H Magic Area Attack 02.fbx", "2H_Cast_03_a");

            UnityEditor.Animations.AnimatorState lState_39634 = MotionControllerMotion.EditorFindState(lSSM_37934, "2H_Cast_03_b");
            if (lState_39634 == null) { lState_39634 = lSSM_37934.AddState("2H_Cast_03_b", new Vector3(876, 720, 0)); }
            lState_39634.speed = 1f;
            lState_39634.mirror = false;
            lState_39634.tag = "";
            lState_39634.motion = MotionControllerMotion.EditorFindAnimationClip("Assets/ootii/MotionControllerPacks/SpellCasting/Content/Animations/Mixamo/Standing 2H Magic Area Attack 02.fbx", "2H_Cast_03_b");

            UnityEditor.Animations.AnimatorState lState_39636 = MotionControllerMotion.EditorFindState(lSSM_37934, "2H_Cast_03_c");
            if (lState_39636 == null) { lState_39636 = lSSM_37934.AddState("2H_Cast_03_c", new Vector3(1104, 720, 0)); }
            lState_39636.speed = 1f;
            lState_39636.mirror = false;
            lState_39636.tag = "";
            lState_39636.motion = MotionControllerMotion.EditorFindAnimationClip("Assets/ootii/MotionControllerPacks/SpellCasting/Content/Animations/Mixamo/Standing 2H Magic Area Attack 02.fbx", "2H_Cast_03_c");

            UnityEditor.Animations.AnimatorState lState_38480 = MotionControllerMotion.EditorFindState(lSSM_37934, "2H_Cast_04");
            if (lState_38480 == null) { lState_38480 = lSSM_37934.AddState("2H_Cast_04", new Vector3(648, 804, 0)); }
            lState_38480.speed = 1f;
            lState_38480.mirror = false;
            lState_38480.tag = "";
            lState_38480.motion = MotionControllerMotion.EditorFindAnimationClip("Assets/ootii/MotionControllerPacks/SpellCasting/Content/Animations/Mixamo/Standing 2H Magic Attack 01.fbx", "2H_Cast_04");

            UnityEditor.Animations.AnimatorState lState_38482 = MotionControllerMotion.EditorFindState(lSSM_37934, "2H_Cast_04_a");
            if (lState_38482 == null) { lState_38482 = lSSM_37934.AddState("2H_Cast_04_a", new Vector3(648, 852, 0)); }
            lState_38482.speed = 1f;
            lState_38482.mirror = false;
            lState_38482.tag = "";
            lState_38482.motion = MotionControllerMotion.EditorFindAnimationClip("Assets/ootii/MotionControllerPacks/SpellCasting/Content/Animations/Mixamo/Standing 2H Magic Attack 01.fbx", "2H_Cast_04_a");

            UnityEditor.Animations.AnimatorState lState_39638 = MotionControllerMotion.EditorFindState(lSSM_37934, "2H_Cast_04_b");
            if (lState_39638 == null) { lState_39638 = lSSM_37934.AddState("2H_Cast_04_b", new Vector3(876, 852, 0)); }
            lState_39638.speed = 1f;
            lState_39638.mirror = false;
            lState_39638.tag = "";
            lState_39638.motion = MotionControllerMotion.EditorFindAnimationClip("Assets/ootii/MotionControllerPacks/SpellCasting/Content/Animations/Mixamo/Standing 2H Magic Attack 01.fbx", "2H_Cast_04_b");

            UnityEditor.Animations.AnimatorState lState_39640 = MotionControllerMotion.EditorFindState(lSSM_37934, "2H_Cast_04_c");
            if (lState_39640 == null) { lState_39640 = lSSM_37934.AddState("2H_Cast_04_c", new Vector3(1104, 852, 0)); }
            lState_39640.speed = 1f;
            lState_39640.mirror = false;
            lState_39640.tag = "";
            lState_39640.motion = MotionControllerMotion.EditorFindAnimationClip("Assets/ootii/MotionControllerPacks/SpellCasting/Content/Animations/Mixamo/Standing 2H Magic Attack 01.fbx", "2H_Cast_04_c");

            UnityEditor.Animations.AnimatorState lState_38484 = MotionControllerMotion.EditorFindState(lSSM_37934, "2H_Cast_05");
            if (lState_38484 == null) { lState_38484 = lSSM_37934.AddState("2H_Cast_05", new Vector3(648, 948, 0)); }
            lState_38484.speed = 1f;
            lState_38484.mirror = false;
            lState_38484.tag = "";
            lState_38484.motion = MotionControllerMotion.EditorFindAnimationClip("Assets/ootii/MotionControllerPacks/SpellCasting/Content/Animations/Mixamo/Standing 2H Magic Attack 02.fbx", "2H_Cast_05");

            UnityEditor.Animations.AnimatorState lState_38486 = MotionControllerMotion.EditorFindState(lSSM_37934, "2H_Cast_05_a");
            if (lState_38486 == null) { lState_38486 = lSSM_37934.AddState("2H_Cast_05_a", new Vector3(648, 996, 0)); }
            lState_38486.speed = 1f;
            lState_38486.mirror = false;
            lState_38486.tag = "";
            lState_38486.motion = MotionControllerMotion.EditorFindAnimationClip("Assets/ootii/MotionControllerPacks/SpellCasting/Content/Animations/Mixamo/Standing 2H Magic Attack 02.fbx", "2H_Cast_05_a");

            UnityEditor.Animations.AnimatorState lState_39642 = MotionControllerMotion.EditorFindState(lSSM_37934, "2H_Cast_05_b");
            if (lState_39642 == null) { lState_39642 = lSSM_37934.AddState("2H_Cast_05_b", new Vector3(876, 996, 0)); }
            lState_39642.speed = 1f;
            lState_39642.mirror = false;
            lState_39642.tag = "";
            lState_39642.motion = MotionControllerMotion.EditorFindAnimationClip("Assets/ootii/MotionControllerPacks/SpellCasting/Content/Animations/Mixamo/Standing 2H Magic Attack 02.fbx", "2H_Cast_05_b");

            UnityEditor.Animations.AnimatorState lState_39644 = MotionControllerMotion.EditorFindState(lSSM_37934, "2H_Cast_05_c");
            if (lState_39644 == null) { lState_39644 = lSSM_37934.AddState("2H_Cast_05_c", new Vector3(1104, 996, 0)); }
            lState_39644.speed = 1f;
            lState_39644.mirror = false;
            lState_39644.tag = "";
            lState_39644.motion = MotionControllerMotion.EditorFindAnimationClip("Assets/ootii/MotionControllerPacks/SpellCasting/Content/Animations/Mixamo/Standing 2H Magic Attack 02.fbx", "2H_Cast_05_c");

            UnityEditor.Animations.AnimatorState lState_38488 = MotionControllerMotion.EditorFindState(lSSM_37934, "2H_Cast_06");
            if (lState_38488 == null) { lState_38488 = lSSM_37934.AddState("2H_Cast_06", new Vector3(648, 1092, 0)); }
            lState_38488.speed = 1f;
            lState_38488.mirror = false;
            lState_38488.tag = "";
            lState_38488.motion = MotionControllerMotion.EditorFindAnimationClip("Assets/ootii/MotionControllerPacks/SpellCasting/Content/Animations/Mixamo/Standing 2H Magic Attack 03.fbx", "2H_Cast_06");

            UnityEditor.Animations.AnimatorState lState_38490 = MotionControllerMotion.EditorFindState(lSSM_37934, "2H_Cast_06_a");
            if (lState_38490 == null) { lState_38490 = lSSM_37934.AddState("2H_Cast_06_a", new Vector3(648, 1140, 0)); }
            lState_38490.speed = 1f;
            lState_38490.mirror = false;
            lState_38490.tag = "";
            lState_38490.motion = MotionControllerMotion.EditorFindAnimationClip("Assets/ootii/MotionControllerPacks/SpellCasting/Content/Animations/Mixamo/Standing 2H Magic Attack 03.fbx", "2H_Cast_06_a");

            UnityEditor.Animations.AnimatorState lState_39646 = MotionControllerMotion.EditorFindState(lSSM_37934, "2H_Cast_06_b");
            if (lState_39646 == null) { lState_39646 = lSSM_37934.AddState("2H_Cast_06_b", new Vector3(876, 1140, 0)); }
            lState_39646.speed = 1f;
            lState_39646.mirror = false;
            lState_39646.tag = "";
            lState_39646.motion = MotionControllerMotion.EditorFindAnimationClip("Assets/ootii/MotionControllerPacks/SpellCasting/Content/Animations/Mixamo/Standing 2H Magic Attack 03.fbx", "2H_Cast_06_b");

            UnityEditor.Animations.AnimatorState lState_39648 = MotionControllerMotion.EditorFindState(lSSM_37934, "2H_Cast_06_c");
            if (lState_39648 == null) { lState_39648 = lSSM_37934.AddState("2H_Cast_06_c", new Vector3(1104, 1140, 0)); }
            lState_39648.speed = 1f;
            lState_39648.mirror = false;
            lState_39648.tag = "";
            lState_39648.motion = MotionControllerMotion.EditorFindAnimationClip("Assets/ootii/MotionControllerPacks/SpellCasting/Content/Animations/Mixamo/Standing 2H Magic Attack 03.fbx", "2H_Cast_06_c");

            UnityEditor.Animations.AnimatorState lState_38492 = MotionControllerMotion.EditorFindState(lSSM_37934, "2H_Cast_07");
            if (lState_38492 == null) { lState_38492 = lSSM_37934.AddState("2H_Cast_07", new Vector3(648, 1236, 0)); }
            lState_38492.speed = 1f;
            lState_38492.mirror = false;
            lState_38492.tag = "";
            lState_38492.motion = MotionControllerMotion.EditorFindAnimationClip("Assets/ootii/MotionControllerPacks/SpellCasting/Content/Animations/Mixamo/Standing 2H Magic Attack 04.fbx", "2H_Cast_07");

            UnityEditor.Animations.AnimatorState lState_38494 = MotionControllerMotion.EditorFindState(lSSM_37934, "2H_Cast_07_a");
            if (lState_38494 == null) { lState_38494 = lSSM_37934.AddState("2H_Cast_07_a", new Vector3(648, 1284, 0)); }
            lState_38494.speed = 1f;
            lState_38494.mirror = false;
            lState_38494.tag = "";
            lState_38494.motion = MotionControllerMotion.EditorFindAnimationClip("Assets/ootii/MotionControllerPacks/SpellCasting/Content/Animations/Mixamo/Standing 2H Magic Attack 04.fbx", "2H_Cast_07_a");

            UnityEditor.Animations.AnimatorState lState_39650 = MotionControllerMotion.EditorFindState(lSSM_37934, "2H_Cast_07_b");
            if (lState_39650 == null) { lState_39650 = lSSM_37934.AddState("2H_Cast_07_b", new Vector3(876, 1284, 0)); }
            lState_39650.speed = 1f;
            lState_39650.mirror = false;
            lState_39650.tag = "";
            lState_39650.motion = MotionControllerMotion.EditorFindAnimationClip("Assets/ootii/MotionControllerPacks/SpellCasting/Content/Animations/Mixamo/Standing 2H Magic Attack 04.fbx", "2H_Cast_07_b");

            UnityEditor.Animations.AnimatorState lState_39652 = MotionControllerMotion.EditorFindState(lSSM_37934, "2H_Cast_07_c");
            if (lState_39652 == null) { lState_39652 = lSSM_37934.AddState("2H_Cast_07_c", new Vector3(1104, 1284, 0)); }
            lState_39652.speed = 1f;
            lState_39652.mirror = false;
            lState_39652.tag = "";
            lState_39652.motion = MotionControllerMotion.EditorFindAnimationClip("Assets/ootii/MotionControllerPacks/SpellCasting/Content/Animations/Mixamo/Standing 2H Magic Attack 04.fbx", "2H_Cast_07_c");

            UnityEditor.Animations.AnimatorState lState_38496 = MotionControllerMotion.EditorFindState(lSSM_37934, "2H_Cast_08");
            if (lState_38496 == null) { lState_38496 = lSSM_37934.AddState("2H_Cast_08", new Vector3(648, 1368, 0)); }
            lState_38496.speed = 1f;
            lState_38496.mirror = false;
            lState_38496.tag = "";
            lState_38496.motion = MotionControllerMotion.EditorFindAnimationClip("Assets/ootii/MotionControllerPacks/SpellCasting/Content/Animations/Mixamo/Standing 2H Magic Attack 05.fbx", "2H_Cast_08");

            UnityEditor.Animations.AnimatorState lState_38498 = MotionControllerMotion.EditorFindState(lSSM_37934, "2H_Cast_08_a");
            if (lState_38498 == null) { lState_38498 = lSSM_37934.AddState("2H_Cast_08_a", new Vector3(648, 1416, 0)); }
            lState_38498.speed = 1f;
            lState_38498.mirror = false;
            lState_38498.tag = "";
            lState_38498.motion = MotionControllerMotion.EditorFindAnimationClip("Assets/ootii/MotionControllerPacks/SpellCasting/Content/Animations/Mixamo/Standing 2H Magic Attack 05.fbx", "2H_Cast_08_a");

            UnityEditor.Animations.AnimatorState lState_39654 = MotionControllerMotion.EditorFindState(lSSM_37934, "2H_Cast_08_b");
            if (lState_39654 == null) { lState_39654 = lSSM_37934.AddState("2H_Cast_08_b", new Vector3(876, 1416, 0)); }
            lState_39654.speed = 1f;
            lState_39654.mirror = false;
            lState_39654.tag = "";
            lState_39654.motion = MotionControllerMotion.EditorFindAnimationClip("Assets/ootii/MotionControllerPacks/SpellCasting/Content/Animations/Mixamo/Standing 2H Magic Attack 05.fbx", "2H_Cast_08_b");

            UnityEditor.Animations.AnimatorState lState_39656 = MotionControllerMotion.EditorFindState(lSSM_37934, "2H_Cast_08_c");
            if (lState_39656 == null) { lState_39656 = lSSM_37934.AddState("2H_Cast_08_c", new Vector3(1104, 1416, 0)); }
            lState_39656.speed = 1f;
            lState_39656.mirror = false;
            lState_39656.tag = "";
            lState_39656.motion = MotionControllerMotion.EditorFindAnimationClip("Assets/ootii/MotionControllerPacks/SpellCasting/Content/Animations/Mixamo/Standing 2H Magic Attack 05.fbx", "2H_Cast_08_c");

            UnityEditor.Animations.AnimatorState lState_38466 = MotionControllerMotion.EditorFindState(lSSM_37934, "1H_Cast_03");
            if (lState_38466 == null) { lState_38466 = lSSM_37934.AddState("1H_Cast_03", new Vector3(648, 48, 0)); }
            lState_38466.speed = 1f;
            lState_38466.mirror = false;
            lState_38466.tag = "";
            lState_38466.motion = MotionControllerMotion.EditorFindAnimationClip("Assets/ootii/MotionControllerPacks/SpellCasting/Content/Animations/Mixamo/Standing 1H Magic Attack 02.fbx", "1H_Cast_03");

            UnityEditor.Animations.AnimatorState lState_38468 = MotionControllerMotion.EditorFindState(lSSM_37934, "1H_Cast_03_a");
            if (lState_38468 == null) { lState_38468 = lSSM_37934.AddState("1H_Cast_03_a", new Vector3(648, 96, 0)); }
            lState_38468.speed = 1f;
            lState_38468.mirror = false;
            lState_38468.tag = "";
            lState_38468.motion = MotionControllerMotion.EditorFindAnimationClip("Assets/ootii/MotionControllerPacks/SpellCasting/Content/Animations/Mixamo/Standing 1H Magic Attack 02.fbx", "1H_Cast_03_a");

            UnityEditor.Animations.AnimatorState lState_39658 = MotionControllerMotion.EditorFindState(lSSM_37934, "1H_Cast_03_b");
            if (lState_39658 == null) { lState_39658 = lSSM_37934.AddState("1H_Cast_03_b", new Vector3(876, 96, 0)); }
            lState_39658.speed = 1f;
            lState_39658.mirror = false;
            lState_39658.tag = "";
            lState_39658.motion = MotionControllerMotion.EditorFindAnimationClip("Assets/ootii/MotionControllerPacks/SpellCasting/Content/Animations/Mixamo/Standing 1H Magic Attack 02.fbx", "1H_Cast_03_b");

            UnityEditor.Animations.AnimatorState lState_39660 = MotionControllerMotion.EditorFindState(lSSM_37934, "1H_Cast_03_c");
            if (lState_39660 == null) { lState_39660 = lSSM_37934.AddState("1H_Cast_03_c", new Vector3(1104, 96, 0)); }
            lState_39660.speed = 1f;
            lState_39660.mirror = false;
            lState_39660.tag = "";
            lState_39660.motion = MotionControllerMotion.EditorFindAnimationClip("Assets/ootii/MotionControllerPacks/SpellCasting/Content/Animations/Mixamo/Standing 1H Magic Attack 02.fbx", "1H_Cast_03_c");

            UnityEditor.Animations.AnimatorState lState_38470 = MotionControllerMotion.EditorFindState(lSSM_37934, "1H_Cast_04");
            if (lState_38470 == null) { lState_38470 = lSSM_37934.AddState("1H_Cast_04", new Vector3(648, 180, 0)); }
            lState_38470.speed = 1f;
            lState_38470.mirror = false;
            lState_38470.tag = "";
            lState_38470.motion = MotionControllerMotion.EditorFindAnimationClip("Assets/ootii/MotionControllerPacks/SpellCasting/Content/Animations/Mixamo/Standing 1H Magic Attack 03.fbx", "1H_Cast_04");

            UnityEditor.Animations.AnimatorState lState_38472 = MotionControllerMotion.EditorFindState(lSSM_37934, "1H_Cast_04_a");
            if (lState_38472 == null) { lState_38472 = lSSM_37934.AddState("1H_Cast_04_a", new Vector3(648, 228, 0)); }
            lState_38472.speed = 1f;
            lState_38472.mirror = false;
            lState_38472.tag = "";
            lState_38472.motion = MotionControllerMotion.EditorFindAnimationClip("Assets/ootii/MotionControllerPacks/SpellCasting/Content/Animations/Mixamo/Standing 1H Magic Attack 03.fbx", "1H_Cast_04_a");

            UnityEditor.Animations.AnimatorState lState_39662 = MotionControllerMotion.EditorFindState(lSSM_37934, "1H_Cast_04_b");
            if (lState_39662 == null) { lState_39662 = lSSM_37934.AddState("1H_Cast_04_b", new Vector3(876, 228, 0)); }
            lState_39662.speed = 1f;
            lState_39662.mirror = false;
            lState_39662.tag = "";
            lState_39662.motion = MotionControllerMotion.EditorFindAnimationClip("Assets/ootii/MotionControllerPacks/SpellCasting/Content/Animations/Mixamo/Standing 1H Magic Attack 03.fbx", "1H_Cast_04_b");

            UnityEditor.Animations.AnimatorState lState_39664 = MotionControllerMotion.EditorFindState(lSSM_37934, "1H_Cast_04_c");
            if (lState_39664 == null) { lState_39664 = lSSM_37934.AddState("1H_Cast_04_c", new Vector3(1104, 228, 0)); }
            lState_39664.speed = 1f;
            lState_39664.mirror = false;
            lState_39664.tag = "";
            lState_39664.motion = MotionControllerMotion.EditorFindAnimationClip("Assets/ootii/MotionControllerPacks/SpellCasting/Content/Animations/Mixamo/Standing 1H Magic Attack 03.fbx", "1H_Cast_04_c");

            UnityEditor.Animations.AnimatorStateTransition lAnyTransition_38164 = MotionControllerMotion.EditorFindAnyStateTransition(lLayerStateMachine, lState_38448, 0);
            if (lAnyTransition_38164 == null) { lAnyTransition_38164 = lLayerStateMachine.AddAnyStateTransition(lState_38448); }
            lAnyTransition_38164.isExit = false;
            lAnyTransition_38164.hasExitTime = false;
            lAnyTransition_38164.hasFixedDuration = true;
            lAnyTransition_38164.exitTime = 0.9f;
            lAnyTransition_38164.duration = 0.1f;
            lAnyTransition_38164.offset = 0f;
            lAnyTransition_38164.mute = false;
            lAnyTransition_38164.solo = false;
            lAnyTransition_38164.canTransitionToSelf = true;
            lAnyTransition_38164.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            for (int i = lAnyTransition_38164.conditions.Length - 1; i >= 0; i--) { lAnyTransition_38164.RemoveCondition(lAnyTransition_38164.conditions[i]); }
            lAnyTransition_38164.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 32141f, "L" + rLayerIndex + "MotionPhase");

            UnityEditor.Animations.AnimatorStateTransition lAnyTransition_38166 = MotionControllerMotion.EditorFindAnyStateTransition(lLayerStateMachine, lState_38450, 0);
            if (lAnyTransition_38166 == null) { lAnyTransition_38166 = lLayerStateMachine.AddAnyStateTransition(lState_38450); }
            lAnyTransition_38166.isExit = false;
            lAnyTransition_38166.hasExitTime = false;
            lAnyTransition_38166.hasFixedDuration = true;
            lAnyTransition_38166.exitTime = 0.9f;
            lAnyTransition_38166.duration = 0.2f;
            lAnyTransition_38166.offset = 0f;
            lAnyTransition_38166.mute = false;
            lAnyTransition_38166.solo = false;
            lAnyTransition_38166.canTransitionToSelf = true;
            lAnyTransition_38166.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            for (int i = lAnyTransition_38166.conditions.Length - 1; i >= 0; i--) { lAnyTransition_38166.RemoveCondition(lAnyTransition_38166.conditions[i]); }
            lAnyTransition_38166.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 32140f, "L" + rLayerIndex + "MotionPhase");
            lAnyTransition_38166.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 0f, "L" + rLayerIndex + "MotionForm");

            UnityEditor.Animations.AnimatorStateTransition lAnyTransition_38168 = MotionControllerMotion.EditorFindAnyStateTransition(lLayerStateMachine, lState_38452, 0);
            if (lAnyTransition_38168 == null) { lAnyTransition_38168 = lLayerStateMachine.AddAnyStateTransition(lState_38452); }
            lAnyTransition_38168.isExit = false;
            lAnyTransition_38168.hasExitTime = false;
            lAnyTransition_38168.hasFixedDuration = true;
            lAnyTransition_38168.exitTime = 0.9f;
            lAnyTransition_38168.duration = 0.1f;
            lAnyTransition_38168.offset = 0f;
            lAnyTransition_38168.mute = false;
            lAnyTransition_38168.solo = false;
            lAnyTransition_38168.canTransitionToSelf = true;
            lAnyTransition_38168.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            for (int i = lAnyTransition_38168.conditions.Length - 1; i >= 0; i--) { lAnyTransition_38168.RemoveCondition(lAnyTransition_38168.conditions[i]); }
            lAnyTransition_38168.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 32140f, "L" + rLayerIndex + "MotionPhase");
            lAnyTransition_38168.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 3f, "L" + rLayerIndex + "MotionForm");

            UnityEditor.Animations.AnimatorStateTransition lAnyTransition_38170 = MotionControllerMotion.EditorFindAnyStateTransition(lLayerStateMachine, lState_38454, 0);
            if (lAnyTransition_38170 == null) { lAnyTransition_38170 = lLayerStateMachine.AddAnyStateTransition(lState_38454); }
            lAnyTransition_38170.isExit = false;
            lAnyTransition_38170.hasExitTime = false;
            lAnyTransition_38170.hasFixedDuration = true;
            lAnyTransition_38170.exitTime = 0.9f;
            lAnyTransition_38170.duration = 0.2f;
            lAnyTransition_38170.offset = 0f;
            lAnyTransition_38170.mute = false;
            lAnyTransition_38170.solo = false;
            lAnyTransition_38170.canTransitionToSelf = true;
            lAnyTransition_38170.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            for (int i = lAnyTransition_38170.conditions.Length - 1; i >= 0; i--) { lAnyTransition_38170.RemoveCondition(lAnyTransition_38170.conditions[i]); }
            lAnyTransition_38170.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 32140f, "L" + rLayerIndex + "MotionPhase");
            lAnyTransition_38170.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 2f, "L" + rLayerIndex + "MotionForm");

            UnityEditor.Animations.AnimatorStateTransition lAnyTransition_38172 = MotionControllerMotion.EditorFindAnyStateTransition(lLayerStateMachine, lState_38456, 0);
            if (lAnyTransition_38172 == null) { lAnyTransition_38172 = lLayerStateMachine.AddAnyStateTransition(lState_38456); }
            lAnyTransition_38172.isExit = false;
            lAnyTransition_38172.hasExitTime = false;
            lAnyTransition_38172.hasFixedDuration = true;
            lAnyTransition_38172.exitTime = 0.9f;
            lAnyTransition_38172.duration = 0.1f;
            lAnyTransition_38172.offset = 0f;
            lAnyTransition_38172.mute = false;
            lAnyTransition_38172.solo = false;
            lAnyTransition_38172.canTransitionToSelf = true;
            lAnyTransition_38172.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            for (int i = lAnyTransition_38172.conditions.Length - 1; i >= 0; i--) { lAnyTransition_38172.RemoveCondition(lAnyTransition_38172.conditions[i]); }
            lAnyTransition_38172.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 32140f, "L" + rLayerIndex + "MotionPhase");
            lAnyTransition_38172.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 1f, "L" + rLayerIndex + "MotionForm");

            UnityEditor.Animations.AnimatorStateTransition lAnyTransition_38174 = MotionControllerMotion.EditorFindAnyStateTransition(lLayerStateMachine, lState_38458, 0);
            if (lAnyTransition_38174 == null) { lAnyTransition_38174 = lLayerStateMachine.AddAnyStateTransition(lState_38458); }
            lAnyTransition_38174.isExit = false;
            lAnyTransition_38174.hasExitTime = false;
            lAnyTransition_38174.hasFixedDuration = true;
            lAnyTransition_38174.exitTime = 0.75f;
            lAnyTransition_38174.duration = 0.25f;
            lAnyTransition_38174.offset = 0f;
            lAnyTransition_38174.mute = false;
            lAnyTransition_38174.solo = false;
            lAnyTransition_38174.canTransitionToSelf = true;
            lAnyTransition_38174.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            for (int i = lAnyTransition_38174.conditions.Length - 1; i >= 0; i--) { lAnyTransition_38174.RemoveCondition(lAnyTransition_38174.conditions[i]); }
            lAnyTransition_38174.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 32145f, "L" + rLayerIndex + "MotionPhase");

            UnityEditor.Animations.AnimatorStateTransition lAnyTransition_38176 = MotionControllerMotion.EditorFindAnyStateTransition(lLayerStateMachine, lState_38460, 0);
            if (lAnyTransition_38176 == null) { lAnyTransition_38176 = lLayerStateMachine.AddAnyStateTransition(lState_38460); }
            lAnyTransition_38176.isExit = false;
            lAnyTransition_38176.hasExitTime = false;
            lAnyTransition_38176.hasFixedDuration = true;
            lAnyTransition_38176.exitTime = 0.75f;
            lAnyTransition_38176.duration = 0.25f;
            lAnyTransition_38176.offset = 0f;
            lAnyTransition_38176.mute = false;
            lAnyTransition_38176.solo = false;
            lAnyTransition_38176.canTransitionToSelf = true;
            lAnyTransition_38176.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            for (int i = lAnyTransition_38176.conditions.Length - 1; i >= 0; i--) { lAnyTransition_38176.RemoveCondition(lAnyTransition_38176.conditions[i]); }
            lAnyTransition_38176.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 32140f, "L" + rLayerIndex + "MotionPhase");
            lAnyTransition_38176.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 4f, "L" + rLayerIndex + "MotionForm");

            UnityEditor.Animations.AnimatorStateTransition lAnyTransition_38178 = MotionControllerMotion.EditorFindAnyStateTransition(lLayerStateMachine, lState_38462, 0);
            if (lAnyTransition_38178 == null) { lAnyTransition_38178 = lLayerStateMachine.AddAnyStateTransition(lState_38462); }
            lAnyTransition_38178.isExit = false;
            lAnyTransition_38178.hasExitTime = false;
            lAnyTransition_38178.hasFixedDuration = true;
            lAnyTransition_38178.exitTime = 0.75f;
            lAnyTransition_38178.duration = 0.25f;
            lAnyTransition_38178.offset = 0f;
            lAnyTransition_38178.mute = false;
            lAnyTransition_38178.solo = false;
            lAnyTransition_38178.canTransitionToSelf = true;
            lAnyTransition_38178.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            for (int i = lAnyTransition_38178.conditions.Length - 1; i >= 0; i--) { lAnyTransition_38178.RemoveCondition(lAnyTransition_38178.conditions[i]); }
            lAnyTransition_38178.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 32140f, "L" + rLayerIndex + "MotionPhase");
            lAnyTransition_38178.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 5f, "L" + rLayerIndex + "MotionForm");

            UnityEditor.Animations.AnimatorStateTransition lAnyTransition_38180 = MotionControllerMotion.EditorFindAnyStateTransition(lLayerStateMachine, lState_38464, 0);
            if (lAnyTransition_38180 == null) { lAnyTransition_38180 = lLayerStateMachine.AddAnyStateTransition(lState_38464); }
            lAnyTransition_38180.isExit = false;
            lAnyTransition_38180.hasExitTime = false;
            lAnyTransition_38180.hasFixedDuration = true;
            lAnyTransition_38180.exitTime = 0.75f;
            lAnyTransition_38180.duration = 0.25f;
            lAnyTransition_38180.offset = 0f;
            lAnyTransition_38180.mute = false;
            lAnyTransition_38180.solo = false;
            lAnyTransition_38180.canTransitionToSelf = true;
            lAnyTransition_38180.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            for (int i = lAnyTransition_38180.conditions.Length - 1; i >= 0; i--) { lAnyTransition_38180.RemoveCondition(lAnyTransition_38180.conditions[i]); }
            lAnyTransition_38180.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 32140f, "L" + rLayerIndex + "MotionPhase");
            lAnyTransition_38180.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 10f, "L" + rLayerIndex + "MotionForm");

            UnityEditor.Animations.AnimatorStateTransition lAnyTransition_38182 = MotionControllerMotion.EditorFindAnyStateTransition(lLayerStateMachine, lState_38466, 0);
            if (lAnyTransition_38182 == null) { lAnyTransition_38182 = lLayerStateMachine.AddAnyStateTransition(lState_38466); }
            lAnyTransition_38182.isExit = false;
            lAnyTransition_38182.hasExitTime = false;
            lAnyTransition_38182.hasFixedDuration = true;
            lAnyTransition_38182.exitTime = 0.75f;
            lAnyTransition_38182.duration = 0.25f;
            lAnyTransition_38182.offset = 0f;
            lAnyTransition_38182.mute = false;
            lAnyTransition_38182.solo = false;
            lAnyTransition_38182.canTransitionToSelf = true;
            lAnyTransition_38182.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            for (int i = lAnyTransition_38182.conditions.Length - 1; i >= 0; i--) { lAnyTransition_38182.RemoveCondition(lAnyTransition_38182.conditions[i]); }
            lAnyTransition_38182.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 32140f, "L" + rLayerIndex + "MotionPhase");
            lAnyTransition_38182.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 6f, "L" + rLayerIndex + "MotionForm");

            UnityEditor.Animations.AnimatorStateTransition lAnyTransition_38184 = MotionControllerMotion.EditorFindAnyStateTransition(lLayerStateMachine, lState_38468, 0);
            if (lAnyTransition_38184 == null) { lAnyTransition_38184 = lLayerStateMachine.AddAnyStateTransition(lState_38468); }
            lAnyTransition_38184.isExit = false;
            lAnyTransition_38184.hasExitTime = false;
            lAnyTransition_38184.hasFixedDuration = true;
            lAnyTransition_38184.exitTime = 0.75f;
            lAnyTransition_38184.duration = 0.25f;
            lAnyTransition_38184.offset = 0f;
            lAnyTransition_38184.mute = false;
            lAnyTransition_38184.solo = false;
            lAnyTransition_38184.canTransitionToSelf = true;
            lAnyTransition_38184.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            for (int i = lAnyTransition_38184.conditions.Length - 1; i >= 0; i--) { lAnyTransition_38184.RemoveCondition(lAnyTransition_38184.conditions[i]); }
            lAnyTransition_38184.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 32140f, "L" + rLayerIndex + "MotionPhase");
            lAnyTransition_38184.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 7f, "L" + rLayerIndex + "MotionForm");

            UnityEditor.Animations.AnimatorStateTransition lAnyTransition_38186 = MotionControllerMotion.EditorFindAnyStateTransition(lLayerStateMachine, lState_38470, 0);
            if (lAnyTransition_38186 == null) { lAnyTransition_38186 = lLayerStateMachine.AddAnyStateTransition(lState_38470); }
            lAnyTransition_38186.isExit = false;
            lAnyTransition_38186.hasExitTime = false;
            lAnyTransition_38186.hasFixedDuration = true;
            lAnyTransition_38186.exitTime = 0.75f;
            lAnyTransition_38186.duration = 0.25f;
            lAnyTransition_38186.offset = 0f;
            lAnyTransition_38186.mute = false;
            lAnyTransition_38186.solo = false;
            lAnyTransition_38186.canTransitionToSelf = true;
            lAnyTransition_38186.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            for (int i = lAnyTransition_38186.conditions.Length - 1; i >= 0; i--) { lAnyTransition_38186.RemoveCondition(lAnyTransition_38186.conditions[i]); }
            lAnyTransition_38186.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 32140f, "L" + rLayerIndex + "MotionPhase");
            lAnyTransition_38186.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 8f, "L" + rLayerIndex + "MotionForm");

            UnityEditor.Animations.AnimatorStateTransition lAnyTransition_38188 = MotionControllerMotion.EditorFindAnyStateTransition(lLayerStateMachine, lState_38472, 0);
            if (lAnyTransition_38188 == null) { lAnyTransition_38188 = lLayerStateMachine.AddAnyStateTransition(lState_38472); }
            lAnyTransition_38188.isExit = false;
            lAnyTransition_38188.hasExitTime = false;
            lAnyTransition_38188.hasFixedDuration = true;
            lAnyTransition_38188.exitTime = 0.75f;
            lAnyTransition_38188.duration = 0.25f;
            lAnyTransition_38188.offset = 0f;
            lAnyTransition_38188.mute = false;
            lAnyTransition_38188.solo = false;
            lAnyTransition_38188.canTransitionToSelf = true;
            lAnyTransition_38188.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            for (int i = lAnyTransition_38188.conditions.Length - 1; i >= 0; i--) { lAnyTransition_38188.RemoveCondition(lAnyTransition_38188.conditions[i]); }
            lAnyTransition_38188.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 32140f, "L" + rLayerIndex + "MotionPhase");
            lAnyTransition_38188.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 9f, "L" + rLayerIndex + "MotionForm");

            UnityEditor.Animations.AnimatorStateTransition lAnyTransition_38190 = MotionControllerMotion.EditorFindAnyStateTransition(lLayerStateMachine, lState_38474, 0);
            if (lAnyTransition_38190 == null) { lAnyTransition_38190 = lLayerStateMachine.AddAnyStateTransition(lState_38474); }
            lAnyTransition_38190.isExit = false;
            lAnyTransition_38190.hasExitTime = false;
            lAnyTransition_38190.hasFixedDuration = true;
            lAnyTransition_38190.exitTime = 0.75f;
            lAnyTransition_38190.duration = 0.25f;
            lAnyTransition_38190.offset = 0f;
            lAnyTransition_38190.mute = false;
            lAnyTransition_38190.solo = false;
            lAnyTransition_38190.canTransitionToSelf = true;
            lAnyTransition_38190.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            for (int i = lAnyTransition_38190.conditions.Length - 1; i >= 0; i--) { lAnyTransition_38190.RemoveCondition(lAnyTransition_38190.conditions[i]); }
            lAnyTransition_38190.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 32140f, "L" + rLayerIndex + "MotionPhase");
            lAnyTransition_38190.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 11f, "L" + rLayerIndex + "MotionForm");

            UnityEditor.Animations.AnimatorStateTransition lAnyTransition_38192 = MotionControllerMotion.EditorFindAnyStateTransition(lLayerStateMachine, lState_38476, 0);
            if (lAnyTransition_38192 == null) { lAnyTransition_38192 = lLayerStateMachine.AddAnyStateTransition(lState_38476); }
            lAnyTransition_38192.isExit = false;
            lAnyTransition_38192.hasExitTime = false;
            lAnyTransition_38192.hasFixedDuration = true;
            lAnyTransition_38192.exitTime = 0.75f;
            lAnyTransition_38192.duration = 0.25f;
            lAnyTransition_38192.offset = 0f;
            lAnyTransition_38192.mute = false;
            lAnyTransition_38192.solo = false;
            lAnyTransition_38192.canTransitionToSelf = true;
            lAnyTransition_38192.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            for (int i = lAnyTransition_38192.conditions.Length - 1; i >= 0; i--) { lAnyTransition_38192.RemoveCondition(lAnyTransition_38192.conditions[i]); }
            lAnyTransition_38192.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 32140f, "L" + rLayerIndex + "MotionPhase");
            lAnyTransition_38192.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 12f, "L" + rLayerIndex + "MotionForm");

            UnityEditor.Animations.AnimatorStateTransition lAnyTransition_38194 = MotionControllerMotion.EditorFindAnyStateTransition(lLayerStateMachine, lState_38478, 0);
            if (lAnyTransition_38194 == null) { lAnyTransition_38194 = lLayerStateMachine.AddAnyStateTransition(lState_38478); }
            lAnyTransition_38194.isExit = false;
            lAnyTransition_38194.hasExitTime = false;
            lAnyTransition_38194.hasFixedDuration = true;
            lAnyTransition_38194.exitTime = 0.75f;
            lAnyTransition_38194.duration = 0.25f;
            lAnyTransition_38194.offset = 0f;
            lAnyTransition_38194.mute = false;
            lAnyTransition_38194.solo = false;
            lAnyTransition_38194.canTransitionToSelf = true;
            lAnyTransition_38194.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            for (int i = lAnyTransition_38194.conditions.Length - 1; i >= 0; i--) { lAnyTransition_38194.RemoveCondition(lAnyTransition_38194.conditions[i]); }
            lAnyTransition_38194.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 32140f, "L" + rLayerIndex + "MotionPhase");
            lAnyTransition_38194.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 13f, "L" + rLayerIndex + "MotionForm");

            UnityEditor.Animations.AnimatorStateTransition lAnyTransition_38196 = MotionControllerMotion.EditorFindAnyStateTransition(lLayerStateMachine, lState_38480, 0);
            if (lAnyTransition_38196 == null) { lAnyTransition_38196 = lLayerStateMachine.AddAnyStateTransition(lState_38480); }
            lAnyTransition_38196.isExit = false;
            lAnyTransition_38196.hasExitTime = false;
            lAnyTransition_38196.hasFixedDuration = true;
            lAnyTransition_38196.exitTime = 0.75f;
            lAnyTransition_38196.duration = 0.25f;
            lAnyTransition_38196.offset = 0f;
            lAnyTransition_38196.mute = false;
            lAnyTransition_38196.solo = false;
            lAnyTransition_38196.canTransitionToSelf = true;
            lAnyTransition_38196.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            for (int i = lAnyTransition_38196.conditions.Length - 1; i >= 0; i--) { lAnyTransition_38196.RemoveCondition(lAnyTransition_38196.conditions[i]); }
            lAnyTransition_38196.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 32140f, "L" + rLayerIndex + "MotionPhase");
            lAnyTransition_38196.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 14f, "L" + rLayerIndex + "MotionForm");

            UnityEditor.Animations.AnimatorStateTransition lAnyTransition_38198 = MotionControllerMotion.EditorFindAnyStateTransition(lLayerStateMachine, lState_38482, 0);
            if (lAnyTransition_38198 == null) { lAnyTransition_38198 = lLayerStateMachine.AddAnyStateTransition(lState_38482); }
            lAnyTransition_38198.isExit = false;
            lAnyTransition_38198.hasExitTime = false;
            lAnyTransition_38198.hasFixedDuration = true;
            lAnyTransition_38198.exitTime = 0.75f;
            lAnyTransition_38198.duration = 0.25f;
            lAnyTransition_38198.offset = 0f;
            lAnyTransition_38198.mute = false;
            lAnyTransition_38198.solo = false;
            lAnyTransition_38198.canTransitionToSelf = true;
            lAnyTransition_38198.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            for (int i = lAnyTransition_38198.conditions.Length - 1; i >= 0; i--) { lAnyTransition_38198.RemoveCondition(lAnyTransition_38198.conditions[i]); }
            lAnyTransition_38198.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 32140f, "L" + rLayerIndex + "MotionPhase");
            lAnyTransition_38198.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 15f, "L" + rLayerIndex + "MotionForm");

            UnityEditor.Animations.AnimatorStateTransition lAnyTransition_38200 = MotionControllerMotion.EditorFindAnyStateTransition(lLayerStateMachine, lState_38484, 0);
            if (lAnyTransition_38200 == null) { lAnyTransition_38200 = lLayerStateMachine.AddAnyStateTransition(lState_38484); }
            lAnyTransition_38200.isExit = false;
            lAnyTransition_38200.hasExitTime = false;
            lAnyTransition_38200.hasFixedDuration = true;
            lAnyTransition_38200.exitTime = 0.75f;
            lAnyTransition_38200.duration = 0.25f;
            lAnyTransition_38200.offset = 0f;
            lAnyTransition_38200.mute = false;
            lAnyTransition_38200.solo = false;
            lAnyTransition_38200.canTransitionToSelf = true;
            lAnyTransition_38200.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            for (int i = lAnyTransition_38200.conditions.Length - 1; i >= 0; i--) { lAnyTransition_38200.RemoveCondition(lAnyTransition_38200.conditions[i]); }
            lAnyTransition_38200.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 32140f, "L" + rLayerIndex + "MotionPhase");
            lAnyTransition_38200.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 16f, "L" + rLayerIndex + "MotionForm");

            UnityEditor.Animations.AnimatorStateTransition lAnyTransition_38202 = MotionControllerMotion.EditorFindAnyStateTransition(lLayerStateMachine, lState_38486, 0);
            if (lAnyTransition_38202 == null) { lAnyTransition_38202 = lLayerStateMachine.AddAnyStateTransition(lState_38486); }
            lAnyTransition_38202.isExit = false;
            lAnyTransition_38202.hasExitTime = false;
            lAnyTransition_38202.hasFixedDuration = true;
            lAnyTransition_38202.exitTime = 0.75f;
            lAnyTransition_38202.duration = 0.25f;
            lAnyTransition_38202.offset = 0f;
            lAnyTransition_38202.mute = false;
            lAnyTransition_38202.solo = false;
            lAnyTransition_38202.canTransitionToSelf = true;
            lAnyTransition_38202.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            for (int i = lAnyTransition_38202.conditions.Length - 1; i >= 0; i--) { lAnyTransition_38202.RemoveCondition(lAnyTransition_38202.conditions[i]); }
            lAnyTransition_38202.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 32140f, "L" + rLayerIndex + "MotionPhase");
            lAnyTransition_38202.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 17f, "L" + rLayerIndex + "MotionForm");

            UnityEditor.Animations.AnimatorStateTransition lAnyTransition_38204 = MotionControllerMotion.EditorFindAnyStateTransition(lLayerStateMachine, lState_38488, 0);
            if (lAnyTransition_38204 == null) { lAnyTransition_38204 = lLayerStateMachine.AddAnyStateTransition(lState_38488); }
            lAnyTransition_38204.isExit = false;
            lAnyTransition_38204.hasExitTime = false;
            lAnyTransition_38204.hasFixedDuration = true;
            lAnyTransition_38204.exitTime = 0.75f;
            lAnyTransition_38204.duration = 0.25f;
            lAnyTransition_38204.offset = 0f;
            lAnyTransition_38204.mute = false;
            lAnyTransition_38204.solo = false;
            lAnyTransition_38204.canTransitionToSelf = true;
            lAnyTransition_38204.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            for (int i = lAnyTransition_38204.conditions.Length - 1; i >= 0; i--) { lAnyTransition_38204.RemoveCondition(lAnyTransition_38204.conditions[i]); }
            lAnyTransition_38204.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 32140f, "L" + rLayerIndex + "MotionPhase");
            lAnyTransition_38204.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 18f, "L" + rLayerIndex + "MotionForm");

            UnityEditor.Animations.AnimatorStateTransition lAnyTransition_38206 = MotionControllerMotion.EditorFindAnyStateTransition(lLayerStateMachine, lState_38490, 0);
            if (lAnyTransition_38206 == null) { lAnyTransition_38206 = lLayerStateMachine.AddAnyStateTransition(lState_38490); }
            lAnyTransition_38206.isExit = false;
            lAnyTransition_38206.hasExitTime = false;
            lAnyTransition_38206.hasFixedDuration = true;
            lAnyTransition_38206.exitTime = 0.75f;
            lAnyTransition_38206.duration = 0.25f;
            lAnyTransition_38206.offset = 0f;
            lAnyTransition_38206.mute = false;
            lAnyTransition_38206.solo = false;
            lAnyTransition_38206.canTransitionToSelf = true;
            lAnyTransition_38206.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            for (int i = lAnyTransition_38206.conditions.Length - 1; i >= 0; i--) { lAnyTransition_38206.RemoveCondition(lAnyTransition_38206.conditions[i]); }
            lAnyTransition_38206.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 32140f, "L" + rLayerIndex + "MotionPhase");
            lAnyTransition_38206.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 19f, "L" + rLayerIndex + "MotionForm");

            UnityEditor.Animations.AnimatorStateTransition lAnyTransition_38208 = MotionControllerMotion.EditorFindAnyStateTransition(lLayerStateMachine, lState_38492, 0);
            if (lAnyTransition_38208 == null) { lAnyTransition_38208 = lLayerStateMachine.AddAnyStateTransition(lState_38492); }
            lAnyTransition_38208.isExit = false;
            lAnyTransition_38208.hasExitTime = false;
            lAnyTransition_38208.hasFixedDuration = true;
            lAnyTransition_38208.exitTime = 0.75f;
            lAnyTransition_38208.duration = 0.25f;
            lAnyTransition_38208.offset = 0f;
            lAnyTransition_38208.mute = false;
            lAnyTransition_38208.solo = false;
            lAnyTransition_38208.canTransitionToSelf = true;
            lAnyTransition_38208.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            for (int i = lAnyTransition_38208.conditions.Length - 1; i >= 0; i--) { lAnyTransition_38208.RemoveCondition(lAnyTransition_38208.conditions[i]); }
            lAnyTransition_38208.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 32140f, "L" + rLayerIndex + "MotionPhase");
            lAnyTransition_38208.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 20f, "L" + rLayerIndex + "MotionForm");

            UnityEditor.Animations.AnimatorStateTransition lAnyTransition_38210 = MotionControllerMotion.EditorFindAnyStateTransition(lLayerStateMachine, lState_38494, 0);
            if (lAnyTransition_38210 == null) { lAnyTransition_38210 = lLayerStateMachine.AddAnyStateTransition(lState_38494); }
            lAnyTransition_38210.isExit = false;
            lAnyTransition_38210.hasExitTime = false;
            lAnyTransition_38210.hasFixedDuration = true;
            lAnyTransition_38210.exitTime = 0.75f;
            lAnyTransition_38210.duration = 0.25f;
            lAnyTransition_38210.offset = 0f;
            lAnyTransition_38210.mute = false;
            lAnyTransition_38210.solo = false;
            lAnyTransition_38210.canTransitionToSelf = true;
            lAnyTransition_38210.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            for (int i = lAnyTransition_38210.conditions.Length - 1; i >= 0; i--) { lAnyTransition_38210.RemoveCondition(lAnyTransition_38210.conditions[i]); }
            lAnyTransition_38210.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 32140f, "L" + rLayerIndex + "MotionPhase");
            lAnyTransition_38210.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 21f, "L" + rLayerIndex + "MotionForm");

            UnityEditor.Animations.AnimatorStateTransition lAnyTransition_38212 = MotionControllerMotion.EditorFindAnyStateTransition(lLayerStateMachine, lState_38496, 0);
            if (lAnyTransition_38212 == null) { lAnyTransition_38212 = lLayerStateMachine.AddAnyStateTransition(lState_38496); }
            lAnyTransition_38212.isExit = false;
            lAnyTransition_38212.hasExitTime = false;
            lAnyTransition_38212.hasFixedDuration = true;
            lAnyTransition_38212.exitTime = 0.75f;
            lAnyTransition_38212.duration = 0.25f;
            lAnyTransition_38212.offset = 0f;
            lAnyTransition_38212.mute = false;
            lAnyTransition_38212.solo = false;
            lAnyTransition_38212.canTransitionToSelf = true;
            lAnyTransition_38212.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            for (int i = lAnyTransition_38212.conditions.Length - 1; i >= 0; i--) { lAnyTransition_38212.RemoveCondition(lAnyTransition_38212.conditions[i]); }
            lAnyTransition_38212.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 32140f, "L" + rLayerIndex + "MotionPhase");
            lAnyTransition_38212.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 22f, "L" + rLayerIndex + "MotionForm");

            UnityEditor.Animations.AnimatorStateTransition lAnyTransition_38214 = MotionControllerMotion.EditorFindAnyStateTransition(lLayerStateMachine, lState_38498, 0);
            if (lAnyTransition_38214 == null) { lAnyTransition_38214 = lLayerStateMachine.AddAnyStateTransition(lState_38498); }
            lAnyTransition_38214.isExit = false;
            lAnyTransition_38214.hasExitTime = false;
            lAnyTransition_38214.hasFixedDuration = true;
            lAnyTransition_38214.exitTime = 0.75f;
            lAnyTransition_38214.duration = 0.25f;
            lAnyTransition_38214.offset = 0f;
            lAnyTransition_38214.mute = false;
            lAnyTransition_38214.solo = false;
            lAnyTransition_38214.canTransitionToSelf = true;
            lAnyTransition_38214.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            for (int i = lAnyTransition_38214.conditions.Length - 1; i >= 0; i--) { lAnyTransition_38214.RemoveCondition(lAnyTransition_38214.conditions[i]); }
            lAnyTransition_38214.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 32140f, "L" + rLayerIndex + "MotionPhase");
            lAnyTransition_38214.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 23f, "L" + rLayerIndex + "MotionForm");

            UnityEditor.Animations.AnimatorStateTransition lTransition_39666 = MotionControllerMotion.EditorFindTransition(lState_38450, lState_39612, 0);
            if (lTransition_39666 == null) { lTransition_39666 = lState_38450.AddTransition(lState_39612); }
            lTransition_39666.isExit = false;
            lTransition_39666.hasExitTime = true;
            lTransition_39666.hasFixedDuration = true;
            lTransition_39666.exitTime = 0.9f;
            lTransition_39666.duration = 0.15f;
            lTransition_39666.offset = 0f;
            lTransition_39666.mute = false;
            lTransition_39666.solo = false;
            lTransition_39666.canTransitionToSelf = true;
            lTransition_39666.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            for (int i = lTransition_39666.conditions.Length - 1; i >= 0; i--) { lTransition_39666.RemoveCondition(lTransition_39666.conditions[i]); }
            lTransition_39666.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 0f, "L" + rLayerIndex + "MotionParameter");

            UnityEditor.Animations.AnimatorStateTransition lTransition_39668 = MotionControllerMotion.EditorFindTransition(lState_38450, lState_39614, 0);
            if (lTransition_39668 == null) { lTransition_39668 = lState_38450.AddTransition(lState_39614); }
            lTransition_39668.isExit = false;
            lTransition_39668.hasExitTime = true;
            lTransition_39668.hasFixedDuration = true;
            lTransition_39668.exitTime = 0.6975827f;
            lTransition_39668.duration = 0.25f;
            lTransition_39668.offset = 0f;
            lTransition_39668.mute = false;
            lTransition_39668.solo = false;
            lTransition_39668.canTransitionToSelf = true;
            lTransition_39668.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            for (int i = lTransition_39668.conditions.Length - 1; i >= 0; i--) { lTransition_39668.RemoveCondition(lTransition_39668.conditions[i]); }
            lTransition_39668.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 1f, "L" + rLayerIndex + "MotionParameter");

            UnityEditor.Animations.AnimatorStateTransition lTransition_39670 = MotionControllerMotion.EditorFindTransition(lState_38448, lState_38450, 0);
            if (lTransition_39670 == null) { lTransition_39670 = lState_38448.AddTransition(lState_38450); }
            lTransition_39670.isExit = false;
            lTransition_39670.hasExitTime = true;
            lTransition_39670.hasFixedDuration = true;
            lTransition_39670.exitTime = 0.3048472f;
            lTransition_39670.duration = 0.2500001f;
            lTransition_39670.offset = 0f;
            lTransition_39670.mute = false;
            lTransition_39670.solo = false;
            lTransition_39670.canTransitionToSelf = true;
            lTransition_39670.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            for (int i = lTransition_39670.conditions.Length - 1; i >= 0; i--) { lTransition_39670.RemoveCondition(lTransition_39670.conditions[i]); }
            lTransition_39670.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 0f, "L" + rLayerIndex + "MotionForm");

            UnityEditor.Animations.AnimatorStateTransition lTransition_39672 = MotionControllerMotion.EditorFindTransition(lState_38448, lState_38452, 0);
            if (lTransition_39672 == null) { lTransition_39672 = lState_38448.AddTransition(lState_38452); }
            lTransition_39672.isExit = false;
            lTransition_39672.hasExitTime = true;
            lTransition_39672.hasFixedDuration = true;
            lTransition_39672.exitTime = 0.5648073f;
            lTransition_39672.duration = 0.1000001f;
            lTransition_39672.offset = 0f;
            lTransition_39672.mute = false;
            lTransition_39672.solo = false;
            lTransition_39672.canTransitionToSelf = true;
            lTransition_39672.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            for (int i = lTransition_39672.conditions.Length - 1; i >= 0; i--) { lTransition_39672.RemoveCondition(lTransition_39672.conditions[i]); }
            lTransition_39672.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 3f, "L" + rLayerIndex + "MotionForm");

            UnityEditor.Animations.AnimatorStateTransition lTransition_39674 = MotionControllerMotion.EditorFindTransition(lState_38448, lState_38454, 0);
            if (lTransition_39674 == null) { lTransition_39674 = lState_38448.AddTransition(lState_38454); }
            lTransition_39674.isExit = false;
            lTransition_39674.hasExitTime = true;
            lTransition_39674.hasFixedDuration = true;
            lTransition_39674.exitTime = 0.3f;
            lTransition_39674.duration = 0.25f;
            lTransition_39674.offset = 0f;
            lTransition_39674.mute = false;
            lTransition_39674.solo = false;
            lTransition_39674.canTransitionToSelf = true;
            lTransition_39674.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            for (int i = lTransition_39674.conditions.Length - 1; i >= 0; i--) { lTransition_39674.RemoveCondition(lTransition_39674.conditions[i]); }
            lTransition_39674.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 2f, "L" + rLayerIndex + "MotionForm");

            UnityEditor.Animations.AnimatorStateTransition lTransition_39676 = MotionControllerMotion.EditorFindTransition(lState_38448, lState_38456, 0);
            if (lTransition_39676 == null) { lTransition_39676 = lState_38448.AddTransition(lState_38456); }
            lTransition_39676.isExit = false;
            lTransition_39676.hasExitTime = true;
            lTransition_39676.hasFixedDuration = true;
            lTransition_39676.exitTime = 0.3048472f;
            lTransition_39676.duration = 0.1000001f;
            lTransition_39676.offset = 0f;
            lTransition_39676.mute = false;
            lTransition_39676.solo = false;
            lTransition_39676.canTransitionToSelf = true;
            lTransition_39676.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            for (int i = lTransition_39676.conditions.Length - 1; i >= 0; i--) { lTransition_39676.RemoveCondition(lTransition_39676.conditions[i]); }
            lTransition_39676.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 1f, "L" + rLayerIndex + "MotionForm");

            UnityEditor.Animations.AnimatorStateTransition lTransition_39678 = MotionControllerMotion.EditorFindTransition(lState_38448, lState_38460, 0);
            if (lTransition_39678 == null) { lTransition_39678 = lState_38448.AddTransition(lState_38460); }
            lTransition_39678.isExit = false;
            lTransition_39678.hasExitTime = true;
            lTransition_39678.hasFixedDuration = true;
            lTransition_39678.exitTime = 0.7115385f;
            lTransition_39678.duration = 0.25f;
            lTransition_39678.offset = 0f;
            lTransition_39678.mute = false;
            lTransition_39678.solo = false;
            lTransition_39678.canTransitionToSelf = true;
            lTransition_39678.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            for (int i = lTransition_39678.conditions.Length - 1; i >= 0; i--) { lTransition_39678.RemoveCondition(lTransition_39678.conditions[i]); }
            lTransition_39678.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 4f, "L" + rLayerIndex + "MotionForm");

            UnityEditor.Animations.AnimatorStateTransition lTransition_39680 = MotionControllerMotion.EditorFindTransition(lState_38448, lState_38462, 0);
            if (lTransition_39680 == null) { lTransition_39680 = lState_38448.AddTransition(lState_38462); }
            lTransition_39680.isExit = false;
            lTransition_39680.hasExitTime = true;
            lTransition_39680.hasFixedDuration = true;
            lTransition_39680.exitTime = 0.7115385f;
            lTransition_39680.duration = 0.25f;
            lTransition_39680.offset = 0f;
            lTransition_39680.mute = false;
            lTransition_39680.solo = false;
            lTransition_39680.canTransitionToSelf = true;
            lTransition_39680.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            for (int i = lTransition_39680.conditions.Length - 1; i >= 0; i--) { lTransition_39680.RemoveCondition(lTransition_39680.conditions[i]); }
            lTransition_39680.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 5f, "L" + rLayerIndex + "MotionForm");

            UnityEditor.Animations.AnimatorStateTransition lTransition_39682 = MotionControllerMotion.EditorFindTransition(lState_38448, lState_38464, 0);
            if (lTransition_39682 == null) { lTransition_39682 = lState_38448.AddTransition(lState_38464); }
            lTransition_39682.isExit = false;
            lTransition_39682.hasExitTime = true;
            lTransition_39682.hasFixedDuration = true;
            lTransition_39682.exitTime = 0.7115385f;
            lTransition_39682.duration = 0.25f;
            lTransition_39682.offset = 0f;
            lTransition_39682.mute = false;
            lTransition_39682.solo = false;
            lTransition_39682.canTransitionToSelf = true;
            lTransition_39682.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            for (int i = lTransition_39682.conditions.Length - 1; i >= 0; i--) { lTransition_39682.RemoveCondition(lTransition_39682.conditions[i]); }
            lTransition_39682.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 10f, "L" + rLayerIndex + "MotionForm");

            UnityEditor.Animations.AnimatorStateTransition lTransition_39684 = MotionControllerMotion.EditorFindTransition(lState_38448, lState_38466, 0);
            if (lTransition_39684 == null) { lTransition_39684 = lState_38448.AddTransition(lState_38466); }
            lTransition_39684.isExit = false;
            lTransition_39684.hasExitTime = true;
            lTransition_39684.hasFixedDuration = true;
            lTransition_39684.exitTime = 0.7115385f;
            lTransition_39684.duration = 0.25f;
            lTransition_39684.offset = 0f;
            lTransition_39684.mute = false;
            lTransition_39684.solo = false;
            lTransition_39684.canTransitionToSelf = true;
            lTransition_39684.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            for (int i = lTransition_39684.conditions.Length - 1; i >= 0; i--) { lTransition_39684.RemoveCondition(lTransition_39684.conditions[i]); }
            lTransition_39684.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 6f, "L" + rLayerIndex + "MotionForm");

            UnityEditor.Animations.AnimatorStateTransition lTransition_39686 = MotionControllerMotion.EditorFindTransition(lState_38448, lState_38468, 0);
            if (lTransition_39686 == null) { lTransition_39686 = lState_38448.AddTransition(lState_38468); }
            lTransition_39686.isExit = false;
            lTransition_39686.hasExitTime = true;
            lTransition_39686.hasFixedDuration = true;
            lTransition_39686.exitTime = 0.7115385f;
            lTransition_39686.duration = 0.25f;
            lTransition_39686.offset = 0f;
            lTransition_39686.mute = false;
            lTransition_39686.solo = false;
            lTransition_39686.canTransitionToSelf = true;
            lTransition_39686.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            for (int i = lTransition_39686.conditions.Length - 1; i >= 0; i--) { lTransition_39686.RemoveCondition(lTransition_39686.conditions[i]); }
            lTransition_39686.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 7f, "L" + rLayerIndex + "MotionForm");

            UnityEditor.Animations.AnimatorStateTransition lTransition_39688 = MotionControllerMotion.EditorFindTransition(lState_38448, lState_38470, 0);
            if (lTransition_39688 == null) { lTransition_39688 = lState_38448.AddTransition(lState_38470); }
            lTransition_39688.isExit = false;
            lTransition_39688.hasExitTime = true;
            lTransition_39688.hasFixedDuration = true;
            lTransition_39688.exitTime = 0.7115385f;
            lTransition_39688.duration = 0.25f;
            lTransition_39688.offset = 0f;
            lTransition_39688.mute = false;
            lTransition_39688.solo = false;
            lTransition_39688.canTransitionToSelf = true;
            lTransition_39688.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            for (int i = lTransition_39688.conditions.Length - 1; i >= 0; i--) { lTransition_39688.RemoveCondition(lTransition_39688.conditions[i]); }
            lTransition_39688.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 8f, "L" + rLayerIndex + "MotionForm");

            UnityEditor.Animations.AnimatorStateTransition lTransition_39690 = MotionControllerMotion.EditorFindTransition(lState_38448, lState_38472, 0);
            if (lTransition_39690 == null) { lTransition_39690 = lState_38448.AddTransition(lState_38472); }
            lTransition_39690.isExit = false;
            lTransition_39690.hasExitTime = true;
            lTransition_39690.hasFixedDuration = true;
            lTransition_39690.exitTime = 0.7115385f;
            lTransition_39690.duration = 0.25f;
            lTransition_39690.offset = 0f;
            lTransition_39690.mute = false;
            lTransition_39690.solo = false;
            lTransition_39690.canTransitionToSelf = true;
            lTransition_39690.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            for (int i = lTransition_39690.conditions.Length - 1; i >= 0; i--) { lTransition_39690.RemoveCondition(lTransition_39690.conditions[i]); }
            lTransition_39690.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 9f, "L" + rLayerIndex + "MotionForm");

            UnityEditor.Animations.AnimatorStateTransition lTransition_39692 = MotionControllerMotion.EditorFindTransition(lState_38448, lState_38474, 0);
            if (lTransition_39692 == null) { lTransition_39692 = lState_38448.AddTransition(lState_38474); }
            lTransition_39692.isExit = false;
            lTransition_39692.hasExitTime = true;
            lTransition_39692.hasFixedDuration = true;
            lTransition_39692.exitTime = 0.7115385f;
            lTransition_39692.duration = 0.25f;
            lTransition_39692.offset = 0f;
            lTransition_39692.mute = false;
            lTransition_39692.solo = false;
            lTransition_39692.canTransitionToSelf = true;
            lTransition_39692.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            for (int i = lTransition_39692.conditions.Length - 1; i >= 0; i--) { lTransition_39692.RemoveCondition(lTransition_39692.conditions[i]); }
            lTransition_39692.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 11f, "L" + rLayerIndex + "MotionForm");

            UnityEditor.Animations.AnimatorStateTransition lTransition_39694 = MotionControllerMotion.EditorFindTransition(lState_38448, lState_38476, 0);
            if (lTransition_39694 == null) { lTransition_39694 = lState_38448.AddTransition(lState_38476); }
            lTransition_39694.isExit = false;
            lTransition_39694.hasExitTime = true;
            lTransition_39694.hasFixedDuration = true;
            lTransition_39694.exitTime = 0.7115385f;
            lTransition_39694.duration = 0.25f;
            lTransition_39694.offset = 0f;
            lTransition_39694.mute = false;
            lTransition_39694.solo = false;
            lTransition_39694.canTransitionToSelf = true;
            lTransition_39694.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            for (int i = lTransition_39694.conditions.Length - 1; i >= 0; i--) { lTransition_39694.RemoveCondition(lTransition_39694.conditions[i]); }
            lTransition_39694.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 12f, "L" + rLayerIndex + "MotionForm");

            UnityEditor.Animations.AnimatorStateTransition lTransition_39696 = MotionControllerMotion.EditorFindTransition(lState_38448, lState_38478, 0);
            if (lTransition_39696 == null) { lTransition_39696 = lState_38448.AddTransition(lState_38478); }
            lTransition_39696.isExit = false;
            lTransition_39696.hasExitTime = true;
            lTransition_39696.hasFixedDuration = true;
            lTransition_39696.exitTime = 0.7115385f;
            lTransition_39696.duration = 0.25f;
            lTransition_39696.offset = 0f;
            lTransition_39696.mute = false;
            lTransition_39696.solo = false;
            lTransition_39696.canTransitionToSelf = true;
            lTransition_39696.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            for (int i = lTransition_39696.conditions.Length - 1; i >= 0; i--) { lTransition_39696.RemoveCondition(lTransition_39696.conditions[i]); }
            lTransition_39696.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 13f, "L" + rLayerIndex + "MotionForm");

            UnityEditor.Animations.AnimatorStateTransition lTransition_39698 = MotionControllerMotion.EditorFindTransition(lState_38448, lState_38480, 0);
            if (lTransition_39698 == null) { lTransition_39698 = lState_38448.AddTransition(lState_38480); }
            lTransition_39698.isExit = false;
            lTransition_39698.hasExitTime = true;
            lTransition_39698.hasFixedDuration = true;
            lTransition_39698.exitTime = 0.7115385f;
            lTransition_39698.duration = 0.25f;
            lTransition_39698.offset = 0f;
            lTransition_39698.mute = false;
            lTransition_39698.solo = false;
            lTransition_39698.canTransitionToSelf = true;
            lTransition_39698.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            for (int i = lTransition_39698.conditions.Length - 1; i >= 0; i--) { lTransition_39698.RemoveCondition(lTransition_39698.conditions[i]); }
            lTransition_39698.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 14f, "L" + rLayerIndex + "MotionForm");

            UnityEditor.Animations.AnimatorStateTransition lTransition_39700 = MotionControllerMotion.EditorFindTransition(lState_38448, lState_38482, 0);
            if (lTransition_39700 == null) { lTransition_39700 = lState_38448.AddTransition(lState_38482); }
            lTransition_39700.isExit = false;
            lTransition_39700.hasExitTime = true;
            lTransition_39700.hasFixedDuration = true;
            lTransition_39700.exitTime = 0.7115385f;
            lTransition_39700.duration = 0.25f;
            lTransition_39700.offset = 0f;
            lTransition_39700.mute = false;
            lTransition_39700.solo = false;
            lTransition_39700.canTransitionToSelf = true;
            lTransition_39700.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            for (int i = lTransition_39700.conditions.Length - 1; i >= 0; i--) { lTransition_39700.RemoveCondition(lTransition_39700.conditions[i]); }
            lTransition_39700.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 15f, "L" + rLayerIndex + "MotionForm");

            UnityEditor.Animations.AnimatorStateTransition lTransition_39702 = MotionControllerMotion.EditorFindTransition(lState_38448, lState_38484, 0);
            if (lTransition_39702 == null) { lTransition_39702 = lState_38448.AddTransition(lState_38484); }
            lTransition_39702.isExit = false;
            lTransition_39702.hasExitTime = true;
            lTransition_39702.hasFixedDuration = true;
            lTransition_39702.exitTime = 0.7115385f;
            lTransition_39702.duration = 0.25f;
            lTransition_39702.offset = 0f;
            lTransition_39702.mute = false;
            lTransition_39702.solo = false;
            lTransition_39702.canTransitionToSelf = true;
            lTransition_39702.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            for (int i = lTransition_39702.conditions.Length - 1; i >= 0; i--) { lTransition_39702.RemoveCondition(lTransition_39702.conditions[i]); }
            lTransition_39702.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 16f, "L" + rLayerIndex + "MotionForm");

            UnityEditor.Animations.AnimatorStateTransition lTransition_39704 = MotionControllerMotion.EditorFindTransition(lState_38448, lState_38486, 0);
            if (lTransition_39704 == null) { lTransition_39704 = lState_38448.AddTransition(lState_38486); }
            lTransition_39704.isExit = false;
            lTransition_39704.hasExitTime = true;
            lTransition_39704.hasFixedDuration = true;
            lTransition_39704.exitTime = 0.7115385f;
            lTransition_39704.duration = 0.25f;
            lTransition_39704.offset = 0f;
            lTransition_39704.mute = false;
            lTransition_39704.solo = false;
            lTransition_39704.canTransitionToSelf = true;
            lTransition_39704.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            for (int i = lTransition_39704.conditions.Length - 1; i >= 0; i--) { lTransition_39704.RemoveCondition(lTransition_39704.conditions[i]); }
            lTransition_39704.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 17f, "L" + rLayerIndex + "MotionForm");

            UnityEditor.Animations.AnimatorStateTransition lTransition_39706 = MotionControllerMotion.EditorFindTransition(lState_38448, lState_38488, 0);
            if (lTransition_39706 == null) { lTransition_39706 = lState_38448.AddTransition(lState_38488); }
            lTransition_39706.isExit = false;
            lTransition_39706.hasExitTime = true;
            lTransition_39706.hasFixedDuration = true;
            lTransition_39706.exitTime = 0.7115385f;
            lTransition_39706.duration = 0.25f;
            lTransition_39706.offset = 0f;
            lTransition_39706.mute = false;
            lTransition_39706.solo = false;
            lTransition_39706.canTransitionToSelf = true;
            lTransition_39706.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            for (int i = lTransition_39706.conditions.Length - 1; i >= 0; i--) { lTransition_39706.RemoveCondition(lTransition_39706.conditions[i]); }
            lTransition_39706.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 18f, "L" + rLayerIndex + "MotionForm");

            UnityEditor.Animations.AnimatorStateTransition lTransition_39708 = MotionControllerMotion.EditorFindTransition(lState_38448, lState_38490, 0);
            if (lTransition_39708 == null) { lTransition_39708 = lState_38448.AddTransition(lState_38490); }
            lTransition_39708.isExit = false;
            lTransition_39708.hasExitTime = true;
            lTransition_39708.hasFixedDuration = true;
            lTransition_39708.exitTime = 0.7115385f;
            lTransition_39708.duration = 0.25f;
            lTransition_39708.offset = 0f;
            lTransition_39708.mute = false;
            lTransition_39708.solo = false;
            lTransition_39708.canTransitionToSelf = true;
            lTransition_39708.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            for (int i = lTransition_39708.conditions.Length - 1; i >= 0; i--) { lTransition_39708.RemoveCondition(lTransition_39708.conditions[i]); }
            lTransition_39708.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 19f, "L" + rLayerIndex + "MotionForm");

            UnityEditor.Animations.AnimatorStateTransition lTransition_39710 = MotionControllerMotion.EditorFindTransition(lState_38448, lState_38492, 0);
            if (lTransition_39710 == null) { lTransition_39710 = lState_38448.AddTransition(lState_38492); }
            lTransition_39710.isExit = false;
            lTransition_39710.hasExitTime = true;
            lTransition_39710.hasFixedDuration = true;
            lTransition_39710.exitTime = 0.7115385f;
            lTransition_39710.duration = 0.25f;
            lTransition_39710.offset = 0f;
            lTransition_39710.mute = false;
            lTransition_39710.solo = false;
            lTransition_39710.canTransitionToSelf = true;
            lTransition_39710.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            for (int i = lTransition_39710.conditions.Length - 1; i >= 0; i--) { lTransition_39710.RemoveCondition(lTransition_39710.conditions[i]); }
            lTransition_39710.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 20f, "L" + rLayerIndex + "MotionForm");

            UnityEditor.Animations.AnimatorStateTransition lTransition_39712 = MotionControllerMotion.EditorFindTransition(lState_38448, lState_38494, 0);
            if (lTransition_39712 == null) { lTransition_39712 = lState_38448.AddTransition(lState_38494); }
            lTransition_39712.isExit = false;
            lTransition_39712.hasExitTime = true;
            lTransition_39712.hasFixedDuration = true;
            lTransition_39712.exitTime = 0.7115385f;
            lTransition_39712.duration = 0.25f;
            lTransition_39712.offset = 0f;
            lTransition_39712.mute = false;
            lTransition_39712.solo = false;
            lTransition_39712.canTransitionToSelf = true;
            lTransition_39712.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            for (int i = lTransition_39712.conditions.Length - 1; i >= 0; i--) { lTransition_39712.RemoveCondition(lTransition_39712.conditions[i]); }
            lTransition_39712.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 21f, "L" + rLayerIndex + "MotionForm");

            UnityEditor.Animations.AnimatorStateTransition lTransition_39714 = MotionControllerMotion.EditorFindTransition(lState_38448, lState_38496, 0);
            if (lTransition_39714 == null) { lTransition_39714 = lState_38448.AddTransition(lState_38496); }
            lTransition_39714.isExit = false;
            lTransition_39714.hasExitTime = true;
            lTransition_39714.hasFixedDuration = true;
            lTransition_39714.exitTime = 0.7115385f;
            lTransition_39714.duration = 0.25f;
            lTransition_39714.offset = 0f;
            lTransition_39714.mute = false;
            lTransition_39714.solo = false;
            lTransition_39714.canTransitionToSelf = true;
            lTransition_39714.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            for (int i = lTransition_39714.conditions.Length - 1; i >= 0; i--) { lTransition_39714.RemoveCondition(lTransition_39714.conditions[i]); }
            lTransition_39714.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 22f, "L" + rLayerIndex + "MotionForm");

            UnityEditor.Animations.AnimatorStateTransition lTransition_39716 = MotionControllerMotion.EditorFindTransition(lState_38448, lState_38498, 0);
            if (lTransition_39716 == null) { lTransition_39716 = lState_38448.AddTransition(lState_38498); }
            lTransition_39716.isExit = false;
            lTransition_39716.hasExitTime = true;
            lTransition_39716.hasFixedDuration = true;
            lTransition_39716.exitTime = 0.7115385f;
            lTransition_39716.duration = 0.25f;
            lTransition_39716.offset = 0f;
            lTransition_39716.mute = false;
            lTransition_39716.solo = false;
            lTransition_39716.canTransitionToSelf = true;
            lTransition_39716.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            for (int i = lTransition_39716.conditions.Length - 1; i >= 0; i--) { lTransition_39716.RemoveCondition(lTransition_39716.conditions[i]); }
            lTransition_39716.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 23f, "L" + rLayerIndex + "MotionForm");

            UnityEditor.Animations.AnimatorStateTransition lTransition_39718 = MotionControllerMotion.EditorFindTransition(lState_39614, lState_39616, 0);
            if (lTransition_39718 == null) { lTransition_39718 = lState_39614.AddTransition(lState_39616); }
            lTransition_39718.isExit = false;
            lTransition_39718.hasExitTime = true;
            lTransition_39718.hasFixedDuration = true;
            lTransition_39718.exitTime = 0.7692308f;
            lTransition_39718.duration = 0.25f;
            lTransition_39718.offset = 0f;
            lTransition_39718.mute = false;
            lTransition_39718.solo = false;
            lTransition_39718.canTransitionToSelf = true;
            lTransition_39718.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            for (int i = lTransition_39718.conditions.Length - 1; i >= 0; i--) { lTransition_39718.RemoveCondition(lTransition_39718.conditions[i]); }

            UnityEditor.Animations.AnimatorStateTransition lTransition_39720 = MotionControllerMotion.EditorFindTransition(lState_38452, lState_39620, 0);
            if (lTransition_39720 == null) { lTransition_39720 = lState_38452.AddTransition(lState_39620); }
            lTransition_39720.isExit = false;
            lTransition_39720.hasExitTime = true;
            lTransition_39720.hasFixedDuration = true;
            lTransition_39720.exitTime = 0.8910829f;
            lTransition_39720.duration = 0.09076428f;
            lTransition_39720.offset = 0f;
            lTransition_39720.mute = false;
            lTransition_39720.solo = false;
            lTransition_39720.canTransitionToSelf = true;
            lTransition_39720.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            for (int i = lTransition_39720.conditions.Length - 1; i >= 0; i--) { lTransition_39720.RemoveCondition(lTransition_39720.conditions[i]); }

            UnityEditor.Animations.AnimatorStateTransition lTransition_39722 = MotionControllerMotion.EditorFindTransition(lState_39618, lState_39612, 0);
            if (lTransition_39722 == null) { lTransition_39722 = lState_39618.AddTransition(lState_39612); }
            lTransition_39722.isExit = false;
            lTransition_39722.hasExitTime = true;
            lTransition_39722.hasFixedDuration = true;
            lTransition_39722.exitTime = 0.8591989f;
            lTransition_39722.duration = 0.1548803f;
            lTransition_39722.offset = 17.12114f;
            lTransition_39722.mute = false;
            lTransition_39722.solo = false;
            lTransition_39722.canTransitionToSelf = true;
            lTransition_39722.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            for (int i = lTransition_39722.conditions.Length - 1; i >= 0; i--) { lTransition_39722.RemoveCondition(lTransition_39722.conditions[i]); }
            lTransition_39722.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 0f, "L" + rLayerIndex + "MotionParameter");

            UnityEditor.Animations.AnimatorStateTransition lTransition_39724 = MotionControllerMotion.EditorFindTransition(lState_39618, lState_39614, 0);
            if (lTransition_39724 == null) { lTransition_39724 = lState_39618.AddTransition(lState_39614); }
            lTransition_39724.isExit = false;
            lTransition_39724.hasExitTime = true;
            lTransition_39724.hasFixedDuration = true;
            lTransition_39724.exitTime = 0.7727273f;
            lTransition_39724.duration = 0.25f;
            lTransition_39724.offset = 0f;
            lTransition_39724.mute = false;
            lTransition_39724.solo = false;
            lTransition_39724.canTransitionToSelf = true;
            lTransition_39724.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            for (int i = lTransition_39724.conditions.Length - 1; i >= 0; i--) { lTransition_39724.RemoveCondition(lTransition_39724.conditions[i]); }
            lTransition_39724.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 1f, "L" + rLayerIndex + "MotionParameter");

            UnityEditor.Animations.AnimatorStateTransition lTransition_39726 = MotionControllerMotion.EditorFindTransition(lState_39620, lState_39618, 0);
            if (lTransition_39726 == null) { lTransition_39726 = lState_39620.AddTransition(lState_39618); }
            lTransition_39726.isExit = false;
            lTransition_39726.hasExitTime = false;
            lTransition_39726.hasFixedDuration = true;
            lTransition_39726.exitTime = 0f;
            lTransition_39726.duration = 0.1f;
            lTransition_39726.offset = 0f;
            lTransition_39726.mute = false;
            lTransition_39726.solo = false;
            lTransition_39726.canTransitionToSelf = true;
            lTransition_39726.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            for (int i = lTransition_39726.conditions.Length - 1; i >= 0; i--) { lTransition_39726.RemoveCondition(lTransition_39726.conditions[i]); }
            lTransition_39726.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 32144f, "L" + rLayerIndex + "MotionPhase");

            UnityEditor.Animations.AnimatorStateTransition lTransition_39728 = MotionControllerMotion.EditorFindTransition(lState_39620, lState_39614, 0);
            if (lTransition_39728 == null) { lTransition_39728 = lState_39620.AddTransition(lState_39614); }
            lTransition_39728.isExit = false;
            lTransition_39728.hasExitTime = false;
            lTransition_39728.hasFixedDuration = true;
            lTransition_39728.exitTime = 0f;
            lTransition_39728.duration = 0.15f;
            lTransition_39728.offset = 0f;
            lTransition_39728.mute = false;
            lTransition_39728.solo = false;
            lTransition_39728.canTransitionToSelf = true;
            lTransition_39728.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            for (int i = lTransition_39728.conditions.Length - 1; i >= 0; i--) { lTransition_39728.RemoveCondition(lTransition_39728.conditions[i]); }
            lTransition_39728.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 32143f, "L" + rLayerIndex + "MotionPhase");
            lTransition_39728.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 1f, "L" + rLayerIndex + "MotionParameter");

            UnityEditor.Animations.AnimatorStateTransition lTransition_39730 = MotionControllerMotion.EditorFindTransition(lState_39620, lState_39612, 0);
            if (lTransition_39730 == null) { lTransition_39730 = lState_39620.AddTransition(lState_39612); }
            lTransition_39730.isExit = false;
            lTransition_39730.hasExitTime = false;
            lTransition_39730.hasFixedDuration = true;
            lTransition_39730.exitTime = 0f;
            lTransition_39730.duration = 0.15f;
            lTransition_39730.offset = 0f;
            lTransition_39730.mute = false;
            lTransition_39730.solo = false;
            lTransition_39730.canTransitionToSelf = true;
            lTransition_39730.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            for (int i = lTransition_39730.conditions.Length - 1; i >= 0; i--) { lTransition_39730.RemoveCondition(lTransition_39730.conditions[i]); }
            lTransition_39730.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 32143f, "L" + rLayerIndex + "MotionPhase");
            lTransition_39730.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 0f, "L" + rLayerIndex + "MotionParameter");

            UnityEditor.Animations.AnimatorStateTransition lTransition_39732 = MotionControllerMotion.EditorFindTransition(lState_38454, lState_39612, 0);
            if (lTransition_39732 == null) { lTransition_39732 = lState_38454.AddTransition(lState_39612); }
            lTransition_39732.isExit = false;
            lTransition_39732.hasExitTime = true;
            lTransition_39732.hasFixedDuration = true;
            lTransition_39732.exitTime = 1f;
            lTransition_39732.duration = 0f;
            lTransition_39732.offset = 0f;
            lTransition_39732.mute = false;
            lTransition_39732.solo = false;
            lTransition_39732.canTransitionToSelf = true;
            lTransition_39732.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            for (int i = lTransition_39732.conditions.Length - 1; i >= 0; i--) { lTransition_39732.RemoveCondition(lTransition_39732.conditions[i]); }
            lTransition_39732.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 0f, "L" + rLayerIndex + "MotionParameter");

            UnityEditor.Animations.AnimatorStateTransition lTransition_39734 = MotionControllerMotion.EditorFindTransition(lState_38454, lState_39614, 0);
            if (lTransition_39734 == null) { lTransition_39734 = lState_38454.AddTransition(lState_39614); }
            lTransition_39734.isExit = false;
            lTransition_39734.hasExitTime = true;
            lTransition_39734.hasFixedDuration = true;
            lTransition_39734.exitTime = 0.8846154f;
            lTransition_39734.duration = 0.25f;
            lTransition_39734.offset = 0f;
            lTransition_39734.mute = false;
            lTransition_39734.solo = false;
            lTransition_39734.canTransitionToSelf = true;
            lTransition_39734.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            for (int i = lTransition_39734.conditions.Length - 1; i >= 0; i--) { lTransition_39734.RemoveCondition(lTransition_39734.conditions[i]); }
            lTransition_39734.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 1f, "L" + rLayerIndex + "MotionParameter");

            UnityEditor.Animations.AnimatorStateTransition lTransition_39736 = MotionControllerMotion.EditorFindTransition(lState_38456, lState_39622, 0);
            if (lTransition_39736 == null) { lTransition_39736 = lState_38456.AddTransition(lState_39622); }
            lTransition_39736.isExit = false;
            lTransition_39736.hasExitTime = true;
            lTransition_39736.hasFixedDuration = true;
            lTransition_39736.exitTime = 0.9f;
            lTransition_39736.duration = 0.1f;
            lTransition_39736.offset = 0f;
            lTransition_39736.mute = false;
            lTransition_39736.solo = false;
            lTransition_39736.canTransitionToSelf = true;
            lTransition_39736.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            for (int i = lTransition_39736.conditions.Length - 1; i >= 0; i--) { lTransition_39736.RemoveCondition(lTransition_39736.conditions[i]); }

            UnityEditor.Animations.AnimatorStateTransition lTransition_39738 = MotionControllerMotion.EditorFindTransition(lState_39622, lState_39624, 0);
            if (lTransition_39738 == null) { lTransition_39738 = lState_39622.AddTransition(lState_39624); }
            lTransition_39738.isExit = false;
            lTransition_39738.hasExitTime = true;
            lTransition_39738.hasFixedDuration = true;
            lTransition_39738.exitTime = 0f;
            lTransition_39738.duration = 0.25f;
            lTransition_39738.offset = 0f;
            lTransition_39738.mute = false;
            lTransition_39738.solo = false;
            lTransition_39738.canTransitionToSelf = true;
            lTransition_39738.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            for (int i = lTransition_39738.conditions.Length - 1; i >= 0; i--) { lTransition_39738.RemoveCondition(lTransition_39738.conditions[i]); }
            lTransition_39738.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 32144f, "L" + rLayerIndex + "MotionPhase");

            UnityEditor.Animations.AnimatorStateTransition lTransition_39740 = MotionControllerMotion.EditorFindTransition(lState_39622, lState_39612, 0);
            if (lTransition_39740 == null) { lTransition_39740 = lState_39622.AddTransition(lState_39612); }
            lTransition_39740.isExit = false;
            lTransition_39740.hasExitTime = false;
            lTransition_39740.hasFixedDuration = true;
            lTransition_39740.exitTime = 0f;
            lTransition_39740.duration = 0.25f;
            lTransition_39740.offset = 0f;
            lTransition_39740.mute = false;
            lTransition_39740.solo = false;
            lTransition_39740.canTransitionToSelf = true;
            lTransition_39740.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            for (int i = lTransition_39740.conditions.Length - 1; i >= 0; i--) { lTransition_39740.RemoveCondition(lTransition_39740.conditions[i]); }
            lTransition_39740.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 32143f, "L" + rLayerIndex + "MotionPhase");
            lTransition_39740.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 0f, "L" + rLayerIndex + "MotionParameter");

            UnityEditor.Animations.AnimatorStateTransition lTransition_39742 = MotionControllerMotion.EditorFindTransition(lState_39622, lState_39614, 0);
            if (lTransition_39742 == null) { lTransition_39742 = lState_39622.AddTransition(lState_39614); }
            lTransition_39742.isExit = false;
            lTransition_39742.hasExitTime = false;
            lTransition_39742.hasFixedDuration = true;
            lTransition_39742.exitTime = 0f;
            lTransition_39742.duration = 0.25f;
            lTransition_39742.offset = 0f;
            lTransition_39742.mute = false;
            lTransition_39742.solo = false;
            lTransition_39742.canTransitionToSelf = true;
            lTransition_39742.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            for (int i = lTransition_39742.conditions.Length - 1; i >= 0; i--) { lTransition_39742.RemoveCondition(lTransition_39742.conditions[i]); }
            lTransition_39742.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 32143f, "L" + rLayerIndex + "MotionPhase");
            lTransition_39742.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 1f, "L" + rLayerIndex + "MotionParameter");

            UnityEditor.Animations.AnimatorStateTransition lTransition_39744 = MotionControllerMotion.EditorFindTransition(lState_39624, lState_39612, 0);
            if (lTransition_39744 == null) { lTransition_39744 = lState_39624.AddTransition(lState_39612); }
            lTransition_39744.isExit = false;
            lTransition_39744.hasExitTime = true;
            lTransition_39744.hasFixedDuration = true;
            lTransition_39744.exitTime = 0.88f;
            lTransition_39744.duration = 0.25f;
            lTransition_39744.offset = 0f;
            lTransition_39744.mute = false;
            lTransition_39744.solo = false;
            lTransition_39744.canTransitionToSelf = true;
            lTransition_39744.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            for (int i = lTransition_39744.conditions.Length - 1; i >= 0; i--) { lTransition_39744.RemoveCondition(lTransition_39744.conditions[i]); }
            lTransition_39744.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 0f, "L" + rLayerIndex + "MotionParameter");

            UnityEditor.Animations.AnimatorStateTransition lTransition_39746 = MotionControllerMotion.EditorFindTransition(lState_39624, lState_39614, 0);
            if (lTransition_39746 == null) { lTransition_39746 = lState_39624.AddTransition(lState_39614); }
            lTransition_39746.isExit = false;
            lTransition_39746.hasExitTime = true;
            lTransition_39746.hasFixedDuration = true;
            lTransition_39746.exitTime = 0.88f;
            lTransition_39746.duration = 0.25f;
            lTransition_39746.offset = 0f;
            lTransition_39746.mute = false;
            lTransition_39746.solo = false;
            lTransition_39746.canTransitionToSelf = true;
            lTransition_39746.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            for (int i = lTransition_39746.conditions.Length - 1; i >= 0; i--) { lTransition_39746.RemoveCondition(lTransition_39746.conditions[i]); }
            lTransition_39746.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 1f, "L" + rLayerIndex + "MotionParameter");

            UnityEditor.Animations.AnimatorStateTransition lTransition_39748 = MotionControllerMotion.EditorFindTransition(lState_38458, lState_39612, 0);
            if (lTransition_39748 == null) { lTransition_39748 = lState_38458.AddTransition(lState_39612); }
            lTransition_39748.isExit = false;
            lTransition_39748.hasExitTime = true;
            lTransition_39748.hasFixedDuration = true;
            lTransition_39748.exitTime = 0.6393927f;
            lTransition_39748.duration = 0.25f;
            lTransition_39748.offset = 0f;
            lTransition_39748.mute = false;
            lTransition_39748.solo = false;
            lTransition_39748.canTransitionToSelf = true;
            lTransition_39748.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            for (int i = lTransition_39748.conditions.Length - 1; i >= 0; i--) { lTransition_39748.RemoveCondition(lTransition_39748.conditions[i]); }
            lTransition_39748.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 0f, "L" + rLayerIndex + "MotionParameter");

            UnityEditor.Animations.AnimatorStateTransition lTransition_39750 = MotionControllerMotion.EditorFindTransition(lState_38458, lState_39614, 0);
            if (lTransition_39750 == null) { lTransition_39750 = lState_38458.AddTransition(lState_39614); }
            lTransition_39750.isExit = false;
            lTransition_39750.hasExitTime = true;
            lTransition_39750.hasFixedDuration = true;
            lTransition_39750.exitTime = 0.5381515f;
            lTransition_39750.duration = 0.2499999f;
            lTransition_39750.offset = 0f;
            lTransition_39750.mute = false;
            lTransition_39750.solo = false;
            lTransition_39750.canTransitionToSelf = true;
            lTransition_39750.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            for (int i = lTransition_39750.conditions.Length - 1; i >= 0; i--) { lTransition_39750.RemoveCondition(lTransition_39750.conditions[i]); }
            lTransition_39750.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 1f, "L" + rLayerIndex + "MotionParameter");

            UnityEditor.Animations.AnimatorStateTransition lTransition_39752 = MotionControllerMotion.EditorFindTransition(lState_38460, lState_39612, 0);
            if (lTransition_39752 == null) { lTransition_39752 = lState_38460.AddTransition(lState_39612); }
            lTransition_39752.isExit = false;
            lTransition_39752.hasExitTime = true;
            lTransition_39752.hasFixedDuration = true;
            lTransition_39752.exitTime = 0.8088763f;
            lTransition_39752.duration = 0.2499999f;
            lTransition_39752.offset = 0f;
            lTransition_39752.mute = false;
            lTransition_39752.solo = false;
            lTransition_39752.canTransitionToSelf = true;
            lTransition_39752.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            for (int i = lTransition_39752.conditions.Length - 1; i >= 0; i--) { lTransition_39752.RemoveCondition(lTransition_39752.conditions[i]); }
            lTransition_39752.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 0f, "L" + rLayerIndex + "MotionParameter");

            UnityEditor.Animations.AnimatorStateTransition lTransition_39754 = MotionControllerMotion.EditorFindTransition(lState_38460, lState_39614, 0);
            if (lTransition_39754 == null) { lTransition_39754 = lState_38460.AddTransition(lState_39614); }
            lTransition_39754.isExit = false;
            lTransition_39754.hasExitTime = true;
            lTransition_39754.hasFixedDuration = true;
            lTransition_39754.exitTime = 0.6674351f;
            lTransition_39754.duration = 0.2499999f;
            lTransition_39754.offset = 0f;
            lTransition_39754.mute = false;
            lTransition_39754.solo = false;
            lTransition_39754.canTransitionToSelf = true;
            lTransition_39754.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            for (int i = lTransition_39754.conditions.Length - 1; i >= 0; i--) { lTransition_39754.RemoveCondition(lTransition_39754.conditions[i]); }
            lTransition_39754.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 1f, "L" + rLayerIndex + "MotionParameter");

            UnityEditor.Animations.AnimatorStateTransition lTransition_39756 = MotionControllerMotion.EditorFindTransition(lState_38462, lState_39626, 0);
            if (lTransition_39756 == null) { lTransition_39756 = lState_38462.AddTransition(lState_39626); }
            lTransition_39756.isExit = false;
            lTransition_39756.hasExitTime = true;
            lTransition_39756.hasFixedDuration = true;
            lTransition_39756.exitTime = 0.2105264f;
            lTransition_39756.duration = 0.25f;
            lTransition_39756.offset = 0f;
            lTransition_39756.mute = false;
            lTransition_39756.solo = false;
            lTransition_39756.canTransitionToSelf = true;
            lTransition_39756.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            for (int i = lTransition_39756.conditions.Length - 1; i >= 0; i--) { lTransition_39756.RemoveCondition(lTransition_39756.conditions[i]); }

            UnityEditor.Animations.AnimatorStateTransition lTransition_39758 = MotionControllerMotion.EditorFindTransition(lState_39626, lState_39628, 0);
            if (lTransition_39758 == null) { lTransition_39758 = lState_39626.AddTransition(lState_39628); }
            lTransition_39758.isExit = false;
            lTransition_39758.hasExitTime = true;
            lTransition_39758.hasFixedDuration = true;
            lTransition_39758.exitTime = 0f;
            lTransition_39758.duration = 0.25f;
            lTransition_39758.offset = 0f;
            lTransition_39758.mute = false;
            lTransition_39758.solo = false;
            lTransition_39758.canTransitionToSelf = true;
            lTransition_39758.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            for (int i = lTransition_39758.conditions.Length - 1; i >= 0; i--) { lTransition_39758.RemoveCondition(lTransition_39758.conditions[i]); }
            lTransition_39758.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 32144f, "L" + rLayerIndex + "MotionPhase");

            UnityEditor.Animations.AnimatorStateTransition lTransition_39760 = MotionControllerMotion.EditorFindTransition(lState_39626, lState_39612, 0);
            if (lTransition_39760 == null) { lTransition_39760 = lState_39626.AddTransition(lState_39612); }
            lTransition_39760.isExit = false;
            lTransition_39760.hasExitTime = false;
            lTransition_39760.hasFixedDuration = true;
            lTransition_39760.exitTime = 0f;
            lTransition_39760.duration = 0.25f;
            lTransition_39760.offset = 0f;
            lTransition_39760.mute = false;
            lTransition_39760.solo = false;
            lTransition_39760.canTransitionToSelf = true;
            lTransition_39760.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            for (int i = lTransition_39760.conditions.Length - 1; i >= 0; i--) { lTransition_39760.RemoveCondition(lTransition_39760.conditions[i]); }
            lTransition_39760.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 32143f, "L" + rLayerIndex + "MotionPhase");
            lTransition_39760.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 0f, "L" + rLayerIndex + "MotionParameter");

            UnityEditor.Animations.AnimatorStateTransition lTransition_39762 = MotionControllerMotion.EditorFindTransition(lState_39626, lState_39614, 0);
            if (lTransition_39762 == null) { lTransition_39762 = lState_39626.AddTransition(lState_39614); }
            lTransition_39762.isExit = false;
            lTransition_39762.hasExitTime = false;
            lTransition_39762.hasFixedDuration = true;
            lTransition_39762.exitTime = 0f;
            lTransition_39762.duration = 0.25f;
            lTransition_39762.offset = 0f;
            lTransition_39762.mute = false;
            lTransition_39762.solo = false;
            lTransition_39762.canTransitionToSelf = true;
            lTransition_39762.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            for (int i = lTransition_39762.conditions.Length - 1; i >= 0; i--) { lTransition_39762.RemoveCondition(lTransition_39762.conditions[i]); }
            lTransition_39762.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 32143f, "L" + rLayerIndex + "MotionPhase");
            lTransition_39762.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 1f, "L" + rLayerIndex + "MotionParameter");

            UnityEditor.Animations.AnimatorStateTransition lTransition_39764 = MotionControllerMotion.EditorFindTransition(lState_39628, lState_39612, 0);
            if (lTransition_39764 == null) { lTransition_39764 = lState_39628.AddTransition(lState_39612); }
            lTransition_39764.isExit = false;
            lTransition_39764.hasExitTime = true;
            lTransition_39764.hasFixedDuration = true;
            lTransition_39764.exitTime = 0.8717949f;
            lTransition_39764.duration = 0.25f;
            lTransition_39764.offset = 0f;
            lTransition_39764.mute = false;
            lTransition_39764.solo = false;
            lTransition_39764.canTransitionToSelf = true;
            lTransition_39764.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            for (int i = lTransition_39764.conditions.Length - 1; i >= 0; i--) { lTransition_39764.RemoveCondition(lTransition_39764.conditions[i]); }
            lTransition_39764.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 0f, "L" + rLayerIndex + "MotionParameter");

            UnityEditor.Animations.AnimatorStateTransition lTransition_39766 = MotionControllerMotion.EditorFindTransition(lState_39628, lState_39614, 0);
            if (lTransition_39766 == null) { lTransition_39766 = lState_39628.AddTransition(lState_39614); }
            lTransition_39766.isExit = false;
            lTransition_39766.hasExitTime = true;
            lTransition_39766.hasFixedDuration = true;
            lTransition_39766.exitTime = 0.8717949f;
            lTransition_39766.duration = 0.25f;
            lTransition_39766.offset = 0f;
            lTransition_39766.mute = false;
            lTransition_39766.solo = false;
            lTransition_39766.canTransitionToSelf = true;
            lTransition_39766.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            for (int i = lTransition_39766.conditions.Length - 1; i >= 0; i--) { lTransition_39766.RemoveCondition(lTransition_39766.conditions[i]); }
            lTransition_39766.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 1f, "L" + rLayerIndex + "MotionParameter");

            UnityEditor.Animations.AnimatorStateTransition lTransition_39768 = MotionControllerMotion.EditorFindTransition(lState_38464, lState_39612, 0);
            if (lTransition_39768 == null) { lTransition_39768 = lState_38464.AddTransition(lState_39612); }
            lTransition_39768.isExit = false;
            lTransition_39768.hasExitTime = true;
            lTransition_39768.hasFixedDuration = true;
            lTransition_39768.exitTime = 0.9152542f;
            lTransition_39768.duration = 0.25f;
            lTransition_39768.offset = 0f;
            lTransition_39768.mute = false;
            lTransition_39768.solo = false;
            lTransition_39768.canTransitionToSelf = true;
            lTransition_39768.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            for (int i = lTransition_39768.conditions.Length - 1; i >= 0; i--) { lTransition_39768.RemoveCondition(lTransition_39768.conditions[i]); }
            lTransition_39768.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 0f, "L" + rLayerIndex + "MotionParameter");

            UnityEditor.Animations.AnimatorStateTransition lTransition_39770 = MotionControllerMotion.EditorFindTransition(lState_38464, lState_39614, 0);
            if (lTransition_39770 == null) { lTransition_39770 = lState_38464.AddTransition(lState_39614); }
            lTransition_39770.isExit = false;
            lTransition_39770.hasExitTime = true;
            lTransition_39770.hasFixedDuration = true;
            lTransition_39770.exitTime = 0.6196442f;
            lTransition_39770.duration = 0.25f;
            lTransition_39770.offset = 0f;
            lTransition_39770.mute = false;
            lTransition_39770.solo = false;
            lTransition_39770.canTransitionToSelf = true;
            lTransition_39770.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            for (int i = lTransition_39770.conditions.Length - 1; i >= 0; i--) { lTransition_39770.RemoveCondition(lTransition_39770.conditions[i]); }
            lTransition_39770.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 1f, "L" + rLayerIndex + "MotionParameter");

            UnityEditor.Animations.AnimatorStateTransition lTransition_39772 = MotionControllerMotion.EditorFindTransition(lState_38474, lState_39630, 0);
            if (lTransition_39772 == null) { lTransition_39772 = lState_38474.AddTransition(lState_39630); }
            lTransition_39772.isExit = false;
            lTransition_39772.hasExitTime = true;
            lTransition_39772.hasFixedDuration = true;
            lTransition_39772.exitTime = 0.9152542f;
            lTransition_39772.duration = 0.25f;
            lTransition_39772.offset = 0f;
            lTransition_39772.mute = false;
            lTransition_39772.solo = false;
            lTransition_39772.canTransitionToSelf = true;
            lTransition_39772.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            for (int i = lTransition_39772.conditions.Length - 1; i >= 0; i--) { lTransition_39772.RemoveCondition(lTransition_39772.conditions[i]); }

            UnityEditor.Animations.AnimatorStateTransition lTransition_39774 = MotionControllerMotion.EditorFindTransition(lState_39630, lState_39632, 0);
            if (lTransition_39774 == null) { lTransition_39774 = lState_39630.AddTransition(lState_39632); }
            lTransition_39774.isExit = false;
            lTransition_39774.hasExitTime = true;
            lTransition_39774.hasFixedDuration = true;
            lTransition_39774.exitTime = 0.9152542f;
            lTransition_39774.duration = 0.25f;
            lTransition_39774.offset = 0f;
            lTransition_39774.mute = false;
            lTransition_39774.solo = false;
            lTransition_39774.canTransitionToSelf = true;
            lTransition_39774.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            for (int i = lTransition_39774.conditions.Length - 1; i >= 0; i--) { lTransition_39774.RemoveCondition(lTransition_39774.conditions[i]); }
            lTransition_39774.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 32144f, "L" + rLayerIndex + "MotionPhase");

            UnityEditor.Animations.AnimatorStateTransition lTransition_39776 = MotionControllerMotion.EditorFindTransition(lState_39630, lState_39612, 0);
            if (lTransition_39776 == null) { lTransition_39776 = lState_39630.AddTransition(lState_39612); }
            lTransition_39776.isExit = false;
            lTransition_39776.hasExitTime = false;
            lTransition_39776.hasFixedDuration = true;
            lTransition_39776.exitTime = 0.9152542f;
            lTransition_39776.duration = 0.25f;
            lTransition_39776.offset = 0f;
            lTransition_39776.mute = false;
            lTransition_39776.solo = false;
            lTransition_39776.canTransitionToSelf = true;
            lTransition_39776.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            for (int i = lTransition_39776.conditions.Length - 1; i >= 0; i--) { lTransition_39776.RemoveCondition(lTransition_39776.conditions[i]); }
            lTransition_39776.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 32143f, "L" + rLayerIndex + "MotionPhase");
            lTransition_39776.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 0f, "L" + rLayerIndex + "MotionParameter");

            UnityEditor.Animations.AnimatorStateTransition lTransition_39778 = MotionControllerMotion.EditorFindTransition(lState_39630, lState_39614, 0);
            if (lTransition_39778 == null) { lTransition_39778 = lState_39630.AddTransition(lState_39614); }
            lTransition_39778.isExit = false;
            lTransition_39778.hasExitTime = false;
            lTransition_39778.hasFixedDuration = true;
            lTransition_39778.exitTime = 0.9152542f;
            lTransition_39778.duration = 0.25f;
            lTransition_39778.offset = 0f;
            lTransition_39778.mute = false;
            lTransition_39778.solo = false;
            lTransition_39778.canTransitionToSelf = true;
            lTransition_39778.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            for (int i = lTransition_39778.conditions.Length - 1; i >= 0; i--) { lTransition_39778.RemoveCondition(lTransition_39778.conditions[i]); }
            lTransition_39778.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 32143f, "L" + rLayerIndex + "MotionPhase");
            lTransition_39778.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 1f, "L" + rLayerIndex + "MotionParameter");

            UnityEditor.Animations.AnimatorStateTransition lTransition_39780 = MotionControllerMotion.EditorFindTransition(lState_39632, lState_39612, 0);
            if (lTransition_39780 == null) { lTransition_39780 = lState_39632.AddTransition(lState_39612); }
            lTransition_39780.isExit = false;
            lTransition_39780.hasExitTime = true;
            lTransition_39780.hasFixedDuration = true;
            lTransition_39780.exitTime = 0.9152542f;
            lTransition_39780.duration = 0.25f;
            lTransition_39780.offset = 0f;
            lTransition_39780.mute = false;
            lTransition_39780.solo = false;
            lTransition_39780.canTransitionToSelf = true;
            lTransition_39780.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            for (int i = lTransition_39780.conditions.Length - 1; i >= 0; i--) { lTransition_39780.RemoveCondition(lTransition_39780.conditions[i]); }
            lTransition_39780.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 0f, "L" + rLayerIndex + "MotionParameter");

            UnityEditor.Animations.AnimatorStateTransition lTransition_39782 = MotionControllerMotion.EditorFindTransition(lState_39632, lState_39614, 0);
            if (lTransition_39782 == null) { lTransition_39782 = lState_39632.AddTransition(lState_39614); }
            lTransition_39782.isExit = false;
            lTransition_39782.hasExitTime = true;
            lTransition_39782.hasFixedDuration = true;
            lTransition_39782.exitTime = 0.9152542f;
            lTransition_39782.duration = 0.25f;
            lTransition_39782.offset = 0f;
            lTransition_39782.mute = false;
            lTransition_39782.solo = false;
            lTransition_39782.canTransitionToSelf = true;
            lTransition_39782.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            for (int i = lTransition_39782.conditions.Length - 1; i >= 0; i--) { lTransition_39782.RemoveCondition(lTransition_39782.conditions[i]); }
            lTransition_39782.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 1f, "L" + rLayerIndex + "MotionParameter");

            UnityEditor.Animations.AnimatorStateTransition lTransition_39784 = MotionControllerMotion.EditorFindTransition(lState_38476, lState_39612, 0);
            if (lTransition_39784 == null) { lTransition_39784 = lState_38476.AddTransition(lState_39612); }
            lTransition_39784.isExit = false;
            lTransition_39784.hasExitTime = true;
            lTransition_39784.hasFixedDuration = true;
            lTransition_39784.exitTime = 0.9230769f;
            lTransition_39784.duration = 0.25f;
            lTransition_39784.offset = 0f;
            lTransition_39784.mute = false;
            lTransition_39784.solo = false;
            lTransition_39784.canTransitionToSelf = true;
            lTransition_39784.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            for (int i = lTransition_39784.conditions.Length - 1; i >= 0; i--) { lTransition_39784.RemoveCondition(lTransition_39784.conditions[i]); }
            lTransition_39784.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 0f, "L" + rLayerIndex + "MotionParameter");

            UnityEditor.Animations.AnimatorStateTransition lTransition_39786 = MotionControllerMotion.EditorFindTransition(lState_38476, lState_39614, 0);
            if (lTransition_39786 == null) { lTransition_39786 = lState_38476.AddTransition(lState_39614); }
            lTransition_39786.isExit = false;
            lTransition_39786.hasExitTime = true;
            lTransition_39786.hasFixedDuration = true;
            lTransition_39786.exitTime = 0.9230769f;
            lTransition_39786.duration = 0.25f;
            lTransition_39786.offset = 0f;
            lTransition_39786.mute = false;
            lTransition_39786.solo = false;
            lTransition_39786.canTransitionToSelf = true;
            lTransition_39786.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            for (int i = lTransition_39786.conditions.Length - 1; i >= 0; i--) { lTransition_39786.RemoveCondition(lTransition_39786.conditions[i]); }
            lTransition_39786.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 1f, "L" + rLayerIndex + "MotionParameter");

            UnityEditor.Animations.AnimatorStateTransition lTransition_39788 = MotionControllerMotion.EditorFindTransition(lState_38478, lState_39634, 0);
            if (lTransition_39788 == null) { lTransition_39788 = lState_38478.AddTransition(lState_39634); }
            lTransition_39788.isExit = false;
            lTransition_39788.hasExitTime = true;
            lTransition_39788.hasFixedDuration = true;
            lTransition_39788.exitTime = 0.9230769f;
            lTransition_39788.duration = 0.25f;
            lTransition_39788.offset = 0f;
            lTransition_39788.mute = false;
            lTransition_39788.solo = false;
            lTransition_39788.canTransitionToSelf = true;
            lTransition_39788.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            for (int i = lTransition_39788.conditions.Length - 1; i >= 0; i--) { lTransition_39788.RemoveCondition(lTransition_39788.conditions[i]); }

            UnityEditor.Animations.AnimatorStateTransition lTransition_39790 = MotionControllerMotion.EditorFindTransition(lState_39634, lState_39636, 0);
            if (lTransition_39790 == null) { lTransition_39790 = lState_39634.AddTransition(lState_39636); }
            lTransition_39790.isExit = false;
            lTransition_39790.hasExitTime = true;
            lTransition_39790.hasFixedDuration = true;
            lTransition_39790.exitTime = 0.9230769f;
            lTransition_39790.duration = 0.25f;
            lTransition_39790.offset = 0f;
            lTransition_39790.mute = false;
            lTransition_39790.solo = false;
            lTransition_39790.canTransitionToSelf = true;
            lTransition_39790.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            for (int i = lTransition_39790.conditions.Length - 1; i >= 0; i--) { lTransition_39790.RemoveCondition(lTransition_39790.conditions[i]); }
            lTransition_39790.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 32144f, "L" + rLayerIndex + "MotionPhase");

            UnityEditor.Animations.AnimatorStateTransition lTransition_39792 = MotionControllerMotion.EditorFindTransition(lState_39634, lState_39612, 0);
            if (lTransition_39792 == null) { lTransition_39792 = lState_39634.AddTransition(lState_39612); }
            lTransition_39792.isExit = false;
            lTransition_39792.hasExitTime = false;
            lTransition_39792.hasFixedDuration = true;
            lTransition_39792.exitTime = 0.9230769f;
            lTransition_39792.duration = 0.25f;
            lTransition_39792.offset = 0f;
            lTransition_39792.mute = false;
            lTransition_39792.solo = false;
            lTransition_39792.canTransitionToSelf = true;
            lTransition_39792.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            for (int i = lTransition_39792.conditions.Length - 1; i >= 0; i--) { lTransition_39792.RemoveCondition(lTransition_39792.conditions[i]); }
            lTransition_39792.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 32143f, "L" + rLayerIndex + "MotionPhase");
            lTransition_39792.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 0f, "L" + rLayerIndex + "MotionParameter");

            UnityEditor.Animations.AnimatorStateTransition lTransition_39794 = MotionControllerMotion.EditorFindTransition(lState_39634, lState_39614, 0);
            if (lTransition_39794 == null) { lTransition_39794 = lState_39634.AddTransition(lState_39614); }
            lTransition_39794.isExit = false;
            lTransition_39794.hasExitTime = false;
            lTransition_39794.hasFixedDuration = true;
            lTransition_39794.exitTime = 0.9230769f;
            lTransition_39794.duration = 0.25f;
            lTransition_39794.offset = 0f;
            lTransition_39794.mute = false;
            lTransition_39794.solo = false;
            lTransition_39794.canTransitionToSelf = true;
            lTransition_39794.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            for (int i = lTransition_39794.conditions.Length - 1; i >= 0; i--) { lTransition_39794.RemoveCondition(lTransition_39794.conditions[i]); }
            lTransition_39794.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 32143f, "L" + rLayerIndex + "MotionPhase");
            lTransition_39794.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 1f, "L" + rLayerIndex + "MotionParameter");

            UnityEditor.Animations.AnimatorStateTransition lTransition_39796 = MotionControllerMotion.EditorFindTransition(lState_39636, lState_39612, 0);
            if (lTransition_39796 == null) { lTransition_39796 = lState_39636.AddTransition(lState_39612); }
            lTransition_39796.isExit = false;
            lTransition_39796.hasExitTime = true;
            lTransition_39796.hasFixedDuration = true;
            lTransition_39796.exitTime = 0.9230769f;
            lTransition_39796.duration = 0.25f;
            lTransition_39796.offset = 0f;
            lTransition_39796.mute = false;
            lTransition_39796.solo = false;
            lTransition_39796.canTransitionToSelf = true;
            lTransition_39796.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            for (int i = lTransition_39796.conditions.Length - 1; i >= 0; i--) { lTransition_39796.RemoveCondition(lTransition_39796.conditions[i]); }
            lTransition_39796.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 0f, "L" + rLayerIndex + "MotionParameter");

            UnityEditor.Animations.AnimatorStateTransition lTransition_39798 = MotionControllerMotion.EditorFindTransition(lState_39636, lState_39614, 0);
            if (lTransition_39798 == null) { lTransition_39798 = lState_39636.AddTransition(lState_39614); }
            lTransition_39798.isExit = false;
            lTransition_39798.hasExitTime = true;
            lTransition_39798.hasFixedDuration = true;
            lTransition_39798.exitTime = 0.9230769f;
            lTransition_39798.duration = 0.25f;
            lTransition_39798.offset = 0f;
            lTransition_39798.mute = false;
            lTransition_39798.solo = false;
            lTransition_39798.canTransitionToSelf = true;
            lTransition_39798.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            for (int i = lTransition_39798.conditions.Length - 1; i >= 0; i--) { lTransition_39798.RemoveCondition(lTransition_39798.conditions[i]); }
            lTransition_39798.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 1f, "L" + rLayerIndex + "MotionParameter");

            UnityEditor.Animations.AnimatorStateTransition lTransition_39800 = MotionControllerMotion.EditorFindTransition(lState_38480, lState_39612, 0);
            if (lTransition_39800 == null) { lTransition_39800 = lState_38480.AddTransition(lState_39612); }
            lTransition_39800.isExit = false;
            lTransition_39800.hasExitTime = true;
            lTransition_39800.hasFixedDuration = true;
            lTransition_39800.exitTime = 0.9074074f;
            lTransition_39800.duration = 0.25f;
            lTransition_39800.offset = 0f;
            lTransition_39800.mute = false;
            lTransition_39800.solo = false;
            lTransition_39800.canTransitionToSelf = true;
            lTransition_39800.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            for (int i = lTransition_39800.conditions.Length - 1; i >= 0; i--) { lTransition_39800.RemoveCondition(lTransition_39800.conditions[i]); }
            lTransition_39800.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 0f, "L" + rLayerIndex + "MotionParameter");

            UnityEditor.Animations.AnimatorStateTransition lTransition_39802 = MotionControllerMotion.EditorFindTransition(lState_38480, lState_39614, 0);
            if (lTransition_39802 == null) { lTransition_39802 = lState_38480.AddTransition(lState_39614); }
            lTransition_39802.isExit = false;
            lTransition_39802.hasExitTime = true;
            lTransition_39802.hasFixedDuration = true;
            lTransition_39802.exitTime = 0.9074074f;
            lTransition_39802.duration = 0.25f;
            lTransition_39802.offset = 0f;
            lTransition_39802.mute = false;
            lTransition_39802.solo = false;
            lTransition_39802.canTransitionToSelf = true;
            lTransition_39802.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            for (int i = lTransition_39802.conditions.Length - 1; i >= 0; i--) { lTransition_39802.RemoveCondition(lTransition_39802.conditions[i]); }
            lTransition_39802.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 1f, "L" + rLayerIndex + "MotionParameter");

            UnityEditor.Animations.AnimatorStateTransition lTransition_39804 = MotionControllerMotion.EditorFindTransition(lState_38482, lState_39638, 0);
            if (lTransition_39804 == null) { lTransition_39804 = lState_38482.AddTransition(lState_39638); }
            lTransition_39804.isExit = false;
            lTransition_39804.hasExitTime = true;
            lTransition_39804.hasFixedDuration = true;
            lTransition_39804.exitTime = 0.9074074f;
            lTransition_39804.duration = 0.25f;
            lTransition_39804.offset = 0f;
            lTransition_39804.mute = false;
            lTransition_39804.solo = false;
            lTransition_39804.canTransitionToSelf = true;
            lTransition_39804.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            for (int i = lTransition_39804.conditions.Length - 1; i >= 0; i--) { lTransition_39804.RemoveCondition(lTransition_39804.conditions[i]); }

            UnityEditor.Animations.AnimatorStateTransition lTransition_39806 = MotionControllerMotion.EditorFindTransition(lState_39638, lState_39640, 0);
            if (lTransition_39806 == null) { lTransition_39806 = lState_39638.AddTransition(lState_39640); }
            lTransition_39806.isExit = false;
            lTransition_39806.hasExitTime = true;
            lTransition_39806.hasFixedDuration = true;
            lTransition_39806.exitTime = 0.9074074f;
            lTransition_39806.duration = 0.25f;
            lTransition_39806.offset = 0f;
            lTransition_39806.mute = false;
            lTransition_39806.solo = false;
            lTransition_39806.canTransitionToSelf = true;
            lTransition_39806.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            for (int i = lTransition_39806.conditions.Length - 1; i >= 0; i--) { lTransition_39806.RemoveCondition(lTransition_39806.conditions[i]); }
            lTransition_39806.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 32144f, "L" + rLayerIndex + "MotionPhase");

            UnityEditor.Animations.AnimatorStateTransition lTransition_39808 = MotionControllerMotion.EditorFindTransition(lState_39638, lState_39612, 0);
            if (lTransition_39808 == null) { lTransition_39808 = lState_39638.AddTransition(lState_39612); }
            lTransition_39808.isExit = false;
            lTransition_39808.hasExitTime = false;
            lTransition_39808.hasFixedDuration = true;
            lTransition_39808.exitTime = 0.9074074f;
            lTransition_39808.duration = 0.25f;
            lTransition_39808.offset = 0f;
            lTransition_39808.mute = false;
            lTransition_39808.solo = false;
            lTransition_39808.canTransitionToSelf = true;
            lTransition_39808.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            for (int i = lTransition_39808.conditions.Length - 1; i >= 0; i--) { lTransition_39808.RemoveCondition(lTransition_39808.conditions[i]); }
            lTransition_39808.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 32143f, "L" + rLayerIndex + "MotionPhase");
            lTransition_39808.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 0f, "L" + rLayerIndex + "MotionParameter");

            UnityEditor.Animations.AnimatorStateTransition lTransition_39810 = MotionControllerMotion.EditorFindTransition(lState_39638, lState_39614, 0);
            if (lTransition_39810 == null) { lTransition_39810 = lState_39638.AddTransition(lState_39614); }
            lTransition_39810.isExit = false;
            lTransition_39810.hasExitTime = false;
            lTransition_39810.hasFixedDuration = true;
            lTransition_39810.exitTime = 0.9074074f;
            lTransition_39810.duration = 0.25f;
            lTransition_39810.offset = 0f;
            lTransition_39810.mute = false;
            lTransition_39810.solo = false;
            lTransition_39810.canTransitionToSelf = true;
            lTransition_39810.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            for (int i = lTransition_39810.conditions.Length - 1; i >= 0; i--) { lTransition_39810.RemoveCondition(lTransition_39810.conditions[i]); }
            lTransition_39810.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 32143f, "L" + rLayerIndex + "MotionPhase");
            lTransition_39810.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 1f, "L" + rLayerIndex + "MotionParameter");

            UnityEditor.Animations.AnimatorStateTransition lTransition_39812 = MotionControllerMotion.EditorFindTransition(lState_39640, lState_39612, 0);
            if (lTransition_39812 == null) { lTransition_39812 = lState_39640.AddTransition(lState_39612); }
            lTransition_39812.isExit = false;
            lTransition_39812.hasExitTime = true;
            lTransition_39812.hasFixedDuration = true;
            lTransition_39812.exitTime = 0.9074074f;
            lTransition_39812.duration = 0.25f;
            lTransition_39812.offset = 0f;
            lTransition_39812.mute = false;
            lTransition_39812.solo = false;
            lTransition_39812.canTransitionToSelf = true;
            lTransition_39812.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            for (int i = lTransition_39812.conditions.Length - 1; i >= 0; i--) { lTransition_39812.RemoveCondition(lTransition_39812.conditions[i]); }
            lTransition_39812.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 0f, "L" + rLayerIndex + "MotionParameter");

            UnityEditor.Animations.AnimatorStateTransition lTransition_39814 = MotionControllerMotion.EditorFindTransition(lState_39640, lState_39614, 0);
            if (lTransition_39814 == null) { lTransition_39814 = lState_39640.AddTransition(lState_39614); }
            lTransition_39814.isExit = false;
            lTransition_39814.hasExitTime = true;
            lTransition_39814.hasFixedDuration = true;
            lTransition_39814.exitTime = 0.9074074f;
            lTransition_39814.duration = 0.25f;
            lTransition_39814.offset = 0f;
            lTransition_39814.mute = false;
            lTransition_39814.solo = false;
            lTransition_39814.canTransitionToSelf = true;
            lTransition_39814.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            for (int i = lTransition_39814.conditions.Length - 1; i >= 0; i--) { lTransition_39814.RemoveCondition(lTransition_39814.conditions[i]); }
            lTransition_39814.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 1f, "L" + rLayerIndex + "MotionParameter");

            UnityEditor.Animations.AnimatorStateTransition lTransition_39816 = MotionControllerMotion.EditorFindTransition(lState_38484, lState_39612, 0);
            if (lTransition_39816 == null) { lTransition_39816 = lState_38484.AddTransition(lState_39612); }
            lTransition_39816.isExit = false;
            lTransition_39816.hasExitTime = true;
            lTransition_39816.hasFixedDuration = true;
            lTransition_39816.exitTime = 0.9050633f;
            lTransition_39816.duration = 0.25f;
            lTransition_39816.offset = 0f;
            lTransition_39816.mute = false;
            lTransition_39816.solo = false;
            lTransition_39816.canTransitionToSelf = true;
            lTransition_39816.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            for (int i = lTransition_39816.conditions.Length - 1; i >= 0; i--) { lTransition_39816.RemoveCondition(lTransition_39816.conditions[i]); }
            lTransition_39816.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 0f, "L" + rLayerIndex + "MotionParameter");

            UnityEditor.Animations.AnimatorStateTransition lTransition_39818 = MotionControllerMotion.EditorFindTransition(lState_38484, lState_39614, 0);
            if (lTransition_39818 == null) { lTransition_39818 = lState_38484.AddTransition(lState_39614); }
            lTransition_39818.isExit = false;
            lTransition_39818.hasExitTime = true;
            lTransition_39818.hasFixedDuration = true;
            lTransition_39818.exitTime = 0.9050633f;
            lTransition_39818.duration = 0.25f;
            lTransition_39818.offset = 0f;
            lTransition_39818.mute = false;
            lTransition_39818.solo = false;
            lTransition_39818.canTransitionToSelf = true;
            lTransition_39818.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            for (int i = lTransition_39818.conditions.Length - 1; i >= 0; i--) { lTransition_39818.RemoveCondition(lTransition_39818.conditions[i]); }
            lTransition_39818.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 1f, "L" + rLayerIndex + "MotionParameter");

            UnityEditor.Animations.AnimatorStateTransition lTransition_39820 = MotionControllerMotion.EditorFindTransition(lState_38486, lState_39642, 0);
            if (lTransition_39820 == null) { lTransition_39820 = lState_38486.AddTransition(lState_39642); }
            lTransition_39820.isExit = false;
            lTransition_39820.hasExitTime = true;
            lTransition_39820.hasFixedDuration = true;
            lTransition_39820.exitTime = 0.9050633f;
            lTransition_39820.duration = 0.25f;
            lTransition_39820.offset = 0f;
            lTransition_39820.mute = false;
            lTransition_39820.solo = false;
            lTransition_39820.canTransitionToSelf = true;
            lTransition_39820.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            for (int i = lTransition_39820.conditions.Length - 1; i >= 0; i--) { lTransition_39820.RemoveCondition(lTransition_39820.conditions[i]); }

            UnityEditor.Animations.AnimatorStateTransition lTransition_39822 = MotionControllerMotion.EditorFindTransition(lState_39642, lState_39644, 0);
            if (lTransition_39822 == null) { lTransition_39822 = lState_39642.AddTransition(lState_39644); }
            lTransition_39822.isExit = false;
            lTransition_39822.hasExitTime = true;
            lTransition_39822.hasFixedDuration = true;
            lTransition_39822.exitTime = 0.9050633f;
            lTransition_39822.duration = 0.25f;
            lTransition_39822.offset = 0f;
            lTransition_39822.mute = false;
            lTransition_39822.solo = false;
            lTransition_39822.canTransitionToSelf = true;
            lTransition_39822.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            for (int i = lTransition_39822.conditions.Length - 1; i >= 0; i--) { lTransition_39822.RemoveCondition(lTransition_39822.conditions[i]); }
            lTransition_39822.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 32144f, "L" + rLayerIndex + "MotionPhase");

            UnityEditor.Animations.AnimatorStateTransition lTransition_39824 = MotionControllerMotion.EditorFindTransition(lState_39642, lState_39612, 0);
            if (lTransition_39824 == null) { lTransition_39824 = lState_39642.AddTransition(lState_39612); }
            lTransition_39824.isExit = false;
            lTransition_39824.hasExitTime = false;
            lTransition_39824.hasFixedDuration = true;
            lTransition_39824.exitTime = 0.9050633f;
            lTransition_39824.duration = 0.25f;
            lTransition_39824.offset = 0f;
            lTransition_39824.mute = false;
            lTransition_39824.solo = false;
            lTransition_39824.canTransitionToSelf = true;
            lTransition_39824.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            for (int i = lTransition_39824.conditions.Length - 1; i >= 0; i--) { lTransition_39824.RemoveCondition(lTransition_39824.conditions[i]); }
            lTransition_39824.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 32143f, "L" + rLayerIndex + "MotionPhase");
            lTransition_39824.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 0f, "L" + rLayerIndex + "MotionParameter");

            UnityEditor.Animations.AnimatorStateTransition lTransition_39826 = MotionControllerMotion.EditorFindTransition(lState_39642, lState_39614, 0);
            if (lTransition_39826 == null) { lTransition_39826 = lState_39642.AddTransition(lState_39614); }
            lTransition_39826.isExit = false;
            lTransition_39826.hasExitTime = false;
            lTransition_39826.hasFixedDuration = true;
            lTransition_39826.exitTime = 0.9050633f;
            lTransition_39826.duration = 0.25f;
            lTransition_39826.offset = 0f;
            lTransition_39826.mute = false;
            lTransition_39826.solo = false;
            lTransition_39826.canTransitionToSelf = true;
            lTransition_39826.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            for (int i = lTransition_39826.conditions.Length - 1; i >= 0; i--) { lTransition_39826.RemoveCondition(lTransition_39826.conditions[i]); }
            lTransition_39826.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 32143f, "L" + rLayerIndex + "MotionPhase");
            lTransition_39826.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 1f, "L" + rLayerIndex + "MotionParameter");

            UnityEditor.Animations.AnimatorStateTransition lTransition_39828 = MotionControllerMotion.EditorFindTransition(lState_39644, lState_39612, 0);
            if (lTransition_39828 == null) { lTransition_39828 = lState_39644.AddTransition(lState_39612); }
            lTransition_39828.isExit = false;
            lTransition_39828.hasExitTime = true;
            lTransition_39828.hasFixedDuration = true;
            lTransition_39828.exitTime = 0.9050633f;
            lTransition_39828.duration = 0.25f;
            lTransition_39828.offset = 0f;
            lTransition_39828.mute = false;
            lTransition_39828.solo = false;
            lTransition_39828.canTransitionToSelf = true;
            lTransition_39828.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            for (int i = lTransition_39828.conditions.Length - 1; i >= 0; i--) { lTransition_39828.RemoveCondition(lTransition_39828.conditions[i]); }
            lTransition_39828.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 0f, "L" + rLayerIndex + "MotionParameter");

            UnityEditor.Animations.AnimatorStateTransition lTransition_39830 = MotionControllerMotion.EditorFindTransition(lState_39644, lState_39614, 0);
            if (lTransition_39830 == null) { lTransition_39830 = lState_39644.AddTransition(lState_39614); }
            lTransition_39830.isExit = false;
            lTransition_39830.hasExitTime = true;
            lTransition_39830.hasFixedDuration = true;
            lTransition_39830.exitTime = 0.9050633f;
            lTransition_39830.duration = 0.25f;
            lTransition_39830.offset = 0f;
            lTransition_39830.mute = false;
            lTransition_39830.solo = false;
            lTransition_39830.canTransitionToSelf = true;
            lTransition_39830.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            for (int i = lTransition_39830.conditions.Length - 1; i >= 0; i--) { lTransition_39830.RemoveCondition(lTransition_39830.conditions[i]); }
            lTransition_39830.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 1f, "L" + rLayerIndex + "MotionParameter");

            UnityEditor.Animations.AnimatorStateTransition lTransition_39832 = MotionControllerMotion.EditorFindTransition(lState_38488, lState_39612, 0);
            if (lTransition_39832 == null) { lTransition_39832 = lState_38488.AddTransition(lState_39612); }
            lTransition_39832.isExit = false;
            lTransition_39832.hasExitTime = true;
            lTransition_39832.hasFixedDuration = true;
            lTransition_39832.exitTime = 0.9418604f;
            lTransition_39832.duration = 0.25f;
            lTransition_39832.offset = 0f;
            lTransition_39832.mute = false;
            lTransition_39832.solo = false;
            lTransition_39832.canTransitionToSelf = true;
            lTransition_39832.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            for (int i = lTransition_39832.conditions.Length - 1; i >= 0; i--) { lTransition_39832.RemoveCondition(lTransition_39832.conditions[i]); }
            lTransition_39832.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 0f, "L" + rLayerIndex + "MotionParameter");

            UnityEditor.Animations.AnimatorStateTransition lTransition_39834 = MotionControllerMotion.EditorFindTransition(lState_38488, lState_39614, 0);
            if (lTransition_39834 == null) { lTransition_39834 = lState_38488.AddTransition(lState_39614); }
            lTransition_39834.isExit = false;
            lTransition_39834.hasExitTime = true;
            lTransition_39834.hasFixedDuration = true;
            lTransition_39834.exitTime = 0.9418604f;
            lTransition_39834.duration = 0.25f;
            lTransition_39834.offset = 0f;
            lTransition_39834.mute = false;
            lTransition_39834.solo = false;
            lTransition_39834.canTransitionToSelf = true;
            lTransition_39834.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            for (int i = lTransition_39834.conditions.Length - 1; i >= 0; i--) { lTransition_39834.RemoveCondition(lTransition_39834.conditions[i]); }
            lTransition_39834.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 1f, "L" + rLayerIndex + "MotionParameter");

            UnityEditor.Animations.AnimatorStateTransition lTransition_39836 = MotionControllerMotion.EditorFindTransition(lState_38490, lState_39646, 0);
            if (lTransition_39836 == null) { lTransition_39836 = lState_38490.AddTransition(lState_39646); }
            lTransition_39836.isExit = false;
            lTransition_39836.hasExitTime = true;
            lTransition_39836.hasFixedDuration = true;
            lTransition_39836.exitTime = 0.9418604f;
            lTransition_39836.duration = 0.25f;
            lTransition_39836.offset = 0f;
            lTransition_39836.mute = false;
            lTransition_39836.solo = false;
            lTransition_39836.canTransitionToSelf = true;
            lTransition_39836.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            for (int i = lTransition_39836.conditions.Length - 1; i >= 0; i--) { lTransition_39836.RemoveCondition(lTransition_39836.conditions[i]); }

            UnityEditor.Animations.AnimatorStateTransition lTransition_39838 = MotionControllerMotion.EditorFindTransition(lState_39646, lState_39648, 0);
            if (lTransition_39838 == null) { lTransition_39838 = lState_39646.AddTransition(lState_39648); }
            lTransition_39838.isExit = false;
            lTransition_39838.hasExitTime = true;
            lTransition_39838.hasFixedDuration = true;
            lTransition_39838.exitTime = 0.9418604f;
            lTransition_39838.duration = 0.25f;
            lTransition_39838.offset = 0f;
            lTransition_39838.mute = false;
            lTransition_39838.solo = false;
            lTransition_39838.canTransitionToSelf = true;
            lTransition_39838.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            for (int i = lTransition_39838.conditions.Length - 1; i >= 0; i--) { lTransition_39838.RemoveCondition(lTransition_39838.conditions[i]); }
            lTransition_39838.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 32144f, "L" + rLayerIndex + "MotionPhase");

            UnityEditor.Animations.AnimatorStateTransition lTransition_39840 = MotionControllerMotion.EditorFindTransition(lState_39646, lState_39612, 0);
            if (lTransition_39840 == null) { lTransition_39840 = lState_39646.AddTransition(lState_39612); }
            lTransition_39840.isExit = false;
            lTransition_39840.hasExitTime = false;
            lTransition_39840.hasFixedDuration = true;
            lTransition_39840.exitTime = 0.9418604f;
            lTransition_39840.duration = 0.25f;
            lTransition_39840.offset = 0f;
            lTransition_39840.mute = false;
            lTransition_39840.solo = false;
            lTransition_39840.canTransitionToSelf = true;
            lTransition_39840.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            for (int i = lTransition_39840.conditions.Length - 1; i >= 0; i--) { lTransition_39840.RemoveCondition(lTransition_39840.conditions[i]); }
            lTransition_39840.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 32143f, "L" + rLayerIndex + "MotionPhase");
            lTransition_39840.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 0f, "L" + rLayerIndex + "MotionParameter");

            UnityEditor.Animations.AnimatorStateTransition lTransition_39842 = MotionControllerMotion.EditorFindTransition(lState_39646, lState_39614, 0);
            if (lTransition_39842 == null) { lTransition_39842 = lState_39646.AddTransition(lState_39614); }
            lTransition_39842.isExit = false;
            lTransition_39842.hasExitTime = false;
            lTransition_39842.hasFixedDuration = true;
            lTransition_39842.exitTime = 0.9418604f;
            lTransition_39842.duration = 0.25f;
            lTransition_39842.offset = 0f;
            lTransition_39842.mute = false;
            lTransition_39842.solo = false;
            lTransition_39842.canTransitionToSelf = true;
            lTransition_39842.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            for (int i = lTransition_39842.conditions.Length - 1; i >= 0; i--) { lTransition_39842.RemoveCondition(lTransition_39842.conditions[i]); }
            lTransition_39842.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 32143f, "L" + rLayerIndex + "MotionPhase");
            lTransition_39842.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 1f, "L" + rLayerIndex + "MotionParameter");

            UnityEditor.Animations.AnimatorStateTransition lTransition_39844 = MotionControllerMotion.EditorFindTransition(lState_39648, lState_39612, 0);
            if (lTransition_39844 == null) { lTransition_39844 = lState_39648.AddTransition(lState_39612); }
            lTransition_39844.isExit = false;
            lTransition_39844.hasExitTime = true;
            lTransition_39844.hasFixedDuration = true;
            lTransition_39844.exitTime = 0.9418604f;
            lTransition_39844.duration = 0.25f;
            lTransition_39844.offset = 0f;
            lTransition_39844.mute = false;
            lTransition_39844.solo = false;
            lTransition_39844.canTransitionToSelf = true;
            lTransition_39844.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            for (int i = lTransition_39844.conditions.Length - 1; i >= 0; i--) { lTransition_39844.RemoveCondition(lTransition_39844.conditions[i]); }
            lTransition_39844.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 0f, "L" + rLayerIndex + "MotionParameter");

            UnityEditor.Animations.AnimatorStateTransition lTransition_39846 = MotionControllerMotion.EditorFindTransition(lState_39648, lState_39614, 0);
            if (lTransition_39846 == null) { lTransition_39846 = lState_39648.AddTransition(lState_39614); }
            lTransition_39846.isExit = false;
            lTransition_39846.hasExitTime = true;
            lTransition_39846.hasFixedDuration = true;
            lTransition_39846.exitTime = 0.9418604f;
            lTransition_39846.duration = 0.25f;
            lTransition_39846.offset = 0f;
            lTransition_39846.mute = false;
            lTransition_39846.solo = false;
            lTransition_39846.canTransitionToSelf = true;
            lTransition_39846.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            for (int i = lTransition_39846.conditions.Length - 1; i >= 0; i--) { lTransition_39846.RemoveCondition(lTransition_39846.conditions[i]); }
            lTransition_39846.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 1f, "L" + rLayerIndex + "MotionParameter");

            UnityEditor.Animations.AnimatorStateTransition lTransition_39848 = MotionControllerMotion.EditorFindTransition(lState_38492, lState_39612, 0);
            if (lTransition_39848 == null) { lTransition_39848 = lState_38492.AddTransition(lState_39612); }
            lTransition_39848.isExit = false;
            lTransition_39848.hasExitTime = true;
            lTransition_39848.hasFixedDuration = true;
            lTransition_39848.exitTime = 0.9246231f;
            lTransition_39848.duration = 0.25f;
            lTransition_39848.offset = 0f;
            lTransition_39848.mute = false;
            lTransition_39848.solo = false;
            lTransition_39848.canTransitionToSelf = true;
            lTransition_39848.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            for (int i = lTransition_39848.conditions.Length - 1; i >= 0; i--) { lTransition_39848.RemoveCondition(lTransition_39848.conditions[i]); }
            lTransition_39848.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 0f, "L" + rLayerIndex + "MotionParameter");

            UnityEditor.Animations.AnimatorStateTransition lTransition_39850 = MotionControllerMotion.EditorFindTransition(lState_38492, lState_39614, 0);
            if (lTransition_39850 == null) { lTransition_39850 = lState_38492.AddTransition(lState_39614); }
            lTransition_39850.isExit = false;
            lTransition_39850.hasExitTime = true;
            lTransition_39850.hasFixedDuration = true;
            lTransition_39850.exitTime = 0.9246231f;
            lTransition_39850.duration = 0.25f;
            lTransition_39850.offset = 0f;
            lTransition_39850.mute = false;
            lTransition_39850.solo = false;
            lTransition_39850.canTransitionToSelf = true;
            lTransition_39850.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            for (int i = lTransition_39850.conditions.Length - 1; i >= 0; i--) { lTransition_39850.RemoveCondition(lTransition_39850.conditions[i]); }
            lTransition_39850.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 1f, "L" + rLayerIndex + "MotionParameter");

            UnityEditor.Animations.AnimatorStateTransition lTransition_39852 = MotionControllerMotion.EditorFindTransition(lState_38494, lState_39650, 0);
            if (lTransition_39852 == null) { lTransition_39852 = lState_38494.AddTransition(lState_39650); }
            lTransition_39852.isExit = false;
            lTransition_39852.hasExitTime = true;
            lTransition_39852.hasFixedDuration = true;
            lTransition_39852.exitTime = 0.9246231f;
            lTransition_39852.duration = 0.25f;
            lTransition_39852.offset = 0f;
            lTransition_39852.mute = false;
            lTransition_39852.solo = false;
            lTransition_39852.canTransitionToSelf = true;
            lTransition_39852.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            for (int i = lTransition_39852.conditions.Length - 1; i >= 0; i--) { lTransition_39852.RemoveCondition(lTransition_39852.conditions[i]); }

            UnityEditor.Animations.AnimatorStateTransition lTransition_39854 = MotionControllerMotion.EditorFindTransition(lState_39650, lState_39652, 0);
            if (lTransition_39854 == null) { lTransition_39854 = lState_39650.AddTransition(lState_39652); }
            lTransition_39854.isExit = false;
            lTransition_39854.hasExitTime = true;
            lTransition_39854.hasFixedDuration = true;
            lTransition_39854.exitTime = 0.9246231f;
            lTransition_39854.duration = 0.25f;
            lTransition_39854.offset = 0f;
            lTransition_39854.mute = false;
            lTransition_39854.solo = false;
            lTransition_39854.canTransitionToSelf = true;
            lTransition_39854.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            for (int i = lTransition_39854.conditions.Length - 1; i >= 0; i--) { lTransition_39854.RemoveCondition(lTransition_39854.conditions[i]); }
            lTransition_39854.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 32144f, "L" + rLayerIndex + "MotionPhase");

            UnityEditor.Animations.AnimatorStateTransition lTransition_39856 = MotionControllerMotion.EditorFindTransition(lState_39650, lState_39612, 0);
            if (lTransition_39856 == null) { lTransition_39856 = lState_39650.AddTransition(lState_39612); }
            lTransition_39856.isExit = false;
            lTransition_39856.hasExitTime = false;
            lTransition_39856.hasFixedDuration = true;
            lTransition_39856.exitTime = 0.9246231f;
            lTransition_39856.duration = 0.25f;
            lTransition_39856.offset = 0f;
            lTransition_39856.mute = false;
            lTransition_39856.solo = false;
            lTransition_39856.canTransitionToSelf = true;
            lTransition_39856.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            for (int i = lTransition_39856.conditions.Length - 1; i >= 0; i--) { lTransition_39856.RemoveCondition(lTransition_39856.conditions[i]); }
            lTransition_39856.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 32143f, "L" + rLayerIndex + "MotionPhase");
            lTransition_39856.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 0f, "L" + rLayerIndex + "MotionParameter");

            UnityEditor.Animations.AnimatorStateTransition lTransition_39858 = MotionControllerMotion.EditorFindTransition(lState_39650, lState_39614, 0);
            if (lTransition_39858 == null) { lTransition_39858 = lState_39650.AddTransition(lState_39614); }
            lTransition_39858.isExit = false;
            lTransition_39858.hasExitTime = false;
            lTransition_39858.hasFixedDuration = true;
            lTransition_39858.exitTime = 0.9246231f;
            lTransition_39858.duration = 0.25f;
            lTransition_39858.offset = 0f;
            lTransition_39858.mute = false;
            lTransition_39858.solo = false;
            lTransition_39858.canTransitionToSelf = true;
            lTransition_39858.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            for (int i = lTransition_39858.conditions.Length - 1; i >= 0; i--) { lTransition_39858.RemoveCondition(lTransition_39858.conditions[i]); }
            lTransition_39858.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 32143f, "L" + rLayerIndex + "MotionPhase");
            lTransition_39858.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 1f, "L" + rLayerIndex + "MotionParameter");

            UnityEditor.Animations.AnimatorStateTransition lTransition_39860 = MotionControllerMotion.EditorFindTransition(lState_39652, lState_39612, 0);
            if (lTransition_39860 == null) { lTransition_39860 = lState_39652.AddTransition(lState_39612); }
            lTransition_39860.isExit = false;
            lTransition_39860.hasExitTime = true;
            lTransition_39860.hasFixedDuration = true;
            lTransition_39860.exitTime = 0.9246231f;
            lTransition_39860.duration = 0.25f;
            lTransition_39860.offset = 0f;
            lTransition_39860.mute = false;
            lTransition_39860.solo = false;
            lTransition_39860.canTransitionToSelf = true;
            lTransition_39860.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            for (int i = lTransition_39860.conditions.Length - 1; i >= 0; i--) { lTransition_39860.RemoveCondition(lTransition_39860.conditions[i]); }
            lTransition_39860.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 0f, "L" + rLayerIndex + "MotionParameter");

            UnityEditor.Animations.AnimatorStateTransition lTransition_39862 = MotionControllerMotion.EditorFindTransition(lState_39652, lState_39614, 0);
            if (lTransition_39862 == null) { lTransition_39862 = lState_39652.AddTransition(lState_39614); }
            lTransition_39862.isExit = false;
            lTransition_39862.hasExitTime = true;
            lTransition_39862.hasFixedDuration = true;
            lTransition_39862.exitTime = 0.9246231f;
            lTransition_39862.duration = 0.25f;
            lTransition_39862.offset = 0f;
            lTransition_39862.mute = false;
            lTransition_39862.solo = false;
            lTransition_39862.canTransitionToSelf = true;
            lTransition_39862.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            for (int i = lTransition_39862.conditions.Length - 1; i >= 0; i--) { lTransition_39862.RemoveCondition(lTransition_39862.conditions[i]); }
            lTransition_39862.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 1f, "L" + rLayerIndex + "MotionParameter");

            UnityEditor.Animations.AnimatorStateTransition lTransition_39864 = MotionControllerMotion.EditorFindTransition(lState_38496, lState_39612, 0);
            if (lTransition_39864 == null) { lTransition_39864 = lState_38496.AddTransition(lState_39612); }
            lTransition_39864.isExit = false;
            lTransition_39864.hasExitTime = true;
            lTransition_39864.hasFixedDuration = true;
            lTransition_39864.exitTime = 0.9292453f;
            lTransition_39864.duration = 0.25f;
            lTransition_39864.offset = 0f;
            lTransition_39864.mute = false;
            lTransition_39864.solo = false;
            lTransition_39864.canTransitionToSelf = true;
            lTransition_39864.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            for (int i = lTransition_39864.conditions.Length - 1; i >= 0; i--) { lTransition_39864.RemoveCondition(lTransition_39864.conditions[i]); }
            lTransition_39864.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 0f, "L" + rLayerIndex + "MotionParameter");

            UnityEditor.Animations.AnimatorStateTransition lTransition_39866 = MotionControllerMotion.EditorFindTransition(lState_38496, lState_39614, 0);
            if (lTransition_39866 == null) { lTransition_39866 = lState_38496.AddTransition(lState_39614); }
            lTransition_39866.isExit = false;
            lTransition_39866.hasExitTime = true;
            lTransition_39866.hasFixedDuration = true;
            lTransition_39866.exitTime = 0.9292453f;
            lTransition_39866.duration = 0.25f;
            lTransition_39866.offset = 0f;
            lTransition_39866.mute = false;
            lTransition_39866.solo = false;
            lTransition_39866.canTransitionToSelf = true;
            lTransition_39866.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            for (int i = lTransition_39866.conditions.Length - 1; i >= 0; i--) { lTransition_39866.RemoveCondition(lTransition_39866.conditions[i]); }
            lTransition_39866.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 1f, "L" + rLayerIndex + "MotionParameter");

            UnityEditor.Animations.AnimatorStateTransition lTransition_39868 = MotionControllerMotion.EditorFindTransition(lState_38498, lState_39654, 0);
            if (lTransition_39868 == null) { lTransition_39868 = lState_38498.AddTransition(lState_39654); }
            lTransition_39868.isExit = false;
            lTransition_39868.hasExitTime = true;
            lTransition_39868.hasFixedDuration = true;
            lTransition_39868.exitTime = 0.9292453f;
            lTransition_39868.duration = 0.25f;
            lTransition_39868.offset = 0f;
            lTransition_39868.mute = false;
            lTransition_39868.solo = false;
            lTransition_39868.canTransitionToSelf = true;
            lTransition_39868.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            for (int i = lTransition_39868.conditions.Length - 1; i >= 0; i--) { lTransition_39868.RemoveCondition(lTransition_39868.conditions[i]); }

            UnityEditor.Animations.AnimatorStateTransition lTransition_39870 = MotionControllerMotion.EditorFindTransition(lState_39654, lState_39656, 0);
            if (lTransition_39870 == null) { lTransition_39870 = lState_39654.AddTransition(lState_39656); }
            lTransition_39870.isExit = false;
            lTransition_39870.hasExitTime = true;
            lTransition_39870.hasFixedDuration = true;
            lTransition_39870.exitTime = 0.9292453f;
            lTransition_39870.duration = 0.25f;
            lTransition_39870.offset = 0f;
            lTransition_39870.mute = false;
            lTransition_39870.solo = false;
            lTransition_39870.canTransitionToSelf = true;
            lTransition_39870.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            for (int i = lTransition_39870.conditions.Length - 1; i >= 0; i--) { lTransition_39870.RemoveCondition(lTransition_39870.conditions[i]); }
            lTransition_39870.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 32144f, "L" + rLayerIndex + "MotionPhase");

            UnityEditor.Animations.AnimatorStateTransition lTransition_39872 = MotionControllerMotion.EditorFindTransition(lState_39654, lState_39612, 0);
            if (lTransition_39872 == null) { lTransition_39872 = lState_39654.AddTransition(lState_39612); }
            lTransition_39872.isExit = false;
            lTransition_39872.hasExitTime = false;
            lTransition_39872.hasFixedDuration = true;
            lTransition_39872.exitTime = 0.9292453f;
            lTransition_39872.duration = 0.25f;
            lTransition_39872.offset = 0f;
            lTransition_39872.mute = false;
            lTransition_39872.solo = false;
            lTransition_39872.canTransitionToSelf = true;
            lTransition_39872.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            for (int i = lTransition_39872.conditions.Length - 1; i >= 0; i--) { lTransition_39872.RemoveCondition(lTransition_39872.conditions[i]); }
            lTransition_39872.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 32143f, "L" + rLayerIndex + "MotionPhase");
            lTransition_39872.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 0f, "L" + rLayerIndex + "MotionParameter");

            UnityEditor.Animations.AnimatorStateTransition lTransition_39874 = MotionControllerMotion.EditorFindTransition(lState_39654, lState_39614, 0);
            if (lTransition_39874 == null) { lTransition_39874 = lState_39654.AddTransition(lState_39614); }
            lTransition_39874.isExit = false;
            lTransition_39874.hasExitTime = false;
            lTransition_39874.hasFixedDuration = true;
            lTransition_39874.exitTime = 0.9292453f;
            lTransition_39874.duration = 0.25f;
            lTransition_39874.offset = 0f;
            lTransition_39874.mute = false;
            lTransition_39874.solo = false;
            lTransition_39874.canTransitionToSelf = true;
            lTransition_39874.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            for (int i = lTransition_39874.conditions.Length - 1; i >= 0; i--) { lTransition_39874.RemoveCondition(lTransition_39874.conditions[i]); }
            lTransition_39874.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 32143f, "L" + rLayerIndex + "MotionPhase");
            lTransition_39874.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 1f, "L" + rLayerIndex + "MotionParameter");

            UnityEditor.Animations.AnimatorStateTransition lTransition_39876 = MotionControllerMotion.EditorFindTransition(lState_39656, lState_39612, 0);
            if (lTransition_39876 == null) { lTransition_39876 = lState_39656.AddTransition(lState_39612); }
            lTransition_39876.isExit = false;
            lTransition_39876.hasExitTime = true;
            lTransition_39876.hasFixedDuration = true;
            lTransition_39876.exitTime = 0.9292453f;
            lTransition_39876.duration = 0.25f;
            lTransition_39876.offset = 0f;
            lTransition_39876.mute = false;
            lTransition_39876.solo = false;
            lTransition_39876.canTransitionToSelf = true;
            lTransition_39876.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            for (int i = lTransition_39876.conditions.Length - 1; i >= 0; i--) { lTransition_39876.RemoveCondition(lTransition_39876.conditions[i]); }
            lTransition_39876.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 0f, "L" + rLayerIndex + "MotionParameter");

            UnityEditor.Animations.AnimatorStateTransition lTransition_39878 = MotionControllerMotion.EditorFindTransition(lState_39656, lState_39614, 0);
            if (lTransition_39878 == null) { lTransition_39878 = lState_39656.AddTransition(lState_39614); }
            lTransition_39878.isExit = false;
            lTransition_39878.hasExitTime = true;
            lTransition_39878.hasFixedDuration = true;
            lTransition_39878.exitTime = 0.9292453f;
            lTransition_39878.duration = 0.25f;
            lTransition_39878.offset = 0f;
            lTransition_39878.mute = false;
            lTransition_39878.solo = false;
            lTransition_39878.canTransitionToSelf = true;
            lTransition_39878.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            for (int i = lTransition_39878.conditions.Length - 1; i >= 0; i--) { lTransition_39878.RemoveCondition(lTransition_39878.conditions[i]); }
            lTransition_39878.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 1f, "L" + rLayerIndex + "MotionParameter");

            UnityEditor.Animations.AnimatorStateTransition lTransition_39880 = MotionControllerMotion.EditorFindTransition(lState_38466, lState_39612, 0);
            if (lTransition_39880 == null) { lTransition_39880 = lState_38466.AddTransition(lState_39612); }
            lTransition_39880.isExit = false;
            lTransition_39880.hasExitTime = true;
            lTransition_39880.hasFixedDuration = true;
            lTransition_39880.exitTime = 0.8863636f;
            lTransition_39880.duration = 0.25f;
            lTransition_39880.offset = 0f;
            lTransition_39880.mute = false;
            lTransition_39880.solo = false;
            lTransition_39880.canTransitionToSelf = true;
            lTransition_39880.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            for (int i = lTransition_39880.conditions.Length - 1; i >= 0; i--) { lTransition_39880.RemoveCondition(lTransition_39880.conditions[i]); }
            lTransition_39880.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 0f, "L" + rLayerIndex + "MotionParameter");

            UnityEditor.Animations.AnimatorStateTransition lTransition_39882 = MotionControllerMotion.EditorFindTransition(lState_38466, lState_39614, 0);
            if (lTransition_39882 == null) { lTransition_39882 = lState_38466.AddTransition(lState_39614); }
            lTransition_39882.isExit = false;
            lTransition_39882.hasExitTime = true;
            lTransition_39882.hasFixedDuration = true;
            lTransition_39882.exitTime = 0.8863636f;
            lTransition_39882.duration = 0.25f;
            lTransition_39882.offset = 0f;
            lTransition_39882.mute = false;
            lTransition_39882.solo = false;
            lTransition_39882.canTransitionToSelf = true;
            lTransition_39882.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            for (int i = lTransition_39882.conditions.Length - 1; i >= 0; i--) { lTransition_39882.RemoveCondition(lTransition_39882.conditions[i]); }
            lTransition_39882.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 1f, "L" + rLayerIndex + "MotionParameter");

            UnityEditor.Animations.AnimatorStateTransition lTransition_39884 = MotionControllerMotion.EditorFindTransition(lState_38468, lState_39658, 0);
            if (lTransition_39884 == null) { lTransition_39884 = lState_38468.AddTransition(lState_39658); }
            lTransition_39884.isExit = false;
            lTransition_39884.hasExitTime = true;
            lTransition_39884.hasFixedDuration = true;
            lTransition_39884.exitTime = 0.8863636f;
            lTransition_39884.duration = 0.25f;
            lTransition_39884.offset = 0f;
            lTransition_39884.mute = false;
            lTransition_39884.solo = false;
            lTransition_39884.canTransitionToSelf = true;
            lTransition_39884.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            for (int i = lTransition_39884.conditions.Length - 1; i >= 0; i--) { lTransition_39884.RemoveCondition(lTransition_39884.conditions[i]); }

            UnityEditor.Animations.AnimatorStateTransition lTransition_39886 = MotionControllerMotion.EditorFindTransition(lState_39658, lState_39660, 0);
            if (lTransition_39886 == null) { lTransition_39886 = lState_39658.AddTransition(lState_39660); }
            lTransition_39886.isExit = false;
            lTransition_39886.hasExitTime = true;
            lTransition_39886.hasFixedDuration = true;
            lTransition_39886.exitTime = 0.8863636f;
            lTransition_39886.duration = 0.25f;
            lTransition_39886.offset = 0f;
            lTransition_39886.mute = false;
            lTransition_39886.solo = false;
            lTransition_39886.canTransitionToSelf = true;
            lTransition_39886.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            for (int i = lTransition_39886.conditions.Length - 1; i >= 0; i--) { lTransition_39886.RemoveCondition(lTransition_39886.conditions[i]); }
            lTransition_39886.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 32144f, "L" + rLayerIndex + "MotionPhase");

            UnityEditor.Animations.AnimatorStateTransition lTransition_39888 = MotionControllerMotion.EditorFindTransition(lState_39658, lState_39612, 0);
            if (lTransition_39888 == null) { lTransition_39888 = lState_39658.AddTransition(lState_39612); }
            lTransition_39888.isExit = false;
            lTransition_39888.hasExitTime = false;
            lTransition_39888.hasFixedDuration = true;
            lTransition_39888.exitTime = 0.8863636f;
            lTransition_39888.duration = 0.25f;
            lTransition_39888.offset = 0f;
            lTransition_39888.mute = false;
            lTransition_39888.solo = false;
            lTransition_39888.canTransitionToSelf = true;
            lTransition_39888.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            for (int i = lTransition_39888.conditions.Length - 1; i >= 0; i--) { lTransition_39888.RemoveCondition(lTransition_39888.conditions[i]); }
            lTransition_39888.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 32143f, "L" + rLayerIndex + "MotionPhase");
            lTransition_39888.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 0f, "L" + rLayerIndex + "MotionPhase");

            UnityEditor.Animations.AnimatorStateTransition lTransition_39890 = MotionControllerMotion.EditorFindTransition(lState_39658, lState_39614, 0);
            if (lTransition_39890 == null) { lTransition_39890 = lState_39658.AddTransition(lState_39614); }
            lTransition_39890.isExit = false;
            lTransition_39890.hasExitTime = false;
            lTransition_39890.hasFixedDuration = true;
            lTransition_39890.exitTime = 0.8863636f;
            lTransition_39890.duration = 0.25f;
            lTransition_39890.offset = 0f;
            lTransition_39890.mute = false;
            lTransition_39890.solo = false;
            lTransition_39890.canTransitionToSelf = true;
            lTransition_39890.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            for (int i = lTransition_39890.conditions.Length - 1; i >= 0; i--) { lTransition_39890.RemoveCondition(lTransition_39890.conditions[i]); }
            lTransition_39890.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 32143f, "L" + rLayerIndex + "MotionPhase");
            lTransition_39890.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 1f, "L" + rLayerIndex + "MotionPhase");

            UnityEditor.Animations.AnimatorStateTransition lTransition_39892 = MotionControllerMotion.EditorFindTransition(lState_39660, lState_39612, 0);
            if (lTransition_39892 == null) { lTransition_39892 = lState_39660.AddTransition(lState_39612); }
            lTransition_39892.isExit = false;
            lTransition_39892.hasExitTime = true;
            lTransition_39892.hasFixedDuration = true;
            lTransition_39892.exitTime = 0.8863636f;
            lTransition_39892.duration = 0.25f;
            lTransition_39892.offset = 0f;
            lTransition_39892.mute = false;
            lTransition_39892.solo = false;
            lTransition_39892.canTransitionToSelf = true;
            lTransition_39892.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            for (int i = lTransition_39892.conditions.Length - 1; i >= 0; i--) { lTransition_39892.RemoveCondition(lTransition_39892.conditions[i]); }
            lTransition_39892.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 0f, "L" + rLayerIndex + "MotionParameter");

            UnityEditor.Animations.AnimatorStateTransition lTransition_39894 = MotionControllerMotion.EditorFindTransition(lState_39660, lState_39614, 0);
            if (lTransition_39894 == null) { lTransition_39894 = lState_39660.AddTransition(lState_39614); }
            lTransition_39894.isExit = false;
            lTransition_39894.hasExitTime = true;
            lTransition_39894.hasFixedDuration = true;
            lTransition_39894.exitTime = 0.8863636f;
            lTransition_39894.duration = 0.25f;
            lTransition_39894.offset = 0f;
            lTransition_39894.mute = false;
            lTransition_39894.solo = false;
            lTransition_39894.canTransitionToSelf = true;
            lTransition_39894.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            for (int i = lTransition_39894.conditions.Length - 1; i >= 0; i--) { lTransition_39894.RemoveCondition(lTransition_39894.conditions[i]); }
            lTransition_39894.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 1f, "L" + rLayerIndex + "MotionParameter");

            UnityEditor.Animations.AnimatorStateTransition lTransition_39896 = MotionControllerMotion.EditorFindTransition(lState_38470, lState_39612, 0);
            if (lTransition_39896 == null) { lTransition_39896 = lState_38470.AddTransition(lState_39612); }
            lTransition_39896.isExit = false;
            lTransition_39896.hasExitTime = true;
            lTransition_39896.hasFixedDuration = true;
            lTransition_39896.exitTime = 0.890511f;
            lTransition_39896.duration = 0.25f;
            lTransition_39896.offset = 0f;
            lTransition_39896.mute = false;
            lTransition_39896.solo = false;
            lTransition_39896.canTransitionToSelf = true;
            lTransition_39896.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            for (int i = lTransition_39896.conditions.Length - 1; i >= 0; i--) { lTransition_39896.RemoveCondition(lTransition_39896.conditions[i]); }
            lTransition_39896.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 0f, "L" + rLayerIndex + "MotionParameter");

            UnityEditor.Animations.AnimatorStateTransition lTransition_39898 = MotionControllerMotion.EditorFindTransition(lState_38470, lState_39614, 0);
            if (lTransition_39898 == null) { lTransition_39898 = lState_38470.AddTransition(lState_39614); }
            lTransition_39898.isExit = false;
            lTransition_39898.hasExitTime = true;
            lTransition_39898.hasFixedDuration = true;
            lTransition_39898.exitTime = 0.890511f;
            lTransition_39898.duration = 0.25f;
            lTransition_39898.offset = 0f;
            lTransition_39898.mute = false;
            lTransition_39898.solo = false;
            lTransition_39898.canTransitionToSelf = true;
            lTransition_39898.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            for (int i = lTransition_39898.conditions.Length - 1; i >= 0; i--) { lTransition_39898.RemoveCondition(lTransition_39898.conditions[i]); }
            lTransition_39898.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 1f, "L" + rLayerIndex + "MotionParameter");

            UnityEditor.Animations.AnimatorStateTransition lTransition_39900 = MotionControllerMotion.EditorFindTransition(lState_38472, lState_39662, 0);
            if (lTransition_39900 == null) { lTransition_39900 = lState_38472.AddTransition(lState_39662); }
            lTransition_39900.isExit = false;
            lTransition_39900.hasExitTime = true;
            lTransition_39900.hasFixedDuration = true;
            lTransition_39900.exitTime = 0.890511f;
            lTransition_39900.duration = 0.25f;
            lTransition_39900.offset = 0f;
            lTransition_39900.mute = false;
            lTransition_39900.solo = false;
            lTransition_39900.canTransitionToSelf = true;
            lTransition_39900.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            for (int i = lTransition_39900.conditions.Length - 1; i >= 0; i--) { lTransition_39900.RemoveCondition(lTransition_39900.conditions[i]); }

            UnityEditor.Animations.AnimatorStateTransition lTransition_39902 = MotionControllerMotion.EditorFindTransition(lState_39662, lState_39664, 0);
            if (lTransition_39902 == null) { lTransition_39902 = lState_39662.AddTransition(lState_39664); }
            lTransition_39902.isExit = false;
            lTransition_39902.hasExitTime = true;
            lTransition_39902.hasFixedDuration = true;
            lTransition_39902.exitTime = 0.890511f;
            lTransition_39902.duration = 0.25f;
            lTransition_39902.offset = 0f;
            lTransition_39902.mute = false;
            lTransition_39902.solo = false;
            lTransition_39902.canTransitionToSelf = true;
            lTransition_39902.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            for (int i = lTransition_39902.conditions.Length - 1; i >= 0; i--) { lTransition_39902.RemoveCondition(lTransition_39902.conditions[i]); }
            lTransition_39902.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 32144f, "L" + rLayerIndex + "MotionPhase");

            UnityEditor.Animations.AnimatorStateTransition lTransition_39904 = MotionControllerMotion.EditorFindTransition(lState_39662, lState_39612, 0);
            if (lTransition_39904 == null) { lTransition_39904 = lState_39662.AddTransition(lState_39612); }
            lTransition_39904.isExit = false;
            lTransition_39904.hasExitTime = true;
            lTransition_39904.hasFixedDuration = true;
            lTransition_39904.exitTime = 0.890511f;
            lTransition_39904.duration = 0.25f;
            lTransition_39904.offset = 0f;
            lTransition_39904.mute = false;
            lTransition_39904.solo = false;
            lTransition_39904.canTransitionToSelf = true;
            lTransition_39904.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            for (int i = lTransition_39904.conditions.Length - 1; i >= 0; i--) { lTransition_39904.RemoveCondition(lTransition_39904.conditions[i]); }
            lTransition_39904.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 32143f, "L" + rLayerIndex + "MotionPhase");
            lTransition_39904.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 0f, "L" + rLayerIndex + "MotionParameter");

            UnityEditor.Animations.AnimatorStateTransition lTransition_39906 = MotionControllerMotion.EditorFindTransition(lState_39662, lState_39614, 0);
            if (lTransition_39906 == null) { lTransition_39906 = lState_39662.AddTransition(lState_39614); }
            lTransition_39906.isExit = false;
            lTransition_39906.hasExitTime = true;
            lTransition_39906.hasFixedDuration = true;
            lTransition_39906.exitTime = 0.890511f;
            lTransition_39906.duration = 0.25f;
            lTransition_39906.offset = 0f;
            lTransition_39906.mute = false;
            lTransition_39906.solo = false;
            lTransition_39906.canTransitionToSelf = true;
            lTransition_39906.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            for (int i = lTransition_39906.conditions.Length - 1; i >= 0; i--) { lTransition_39906.RemoveCondition(lTransition_39906.conditions[i]); }
            lTransition_39906.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 32143f, "L" + rLayerIndex + "MotionPhase");
            lTransition_39906.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 1f, "L" + rLayerIndex + "MotionParameter");

            UnityEditor.Animations.AnimatorStateTransition lTransition_39908 = MotionControllerMotion.EditorFindTransition(lState_39664, lState_39612, 0);
            if (lTransition_39908 == null) { lTransition_39908 = lState_39664.AddTransition(lState_39612); }
            lTransition_39908.isExit = false;
            lTransition_39908.hasExitTime = true;
            lTransition_39908.hasFixedDuration = true;
            lTransition_39908.exitTime = 0.890511f;
            lTransition_39908.duration = 0.25f;
            lTransition_39908.offset = 0f;
            lTransition_39908.mute = false;
            lTransition_39908.solo = false;
            lTransition_39908.canTransitionToSelf = true;
            lTransition_39908.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            for (int i = lTransition_39908.conditions.Length - 1; i >= 0; i--) { lTransition_39908.RemoveCondition(lTransition_39908.conditions[i]); }
            lTransition_39908.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 0f, "L" + rLayerIndex + "MotionParameter");

            UnityEditor.Animations.AnimatorStateTransition lTransition_39910 = MotionControllerMotion.EditorFindTransition(lState_39664, lState_39614, 0);
            if (lTransition_39910 == null) { lTransition_39910 = lState_39664.AddTransition(lState_39614); }
            lTransition_39910.isExit = false;
            lTransition_39910.hasExitTime = true;
            lTransition_39910.hasFixedDuration = true;
            lTransition_39910.exitTime = 0.890511f;
            lTransition_39910.duration = 0.25f;
            lTransition_39910.offset = 0f;
            lTransition_39910.mute = false;
            lTransition_39910.solo = false;
            lTransition_39910.canTransitionToSelf = true;
            lTransition_39910.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            for (int i = lTransition_39910.conditions.Length - 1; i >= 0; i--) { lTransition_39910.RemoveCondition(lTransition_39910.conditions[i]); }
            lTransition_39910.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 1f, "L" + rLayerIndex + "MotionParameter");
        }

        /// <summary>
        /// New way to create sub-state machines without destroying what exists first.
        /// </summary>
        public static void ExtendBasicDamaged(MotionController rMotionController, int rLayerIndex)
        {
            UnityEditor.Animations.AnimatorController lController = null;

            Animator lAnimator = rMotionController.Animator;
            if (lAnimator == null) { lAnimator = rMotionController.gameObject.GetComponent<Animator>(); }
            if (lAnimator != null) { lController = lAnimator.runtimeAnimatorController as UnityEditor.Animations.AnimatorController; }
            if (lController == null) { return; }

            while (lController.layers.Length <= rLayerIndex)
            {
                UnityEditor.Animations.AnimatorControllerLayer lNewLayer = new UnityEditor.Animations.AnimatorControllerLayer();
                lNewLayer.name = "Layer " + (lController.layers.Length + 1);
                lNewLayer.stateMachine = new UnityEditor.Animations.AnimatorStateMachine();
                lController.AddLayer(lNewLayer);
            }

            UnityEditor.Animations.AnimatorControllerLayer lLayer = lController.layers[rLayerIndex];

            UnityEditor.Animations.AnimatorStateMachine lLayerStateMachine = lLayer.stateMachine;

            UnityEditor.Animations.AnimatorStateMachine lSSM_N181712 = MotionControllerMotion.EditorFindSSM(lLayerStateMachine, "BasicDamaged-SM");
            if (lSSM_N181712 == null) { lSSM_N181712 = lLayerStateMachine.AddStateMachine("BasicDamaged-SM", new Vector3(192, -960, 0)); }

            UnityEditor.Animations.AnimatorState lState_N256722 = MotionControllerMotion.EditorFindState(lSSM_N181712, "Unarmed Damaged 0");
            if (lState_N256722 == null) { lState_N256722 = lSSM_N181712.AddState("Unarmed Damaged 0", new Vector3(312, -24, 0)); }
            lState_N256722.speed = 3f;
            lState_N256722.mirror = false;
            lState_N256722.tag = "Exit";
            lState_N256722.motion = MotionControllerMotion.EditorFindAnimationClip("Assets/ootii/MotionController/Content/Animations/Humanoid/Utilities/Utilities_01.fbx", "Damaged");

            UnityEditor.Animations.AnimatorStateTransition lAnyTransition_N265182 = MotionControllerMotion.EditorFindAnyStateTransition(lLayerStateMachine, lState_N256722, 0);
            if (lAnyTransition_N265182 == null) { lAnyTransition_N265182 = lLayerStateMachine.AddAnyStateTransition(lState_N256722); }
            lAnyTransition_N265182.isExit = false;
            lAnyTransition_N265182.hasExitTime = false;
            lAnyTransition_N265182.hasFixedDuration = true;
            lAnyTransition_N265182.exitTime = 0.75f;
            lAnyTransition_N265182.duration = 0.1f;
            lAnyTransition_N265182.offset = 0.106185f;
            lAnyTransition_N265182.mute = false;
            lAnyTransition_N265182.solo = false;
            lAnyTransition_N265182.canTransitionToSelf = true;
            lAnyTransition_N265182.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            for (int i = lAnyTransition_N265182.conditions.Length - 1; i >= 0; i--) { lAnyTransition_N265182.RemoveCondition(lAnyTransition_N265182.conditions[i]); }
            lAnyTransition_N265182.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 3350f, "L" + rLayerIndex + "MotionPhase");
            lAnyTransition_N265182.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 0f, "L" + rLayerIndex + "MotionForm");
        }

        /// <summary>
        /// New way to create sub-state machines without destroying what exists first.
        /// </summary>
        public static void ExtendBasicDeath(MotionController rMotionController, int rLayerIndex)
        {
            UnityEditor.Animations.AnimatorController lController = null;

            Animator lAnimator = rMotionController.Animator;
            if (lAnimator == null) { lAnimator = rMotionController.gameObject.GetComponent<Animator>(); }
            if (lAnimator != null) { lController = lAnimator.runtimeAnimatorController as UnityEditor.Animations.AnimatorController; }
            if (lController == null) { return; }

            while (lController.layers.Length <= rLayerIndex)
            {
                UnityEditor.Animations.AnimatorControllerLayer lNewLayer = new UnityEditor.Animations.AnimatorControllerLayer();
                lNewLayer.name = "Layer " + (lController.layers.Length + 1);
                lNewLayer.stateMachine = new UnityEditor.Animations.AnimatorStateMachine();
                lController.AddLayer(lNewLayer);
            }

            UnityEditor.Animations.AnimatorControllerLayer lLayer = lController.layers[rLayerIndex];

            UnityEditor.Animations.AnimatorStateMachine lLayerStateMachine = lLayer.stateMachine;

            UnityEditor.Animations.AnimatorStateMachine lSSM_N237494 = MotionControllerMotion.EditorFindSSM(lLayerStateMachine, "BasicDeath-SM");
            if (lSSM_N237494 == null) { lSSM_N237494 = lLayerStateMachine.AddStateMachine("BasicDeath-SM", new Vector3(192, -912, 0)); }

            UnityEditor.Animations.AnimatorState lState_N247470 = MotionControllerMotion.EditorFindState(lSSM_N237494, "Unarmed Death 0");
            if (lState_N247470 == null) { lState_N247470 = lSSM_N237494.AddState("Unarmed Death 0", new Vector3(324, -72, 0)); }
            lState_N247470.speed = 1.5f;
            lState_N247470.mirror = false;
            lState_N247470.tag = "";
            lState_N247470.motion = MotionControllerMotion.EditorFindAnimationClip("Assets/ootii/MotionController/Content/Animations/Humanoid/Utilities/Utilities_01.fbx", "DeathBackward");

            UnityEditor.Animations.AnimatorState lState_N247472 = MotionControllerMotion.EditorFindState(lSSM_N237494, "Unarmed Death 180");
            if (lState_N247472 == null) { lState_N247472 = lSSM_N237494.AddState("Unarmed Death 180", new Vector3(324, -24, 0)); }
            lState_N247472.speed = 1.8f;
            lState_N247472.mirror = false;
            lState_N247472.tag = "";
            lState_N247472.motion = MotionControllerMotion.EditorFindAnimationClip("Assets/ootii/MotionController/Content/Animations/Humanoid/Utilities/Utilities_01.fbx", "DeathForward");

            UnityEditor.Animations.AnimatorStateTransition lAnyTransition_N299372 = MotionControllerMotion.EditorFindAnyStateTransition(lLayerStateMachine, lState_N247470, 0);
            if (lAnyTransition_N299372 == null) { lAnyTransition_N299372 = lLayerStateMachine.AddAnyStateTransition(lState_N247470); }
            lAnyTransition_N299372.isExit = false;
            lAnyTransition_N299372.hasExitTime = false;
            lAnyTransition_N299372.hasFixedDuration = true;
            lAnyTransition_N299372.exitTime = 0.75f;
            lAnyTransition_N299372.duration = 0.1f;
            lAnyTransition_N299372.offset = 0.115787f;
            lAnyTransition_N299372.mute = false;
            lAnyTransition_N299372.solo = false;
            lAnyTransition_N299372.canTransitionToSelf = true;
            lAnyTransition_N299372.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            for (int i = lAnyTransition_N299372.conditions.Length - 1; i >= 0; i--) { lAnyTransition_N299372.RemoveCondition(lAnyTransition_N299372.conditions[i]); }
            lAnyTransition_N299372.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 3375f, "L" + rLayerIndex + "MotionPhase");
            lAnyTransition_N299372.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 0f, "L" + rLayerIndex + "MotionForm");
            lAnyTransition_N299372.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Greater, -100f, "L" + rLayerIndex + "MotionParameter");
            lAnyTransition_N299372.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Less, 100f, "L" + rLayerIndex + "MotionParameter");

            UnityEditor.Animations.AnimatorStateTransition lAnyTransition_N299806 = MotionControllerMotion.EditorFindAnyStateTransition(lLayerStateMachine, lState_N247472, 0);
            if (lAnyTransition_N299806 == null) { lAnyTransition_N299806 = lLayerStateMachine.AddAnyStateTransition(lState_N247472); }
            lAnyTransition_N299806.isExit = false;
            lAnyTransition_N299806.hasExitTime = false;
            lAnyTransition_N299806.hasFixedDuration = true;
            lAnyTransition_N299806.exitTime = 0.75f;
            lAnyTransition_N299806.duration = 0.25f;
            lAnyTransition_N299806.offset = 0f;
            lAnyTransition_N299806.mute = false;
            lAnyTransition_N299806.solo = false;
            lAnyTransition_N299806.canTransitionToSelf = true;
            lAnyTransition_N299806.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            for (int i = lAnyTransition_N299806.conditions.Length - 1; i >= 0; i--) { lAnyTransition_N299806.RemoveCondition(lAnyTransition_N299806.conditions[i]); }
            lAnyTransition_N299806.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 3375f, "L" + rLayerIndex + "MotionPhase");
            lAnyTransition_N299806.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 0f, "L" + rLayerIndex + "MotionForm");
            lAnyTransition_N299806.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Greater, 100f, "L" + rLayerIndex + "MotionParameter");

            UnityEditor.Animations.AnimatorStateTransition lAnyTransition_N300182 = MotionControllerMotion.EditorFindAnyStateTransition(lLayerStateMachine, lState_N247472, 1);
            if (lAnyTransition_N300182 == null) { lAnyTransition_N300182 = lLayerStateMachine.AddAnyStateTransition(lState_N247472); }
            lAnyTransition_N300182.isExit = false;
            lAnyTransition_N300182.hasExitTime = false;
            lAnyTransition_N300182.hasFixedDuration = true;
            lAnyTransition_N300182.exitTime = 0.75f;
            lAnyTransition_N300182.duration = 0.1f;
            lAnyTransition_N300182.offset = 0.115787f;
            lAnyTransition_N300182.mute = false;
            lAnyTransition_N300182.solo = false;
            lAnyTransition_N300182.canTransitionToSelf = true;
            lAnyTransition_N300182.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            for (int i = lAnyTransition_N300182.conditions.Length - 1; i >= 0; i--) { lAnyTransition_N300182.RemoveCondition(lAnyTransition_N300182.conditions[i]); }
            lAnyTransition_N300182.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 3375f, "L" + rLayerIndex + "MotionPhase");
            lAnyTransition_N300182.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 0f, "L" + rLayerIndex + "MotionForm");
            lAnyTransition_N300182.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Less, -100f, "L" + rLayerIndex + "MotionParameter");
        }

#endif

    }
}
