using System;
using System.Collections.Generic;
using UnityEngine;
using com.ootii.Actors.LifeCores;
using com.ootii.Base;
using com.ootii.Helpers;

namespace com.ootii.Actors.Magic
{
    [Serializable]
    [BaseName("Spawn Particles")]
    [BaseDescription("Spawns particles that anchor to the specified target. The action won't fully shut down until all particles are done processing.")]
    public class SpawnParticles : SpawnGameObject
    {
        /// <summary>
        /// Floating point error constant
        /// </summary>
        public const float EPSILON = 0.001f;

        // Color names that could be used in materials
        private static string[] MATERIAL_COLORS = new string[] { "_Color", "_MainColor", "_TintColor", "_EmissionColor", "_BorderColor", "_ReflectColor", "_RimColor", "_CoreColor" };

        /// <summary>
        /// Determines if we use the camera forward as the direction of the projectile's launch
        /// </summary>
        public bool _ReleaseFromCameraForward = false;
        public bool ReleaseFromCameraForward
        {
            get { return _ReleaseFromCameraForward; }
            set { _ReleaseFromCameraForward = value; }
        }

        /// <summary>
        /// Determines if the particles have an attractor
        /// </summary>
        public bool _UseAttractor = false;
        public bool UseAttractor
        {
            get { return _UseAttractor; }
            set { _UseAttractor = value; }
        }

        /// <summary>
        /// Attempt to find the attractor
        /// </summary>
        public int _AttractorTypeIndex = 4;
        public int AttractorTypeIndex
        {
            get { return _AttractorTypeIndex; }
            set { _AttractorTypeIndex = value; }
        }

        /// <summary>
        /// Attractor offset
        /// </summary>
        public Vector3 _AttractorOffset = Vector3.zero;
        public Vector3 AttractorOffset
        {
            get { return _AttractorOffset; }
            set { _AttractorOffset = value; }
        }

        /// <summary>
        /// Speed at which we change the projector's alpha
        /// </summary>
        public float _AudioFadeInSpeed = 0f;
        public float AudioFadeInSpeed
        {
            get { return _AudioFadeInSpeed; }
            set { _AudioFadeInSpeed = value; }
        }

        /// <summary>
        /// Speed at which we change the projector's alpha
        /// </summary>
        public float _AudioFadeOutSpeed = 1f;
        public float AudioFadeOutSpeed
        {
            get { return _AudioFadeOutSpeed; }
            set { _AudioFadeOutSpeed = value; }
        }

        /// <summary>
        /// Speed at which we change the projector's alpha
        /// </summary>
        public float _LightFadeInSpeed = 0f;
        public float LightFadeInSpeed
        {
            get { return _LightFadeInSpeed; }
            set { _LightFadeInSpeed = value; }
        }

        /// <summary>
        /// Speed at which we change the projector's alpha
        /// </summary>
        public float _LightFadeOutSpeed = 1f;
        public float LightFadeOutSpeed
        {
            get { return _LightFadeOutSpeed; }
            set { _LightFadeOutSpeed = value; }
        }

        /// <summary>
        /// Speed at which we change the projector's alpha
        /// </summary>
        public float _ProjectorFadeInSpeed = 0f;
        public float ProjectorFadeInSpeed
        {
            get { return _ProjectorFadeInSpeed; }
            set { _ProjectorFadeInSpeed = value; }
        }

        /// <summary>
        /// Speed at which we change the projector's alpha
        /// </summary>
        public float _ProjectorFadeOutSpeed = 1f;
        public float ProjectorFadeOutSpeed
        {
            get { return _ProjectorFadeOutSpeed; }
            set { _ProjectorFadeOutSpeed = value; }
        }

        // Particle core that actually plays the particles and expires
        protected List<ParticleCore> mParticleCores = null;

        // Allows us to increase and decrease intensity over time.
        protected float mIntensity = 1f;

        // Speed at which we'll increase and decrease the intensity
        protected float mIntensitySpeed = 0.02f;

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

                for (int i = 0; i < mInstances.Count; i++)
                {
                    ParticleCore lParticleCore = mInstances[i].GetComponent<ParticleCore>();
                    if (lParticleCore != null)
                    {
                        lParticleCore.Age = 0f;
                        lParticleCore.Prefab = _Prefab;
                        lParticleCore.OnReleasedEvent = OnCoreReleased;

                        if (MaxAge > 0f)
                        {
                            if (_DeactivationIndex == EnumSpellActionDeactivation.TIMER || _DeactivationIndex == EnumSpellActionDeactivation.IMMEDIATELY)
                            {
                                lParticleCore.MaxAge = MaxAge;
                            }
                        }

                        if (UseAttractor)
                        {
                            Transform lAttractor = null;
                            Vector3 lAttractorPosition = _AttractorOffset;

                            // SpellData Targets
                            if (AttractorTypeIndex == 4 && _Spell.Data.Targets != null)
                            {
                                if (i < _Spell.Data.Targets.Count)
                                {
                                    lAttractor = _Spell.Data.Targets[i].transform;
                                }
                            }
                            // SpellData Prev Targets
                            else if (AttractorTypeIndex == 5 && _Spell.Data.PreviousTargets != null)
                            {
                                if (_Spell.Data.PreviousTargets.Count > 0)
                                {
                                    lAttractor = _Spell.Data.PreviousTargets[_Spell.Data.PreviousTargets.Count - 1].transform;
                                }
                            }
                            else
                            {
                                GetBestPosition(AttractorTypeIndex, rData, _Spell.Data, _AttractorOffset, out lAttractor, out lAttractorPosition);
                                lAttractorPosition = _AttractorOffset;
                            }

                            lParticleCore.Attractor = lAttractor;
                            lParticleCore.AttractorOffset = lAttractorPosition;
                        }

                        lParticleCore.Play();

                        if (mParticleCores == null) { mParticleCores = new List<ParticleCore>(); }
                        mParticleCores.Add(lParticleCore);
                    }

                    // Determine how we release the spell
                    if (_Spell.ReleaseFromCamera && Camera.main != null)
                    {
                        mInstances[i].transform.rotation = Camera.main.transform.rotation;
                    }
                    else if (ReleaseFromCameraForward && Camera.main != null)
                    {
                        mInstances[i].transform.rotation = Camera.main.transform.rotation;
                    }
                    else
                    {
                        mInstances[i].transform.rotation = _Spell.Owner.transform.rotation;
                    }
                }
            }

            // If there were no particles, stop
            if (mInstances == null)
            {
                Deactivate();
            }
        }

        /// <summary>
        /// Called when the action is meant to be deactivated
        /// </summary>
        public override void Deactivate()
        {
            // Determine if we can just get out
            if (mInstances == null)
            {
                base.Deactivate();
            }
            // If we're not dealing with particle cores, we need to do this ourselves
            else if (mParticleCores == null)
            {
                if (!mIsShuttingDown)
                {
                    State = EnumSpellActionState.SUCCEEDED;

                    Stop(_DeactivationIndex == EnumSpellActionDeactivation.IMMEDIATELY);
                }
            }
            // If not, wait for the particles to end
            else if (!mIsShuttingDown)
            {
                mIsShuttingDown = true;
                State = EnumSpellActionState.SUCCEEDED;

                if (_DeactivationIndex != EnumSpellActionDeactivation.IMMEDIATELY)
                {
                    // If we're dealing with particle cores, we can just tell them to stop.
                    // Otherwise, we need to tell the individual components to stop.
                    for (int i = 0; i < mParticleCores.Count; i++)
                    {
                        mParticleCores[i].Stop();
                    }
                }
            }
        }

        /// <summary>
        /// Called each frame that the action is active
        /// </summary>
        /// <param name="rDeltaTime">Time in seconds since the last update</param>
        public override void Update()
        {
            mAge = mAge + Time.deltaTime;

            // Determine if it's time to shut down
            if (mState == EnumSpellActionState.ACTIVE)
            {
                if (TestDeactivate())
                {
                    Deactivate();
                }
            }

            // If we're shutting down with out any partilce cores, manage it ourselves
            if (mParticleCores == null)
            {
                bool lIsAlive = false;

                // Test each of the instances to see if we're alive
                for (int i = 0; i < mInstances.Count; i++)
                {
                    bool lIsInstanceAlive = UpdateEffects(mInstances[i], mIsShuttingDown);
                    if (lIsInstanceAlive) { lIsAlive = true; }
                }

                // Finally deactivate if nothing is alive
                if (!lIsAlive)
                {
                    base.Deactivate();
                }
            }
        }

        /// <summary>
        /// Called when the core has finished running and is released
        /// </summary>
        /// <param name="rCore"></param>
        protected virtual void OnCoreReleased(ILifeCore rCore, object rUserData = null)
        {
            ParticleCore lParticleCore = rCore as ParticleCore;
            if (lParticleCore != null && mParticleCores != null)
            {
                mParticleCores.Remove(lParticleCore);
            }

            if (mParticleCores == null || mParticleCores.Count == 0)
            {
                base.Deactivate();
            }
        }

        /// <summary>
        /// Stops are the raw particles and sounds. I prever to use the particle core,
        /// but this allows us to use particles and sounds that are from other providers.
        /// </summary>
        public virtual void Stop(bool rHardStop = false)
        {
            if (mIsShuttingDown) { return; }

            mIntensity = 1f;
            mIsShuttingDown = true;

            // Stop the effects
            for (int i = 0; i < mInstances.Count; i++)
            {
                StopEffects(mInstances[i]);
            }
        }

        /// <summary>
        /// Start the effects based on the fade out speed
        /// </summary>
        /// <param name="rInstance"></param>
        protected void StartEffects(GameObject rInstance)
        {
            if (rInstance == null) { return; }

            // Ensure particles are running
            ParticleSystem[] lParticleSystems = rInstance.GetComponentsInChildren<ParticleSystem>();
            for (int i = 0; i < lParticleSystems.Length; i++)
            {
                if (!lParticleSystems[i].IsAlive(true))
                {
                    lParticleSystems[i].Play(true);
                }
            }

            // Check if sounds are alive
            AudioSource[] lAudioSources = rInstance.GetComponentsInChildren<AudioSource>();
            for (int i = 0; i < lAudioSources.Length; i++)
            {
                if (!lAudioSources[i].isPlaying && AudioFadeInSpeed <= 0f)
                {
                    lAudioSources[i].Play();
                }
            }

            // Check if lights are alive
            Light[] lLights = rInstance.GetComponentsInChildren<Light>();
            for (int i = 0; i < lLights.Length; i++)
            {
                if (lLights[i].intensity == 0f && LightFadeInSpeed <= 0f)
                {
                    lLights[i].intensity = 1f;
                }
            }

            // Ensure projectors are running
            Projector[] lProjectors = rInstance.GetComponentsInChildren<Projector>();
            for (int i = 0; i < lProjectors.Length; i++)
            {
                if (lProjectors[i].material.HasProperty("_Alpha"))
                {
                    float lAlpha = lProjectors[i].material.GetFloat("_Alpha");
                    if (lAlpha == 0f && ProjectorFadeOutSpeed <= 0f)
                    {
                        lProjectors[i].material.SetFloat("_Alpha", 1f);
                    }
                }

                Material lMaterial = lProjectors[i].material;
                for (int j = 0; j < MATERIAL_COLORS.Length; j++)
                {
                    if (lMaterial.HasProperty(MATERIAL_COLORS[j]))
                    {
                        Color lColor = lMaterial.GetColor(MATERIAL_COLORS[j]);
                        if (lColor.a == 0f && ProjectorFadeOutSpeed <= 0f)
                        {
                            lColor.a = 1f;
                            lProjectors[i].material.SetColor(MATERIAL_COLORS[j], lColor);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Stops the effects based on the fade out speed
        /// </summary>
        /// <param name="rInstance"></param>
        protected void StopEffects(GameObject rInstance)
        {
            if (rInstance == null) { return; }

            // Ensure particles are running
            ParticleSystem[] lParticleSystems = rInstance.GetComponentsInChildren<ParticleSystem>();
            for (int i = 0; i < lParticleSystems.Length; i++)
            {
                if (lParticleSystems[i].IsAlive(true))
                {
#if UNITY_5_0 || UNITY_5_1 || UNITY_5_2 || UNITY_5_3 || UNITY_5_4
                    lParticleSystems[i].Stop(true);
#else
                    lParticleSystems[i].Stop(true, ParticleSystemStopBehavior.StopEmitting);
#endif
                }
            }

            // Check if sounds are alive
            AudioSource[] lAudioSources = rInstance.GetComponentsInChildren<AudioSource>();
            for (int i = 0; i < lAudioSources.Length; i++)
            {
                if (lAudioSources[i].isPlaying && AudioFadeOutSpeed <= 0f)
                {
                    lAudioSources[i].Stop();
                }
            }

            // Check if lights are alive
            Light[] lLights = rInstance.GetComponentsInChildren<Light>();
            for (int i = 0; i < lLights.Length; i++)
            {
                if (lLights[i].intensity > 0f && LightFadeOutSpeed <= 0f)
                {
                    lLights[i].intensity = 0f;
                }
            }

            // Ensure projectors are running
            Projector[] lProjectors = rInstance.GetComponentsInChildren<Projector>();
            for (int i = 0; i < lProjectors.Length; i++)
            {
                if (lProjectors[i].material.HasProperty("_Alpha"))
                {
                    float lAlpha = lProjectors[i].material.GetFloat("_Alpha");
                    if (lAlpha > 0f && ProjectorFadeOutSpeed <= 0f)
                    {
                        lProjectors[i].material.SetFloat("_Alpha", 0f);
                    }
                }

                Material lMaterial = lProjectors[i].material;
                for (int j = 0; j < MATERIAL_COLORS.Length; j++)
                {
                    if (lMaterial.HasProperty(MATERIAL_COLORS[j]))
                    {
                        Color lColor = lMaterial.GetColor(MATERIAL_COLORS[j]);
                        if (lColor.a > 0f && ProjectorFadeOutSpeed <= 0f)
                        {
                            lColor.a = 0f;
                            lProjectors[i].material.SetColor(MATERIAL_COLORS[j], lColor);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Update the effects and fade them in and out as needed
        /// </summary>
        /// <param name="rInstance">Instance we're processing</param>
        /// <param name="rShutDown">Determines if we're shutting down or not</param>
        /// <returns></returns>
        protected bool UpdateEffects(GameObject rInstance, bool rShutDown)
        {
            if (rInstance == null) { return false; }

            bool lIsAlive = false;

            if (!rShutDown)
            {
                // Ensure particles are running
                ParticleSystem[] lParticleSystems = rInstance.GetComponentsInChildren<ParticleSystem>();
                for (int i = 0; i < lParticleSystems.Length; i++)
                {
                    if (lParticleSystems[i].IsAlive(true))
                    {
                        lIsAlive = true;
                    }
                }

                // Check if sounds are alive
                if (AudioFadeInSpeed > 0f)
                {
                    AudioSource[] lAudioSources = rInstance.GetComponentsInChildren<AudioSource>();
                    for (int i = 0; i < lAudioSources.Length; i++)
                    {
                        if (lAudioSources[i].isPlaying)
                        {
                            lIsAlive = true;

                            if (lAudioSources[i].volume < 1f)
                            {
                                lAudioSources[i].volume = Mathf.Clamp01(lAudioSources[i].volume - (AudioFadeInSpeed * Time.deltaTime));
                            }
                        }
                    }
                }

                // Check if lights are alive
                if (LightFadeInSpeed > 0f)
                {
                    Light[] lLights = rInstance.GetComponentsInChildren<Light>();
                    for (int i = 0; i < lLights.Length; i++)
                    {
                        lIsAlive = true;

                        if (lLights[i].intensity < 1f)
                        {
                            lLights[i].intensity = Mathf.Clamp01(lLights[i].intensity + (LightFadeInSpeed * Time.deltaTime));
                        }
                    }
                }

                // Ensure projectors are running
                if (ProjectorFadeInSpeed > 0f)
                {
                    Projector[] lProjectors = rInstance.GetComponentsInChildren<Projector>();
                    for (int i = 0; i < lProjectors.Length; i++)
                    {
                        if (lProjectors[i].material.HasProperty("_Alpha"))
                        {
                            float lAlpha = lProjectors[i].material.GetFloat("_Alpha");
                            if (lAlpha < 1f)
                            {
                                lAlpha = Mathf.Clamp01(lAlpha + (ProjectorFadeInSpeed * Time.deltaTime));
                                lProjectors[i].material.SetFloat("_Alpha", lAlpha);
                            }
                        }

                        Material lMaterial = lProjectors[i].material;
                        for (int j = 0; j < MATERIAL_COLORS.Length; j++)
                        {
                            if (lMaterial.HasProperty(MATERIAL_COLORS[j]))
                            {
                                lIsAlive = true;

                                Color lColor = lMaterial.GetColor(MATERIAL_COLORS[j]);
                                if (lColor.a < 1f)
                                {
                                    lColor.a = Mathf.Clamp01(lColor.a + (ProjectorFadeInSpeed * Time.deltaTime));
                                    lProjectors[i].material.SetColor(MATERIAL_COLORS[j], lColor);
                                }
                            }
                        }
                    }
                }
            }
            else
            {
                // Ensure particles are running
                ParticleSystem[] lParticleSystems = rInstance.GetComponentsInChildren<ParticleSystem>();
                for (int i = 0; i < lParticleSystems.Length; i++)
                {
                    if (lParticleSystems[i].IsAlive(true))
                    {
                        lIsAlive = true;
                    }
                }

                // Check if sounds are alive
                AudioSource[] lAudioSources = rInstance.GetComponentsInChildren<AudioSource>();
                for (int i = 0; i < lAudioSources.Length; i++)
                {
                    if (lAudioSources[i].isPlaying && lAudioSources[i].volume > 0f)
                    {
                        if (AudioFadeOutSpeed <= 0f)
                        {
                            lAudioSources[i].volume = 0f;
                        }
                        else
                        {
                            lAudioSources[i].volume = Mathf.Clamp01(lAudioSources[i].volume - (AudioFadeOutSpeed * Time.deltaTime));
                        }

                        if (lAudioSources[i].volume > 0f) { lIsAlive = true; }
                    }
                }

                // Check if lights are alive
                Light[] lLights = rInstance.GetComponentsInChildren<Light>();
                for (int i = 0; i < lLights.Length; i++)
                {
                    if (lLights[i].intensity > 0f)
                    {
                        if (LightFadeOutSpeed <= 0f)
                        {
                            lLights[i].intensity = 0f;
                        }
                        else
                        {
                            lLights[i].intensity = Mathf.Clamp01(lLights[i].intensity - (LightFadeOutSpeed * Time.deltaTime));
                        }

                        if (lLights[i].intensity > 0f) { lIsAlive = true; }
                    }
                }

                // Ensure projectors are running
                Projector[] lProjectors = rInstance.GetComponentsInChildren<Projector>();
                for (int i = 0; i < lProjectors.Length; i++)
                {
                    if (lProjectors[i].material.HasProperty("_Alpha"))
                    {
                        float lAlpha = lProjectors[i].material.GetFloat("_Alpha");
                        if (lAlpha > 0f)
                        {
                            if (ProjectorFadeOutSpeed <= 0f)
                            {
                                lAlpha = 0f;
                                lProjectors[i].material.SetFloat("_Alpha", lAlpha);
                            }
                            else
                            {
                                lAlpha = Mathf.Clamp01(lAlpha - (ProjectorFadeOutSpeed * Time.deltaTime));
                                lProjectors[i].material.SetFloat("_Alpha", lAlpha);
                            }

                            if (lAlpha > 0f) { lIsAlive = true; }
                        }
                    }

                    Material lMaterial = lProjectors[i].material;
                    for (int j = 0; j < MATERIAL_COLORS.Length; j++)
                    {
                        if (lMaterial.HasProperty(MATERIAL_COLORS[j]))
                        {
                            Color lColor = lMaterial.GetColor(MATERIAL_COLORS[j]);
                            if (lColor.a > 0f)
                            {
                                if (ProjectorFadeOutSpeed <= 0f)
                                {
                                    lColor.a = 0f;
                                    lProjectors[i].material.SetColor(MATERIAL_COLORS[j], lColor);
                                }
                                else
                                {
                                    lColor.a = Mathf.Clamp01(lColor.a - (ProjectorFadeOutSpeed * Time.deltaTime));
                                    lProjectors[i].material.SetColor(MATERIAL_COLORS[j], lColor);
                                }

                                if (lColor.a > 0f) { lIsAlive = true; }
                            }
                        }
                    }
                }
            }

            return lIsAlive;
        }

        #region Editor Functions

#if UNITY_EDITOR

        /// <summary>
        /// Called when the inspector needs to draw
        /// </summary>
        public override bool OnInspectorGUI(UnityEngine.Object rTarget)
        {
            mEditorShowDeactivationField = true;
            bool lIsDirty = base.OnInspectorGUI(rTarget);

            GUILayout.Space(5f);

            if (EditorHelper.FloatField("Particle Age", "Max age of the particles.", MaxAge, rTarget))
            {
                lIsDirty = true;
                MaxAge = EditorHelper.FieldFloatValue;
            }

            GUILayout.Space(5f);

            if (EditorHelper.BoolField("Use Camera Forward", "Determines if we use the camera forward as the direction of the particles.", ReleaseFromCameraForward, rTarget))
            {
                lIsDirty = true;
                ReleaseFromCameraForward = EditorHelper.FieldBoolValue;
            }

            if (EditorHelper.BoolField("Use Attractor", "Determines if the particles use an attractor", UseAttractor, rTarget))
            {
                lIsDirty = true;
                UseAttractor = EditorHelper.FieldBoolValue;
            }

            if (UseAttractor)
            {
                if (EditorHelper.PopUpField("Attractor Type", "Determines the kind of targeting we'll use.", AttractorTypeIndex, SpellAction.GetBestPositionTypes, rTarget))
                {
                    lIsDirty = true;
                    AttractorTypeIndex = EditorHelper.FieldIntValue;
                }

                if (EditorHelper.Vector3Field("Attractor Offset", "Offset from the attractor that the particles will pull towards.", AttractorOffset, mTarget))
                {
                    lIsDirty = true;
                    AttractorOffset = EditorHelper.FieldVector3Value;
                }
            }

            return lIsDirty;
        }

#endif

        #endregion
    }
}