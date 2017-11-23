using UnityEngine;

namespace com.ootii.Actors.Combat
{
    /// <summary>
    /// Structure used to hold criteria that will help us filter combatants. This
    /// allows our function to be expanded.
    /// </summary>
    public struct CombatFilter
    {
        public bool RequireCombatant;

        public float MinDistance;

        public float MaxDistance;

        public float HorizontalFOA;

        public float VerticalFOA;

        public int Layers;

        public string Tag;

        /// <summary>
        /// AttackStyle constructor
        /// </summary>
        /// <param name="rStyle"></param>
        public CombatFilter(AttackStyle rStyle)
        {
            RequireCombatant = false;

            MinDistance = 0.5f;

            MaxDistance = 3f;

            HorizontalFOA = rStyle.HorizontalFOA;

            VerticalFOA = rStyle.VerticalFOA;

            Layers = -1;

            Tag = null;
        }
    }
}
