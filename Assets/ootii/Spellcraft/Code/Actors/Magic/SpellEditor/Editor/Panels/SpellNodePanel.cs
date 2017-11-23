using System;
using UnityEngine;
using UnityEditor;
using com.ootii.Base;
using com.ootii.Graphics.NodeGraph;
using com.ootii.Helpers;

namespace com.ootii.Actors.Magic
{
    /// <summary>
    /// Panel that shows the properties of the selected node
    /// </summary>
    public class SpellNodePanel : NodePanel
    {
        /// <summary>
        /// Default constructor
        /// </summary>
        public SpellNodePanel() : base()
        {
            IsEnabled = false;

            Position.width = 220f;
            Position.height = 450f;
        }

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
        /// Draws in the title area of the window
        /// </summary>
        public override void DrawTitle()
        {
            if (Editor.Canvas.SelectedNode != null && Editor.Canvas.SelectedNode.Content != null)
            {
                string lTitle = "";

                SpellAction lAction = Editor.Canvas.SelectedNode.Content as SpellAction;
                if (lAction != null) { lTitle = lAction.Name; }

                if (lTitle.Length == 0) { lTitle = BaseNameAttribute.GetName(Editor.Canvas.SelectedNode.Content.GetType()); }
                GUI.Label(new Rect(12f, 8f, Position.width - 12f, 20f), lTitle, PanelTitleStyle);

                if (GUI.Button(new Rect(Position.width - 31f, 10f, 16f, 16f), new GUIContent(" ", "Remove Spell Action"), NodeEditorStyle.ButtonX))
                {
                    Editor.Canvas.SelectedNode.DestroyContent();
                }
            }
            else
            {
                GUI.Label(new Rect(12f, 8f, Position.width - 12f, 20f), Title, PanelTitleStyle);
            }
        }

        /// <summary>
        /// Draws in the content area of the window
        /// </summary>
        public override void DrawContent()
        {
            if (Editor.Canvas.SelectedNode == null) { return; }

            bool lIsDirty = false;

            if (Editor.Canvas.SelectedNode.Content != null)
            {
                string lDescription = BaseDescriptionAttribute.GetDescription(Editor.Canvas.SelectedNode.Content.GetType());
                if (lDescription != null && lDescription.Length > 0)
                {
                    NodeEditorStyle.DrawInspectorDescription(lDescription, MessageType.None);
                }
            }

            GUILayout.BeginHorizontal();

            if (EditorHelper.BoolField("Is Start Node", "Determines if the node is used as a starting node.", Editor.Canvas.SelectedNode.IsStartNode, Editor.RootAsset))
            {
                lIsDirty = true;
                Editor.Canvas.SelectedNode.IsStartNode = EditorHelper.FieldBoolValue;

                Spell lSpell = ((SpellEditor)Editor).Spell;
                if (lSpell != null)
                {
                    if (lSpell.StartNodes == null) { lSpell.StartNodes = new System.Collections.Generic.List<Node>(); }
                    if (lSpell.StartNodes.Contains(Editor.Canvas.SelectedNode))
                    {
                        if (!Editor.Canvas.SelectedNode.IsStartNode)
                        {
                            lSpell.StartNodes.Remove(Editor.Canvas.SelectedNode);
                            UnityEditor.EditorUtility.SetDirty(lSpell);
                        }
                    }
                    else if (Editor.Canvas.SelectedNode.IsStartNode)
                    {
                        lSpell.StartNodes.Add(Editor.Canvas.SelectedNode);
                        UnityEditor.EditorUtility.SetDirty(lSpell);
                    }
                }
            }

            EditorHelper.LabelField("End Node", "Determines if the node is used as an ending node.", 60f);
            if (EditorHelper.BoolField(Editor.Canvas.SelectedNode.IsEndNode, "End Node", Editor.RootAsset, 16f))
            {
                lIsDirty = true;
                Editor.Canvas.SelectedNode.IsEndNode = EditorHelper.FieldBoolValue;

                Spell lSpell = ((SpellEditor)Editor).Spell;
                if (lSpell != null)
                {
                    if (lSpell.EndNodes == null) { lSpell.EndNodes = new System.Collections.Generic.List<Node>(); }
                    if (lSpell.EndNodes.Contains(Editor.Canvas.SelectedNode))
                    {
                        if (!Editor.Canvas.SelectedNode.IsEndNode)
                        {
                            lSpell.EndNodes.Remove(Editor.Canvas.SelectedNode);
                            UnityEditor.EditorUtility.SetDirty(lSpell);
                        }
                    }
                    else if (Editor.Canvas.SelectedNode.IsEndNode)
                    {
                        lSpell.EndNodes.Add(Editor.Canvas.SelectedNode);
                        UnityEditor.EditorUtility.SetDirty(lSpell);
                    }
                }
            }

            GUILayout.EndHorizontal();

            if (Editor.Canvas.SelectedNode.Content == null)
            {
                EditorGUILayout.HelpBox(" Select an action for this node to take. Actions will be used to drive how the spell works.", MessageType.None);

                if (GUILayout.Button(new GUIContent("Select")))
                {
                    TypeSelectWindow lWindow = EditorWindow.GetWindow(typeof(TypeSelectWindow), false, "Select", true) as TypeSelectWindow;
                    lWindow.BaseType = typeof(SpellAction);
                    lWindow.SelectedEvent = OnTypeSelected;
                }
            }
            else
            {
                SpellAction lAction = Editor.Canvas.SelectedNode.Content as SpellAction;
                if (lAction != null)
                {
                    bool lIsActionDirty = lAction.OnInspectorGUI(Editor.RootAsset);
                    if (lIsActionDirty) { lIsDirty = true; }
                }
            }

            // If the node is dirty, we need to report it
            if (lIsDirty)
            {
                UnityEditor.EditorUtility.SetDirty(Editor.Canvas.SelectedNode);

                if (Editor.Canvas.SelectedNode._Content != null)
                {
                    UnityEditor.EditorUtility.SetDirty(Editor.Canvas.SelectedNode._Content);
                }

                Editor.SetDirty();
            }
        }

        /// <summary>
        /// Raised when a canvas node is selected or when the selection is cleared
        /// </summary>
        /// <param name="rNode">Node that is selected or null if deselected</param>
        public override void OnCanvasNodeSelected(Node rNode)
        {
            IsEnabled = (rNode != null);
        }

        /// <summary>
        /// Raised when a type is selected from the editor window
        /// </summary>
        /// <param name="rType">Type that was retrieved</param>
        /// <param name="rUserData">UserData set during opening</param>
        private void OnTypeSelected(Type rType, object rUserData)
        {
            if (rType == null) { return; }
            if (Editor.Canvas.SelectedNode == null) { return; }

            Editor.Canvas.SelectedNode.CreateContent(rType);                       
        }
    }
}
