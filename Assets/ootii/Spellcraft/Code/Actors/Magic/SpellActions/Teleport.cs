using System;
using UnityEngine;
using com.ootii.Actors.AnimationControllers;
using com.ootii.Base;
using com.ootii.Graphics.NodeGraph;
using com.ootii.Helpers;

namespace com.ootii.Actors.Magic
{
    [Serializable]
    [BaseName("Teleport")]
    [BaseDescription("Teleports the target to the specified target position.")]
    public class Teleport : SpellAction
    {
        /// <summary>
        /// Determines if we're teleporting ourself or a target(s)
        /// </summary>
        public bool _TeleportSelf = true;
        public bool TeleportSelf
        {
            get { return _TeleportSelf; }
            set { _TeleportSelf = value; }
        }

        /// <summary>
        /// Determines if we'll attempt to teleport the camera too
        /// </summary>
        public bool _TeleportCamera = true;
        public bool TeleportCamera
        {
            get { return _TeleportCamera; }
            set { _TeleportCamera = value; }
        }

        /// <summary>
        /// Fixed position to teleport to
        /// </summary>
        public Vector3 _Position = Vector3.zero;
        public Vector3 Position
        {
            get { return _Position; }
            set { _Position = value; }
        }

        /// <summary>
        /// Used to initialize any actions prior to them being activated
        /// </summary>
        public override void Awake()
        {
            _DeactivationIndex = EnumSpellActionDeactivation.IMMEDIATELY;
            base.Awake();
        }

        /// <summary>
        /// Called when the action is first activated
        /// <param name="rPreviousSpellActionState">State of the action prior to this one activating</param>
        public override void Activate(int rPreviousSpellActionState = -1, object rData = null)
        {
            // Ensure we have a position
            Vector3 lPosition = GetBestPosition(rData, _Spell.Data);

            // Teleport the target to that position
            GameObject lTarget = null;

            // If we're not teleporting ourself, look for a target
            if (TeleportSelf)
            {
                lTarget = _Spell.Owner;
            }
            else
            {
                lTarget = GetBestTarget(1, rData, _Spell.Data);
            }

            // Determine the position
            if (lTarget != null)
            {
                // Determine if we should be teleporting the camera too
                MotionController lMotionController = lTarget.GetComponent<MotionController>();
                if (TeleportCamera && lMotionController != null && lMotionController.CameraTransform != null)
                {
                    Vector3 lLocalPosition = lTarget.transform.InverseTransformPoint(lMotionController.CameraTransform.position);

                    lTarget.transform.position = lPosition;

                    lMotionController.CameraTransform.position = lTarget.transform.TransformPoint(lLocalPosition);
                }
                // Otherwise, just move the target
                else
                {
                    lTarget.transform.position = lPosition;
                }
            }
        }

        ///// <summary>
        ///// Utility function used to get the target given some different options
        ///// </summary>
        ///// <param name="rData">Data that typically comes from activation</param>
        ///// <param name="rSpellData">SpellData belonging to the spell</param>
        ///// <returns>Transform that is the expected target or null</returns>
        //protected virtual Transform GetBestTarget(object rData, SpellData rSpellData)
        //{
        //    Transform lTarget = null;

        //    if (rData != null)
        //    {
        //        if (rData is Collider)
        //        {
        //            lTarget = ((Collider)rData).gameObject.transform;
        //        }
        //        else if (rData is Transform)
        //        {
        //            lTarget = (Transform)rData;
        //        }
        //        else if (rData is GameObject)
        //        {
        //            lTarget = ((GameObject)rData).transform;
        //        }
        //        else if (rData is MonoBehaviour)
        //        {
        //            lTarget = ((MonoBehaviour)rData).gameObject.transform;
        //        }
        //    }

        //    if (lTarget == null && rSpellData != null)
        //    {
        //        if (rSpellData.Targets != null && rSpellData.Targets.Count > 0)
        //        {
        //            lTarget = rSpellData.Targets[0].transform;
        //        }
        //    }

        //    return lTarget;
        //}

        /// <summary>
        /// Utility function used to get the position given some different options
        /// </summary>
        /// <param name="rData">Data that typically comes from activation</param>
        /// <param name="rSpellData">SpellData belonging to the spell</param>
        /// <returns>Vector3 that is the target position</returns>
        protected virtual Vector3 GetBestPosition(object rData, SpellData rSpellData)
        {
            if (Position.sqrMagnitude > 0f)
            {
                return Position;
            }

            if (rData != null && rData != _Spell.Data)
            {
                if (rData is Vector3)
                {
                    return (Vector3)rData;
                }
                else if (rData is Collider)
                {
                    return ((Collider)rData).gameObject.transform.position;
                }
                else if (rData is Transform)
                {
                    return ((Transform)rData).position;
                }
                else if (rData is GameObject)
                {
                    return ((GameObject)rData).transform.position;
                }
                else if (rData is MonoBehaviour)
                {
                    return ((MonoBehaviour)rData).gameObject.transform.position;
                }
            }

            if (rSpellData.Positions != null && rSpellData.Positions.Count > 0)
            {
                return rSpellData.Positions[0];
            }
            else if (rSpellData.Targets != null && rSpellData.Targets.Count > 0)
            {
                return rSpellData.Targets[0].transform.position;
            }

            return _Spell.Owner.transform.position;
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

            if (EditorHelper.BoolField("Teleport Self", "Determines if we're teleporting ourselves or a target.", TeleportSelf, rTarget))
            {
                lIsDirty = true;
                TeleportSelf = EditorHelper.FieldBoolValue;
            }

            if (EditorHelper.BoolField("Teleport Camera", "Determines if we're teleporting the camera too (if it is set on the MC).", TeleportCamera, rTarget))
            {
                lIsDirty = true;
                TeleportCamera = EditorHelper.FieldBoolValue;
            }

            if (EditorHelper.Vector3Field("Position", "Fixed position to teleport to. Use (0, 0, 0) to not use a fixed position.", Position, rTarget))
            {
                lIsDirty = true;
                Position = EditorHelper.FieldVector3Value;
            }

            return lIsDirty;
        }

#endif

        #endregion
    }
}