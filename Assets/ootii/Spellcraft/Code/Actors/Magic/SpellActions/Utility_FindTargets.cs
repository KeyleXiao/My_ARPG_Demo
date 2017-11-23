using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using com.ootii.Actors.Attributes;
using com.ootii.Actors.LifeCores;
using com.ootii.Actors.Combat;
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
    [BaseName("Utility - Find Targets")]
    [BaseDescription("Gathers targets that are within a radius of the target position.")]
    public class Utility_FindTargets : SpellAction
    {
        // Conditions used to determine how we'll find the target
        public static string[] SearchTypes = new string[] { "Any", "Nearest", "Furthest" };

        /// <summary>
        /// Determines the source of the search
        /// </summary>
        public int _SearchRootIndex = 1;
        public int SearchRootIndex
        {
            get { return _SearchRootIndex; }
            set { _SearchRootIndex = value; }
        }

        /// <summary>
        /// Determines how we'll search
        /// </summary>
        public int _SearchTypeIndex = 0;
        public int SearchTypeIndex
        {
            get { return _SearchTypeIndex; }
            set { _SearchTypeIndex = value; }
        }

        /// <summary>
        /// Position offset from the root that we'll center the search
        /// </summary>
        public Vector3 _PositionOffset = Vector3.zero;
        public Vector3 PositionOffset
        {
            get { return _PositionOffset; }
            set { _PositionOffset = value; }
        }

        /// <summary>
        /// Number of targets we'll return
        /// </summary>
        public int _MaxTargets = 10;
        public int MaxTargets
        {
            get { return _MaxTargets; }
            set { _MaxTargets = value; }
        }

        /// <summary>
        /// Objects we'll collide with
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
        /// Radius of the explosive force
        /// </summary>
        public float _Radius = 5f;
        public float Radius
        {
            get { return _Radius; }
            set { _Radius = value; }
        }

        /// <summary>
        /// Determines if we replace the current list or append to it
        /// </summary>
        public bool _Replace = true;
        public bool Replace
        {
            get { return _Replace; }
            set { _Replace = value; }
        }

        /// <summary>
        /// Determines if we add previous targets to our ignore list
        /// </summary>
        public bool _IgnorePreviousTargets = true;
        public bool IgnorePreviousTargets
        {
            get { return _IgnorePreviousTargets; }
            set { _IgnorePreviousTargets = value; }
        }

        /// <summary>
        /// Determines if the targets require a rigidbody
        /// </summary>
        public bool _RequireRigidbody = false;
        public bool RequireRigidbody
        {
            get { return _RequireRigidbody; }
            set { _RequireRigidbody = value; }
        }

        /// <summary>
        /// Determines if the targets require a rigidbody
        /// </summary>
        public bool _RequireActorCore = false;
        public bool RequireActorCore
        {
            get { return _RequireActorCore; }
            set { _RequireActorCore = value; }
        }

        /// <summary>
        /// Determines if the targets require a rigidbody
        /// </summary>
        public bool _RequireCombatant = false;
        public bool RequireCombatant
        {
            get { return _RequireCombatant; }
            set { _RequireCombatant = value; }
        }

        public bool _UseBatches = false;
        public bool UseBatches
        {
            get { return _UseBatches; }
            set { _UseBatches = value; }
        }

        /// <summary>
        /// Name of the tag to send to activate links
        /// </summary>
        public string _OnBatchTag = "OnBatch";
        public string OnBatchTag
        {
            get { return _OnBatchTag; }
            set { _OnBatchTag = value; }
        }

        /// <summary>
        /// Minimum items per batch
        /// </summary>
        public int _MinBatchCount = 1;
        public int MinBatchCount
        {
            get { return _MinBatchCount; }
            set { _MinBatchCount = value; }
        }

        /// <summary>
        /// Maximum items per batc
        /// </summary>
        public int _MaxBatchCount = 1;
        public int MaxBatchCount
        {
            get { return _MaxBatchCount; }
            set { _MaxBatchCount = value; }
        }

        /// <summary>
        /// Minimum time between batches
        /// </summary>
        public float _MinBatchDelay = 0f;
        public float MinBatchDelay
        {
            get { return _MinBatchDelay; }
            set { _MinBatchDelay = value; }
        }

        /// <summary>
        /// Maximum time between batches
        /// </summary>
        public float _MaxBatchDelay = 0f;
        public float MaxBatchDelay
        {
            get { return _MaxBatchDelay; }
            set { _MaxBatchDelay = value; }
        }

        // Items that have been batched
        protected int mActivations = 0;

        // Current delay for the batch
        protected float mBatchDelay = 0f;

        // Last time a batch was sent
        protected float mLastBatchTime = 0f;

        /// <summary>
        /// Used to initialize any actions prior to them being activated
        /// </summary>
        public override void Awake()
        {
            base.Awake();

            if (_UseBatches)
            {
                _DeactivationIndex = EnumSpellActionDeactivation.MANAGED;
            }
            else
            {
                _DeactivationIndex = EnumSpellActionDeactivation.IMMEDIATELY;
            }
        }

        /// <summary>
        /// Called when the action is first activated
        /// <param name="rPreviousSpellActionState">State of the action prior to this one activating</param>
        public override void Activate(int rPreviousSpellActionState = -1, object rData = null)
        {
            base.Activate(rPreviousSpellActionState, rData);

            mActivations = 0;
            mBatchDelay = 0f;
            mLastBatchTime = 0f;

            if (_Spell != null && _Spell.Data != null)
            {
                SpellData lSpellData = _Spell.Data;

                Transform lCenter = null;
                Vector3 lCenterPosition = PositionOffset;
                GetBestPosition(SearchRootIndex, rData, _Spell.Data, PositionOffset, out lCenter, out lCenterPosition);

                // Ignore any existing targets for future tests
                if (IgnorePreviousTargets && lSpellData.Targets != null && lSpellData.Targets.Count > 0)
                {
                    if (lSpellData.PreviousTargets == null) { lSpellData.PreviousTargets = new List<GameObject>(); }

                    for (int i = 0; i < lSpellData.Targets.Count; i++)
                    {
                        if (!lSpellData.PreviousTargets.Contains(lSpellData.Targets[i]))
                        {
                            lSpellData.PreviousTargets.Add(lSpellData.Targets[i]);
                        }
                    }
                }

                // Remove any existing targets
                if (Replace)
                {
                    if (lSpellData.Targets != null)
                    {
                        lSpellData.Targets.Clear();
                        lSpellData.Targets = null;
                    }
                }

                // Find new targets
                if (lCenterPosition != Vector3Ext.Null)
                {
                    Collider[] lColliders = null;
                    List<GameObject> lTargets = null;

                    int lCount = RaycastExt.SafeOverlapSphere(lCenterPosition, Radius, out lColliders, CollisionLayers, _Spell.Owner.transform, null, true);
                    if (lColliders != null && lCount > 0)
                    {
                        if (lTargets == null) { lTargets = new List<GameObject>(); }

                        for (int i = 0; i < lCount; i++)
                        {
                            GameObject lGameObject = lColliders[i].gameObject;
                            if (lGameObject == _Spell.Owner) { continue; }
                            if (RequireRigidbody && lGameObject.GetComponent<Rigidbody>() == null) { continue; }
                            if (RequireActorCore && lGameObject.GetComponent<ActorCore>() == null) { continue; }
                            if (RequireCombatant && lGameObject.GetComponent<ICombatant>() == null) { continue; }
                            if (lSpellData.PreviousTargets != null && lSpellData.PreviousTargets.Contains(lGameObject)) { continue; }

                            if (_Tags != null && _Tags.Length > 0)
                            {
                                IAttributeSource lAttributeSource = lGameObject.GetComponent<IAttributeSource>();
                                if (lAttributeSource == null || !lAttributeSource.AttributesExist(_Tags)) { continue; }
                            }

                            if (!lTargets.Contains(lGameObject))
                            {
                                lTargets.Add(lGameObject);
                            }
                        }

                        // Sort the list based on distance
                        if (lTargets.Count > 1)
                        {
                            lTargets = lTargets.OrderBy(x => Vector3.Distance(lCenterPosition, x.transform.position)).ToList<GameObject>();
                        }
                    }

                    // Choose what we want
                    if (lTargets != null && lTargets.Count > 0)
                    {
                        if (lSpellData.Targets == null) { lSpellData.Targets = new List<GameObject>(); }

                        // Any or nearest
                        if (SearchTypeIndex == 0 || SearchTypeIndex == 1)
                        {
                            for (int i = 0; i < lTargets.Count; i++)
                            {
                                if (!lSpellData.Targets.Contains(lTargets[i])) { lSpellData.Targets.Add(lTargets[i]); }
                                if (MaxTargets > 0 && lSpellData.Targets.Count >= MaxTargets) break;
                            }
                        }
                        // Furthest
                        else if (SearchTypeIndex == 2)
                        {
                            for (int i = lTargets.Count - 1; i >= 0; i--)
                            {
                                if (!lSpellData.Targets.Contains(lTargets[i])) { lSpellData.Targets.Insert(0, lTargets[i]); }
                                if (MaxTargets > 0 && lSpellData.Targets.Count >= MaxTargets) break;
                            }
                        }
                    }
                }

                if (lSpellData.Targets != null && lSpellData.Targets.Count > 0)
                {
                    if (!UseBatches)
                    {
                        OnSuccess();
                        return;
                    }
                }
            }

            // Immediately deactivate
            if (!UseBatches)
            {
                OnFailure();
            }
        }

        /// <summary>
        /// Runs each frame to see if the action should continue
        /// </summary>
        public override void Update()
        {
            // Ensure we have valid targets
            if (_Spell.Data.Targets != null && _Spell.Data.Targets.Count > 0)
            {
                // Check if it's time to send a batch
                if (mLastBatchTime + mBatchDelay < Time.time)
                {
                    int lBatchCount = UnityEngine.Random.Range(MinBatchCount, MaxBatchCount + 1);
                    for (int i = 0; i < lBatchCount; i++)
                    {
                        if (mActivations < _Spell.Data.Targets.Count)
                        {
                            ActivateInstance(_Spell.Data.Targets[mActivations]);

                            mActivations++;
                        }
                    }

                    mLastBatchTime = Time.time;
                    mBatchDelay = UnityEngine.Random.Range(MinBatchDelay, MaxBatchDelay);
                }
            }

            // Determine if it's time to end
            if (_Spell.Data.Targets == null || mActivations >= _Spell.Data.Targets.Count)
            {
                OnSuccess();
            }
        }

        /// <summary>
        /// Activates a single target by running the links for that target
        /// </summary>
        /// <param name="rTarget"></param>
        protected virtual void ActivateInstance(GameObject rTarget)
        {
            // Check if we should force a node link to activate
            if (mNode != null)
            {
                for (int i = 0; i < mNode.Links.Count; i++)
                {
                    if (mNode.Links[i].TestActivate(_OnBatchTag))
                    {
                        _Spell.ActivateLink(mNode.Links[i], rTarget);
                    }
                }
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

            if (EditorHelper.PopUpField("Position Type", "", SearchRootIndex, SpellAction.GetBestPositionTypes, rTarget))
            {
                lIsDirty = true;
                SearchRootIndex = EditorHelper.FieldIntValue;
            }

            if (EditorHelper.Vector3Field("Position Offset", "Addition position value to use with the root.", PositionOffset, rTarget))
            {
                lIsDirty = true;
                PositionOffset = EditorHelper.FieldVector3Value;
            }

            GUILayout.Space(5f);

            if (EditorHelper.FloatField("Radius", "Radius of the explosive force", Radius, rTarget))
            {
                lIsDirty = true;
                Radius = EditorHelper.FieldFloatValue;
            }

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

            if (EditorHelper.BoolField("Ignore Previous Targets", "Determines if we add the previous targets to the ignore list.", IgnorePreviousTargets, rTarget))
            {
                lIsDirty = true;
                IgnorePreviousTargets = EditorHelper.FieldBoolValue;
            }

            if (EditorHelper.BoolField("Replace Targets", "Determines if we replace any existing targets.", Replace, rTarget))
            {
                lIsDirty = true;
                Replace = EditorHelper.FieldBoolValue;
            }

            if (EditorHelper.BoolField("Rigidbody Required", "Determines if the target must have a Rigidbody component.", RequireRigidbody, rTarget))
            {
                lIsDirty = true;
                RequireRigidbody = EditorHelper.FieldBoolValue;
            }

            if (EditorHelper.BoolField("ActorCore Required", "Determines if the target must have an ActorCore component.", RequireActorCore, rTarget))
            {
                lIsDirty = true;
                RequireActorCore = EditorHelper.FieldBoolValue;
            }

            if (EditorHelper.BoolField("Combatant Required", "Determines if the target must have a Combatant component.", RequireCombatant, rTarget))
            {
                lIsDirty = true;
                RequireCombatant = EditorHelper.FieldBoolValue;
            }

            NodeEditorStyle.DrawLine(NodeEditorStyle.LineBlue);

            if (EditorHelper.PopUpField("Results Order", "", SearchTypeIndex, SearchTypes, rTarget))
            {
                lIsDirty = true;
                SearchTypeIndex = EditorHelper.FieldIntValue;
            }

            if (EditorHelper.IntField("Max Targets", "Max number of targets to return", MaxTargets, rTarget))
            {
                lIsDirty = true;
                MaxTargets = EditorHelper.FieldIntValue;
            }

            NodeEditorStyle.DrawLine(NodeEditorStyle.LineBlue);

            if (EditorHelper.BoolField("Use Batches", "Determines if we'll batch the targets and activate the node links individually.", UseBatches, rTarget))
            {
                lIsDirty = true;
                UseBatches = EditorHelper.FieldBoolValue;
            }

            if (UseBatches)
            {
                if (EditorHelper.TextField("Batch Tag", "Optional tag used to activate the links for each batch.", OnBatchTag, rTarget))
                {
                    lIsDirty = true;
                    OnBatchTag = EditorHelper.FieldStringValue;
                }

                // Instances
                UnityEngine.GUILayout.BeginHorizontal();

                UnityEditor.EditorGUILayout.LabelField(new UnityEngine.GUIContent("Batch Items", "Min and Max items per batch."), UnityEngine.GUILayout.Width(UnityEditor.EditorGUIUtility.labelWidth));

                if (EditorHelper.IntField(MinBatchCount, "Min Time", rTarget, 0f, 20f))
                {
                    lIsDirty = true;
                    MinBatchCount = EditorHelper.FieldIntValue;
                }

                if (EditorHelper.IntField(MaxBatchCount, "Max Time", rTarget, 0f, 20f))
                {
                    lIsDirty = true;
                    MaxBatchCount = EditorHelper.FieldIntValue;
                }

                UnityEngine.GUILayout.EndHorizontal();

                // Delay
                UnityEngine.GUILayout.BeginHorizontal();

                UnityEditor.EditorGUILayout.LabelField(new UnityEngine.GUIContent("Batch Delay", "Min and Max seconds between batches."), UnityEngine.GUILayout.Width(UnityEditor.EditorGUIUtility.labelWidth));

                if (EditorHelper.FloatField(MinBatchDelay, "Min Delay", rTarget, 0f, 20f))
                {
                    lIsDirty = true;
                    MinBatchDelay = EditorHelper.FieldFloatValue;
                }

                if (EditorHelper.FloatField(MaxBatchDelay, "Max Delay", rTarget, 0f, 20f))
                {
                    lIsDirty = true;
                    MaxBatchDelay = EditorHelper.FieldFloatValue;
                }

                UnityEngine.GUILayout.EndHorizontal();
            }

            return lIsDirty;
        }

#endif

        #endregion
    }
}