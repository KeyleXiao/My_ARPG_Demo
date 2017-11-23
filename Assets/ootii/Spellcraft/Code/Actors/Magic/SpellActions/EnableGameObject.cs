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
    [BaseName("Enable Game Object")]
    [BaseDescription("Enables (or disables) a targeted GameObject. Use the name to select a global GameObject without targeting.")]
    public class EnableGameObject : SpellAction
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
        /// Name of the Game Object to target
        /// </summary>
        public string _TargetName = "";
        public string TargetName
        {
            get { return _TargetName; }
            set { _TargetName = value; }
        }

        /// <summary>
        /// Determines if we are enabling or disabling the target
        /// </summary>
        public bool _EnableTarget = true;
        public bool EnableTarget
        {
            get { return _EnableTarget; }
            set { _EnableTarget = value; }
        }

        /// <summary>
        /// Default constructor
        /// </summary>
        public EnableGameObject() : base()
        {
            _DeactivationIndex = EnumSpellActionDeactivation.IMMEDIATELY;
        }

        /// <summary>
        /// Called when the action is first activated
        /// <param name="rPreviousSpellActionState">State of the action prior to this one activating</param>
        public override void Activate(int rPreviousSpellActionState = -1, object rData = null)
        {
            if (_TargetName.Length == 0)
            {
                GetBestTargets(TargetTypeIndex, rData, _Spell.Data, ActivateInstance);
            }
            else
            {
                GameObject lGameObject = GameObject.Find(_TargetName);
                if (lGameObject != null)
                {
                    ActivateInstance(lGameObject, rData);
                }
                else
                {
                    // Since GameObject.Find() won't find disabled objects, we need to do this trick
                    Transform[] lObjects = Resources.FindObjectsOfTypeAll<Transform>();
                    for (int i = 0; i < lObjects.Length; i++)
                    {
                        if (lObjects[i].name == _TargetName)
                        {
                            ActivateInstance(lObjects[i].gameObject, rData);
                        }
                    }
                }
            }

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
                rTarget.SetActive(_EnableTarget);
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

            if (EditorHelper.PopUpField("Target Type", "Determines the target(s) we'll do the test on.", TargetTypeIndex, ActivateMotion.GetBestTargetTypes, rTarget))
            {
                lIsDirty = true;
                TargetTypeIndex = EditorHelper.FieldIntValue;
            }

            if (EditorHelper.TextField("Target Name", "Name of the GameObject to be enabled or disabled. This overrides any targets that were set by other actions.", TargetName, rTarget))
            {
                lIsDirty = true;
                TargetName = EditorHelper.FieldStringValue;
            }

            if (EditorHelper.BoolField("Enable Target", "Determines if we are enabling or disabling the target", EnableTarget, rTarget))
            {
                lIsDirty = true;
                EnableTarget = EditorHelper.FieldBoolValue;
            }

            return lIsDirty;
        }

#endif

        #endregion
    }
}