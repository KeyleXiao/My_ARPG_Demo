using System;
using UnityEngine;
using com.ootii.Actors.LifeCores;
using com.ootii.Base;
using com.ootii.Geometry;
using com.ootii.Graphics.NodeGraph;
using com.ootii.Helpers;

namespace com.ootii.Actors.Magic
{
    [Serializable]
    [BaseName("Stop Particles")]
    [BaseDescription("Stops the particles or Particle Core on the specified object. Use the TargetName to explicity state the particles to stop.")]
    public class StopParticles : SpellAction
    {
        /// <summary>
        /// Determines how we'll position the game object at creation
        /// </summary>
        public int _OwnerTypeIndex = 0;
        public int OwnerTypeIndex
        {
            get { return _OwnerTypeIndex; }
            set { _OwnerTypeIndex = value; }
        }
        
        /// <summary>
        /// Particle name
        /// </summary>
        public string _TargetName = "";
        public string TargetName
        {
            get { return _TargetName; }
            set { _TargetName = value; }
        }

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
            base.Activate(rPreviousSpellActionState, rData);

            GameObject lOwner = base.GetBestTarget(_OwnerTypeIndex, rData, _Spell.Data);
            ActivateInstance(lOwner);

            // Immediately deactivate
            Deactivate();
        }


        /// <summary>
        /// Activates the action on a single target
        /// </summary>
        /// <param name="rTarget">Target to activate on</param>
        protected void ActivateInstance(GameObject rTarget)
        {
            if (rTarget == null) { rTarget = _Spell.Owner; }

            Transform lOwner = rTarget.transform;
            if (lOwner != null && _TargetName.Length > 0) { lOwner = lOwner.transform.FindTransform(_TargetName); }

            if (lOwner != null)
            {
                bool lParticlesFound = false;

                ParticleCore[] lParticleCores = lOwner.gameObject.GetComponents<ParticleCore>();
                if (lParticleCores != null && lParticleCores.Length > 0)
                {
                    for (int i = 0; i < lParticleCores.Length; i++)
                    {
                        lParticlesFound = true;
                        lParticleCores[i].Stop();
                    }
                }

                lParticleCores = lOwner.gameObject.GetComponentsInChildren<ParticleCore>();
                if (lParticleCores != null && lParticleCores.Length > 0)
                {
                    for (int i = 0; i < lParticleCores.Length; i++)
                    {
                        lParticlesFound = true;
                        lParticleCores[i].Stop();
                    }
                }

                if (!lParticlesFound)
                {
                    ParticleSystem[] lParticleSystems = lOwner.gameObject.GetComponents<ParticleSystem>();
                    if (lParticleSystems != null && lParticleSystems.Length > 0)
                    {
                        for (int i = 0; i < lParticleSystems.Length; i++)
                        {
#if UNITY_5_0 || UNITY_5_1 || UNITY_5_2 || UNITY_5_3 || UNITY_5_4
                            lParticleSystems[i].Stop(true);
#else
                            lParticleSystems[i].Stop(true, ParticleSystemStopBehavior.StopEmitting);
#endif

                            GameObject.Destroy(lOwner.gameObject);
                        }
                    }

                    lParticleSystems = lOwner.gameObject.GetComponentsInChildren<ParticleSystem>();
                    if (lParticleSystems != null && lParticleSystems.Length > 0)
                    {
                        for (int i = 0; i < lParticleSystems.Length; i++)
                        {
#if UNITY_5_0 || UNITY_5_1 || UNITY_5_2 || UNITY_5_3 || UNITY_5_4
                            lParticleSystems[i].Stop(true);
#else
                            lParticleSystems[i].Stop(true, ParticleSystemStopBehavior.StopEmitting);
#endif

                            GameObject.Destroy(lOwner.gameObject);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Called when the action is meant to be deactivated
        /// </summary>
        public override void Deactivate()
        {
            base.Deactivate();
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

            if (EditorHelper.PopUpField("Owner Type", "Determines the type of GameObject that owns the particle.", OwnerTypeIndex, SpellAction.GetBestTargetTypes, rTarget))
            {
                lIsDirty = true;
                OwnerTypeIndex = EditorHelper.FieldIntValue;
            }

            if (EditorHelper.TextField("Target Name", "Name of the ParticleCore object to be stopped.", TargetName, rTarget))
            {
                lIsDirty = true;
                TargetName = EditorHelper.FieldStringValue;
            }

            return lIsDirty;
        }

#endif

        #endregion
    }
}