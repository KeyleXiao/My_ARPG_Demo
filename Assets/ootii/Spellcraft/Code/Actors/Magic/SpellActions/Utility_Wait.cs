using System;
using com.ootii.Base;
using com.ootii.Graphics.NodeGraph;
using com.ootii.Helpers;

namespace com.ootii.Actors.Magic
{
    [Serializable]
    [BaseName("Utility - Wait")]
    [BaseDescription("Waits a specified number of seconds before completing.")]
    public class Utility_Wait : SpellAction
    {
        /// <summary>
        /// Time in seconds to wait
        /// </summary>
        public float _Time = 0f;
        public float Time
        {
            get { return _Time; }
            set { _Time = value; }
        }

        /// <summary>
        /// Max time in seconds to wait
        /// </summary>
        public float _MaxTime = 0f;
        public float MaxTime
        {
            get { return _MaxTime; }
            set { _MaxTime = value; }
        }

        // Time to actually wait
        protected float mTime = 0f;

        // Time we've waited so far
        protected float mElapsedTime = 0f;

        /// <summary>
        /// Used to initialize any actions prior to them being activated
        /// </summary>
        public override void Awake()
        {
            base.Awake();
            //Utilities.Debug.Log.FileWrite("Utility_Wait awake elapsed:" + mElapsedTime.ToString("f3") + " max:" + Time.ToString("f3"));

            _DeactivationIndex = EnumSpellActionDeactivation.MANAGED;
        }

        /// <summary>
        /// Called when the action is first activated
        /// <param name="rPreviousSpellActionState">State of the action prior to this one activating</param>
        public override void Activate(int rPreviousSpellActionState = -1, object rData = null)
        {
            mElapsedTime = 0f;
            
            mNode.Data = (rData != _Spell.Data ? rData : null);

            mTime = (_MaxTime == 0f ? _Time : UnityEngine.Random.Range(_Time, _MaxTime));

            //Utilities.Debug.Log.FileWrite("Utility_Wait activate elapsed:" + mElapsedTime.ToString("f3") + " max:" + Time.ToString("f3"));

            base.Activate(rPreviousSpellActionState, rData);
        }

        /// <summary>
        /// Runs each frame to see if the action should continue
        /// </summary>
        public override void Update()
        {
            mElapsedTime = mElapsedTime + UnityEngine.Time.deltaTime;

            // Determine if we've waited long enough
            if (mElapsedTime >= mTime)
            {
                Deactivate();
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

            // Damage
            UnityEngine.GUILayout.BeginHorizontal();

            UnityEditor.EditorGUILayout.LabelField(new UnityEngine.GUIContent("Time", "Min and Max time to wait. When Max is > 0, a random time will be chosen between the Min and Max."), UnityEngine.GUILayout.Width(UnityEditor.EditorGUIUtility.labelWidth));

            if (EditorHelper.FloatField(Time, "Min Time", rTarget, 0f, 20f))
            {
                lIsDirty = true;
                Time = EditorHelper.FieldFloatValue;
            }

            if (EditorHelper.FloatField(MaxTime, "Max Time", rTarget, 0f, 20f))
            {
                lIsDirty = true;
                MaxTime = EditorHelper.FieldFloatValue;
            }

            UnityEngine.GUILayout.EndHorizontal();

            return lIsDirty;
        }

#endif

        #endregion
    }
}