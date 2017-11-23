using System.Collections.Generic;
using UnityEngine;
using com.ootii.Actors.Combat;

namespace com.ootii.Actors.Magic
{
    /// <summary>
    /// Data associated with the instance of a spell. It allows us to modify the
    /// spell between the different actions
    /// </summary>
    public class SpellData
    {
        /// <summary>
        /// GameObjects the spell is targeting
        /// </summary>
        public List<GameObject> Targets = null;

        /// <summary>
        /// GameObjects not targeted by the spell.
        /// </summary>
        public List<GameObject> PreviousTargets = null;

        /// <summary>
        /// Positions the spell is using
        /// </summary>
        public List<Vector3> Positions = null;

        /// <summary>
        /// Directions the spell is using
        /// </summary>
        public List<Vector3> Forwards = null;

        /// <summary>
        /// Simple float value to store
        /// </summary>
        public List<float> FloatValues = null;

        /// <summary>
        /// Default constructor
        /// </summary>
        public SpellData()
        {
        }

        /// <summary>
        /// Clears the spell data
        /// </summary>
        public void Clear()
        {
            if (Targets != null)
            {
                Targets.Clear();
                Targets = null;
            }

            if (PreviousTargets != null)
            {
                PreviousTargets.Clear();
                PreviousTargets = null;
            }

            if (Positions != null)
            {
                Positions.Clear();
                Positions = null;
            }

            if (Forwards != null)
            {
                Forwards.Clear();
                Forwards = null;
            }

            if (FloatValues != null)
            {
                FloatValues.Clear();
                FloatValues = null;
            }
        }
    }
}
