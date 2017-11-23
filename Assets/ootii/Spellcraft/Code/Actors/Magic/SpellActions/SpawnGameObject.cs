using System;
using System.Collections.Generic;
using UnityEngine;
using com.ootii.Base;
using com.ootii.Collections;
using com.ootii.Geometry;
using com.ootii.Graphics.NodeGraph;
using com.ootii.Helpers;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace com.ootii.Actors.Magic
{
    [Serializable]
    [BaseName("Spawn GameObject")]
    [BaseDescription("Spawns an instance of the specified prefab and parents it to a target if needed.")]
    public class SpawnGameObject : SpellAction
    {
        /// <summary>
        /// Prefab that is the GameObject being spawned
        /// </summary>
        public GameObject _Prefab = null;
        public virtual GameObject Prefab
        {
            get { return _Prefab; }
            set { _Prefab = value; }
        }

        /// <summary>
        /// ID to give the instance we create. This way we can find it later
        /// </summary>
        public string _InstanceName = "";
        public string InstanceName
        {
            get { return _InstanceName; }
            set { _InstanceName = value; }
        }

        /// <summary>
        /// Determines how we'll position the game object at creation
        /// </summary>
        public int _PositionTypeIndex = 0;
        public int PositionTypeIndex
        {
            get { return _PositionTypeIndex; }
            set { _PositionTypeIndex = value; }
        }

        /// <summary>
        /// Determines if we make the GameObject a child of the target
        /// </summary>
        public bool _ParentToTarget = true;
        public bool ParentToTarget
        {
            get { return _ParentToTarget; }
            set { _ParentToTarget = value; }
        }

        /// <summary>
        /// Parent object we'll tie the spawned object to. Typically this is the
        /// character casting the spell.
        /// </summary>
        [NonSerialized]
        protected Transform mTarget = null;
        public Transform Target
        {
            get { return mTarget; }
            set { mTarget = value; }
        }

        /// <summary>
        /// Position we'll spawn the object at.
        /// </summary>
        [NonSerialized]
        protected Vector3 mTargetPosition = Vector3.zero;
        public Vector3 TargetPosition
        {
            get { return mTargetPosition; }
            set { mTargetPosition = value; }
        }

        /// <summary>
        /// Bone index on the target we'll tie the spawned object to. A value of
        /// 0 means there is no target.
        /// </summary>
        public int _BoneIndex = 0;
        public int BoneIndex
        {
            get { return _BoneIndex; }
            set { _BoneIndex = value; }
        }

        /// <summary>
        /// Name of the bone we're tying the spawned object to.
        /// </summary>
        public string _BoneName = "";
        public string BoneName
        {
            get { return _BoneName; }
            set { _BoneName = value; }
        }

        /// <summary>
        /// Local position relative to the parent
        /// </summary>
        public Vector3 _LocalPosition = Vector3.zero;
        public Vector3 LocalPosition
        {
            get { return _LocalPosition; }
            set { _LocalPosition = value; }
        }

        /// <summary>
        /// Instance of the prefab that is active
        /// </summary>
        [NonSerialized]
        protected List<GameObject> mInstances = null;
        public List<GameObject> Instances
        {
            get { return mInstances; }
        }

        /// <summary>
        /// Determines if the content is processed immediately. In this case,
        /// the flow is also immediate and no Update() is used.
        /// </summary>
        public override bool IsImmediate
        {
            get { return false; }
        }

        /// <summary>
        /// Used to initialize any actions prior to them being activated
        /// </summary>
        public override void Awake()
        {
            base.Awake();
        }

        /// <summary>
        /// Called when the action is first activated
        /// <param name="rPreviousSpellActionState">State of the action prior to this one activating</param>
        public override void Activate(int rPreviousSpellActionState = -1, object rData = null)
        {
            mAge = 0f;
            mIsShuttingDown = false;
            State = EnumSpellActionState.ACTIVE;

            if (_Prefab != null)
            {
                // SpellData Positions
                if (_PositionTypeIndex == 3)
                {
                    if (_Spell.Data.Positions != null)
                    {
                        for (int i = 0; i < _Spell.Data.Positions.Count; i++)
                        {
                            Vector3 lPosition = _Spell.Data.Positions[i];
                            GameObject lInstance = ActivateInstance(null, lPosition);
                            if (lInstance != null)
                            {
                                if (mInstances == null) { mInstances = new List<GameObject>(); }
                                mInstances.Add(lInstance);
                            }
                        }
                    }
                }
                // SpellData Targets
                else if (_PositionTypeIndex == 4)
                {
                    if (_Spell.Data.Targets != null)
                    {
                        for (int i = 0; i < _Spell.Data.Targets.Count; i++)
                        {
                            Transform lTarget = _Spell.Data.Targets[i].transform;
                            if (lTarget != null)
                            {
                                Vector3 lTargetPosition = lTarget.position + (lTarget.rotation * LocalPosition);

                                GameObject lInstance = ActivateInstance(lTarget, lTargetPosition);
                                if (lInstance != null)
                                {
                                    if (mInstances == null) { mInstances = new List<GameObject>(); }
                                    mInstances.Add(lInstance);
                                }
                            }
                        }
                    }
                }
                // SpellData Prev Targets
                else if (_PositionTypeIndex == 5)
                {
                    if (_Spell.Data.PreviousTargets != null && _Spell.Data.PreviousTargets.Count > 0)
                    {
                        Transform lTarget = _Spell.Data.PreviousTargets[_Spell.Data.PreviousTargets.Count - 1].transform;
                        if (lTarget != null)
                        {
                            Vector3 lTargetPosition = lTarget.position + (lTarget.rotation * LocalPosition);

                            GameObject lInstance = ActivateInstance(lTarget, lTargetPosition);
                            if (lInstance != null)
                            {
                                if (mInstances == null) { mInstances = new List<GameObject>(); }
                                mInstances.Add(lInstance);
                            }
                        }
                    }
                }
                // Single instance
                else
                {
                    GetBestPosition(_PositionTypeIndex, rData, _Spell.Data, LocalPosition, out mTarget, out mTargetPosition);

                    GameObject lInstance = ActivateInstance(mTarget, mTargetPosition);
                    if (lInstance != null)
                    {
                        if (mInstances == null) { mInstances = new List<GameObject>(); }
                        mInstances.Add(lInstance);
                    }
                }
            }
        }

        public GameObject ActivateInstance(Transform rTarget, Vector3 rTargetPosition)
        {
            if (rTarget == null && rTargetPosition == Vector3Ext.Null) { return null; }

            GameObject lInstance = GameObject.Instantiate(_Prefab);
            if (lInstance != null)
            {
                // Grab the best target for where we create the game object
                //GetBestTarget(_PositionTypeIndex, rData, _Spell.Data, LocalPosition, out mTarget, out mTargetPosition);

                // Set the name so we can grab it later
                if (_InstanceName.Length > 0) { lInstance.name = _InstanceName; }

                // Fixed position
                if (PositionTypeIndex == 0 || rTarget == null)
                {
                    lInstance.transform.parent = null;
                    lInstance.transform.position = rTargetPosition;
                }
                // If there's no bone, use the target transform and position
                else if (_BoneIndex == 0)
                {
                    lInstance.transform.parent = null;
                    lInstance.transform.position = rTargetPosition;

                    // Tie it to the parent if needed
                    if (ParentToTarget)
                    {
                        lInstance.transform.parent = rTarget;
                    }
                }
                // Check if we have a bone we're tying to
                else if (_BoneIndex > 0)
                {
                    Transform lParent = rTarget;

                    // Find the real parent
                    if (_BoneIndex == 1 && _BoneName.Length > 0)
                    {
                        Transform lBone = lParent.FindTransform(_BoneName);
                        if (lBone != null) { lParent = lBone; }
                    }
                    else if (_BoneIndex > 1 && _BoneName.Length > 0)
                    {
                        try
                        {
                            HumanBodyBones lHumanBodyBone = (HumanBodyBones)Enum.Parse(typeof(HumanBodyBones), _BoneName, true);
                            Transform lBone = lParent.FindTransform(lHumanBodyBone);
                            if (lBone != null) { lParent = lBone; }
                        }
                        catch { }
                    }

                    // Set the parent and local values
                    lInstance.transform.parent = lParent;
                    lInstance.transform.localRotation = Quaternion.identity;
                    lInstance.transform.localPosition = LocalPosition;

                    // Remove the parent if it isn't wanted
                    if (!ParentToTarget)
                    {
                        lInstance.transform.parent = null;
                        lInstance.transform.rotation = _Spell.Owner.transform.rotation;
                    }
                }

                lInstance.SetActive(true);
            }

            return lInstance;
        }

        /// <summary>
        /// Called when the action is meant to be deactivated
        /// </summary>
        public override void Deactivate()
        {
            base.Deactivate();

            if (mInstances != null)
            {
                for (int i = 0; i < mInstances.Count; i++)
                {
                    GameObject.Destroy(mInstances[i]);
                }

                mInstances.Clear();
            }
        }

        #region Editor Functions

#if UNITY_EDITOR

        /// <summary>
        /// Unity bone names
        /// </summary>
        protected static string[] sHumanBodyBones = null;

        /// <summary>
        /// Determine if we show the prefab field
        /// </summary>
        protected bool mEditorShowPrefabField = true;

        /// <summary>
        /// Determines if we show the parenting fields
        /// </summary>
        protected bool mEditorShowParentFields = true;

        /// <summary>
        /// Called before the action is removed. Allows us to clear any variables
        /// </summary>
        public override void Clear(UnityEngine.Object rTarget)
        {
            // We can clear the prefab since it's a prefab for the GameObject
            // to spawn, not a prefab for this action
            Prefab = null;
        }

        /// <summary>
        /// Called when the inspector needs to draw
        /// </summary>
        public override bool OnInspectorGUI(UnityEngine.Object rTarget)
        {
            mEditorShowDeactivationField = true;
            bool lIsDirty = base.OnInspectorGUI(rTarget);

            if (mEditorShowPrefabField || mEditorShowParentFields)
            {
                NodeEditorStyle.DrawLine(NodeEditorStyle.LineBlue);

                if (mEditorShowPrefabField)
                {
                    if (EditorHelper.ObjectField<GameObject>("Prefab", "Prefab we'll use as a template to spawn GameObjects.", Prefab, rTarget))
                    {
                        lIsDirty = true;
                        Prefab = EditorHelper.FieldObjectValue as GameObject;
                    }

                    if (EditorHelper.TextField("Instance Name", "Unique Name to give an instance so other actions can reference it later.", InstanceName, rTarget))
                    {
                        lIsDirty = true;
                        InstanceName = EditorHelper.FieldStringValue;
                    }

                    GUILayout.Space(5f);
                }

                if (mEditorShowParentFields)
                {
                    if (EditorHelper.PopUpField("Position Type", "Determines the kind of positioning we'll use.", PositionTypeIndex, SpellAction.GetBestPositionTypes, rTarget))
                    {
                        lIsDirty = true;
                        PositionTypeIndex = EditorHelper.FieldIntValue;
                    }

                    // Fixed position
                    if (PositionTypeIndex == 0)
                    {
                        if (EditorHelper.Vector3Field("Position", "World position to spawn the object.", LocalPosition, rTarget))
                        {
                            lIsDirty = true;
                            LocalPosition = EditorHelper.FieldVector3Value;
                        }
                    }
                    // Owner transform
                    else if (PositionTypeIndex == 1)
                    {
                        //GUILayout.Space(5f);

                        //if (EditorHelper.BoolField("Attach to Owner", "Determines if we make the GameObject a child of the target", ParentToTarget, rTarget))
                        //{
                        //    lIsDirty = true;
                        //    ParentToTarget = EditorHelper.FieldBoolValue;
                        //}

                        //if (ParentToTarget)
                        //{
                            if (sHumanBodyBones == null || sHumanBodyBones.Length == 0) { LoadBoneNames(); }
                            if (EditorHelper.PopUpField("Bone", "Bone name to tie the GameObject to.", BoneIndex, sHumanBodyBones, rTarget))
                            {
                                lIsDirty = true;
                                BoneIndex = EditorHelper.FieldIntValue;

                                if (_BoneIndex > 1)
                                {
                                    BoneName = sHumanBodyBones[_BoneIndex].Replace("Unity ", "");
                                }
                            }

                            if (_BoneIndex == 1)
                            {
                                if (EditorHelper.TextField("Bone Name", "Name of the custom bone to tie the GameObject to.", BoneName, rTarget))
                                {
                                    lIsDirty = true;
                                    BoneName = EditorHelper.FieldStringValue;
                                }
                            }

                            //if (_BoneIndex > 0)
                            {
                                if (EditorHelper.Vector3Field("Offset", "Local position relative to the bone the instance should be parented to", LocalPosition, rTarget))
                                {
                                    lIsDirty = true;
                                    LocalPosition = EditorHelper.FieldVector3Value;
                                }
                            }
                        //}
                        if (EditorHelper.BoolField("Attach to Owner", "Determines if we make the GameObject a child of the target", ParentToTarget, rTarget))
                        {
                            lIsDirty = true;
                            ParentToTarget = EditorHelper.FieldBoolValue;
                        }
                    }
                    // Data Vector3
                    else if (PositionTypeIndex == 2)
                    {
                        if (EditorHelper.Vector3Field("Offset", "Local position relative to the bone the instance should be parented to", LocalPosition, rTarget))
                        {
                            lIsDirty = true;
                            LocalPosition = EditorHelper.FieldVector3Value;
                        }
                    }
                    // Data Transform
                    else
                    {
                        //GUILayout.Space(5f);

                        //if (EditorHelper.BoolField("Attach to Owner", "Determines if we make the GameObject a child of the target", ParentToTarget, rTarget))
                        //{
                        //    lIsDirty = true;
                        //    ParentToTarget = EditorHelper.FieldBoolValue;
                        //}

                        //if (ParentToTarget)
                        //{
                        if (sHumanBodyBones == null || sHumanBodyBones.Length == 0) { LoadBoneNames(); }
                        if (EditorHelper.PopUpField("Bone", "Bone name to tie the GameObject to.", BoneIndex, sHumanBodyBones, rTarget))
                        {
                            lIsDirty = true;
                            BoneIndex = EditorHelper.FieldIntValue;

                            if (_BoneIndex > 1)
                            {
                                BoneName = sHumanBodyBones[_BoneIndex].Replace("Unity ", "");
                            }
                        }

                        if (_BoneIndex == 1)
                        {
                            if (EditorHelper.TextField("Bone Name", "Name of the custom bone to tie the GameObject to.", BoneName, rTarget))
                            {
                                lIsDirty = true;
                                BoneName = EditorHelper.FieldStringValue;
                            }
                        }

                        //if (_BoneIndex > 0)
                        {
                            if (EditorHelper.Vector3Field("Offset", "Local position relative to the bone the instance should be parented to", LocalPosition, rTarget))
                            {
                                lIsDirty = true;
                                LocalPosition = EditorHelper.FieldVector3Value;
                            }
                        }
                        //}
                        if (EditorHelper.BoolField("Attach to Owner", "Determines if we make the GameObject a child of the target", ParentToTarget, rTarget))
                        {
                            lIsDirty = true;
                            ParentToTarget = EditorHelper.FieldBoolValue;
                        }
                    }
                }
            }

            return lIsDirty;
        }

        /// <summary>
        /// Builds the list of bones to tie the mount point to
        /// </summary>
        protected void LoadBoneNames()
        {
            sHumanBodyBones = Enum.GetNames(typeof(HumanBodyBones));

            Array.Resize(ref sHumanBodyBones, sHumanBodyBones.Length + 2);
            for (int i = sHumanBodyBones.Length - 1; i > 1; i--)
            {
                sHumanBodyBones[i] = "Unity " + sHumanBodyBones[i - 2];
            }

            sHumanBodyBones[0] = "None";
            sHumanBodyBones[1] = "Custom Bone";
        }

#endif

        #endregion
    }
}