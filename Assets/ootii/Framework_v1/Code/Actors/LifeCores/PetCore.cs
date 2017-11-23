using UnityEngine;

namespace com.ootii.Actors.LifeCores
{
    /// <summary>
    /// Foundation for an actor that follows the target or owner
    /// </summary>
    public class PetCore : ParticleCore, IPetCore
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
        /// Amount of wandering that the light does
        /// </summary>
        public float _WanderRadius = 0.1f;
        public float WanderRadius
        {
            get { return _WanderRadius; }
            set { _WanderRadius = value; }
        }

        // Track the last target position
        protected Vector3 mLastTargetPosition = Vector3.zero;

        // Add some local movement
        protected Vector3 mLocalPosition = Vector3.zero;

        /// <summary>
        /// Awake is called when the script instance is being loaded.
        /// </summary>
        public override void Awake()
        {
            base.Awake();
        }

        /// <summary>
        /// Used to start the particle core all over
        /// </summary>
        public override void Play()
        {
            base.Play();
        }

        /// <summary>
        /// Stop the particle core. This will may release it as well.
        /// </summary>
        public override void Stop(bool rHardStop = false)
        {
            base.Stop(rHardStop);
        }

        /// <summary>
        /// Moves the pet given the anchor's position
        /// </summary>
        public override void LateUpdate()
        {
            base.LateUpdate();

            // Move to the new position
            if (_Anchor != null)
            {
                Vector3 lAnchorTargetPosition = _Anchor.position + (_Anchor.rotation * _AnchorOffset);

                Vector3 lTargetPosition = lAnchorTargetPosition;
                if ((lAnchorTargetPosition - mLastTargetPosition).sqrMagnitude == 0f)
                {

                }
                else
                {

                }

                if (WanderRadius > 0f)
                {
                    mLocalPosition.x = WanderRadius * Mathf.Cos(Time.time);
                    mLocalPosition.y = WanderRadius * Mathf.Sin(Time.time);
                    mLocalPosition.z = WanderRadius * Mathf.Cos(Time.time) * Mathf.Sin(Time.time);
                }

                _Transform.position = Vector3.Lerp(_Transform.position, lTargetPosition + mLocalPosition, Time.deltaTime * 2f);

                mLastTargetPosition = lAnchorTargetPosition;
            }
        }
    }
}
