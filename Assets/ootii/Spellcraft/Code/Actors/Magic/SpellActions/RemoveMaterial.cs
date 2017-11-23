using System;
using UnityEngine;
using com.ootii.Base;
using com.ootii.Graphics.NodeGraph;
using com.ootii.Helpers;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace com.ootii.Actors.Magic
{
    [Serializable]
    [BaseName("Remove Material")]
    [BaseDescription("Removes a material from a GameObject's renderer based on the index.")]
    public class RemoveMaterial : SpellAction
    {
        /// <summary>
        /// Determines the actual target source
        /// </summary>
        public int _TargetTypeIndex = 0;
        public int TargetTypeIndex
        {
            get { return _TargetTypeIndex; }
            set { _TargetTypeIndex = value; }
        }

        /// <summary>
        /// Index of the material to remove
        /// </summary>
        public int _Index = -1;
        public int Index
        {
            get { return _Index; }
            set { _Index = value; }
        }

        /// <summary>
        /// Default constructor
        /// </summary>
        public RemoveMaterial() : base()
        {
            _DeactivationIndex = EnumSpellActionDeactivation.IMMEDIATELY;
        }

        /// <summary>
        /// Called when the action is first activated
        /// <param name="rPreviousSpellActionState">State of the action prior to this one activating</param>
        public override void Activate(int rPreviousSpellActionState = -1, object rData = null)
        {
            GetBestTargets(TargetTypeIndex, rData, _Spell.Data, ActivateInstance);

            base.Activate(rPreviousSpellActionState, rData);
        }

        /// <summary>
        /// Activates a single target by running the links for that target
        /// </summary>
        /// <param name="rTarget"></param>
        protected bool ActivateInstance(GameObject rTarget, object rData)
        {
            if (rTarget != null)
            {
                Renderer lRenderer = rTarget.GetComponent<Renderer>();
                if (lRenderer == null) { lRenderer = rTarget.GetComponentInChildren<Renderer>(); }

                if (lRenderer != null)
                {
                    int lNewIndex = 0;
                    int lIndex = _Index;

                    Material[] lMaterials = new Material[lRenderer.materials.Length - 1];
                    if (lIndex < 0 || lIndex >= lRenderer.materials.Length) { lIndex = lRenderer.materials.Length - 1; }

                    for (int i = 0; i < lRenderer.materials.Length; i++)
                    {
                        if (i != lIndex)
                        {
                            lMaterials[lNewIndex] = lRenderer.materials[i];
                            lNewIndex++;
                        }
                    }

                    lRenderer.materials = lMaterials;
                }
            }

            return true;
        }

        #region Editor Functions

#if UNITY_EDITOR

        /// <summary>
        /// Called when the inspector needs to draw
        /// </summary>
        public override bool OnInspectorGUI(UnityEngine.Object rTarget)
        {
            bool lIsDirty = base.OnInspectorGUI(rTarget);

            NodeEditorStyle.DrawLine(NodeEditorStyle.LineBlue);

            if (EditorHelper.PopUpField("Target Type", "Determines the target(s) we'll modify.", TargetTypeIndex, ActivateMotion.GetBestTargetTypes, rTarget))
            {
                lIsDirty = true;
                TargetTypeIndex = EditorHelper.FieldIntValue;
            }

            if (EditorHelper.IntField("Material Index", "Index of the material to use. Use -1 to remove the last material.", Index, rTarget))
            {
                lIsDirty = true;
                Index = EditorHelper.FieldIntValue;
            }

            return lIsDirty;
        }

#endif

        #endregion
    }
}
