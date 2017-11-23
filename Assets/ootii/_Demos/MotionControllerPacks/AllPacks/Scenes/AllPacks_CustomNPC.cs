using UnityEngine;
using com.ootii.Actors.AnimationControllers;
using com.ootii.Actors.Combat;
using com.ootii.Actors.Inventory;
using com.ootii.Actors.LifeCores;
using com.ootii.Helpers;
using com.ootii.Reactors;

using com.ootii.MotionControllerPacks;

namespace com.ootii.Demos
{
    /// <summary>
    /// Simple code based AI for managing an NPC
    /// </summary>
    public class AllPacks_CustomNPC : MonoBehaviour
    {
        /// <summary>
        /// Different states of our NPC
        /// </summary>
        public const int IDLE = 0;
        public const int MOVING = 1;
        public const int EQUIPPING = 2;
        public const int ATTACKING = 3;
        public const int BLOCKING = 4;
        public const int REACTING = 5;
        public const int KILLED = 10;

        /// <summary>
        /// Determines if the NPC moves
        /// </summary>
        public bool Move = true;

        /// <summary>
        /// Determines how quickly the character moves
        /// </summary>
        public float MovementSpeed = 1.9f;

        /// <summary>
        /// Determine if the NPC rotates
        /// </summary>
        public bool Rotate = true;

        /// <summary>
        /// Determines how quickly the character rotates
        /// </summary>
        public float RotationSpeed = 360f;

        /// <summary>
        /// Target we're going after
        /// </summary>
        public GameObject _Target = null;
        public GameObject Target
        {
            get { return _Target; }

            set
            {
                _Target = value;

                if (_Target == null)
                {
                    mTargetActorCore = null;
                    mTargetMotionController = null;
                }
                else
                {
                    mTargetActorCore = _Target.GetComponent<ActorCore>();
                    mTargetMotionController = _Target.GetComponent<MotionController>();
                }
            }
        }

        /// <summary>
        /// Determines if the NPC will use a sword
        /// </summary>
        public bool UseSword = true;

        /// <summary>
        /// Determines if the NPC will block with the shield
        /// </summary>
        public bool UseShield = true;

        /// <summary>
        /// Determines if the NPC will use a bow
        /// </summary>
        public bool UseBow = true;

        /// <summary>
        /// Number of arrows the actor has
        /// </summary>
        public int ArrowCount = 1;

        // Life core that determines how the actor behaves
        private ActorCore mTargetActorCore = null;

        // Manages animations for the target
        private MotionController mTargetMotionController = null;

        // Life core that determines how the actor behaves
        private ActorCore mActorCore = null;

        // Manages animations
        private MotionController mMotionController = null;

        // Manages inventory
        private BasicInventory mInventory = null;

        /// <summary>
        /// Initializes the MonoBehaviour
        /// </summary>
        private void Awake()
        {
            mActorCore = gameObject.GetComponent<ActorCore>();
            mActorCore.SetStateValue("State", 0);

            mMotionController = gameObject.GetComponent<MotionController>();
            mInventory = gameObject.GetComponent<BasicInventory>();

            if (_Target != null) { Target = _Target; }
        }

        /// <summary>
        /// Called once per frame to manage the life cycle of the NPC
        /// </summary>
        public void Update()
        {

#if !USE_SWORD_SHIELD_MP && !OOTII_SSMP
            UseShield = false;
            UseSword = false;
#endif

#if !USE_ARCHERY_MP && !OOTII_AYMP
            UseBow = false;
#endif

            if (!mActorCore.IsAlive) { return; }

            DetermineTarget();

            int lState = mActorCore.GetStateValue("State");
            if (lState == IDLE || lState == MOVING)
            {
                DetermineWeapon();
            }

            lState = mActorCore.GetStateValue("State");
            if (lState == IDLE || lState == BLOCKING || lState == MOVING)
            {
                DetermineBlock();
            }

            lState = mActorCore.GetStateValue("State");
            if (lState == IDLE || lState == MOVING)
            {
                DetermineAttack(2f, 5f, 10f);
            }

            lState = mActorCore.GetStateValue("State");
            if (lState == IDLE || lState == MOVING)
            {

                if (Rotate)
                {
                    RotateToTarget();
                }

                if (Move)
                {
                    MoveToTarget(1f * (lState == MOVING ? 1f : 1.5f));
                }
            }
        }

        /// <summary>
        /// Determines if we have a target and who it is
        /// </summary>
        public void DetermineTarget()
        {
            if (mTargetActorCore != null && !mTargetActorCore.IsAlive)
            {
                Target = null;
            }
        }

        /// <summary>
        /// Determines which weapon to use and equips it
        /// </summary>
        public void DetermineWeapon()
        {
            if (_Target == null)
            {
                if (mInventory.ActiveWeaponSet != 3)
                {
                    mInventory.ToggleWeaponSet(3);
                    mActorCore.SetStateValue("State", EQUIPPING);
                }
            }
            else
            {
                Vector3 lToTarget = _Target.transform.position - transform.position;
                float lToTargetDistance = lToTarget.magnitude;

                if ((UseSword || UseShield) && (ArrowCount == 0 || lToTargetDistance < 2f))
                {
                    if (mInventory.ActiveWeaponSet != 0)
                    {
                        mInventory.ToggleWeaponSet(0);
                        mActorCore.SetStateValue("State", EQUIPPING);
                    }
                }
                else if (UseBow && lToTargetDistance > 7f)
                {
                    if (mInventory.ActiveWeaponSet != 1)
                    {
                        mInventory.ToggleWeaponSet(1);
                        mActorCore.SetStateValue("State", EQUIPPING);
                    }
                }             
            }
        }

        /// <summary>
        /// Tells the actor when to block
        /// </summary>
        public void DetermineBlock()
        {
            if (_Target == null) { return; }

#if USE_SWORD_SHIELD_MP || OOTII_SSMP

            Vector3 lToTarget = _Target.transform.position - transform.position;
            float lToTargetDistance = lToTarget.magnitude;

            BasicMeleeBlock lMeleeBlock = mMotionController.GetMotion<BasicMeleeBlock>();
            if (lMeleeBlock != null)
            {
                if (lMeleeBlock.IsActive)
                {
                    if (lMeleeBlock.Age > 1f)
                    {
                        CombatMessage lMessage = CombatMessage.Allocate();
                        lMessage.ID = CombatMessage.MSG_COMBATANT_CANCEL;
                        lMessage.Defender = gameObject;
                        lMeleeBlock.OnMessageReceived(lMessage);

                        CombatMessage.Release(lMessage);

                        mActorCore.SetStateValue("State", IDLE);
                    }
                }
                else
                {
                    BasicMeleeAttack lMeleeAttack = mTargetMotionController.GetMotion<BasicMeleeAttack>();
                    if (lMeleeAttack != null && lMeleeAttack.IsActive)
                    {
                        if (lMeleeAttack.Age < 0.1f && !lMeleeBlock.IsActive)
                        {
                            if (mActorCore.GetStateValue("Stance") == EnumControllerStance.COMBAT_MELEE)
                            {
                                mMotionController.ActivateMotion(lMeleeBlock);
                                mActorCore.SetStateValue("State", BLOCKING);
                            }
                        }
                    }

#if USE_ARCHERY_MP || OOTII_AYMP

                    BasicRangedAttack lRangedAttack = mTargetMotionController.GetMotion<BasicRangedAttack>();
                    if (lRangedAttack != null && lRangedAttack.IsActive)
                    {

                    }

#endif
                }
            }

#endif
        }

        /// <summary>
        /// Tells the actor when to attack
        /// </summary>
        /// <param name="rMeleeMax"></param>
        /// <param name="rRangedMin"></param>
        /// <param name="rRangedMax"></param>
        public void DetermineAttack(float rMeleeMax, float rRangedMin, float rRangedMax)
        {
            if (_Target == null) { return; }

            Vector3 lToTarget = _Target.transform.position - transform.position;
            float lToTargetDistance = lToTarget.magnitude;

            int lStance = mActorCore.GetStateValue("Stance");
            if (UseSword && lStance == EnumControllerStance.COMBAT_MELEE)
            {
                if (lToTargetDistance < rMeleeMax)
                {
#if USE_SWORD_SHIELD_MP || OOTII_SSMP

                    BasicMeleeAttack lAttack = mMotionController.GetMotion<BasicMeleeAttack>();
                    if (lAttack != null && !lAttack.IsActive)
                    {
                        mMotionController.ActivateMotion(lAttack);
                        mActorCore.SetStateValue("State", ATTACKING);
                    }

#endif
                }
            }
            else if (UseBow && ArrowCount > 0 && lStance == EnumControllerStance.COMBAT_RANGED)
            {
                if (lToTargetDistance > rRangedMin && lToTargetDistance < rRangedMax)
                {
#if USE_ARCHERY_MP || OOTII_AYMP

                    BasicRangedAttack lAttack = mMotionController.GetMotion<BasicRangedAttack>();
                    if (lAttack != null && !lAttack.IsActive)
                    {
                        mMotionController.ActivateMotion(lAttack);
                        mActorCore.SetStateValue("State", ATTACKING);
                    }

#endif
                }
            }
        }

        /// <summary>
        /// Rotates to the direction of the target over time
        /// </summary>
        public void RotateToTarget()
        {
            if (_Target == null) { return; }

            Vector3 lToTarget = _Target.transform.position - transform.position;
            Vector3 lToTargetDirection = lToTarget.normalized;

            float lToTargetAngle = NumberHelper.GetHorizontalAngle(transform.forward, lToTargetDirection, transform.up);
            if (lToTargetAngle != 0f)
            {
                float lRotationSpeed = Mathf.Sign(lToTargetAngle) * Mathf.Min(RotationSpeed * Time.deltaTime, Mathf.Abs(lToTargetAngle));
                transform.rotation = transform.rotation * Quaternion.AngleAxis(lRotationSpeed, transform.up);
            }
        }

        /// <summary>
        /// Moves to the target over time
        /// </summary>
        /// <param name="rDistance"></param>
        public void MoveToTarget(float rDistance)
        {
            if (_Target == null) { return; }

            Vector3 lToTarget = _Target.transform.position - transform.position;
            Vector3 lToTargetDirection = lToTarget.normalized;
            float lToTargetDistance = lToTarget.magnitude;

            if (lToTargetDistance > rDistance)
            {
                if (mActorCore.GetStateValue("State") == IDLE) { mActorCore.SetStateValue("State", MOVING); }

                float lSpeed = Mathf.Min(MovementSpeed * Time.deltaTime, lToTargetDistance);
                transform.position = transform.position + (lToTargetDirection * lSpeed);
            }
            else
            {
                mActorCore.SetStateValue("State", IDLE);
            }
        }

        /// <summary>
        /// Reports when the weapon style is equipped
        /// </summary>
        /// <param name="rAction"></param>
        public void OnWeaponSetEquipped(ReactorAction rAction)
        {
            mActorCore.SetStateValue("State", IDLE);
        }

        /// <summary>
        /// Allows us to choose the attack style we'll attack with
        /// </summary>
        /// <param name="rAction"></param>
        public void OnPreAttack(ReactorAction rAction)
        {
            mActorCore.SetStateValue("State", ATTACKING);

            if (rAction == null || rAction.Message == null) { return; }

#if USE_SWORD_SHIELD_MP || OOTII_SSMP

            // Choose the attack style
            CombatMessage lCombatMessage = rAction.Message as CombatMessage;
            if (lCombatMessage == null) { return; }

            if (lCombatMessage.CombatMotion is BasicMeleeAttack)
            {
                //lCombatMessage.StyleIndex = 2;
            }

#endif
        }

        /// <summary>
        /// Allows us to choose the attack style we'll attack with
        /// </summary>
        /// <param name="rAction"></param>
        public void OnPostAttack(ReactorAction rAction)
        {
            mActorCore.SetStateValue("State", IDLE);

            if (rAction == null || rAction.Message == null) { return; }

#if USE_ARCHERY_MP || OOTII_AYMP

            // Decrease the arrows
            CombatMessage lCombatMessage = rAction.Message as CombatMessage;
            if (lCombatMessage.CombatMotion is BasicRangedAttack)
            {
                ArrowCount--;
            }

#endif
        }

        /// <summary>
        /// Occurs after the character has been damaged
        /// </summary>
        /// <param name="rAction"></param>
        public void OnDamaged(ReactorAction rAction)
        {
            // This is primarily done to clear any block that is up
            mActorCore.SetStateValue("State", IDLE);
        }

        /// <summary>
        /// Occurs after the character has been killed
        /// </summary>
        /// <param name="rAction"></param>
        public void OnKilled(ReactorAction rAction)
        {
            if (rAction.Message is CombatMessage)
            {
                if (((CombatMessage)rAction.Message).Defender == gameObject)
                {
                    mActorCore.SetStateValue("State", KILLED);
                }
            }
        }
    }
}
