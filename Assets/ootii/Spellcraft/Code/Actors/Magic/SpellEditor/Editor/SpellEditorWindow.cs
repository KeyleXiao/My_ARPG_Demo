using UnityEngine;
using UnityEditor;
using com.ootii.Graphics.NodeGraph;

namespace com.ootii.Actors.Magic
{
    /// <summary>
    /// Window that represents the frame
    /// </summary>
    public class SpellEditorWindow : NodeEditorWindow
    {
        /// <summary>
        /// Provides global access to the window
        /// </summary>
        public new static SpellEditorWindow Instance = null;

        /// <summary>
        /// Opens the Node Editor window and loads the last session
        /// </summary>
        [MenuItem("Window/ootii Tools/Spell Editor")]
        public static SpellEditorWindow OpenSpellEditor()
        {
            //Debug.Log("SpellEditorWindow.OpenSpellEditor() Instance:" + (Instance == null ? "null" : "value"));

            if (EditorApplication.isCompiling)
            {
                //Debug.Log("Compiling ");
                return null;
            }

            Instance = GetWindow<SpellEditorWindow>("Spell Editor");
            Instance.minSize = new Vector2(400f, 300f);
            Instance.titleContent = new GUIContent("Spell Editor");

            Instance.Editor = new SpellEditor();
            //Instance.Editor = ScriptableObject.CreateInstance<SpellEditor>();
            Instance.Editor.Initialize(Instance.position.width, Instance.position.height);
            Instance.Editor.RepaintEvent = Instance.OnRepaint;

            Instance.wantsMouseMove = true;

            return Instance;
        }

        /// <summary>
        /// Automatically opens the window when the spell is selected.
        /// </summary>
        public static bool OpenSpellEditor(int rInstanceID)
        {
            if (rInstanceID != 0)
            {
                SpellEditorWindow.OpenSpellEditor();
                if (Instance != null)
                {
                    string lAssetPath = AssetDatabase.GetAssetPath(rInstanceID);

                    Instance.AssetPath = lAssetPath;
                    Instance.Editor.LoadRootAsset(lAssetPath);

                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Automatically opens the window when the spell is selected.
        /// </summary>
        [UnityEditor.Callbacks.OnOpenAsset(1)]
        private static bool AutoOpenCanvas(int rInstanceID, int rLine)
        {
            if (Selection.activeObject != null && Selection.activeObject is Spell)
            {
                SpellEditorWindow.OpenSpellEditor();
                if (Instance != null)
                {
                    string lAssetPath = AssetDatabase.GetAssetPath(rInstanceID);

                    Instance.AssetPath = lAssetPath;
                    Instance.Editor.LoadRootAsset(lAssetPath);

                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Called multiple times per second on all visible windows.
        /// </summary>
        protected override void OnEnable()
        {
            //Debug.Log("SpellEditorWindow.OnEnable() Instance:" + (Instance == null ? "null" : "value"));

            if (Instance == null)
            {
                //Instance = GetWindow<SpellEditorWindow>("Spell Editor");
                Instance = this;
            }

            if (Editor == null)
            {
                //Debug.Log("   Create Editor");

                if (EditorApplication.isCompiling)
                {
                    //Debug.Log("   Compiling ");
                    return;
                }

                Editor = new SpellEditor();
                //Editor = ScriptableObject.CreateInstance<SpellEditor>();
                Editor.Initialize(Instance.position.width, Instance.position.height);
                Editor.RepaintEvent = Instance.OnRepaint;
            }

            string lPath = Editor.RootAssetPath;
            if (lPath.Length == 0) { lPath = AssetPath; }
            if (lPath.Length > 0) { Editor.LoadRootAsset(lPath); }
        }
    }
}
