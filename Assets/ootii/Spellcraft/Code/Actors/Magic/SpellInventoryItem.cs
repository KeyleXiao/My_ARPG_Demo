using System;
using UnityEngine;

namespace com.ootii.Actors.Magic
{
    /// <summary>
    /// The spell inventory item merges the spell "template" and the spell "instance" in order
    /// to manage the life of spells for a specific spell inventory.
    /// </summary>
    [Serializable]
    public class SpellInventoryItem
    {
        /// <summary>
        /// Spell "template" that defines how the spell actually works including
        /// template-level data like max damage, max range, casting cost, etc.
        /// </summary>
        public Spell _SpellPrefab = null;
        public Spell SpellPrefab
        {
            get { return _SpellPrefab; }
            set { _SpellPrefab = value; }
        }

        ///// <summary>
        ///// Spell instance created from the template
        ///// </summary>
        //[NonSerialized]
        //public Spell mSpell = null;
        //public Spell Spell
        //{
        //    get { return mSpell; }
        //}

        /// <summary>
        /// Friendly name of the spell item
        /// </summary>
        public string _Name = "";
        public virtual string Name
        {
            get { return (_Name.Length > 0 || _SpellPrefab == null ? _Name : _SpellPrefab._Name); }
            set { _Name = value; }
        }

        protected GameObject mOwner = null;
        public GameObject Owner
        {
            get { return mOwner; }
            set { mOwner = value; }
        }
    }
}
