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
    [BaseName("Add Material")]
    [BaseDescription("Adds a material instance to a GameObject's renderer.")]
    public class AddMaterial : SpellAction
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
        /// Material that we'll add to the target
        /// </summary>
        public Material _Material = null;
        public Material Material
        {
            get { return _Material; }
            set { _Material = value; }
        }

        /// <summary>
        /// Default constructor
        /// </summary>
        public AddMaterial() : base()
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
                    Material[] lMaterials = new Material[lRenderer.materials.Length + 1];
                    Array.Copy(lRenderer.materials, lMaterials, lRenderer.materials.Length);

                    lMaterials[lRenderer.materials.Length] = Material.Instantiate(_Material);

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

            if (EditorHelper.ObjectField<Material>("Material", "Material to add to the target", Material, rTarget))
            {
                lIsDirty = true;
                Material = EditorHelper.FieldObjectValue as Material;
            }

            return lIsDirty;
        }

#endif

        #endregion
    }
}
