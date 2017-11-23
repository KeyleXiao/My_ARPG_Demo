using System;
using com.ootii.Base;
using com.ootii.Graphics.NodeGraph;
using com.ootii.Helpers;

namespace com.ootii.Actors.Magic
{
    [Serializable]
    [BaseName("Utility - Wait For Spell State")]
    [BaseDescription("Waits for a specific spell state to occur.")]
    public class Utility_WaitForSpellState : SpellAction
    {
        /// <summary>
        /// Time in seconds to wait
        /// </summary>
        public float _Time = 5f;
        public float Time
        {
            get { return _Time; }
            set { _Time = value; }
        }

        /// <summary>
        /// State we're waiting for
        /// </summary>
        public int _SpellState = EnumSpellState.SPELL_CAST;
        public int SpellState
        {
            get { return _SpellState; }
            set { _SpellState = value; }
        }

        // Time we've waited so far
        protected float mElapsedTime = 0f;

        /// <summary>
        /// Used to initialize any actions prior to them being activated
        /// </summary>
        public override void Awake()
        {
            base.Awake();

            _DeactivationIndex = EnumSpellActionDeactivation.MANAGED;
        }

        /// <summary>
        /// Called when the action is first activated
        /// <param name="rPreviousSpellActionState">State of the action prior to this one activating</param>
        public override void Activate(int rPreviousSpellActionState = -1, object rData = null)
        {
            mElapsedTime = 0f;

            base.Activate(rPreviousSpellActionState, rData);
        }

        /// <summary>
        /// Runs each frame to see if the action should continue
        /// </summary>
        public override void Update()
        {
            mElapsedTime = mElapsedTime + UnityEngine.Time.deltaTime;

            // Determine if we hit a matching state
            if (_SpellState == _Spell.State)
            {
                OnSuccess();
            }

            // Determine if we've waited long enough
            if (Time > 0f && mElapsedTime >= Time)
            {
                OnFailure();
            }
        }

        #region Editor Functions

#if UNITY_EDITOR

        /// <summary>
        /// Called when the inspector needs to draw
        /// </summary>
        public override bool OnInspectorGUI(UnityEngine.Object rTarget)
        {
            mEditorShowDeactivationField = false;
            bool lIsDirty = base.OnInspectorGUI(rTarget);

            NodeEditorStyle.DrawLine(NodeEditorStyle.LineBlue);

            if (EditorHelper.PopUpField("Spell State", "State of the spell we were comparing to. If equal, the test passes.", SpellState, EnumSpellState.Names, rTarget))
            {
                lIsDirty = true;
                SpellState = EditorHelper.FieldIntValue;
            }

            if (EditorHelper.FloatField("Expire Time", "Max amount of time we'll wait.", Time, rTarget))
            {
                lIsDirty = true;
                Time = EditorHelper.FieldFloatValue;
            }

            return lIsDirty;
        }

#endif

        #endregion
    }
}