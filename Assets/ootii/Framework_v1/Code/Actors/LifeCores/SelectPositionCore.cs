using System;
using UnityEngine;
using com.ootii.Actors.Attributes;
using com.ootii.Cameras;
using com.ootii.Geometry;
using com.ootii.Game;
using com.ootii.Input;

namespace com.ootii.Actors.LifeCores
{
    /// <summary>
    /// A life cycle is the heart-beat of the area. This code is used to manage
    /// the area and objects that enter it
    /// </summary>
    public class SelectPositionCore : ParticleCore
    {
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
        /// Minimum Distance the selector can succeed
        /// </summary>
        public float _MinDistance = 2f;
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
        /// Determines if the position's normal must match our owner's normal
        /// </summary>
        public bool _RequireMatchingUp = true;
        public bool RequireMatchingUp
        {
            get { return _RequireMatchingUp; }
            set { _RequireMatchingUp = value; }
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

        // Last position of the selector
        protected Vector3 mSelectedPosition = Vector3Ext.Null;
        public Vector3 SelectedPosition
        {
            get { return mSelectedPosition; }
            set { mSelectedPosition = value; }
        }

        // Forward direction of the selector
        protected Vector3 mSelectedForward = Vector3.zero;
        public Vector3 SelectedForward
        {
            get { return mSelectedForward; }
            set { mSelectedForward = value; }
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

        /// <summary>
        /// Awake is called when the script instance is being loaded.
        /// </summary>
        public override void Awake()
        {
            base.Awake();

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

            mSelectedPosition = Vector3Ext.Null;
            mSelectedForward = Vector3Ext.Null;

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
                        mSelectedPosition = Vector3Ext.Null;
                        mSelectedForward = Vector3Ext.Null;

                        if (OnCancelledEvent != null) { OnCancelledEvent(this, mSelectedPosition); }

                        Stop();
                    }
                    // Check if we're selecting
                    else if (mInputSource.IsJustPressed(_ActionAlias))
                    {
                        lIsClicked = true;
                    }
                }

                bool lRayHit = Raycast(!_UseMouse);

                if (lRayHit)
                {
                    ValidatePosition();
                }

                // Return the results
                if (lIsClicked)
                { 
                    if (OnSelectedEvent != null) { OnSelectedEvent(this, mSelectedPosition); }

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

            OnSelectedEvent = null;

            mOwner = null;
            mInputSource = null;
            mHitInfo = RaycastExt.EmptyHitInfo;
            mSelectedPosition = Vector3.zero;
            mSelectedForward = Vector3.zero;
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
                lRayHit = RaycastExt.SafeRaycast(lRay.origin, lRay.direction, out mHitInfo, _MaxDistance, _CollisionLayers, lOwner);
            }

            // Since we don't seem to have a hit, test if we can use the max distance and shoot a ray down
            if (!lRayHit)
            {
                Transform lOrigin = TargetingReticle.Instance.RaycastRoot;
                if (lOrigin == null && Camera.main != null) { lOrigin = Camera.main.transform; }
                if (lOrigin == null) { lOrigin = transform; }

                Vector3 lStart = lOrigin.position + (lOrigin.forward * _MaxDistance);

                RaycastHit lHitInfo;
                if (RaycastExt.SafeRaycast(lStart, -lOwner.up, out lHitInfo, 10f, _CollisionLayers, lOwner, null, true))
                {
                    if (_Tags == null || _Tags.Length == 0)
                    {
                        lRayHit = true;
                        mHitInfo = lHitInfo;
                    }
                    else
                    {
                        IAttributeSource lAttributeSource = lHitInfo.collider.gameObject.GetComponent<IAttributeSource>();
                        if (lAttributeSource != null && lAttributeSource.AttributesExist(_Tags))
                        {
                            lRayHit = true;
                            mHitInfo = lHitInfo;
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
        protected virtual void ValidatePosition()
        {
            Transform lOwner = mOwner.transform;

            float lAngle = Vector3.Angle(mHitInfo.normal, lOwner.up);
            if (!_RequireMatchingUp || Mathf.Abs(lAngle) <= 2f)
            {
                Vector3 lToPoint = mHitInfo.point - lOwner.position;
                float lToPointDistance = lToPoint.magnitude;
                mSelectedForward = lToPoint.normalized;

                lToPointDistance = Mathf.Clamp(lToPointDistance, (MinDistance > 0f ? MinDistance : lToPointDistance), MaxDistance);
                mSelectedPosition = lOwner.position + (mSelectedForward * lToPointDistance);

                transform.rotation = Quaternion.LookRotation(mSelectedForward, lOwner.up);
                transform.position = mSelectedPosition;
            }
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
                bool lRayHit = Raycast(true);

                if (lRayHit)
                {
                    ValidatePosition();
                }
            }
        }
    }
}
