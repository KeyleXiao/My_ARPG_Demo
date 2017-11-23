using System.Collections;
using UnityEngine;
using com.ootii.Actors.AnimationControllers;
using com.ootii.Actors.Attributes;
using com.ootii.Actors.Combat;
using com.ootii.Actors.Inventory;
using com.ootii.Actors.Magic;
using com.ootii.Helpers;
using com.ootii.MotionControllerPacks;

namespace com.ootii.Demos
{
    /// <summary>
    /// Controls the NPC to make him attack the player
    /// </summary>
    public class SCMP_NPC_Controller_v2 : MonoBehaviour
    {
        /// <summary>
        /// Determines if we're processing AI
        /// </summary>
        public bool IsActive = true;

        /// <summary>
        /// Target we're going after
        /// </summary>
        public Combatant Target = null;

        /// <summary>
        /// Determines if our character can move
        /// </summary>
        public bool Move = true;

        /// <summary>
        /// Determines how quickly the character moves
        /// </summary>
        public float MovementSpeed = 1.9f;

        /// <summary>
        /// Determines how quickly the character rotates
        /// </summary>
        public float RotationSpeed = 360f;

        /// <summary>
        /// Range to get within the target
        /// </summary>
        public float Range = 5f;

        /// <summary>
        /// Determines if our character attacks
        /// </summary>
        public bool Attack = true;

        /// <summary>
        /// Minimum delay between attacks
        /// </summary>
        public float AttackDelay = 3.0f;

        /// <summary>
        /// Determines if we can cast spells
        /// </summary>
        public bool Cast = true;

        /// <summary>
        /// Minimum delay between casting spells
        /// </summary>
        public float CastDelay = 3.0f;

        /// <summary>
        /// Determines if our character defends
        /// </summary>
        public bool Block = true;

        /// <summary>
        /// Amount of time to hold the block for
        /// </summary>
        public float BlockHold = 2f;

        /// <summary>
        /// Target's motion controller
        /// </summary>
        private MotionController mTargetMotionController = null;

        /// <summary>
        /// Manages combat information
        /// </summary>
        private Combatant mCombatant = null;

        /// <summary>
        /// Manages animations
        /// </summary>
        private MotionController mMotionController = null;

        /// <summary>
        /// Manages attributes
        /// </summary>
        private BasicAttributes mBasicAttributes = null;

        /// <summary>
        /// Manages inventory
        /// </summary>
        private BasicInventory mBasicInventory = null;

        /// <summary>
        /// Manages spells
        /// </summary>
        private SpellInventory mSpellInventory = null;

        /// <summary>
        /// Add a delay for the equip so we don't call it over and over
        /// </summary>
        //private float mLastEquipTime = -5f;

        /// <summary>
        /// Track the last time the character attacked
        /// </summary>
        private float mLastAttackTime = -5f;

        /// <summary>
        /// Track the last time the character cast a spell
        /// </summary>
        private float mLastCastTime = -5f;

        /// <summary>
        /// Determines if we're following the target
        /// </summary>
        private bool mFollow = false;

        /// <summary>
        /// Initializes the MonoBehaviour
        /// </summary>
        private void Awake()
        {
            mCombatant = gameObject.GetComponent<Combatant>();
            mMotionController = gameObject.GetComponent<MotionController>();
            mBasicAttributes = gameObject.GetComponent<BasicAttributes>();
            mBasicInventory = gameObject.GetComponent<BasicInventory>();
            mSpellInventory = gameObject.GetComponent<SpellInventory>();

            if (Target != null)
            {
                mTargetMotionController = Target.gameObject.GetComponent<MotionController>();
            }
        }

        /// <summary>
        /// Called each frame
        /// </summary>
        private void Update()
        {
            if (!IsActive) { return; }
            if (mCombatant == null) { return; }
            if (Target == null) { return; }

            MotionControllerMotion lMotion = mMotionController.ActiveMotion;
            if (lMotion == null) { return; }

            MotionControllerMotion lTargetMotion = mTargetMotionController.ActiveMotion;

            // Determine if we rotate to the target
            bool lRotate = true;

            // Ensure our weapon is equipped
            //if (!mBasicInventory.IsWeaponSetEquipped(2))
            //{
            //    if (mLastEquipTime + AttackDelay < Time.time)
            //    {
            //        mBasicInventory.EquipWeaponSet(2);
            //        mLastEquipTime = Time.time;
            //    }
            //}
            //// The main AI loop
            //else
            //{
                Vector3 lToTarget = Target._Transform.position - transform.position;
                lToTarget.y = 0f;

                Vector3 lToTargetDirection = lToTarget.normalized;
                float lToTargetDistance = lToTarget.magnitude;

                bool lIsTargetAimingAtMe = false;

#if USE_ARCHERY_MP || OOTII_AYMP
            //float lTargetToMeHorizontalAngle = NumberHelper.GetHorizontalAngle(Target._Transform.forward, -lToTargetDirection, Target._Transform.up);
            //lIsTargetAimingAtMe = ((lTargetMotion is Bow_WalkRunTarget || lTargetMotion is Bow_BasicAttacks) && Mathf.Abs(lTargetToMeHorizontalAngle) < 10f);
#endif

            // Determine if we should move to the target
            float lRange = Range;

                if (!mFollow && lToTargetDistance > lRange + 1f) { mFollow = true; }
                if (mFollow && lToTargetDistance <= lRange) { mFollow = false; }
                if (mFollow && (lMotion.Category != EnumMotionCategories.IDLE && lMotion.Category != EnumMotionCategories.WALK)) { mFollow = false; }
                if (mFollow && lIsTargetAimingAtMe) { mFollow = false; }
                if (!Move) { mFollow = false; }

                // Ensure we're not casting
                if (mMotionController.ActiveMotion is BasicSpellCasting)
                {
                }
                // Cast a healing spell
                else if (Cast && 
                         mLastCastTime + CastDelay < Time.time && 
                         mBasicAttributes != null && mBasicAttributes.GetAttributeValue<float>("Health", 100f) < 40f &&
                         mSpellInventory != null && mSpellInventory.GetSpellIndex("Heal Self") >= 0)
                {
                    int lSpellIndex = mSpellInventory.GetSpellIndex("Heal Self");
                    if (lSpellIndex >= 0)
                    {
                        BasicSpellCasting lCastMotion = mMotionController.GetMotion<BasicSpellCasting>();
                        mMotionController.ActivateMotion(lCastMotion, lSpellIndex);

                        mLastCastTime = Time.time;
                    }
                }
                // Move to the target
                else if (mFollow)
                {
                    float lSpeed = Mathf.Min(MovementSpeed * Time.deltaTime, lToTargetDistance);
                    transform.position = transform.position + (lToTargetDirection * lSpeed);
                }
                // If we're being shot at, block
                else if (Block && lIsTargetAimingAtMe)
                {
                    mFollow = false;

                    CombatMessage lMessage = CombatMessage.Allocate();
                    lMessage.ID = CombatMessage.MSG_COMBATANT_BLOCK;
                    lMessage.Attacker = null;
                    lMessage.Defender = gameObject;

                    mMotionController.SendMessage(lMessage);
                    CombatMessage.Release(lMessage);
                }
                // Let the movement finish up
                else if (lMotion.Category == EnumMotionCategories.WALK)
                {
                }
                // Attack with the sword
                else if (Attack && lMotion.Category == EnumMotionCategories.IDLE && (mLastAttackTime + AttackDelay < Time.time))
                {
                    CombatMessage lMessage = CombatMessage.Allocate();
                    lMessage.ID = CombatMessage.MSG_COMBATANT_ATTACK;
                    lMessage.Attacker = gameObject;
                    lMessage.Defender = Target.gameObject;

                    mMotionController.SendMessage(lMessage);
                    CombatMessage.Release(lMessage);

                    mLastAttackTime = Time.time;
                }
                // Block with the shield
                else if (Block && lMotion.Category == EnumMotionCategories.IDLE && lMotion.Age > 0.5f)
                {
                    CombatMessage lMessage = CombatMessage.Allocate();
                    lMessage.ID = CombatMessage.MSG_COMBATANT_BLOCK;
                    lMessage.Attacker = null;
                    lMessage.Defender = gameObject;

                    mMotionController.SendMessage(lMessage);
                    CombatMessage.Release(lMessage);
                }
                // Free the block
                else if (lMotion.Category == EnumMotionCategories.COMBAT_MELEE_BLOCK && (lToTargetDistance > lRange + 1f || lMotion.Age > BlockHold))
                {
                    CombatMessage lMessage = CombatMessage.Allocate();
                    lMessage.ID = CombatMessage.MSG_COMBATANT_CANCEL;
                    lMessage.Attacker = null;
                    lMessage.Defender = gameObject;

                    mMotionController.SendMessage(lMessage);
                    CombatMessage.Release(lMessage);
                }

                // Allow rotation only
                if (mMotionController.enabled && lRotate)
                {
                    float lAngle = NumberHelper.GetHorizontalAngle(transform.forward, lToTargetDirection, transform.up);
                    if (lAngle != 0f)
                    {
                        float lRotationSpeed = Mathf.Sign(lAngle) * Mathf.Min(RotationSpeed * Time.deltaTime, Mathf.Abs(lAngle));
                        transform.rotation = transform.rotation * Quaternion.AngleAxis(lRotationSpeed, transform.up);
                    }
                }
            //}

            // If we're dead, we can just stop
            if (lMotion.Category == EnumMotionCategories.DEATH)
            {
                IsActive = false;
            }
            // Clear the target if they are dead
            else if (Target != null && !Target.enabled)
            {
                Target = null;
                //StartCoroutine(WaitAndStoreEquipment(2f));
            }
        }

        /// <summary>
        /// Time to wait before storing the weapon
        /// </summary>
        /// <param name="rSeconds">Seconds to wait</param>
        private IEnumerator WaitAndStoreEquipment(float rSeconds)
        {
            //if (mCombatant.SecondaryWeapon != null)
            {
                yield return new WaitForSeconds(rSeconds);
                mBasicInventory.StoreWeaponSet();
            }
        }

        /// <summary>
        /// Example code on how to force which attack style to use.
        /// </summary>
        /// <param name="rCombatant">Combatant the event was fired for</param>
        /// <param name="rMotion">Motion that represents the attack</param>
        /// <returns></returns>
        private bool OnAttackActivated(Combatant rCombatant, MotionControllerMotion rMotion)
        {
            BasicSpellCasting lAttacks = rMotion as BasicSpellCasting;
            if (lAttacks != null)
            {
                lAttacks.SpellIndex = 0;
            }

            return true;
        }
    }
}
