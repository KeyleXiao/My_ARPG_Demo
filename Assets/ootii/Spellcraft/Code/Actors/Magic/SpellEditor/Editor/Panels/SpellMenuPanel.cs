using UnityEngine;
using UnityEditor;
using com.ootii.Helpers;
using com.ootii.Graphics.NodeGraph;

namespace com.ootii.Actors.Magic
{
    /// <summary>
    /// Panel that shows the properties of the selected node
    /// </summary>
    public class SpellMenuPanel : NodePanel
    {
        /// <summary>
        /// Determine if the spell is dirty or not
        /// </summary>
        public bool IsDirty = false;

        /// <summary>
        /// Draws the area to the window
        /// </summary>
        public override void Draw()
        {
            Rect lPosition = Position;
            GUI.BeginGroup(lPosition);

            Rect lButton = new Rect(3f, 1f, 80f, 30f);
            Rect lIcon = new Rect(lButton.x + 8f, 7f, 16f, 16f);
            Rect lText = new Rect(lIcon.x + lIcon.width + 3f, 5f, 50f, 20f);

            bool lClicked = GUI.Button(lButton, " ", NodeEditorStyle.Button);
            GUI.Label(lText, new GUIContent("New", "Create a spell"), NodeEditorStyle.PanelTitle);
            GUI.DrawTexture(lIcon, NodeEditorStyle.IconNew);

            if (lClicked)
            {
                ((SpellEditor)Editor).CreateSpell();
            }

            lButton = new Rect(83f, 1f, 80f, 30f);
            lIcon = new Rect(lButton.x + 8f, 7f, 16f, 16f);
            lText = new Rect(lIcon.x + lIcon.width + 3f, 5f, 50f, 20f);

            lClicked = GUI.Button(lButton, " ", NodeEditorStyle.Button);
            GUI.Label(lText, new GUIContent("Open", "Open a spell"), NodeEditorStyle.PanelTitle);
            GUI.DrawTexture(lIcon, NodeEditorStyle.IconOpen);

            if (lClicked)
            {
                ((SpellEditor)Editor).OpenSpell();
            }

            lButton = new Rect(175f, 1f, 80f, 30f);
            lIcon = new Rect(lButton.x + 8f, 7f, 16f, 16f);
            lText = new Rect(lIcon.x + lIcon.width + 3f, 5f, 50f, 20f);

            lClicked = GUI.Button(lButton, " ", (IsDirty ? NodeEditorStyle.ButtonGreen : NodeEditorStyle.Button));
            GUI.Label(lText, new GUIContent("Save", "Save the spell"), NodeEditorStyle.PanelTitle);
            GUI.DrawTexture(lIcon, NodeEditorStyle.IconSave);

            if (lClicked)
            {
                Spell lSpell = ((SpellEditor)Editor).Spell;
                if (lSpell != null)
                {
                    IsDirty = false;

                    EditorUtility.SetDirty(lSpell);
                    AssetDatabase.SaveAssets();
                }
            }

            lButton = new Rect(255f, 1f, 80f, 30f);
            lIcon = new Rect(lButton.x + 8f, 7f, 16f, 16f);
            lText = new Rect(lIcon.x + lIcon.width + 1f, 5f, 50f, 20f);

            lClicked = GUI.Button(lButton, " ", NodeEditorStyle.Button);
            GUI.Label(lText, new GUIContent("Delete", "Delete the spell"), NodeEditorStyle.PanelTitle);
            GUI.DrawTexture(lIcon, NodeEditorStyle.IconTrash);

            if (lClicked)
            {
                ((SpellEditor)Editor).DeleteSpell();
            }

            GUI.EndGroup();
        }
    }
}
