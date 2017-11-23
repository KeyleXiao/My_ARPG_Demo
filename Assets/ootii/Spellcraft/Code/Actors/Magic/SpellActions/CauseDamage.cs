using System;
using UnityEngine;
using com.ootii.Actors.Attributes;
using com.ootii.Actors.Combat;
using com.ootii.Actors.LifeCores;
using com.ootii.Base;
using com.ootii.Graphics.NodeGraph;
using com.ootii.Helpers;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace com.ootii.Actors.Magic
{
    [Serializable]
    [BaseName("Cause Damage")]
    [BaseDescription("Causes a single instance of damage to the target.")]
    public class CauseDamage : SpellAction
    {
        /// <summary>
        /// Type of damage being applied
        /// </summary>
        public int _DamageType = 0;
        public int DamageType
        {
            get { return _DamageType; }
            set { _DamageType = value; }
        }

        /// <summary>
        /// Determines how the damage is applied
        /// </summary>
        public int _ImpactType = 0;
        public int ImpactType
        {
            get { return _ImpactType; }
            set { _ImpactType = value; }
        }

        /// <summary>
        /// Index of the stored float to use for damage
        /// </summary>
        public int _DamageFloatValueIndex = -1;
        public int DamageFloatValueIndex
        {
            get { return _DamageFloatValueIndex; }
            set { _DamageFloatValueIndex = value; }
        }

        /// <summary>
        /// Minimum amount of damage caused
        /// </summary>
        public float _MinDamage = 5f;
        public float MinDamage
        {
            get { return _MinDamage; }
            set { _MinDamage = value; }
        }

        /// <summary>
        /// Maximum amount of damage caused
        /// </summary>
        public float _MaxDamage = 5f;
        public float MaxDamage
        {
            get { return _MaxDamage; }
            set { _MaxDamage = value; }
        }

        /// <summary>
        /// Determines if we should play the damage animation
        /// </summary>
        public bool _PlayAnimation = false;
        public bool PlayAnimation
        {
            get { return _PlayAnimation; }
            set { _PlayAnimation = value; }
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

            if (rData != null && rData != _Spell.Data)
            {
                if (rData is Collider)
                {
                    ActivateInstance(((Collider)rData).gameObject);
                }
                else if (rData is Transform)
                {
                    ActivateInstance(((Transform)rData).gameObject);
                }
                else if (rData is GameObject)
                {
                    ActivateInstance((GameObject)rData);
                }
                else if (rData is MonoBehaviour)
                {
                    ActivateInstance(((MonoBehaviour)rData).gameObject);
                }
            }
            else if (_Spell.Data != null && _Spell.Data.Targets != null)
            {
                for (int i = 0; i < _Spell.Data.Targets.Count; i++)
                {
                    ActivateInstance(_Spell.Data.Targets[i]);
                }
            }            

            // Immediately deactivate
            Deactivate();
        }

        /// <summary>
        /// Activates the action on a single target
        /// </summary>
        /// <param name="rTarget">Target to activate on</param>
        protected void ActivateInstance(GameObject rTarget)
        {
            Vector3 lPoint = rTarget.transform.position;
            Vector3 lForward = rTarget.transform.forward;

            // Combatant that is attacking
            ICombatant lAttacker = (_Spell.Owner != null ? _Spell.Owner.GetComponent<ICombatant>() : null);

            // Determine who we're colliding with
            IActorCore lDefenderCore = rTarget.GetComponent<IActorCore>();
            IDamageable lDamageable = rTarget.GetComponent<IDamageable>();

            if (lDefenderCore == null)
            {
                IWeaponCore lWeaponCore = rTarget.GetComponent<IWeaponCore>();
                if (lWeaponCore != null)
                {
                    lDefenderCore = lWeaponCore.Owner.GetComponent<IActorCore>();
                    if (lDamageable == null) { lDamageable = lWeaponCore.Owner.GetComponent<IDamageable>(); }
                }
            }

            // Save the hit information
            Transform lHitTransform = GetClosestTransform(lPoint, rTarget.transform);
            Vector3 lCombatCenter = lHitTransform.position;
            Vector3 lHitDirection = Vector3.zero;

            if (lDefenderCore != null)
            {
                ICombatant lDefenderCombatant = lDefenderCore.gameObject.GetComponent<ICombatant>();
                if (lDefenderCombatant != null)
                {
                    lHitDirection = Quaternion.Inverse(lDefenderCore.Transform.rotation) * (lPoint - lDefenderCombatant.CombatOrigin).normalized;
                }
            }
            else
            {
                lHitDirection = Quaternion.Inverse(lHitTransform.rotation) * (lPoint - lCombatCenter).normalized;
            }

            // Determine the damage
            float lDamage = UnityEngine.Random.Range(_MinDamage, _MaxDamage);
            if (_DamageFloatValueIndex >= 0 && _Spell.Data.FloatValues != null)
            {
                lDamage = _Spell.Data.FloatValues[_DamageFloatValueIndex];
            }

            // Put together the combat round info
            CombatMessage lCombatMessage = CombatMessage.Allocate();
            lCombatMessage.ID = CombatMessage.MSG_DEFENDER_ATTACKED;
            lCombatMessage.Attacker = (lAttacker != null ? lAttacker.Transform.gameObject : _Spell.Owner);
            lCombatMessage.Defender = (lDefenderCore != null ? lDefenderCore.Transform.gameObject : rTarget);
            lCombatMessage.Weapon = null;
            lCombatMessage.DamageType = _DamageType;
            lCombatMessage.ImpactType = _ImpactType;
            lCombatMessage.Damage = lDamage;
            lCombatMessage.AnimationEnabled = _PlayAnimation;
            lCombatMessage.HitPoint = lPoint;
            lCombatMessage.HitDirection = lHitDirection;
            lCombatMessage.HitVector = lForward;
            lCombatMessage.HitTransform = lHitTransform;

            // Let the defender react to the damage
            if (lDefenderCore != null)
            {
                lDefenderCore.SendMessage(lCombatMessage);
            }
            // If needed, send the damage directly to the actor core
            else if (lDamageable != null)
            {
                lDamageable.OnDamaged(lCombatMessage);
            }
            // Without an actor core, check if we can set attributes
            else
            {
                IAttributeSource lAttributeSource = rTarget.GetComponent<IAttributeSource>();
                if (lAttributeSource != null)
                {
                    float lHealth = lAttributeSource.GetAttributeValue<float>(EnumAttributeIDs.HEALTH);
                    lAttributeSource.SetAttributeValue(EnumAttributeIDs.HEALTH, Mathf.Max(lHealth - lCombatMessage.Damage, 0f));
                }
            }

            // Release the message
            lCombatMessage.Release();
        }

        /// <summary>
        /// Starts the recursive function for the closest transform to the specified point
        /// </summary>
        /// <param name="rPosition">Reference point for for the closest transform</param>
        /// <param name="rCollider">Transform that represents the collision</param>
        /// <returns></returns>
        protected virtual Transform GetClosestTransform(Vector3 rPosition, Transform rCollider)
        {
            // Find the anchor's root transform
            Transform lActorTransform = rCollider;

            // Grab the closest body transform
            float lMinDistance = float.MaxValue;
            Transform lMinTransform = lActorTransform;
            GetClosestTransform(rPosition, lActorTransform, ref lMinDistance, ref lMinTransform);

            // Return it
            return lMinTransform;
        }

        /// <summary>
        /// Find the closes transform to the hit position. This is what we'll attach the projectile to
        /// </summary>
        /// <param name="rPosition">Hit position</param>
        /// <param name="rTransform">Transform to be tested</param>
        /// <param name="rMinDistance">Current min distance between the hit position and closest transform</param>
        /// <param name="rMinTransform">Closest transform</param>
        protected virtual void GetClosestTransform(Vector3 rPosition, Transform rTransform, ref float rMinDistance, ref Transform rMinTransform)
        {
            // Limit what we'll connect to
            //if (rTransform.name.Contains("connector")) { return; }
            //if (rTransform.gameObject.GetComponent<IWeaponCore>() != null) { return; }

            // If this transform is closer to the hit position, use it
            float lDistance = Vector3.Distance(rPosition, rTransform.position);
            if (lDistance < rMinDistance)
            {
                rMinDistance = lDistance;
                rMinTransform = rTransform;
            }

            // Check if any child transform is closer to the hit position
            for (int i = 0; i < rTransform.childCount; i++)
            {
                GetClosestTransform(rPosition, rTransform.GetChild(i), ref rMinDistance, ref rMinTransform);
            }
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

            if (EditorHelper.PopUpField("Damage Type", "Type of damage to apply.", DamageType, EnumDamageType.Names, rTarget))
            {
                lIsDirty = true;
                DamageType = EditorHelper.FieldIntValue;
            }

            if (DamageType == 0 || DamageType >= EnumDamageType.Names.Length)
            {
                if (EditorHelper.IntField("Custom ID", "Enter your own custom ID for the damage type.", DamageType, rTarget))
                {
                    lIsDirty = true;
                    DamageType = EditorHelper.FieldIntValue;
                }

                GUILayout.Space(5f);
            }

            if (EditorHelper.PopUpField("Impact Type", "Way in which damage is applied.", ImpactType, EnumImpactType.Names, rTarget))
            {
                lIsDirty = true;
                ImpactType = EditorHelper.FieldIntValue;
            }

            if (ImpactType == 0 || ImpactType >= EnumImpactType.Names.Length)
            {
                if (EditorHelper.IntField("Custom ID", "Enter your own custom ID for the impact type.", ImpactType, rTarget))
                {
                    lIsDirty = true;
                    ImpactType = EditorHelper.FieldIntValue;
                }
            }

            GUILayout.Space(5f);

            // Damage
            if (EditorHelper.IntField("Value Index", "Index into the spell data values or -1 if constants are used.", DamageFloatValueIndex, rTarget))
            {
                lIsDirty = true;
                DamageFloatValueIndex = EditorHelper.FieldIntValue;
            }

            if (DamageFloatValueIndex < 0)
            {
                GUILayout.BeginHorizontal();

                EditorGUILayout.LabelField(new GUIContent("Damage", "Min and max damage to apply."), GUILayout.Width(EditorGUIUtility.labelWidth - 4f));

                if (EditorHelper.FloatField(MinDamage, "Min Damage", rTarget, 0f, 20f))
                {
                    lIsDirty = true;
                    MinDamage = EditorHelper.FieldFloatValue;
                }

                if (EditorHelper.FloatField(MaxDamage, "Max Damage", rTarget, 0f, 20f))
                {
                    lIsDirty = true;
                    MaxDamage = EditorHelper.FieldFloatValue;
                }

                GUILayout.EndHorizontal();
            }

            GUILayout.Space(5f);

            if (EditorHelper.BoolField("Play Animation", "Determines if the damage will cause an animation to play.", PlayAnimation, rTarget))
            {
                lIsDirty = true;
                PlayAnimation = EditorHelper.FieldBoolValue;
            }

            return lIsDirty;
        }

#endif

        #endregion
    }
}