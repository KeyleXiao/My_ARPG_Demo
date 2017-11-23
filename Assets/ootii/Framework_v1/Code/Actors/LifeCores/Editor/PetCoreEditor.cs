using UnityEngine;
using UnityEditor;
using com.ootii.Actors.LifeCores;
using com.ootii.Helpers;

[CanEditMultipleObjects]
[CustomEditor(typeof(PetCore), true)]
public class PetCoreEditor : Editor
{
    // Helps us keep track of when the list needs to be saved. This
    // is important since some changes happen in scene.
    private bool mIsDirty;

    // The actual class we're storing
    private PetCore mTarget;
    private SerializedObject mTargetSO;

    /// <summary>
    /// Called when the object is selected in the editor
    /// </summary>
    private void OnEnable()
    {
        // Grab the serialized objects
        mTarget = (PetCore)target;
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

        float lLabelWidth = EditorGUIUtility.labelWidth;
        EditorGUIUtility.labelWidth = 100f;

        GUILayout.Space(5);

        EditorHelper.DrawInspectorTitle("ootii Pet Core");

        EditorHelper.DrawInspectorDescription("Very basic foundation for followers. This allows us to set some simple properties and auto-destroy.", MessageType.None);

        GUILayout.Space(5);

        if (EditorHelper.FloatField("Max Age", "Seconds before the object is destroyed.", mTarget.MaxAge, mTarget))
        {
            mIsDirty = true;
            mTarget.MaxAge = EditorHelper.FieldFloatValue;
        }

        if (EditorHelper.FloatField("Wander Radius", "Distance the pet will wander.", mTarget.WanderRadius, mTarget))
        {
            mIsDirty = true;
            mTarget.WanderRadius = EditorHelper.FieldFloatValue;
        }

        GUILayout.Space(5f);

        EditorGUILayout.LabelField("Particle & Sound Containers", EditorStyles.boldLabel, GUILayout.Height(16f));

        GUILayout.BeginVertical(EditorHelper.Box);

        if (EditorHelper.ObjectField<GameObject>("Particle Root", "Child objects that holds effects and sounds during the life of the area.", mTarget.LifeRoot, mTarget))
        {
            mIsDirty = true;
            mTarget.LifeRoot = EditorHelper.FieldObjectValue as GameObject;
        }

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
}
