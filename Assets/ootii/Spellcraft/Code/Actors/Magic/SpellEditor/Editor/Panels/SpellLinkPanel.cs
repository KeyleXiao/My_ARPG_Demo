using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using com.ootii.Base;
using com.ootii.Graphics.NodeGraph;
using com.ootii.Helpers;

namespace com.ootii.Actors.Magic
{
    /// <summary>
    /// Panel that shows the properties of the selected link
    /// </summary>
    public class SpellLinkPanel : NodePanel
    {
        // Link that we are currently editing
        private NodeLink mTarget = null;

        // List object for our Items
        private ReorderableList mActionList;

        // Store the action types
        private int mActionIndex = 0;
        private List<Type> mActionTypes = new List<Type>();
        private List<string> mActionNames = new List<string>();

        // Index of the currently selected action in the list
        private int mEditorActionIndex = -1;

        // Used to determine if the link action is dirty
        private bool mIsDirty = false;

        /// <summary>
        /// Determine the panel background style 
        /// </summary>
        public override GUIStyle PanelStyle
        {
            get { return NodeEditorStyle.PanelSelected; }
        }

        /// <summary>
        /// Determine the panel title style
        /// </summary>
        public override GUIStyle PanelTitleStyle
        {
            get { return NodeEditorStyle.PanelSelectedTitle; }
        }

        /// <summary>
        /// Default constructor
        /// </summary>
        public SpellLinkPanel() : base()
        {
            IsEnabled = false;

            Position.width = 220f;
            Position.height = 450f;
        }

        /// <summary>
        /// Draws in the content area of the window 
        /// </summary>
        public override void DrawContent()
        {
            // Stop if no link is selected
            if (Editor.Canvas.SelectedLink == null)
            {
                mTarget = null;
                return;
            }

            bool lHasChanged = (mTarget != Editor.Canvas.SelectedLink);

            mIsDirty = false;
            mTarget = Editor.Canvas.SelectedLink;

            // Update the list if a new target is selected
            if (mActionList == null || lHasChanged)
            {
                if (mTarget.Actions == null)
                {
                    mTarget.Actions = new List<NodeLinkAction>();
                }

                InstantiateActionList();
            }

            if (mActionList != null)
            {
                if (mActionList.index >= mActionList.count) { mActionList.index = mActionList.count - 1; }
                if (mEditorActionIndex >= mActionList.count) { mEditorActionIndex = mActionList.count - 1; }
            }

            // Edit the link

            if (EditorHelper.BoolField("Is Enabled", "Determines if the link can be traversed.", mTarget.IsEnabled, Editor.RootAsset))
            {
                mIsDirty = true;
                mTarget.IsEnabled = EditorHelper.FieldBoolValue;
            }

            if (EditorHelper.TextField("Name", "Name used to help identify the link.", mTarget.Name, Editor.RootAsset))
            {
                mIsDirty = true;
                mTarget.Name = EditorHelper.FieldStringValue;
            }

            GUILayout.Space(5f);

            // Show the items
            GUILayout.BeginVertical(NodeEditorStyle.GroupBox);
            mActionList.DoLayoutList();

            if (mActionList.index >= 0 && mActionList.index < mActionList.count)
            {
                GUILayout.Space(5f);
                GUILayout.BeginVertical(NodeEditorStyle.Box);

                bool lListIsDirty = DrawActionDetailItem(mTarget.Actions[mActionList.index]);
                if (lListIsDirty) { mIsDirty = true; }

                GUILayout.EndVertical();
            }

            EditorGUILayout.EndVertical();

            if (mIsDirty)
            {
                EditorUtility.SetDirty(mTarget);
                Editor.SetDirty();
            }
        }

        /// <summary>
        /// Raised when a canvas link is selected or when the selection is cleared
        /// </summary>
        /// <param name="rLink">Node link that is selected or null if deselected</param>
        public override void OnCanvasLinkSelected(NodeLink rLink)
        {
            IsEnabled = (rLink != null);
        }


        #region Actions

        /// <summary>
        /// Create the reorderable list
        /// </summary>
        private void InstantiateActionList()
        {
            // Dropdown values
            mActionTypes.Clear();
            mActionNames.Clear();

            // Generate the list of motions to display
            Assembly lAssembly = Assembly.GetAssembly(typeof(NodeLinkAction));
            Type[] lMotionTypes = lAssembly.GetTypes().OrderBy(x => x.Name).ToArray<Type>();
            for (int i = 0; i < lMotionTypes.Length; i++)
            {
                Type lType = lMotionTypes[i];
                if (lType.IsAbstract) { continue; }
                if (typeof(NodeLinkAction).IsAssignableFrom(lType))
                {
                    mActionTypes.Add(lType);
                    mActionNames.Add(BaseNameAttribute.GetName(lType));
                }
            }

            mActionList = new ReorderableList(mTarget.Actions, typeof(NodeLinkAction), true, true, true, true);
            mActionList.drawHeaderCallback = DrawActionListHeader;
            mActionList.drawFooterCallback = DrawActionListFooter;
            mActionList.drawElementCallback = DrawActionListItem;
            mActionList.onAddCallback = OnActionListItemAdd;
            mActionList.onRemoveCallback = OnActionListItemRemove;
            mActionList.onSelectCallback = OnActionListItemSelect;
            mActionList.onReorderCallback = OnActionListReorder;
            mActionList.footerHeight = 17f;

            if (mEditorActionIndex >= 0 && mEditorActionIndex < mActionList.count)
            {
                mActionList.index = mEditorActionIndex;
            }
        }

        /// <summary>
        /// Header for the list
        /// </summary>
        /// <param name="rRect"></param>
        private void DrawActionListHeader(Rect rRect)
        {
            EditorGUI.LabelField(rRect, "Link Actions", NodeEditorStyle.BoldLabel);

            Rect lNoteRect = new Rect(rRect.width + 12f, rRect.y, 11f, rRect.height);
            EditorGUI.LabelField(lNoteRect, "-", EditorStyles.miniLabel);

            if (GUI.Button(rRect, "", EditorStyles.label))
            {
                mActionList.index = -1;
                OnActionListItemSelect(mActionList);
            }
        }

        /// <summary>
        /// Allows us to draw each item in the list
        /// </summary>
        /// <param name="rRect"></param>
        /// <param name="rIndex"></param>
        /// <param name="rIsActive"></param>
        /// <param name="rIsFocused"></param>
        private void DrawActionListItem(Rect rRect, int rIndex, bool rIsActive, bool rIsFocused)
        {
            if (rIndex < mTarget.Actions.Count)
            {
                NodeLinkAction lItem = mTarget.Actions[rIndex];
                if (lItem == null)
                {
                    EditorGUI.LabelField(rRect, "NULL");
                    return;
                }

                rRect.y += 2;

                string lName = lItem.Name;
                if (lName.Length == 0) { lName = BaseNameAttribute.GetName(lItem.GetType()); }

                Rect lNameRect = new Rect(rRect.x, rRect.y, rRect.width, EditorGUIUtility.singleLineHeight);
                EditorGUI.LabelField(lNameRect, lName);
            }
        }

        /// <summary>
        /// Footer for the list
        /// </summary>
        /// <param name="rRect"></param>
        private void DrawActionListFooter(Rect rRect)
        {
            Rect lMotionRect = new Rect(rRect.x, rRect.y + 1, rRect.width - 4 - 28 - 28, 16);
            mActionIndex = EditorGUI.Popup(lMotionRect, mActionIndex, mActionNames.ToArray());

            Rect lAddRect = new Rect(rRect.x + rRect.width - 28 - 28 - 1, rRect.y + 1, 28, 15);
            if (GUI.Button(lAddRect, new GUIContent("+", "Add Node Link Action."), EditorStyles.miniButtonLeft)) { OnActionListItemAdd(mActionList); }

            Rect lDeleteRect = new Rect(lAddRect.x + lAddRect.width, lAddRect.y, 28, 15);
            if (GUI.Button(lDeleteRect, new GUIContent("-", "Delete Node Link Action."), EditorStyles.miniButtonRight)) { OnActionListItemRemove(mActionList); };
        }

        /// <summary>
        /// Allows us to add to a list
        /// </summary>
        /// <param name="rList"></param>
        private void OnActionListItemAdd(ReorderableList rList)
        {
            if (mActionIndex >= mActionTypes.Count) { return; }

            NodeLinkAction lItem = ScriptableObject.CreateInstance(mActionTypes[mActionIndex]) as NodeLinkAction;
            lItem._Link = mTarget;

            mTarget.Actions.Add(lItem);

            mActionList.index = mTarget.Actions.Count - 1;
            OnActionListItemSelect(rList);

            // Add the action as an asset. However, we have to name it this:
            // http://answers.unity3d.com/questions/1164341/can-a-scriptableobject-contain-a-list-of-scriptabl.html
            lItem.name = "zzzz NodeListAction " + mTarget.Actions.Count.ToString("D4");
            lItem.hideFlags = HideFlags.HideInHierarchy;
            AssetDatabase.AddObjectToAsset(lItem, mTarget.StartNode.Canvas.RootAsset);

            EditorUtility.SetDirty(lItem);
            EditorUtility.SetDirty(mTarget);
            mTarget.StartNode.Canvas.SetDirty();

            mIsDirty = true;
        }

        /// <summary>
        /// Allows us process when a list is selected
        /// </summary>
        /// <param name="rList"></param>
        private void OnActionListItemSelect(ReorderableList rList)
        {
            mEditorActionIndex = rList.index;
        }

        /// <summary>
        /// Allows us to stop before removing the item
        /// </summary>
        /// <param name="rList"></param>
        private void OnActionListItemRemove(ReorderableList rList)
        {
            if (EditorUtility.DisplayDialog("Warning!", "Are you sure you want to delete the item?", "Yes", "No"))
            {
                int rIndex = rList.index;
                rList.index--;

                NodeLinkAction lAction = mTarget.Actions[rIndex];
                lAction._Link = null;

                mTarget.Actions.RemoveAt(rIndex);

                OnActionListItemSelect(rList);

                // Remove the asset that represents the action
                UnityEngine.Object.DestroyImmediate(lAction, true);

                mIsDirty = true;
            }
        }

        /// <summary>
        /// Allows us to process after the motions are reordered
        /// </summary>
        /// <param name="rList"></param>
        private void OnActionListReorder(ReorderableList rList)
        {
            mIsDirty = true;
        }

        /// <summary>
        /// Renders the currently selected step
        /// </summary>
        /// <param name="rStep"></param>
        private bool DrawActionDetailItem(NodeLinkAction rItem)
        {
            bool lIsDirty = false;
            if (rItem == null)
            {
                EditorGUILayout.LabelField("NULL");
                return false;
            }

            string lDescription = BaseDescriptionAttribute.GetDescription(rItem.GetType());
            if (lDescription.Length > 0) { NodeEditorStyle.DrawInspectorDescription(lDescription, MessageType.None); }

            // Render out the action specific inspectors
            bool lIsActionDirty = rItem.OnInspectorGUI(mTarget);
            if (lIsActionDirty) { lIsDirty = true; }

            if (lIsDirty)
            {
                EditorUtility.SetDirty(rItem);
            }

            return lIsDirty;
        }

        #endregion
    }
}
