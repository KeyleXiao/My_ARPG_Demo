using System;
using com.ootii.Base;
using com.ootii.Game;
using com.ootii.Helpers;

namespace com.ootii.Actors.Magic
{
    [Serializable]
    [BaseName("Utility - Show Reticle")]
    [BaseDescription("Shows or hides the reticle based on the settings.")]
    public class Utility_ShowReticle : SpellAction
    {
        // States we can set with this action
        private static string[] ReticleStates = new string[] { "Show", "Hide", "Default" };

        /// <summary>
        /// Determine if we're showing or hiding the reticle
        /// </summary>
        public int _ReticleStateIndex = 0;
        public int ReticleStateIndex
        {
            get { return _ReticleStateIndex; }
            set { _ReticleStateIndex = value; }
        }

        /// <summary>
        /// Used to initialize any actions prior to them being activated
        /// </summary>
        public override void Awake()
        {
            _DeactivationIndex = EnumSpellActionDeactivation.IMMEDIATELY;
            base.Awake();
        }

        /// <summary>
        /// Called when the action is first activated
        /// <param name="rPreviousSpellActionState">State of the action prior to this one activating</param>
        public override void Activate(int rPreviousSpellActionState = -1, object rData = null)
        {
            base.Activate(rPreviousSpellActionState, rData);

            // Set the visibility
            if (TargetingReticle.Instance != null)
            {
                if (_ReticleStateIndex == 0)
                {
                    TargetingReticle.Instance.IsVisible = true;
                }
                else if (_ReticleStateIndex == 1)
                {
                    TargetingReticle.Instance.IsVisible = false;
                }
                else if (_ReticleStateIndex == 2)
                {
                    TargetingReticle.Instance.IsVisible = TargetingReticle.DefaultIsVisible;
                }
            }

            // Immediately deactivate
            Deactivate();
        }

        #region Editor Functions

#if UNITY_EDITOR

        /// <summary>
        /// Called when the inspector needs to draw
        /// </summary>
        public override bool OnInspectorGUI(UnityEngine.Object rTarget)
        {
            bool lIsDirty = base.OnInspectorGUI(rTarget);

            EditorHelper.DrawLine();

            if (EditorHelper.PopUpField("Visibility", "Sets the visibility of the reticle.", ReticleStateIndex, ReticleStates, rTarget))
            {
                lIsDirty = true;
                ReticleStateIndex = EditorHelper.FieldIntValue;
            }

            return lIsDirty;
        }

#endif

        #endregion
    }
}