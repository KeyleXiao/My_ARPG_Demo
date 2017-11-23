//#define OOTII_PROFILE

using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using com.ootii.Actors;
using com.ootii.Actors.LifeCores;
using com.ootii.Geometry;
using com.ootii.Graphics;
using com.ootii.Utilities;

namespace com.ootii.Actors.Combat
{
    /// <summary>
    /// Static class for managing all the conflicts and skirmishes that exist.
    /// This class is globally accessible so we can manage all the combat that is going on.
    /// </summary>
    public class CombatManager
    {
        /// <summary>
        /// Unique identifier used to help manage instances of attacks, blocks, and parries
        /// </summary>
        protected static int mUCID = 0;
        public static int NextUCID
        {
            get { return ++mUCID; }
        }

        /// <summary>
        /// Determines if we'll show debug information or not
        /// </summary>
        protected static bool mShowDebug = false;
        public static bool ShowDebug
        {
            get { return mShowDebug; }
            set { mShowDebug = value; }
        }

        /// <summary>
        /// This stub is a game object that will update the input over time. The
        /// stub can be placed by the scene builder or generated automatically.
        /// </summary>
        public static CombatManagerCore Core = null;

        /// <summary>
        /// Static constructor is called at most one time, before any 
        /// instance constructor is invoked or member is accessed. 
        /// </summary>
        static CombatManager()
        {
            // Check to see if a combat manager stub exists. If so, associate it.
            // If not, we'll need to create one. The core is what manages our update cycle.
            Core = Component.FindObjectOfType<CombatManagerCore>();
            if (Core == null)
            {
#pragma warning disable 0414

                //GameObject lCoreGameObject = new GameObject("CombatManagerCore", typeof(CombatManagerCore));
                //lCoreGameObject.hideFlags = HideFlags.HideInHierarchy;

                //Core = lCoreGameObject.GetComponent<CombatManagerCore>();

#pragma warning restore 0414
            }
        }

        /// <summary>
        /// Provides a way to initialize the class.
        /// </summary>
        public static void Awake()
        {
        }

        /// <summary>
        /// Called at the end of the frame to allow the combat manager to do any final processing
        /// </summary>
        public static void EndOfFrameUpdate()
        {
        }

        /// <summary>
        /// Grabs the combatants that fit the specified criteria
        /// </summary>
        /// <param name="rHunter">Transform who is searching for the combatants</param>
        /// <param name="rSeekOrigin">Combat origin of the hunter</param>
        /// <param name="rFilter">Filters we'll use to limit which combatants are returned</param>
        /// <param name="rCombatantHits">List of CombatantHit values who are the combatants</param>
        /// <param name="rIgnore">Transform that we won't consider a target (typically the character)</param>
        /// <returns>Count of combatants returned</returns>
        public static int QueryCombatTargets(Transform rSeeker, Vector3 rSeekOrigin, CombatFilter rFilter, List<CombatTarget> rCombatTargets, Transform rIgnore)
        {
            if (rSeeker == null) { return 0; }
            if (rCombatTargets == null) { return 0; }

#if OOTII_PROFILE
            com.ootii.Utilities.Profiler.Start(rSeeker.name + ".QueryCombatTargets");
#endif

            Collider[] lHitColliders;

            rCombatTargets.Clear();
            
            int lHitCount = RaycastExt.SafeOverlapSphere(rSeekOrigin, rFilter.MaxDistance, out lHitColliders, rFilter.Layers, rIgnore);
            for (int i = 0; i < lHitCount; i++)
            {
                GameObject lGameObject = lHitColliders[i].gameObject;

                // Don't count the ignore
                if (lGameObject.transform == rSeeker) { continue; }
                if (lGameObject.transform == rIgnore) { continue; }

                // Determine if the combatant has the appropriate tag
                if (rFilter.Tag != null && rFilter.Tag.Length > 0)
                {
                    if (!lGameObject.CompareTag(rFilter.Tag)) { continue; }
                }

                // We only care about combatants we'll enage with
                ICombatant lCombatant = null;

                Transform lHitTransform = lHitColliders[i].transform;
                while (lHitTransform != null)
                {
                    lCombatant = lGameObject.GetComponent<ICombatant>();
                    if (lCombatant != null) { break; }

                    lHitTransform = lHitTransform.parent;
                }

                if (rFilter.RequireCombatant && lCombatant == null) { continue; }

                // Determine if the combatant is within range
                Vector3 lClosestPoint = Vector3.zero;
                ActorController lActorController = lGameObject.GetComponent<ActorController>();
                if (lActorController != null)
                {
                    lClosestPoint = lActorController.ClosestPoint(rSeekOrigin);
                }
                else
                {
                    lClosestPoint = GeometryExt.ClosestPoint(rSeekOrigin, lHitColliders[i]);
                }

                // If we have an invalid point, stop
                if (lClosestPoint == Vector3Ext.Null) { continue; }

                // Determine if the point is in range
                bool lIsValid = true;
                Vector3 lToClosestPoint = lClosestPoint - rSeekOrigin;

                float lDistance = lToClosestPoint.magnitude;
                if (rFilter.MinDistance > 0f && lDistance < rFilter.MinDistance) { lIsValid = false; }
                if (rFilter.MaxDistance > 0f && lDistance > rFilter.MaxDistance) { lIsValid = false; }

                // Ensure we're not ontop of the combatant. In that case, it's probably the ground
                Vector3 lDirection = lToClosestPoint.normalized;
                if (lDirection == -rSeeker.up) { lIsValid = false; }

                // Check if we're within the field of view
                float lHAngle = Vector3Ext.HorizontalAngleTo(rSeeker.forward, lDirection, rSeeker.up); 
                if (rFilter.HorizontalFOA > 0f && Mathf.Abs(lHAngle) > rFilter.HorizontalFOA * 0.5f) { lIsValid = false; }

                float lVAngle = Vector3Ext.HorizontalAngleTo(rSeeker.forward, lDirection, rSeeker.right);
                if (rFilter.VerticalFOA > 0f && Mathf.Abs(lVAngle) > rFilter.VerticalFOA * 0.5f) { lIsValid = false; }

                // This is an odd test, but we have to do it. If the closest point of a sphere is out of the FOA, it may
                // be that the top of the sphere is in the FOA. So, we'll grab the top of the sphere and test it.
                if (!lIsValid && lCombatant != null && lHitColliders[i] is SphereCollider)
                {
                    lIsValid = true;
                    SphereCollider lSphereCollider = lHitColliders[i] as SphereCollider;

                    lClosestPoint = lCombatant.Transform.position + (lCombatant.Transform.rotation * (lSphereCollider.center + (Vector3.up * lSphereCollider.radius)));
                    lToClosestPoint = lClosestPoint - rSeekOrigin;

                    lDistance = lToClosestPoint.magnitude;
                    if (rFilter.MinDistance > 0f && lDistance < rFilter.MinDistance) { lIsValid = false; }
                    if (rFilter.MaxDistance > 0f && lDistance > rFilter.MaxDistance) { lIsValid = false; }

                    // Ensure we're not ontop of the combatant. In that case, it's probably the ground
                    lDirection = lToClosestPoint.normalized;
                    if (lDirection == -rSeeker.up) { lIsValid = false; }

                    // Check if we're within the field of view
                    lHAngle = Vector3Ext.HorizontalAngleTo(rSeeker.forward, lDirection, rSeeker.up);
                    if (rFilter.HorizontalFOA > 0f && Mathf.Abs(lHAngle) > rFilter.HorizontalFOA * 0.5f) { lIsValid = false; }

                    lVAngle = Vector3Ext.HorizontalAngleTo(rSeeker.forward, lDirection, rSeeker.right);
                    if (rFilter.VerticalFOA > 0f && Mathf.Abs(lVAngle) > rFilter.VerticalFOA * 0.5f) { lIsValid = false; }
                }

                if (mShowDebug)
                {
                    GraphicsManager.DrawPoint(rSeekOrigin, Color.white, null, 2f);
                    GraphicsManager.DrawPoint(lClosestPoint, Color.red, null, 2f);
                }

                if (lIsValid)
                {
                    // Add the combatant to our list
                    CombatTarget lTargetInfo = new CombatTarget();
                    lTargetInfo.SeekOrigin = rSeekOrigin;
                    lTargetInfo.Collider = lHitColliders[i];
                    lTargetInfo.Combatant = lCombatant;
                    lTargetInfo.ClosestPoint = lClosestPoint;
                    lTargetInfo.Distance = lDistance;
                    lTargetInfo.Direction = lDirection;
                    lTargetInfo.HorizontalAngle = lHAngle;
                    lTargetInfo.VerticalAngle = lVAngle;

                    rCombatTargets.Add(lTargetInfo);
                }
            }

            // Sort the combatants by distance
            if (rCombatTargets.Count > 1)
            {
                rCombatTargets.Sort((rLeft, rRight) => rLeft.Distance.CompareTo(rRight.Distance));

                // We also want to remove duplicates
                for (int i = 0; i < rCombatTargets.Count; i++)
                {
                    Transform lTarget = rCombatTargets[i].Collider.transform;
                    if (rCombatTargets[i].Combatant != null) { lTarget = rCombatTargets[i].Combatant.Transform; }

                    // Check if there's a duplicate and remove it
                    for (int j = rCombatTargets.Count - 1; j > i && j > 0; j--)
                    {
                        Transform lNextTarget = rCombatTargets[j].Collider.transform;
                        if (rCombatTargets[j].Combatant != null) { lNextTarget = rCombatTargets[j].Combatant.Transform; }

                        if (lNextTarget == lTarget)
                        {
                            rCombatTargets.RemoveAt(j);
                        }
                    }
                }
            }

#if OOTII_PROFILE
            float lTime = Utilities.Profiler.Stop(rSeeker.name + ".QueryCombatTargets");
            //Utilities.Debug.Log.FileWrite(rSeeker.name + ".QueryCombatTargets time:" + lTime.ToString("f5") + "ms");
#endif

            // Finally, return the count
            return rCombatTargets.Count;
        }
    }
}