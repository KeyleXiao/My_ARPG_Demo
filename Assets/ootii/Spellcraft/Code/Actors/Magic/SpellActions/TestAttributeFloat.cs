using System;
using UnityEngine;
using com.ootii.Actors.Attributes;
using com.ootii.Base;
using com.ootii.Graphics.NodeGraph;
using com.ootii.Helpers;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace com.ootii.Actors.Magic
{
    [Serializable]
    [BaseName("Test Attribute Float")]
    [BaseDescription("Compares the attribute value + any random value on the target against the value entered. The node succeeds if the comparison returns true.")]
    public class TestAttributeFloat : SpellAction
    {
        // Condition to test against
        public static string[] Comparisons = new string[] { "=", "!=", "<", "<=", ">", ">=" };

        /// <summary>
        /// Determines who we'll activate the motions on
        /// </summary>
        public int _TargetTypeIndex = 0;
        public int TargetTypeIndex
        {
            get { return _TargetTypeIndex; }
            set { _TargetTypeIndex = value; }
        }

        /// <summary>
        /// Condition index
        /// </summary>
        public int _ComparisonIndex = 0;
        public int ComparisonIndex
        {
            get { return _ComparisonIndex; }
            set { _ComparisonIndex = value; }
        }

        /// <summary>
        /// Name of the attribute whose value will be compared
        /// </summary>
        public string _AttributeName = "";
        public string AttributeName
        {
            get { return _AttributeName; }
            set { _AttributeName = value; }
        }

        /// <summary>
        /// Value we're comparing to
        /// </summary>
        public float _Value = 100f;
        public float Value
        {
            get { return _Value; }
            set { _Value = value; }
        }

        /// <summary>
        /// Minimum random value to add to the attribute value before comparing
        /// </summary>
        public float _MinRandom = 0f;
        public float MinRandom
        {
            get { return _MinRandom; }
            set { _MinRandom = value; }
        }

        /// <summary>
        /// Maximum random value to add to the attribute value before comparing
        /// </summary>
        public float _MaxRandom = 0f;
        public float MaxRandom
        {
            get { return _MaxRandom; }
            set { _MaxRandom = value; }
        }

        /// <summary>
        /// Used to initialize any actions prior to them being activated
        /// </summary>
        public override void Awake()
        {
            base.Awake();
            _DeactivationIndex = EnumSpellActionDeactivation.IMMEDIATELY;
        }

        /// <summary>
        /// Called when the action is first activated
        /// <param name="rPreviousSpellActionState">State of the action prior to this one activating</param>
        public override void Activate(int rPreviousSpellActionState = -1, object rData = null)
        {
            base.Activate(rPreviousSpellActionState, rData);

            // Determines if the test is valid
            bool lIsValid = false;

            // Grab the target and test it
            GameObject lTarget = GetBestTarget(TargetTypeIndex, rData, _Spell.Data);
            if (lTarget != null)
            {
                lIsValid = ActivateInstance(lTarget);
            }

            // Immediately deactivate
            if (lIsValid)
            {
                OnSuccess();
            }
            else
            {
                OnFailure();
            }
        }

        /// <summary>
        /// Activates the action on a single target
        /// </summary>
        /// <param name="rTarget">Target to activate on</param>
        protected bool ActivateInstance(GameObject rTarget)
        {
            if (rTarget == null) { return false; }

            IAttributeSource lAttributeSource = rTarget.GetComponent<IAttributeSource>();
            if (lAttributeSource == null) { return false; }

            if (!lAttributeSource.AttributeExists(AttributeName)) { return false; }

            float lValue = lAttributeSource.GetAttributeValue<float>(AttributeName);
            lValue = lValue + UnityEngine.Random.Range(MinRandom, MaxRandom);

            switch (ComparisonIndex)
            {
                case 0:
                    return (lValue == Value);

                case 1:
                    return (lValue != Value);

                case 2:
                    return (lValue < Value);

                case 3:
                    return (lValue <= Value);

                case 4:
                    return (lValue > Value);

                case 5:
                    return (lValue >= Value);
            }

            return false;
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

            if (EditorHelper.TextField("Attribute Name", "Name of the attribute whose value we will compare", AttributeName, rTarget))
            {
                lIsDirty = true;
                AttributeName = EditorHelper.FieldStringValue;
            }

            // Random
            GUILayout.BeginHorizontal();

            EditorGUILayout.LabelField(new GUIContent("+ Random", "Min and max random value to add to the attribute value before testin."), GUILayout.Width(EditorGUIUtility.labelWidth - 4f));

            if (EditorHelper.FloatField(MinRandom, "Min Damage", rTarget, 0f, 20f))
            {
                lIsDirty = true;
                MinRandom = EditorHelper.FieldFloatValue;
            }

            if (EditorHelper.FloatField(MaxRandom, "Max Damage", rTarget, 0f, 20f))
            {
                lIsDirty = true;
                MaxRandom = EditorHelper.FieldFloatValue;
            }

            GUILayout.EndHorizontal();

            if (EditorHelper.PopUpField("Comparison", "Comparison to make.", ComparisonIndex, Comparisons, rTarget))
            {
                lIsDirty = true;
                ComparisonIndex = EditorHelper.FieldIntValue;
            }

            if (EditorHelper.FloatField("Value", "Value we'll compare the 'attribute + random' value to.", Value, rTarget))
            {
                lIsDirty = true;
                Value = EditorHelper.FieldFloatValue;
            }

            return lIsDirty;
        }

#endif

        #endregion
    }
}