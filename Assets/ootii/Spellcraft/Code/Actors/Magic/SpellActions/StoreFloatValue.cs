using System;
using System.Collections.Generic;
using UnityEngine;
using com.ootii.Base;
using com.ootii.Helpers;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace com.ootii.Actors.Magic
{
    [Serializable]
    [BaseName("Store Float Value")]
    [BaseDescription("Stores a float value in the Spell.Data.FloatValues array based on the index set.")]
    public class StoreFloatValue : SpellAction
    {
        /// <summary>
        /// Index to store the float value in
        /// </summary>
        public int _Index = 0;
        public int Index
        {
            get { return _Index; }
            set { _Index = value; }
        }

        /// <summary>
        /// Min value that is to be stored
        /// </summary>
        public float _MinValue = 0f;
        public float MinValue
        {
            get { return _MinValue; }
            set { _MinValue = value; }
        }

        /// <summary>
        /// Max value that is to be stored
        /// </summary>
        public float _MaxValue = 0f;
        public float MaxValue
        {
            get { return _MaxValue; }
            set { _MaxValue = value; }
        }

        /// <summary>
        /// Called when the action is first activated
        /// <param name="rPreviousSpellActionState">State of the action prior to this one activating</param>
        public override void Activate(int rPreviousSpellActionState = -1, object rData = null)
        {
            base.Activate(rPreviousSpellActionState, rData);

            if (_Spell.Data.FloatValues == null) { _Spell.Data.FloatValues = new List<float>(); }
            while (_Spell.Data.FloatValues.Count <= _Index) { _Spell.Data.FloatValues.Add(0f); }

            _Spell.Data.FloatValues[_Index] = UnityEngine.Random.Range(_MinValue, _MaxValue);

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
            bool lIsDirty = false;

            if (EditorHelper.IntField("Value Index", "Index into the spell data values.", Index, rTarget))
            {
                lIsDirty = true;
                Index = EditorHelper.FieldIntValue;
            }
            
            // Value
            GUILayout.BeginHorizontal();

            EditorGUILayout.LabelField(new GUIContent("Value", "Min and max value to store."), GUILayout.Width(EditorGUIUtility.labelWidth - 4f));

            if (EditorHelper.FloatField(MinValue, "Min Value", rTarget, 0f, 20f))
            {
                lIsDirty = true;
                MinValue = EditorHelper.FieldFloatValue;
            }

            if (EditorHelper.FloatField(MaxValue, "Max Value", rTarget, 0f, 20f))
            {
                lIsDirty = true;
                MaxValue = EditorHelper.FieldFloatValue;
            }

            GUILayout.EndHorizontal();

            return lIsDirty;
        }

#endif

        #endregion
    }
}
