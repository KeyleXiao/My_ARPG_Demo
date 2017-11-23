using UnityEngine;
using UnityEditor;
using com.ootii.Actors.LifeCores;
using com.ootii.Helpers;

[CanEditMultipleObjects]
[CustomEditor(typeof(TriggerAreaCore), true)]
public class TriggerAreaCoreEditor : Editor
{
    // Helps us keep track of when the list needs to be saved. This
    // is important since some changes happen in scene.
    private bool mIsDirty;

    // The actual class we're storing
    private TriggerAreaCore mTarget;
    private SerializedObject mTargetSO;

    /// <summary>
    /// Called when the object is selected in the editor
    /// </summary>
    private void OnEnable()
    {
        // Grab the serialized objects
        mTarget = (TriggerAreaCore)target;
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

        EditorHelper.DrawInspectorTitle("ootii Trigger Area Core");

        EditorHelper.DrawInspectorDescription("Foundation for areas. This is used for area- of- effect spells.", MessageType.None);

        GUILayout.Space(5f);

        if (EditorHelper.FloatField("Max Age", "Seconds before the object is destroyed.", mTarget.MaxAge, mTarget))
        {
            mIsDirty = true;
            mTarget.MaxAge = EditorHelper.FieldFloatValue;
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

        EditorGUILayout.LabelField("Skim Properties", EditorStyles.boldLabel, GUILayout.Height(16f));

        GUILayout.BeginVertical(EditorHelper.Box);

        if (EditorHelper.BoolField("Skim Surface", "Determines if we'll move the particles to skim the surface/ground", mTarget.SkimSurface, mTarget))
        {
            mIsDirty = true;
            mTarget.SkimSurface = EditorHelper.FieldBoolValue;
        }

        if (mTarget.SkimSurface)
        {
            if (EditorHelper.FloatField("Distance", "Distance offset from the surface/ground", mTarget.SkimSurfaceDistance, mTarget))
            {
                mIsDirty = true;
                mTarget.SkimSurfaceDistance = EditorHelper.FieldFloatValue;
            }

            // Collisions layer
            int lNewCollisionLayers = EditorHelper.LayerMaskField(new GUIContent("Collision Layers", "Layers that we'll test collisions against"), mTarget.SkimSurfaceLayers);
            if (lNewCollisionLayers != mTarget.SkimSurfaceLayers)
            {
                mIsDirty = true;
                mTarget.SkimSurfaceLayers = lNewCollisionLayers;
            }
        }

        GUILayout.EndVertical();

        GUILayout.Space(5f);

        EditorGUILayout.LabelField("Projector Properties", EditorStyles.boldLabel, GUILayout.Height(16f));

        GUILayout.BeginVertical(EditorHelper.Box);

        // Fade speed
        GUILayout.BeginHorizontal();

        EditorGUILayout.LabelField(new GUIContent("Fade Speed", "Speed at which the projector fades in and out"), GUILayout.Width(EditorGUIUtility.labelWidth - 4f));

        if (EditorHelper.FloatField(mTarget.ProjectorFadeInSpeed, "Fade In Speed", mTarget, 0f, 20f))
        {
            mIsDirty = true;
            mTarget.ProjectorFadeInSpeed = EditorHelper.FieldFloatValue;
        }

        if (EditorHelper.FloatField(mTarget.ProjectorFadeOutSpeed, "Fade Out Speed", mTarget, 0f, 20f))
        {
            mIsDirty = true;
            mTarget.ProjectorFadeOutSpeed = EditorHelper.FieldFloatValue;
        }

        GUILayout.EndHorizontal();

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
