using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using com.ootii.Actors.Attributes;
using com.ootii.Helpers;

[CanEditMultipleObjects]
[CustomEditor(typeof(BasicAttributes))]
public class BasicAttributesEditor : Editor
{
    // Helps us keep track of when the list needs to be saved. This
    // is important since some changes happen in scene.
    private bool mIsDirty;

    // The actual class we're stroing
    private BasicAttributes mTarget;
    private SerializedObject mTargetSO;

    // List object for our Items
    private ReorderableList mItemList;

    // Text value to show
    private string mAttributeValue = "";

    /// <summary>
    /// Called when the script object is loaded
    /// </summary>
    void OnEnable()
    {
        // Grab the serialized objects
        mTarget = (BasicAttributes)target;
        mTargetSO = new SerializedObject(target);

        // Create the list of items to display
        InstantiateItemList();
    }

    /// <summary>
    /// Called when the inspector needs to draw
    /// </summary>
    public override void OnInspectorGUI()
    {
        // Pulls variables from runtime so we have the latest values.
        mTargetSO.Update();

        GUILayout.Space(5);

        EditorHelper.DrawInspectorTitle("ootii Basic Attributes");

        EditorHelper.DrawInspectorDescription("Allows us to assign attributes to game objects.", MessageType.None);

        GUILayout.Space(5);

        EditorGUILayout.LabelField("Attributes", EditorStyles.boldLabel, GUILayout.Height(16f));

        GUILayout.BeginVertical(EditorHelper.GroupBox);

        mItemList.DoLayoutList();

        if (mItemList.index >= 0)
        {
            GUILayout.Space(5f);
            GUILayout.BeginVertical(EditorHelper.Box);

            bool lListIsDirty = DrawItemDetail(mTarget.Items[mItemList.index]);
            if (lListIsDirty) { mIsDirty = true; }

            GUILayout.EndVertical();
        }

        EditorGUILayout.EndVertical();

        GUILayout.Space(5);

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

    #region Items

    /// <summary>
    /// Create the reorderable list
    /// </summary>
    private void InstantiateItemList()
    {
        mItemList = new ReorderableList(mTarget.Items, typeof(string), true, true, true, true);
        mItemList.drawHeaderCallback = DrawItemListHeader;
        mItemList.drawFooterCallback = DrawItemListFooter;
        mItemList.drawElementCallback = DrawItemListItem;
        mItemList.onAddCallback = OnItemListItemAdd;
        mItemList.onRemoveCallback = OnItemListItemRemove;
        mItemList.onSelectCallback = OnItemListItemSelect;
        mItemList.onReorderCallback = OnItemListReorder;
        mItemList.footerHeight = 17f;

        if (mTarget.EditorItemIndex >= 0 && mTarget.EditorItemIndex < mItemList.count)
        {
            mItemList.index = mTarget.EditorItemIndex;
        }
    }

    /// <summary>
    /// Header for the list
    /// </summary>
    /// <param name="rRect"></param>
    private void DrawItemListHeader(Rect rRect)
    {
        EditorGUI.LabelField(rRect, "Items");

        Rect lNoteRect = new Rect(rRect.width + 12f, rRect.y, 11f, rRect.height);
        EditorGUI.LabelField(lNoteRect, "-", EditorStyles.miniLabel);

        if (GUI.Button(rRect, "", EditorStyles.label))
        {
            mItemList.index = -1;
            OnItemListItemSelect(mItemList);
        }
    }

    /// <summary>
    /// Allows us to draw each item in the list
    /// </summary>
    /// <param name="rRect"></param>
    /// <param name="rIndex"></param>
    /// <param name="rIsActive"></param>
    /// <param name="rIsFocused"></param>
    private void DrawItemListItem(Rect rRect, int rIndex, bool rIsActive, bool rIsFocused)
    {
        if (rIndex < mTarget.Items.Count)
        {
            BasicAttribute lItem = mTarget.Items[rIndex];

            rRect.y += 2;

            float lWidth = (rRect.width - 5f) *0.5f;

            Rect lNameRect = new Rect(rRect.x, rRect.y, lWidth, EditorGUIUtility.singleLineHeight);
            EditorGUI.LabelField(lNameRect, lItem.ID);

            Rect lValueRect = new Rect(lNameRect.x + lNameRect.width + 0.5f, lNameRect.y, lWidth, EditorGUIUtility.singleLineHeight);

            if (lItem.ValueType == typeof(float))
            {
                float lOldValue = lItem.GetValue<float>();
                float lNewValue = EditorGUI.FloatField(lValueRect, lOldValue);
                if (lNewValue != lOldValue)
                {
                    mIsDirty = true;
                    lItem.SetValue<float>(lNewValue);
                }
            }
            else if (lItem.ValueType == typeof(int))
            {
                int lOldValue = lItem.GetValue<int>();
                int lNewValue = EditorGUI.IntField(lValueRect, lOldValue);
                if (lNewValue != lOldValue)
                {
                    mIsDirty = true;
                    lItem.SetValue<int>(lNewValue);
                }
            }
            else if (lItem.ValueType == typeof(bool))
            {
                bool lOldValue = lItem.GetValue<bool>();
                bool lNewValue = EditorGUI.Toggle(lValueRect, lOldValue);
                if (lNewValue != lOldValue)
                {
                    mIsDirty = true;
                    lItem.SetValue<bool>(lNewValue);
                }

            }
            else if (lItem.ValueType == typeof(string))
            {
                string lOldValue = lItem.GetValue<string>();
                string lNewValue = EditorGUI.TextField(lValueRect, lOldValue);
                if (lNewValue != lOldValue)
                {
                    mIsDirty = true;
                    lItem.SetValue<string>(lNewValue);
                }

            }
            else if (lItem.ValueType == typeof(Vector2))
            {
                Vector2 lOldValue = lItem.GetValue<Vector2>();
                Vector2 lNewValue = EditorGUI.Vector2Field(lValueRect, "", lOldValue);
                if (lNewValue != lOldValue)
                {
                    mIsDirty = true;
                    lItem.SetValue<Vector2>(lNewValue);
                }
            }
            else if (lItem.ValueType == typeof(Vector3))
            {
                Vector3 lOldValue = lItem.GetValue<Vector3>();
                Vector3 lNewValue = EditorGUI.Vector3Field(lValueRect, "", lOldValue);
                if (lNewValue != lOldValue)
                {
                    mIsDirty = true;
                    lItem.SetValue<Vector3>(lNewValue);
                }
            }
            else if (lItem.ValueType == typeof(Vector4))
            {
                Vector4 lOldValue = lItem.GetValue<Vector4>();
                Vector4 lNewValue = EditorGUI.Vector4Field(lValueRect, "", lOldValue);
                if (lNewValue != lOldValue)
                {
                    mIsDirty = true;
                    lItem.SetValue<Vector4>(lNewValue);
                }
            }
            else if (lItem.ValueType == typeof(Quaternion))
            {
                Vector3 lOldValue = lItem.GetValue<Quaternion>().eulerAngles;
                Vector3 lNewValue = EditorGUI.Vector3Field(lValueRect, "", lOldValue);
                if (lNewValue != lOldValue)
                {
                    mIsDirty = true;
                    lItem.SetValue<Quaternion>(Quaternion.Euler(lNewValue));
                }
            }
            else if (lItem.ValueType == typeof(Transform))
            {
                Transform lOldValue = lItem.GetValue<Transform>();
                Transform lNewValue = EditorGUI.ObjectField(lValueRect, lOldValue, typeof(Transform), true) as Transform;
                if (lNewValue != lOldValue)
                {
                    mIsDirty = true;
                    lItem.SetValue<Transform>(lNewValue);
                }
            }
            else if (lItem.ValueType == typeof(GameObject))
            {
                GameObject lOldValue = lItem.GetValue<GameObject>();
                GameObject lNewValue = EditorGUI.ObjectField(lValueRect, lOldValue, typeof(GameObject), true) as GameObject;
                if (lNewValue != lOldValue)
                {
                    mIsDirty = true;
                    lItem.SetValue<GameObject>(lNewValue);
                }
            }
        }
    }

    /// <summary>
    /// Footer for the list
    /// </summary>
    /// <param name="rRect"></param>
    private void DrawItemListFooter(Rect rRect)
    {
        Rect lTextRect = new Rect(rRect.x, rRect.y + 1, rRect.width - 4 - 28 - 28, 16);
        mAttributeValue = EditorGUI.TextField(lTextRect, mAttributeValue);

        Rect lAddRect = new Rect(rRect.x + rRect.width - 28 - 28 - 1, rRect.y + 1, 28, 15);
        if (GUI.Button(lAddRect, new GUIContent("+", "Add Item."), EditorStyles.miniButtonLeft)) { OnItemListItemAdd(mItemList); }

        Rect lDeleteRect = new Rect(lAddRect.x + lAddRect.width, lAddRect.y, 28, 15);
        if (GUI.Button(lDeleteRect, new GUIContent("-", "Delete Item."), EditorStyles.miniButtonRight)) { OnItemListItemRemove(mItemList); };
    }

    /// <summary>
    /// Allows us to add to a list
    /// </summary>
    /// <param name="rList"></param>
    private void OnItemListItemAdd(ReorderableList rList)
    {
        if (mAttributeValue.Length == 0) { return; }
        if (mTarget.AttributeExists(mAttributeValue)) { return; }

        mTarget.AddAttribute(mAttributeValue);

        mItemList.index = mTarget.Items.Count - 1;
        OnItemListItemSelect(rList);

        mIsDirty = true;
    }

    /// <summary>
    /// Allows us process when a list is selected
    /// </summary>
    /// <param name="rList"></param>
    private void OnItemListItemSelect(ReorderableList rList)
    {
        mTarget.EditorItemIndex = rList.index;
    }

    /// <summary>
    /// Allows us to stop before removing the item
    /// </summary>
    /// <param name="rList"></param>
    private void OnItemListItemRemove(ReorderableList rList)
    {
        if (EditorUtility.DisplayDialog("Warning!", "Are you sure you want to delete the item?", "Yes", "No"))
        {
            int lIndex = rList.index;
            BasicAttribute lAttibute = mTarget.Items[lIndex];

            rList.index--;
            mTarget.RemoveAttribute(lAttibute);

            OnItemListItemSelect(rList);

            mIsDirty = true;
        }
    }

    /// <summary>
    /// Allows us to process after the motions are reordered
    /// </summary>
    /// <param name="rList"></param>
    private void OnItemListReorder(ReorderableList rList)
    {
        mIsDirty = true;
    }

    /// <summary>
    /// Renders the currently selected step
    /// </summary>
    /// <param name="rStep"></param>
    private bool DrawItemDetail(BasicAttribute rItem)
    {
        bool lIsDirty = false;

        EditorHelper.DrawSmallTitle(rItem.ID.Length > 0 ? rItem.ID : "Attribute Item");

        float lLabelWidth = EditorGUIUtility.labelWidth;
        EditorGUIUtility.labelWidth = 70f;

        if (EditorHelper.TextField("ID", "Unique ID of the Item", rItem.ID, mTarget))
        {
            lIsDirty = true;
            rItem.ID = EditorHelper.FieldStringValue;
        }

        int lIndex = EnumAttributeTypes.GetEnum(rItem.ValueType);
        if (EditorHelper.PopUpField("Type", "Data type the attribute holds", lIndex, EnumAttributeTypes.Names, mTarget))
        {
            lIsDirty = true;
            rItem.ValueType = EnumAttributeTypes.Types[EditorHelper.FieldIntValue];
        }

        if (rItem.ValueType != null)
        {
            GUILayout.Space(5f);
        }

        if (rItem.ValueType == typeof(float))
        {
            if (EditorHelper.FloatField("Value", "Value of the attribute", rItem.GetValue<float>(), mTarget))
            {
                lIsDirty = true;
                rItem.SetValue<float>(EditorHelper.FieldFloatValue);
            }
        }
        else if (rItem.ValueType == typeof(int))
        {
            if (EditorHelper.IntField("Value", "Value of the attribute", rItem.GetValue<int>(), mTarget))
            {
                lIsDirty = true;
                rItem.SetValue<int>(EditorHelper.FieldIntValue);
            }
        }
        else if (rItem.ValueType == typeof(bool))
        {
            if (EditorHelper.BoolField("Value", "Value of the attribute", rItem.GetValue<bool>(), mTarget))
            {
                lIsDirty = true;
                rItem.SetValue<bool>(EditorHelper.FieldBoolValue);
            }
        }
        else if (rItem.ValueType == typeof(string))
        {
            if (EditorHelper.TextField("Value", "Value of the attribute", rItem.GetValue<string>(), mTarget))
            {
                lIsDirty = true;
                rItem.SetValue<string>(EditorHelper.FieldStringValue);
            }
        }
        else if (rItem.ValueType == typeof(Vector2))
        {
            if (EditorHelper.Vector2Field("Value", "Value of the attribute", rItem.GetValue<Vector2>(), mTarget))
            {
                lIsDirty = true;
                rItem.SetValue<Vector2>(EditorHelper.FieldVector2Value);
            }
        }
        else if (rItem.ValueType == typeof(Vector3))
        {
            if (EditorHelper.Vector3Field("Value", "Value of the attribute", rItem.GetValue<Vector3>(), mTarget))
            {
                lIsDirty = true;
                rItem.SetValue<Vector3>(EditorHelper.FieldVector3Value);
            }
        }
        else if (rItem.ValueType == typeof(Vector4))
        {
#if UNITY_5_0 || UNITY_5_1 || UNITY_5_2 || UNITY_5_3 || UNITY_5_4
            Vector4 lValue = EditorGUILayout.Vector4Field("Value", rItem.GetValue<Vector4>());
#else
            Vector4 lValue = EditorGUILayout.Vector4Field(new GUIContent("Value", "Value of the attribute"), rItem.GetValue<Vector4>());
#endif
            if (lValue != rItem.GetValue<Vector4>())
            {
                lIsDirty = true;
                rItem.SetValue<Vector4>(lValue);
            }
        }
        else if (rItem.ValueType == typeof(Quaternion))
        {
            if (EditorHelper.QuaternionField("Value", "Value of the attribute", rItem.GetValue<Quaternion>(), mTarget))
            {
                lIsDirty = true;
                rItem.SetValue<Quaternion>(EditorHelper.FieldQuaternionValue);
            }
        }
        else if (rItem.ValueType == typeof(Transform))
        {
            if (EditorHelper.ObjectField<Transform>("Value", "Value of the attribute", rItem.GetValue<Transform>(), mTarget))
            {
                lIsDirty = true;
                rItem.SetValue<Transform>(EditorHelper.FieldObjectValue as Transform);
            }
        }
        else if (rItem.ValueType == typeof(GameObject))
        {
            if (EditorHelper.ObjectField<GameObject>("Value", "Value of the attribute", rItem.GetValue<GameObject>(), mTarget))
            {
                lIsDirty = true;
                rItem.SetValue<GameObject>(EditorHelper.FieldObjectValue as GameObject);
            }
        }

        EditorGUIUtility.labelWidth = lLabelWidth;

        return lIsDirty;
    }

    #endregion
}