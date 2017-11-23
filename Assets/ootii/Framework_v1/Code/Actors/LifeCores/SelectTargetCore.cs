using System;
using UnityEngine;
using com.ootii.Actors.Attributes;
using com.ootii.Actors.Combat;
using com.ootii.Cameras;
using com.ootii.Game;
using com.ootii.Geometry;
using com.ootii.Input;

namespace com.ootii.Actors.LifeCores
{
    /// <summary>
    /// A life cycle is the heart-beat of the area. This code is used to manage
    /// the area and objects that enter it
    /// </summary>
    public class SelectTargetCore : ParticleCore
    {
        // Color names that could be used in materials
        private static string[] MATERIAL_COLORS = new string[] { "_Color", "_MainColor", "_BorderColor", "_OutlineColor" };

        /// <summary>
        /// Action alias for the selector to complete
        /// </summary>
        public string _ActionAlias = "Spell Casting Continue";
        public string ActionAlias
        {
            get { return _ActionAlias; }
            set { _ActionAlias = value; }
        }

        /// <summary>
        /// Action alias for the selector to cancel
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
        /// Maximum Distance the selector can succeed
        /// </summary>
        public float _MaxDistance = 8f;
        public float MaxDistance
        {
            get { return _MaxDistance; }
            set { _MaxDistance = value; }
        }

        /// <summary>
        /// Radius to use with the raycast
        /// </summary>
        public float _Radius = 0f;
        public float Radius
        {
            get { return _Radius; }
            set { _Radius = value; }
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
        public int _CollisionLayers = -1;
        public int CollisionLayers
        {
            get { return _CollisionLayers; }
            set { _CollisionLayers = value; }
        }

        /// <summary>
        /// Tags that must exist for the selection to be valid
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
        /// Material used to highlight the target
        /// </summary>
        public Material _HighlightMaterial = null;
        public Material HighlightMaterial
        {
            get { return _HighlightMaterial; }
            set { _HighlightMaterial = value; }
        }

        /// <summary>
        /// Color to apply to the highlight
        /// </summary>
        public Color _HighlightColor = Color.white;
        public Color HighlightColor
        {
            get { return _HighlightColor; }
            set { _HighlightColor = value; }
        }

        /// <summary>
        /// Determines if we render debug info
        /// </summary>
        public bool _ShowDebug = false;
        public bool ShowDebug
        {
            get { return _ShowDebug; }
            set { _ShowDebug = value; }
        }

        /// <summary>
        /// Event for when the selection is made
        /// </summary>
        [NonSerialized]
        public LifeCoreDelegate OnSelectedEvent = null;

        /// <summary>
        /// Event for when the selection is cancelled
        /// </summary>
        [NonSerialized]
        public LifeCoreDelegate OnCancelledEvent = null;

        /// <summary>
        /// Owner controlling the selector
        /// </summary>
        protected GameObject mOwner = null;
        public GameObject Owner
        {
            get { return mOwner; }
            set { mOwner = value; }
        }

        /// <summary>
        /// Input source that causes the selector to select
        /// </summary>
        protected IInputSource mInputSource = null;
        public IInputSource InputSource
        {
            get { return mInputSource; }
            set { mInputSource = value; }
        }

        // Last target of the selector
        protected Transform mSelectedTarget = null;
        public Transform SelectedTarget
        {
            get { return mSelectedTarget; }
            set { mSelectedTarget = value; }
        }

        /// <summary>
        /// Contains information about what was hit
        /// </summary>
        protected RaycastHit mHitInfo = RaycastExt.EmptyHitInfo;
        public RaycastHit HitInfo
        {
            get { return mHitInfo; }
        }

        // Store the camera rig in case we need it later
        protected BaseCameraRig mCameraRig = null;

        // Store the last instance created
        protected Material mMaterialInstance = null;

        /// <summary>
        /// Awake is called when the script instance is being loaded.
        /// </summary>
        public override void Awake()
        {
            base.Awake();

            _ShowDebug = true;

            // We extract the camera so we can tap into the events. Otherwise, our raycast values will
            // refer to the last frame's position and we'll get stuttering
            if (!_UseMouse && TargetingReticle.Instance != null)
            {
                mCameraRig = BaseCameraRig.ExtractCameraRig(TargetingReticle.Instance.RaycastRoot);
            }
        }

        /// <summary>
        /// Used to start the core all over
        /// </summary>
        public override void Play()
        {
            base.Play();

            mSelectedTarget = null;

            // Hide the selector until we have a good position
            transform.position = new Vector3(0f, (mOwner != null ? mOwner.transform.position.y : 0f) - 200f, 0f);

            // Ensure we are cleaned up from the last run
            if (mCameraRig != null)
            {
                mCameraRig.OnPostLateUpdate -= OnCameraUpdated;

                // Register so we can get the latest updates
                if (!_UseMouse && TargetingReticle.Instance != null)
                {
                    mCameraRig.OnPostLateUpdate += OnCameraUpdated;
                }
            }
        }

        /// <summary>
        /// Used to stop the core from processing
        /// </summary>
        public override void Stop(bool rHardStop = false)
        {
            if (mSelectedTarget != null)
            {
                RemoveMaterial(mSelectedTarget.gameObject, mMaterialInstance);
            }

            // Unregister so we stop getting updates
            if (!_UseMouse && TargetingReticle.Instance != null)
            {
                if (mCameraRig != null)
                {
                    mCameraRig.OnPostLateUpdate -= OnCameraUpdated;
                }
            }

            base.Stop(true);
        }

        /// <summary>
        /// Called each frame that the action is active
        /// </summary>
        public override void Update()
        {
            if (!mIsShuttingDown)
            {
                bool lIsClicked = false;

                // Determine if we're done choosing a target
                if (mInputSource != null)
                {
                    // Check if we're cancelling
                    if (_CancelActionAlias.Length > 0 && mInputSource.IsJustPressed(_CancelActionAlias))
                    {
                        if (mSelectedTarget != null)
                        {
                            RemoveMaterial(mSelectedTarget.gameObject, mMaterialInstance);
                        }

                        mSelectedTarget = null;

                        if (OnCancelledEvent != null) { OnCancelledEvent(this, mSelectedTarget); }

                        Stop();
                    }
                    // Check if we're selecting
                    else if (mInputSource.IsJustPressed(_ActionAlias))
                    {
                        lIsClicked = true;
                    }
                }

                // Determine if we should actively look for a target
                if (_ContinuousSelect || lIsClicked)
                {
                    bool lRayHit = Raycast(!_UseMouse);

                    if (lRayHit)
                    {
                        lRayHit = ValidateTarget();
                    }

                    if (!lRayHit)
                    {
                        if (mSelectedTarget != null)
                        {
                            RemoveMaterial(mSelectedTarget.gameObject, mMaterialInstance);
                        }

                        mSelectedTarget = null;
                        transform.position = new Vector3(0f, (mOwner != null ? mOwner.transform.position.y : 0f) - 200f, 0f);
                    }
                }

                // Report the result of the target
                if (lIsClicked)
                {
                    if (mSelectedTarget != null)
                    {
                        RemoveMaterial(mSelectedTarget.gameObject, mMaterialInstance);
                    }

                    if (OnSelectedEvent != null) { OnSelectedEvent(this, mSelectedTarget); }

                    Stop();
                }
            }

            // Handle the projectiles
            base.Update();
        }

        /// <summary>
        /// Releases the game object back to the pool (if allocated) or simply destroys if it not.
        /// </summary>
        public override void Release()
        {
            base.Release();

            if (mSelectedTarget != null)
            {
                RemoveMaterial(mSelectedTarget.gameObject, mMaterialInstance);
            }

            OnSelectedEvent = null;

            mOwner = null;
            mInputSource = null;
            mSelectedTarget = null;
        }

        /// <summary>
        /// Casts a ray using the reticle and then validates the position
        /// </summary>
        /// <returns></returns>
        protected virtual bool Raycast(bool rUseReticle = true)
        {
            bool lRayHit = false;
            Transform lOwner = mOwner.transform;

            if (rUseReticle && TargetingReticle.Instance != null)
            {
                RaycastHit[] lHitInfos;
                int lHitCount = TargetingReticle.Instance.RaycastAll(out lHitInfos, _MinDistance, _MaxDistance, _Radius, _CollisionLayers, lOwner);

                if (lHitCount > 0)
                {
                    if (_Tags == null || _Tags.Length == 0)
                    {
                        lRayHit = true;
                        mHitInfo = lHitInfos[0];
                    }
                    else
                    {
                        for (int i = 0; i < lHitCount; i++)
                        {
                            IAttributeSource lAttributeSource = lHitInfos[i].collider.gameObject.GetComponent<IAttributeSource>();
                            if (lAttributeSource != null && lAttributeSource.AttributesExist(_Tags))
                            {
                                lRayHit = true;
                                mHitInfo = lHitInfos[i];

                                break;
                            }
                        }
                    }
                }
            }
            else
            {
                Ray lRay = Camera.main.ScreenPointToRay(UnityEngine.Input.mousePosition);
                if (RaycastExt.SafeRaycast(lRay.origin, lRay.direction, out mHitInfo, _MaxDistance, _CollisionLayers, lOwner))
                {
                    if (_Tags == null || _Tags.Length == 0)
                    {
                        lRayHit = true;
                    }
                    else
                    {
                        IAttributeSource lAttributeSource = mHitInfo.collider.gameObject.GetComponent<IAttributeSource>();
                        if (lAttributeSource != null && lAttributeSource.AttributesExist(_Tags))
                        {
                            lRayHit = true;
                        }
                    }
                }
            }
            
            return lRayHit;
        }

        /// <summary>
        /// Once a hit position is found, check if it really is a valid one and 
        /// then ensure we use it
        /// </summary>
        protected virtual bool ValidateTarget()
        {
            bool lIsValid = true;
            Transform lOwner = mOwner.transform;

            // Grab the gameobject this collider belongs to
            GameObject lGameObject = mHitInfo.collider.gameObject;

            // Don't count the ignore
            if (lGameObject == null) { return false; }

            // Ensure we're not clicking on terrain
            if (mHitInfo.collider is TerrainCollider)
            {
                lIsValid = false;
            }

            // We require Actor Cores
            if (RequiresRigidbody && lGameObject.GetComponent<Rigidbody>() == null)
            {
                lIsValid = false;
            }

            // We require Actor Cores
            if (RequiresActorCore && lGameObject.GetComponent<IActorCore>() == null)
            {
                lIsValid = false;
            }

            // We only care about combatants we'll enage with
            if (RequiresCombatant && lGameObject.GetComponent<ICombatant>() == null)
            {
                lIsValid = false;
            }

            // We can do a catch-all if a combatant isn't required
            if (lIsValid)
            {
                if (mSelectedTarget != lGameObject.transform)
                {
                    if (mSelectedTarget != null)
                    {
                        RemoveMaterial(mSelectedTarget.gameObject, mMaterialInstance);
                    }

                    mSelectedTarget = lGameObject.transform;

                    transform.rotation = Quaternion.LookRotation(mSelectedTarget.forward, lOwner.up);
                    transform.position = mSelectedTarget.position;

                    // Add the highlight material
                    AddMaterial(lGameObject, _HighlightMaterial);
                }
            }
            else
            {
                if (mSelectedTarget != null)
                {
                    RemoveMaterial(mSelectedTarget.gameObject, mMaterialInstance);
                }
            }

            return lIsValid;
        }

        /// <summary>
        /// When we want to raycast based on the camera direction, we need to do it AFTER we process the camera. 
        /// Otherwise, we can get small stutters during camera rotation.
        /// </summary>
        /// <param name="rDeltaTime"></param>
        /// <param name="rUpdateIndex"></param>
        /// <param name="rCamera"></param>
        protected void OnCameraUpdated(float rDeltaTime, int rUpdateIndex, BaseCameraRig rCamera)
        {
            if (!mIsShuttingDown)
            {
                bool lIsClicked = false;

                // Determine if we're done choosing a target
                if (!_ContinuousSelect && mInputSource != null && mInputSource.IsJustPressed(_ActionAlias))
                {
                     lIsClicked = true;
                }

                // Determine if we should actively look for a target
                if (_ContinuousSelect || lIsClicked)
                {
                    bool lRayHit = Raycast(true);

                    if (lRayHit)
                    {
                        lRayHit = ValidateTarget();
                    }

                    if (!lRayHit)
                    {
                        if (mSelectedTarget != null)
                        {
                            RemoveMaterial(mSelectedTarget.gameObject, mMaterialInstance);
                        }

                        mSelectedTarget = null;
                        transform.position = new Vector3(0f, (mOwner != null ? mOwner.transform.position.y : 0f) - 200f, 0f);
                    }
                }
            }
        }

        /// <summary>
        /// Adds a material instance to the specified target
        /// </summary>
        /// <param name="rTarget"></param>
        protected void AddMaterial(GameObject rTarget, Material rMaterial)
        {
            if (rTarget == null) { return; }
            if (rMaterial == null) { return; }

            Renderer lRenderer = rTarget.GetComponent<Renderer>();
            if (lRenderer == null) { lRenderer = rTarget.GetComponentInChildren<Renderer>(); }

            if (lRenderer != null)
            {
                for (int i = 0; i < lRenderer.materials.Length; i++)
                {
                    if (lRenderer.materials[i] == mMaterialInstance) { return; }
                }

                Material[] lMaterials = new Material[lRenderer.materials.Length + 1];
                Array.Copy(lRenderer.materials, lMaterials, lRenderer.materials.Length);

                //mMaterialInstance = Material.Instantiate(rMaterial);
                lMaterials[lRenderer.materials.Length] = rMaterial;

                lRenderer.materials = lMaterials;

                mMaterialInstance = lRenderer.materials[lRenderer.materials.Length - 1];

                // Set the material color
                for (int i = 0; i < MATERIAL_COLORS.Length; i++)
                {
                    if (mMaterialInstance.HasProperty(MATERIAL_COLORS[i]))
                    {
                        mMaterialInstance.SetColor(MATERIAL_COLORS[i], HighlightColor);
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// Removes the material instance from the specified target
        /// </summary>
        /// <param name="rTarget"></param>
        /// <param name="rMaterialInstance"></param>
        protected void RemoveMaterial(GameObject rTarget, Material rMaterialInstance)
        {
            if (rTarget == null) { return; }
            if (rMaterialInstance == null) { return; }

            Renderer lRenderer = rTarget.GetComponent<Renderer>();
            if (lRenderer == null) { lRenderer = rTarget.GetComponentInChildren<Renderer>(); }

            if (lRenderer != null)
            {
                // Check if the material exists
                bool lFound = false;
                for (int i = 0; i < lRenderer.materials.Length; i++)
                {
                    if (lRenderer.materials[i] == mMaterialInstance) { lFound = true; }
                }

                if (!lFound) { return; }

                // Remove the material
                int lNewIndex = 0;
                Material[] lMaterials = new Material[lRenderer.materials.Length - 1];

                for (int i = 0; i < lRenderer.materials.Length; i++)
                {
                    if (lRenderer.materials[i] != rMaterialInstance)
                    {
                        lMaterials[lNewIndex] = lRenderer.materials[i];
                        lNewIndex++;
                    }
                }

                lRenderer.materials = lMaterials;

                // Remove the material instance
                mMaterialInstance = null;
            }
        }
    }
}
