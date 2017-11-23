using UnityEngine;
using UnityEditor;
using com.ootii.Actors.LifeCores;
using com.ootii.Helpers;

[CanEditMultipleObjects]
[CustomEditor(typeof(WeaponCore))]
public class WeaponCoreEditor : Editor
{
    // Helps us keep track of when the list needs to be saved. This
    // is important since some changes happen in scene.
    private bool mIsDirty;

    // The actual class we're storing
    private WeaponCore mTarget;
    private SerializedObject mTargetSO;

    /// <summary>
    /// Called when the object is selected in the editor
    /// </summary>
    private void OnEnable()
    {
        // Grab the serialized objects
        mTarget = (WeaponCore)target;
        mTargetSO = new SerializedObject(target);
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
        // Pulls variables from runtime so we have the latest values.
        mTargetSO.Update();

        GUILayout.Space(5);

        EditorHelper.DrawInspectorTitle("ootii Weapon Core");

        EditorHelper.DrawInspectorDescription("Very basic foundation for weapons. This allows us to set some simple properties.", MessageType.None);

        GUILayout.Space(5);

        if (EditorHelper.Vector3Field("Local Position", "Local position from the character's right hand (when Mount Points isn't used).", mTarget.LocalPosition))
        {
            mIsDirty = true;
            mTarget.LocalPosition = EditorHelper.FieldVector3Value;
        }

        // Temporary to ensure we have a the euler value set
        if (mTarget._LocalRotationEuler.sqrMagnitude == 0f)
        {
            mTarget._LocalRotationEuler = mTarget._LocalRotation.eulerAngles;
        }

        if (EditorHelper.Vector3Field("Local Rotation", "Local rotation from the character's hand (when Mount Points isn't used).", mTarget.LocalRotationEuler))
        {
            mIsDirty = true;
            mTarget.LocalRotationEuler = EditorHelper.FieldVector3Value;
        }

        //if (EditorHelper.QuaternionField("Local Rotation", "Local rotation from the character's right hand (when Mount Points isn't used).", mTarget.LocalRotation))
        //{
        //    mIsDirty = true;
        //    mTarget.LocalRotation = EditorHelper.FieldQuaternionValue;
        //}

        GUILayout.Space(5f);

        // Range
        GUILayout.BeginHorizontal();

        EditorGUILayout.LabelField(new GUIContent("Range", "Additive min and max range that the weapon can reach."), GUILayout.Width(EditorGUIUtility.labelWidth));

        if (EditorHelper.FloatField(mTarget.MinRange, "Min Range", mTarget, 0f, 20f))
        {
            mIsDirty = true;
            mTarget.MinRange = EditorHelper.FieldFloatValue;
        }

        if (EditorHelper.FloatField(mTarget.MaxRange, "Max Range", mTarget, 0f, 20f))
        {
            mIsDirty = true;
            mTarget.MaxRange = EditorHelper.FieldFloatValue;
        }

        GUILayout.EndHorizontal();

        // Damage
        GUILayout.BeginHorizontal();

        EditorGUILayout.LabelField(new GUIContent("Damage", "Min and max damage applied on impact. Currently, only the 'max' value matters."), GUILayout.Width(EditorGUIUtility.labelWidth));

        if (EditorHelper.FloatField(mTarget.MinDamage, "Min Damage", mTarget, 0f, 20f))
        {
            mIsDirty = true;
            mTarget.MinDamage = EditorHelper.FieldFloatValue;
        }

        if (EditorHelper.FloatField(mTarget.MaxDamage, "Max Damage", mTarget, 0f, 20f))
        {
            mIsDirty = true;
            mTarget.MaxDamage = EditorHelper.FieldFloatValue;
        }

        GUILayout.EndHorizontal();

        // Impact Power
        GUILayout.BeginHorizontal();

        EditorGUILayout.LabelField(new GUIContent("Impact Power", "Min and max multiplier to apply on impact. Currently, only the 'max' value matters."), GUILayout.Width(EditorGUIUtility.labelWidth));

        if (EditorHelper.FloatField(mTarget.MinImpactPower, "Min Impact Power", mTarget, 0f, 20f))
        {
            mIsDirty = true;
            mTarget.MinImpactPower = EditorHelper.FieldFloatValue;
        }

        if (EditorHelper.FloatField(mTarget.MaxImpactPower, "Max Impact Power", mTarget, 0f, 20f))
        {
            mIsDirty = true;
            mTarget.MaxImpactPower = EditorHelper.FieldFloatValue;
        }

        GUILayout.EndHorizontal();

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
}
