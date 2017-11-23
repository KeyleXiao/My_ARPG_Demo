//#define NO_ALLOCATION

using System;
using System.Collections.Generic;
using UnityEngine;
using com.ootii.Actors.AnimationControllers;
using com.ootii.Collections;
using com.ootii.Helpers;
using com.ootii.Input;
using com.ootii.MotionControllerPacks;

namespace com.ootii.Actors.Magic
{
    /// <summary>
    /// List of spells that are known/owned by a specific character. Think of this as a 
    /// spell book that understands the "template" (Spell) and the "instance" (SpellData). 
    /// 
    /// It could also represent spells that affect the character.
    /// </summary>
    public class SpellInventory : MonoBehaviour
    {
        /// <summary>
        /// GameObject that owns the IInputSource we really want
        /// </summary>
        public GameObject _InputSourceOwner = null;
        public GameObject InputSourceOwner
        {
            get { return _InputSourceOwner; }
            set { _InputSourceOwner = value; }
        }

        /// <summary>
        /// Defines the source of the input that we'll use to control
        /// the character movement, rotations, and animations.
        /// </summary>
        [NonSerialized]
        public IInputSource _InputSource = null;
        public IInputSource InputSource
        {
            get { return _InputSource; }
            set { _InputSource = value; }
        }

        /// <summary>
        /// Determines if we'll auto find the input source if one doesn't exist
        /// </summary>
        public bool _AutoFindInputSource = true;
        public bool AutoFindInputSource
        {
            get { return _AutoFindInputSource; }
            set { _AutoFindInputSource = value; }
        }

        /// <summary>
        /// Default spell index to cast
        /// </summary>
        public int _DefaultSpellIndex = 0;
        public int DefaultSpellIndex
        {
            get { return _DefaultSpellIndex; }
            set { _DefaultSpellIndex = value; }
        }

        /// <summary>
        /// Action alias for casting a spell
        /// </summary>
        public string _ActionAlias = "";
        public string ActionAlias
        {
            get { return _ActionAlias; }
            set { _ActionAlias = value; }
        }

        /// <summary>
        /// Shows the debug information for spells
        /// </summary>
        public bool _ShowDebug = false;
        public bool ShowDebug
        {
            get { return _ShowDebug; }
            set { _ShowDebug = value; }
        }

        /// <summary>
        /// List of spells managed by the inventory. 
        /// </summary>
        public List<SpellInventoryItem> _Spells = new List<SpellInventoryItem>();

        /// <summary>
        /// List of spell instances that are active. These are spells that are actually cast.
        /// </summary>
        [NonSerialized]
        public List<Spell> _ActiveSpells = new List<Spell>();

        /// <summary>
        /// Once the objects are instanciated, awake is called before start. Use it
        /// to setup references to other objects
        /// </summary>
        protected virtual void Awake()
        {
            // Object that will provide access to the keyboard, mouse, etc
            if (_InputSourceOwner != null) { _InputSource = InterfaceHelper.GetComponent<IInputSource>(_InputSourceOwner); }

            // If the input source is still null, see if we can grab a local input source
            if (_AutoFindInputSource && _InputSource == null)
            {
                _InputSource = InterfaceHelper.GetComponent<IInputSource>(gameObject);
                if (_InputSource != null) { _InputSourceOwner = gameObject; }
            }

            // If that's still null, see if we can grab one from the scene. This may happen
            // if the MC was instanciated from a prefab which doesn't hold a reference to the input source
            if (_AutoFindInputSource && _InputSource == null)
            {
                IInputSource[] lInputSources = InterfaceHelper.GetComponents<IInputSource>();
                for (int i = 0; i < lInputSources.Length; i++)
                {
                    GameObject lInputSourceOwner = ((MonoBehaviour)lInputSources[i]).gameObject;
                    if (lInputSourceOwner.activeSelf && lInputSources[i].IsEnabled)
                    {
                        _InputSource = lInputSources[i];
                        _InputSourceOwner = lInputSourceOwner;
                    }
                }
            }

            // Initialize the spells
            for (int i = 0; i < _Spells.Count; i++)
            {
                _Spells[i].Owner = gameObject;
            }
        }

        /// <summary>
        /// Returns the index of the spell given the name
        /// </summary>
        /// <param name="rName">Name of the spell we are looking for</param>
        /// <returns>The index of the spell or -1 if not found</returns>
        public virtual int GetSpellIndex(string rName)
        {
            if (rName.Length == 0) { return -1; }

            for (int i = 0; i < _Spells.Count; i++)
            {
                if (string.Compare(_Spells[i].Name, rName, true) == 0)
                {
                    return i;
                }
            }

            return -1;
        }

        /// <summary>
        /// Returns the default spell that is to be cast
        /// </summary>
        /// <returns></returns>
        public virtual Spell InstantiateSpell(int rIndex = -1)
        {
            int lIndex = rIndex;
            if (lIndex < 0) { lIndex = _DefaultSpellIndex; }

            if (lIndex >= 0 && lIndex < _Spells.Count)
            {
                Spell lPrefab = _Spells[lIndex].SpellPrefab;

                //Utilities.Profiler.Start("CopySpell", "");

#if UNITY_EDITOR && NO_ALLOCATION
                Spell lInstance = ScriptableObjectPool.DeepCopy(lPrefab, true) as Spell;
#else
                Spell lInstance = ScriptableObjectPool.Allocate(lPrefab, true) as Spell;
#endif

                //Debug.Log("deep copy:" + Utilities.Profiler.Stop("CopySpell").ToString("f4"));

                if (lInstance != null)
                {
                    lInstance.Prefab = lPrefab;
                    lInstance.Owner = gameObject;
                    lInstance.SpellInventory = this;
                    lInstance.State = EnumSpellState.READY;

                    _ActiveSpells.Add(lInstance);
                    return lInstance;
                }
            }

            return null;
        }

        /// <summary>
        /// Update is called every frame, if the MonoBehaviour is enabled.
        /// </summary>
        protected virtual void Update()
        {
            // Check if we should cast a spell
            if (_ActionAlias.Length > 0 && _InputSource != null && _DefaultSpellIndex > 0 && _DefaultSpellIndex < _Spells.Count)
            {
                if (_InputSource.IsJustPressed(_ActionAlias))
                {
                    bool lCast = false;

                    // Activate the spell through the motion
                    MotionController lMotionController = gameObject.GetComponent<MotionController>();
                    if (lMotionController != null)
                    {
                        PMP_BasicSpellCastings lCastMotion = lMotionController.GetMotion<PMP_BasicSpellCastings>();
                        if (lCastMotion != null)
                        {
                            lCast = true;
                            lMotionController.ActivateMotion(lCastMotion, _DefaultSpellIndex);
                        }
                    }

                    // If we couldn't activate the motion, activate the spell directly
                    if (!lCast)
                    {
                        InstantiateSpell(_DefaultSpellIndex);
                    }
                }
            }

            // Update each active spell
            for (int i = 0; i < _ActiveSpells.Count; i++)
            {
                _ActiveSpells[i].Update();
            }

            // Release the completed spells
            for (int i = _ActiveSpells.Count - 1; i >= 0; i--)
            {
                Spell lSpell = _ActiveSpells[i];
                if (lSpell.State == EnumSpellState.COMPLETED)
                {
                    lSpell.Release();
                    _ActiveSpells.RemoveAt(i);
                }
            }
        }

#region Editor Functions

#if UNITY_EDITOR

            /// <summary>
            /// Allows us to re-open the last selected item
            /// </summary>
        public int EditorItemIndex = -1;

#endif

#endregion
    }
}
