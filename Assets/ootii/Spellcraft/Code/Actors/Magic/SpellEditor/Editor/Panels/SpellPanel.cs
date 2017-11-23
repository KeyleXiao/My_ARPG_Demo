using UnityEngine;
using UnityEditor;
using com.ootii.Helpers;
using com.ootii.Graphics.NodeGraph;

namespace com.ootii.Actors.Magic
{
    /// <summary>
    /// Panel that shows the properties of the selected node
    /// </summary>
    public class SpellPanel : NodePanel
    {
        /// <summary>
        /// Default constructor
        /// </summary>
        public SpellPanel() : base()
        {
            Position.width = 220f;
            Position.height = 144f;
        }

        /// <summary>
        /// Draws the area to the window
        /// </summary>
        public override void Draw()
        {
            if (((SpellEditor)Editor).Spell != null)
            {
                base.Draw();
            }
        }

        /// <summary>
        /// Draws in the content area of the window
        /// </summary>
        public override void DrawContent()
        {
            Spell lSpell = ((SpellEditor)Editor).Spell;
            if (lSpell == null) { return; }

            bool lIsDirty = false;

            if (EditorHelper.TextField("Name", "Name of the spell.", lSpell.Name, lSpell))
            {
                lIsDirty = true;
                lSpell.Name = EditorHelper.FieldStringValue;
            }

            string lDescription = GUILayout.TextArea(lSpell.Description, GUILayout.Height(30f));
            if (lDescription != lSpell.Description)
            { 
                lIsDirty = true;
                lSpell.Description = lDescription;
            }

            GUILayout.Space(5f);

            GUILayout.BeginHorizontal();

            if (EditorHelper.IntField("Casting Style", "Motion Controller parameter used to determine which casting animation to use.", lSpell.CastingStyle, lSpell, 50f))
            {
                lIsDirty = true;
                lSpell.CastingStyle = EditorHelper.FieldIntValue;
            }

            //EditorHelper.LabelField("Pause", "Determines if the casting will be paused so the player can activate it.", 40f);
            //if (EditorHelper.BoolField(lSpell.CastingPause, "Pause", lSpell, 16f))
            //{
            //    lIsDirty = true;
            //    lSpell.CastingPause = EditorHelper.FieldBoolValue;
            //}

            GUILayout.EndHorizontal();

            if (EditorHelper.BoolField("Show Debug", "Determines if we show debug information about the spell.", lSpell.ShowDebug, lSpell))
            {
                lIsDirty = true;
                lSpell.ShowDebug = EditorHelper.FieldBoolValue;
            }

            // Flag the spell as dirty so it will be saved
            if (lIsDirty)
            {
                Editor.SetDirty();
                EditorUtility.SetDirty(lSpell);
            }
        }

        /// <summary>
        /// Allows us to react when a node is added to the canvas.
        /// </summary>
        /// <param name="rNode">Node that is added</param>
        public void OnNodeAdded(Node rNode)
        {
            Editor.SetDirty();
        }

        /// <summary>
        /// Allows us to react when a node is removed from the canvas.
        /// </summary>
        /// <param name="rNode">Node that is being removed</param>
        public void OnNodeRemoved(Node rNode)
        {
            if (rNode == null) { return; }

            Spell lSpell = ((SpellEditor)Editor).Spell;
            if (lSpell == null) { return; }

            if (lSpell.StartNodes == null) { return; }

            // Remove the node from the start nodes if it is there
            if (lSpell.StartNodes.Contains(rNode))
            {
                lSpell.StartNodes.Remove(rNode);
                EditorUtility.SetDirty(lSpell);
            }

            // Remove the node from the end nodes if it is there
            if (lSpell.EndNodes.Contains(rNode))
            {
                lSpell.EndNodes.Remove(rNode);
                EditorUtility.SetDirty(lSpell);
            }

            Editor.SetDirty();
        }

        /// <summary>
        /// Allows us to react when a link is added to the canvas.
        /// </summary>
        /// <param name="rLink">Link that is added</param>
        public void OnLinkAdded(NodeLink rLink)
        {
            Editor.SetDirty();
        }

        /// <summary>
        /// Allows us to react when a link is removed from the canvas.
        /// </summary>
        /// <param name="rLink">Link that is being removed</param>
        public void OnLinkRemoved(NodeLink rLink)
        {
            Editor.SetDirty();
        }
    }
}
