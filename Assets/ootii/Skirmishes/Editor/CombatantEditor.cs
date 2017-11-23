using UnityEditor;
using UnityEngine;
using com.ootii.Actors.Combat;
using com.ootii.Helpers;
using com.ootii.Input;

[CanEditMultipleObjects]
[CustomEditor(typeof(com.ootii.Actors.Combat.Combatant))]
public class CombatantEditor : Editor
{
    // Helps us keep track of when the list needs to be saved. This
    // is important since some changes happen in scene.
    private bool mIsDirty;

    // Object we're editing
    private com.ootii.Actors.Combat.Combatant mTarget;
    private SerializedObject mTargetSO;

    /// <summary>
    /// Called when the script object is loaded
    /// </summary>
    void OnEnable()
    {
        // Grab the serialized objects
        mTarget = (com.ootii.Actors.Combat.Combatant)target;
        mTargetSO = new SerializedObject(target);

        // Setup the input
        if (!TestInputManagerSettings())
        {
            CreateInputManagerSettings();
        }
    }

    /// <summary>
    /// This function is called when the scriptable object goes out of scope.
    /// </summary>
    private void OnDisable()
    {
    }

    /// <summary>
    /// Called when the inspector needs to draw
    /// </summary>
    public override void OnInspectorGUI()
    {
        mIsDirty = false;

        // Pulls variables from runtime so we have the latest values.
        mTargetSO.Update();

        GUILayout.Space(5);

        EditorHelper.DrawInspectorTitle("ootii Combatant");

        EditorHelper.DrawInspectorDescription("A combatant is able to engage in combat. This component helps manage things like targets, times of attacks, etc.", MessageType.None);

        GUILayout.Space(5);

        EditorGUILayout.LabelField("Actor", EditorStyles.boldLabel, GUILayout.Height(16f));

        EditorGUILayout.BeginVertical(EditorHelper.GroupBox);

        EditorHelper.DrawInspectorDescription("Information about this character that helps with combat.", MessageType.None);

        EditorGUILayout.BeginVertical(EditorHelper.Box);

        if (EditorHelper.ObjectField<Transform>("Combat Transform", "Bone to use as the combat center. Typically this is the chest or top spine. Leave empty to use the character's root.", mTarget.CombatTransform, mTarget))
        {
            mIsDirty = true;
            mTarget.CombatTransform = EditorHelper.FieldObjectValue as Transform;
        }

        if (EditorHelper.BoolField("   Height Only", "Determines if the transform is used for the height value only", mTarget.CombatTransformHeightOnly, mTarget))
        {
            mIsDirty = true;
            mTarget.CombatTransformHeightOnly = EditorHelper.FieldBoolValue;
        }

        GUILayout.Space(5f);

        if (EditorHelper.Vector3Field("Combat Offset", "Represents the offset from the transform of the combatant. For humanoids, this would be the chest or shoulder area.", mTarget.CombatOffset, mTarget))
        {
            mIsDirty = true;
            mTarget.CombatOffset = EditorHelper.FieldVector3Value;
        }

        GUILayout.Space(5f);

        EditorGUILayout.BeginHorizontal();

        EditorHelper.LabelField("Melee Reach", "Min and Max reach for melee combat (without taking the weapon into account).", EditorGUIUtility.labelWidth - 4f);

        if (EditorHelper.FloatField(mTarget.MinMeleeReach, "Min Melee Reach", mTarget, 0f, 48f))
        {
            mIsDirty = true;
            mTarget.MinMeleeReach = EditorHelper.FieldFloatValue;
        }

        if (EditorHelper.FloatField(mTarget.MaxMeleeReach, "Max Melee Reach", mTarget, 0f, 48f))
        {
            mIsDirty = true;
            mTarget.MaxMeleeReach = EditorHelper.FieldFloatValue;
        }

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.EndVertical();

        EditorGUILayout.EndVertical();

        GUILayout.Space(5);

        EditorGUILayout.LabelField("Target & Locking", EditorStyles.boldLabel, GUILayout.Height(16f));

        EditorGUILayout.BeginVertical(EditorHelper.GroupBox);

        EditorHelper.DrawInspectorDescription("The active focus of our character during combat.", MessageType.None);

        EditorGUILayout.BeginVertical(EditorHelper.Box);

        if (EditorHelper.ObjectField<Transform>("Target", "Target that the combatant is focusing on.", mTarget.Target, mTarget))
        {
            mIsDirty = true;
            mTarget.Target = EditorHelper.FieldObjectValue as Transform;
        }

        if (EditorHelper.BoolField("Target Locked", "Determines if we're locked to the current target.", mTarget.IsTargetLocked, mTarget))
        {
            mIsDirty = true;
            mTarget.IsTargetLocked = EditorHelper.FieldBoolValue;
        }

        GUILayout.Space(5f);

        EditorGUILayout.BeginVertical(GUI.skin.box);

        if (EditorHelper.BoolField("Is Locking Enabled", "Determines if the combatant can lock onto a target", mTarget.IsLockingEnabled, mTarget))
        {
            mIsDirty = true;
            mTarget.IsLockingEnabled = EditorHelper.FieldBoolValue;
        }

        if (mTarget.IsLockingEnabled)
        {
            GUILayout.Space(5f);

            if (EditorHelper.TextField("Toggle Lock Alias", "Toggle used to set/release the current target.", mTarget.ToggleCombatantLockAlias, mTarget))
            {
                mIsDirty = true;
                mTarget.ToggleCombatantLockAlias = EditorHelper.FieldStringValue;
            }

#if USE_ACTOR_CONTROLLER || OOTII_AC

            if (EditorHelper.TextField("Valid Actor Stances", "Comma delimited list of Actor Controller stances that the targeting will work in. Leave empty to ignore this condition.", mTarget.ActorStances, mTarget))
            {
                mIsDirty = true;
                mTarget.ActorStances = EditorHelper.FieldStringValue;
            }

#endif

            if (EditorHelper.BoolField("Requires Combatant", "Determines if the target must be a 'combatant' in order to lock.", mTarget.LockRequiresCombatant, mTarget))
            {
                mIsDirty = true;
                mTarget.LockRequiresCombatant = EditorHelper.FieldBoolValue;
            }

            GUILayout.Space(5f);

            if (EditorHelper.FloatField("Max Distance", "Max distance we'll search for a target.", mTarget.MaxLockDistance, mTarget))
            {
                mIsDirty = true;
                mTarget.MaxLockDistance = EditorHelper.FieldFloatValue;
            }

            GUILayout.Space(5f);

            if (EditorHelper.BoolField("Force Character Direction", "Determines if we force the character to rotate to the target.", mTarget.ForceActorRotation, mTarget))
            {
                mIsDirty = true;
                mTarget.ForceActorRotation = EditorHelper.FieldBoolValue;
            }

            if (EditorHelper.BoolField("Force Camera Direction", "Determines if we force the camera to look at the target.", mTarget.ForceCameraRotation, mTarget))
            {
                mIsDirty = true;
                mTarget.ForceCameraRotation = EditorHelper.FieldBoolValue;
            }

#if USE_CAMERA_CONTROLLER || OOTII_CC

            EditorGUILayout.BeginHorizontal();

            EditorHelper.LabelField("Camera Modes", "Camera modes (motor indexes) to use as targeting is locked and unlocked. Use -1 to not set the camera mode.", EditorGUIUtility.labelWidth - 4f);

            if (EditorHelper.IntField(mTarget.LockCameraMode, "Locked Mode", mTarget, 0f, 31f))
            {
                mIsDirty = true;
                mTarget.LockCameraMode = EditorHelper.FieldIntValue;
            }

            if (EditorHelper.IntField(mTarget.UnlockCameraMode, "Unlocked Mode", mTarget, 0f, 31f))
            {
                mIsDirty = true;
                mTarget.UnlockCameraMode = EditorHelper.FieldIntValue;
            }

            EditorGUILayout.EndHorizontal();

#endif

            if (EditorHelper.ObjectField<Texture>("Locked Icon", "Icon to use when we're locked to a target.", mTarget.TargetLockedIcon, mTarget))
            {
                mIsDirty = true;
                mTarget.TargetLockedIcon = EditorHelper.FieldObjectValue as Texture;
            }
        }

        EditorGUILayout.EndVertical();

        EditorGUILayout.EndVertical();

        EditorGUILayout.EndVertical();

        GUILayout.Space(5);

        // Show the events
        GUILayout.BeginHorizontal();

        if (GUILayout.Button(new GUIContent("Events"), EditorStyles.boldLabel))
        {
            mTarget.EditorShowEvents = !mTarget.EditorShowEvents;
        }

        GUILayout.FlexibleSpace();

        if (GUILayout.Button(new GUIContent(mTarget.EditorShowEvents ? "-" : "+"), EditorStyles.boldLabel))
        {
            mTarget.EditorShowEvents = !mTarget.EditorShowEvents;
        }

        GUILayout.EndHorizontal();

        GUILayout.BeginVertical(EditorHelper.GroupBox);
        EditorHelper.DrawInspectorDescription("Assign functions to be called when specific events take place.", MessageType.None);

        if (mTarget.EditorShowEvents)
        {
            GUILayout.BeginVertical(EditorHelper.Box);

            SerializedProperty lItemEquippedEvent = mTargetSO.FindProperty("TargetLockedEvent");
            if (lItemEquippedEvent != null)
            {
                EditorGUI.BeginChangeCheck();
                EditorGUILayout.PropertyField(lItemEquippedEvent);
                if (EditorGUI.EndChangeCheck())
                {
                    mIsDirty = true;
                }
            }

            SerializedProperty lItemStoredEvent = mTargetSO.FindProperty("TargetUnlockedEvent");
            if (lItemStoredEvent != null)
            {
                EditorGUI.BeginChangeCheck();
                EditorGUILayout.PropertyField(lItemStoredEvent);
                if (EditorGUI.EndChangeCheck())
                {
                    mIsDirty = true;
                }
            }

            GUILayout.EndVertical();
        }

        GUILayout.EndVertical();

        GUILayout.Space(5);

        // Show the Layers
        EditorGUILayout.LabelField("Debug", EditorStyles.boldLabel, GUILayout.Height(16f));

        GUILayout.BeginVertical(EditorHelper.GroupBox);
        EditorHelper.DrawInspectorDescription("Determines if we'll render debug information. We can do this motion-by-motion or for all.", MessageType.None);

        GUILayout.BeginVertical(EditorHelper.Box);

        if (EditorHelper.BoolField("Show Debug Info", "Determines if we render debug information at all.", mTarget.ShowDebug, mTarget))
        {
            mIsDirty = true;
            mTarget.ShowDebug = EditorHelper.FieldBoolValue;

            CombatManager.ShowDebug = mTarget.ShowDebug;
        }

        GUILayout.EndVertical();

        GUILayout.EndVertical();

        GUILayout.Space(5f);

        // If there is a change... update.
        if (mIsDirty)
        {
            // Flag the object as needing to be saved
            EditorUtility.SetDirty(mTarget);

#if UNITY_4_0 || UNITY_4_0_1 || UNITY_4_1 || UNITY_4_2 || UNITY_4_3 || UNITY_4_4 || UNITY_4_5 || UNITY_4_6 || UNITY_5_0 || UNITY_5_1 || UNITY_5_2
            EditorApplication.MarkSceneDirty();
#else
            if (!EditorApplication.isPlaying)
            {
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
            }
#endif

            // Pushes the values back to the runtime so it has the changes
            mTargetSO.ApplyModifiedProperties();

            // Clear out the dirty flag
            mIsDirty = false;
        }
    }

    /// <summary>
    /// Test if we need to setup input manager entries
    /// </summary>
    /// <returns></returns>
    private bool TestInputManagerSettings()
    {
        if (!InputManagerHelper.IsDefined("Combatant Toggle Lock")) { return false; }

        return true;
    }

    /// <summary>
    /// If the input manager entries don't exist, create them
    /// </summary>
    private void CreateInputManagerSettings()
    {
        if (!InputManagerHelper.IsDefined("Combatant Toggle Lock"))
        {
            InputManagerEntry lEntry = new InputManagerEntry();
            lEntry.Name = "Combatant Toggle Lock";
            lEntry.PositiveButton = "t";
            lEntry.Gravity = 100;
            lEntry.Dead = 0.3f;
            lEntry.Sensitivity = 100;
            lEntry.Type = InputManagerEntryType.KEY_MOUSE_BUTTON;
            lEntry.Axis = 1;
            lEntry.JoyNum = 0;
            InputManagerHelper.AddEntry(lEntry, true);

#if UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
            lEntry = new InputManagerEntry();
            lEntry.Name = "Combatant Toggle Lock";
            lEntry.PositiveButton = "joystick button 12";
            lEntry.Gravity = 100;
            lEntry.Dead = 0.3f;
            lEntry.Sensitivity = 100;
            lEntry.Type = InputManagerEntryType.KEY_MOUSE_BUTTON;
            lEntry.Axis = 1;
            lEntry.JoyNum = 0;
            InputManagerHelper.AddEntry(lEntry, true);
#else
            lEntry = new InputManagerEntry();
            lEntry.Name = "Combatant Toggle Lock";
            lEntry.PositiveButton = "joystick button 9";
            lEntry.Gravity = 100;
            lEntry.Dead = 0.3f;
            lEntry.Sensitivity = 100;
            lEntry.Type = InputManagerEntryType.KEY_MOUSE_BUTTON;
            lEntry.Axis = 1;
            lEntry.JoyNum = 0;
            InputManagerHelper.AddEntry(lEntry, true);
#endif
        }
    }
}