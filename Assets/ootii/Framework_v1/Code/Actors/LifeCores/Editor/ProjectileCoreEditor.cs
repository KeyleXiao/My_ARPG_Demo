using UnityEngine;
using UnityEditor;
using com.ootii.Actors.LifeCores;
using com.ootii.Helpers;

[CanEditMultipleObjects]
[CustomEditor(typeof(ProjectileCore), true)]
public class SpellProjectileCoreEditor : Editor
{
    // Helps us keep track of when the list needs to be saved. This
    // is important since some changes happen in scene.
    private bool mIsDirty;

    // The actual class we're storing
    private ProjectileCore mTarget;
    private SerializedObject mTargetSO;

    /// <summary>
    /// Called when the object is selected in the editor
    /// </summary>
    private void OnEnable()
    {
        // Grab the serialized objects
        mTarget = (ProjectileCore)target;
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

        EditorHelper.DrawInspectorTitle("ootii Projectile Core");

        EditorHelper.DrawInspectorDescription("Foundation for projectiles. This allows us to set some basic properties as well as effects and sounds.", MessageType.None);

        GUILayout.Space(5f);

        if (EditorHelper.FloatField("Max Age", "Once impact occurs, the seconds before the arrow is destroyed.", mTarget.MaxAge, mTarget))
        {
            mIsDirty = true;
            mTarget.MaxAge = EditorHelper.FieldFloatValue;
        }

        if (EditorHelper.FloatField("Speed", "Units per second that the projectile moves (when no rigidbody is attached).", mTarget.Speed, mTarget))
        {
            mIsDirty = true;
            mTarget.Speed = EditorHelper.FieldFloatValue;
        }

        // Distance
        GUILayout.BeginHorizontal();

        EditorGUILayout.LabelField(new GUIContent("Range", "Min and max Distance for the projectile to succeed."), GUILayout.Width(EditorGUIUtility.labelWidth - 4f));

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

        if (EditorHelper.BoolField("Is Homing", "Determines if the projectile tracks the target and moves to it.", mTarget.IsHoming, mTarget))
        {
            mIsDirty = true;
            mTarget.IsHoming = EditorHelper.FieldBoolValue;
        }

        if (EditorHelper.BoolField("Use Raycasts", "Determines if we use raycasting for collisions instead of colliders.", mTarget.UseRaycast, mTarget))
        {
            mIsDirty = true;
            mTarget.UseRaycast = EditorHelper.FieldBoolValue;
        }

        GUILayout.Space(5f);

        EditorGUILayout.LabelField("Impact Properties", EditorStyles.boldLabel, GUILayout.Height(16f));

        GUILayout.BeginVertical(EditorHelper.Box);

        if (EditorHelper.BoolField("Embed On Impact", "Determines if the projectile sticks around after impact.", mTarget.EmbedOnImpact, mTarget))
        {
            mIsDirty = true;
            mTarget.EmbedOnImpact = EditorHelper.FieldBoolValue;
        }

        if (EditorHelper.FloatField("Embed Distance", "Amount to embed the projectile after impact.", mTarget.EmbedDistance, mTarget))
        {
            mIsDirty = true;
            mTarget.EmbedDistance = EditorHelper.FieldFloatValue;
        }

        GUILayout.EndVertical();

        GUILayout.Space(5f);

        EditorGUILayout.LabelField("Particle & Sound Containers", EditorStyles.boldLabel, GUILayout.Height(16f));

        GUILayout.BeginVertical(EditorHelper.Box);

        if (EditorHelper.ObjectField<GameObject>("Launch Root", "Child objects that holds effects and sounds for the launch.", mTarget.LaunchRoot, mTarget))
        {
            mIsDirty = true;
            mTarget.LaunchRoot = EditorHelper.FieldObjectValue as GameObject;
        }

        if (EditorHelper.ObjectField<GameObject>("Fly Root", "Child objects that holds effects and sounds during launch.", mTarget.FlyRoot, mTarget))
        {
            mIsDirty = true;
            mTarget.FlyRoot = EditorHelper.FieldObjectValue as GameObject;
        }

        if (EditorHelper.ObjectField<GameObject>("Impact Root", "Child objects that holds effects and sounds for the impact.", mTarget.ImpactRoot, mTarget))
        {
            mIsDirty = true;
            mTarget.ImpactRoot = EditorHelper.FieldObjectValue as GameObject;
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
