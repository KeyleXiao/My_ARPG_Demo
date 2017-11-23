using UnityEngine;
using UnityEditor;
using com.ootii.Graphics.NodeGraph;

namespace com.ootii.Actors.Magic
{
    /// <summary>
    /// Editor represents the entire editor including canvas and panels
    /// </summary>
    public class SpellEditor : NodeEditor
    {
        /// <summary>
        /// Spell that we're editing.
        /// </summary>
        public Spell Spell = null;

        /// <summary>
        /// Panel to show buttons and menu
        /// </summary>
        public SpellMenuPanel SpellMenuPanel = null;

        /// <summary>
        /// Panel to edit the spell information
        /// </summary>
        public SpellPanel SpellPanel = null;

        /// <summary>
        /// Panel to edit the spell action
        /// </summary>
        public SpellNodePanel SpellNodePanel = null;

        /// <summary>
        /// Panel to edit the spell link
        /// </summary>
        public SpellLinkPanel SpellLinkPanel = null;

        /// <summary>
        /// Initializes the editor
        /// </summary>
        public override void Initialize(float rWidth, float rHeight)
        {
            base.Initialize(rWidth, rHeight);


            SpellMenuPanel = new SpellMenuPanel();
            //SpellMenuPanel = ScriptableObject.CreateInstance<SpellMenuPanel>();
            SpellMenuPanel.Title = "";
            SpellMenuPanel.Editor = this;
            SpellMenuPanel.Position = new Rect(5f, 5f, 340f, 30f);
            mPanels.Add(SpellMenuPanel);

            SpellPanel = new SpellPanel();
            //SpellPanel = ScriptableObject.CreateInstance<SpellPanel>();
            SpellPanel.Title = "Spell";
            SpellPanel.Editor = this;
            SpellPanel.Position = new Rect(SpellMenuPanel.Position.xMin, SpellMenuPanel.Position.yMax + 3f, SpellPanel.Position.width, SpellPanel.Position.height);
            mPanels.Add(SpellPanel);

            SpellNodePanel = new SpellNodePanel();
            //SpellNodePanel = ScriptableObject.CreateInstance<SpellNodePanel>();
            SpellNodePanel.IsEnabled = false;
            SpellNodePanel.Title = "Spell Action";
            SpellNodePanel.Editor = this;
            SpellNodePanel.Position = new Rect(SpellPanel.Position.xMin, SpellPanel.Position.yMax - 5f, SpellPanel.Position.width, SpellNodePanel.Position.height);
            mPanels.Add(SpellNodePanel);

            SpellLinkPanel = new SpellLinkPanel();
            //SpellLinkPanel = ScriptableObject.CreateInstance<SpellLinkPanel>();
            SpellLinkPanel.IsEnabled = false;
            SpellLinkPanel.Title = "Spell Link ";
            SpellLinkPanel.Editor = this;
            SpellLinkPanel.Position = new Rect(SpellPanel.Position.xMin, SpellPanel.Position.yMax - 5f, SpellPanel.Position.width, 450f);
            mPanels.Add(SpellLinkPanel);

            mCanvas.NodeAddedEvent += SpellPanel.OnNodeAdded;
            mCanvas.NodeRemovedEvent += SpellPanel.OnNodeRemoved;
            mCanvas.LinkAddedEvent += SpellPanel.OnLinkAdded;
            mCanvas.LinkRemovedEvent += SpellPanel.OnLinkRemoved;
            mCanvas.NodeSelectedEvent += SpellNodePanel.OnCanvasNodeSelected;
            mCanvas.LinkSelectedEvent += SpellLinkPanel.OnCanvasLinkSelected;
        }

        /// <summary>
        /// Opens an existing spell
        /// </summary>
        /// <returns>Spell that was opened</returns>
        public Spell OpenSpell()
        {
            Spell lSpell = null;

            string lPath = EditorUtility.OpenFilePanel("Open Spell", "Assets/ootii/Spellcraft/Content/Data/Spells", "asset");
            if (lPath != null && lPath.Length > 0)
            {
                RootAssetPath = lPath.Replace(Application.dataPath, "Assets");
                RootAsset = AssetDatabase.LoadAssetAtPath<Spell>(RootAssetPath);

                lSpell = RootAsset as Spell;
                LoadSpell(lSpell, true);
            }

            return lSpell;
        }

        /// <summary>
        /// Creates a spell asset and loads it in the editor
        /// </summary>
        /// <returns>Spell that was created</returns>
        public Spell CreateSpell()
        {
            Spell lSpell = null;

            string lPath = EditorUtility.SaveFilePanel("Create Spell", "Assets/ootii/Spellcraft/Content/Data/Spells", "NewSpell", "asset");
            if (lPath != null && lPath.Length > 0)
            {
                RootAssetPath = lPath.Replace(Application.dataPath, "Assets");

                // Name the spell
                string lFileName = RootAssetPath.Substring(RootAssetPath.LastIndexOf("/") + 1);
                lFileName = lFileName.Replace(".asset", "");

                // Create the spell
                lSpell = ScriptableObject.CreateInstance<Spell>();
                lSpell.Name = lFileName;

                // Save the spell
                AssetDatabase.CreateAsset(lSpell, RootAssetPath);

                LoadSpell(lSpell);
            }

            return lSpell;
        }

        /// <summary>
        /// Delets an existing spell
        /// </summary>
        /// <returns>Spell that was opened</returns>
        public void DeleteSpell()
        {
            if (EditorUtility.DisplayDialog(" Spell Editor", "Delete this spell permanently?", "OK", "Cancel"))
            {
                AssetDatabase.DeleteAsset(RootAssetPath);

                RootAssetPath = "";
                LoadSpell(null);
            }
        }

        /// <summary>
        /// Loads the spell at the specified path
        /// </summary>
        /// <param name="rAssetPath">Path to the spell to edit</param>
        public override void LoadRootAsset(string rAssetPath)
        {
            RootAssetPath = rAssetPath;
            RootAsset = AssetDatabase.LoadAssetAtPath<Spell>(RootAssetPath);

            Spell lSpell = RootAsset as Spell;
            LoadSpell(lSpell);
        }

        /// <summary>
        /// Loads the specified spell in the editor
        /// </summary>
        /// <param name="rSpell">Spell to edit</param>
        public void LoadSpell(Spell rSpell, bool rForceRefresh = false)
        {
            if (!rForceRefresh && Spell == rSpell) { return; }

            RootAsset = rSpell;
            Spell = rSpell;

            mCanvas.Clear();

            SpellMenuPanel.IsDirty = false;
            SpellNodePanel.IsEnabled = false;
            SpellLinkPanel.IsEnabled = false;

            if (Spell != null)
            {
                // Extract out the nodes and add them to the canvas
                UnityEngine.Object[] lObjects = UnityEditor.AssetDatabase.LoadAllAssetsAtPath(RootAssetPath);
                for (int i = 0; i < lObjects.Length; i++)
                {
                    Node lNode = lObjects[i] as Node;
                    if (lNode != null)
                    {
                        // Ensure the links are valid and remove them if not
                        for (int j = lNode.Links.Count - 1; j >= 0; j--)
                        {
                            NodeLink lLink = lNode.Links[j];
                            if (lLink == null || lLink.StartNode == null || lLink.EndNode == null)
                            {
                                Debug.Log("SpellEditor.LoadSpell - removing Link link:" + lLink.name);
                                lNode.Links.RemoveAt(j);
                            }
                        }

                        // Add the node to the canvas
                        lNode.Canvas = mCanvas;
                        mCanvas.Nodes.Add(lNode);
                    }
                }
            }

            // Set the asset path so when the window reloads, we can load it
            SpellEditorWindow lWindow = SpellEditorWindow.Instance; // EditorWindow.GetWindow<SpellEditorWindow>();
            if (lWindow != null) { lWindow.AssetPath = RootAssetPath; }

            Repaint();
        }

        /// <summary>
        /// Used to inform the editor that the object being managed is dirty. However,
        /// this does not set the ScriptableObject itself.
        /// </summary>
        /// <param name="rIsDirty"></param>
        public override void SetDirty(bool rIsDirty = true)
        {
            SpellMenuPanel.IsDirty = rIsDirty;
        }
    }
}