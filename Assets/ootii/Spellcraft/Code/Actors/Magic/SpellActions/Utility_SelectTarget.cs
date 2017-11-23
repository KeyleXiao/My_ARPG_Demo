using System;
using System.Collections.Generic;
using UnityEngine;
using com.ootii.Actors.LifeCores;
using com.ootii.Base;
using com.ootii.Geometry;
using com.ootii.Graphics.NodeGraph;
using com.ootii.Helpers;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace com.ootii.Actors.Magic
{
    [Serializable]
    [BaseName("Utility - Select Target")]
    [BaseDescription("Selects a target for a future action.")]
    public class Utility_SelectTarget : SpawnGameObject
    {
        /// <summary>
        /// Action alias for the spell to complete
        /// </summary>
        public string _ActionAlias = "Spell Casting Continue";
        public string ActionAlias
        {
            get { return _ActionAlias; }
            set { _ActionAlias = value; }
        }

        /// <summary>
        /// Action alias for the spell to cancel
        /// </summary>
        public string _CancelActionAlias = "Spell Casting Cancel";
        public string CancelActionAlias
        {
            get { return _CancelActionAlias; }
            set { _CancelActionAlias = value; }
        }
        
        /// <summary>
        /// Minimum Distance the projectile can succeed
        /// </summary>
        public float _MinDistance = 0f;
        public float MinDistance
        {
            get { return _MinDistance; }
            set { _MinDistance = value; }
        }

        /// <summary>
        /// Maximum Distance the projectile can succeed
        /// </summary>
        public float _MaxDistance = 15f;
        public float MaxDistance
        {
            get { return _MaxDistance; }
            set { _MaxDistance = value; }
        }

        /// <summary>
        /// Determines if we continuously select based on the active ray or only when the
        /// input is activated.
        /// </summary>
        public bool _ContinuousSelect = true;
        public bool ContinuousSelect
        {
            get { return _ContinuousSelect; }
            set { _ContinuousSelect = value; }
        }

        /// <summary>
        /// Determines if we're using the mouse to select
        /// </summary>
        public bool _UseMouse = false;
        public bool UseMouse
        {
            get { return _UseMouse; }
            set { _UseMouse = value; }
        }

        /// <summary>
        /// Layers that we can collide with
        /// </summary>
        public int _CollisionLayers = 1;
        public int CollisionLayers
        {
            get { return _CollisionLayers; }
            set { _CollisionLayers = value; }
        }

        /// <summary>
        /// Comma delimited list of tags where one must exists in order
        /// for the position to be valid
        /// </summary>
        public string _Tags = "";
        public string Tags
        {
            get { return _Tags; }
            set { _Tags = value; }
        }

        /// <summary>
        /// Determines if the targets require a rigidbody
        /// </summary>
        public bool _RequiresRigidbody = false;
        public bool RequiresRigidbody
        {
            get { return _RequiresRigidbody; }
            set { _RequiresRigidbody = value; }
        }

        /// <summary>
        /// Determines if the target must have an actor core
        /// </summary>
        public bool _RequiresActorCore = true;
        public bool RequiresActorCore
        {
            get { return _RequiresActorCore; }
            set { _RequiresActorCore = value; }
        }

        /// <summary>
        /// Determines if the target must be a combatant
        /// </summary>
        public bool _RequiresCombatant = true;
        public bool RequiresCombatant
        {
            get { return _RequiresCombatant; }
            set { _RequiresCombatant = value; }
        }

        /// <summary>
        /// Life core used for a projector object
        /// </summary>
        protected SelectTargetCore mSelectorCore = null;

        /// <summary>
        /// Used to initialize any actions prior to them being activated
        /// </summary>
        public override void Awake()
        {
            // Create the pool of prefabs
            base.Awake();
        }

        /// <summary>
        /// Called when the action is first activated
        /// <param name="rPreviousSpellActionState">State of the action prior to this one activating</param>
        public override void Activate(int rPreviousSpellActionState = -1, object rData = null)
        {
            // Ensure we don't parent to the target
            ParentToTarget = false;

            // Grab an instance from the pool
            base.Activate(rPreviousSpellActionState, rData);

            // Set the material
            if (mInstances != null && mInstances.Count > 0)
            {
                mSelectorCore = mInstances[0].GetComponent<SelectTargetCore>();
                if (mSelectorCore != null)
                {
                    mSelectorCore.Age = 0f;
                    mSelectorCore.MaxAge = 0f;
                    mSelectorCore.Prefab = _Prefab;
                    mSelectorCore.Owner = _Spell.Owner;
                    mSelectorCore.InputSource = _Spell.SpellInventory._InputSource;
                    mSelectorCore.MinDistance = MinDistance;
                    mSelectorCore.MaxDistance = MaxDistance;
                    mSelectorCore.UseMouse = UseMouse;
                    mSelectorCore.ContinuousSelect = ContinuousSelect;
                    mSelectorCore.CollisionLayers = CollisionLayers;
                    mSelectorCore.Tags = Tags;
                    mSelectorCore.ActionAlias = ActionAlias;
                    mSelectorCore.CancelActionAlias = CancelActionAlias;
                    mSelectorCore.RequiresRigidbody = RequiresRigidbody;
                    mSelectorCore.RequiresActorCore = RequiresActorCore;
                    mSelectorCore.RequiresCombatant = RequiresCombatant;
                    mSelectorCore.OnSelectedEvent = OnSelected;
                    mSelectorCore.OnCancelledEvent = OnCancelled;
                    mSelectorCore.OnReleasedEvent = OnCoreReleased;
                    mSelectorCore.Play();
                }
            }
            else
            {
                Deactivate();
            }
        }

        /// <summary>
        /// Called when the action is meant to be deactivated
        /// </summary>
        public override void Deactivate()
        {
            base.Deactivate();
        }

        /// <summary>
        /// Called each frame that the action is active
        /// </summary>
        public override void Update()
        {
        }

        /// <summary>
        /// Called when the selector has finished selecting
        /// </summary>
        /// <param name="rCore"></param>
        /// <param name="rUserData"></param>
        private void OnSelected(ILifeCore rCore, object rUserData = null)
        {
            Transform lTarget = null;

            if (rCore is SelectTargetCore)
            {
                lTarget = ((SelectTargetCore)rCore).SelectedTarget;
            }

            if (lTarget != null)
            {
                // Determine if we're done choosing a target
                if (_Spell.Data.Targets == null) { _Spell.Data.Targets = new List<GameObject>(); }
                _Spell.Data.Targets.Clear();

                _Spell.Data.Targets.Add(lTarget.gameObject);

                // Flag the action as done
                State = EnumSpellActionState.SUCCEEDED;
                mIsShuttingDown = true;
                //OnSuccess();
            }
            else
            {
                OnFailure();
            }
        }

        /// <summary>
        /// Called when the selector has finished selecting
        /// </summary>
        /// <param name="rCore"></param>
        /// <param name="rUserData"></param>
        private void OnCancelled(ILifeCore rCore, object rUserData = null)
        {
            State = EnumSpellActionState.FAILED;
            mIsShuttingDown = true;
        }

        /// <summary>
        /// Called when the core has finished running and is released
        /// </summary>
        /// <param name="rCore"></param>
        private void OnCoreReleased(ILifeCore rCore, object rUserData = null)
        {
            if (mSelectorCore != null)
            {
                mSelectorCore.Owner = null;
                mSelectorCore.InputSource = null;
                mSelectorCore.OnSelectedEvent = null;
                mSelectorCore.OnReleasedEvent = null;
                mSelectorCore = null;
            }

            // If our state isn't set to succes (meaning we have something)
            // we are going to flag this node as a failure
            if (State != EnumSpellActionState.SUCCEEDED)
            {
                State = EnumSpellActionState.FAILED;
            }

            base.Deactivate();
        }

        #region Editor Functions

#if UNITY_EDITOR

        /// <summary>
        /// Called when the inspector needs to draw
        /// </summary>
        public override bool OnInspectorGUI(UnityEngine.Object rTarget)
        {
            mEditorShowPrefabField = true;
            mEditorShowParentFields = false;
            bool lIsDirty = base.OnInspectorGUI(rTarget);

            NodeEditorStyle.DrawLine(NodeEditorStyle.LineBlue);

            if (EditorHelper.TextField("Action Alias", "Action alias used to select the position", ActionAlias, rTarget))
            {
                lIsDirty = true;
                ActionAlias = EditorHelper.FieldStringValue;
            }

            if (EditorHelper.TextField("Cancel Alias", "Action alias used to cancel the selection", CancelActionAlias, rTarget))
            {
                lIsDirty = true;
                CancelActionAlias = EditorHelper.FieldStringValue;
            }

            if (EditorHelper.BoolField("Use Mouse", "Determines if we use the mouse to select the position or the reticle (camera).", UseMouse, mTarget))
            {
                lIsDirty = true;
                UseMouse = EditorHelper.FieldBoolValue;
            }

            if (EditorHelper.BoolField("Continuous Select", "Determines if we continually select a target based on the ray or only select when the Action Alias is pressed.", ContinuousSelect, rTarget))
            {
                lIsDirty = true;
                ContinuousSelect = EditorHelper.FieldBoolValue;
            }

            GUILayout.Space(5f);

            // Distance
            GUILayout.BeginHorizontal();

            EditorGUILayout.LabelField(new GUIContent("Distance", "Min and max Distance for the projectile to succeed."), GUILayout.Width(EditorGUIUtility.labelWidth - 4f));

            if (EditorHelper.FloatField(MinDistance, "Min Distance", rTarget, 0f, 20f))
            {
                lIsDirty = true;
                MinDistance = EditorHelper.FieldFloatValue;
            }

            if (EditorHelper.FloatField(MaxDistance, "Max Distance", rTarget, 0f, 20f))
            {
                lIsDirty = true;
                MaxDistance = EditorHelper.FieldFloatValue;
            }

            GUILayout.EndHorizontal();

            GUILayout.Space(5f);

            int lNewGroundingLayers = EditorHelper.LayerMaskField(new GUIContent("Layers", "Layers that we'll test collisions against."), CollisionLayers);
            if (lNewGroundingLayers != CollisionLayers)
            {
                lIsDirty = true;
                CollisionLayers = lNewGroundingLayers;
            }

            if (EditorHelper.TextField("Tags", "Comma delimited list of tags where at least one must exist for the selection to be valid.", Tags, rTarget))
            {
                lIsDirty = true;
                Tags = EditorHelper.FieldStringValue;
            }

            if (EditorHelper.BoolField("Rigidbody Required", "Determines if the target must have a Rigidbody component.", RequiresRigidbody, rTarget))
            {
                lIsDirty = true;
                RequiresRigidbody = EditorHelper.FieldBoolValue;
            }

            if (EditorHelper.BoolField("ActorCore Required", "Determines if an Actor Core is required to be a target.", RequiresActorCore, rTarget))
            {
                lIsDirty = true;
                RequiresActorCore = EditorHelper.FieldBoolValue;
            }

            if (EditorHelper.BoolField("Combatant Required", "Determines if a Combatant is required to be a target.", RequiresCombatant, rTarget))
            {
                lIsDirty = true;
                RequiresCombatant = EditorHelper.FieldBoolValue;
            }

            return lIsDirty;
        }

#endif

        #endregion
    }
}