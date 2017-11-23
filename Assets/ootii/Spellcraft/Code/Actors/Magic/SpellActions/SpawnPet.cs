using System;
using UnityEngine;
using com.ootii.Actors.LifeCores;
using com.ootii.Base;
using com.ootii.Helpers;
using com.ootii.Graphics.NodeGraph;

namespace com.ootii.Actors.Magic
{
    [Serializable]
    [BaseName("Spawn Pet")]
    [BaseDescription("Spawns a pet from the prefab that anchors to the target.")]
    public class SpawnPet : SpawnParticles
    {
        /// <summary>
        /// Entity that the pet is anchored to
        /// </summary>
        public Transform _Anchor = null;
        public Transform Anchor
        {
            get { return _Anchor; }
            set { _Anchor = value; }
        }

        /// <summary>
        /// Offset from the anchor the pet follows
        /// </summary>
        public Vector3 _AnchorOffset = Vector3.zero;
        public Vector3 AnchorOffset
        {
            get { return _AnchorOffset; }
            set { _AnchorOffset = value; }
        }

        /// <summary>
        /// Pet core that actually runs
        /// </summary>
        protected PetCore mPetCore = null;

        /// <summary>
        /// Called when the action is first activated
        /// <param name="rPreviousSpellActionState">State of the action prior to this one activating</param>
        public override void Activate(int rPreviousSpellActionState = -1, object rData = null)
        {
            mIsShuttingDown = false;

            base.Activate(rPreviousSpellActionState, rData);

            if (mInstances != null && mInstances.Count > 0)
            {
                if (_Spell.ShowDebug)
                {
                    for (int i = 0; i < mInstances.Count; i++)
                    {
                        mInstances[i].hideFlags = HideFlags.None;
                    }
                }

                mPetCore = mInstances[0].GetComponent<PetCore>();
                if (mPetCore != null)
                {
                    mPetCore.Age = 0f;

                    if (MaxAge > 0f && _DeactivationIndex == EnumSpellActionDeactivation.TIMER)
                    {
                        mPetCore.MaxAge = MaxAge;
                    }

                    mPetCore.Prefab = _Prefab;
                    mPetCore.Anchor = mTarget;
                    mPetCore.AnchorOffset = (mTarget != null ? mTarget.InverseTransformPoint(mInstances[0].transform.position) : mTargetPosition);
                    mPetCore.OnReleasedEvent = OnCoreReleased;
                    mPetCore.Play();
                }
            }

            // If there were no particles, stop
            if (mInstances[0] == null || mParticleCores == null)
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
        /// <param name="rDeltaTime">Time in seconds since the last update</param>
        public override void Update()
        {
            base.Update();
        }

        /// <summary>
        /// Called when the core has finished running and is released
        /// </summary>
        /// <param name="rCore"></param>
        protected override void OnCoreReleased(ILifeCore rCore, object rUserData = null)
        {
            if (object.ReferenceEquals(rCore, mPetCore))
            {
                mPetCore.OnReleasedEvent = null;
                mPetCore = null;

                base.OnCoreReleased(rCore, rUserData);
            }
        }

        #region Editor Functions

#if UNITY_EDITOR

        /// <summary>
        /// Called when the inspector needs to draw
        /// </summary>
        public override bool OnInspectorGUI(UnityEngine.Object rTarget)
        {
            bool lIsDirty = false;

            if (EditorHelper.TextField("Name", "Name of the spell action", Name, rTarget))
            {
                lIsDirty = true;
                Name = EditorHelper.FieldStringValue;
            }

            if (mEditorShowDeactivationField)
            {
                NodeEditorStyle.DrawLine(NodeEditorStyle.LineBlue);

                if (EditorHelper.PopUpField("Deactivation", "", DeactivationIndex, EnumSpellActionDeactivation.Names, rTarget))
                {
                    lIsDirty = true;
                    DeactivationIndex = EditorHelper.FieldIntValue;
                }

                if (DeactivationIndex == EnumSpellActionDeactivation.TIMER)
                {
                    if (EditorHelper.FloatField("Max Age", "Max age the action can live for.", MaxAge, rTarget))
                    {
                        lIsDirty = true;
                        MaxAge = EditorHelper.FieldFloatValue;
                    }
                }
            }

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

#endif

        #endregion
    }
}