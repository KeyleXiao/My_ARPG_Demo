using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using com.ootii.Helpers;
using com.ootii.Input;

namespace com.ootii.Actors.Magic
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(SpellInventory))]
    public class SpellInventoryEditor : UnityEditor.Editor
    {
        // Helps us keep track of when the list needs to be saved. This
        // is important since some changes happen in scene.
        private bool mIsDirty;

        // The actual class we're storing
        private SpellInventory mTarget;
        private SerializedObject mTargetSO;

        // List object for our Items
        private ReorderableList mItemList;

        /// <summary>
        /// Called when the object is selected in the editor
        /// </summary>
        private void OnEnable()
        {
            // Grab the serialized objects
            mTarget = (SpellInventory)target;
            mTargetSO = new SerializedObject(target);

            // Create the list of items to display
            InstantiateItemList();
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

            EditorHelper.DrawInspectorTitle("ootii Spell Inventory");

            EditorHelper.DrawInspectorDescription("Very basic foundation for a list of spells.", MessageType.None);

            GUILayout.Space(5f);

            EditorGUILayout.LabelField("Input", EditorStyles.boldLabel, GUILayout.Height(16f));

            EditorGUILayout.BeginVertical(EditorHelper.Box);

            EditorGUILayout.BeginHorizontal();

            GameObject lNewInputSourceOwner = EditorHelper.InterfaceOwnerField<IInputSource>(new GUIContent("Input Source", "Input source we'll use to get key presses, mouse movement, etc. This GameObject should have a component implementing the IInputSource interface."), mTarget.InputSourceOwner, true);
            if (lNewInputSourceOwner != mTarget.InputSourceOwner)
            {
                mIsDirty = true;
                mTarget.InputSourceOwner = lNewInputSourceOwner;
            }

            GUILayout.Space(2);

            EditorGUILayout.LabelField(new GUIContent("Find", "Determines if we attempt to automatically find the input source at startup if one isn't set."), GUILayout.Width(30));

            bool lNewAutoFindInputSource = EditorGUILayout.Toggle(mTarget.AutoFindInputSource, GUILayout.Width(16));
            if (lNewAutoFindInputSource != mTarget.AutoFindInputSource)
            {
                mIsDirty = true;
                mTarget.AutoFindInputSource = lNewAutoFindInputSource;
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();

            GUILayout.Space(5f);

            // Default spell to cast
            if (EditorHelper.IntField("Default Spell", "Default spell to cast when activated.", mTarget.DefaultSpellIndex, mTarget))
            {
                mIsDirty = true;
                mTarget.DefaultSpellIndex = EditorHelper.FieldIntValue;
            }

            if (EditorHelper.TextField("Action Alias", "Action alias used to cast the default spell.", mTarget.ActionAlias, mTarget))
            {
                mIsDirty = true;
                mTarget.ActionAlias = EditorHelper.FieldStringValue;
            }

            GUILayout.Space(5f);

            // Show the items
            EditorGUILayout.LabelField("Spell Items", EditorStyles.boldLabel, GUILayout.Height(16f));

            GUILayout.BeginVertical(EditorHelper.GroupBox);
            EditorHelper.DrawInspectorDescription("Spell items represent the connection between the spell (template) and the data (instance).", MessageType.None);

            mItemList.DoLayoutList();

            if (mItemList.index >= 0)
            {
                GUILayout.Space(5f);
                GUILayout.BeginVertical(EditorHelper.Box);

                bool lListIsDirty = DrawItemDetailItem(mTarget._Spells[mItemList.index]);
                if (lListIsDirty) { mIsDirty = true; }

                GUILayout.EndVertical();
            }

            EditorGUILayout.EndVertical();

            GUILayout.Space(5f);

            // Show the debug
            EditorGUILayout.LabelField("Debug", EditorStyles.boldLabel, GUILayout.Height(16f));

            GUILayout.BeginVertical(EditorHelper.GroupBox);
            EditorHelper.DrawInspectorDescription("Determines if we'll render debug information for all the spells.", MessageType.None);

            GUILayout.BeginVertical(EditorHelper.Box);

            if (EditorHelper.BoolField("Show Debug Info", "Determines if the MC will render debug information at all.", mTarget.ShowDebug, mTarget))
            {
                mIsDirty = true;
                mTarget.ShowDebug = EditorHelper.FieldBoolValue;
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

            EditorGUIUtility.labelWidth = lLabelWidth;
        }

        #region Items

        /// <summary>
        /// Create the reorderable list
        /// </summary>
        private void InstantiateItemList()
        {
            mItemList = new ReorderableList(mTarget._Spells, typeof(SpellInventoryItem), true, true, true, true);
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
            EditorGUI.LabelField(rRect, "Spell Items");

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
            if (rIndex < mTarget._Spells.Count)
            {
                SpellInventoryItem lItem = mTarget._Spells[rIndex];

                rRect.y += 2;

                Rect lNameRect = new Rect(rRect.x, rRect.y, rRect.width - 60f, EditorGUIUtility.singleLineHeight);
                EditorGUI.LabelField(lNameRect, string.Format("[{0}] {1}", rIndex, lItem.Name));

                Rect lButtonRect = new Rect(lNameRect.x + lNameRect.width + 10f, lNameRect.y, 50f, lNameRect.height);
                if (GUI.Button(lButtonRect, "open", EditorHelper.LinkLabel))
                {
                    int lInstance = lItem.SpellPrefab.GetInstanceID();
                    SpellEditorWindow.OpenSpellEditor(lInstance);
                }
            }
        }

        /// <summary>
        /// Footer for the list
        /// </summary>
        /// <param name="rRect"></param>
        private void DrawItemListFooter(Rect rRect)
        {
            Rect lAddRect = new Rect(rRect.x + rRect.width - 28 - 28 - 1, rRect.y + 1, 28, 15);
            if (GUI.Button(lAddRect, new GUIContent("+", "Add Spell Item."), EditorStyles.miniButtonLeft)) { OnItemListItemAdd(mItemList); }

            Rect lDeleteRect = new Rect(lAddRect.x + lAddRect.width, lAddRect.y, 28, 15);
            if (GUI.Button(lDeleteRect, new GUIContent("-", "Delete Spell Item."), EditorStyles.miniButtonRight)) { OnItemListItemRemove(mItemList); };
        }

        /// <summary>
        /// Allows us to add to a list
        /// </summary>
        /// <param name="rList"></param>
        private void OnItemListItemAdd(ReorderableList rList)
        {
            SpellInventoryItem lItem = new SpellInventoryItem();

            mTarget._Spells.Add(lItem);

            mItemList.index = mTarget._Spells.Count - 1;
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
                int rIndex = rList.index;

                rList.index--;
                mTarget._Spells.RemoveAt(rIndex);

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
        private bool DrawItemDetailItem(SpellInventoryItem rItem)
        {
            bool lIsDirty = false;

            EditorHelper.DrawSmallTitle(rItem.Name.Length > 0 ? rItem.Name : "Spell Inventory Item");

            if (rItem.SpellPrefab != null && rItem.SpellPrefab.Description.Length > 0)
            {
                EditorHelper.DrawInspectorDescription(rItem.SpellPrefab.Description, MessageType.None);
            }
            else
            {
                EditorGUILayout.LabelField("", GUILayout.Height(1f));
            }

            if (EditorHelper.TextField("Name", "Friendly name of the spell item", rItem._Name, mTarget))
            {
                lIsDirty = true;
                rItem.Name = EditorHelper.FieldStringValue;
            }

            if (EditorHelper.ObjectField<Spell>("Spell Prefab", "Spell 'template' that defines what the spell is and template-level detail like 'max range'.", rItem.SpellPrefab, mTarget))
            {
                lIsDirty = true;
                rItem.SpellPrefab = EditorHelper.FieldObjectValue as Spell;
            }

            GUILayout.Space(2f);

            return lIsDirty;
        }

        #endregion
    }
}