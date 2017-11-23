using UnityEngine;
using UnityEditor;
using com.ootii.Actors.LifeCores;
using com.ootii.Helpers;

[CanEditMultipleObjects]
[CustomEditor(typeof(SelectPositionCore), true)]
public class SelectPositionCoreEditor : Editor
{
    // Helps us keep track of when the list needs to be saved. This
    // is important since some changes happen in scene.
    private bool mIsDirty;

    // The actual class we're storing
    private SelectPositionCore mTarget;
    private SerializedObject mTargetSO;

    /// <summary>
    /// Called when the object is selected in the editor
    /// </summary>
    private void OnEnable()
    {
        // Grab the serialized objects
        mTarget = (SelectPositionCore)target;
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

        EditorHelper.DrawInspectorTitle("ootii Select Position Core");

        EditorHelper.DrawInspectorDescription("Foundation for a selector object that can select positions, targets, etc.", MessageType.None);

        GUILayout.Space(5f);

        if (EditorHelper.TextField("Action Alias", "Action alias used to select the position", mTarget.ActionAlias, mTarget))
        {
            mIsDirty = true;
            mTarget.ActionAlias = EditorHelper.FieldStringValue;
        }

        if (EditorHelper.TextField("Cancel Alias", "Action alias used to cancel the selection", mTarget.CancelActionAlias, mTarget))
        {
            mIsDirty = true;
            mTarget.CancelActionAlias = EditorHelper.FieldStringValue;
        }

        if (EditorHelper.BoolField("Use Mouse", "Determines if we use the mouse to select the position or the reticle (camera).", mTarget.UseMouse, mTarget))
        {
            mIsDirty = true;
            mTarget.UseMouse = EditorHelper.FieldBoolValue;
        }

        GUILayout.Space(5f);

        // Distance
        GUILayout.BeginHorizontal();

        EditorGUILayout.LabelField(new GUIContent("Distance", "Min and max Distance for the projectile to succeed."), GUILayout.Width(EditorGUIUtility.labelWidth - 4f));

        if (EditorHelper.FloatField(mTarget.MinDistance, "Min Distance", mTarget, 0f, 20f))
        {
            mIsDirty = true;
            mTarget.MinDistance = EditorHelper.FieldFloatValue;
        }

        if (EditorHelper.FloatField(mTarget.MaxDistance, "Max Distance", mTarget, 0f, 20f))
        {
            mIsDirty = true;
            mTarget.MaxDistance = EditorHelper.FieldFloatValue;
        }

        GUILayout.EndHorizontal();

        if (EditorHelper.FloatField("Radius", "Radius to use with the cast to ensure the selected position isn't too close to an object. Default is 0.", mTarget.Radius, mTarget))
        {
            mIsDirty = true;
            mTarget.Radius = EditorHelper.FieldFloatValue;
        }

        int lNewGroundingLayers = EditorHelper.LayerMaskField(new GUIContent("Collision Layers", "Layers that we'll test collisions against."), mTarget.CollisionLayers);
        if (lNewGroundingLayers != mTarget.CollisionLayers)
        {
            mIsDirty = true;
            mTarget.CollisionLayers = lNewGroundingLayers;
        }

        if (EditorHelper.TextField("Tags", "Tags that the target must contain to be valid.", mTarget.Tags, mTarget))
        {
            mIsDirty = true;
            mTarget.Tags = EditorHelper.FieldStringValue;
        }

        if (EditorHelper.BoolField("Matching Up Required", "Determines if the position's normal must match the owner's up.", mTarget.RequireMatchingUp, mTarget))
        {
            mIsDirty = true;
            mTarget.RequireMatchingUp = EditorHelper.FieldBoolValue;
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

        EditorGUILayout.LabelField("Fade Properties", EditorStyles.boldLabel, GUILayout.Height(16f));

        GUILayout.BeginVertical(EditorHelper.Box);

        // Audio fade
        UnityEngine.GUILayout.BeginHorizontal();

        UnityEditor.EditorGUILayout.LabelField(new UnityEngine.GUIContent("Audio Fade", "Fade in and fade out speed."), GUILayout.Width(EditorGUIUtility.labelWidth));

        if (EditorHelper.FloatField(mTarget.AudioFadeInSpeed, "Fade In", mTarget, 0f, 20f))
        {
            mIsDirty = true;
            mTarget.AudioFadeInSpeed = EditorHelper.FieldFloatValue;
        }

        if (EditorHelper.FloatField(mTarget.AudioFadeOutSpeed, "Fade Out", mTarget, 0f, 20f))
        {
            mIsDirty = true;
            mTarget.AudioFadeOutSpeed = EditorHelper.FieldFloatValue;
        }

        UnityEngine.GUILayout.EndHorizontal();

        // Light fade
        UnityEngine.GUILayout.BeginHorizontal();

        UnityEditor.EditorGUILayout.LabelField(new UnityEngine.GUIContent("Light Fade", "Fade in and fade out speed."), GUILayout.Width(EditorGUIUtility.labelWidth));

        if (EditorHelper.FloatField(mTarget.LightFadeInSpeed, "Fade In", mTarget, 0f, 20f))
        {
            mIsDirty = true;
            mTarget.LightFadeInSpeed = EditorHelper.FieldFloatValue;
        }

        if (EditorHelper.FloatField(mTarget.LightFadeOutSpeed, "Fade Out", mTarget, 0f, 20f))
        {
            mIsDirty = true;
            mTarget.LightFadeOutSpeed = EditorHelper.FieldFloatValue;
        }

        UnityEngine.GUILayout.EndHorizontal();

        // Projector fade
        UnityEngine.GUILayout.BeginHorizontal();

        UnityEditor.EditorGUILayout.LabelField(new UnityEngine.GUIContent("Projector Fade", "Fade in and fade out speed."), GUILayout.Width(EditorGUIUtility.labelWidth));

        if (EditorHelper.FloatField(mTarget.ProjectorFadeInSpeed, "Fade In", mTarget, 0f, 20f))
        {
            mIsDirty = true;
            mTarget.ProjectorFadeInSpeed = EditorHelper.FieldFloatValue;
        }

        if (EditorHelper.FloatField(mTarget.ProjectorFadeOutSpeed, "Fade Out", mTarget, 0f, 20f))
        {
            mIsDirty = true;
            mTarget.ProjectorFadeOutSpeed = EditorHelper.FieldFloatValue;
        }

        UnityEngine.GUILayout.EndHorizontal();

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
