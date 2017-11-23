using UnityEngine;
using com.ootii.Actors.AnimationControllers;
using com.ootii.Collections;
using com.ootii.Messages;

namespace com.ootii.Actors.Magic
{
    /// <summary>
    /// Message
    /// </summary>
    public class MagicMessage : Message
    {
        /// <summary>
        /// Message type to send to the MC
        /// </summary>
        public static int MSG_UNKNOWN = 5000;
        public static int MSG_MAGIC_CAST = 5001;
        public static int MSG_MAGIC_CONTINUE = 5002;
        public static int MSG_MAGIC_CANCEL = 5003;
        public static int MSG_MAGIC_PRE_CAST = 5004;
        public static int MSG_MAGIC_POST_CAST = 5005;

        /// <summary>
        /// Index of the spell being dealt with
        /// </summary>
        public int SpellIndex = -1;

        /// <summary>
        /// Combatant that represents the attacker
        /// </summary>
        public GameObject Caster = null;

        /// <summary>
        /// Motion that the caster is casting from
        /// </summary>
        public IMotionControllerMotion CastingMotion = null;

        /// <summary>
        /// Clear this instance.
        /// </summary>
        public override void Clear()
        {
            SpellIndex = -1;
            Caster = null;

            base.Clear();
        }

        /// <summary>
        /// Release this instance.
        /// </summary>
        public new virtual void Release()
        {
            // We should never release an instance unless we're
            // sure we're done with it. So clearing here is fine
            Clear();

            // Reset the sent flags. We do this so messages are flagged as 'completed'
            // and removed by default.
            IsSent = true;
            IsHandled = true;

            // Make it available to others.
            if (this is MagicMessage)
            {
                sPool.Release(this);
            }
        }

        // ******************************** OBJECT POOL ********************************

        /// <summary>
        /// Allows us to reuse objects without having to reallocate them over and over
        /// </summary>
        private static ObjectPool<MagicMessage> sPool = new ObjectPool<MagicMessage>(40, 10);

        /// <summary>
        /// Pulls an object from the pool.
        /// </summary>
        /// <returns></returns>
        public new static MagicMessage Allocate()
        {
            // Grab the next available object
            MagicMessage lInstance = sPool.Allocate();

            // Reset the sent flags. We do this so messages are flagged as 'completed'
            // by default.
            lInstance.IsSent = false;
            lInstance.IsHandled = false;

            // For this type, guarentee we have something
            // to hand back tot he caller
            if (lInstance == null) { lInstance = new MagicMessage(); }
            return lInstance;
        }

        /// <summary>
        /// Returns an element back to the pool.
        /// </summary>
        /// <param name="rEdge"></param>
        public static void Release(MagicMessage rInstance)
        {
            if (rInstance == null) { return; }

            // We should never release an instance unless we're
            // sure we're done with it. So clearing here is fine
            rInstance.Clear();

            // Reset the sent flags. We do this so messages are flagged as 'completed'
            // and removed by default.
            rInstance.IsSent = true;
            rInstance.IsHandled = true;

            // Make it available to others.
            sPool.Release(rInstance);
        }

        /// <summary>
        /// Returns an element back to the pool.
        /// </summary>
        /// <param name="rEdge"></param>
        public new static void Release(IMessage rInstance)
        {
            if (rInstance == null) { return; }

            // We should never release an instance unless we're
            // sure we're done with it. So clearing here is fine
            rInstance.Clear();

            // Reset the sent flags. We do this so messages are flagged as 'completed'
            // and removed by default.
            rInstance.IsSent = true;
            rInstance.IsHandled = true;

            // Make it available to others.
            if (rInstance is MagicMessage)
            {
                sPool.Release((MagicMessage)rInstance);
            }
        }
    }
}
