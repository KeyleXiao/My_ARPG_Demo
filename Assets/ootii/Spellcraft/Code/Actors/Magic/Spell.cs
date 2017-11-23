using System;
using System.Collections.Generic;
using UnityEngine;
using com.ootii.Base;
using com.ootii.Collections;
using com.ootii.Graphics.NodeGraph;

namespace com.ootii.Actors.Magic
{
    /// <summary>
    /// Base class for all spells. This is a "template" used to define how the
    /// spell works and run updates given the "instance-level" spell data.
    /// 
    /// Having a Spell as a ScriptableObject allows us to use generic lists and the
    /// derived child types are respected. However, placing the asset in multiple lists
    /// treats them all as references back to the original.
    /// 
    /// So, each list instantiates an instance from the the template. In this way, Spell is
    /// both a "template" and an "instance".
    /// </summary>
    [Serializable]
    [BaseName("Basic Spell")]
    [CreateAssetMenu(menuName = "ootii/Spell Casting/Basic Spell")]
    public class Spell : ScriptableObject
    {
        /// <summary>
        /// When dealing with an instance of a spell, the prefab the instance was created from
        /// </summary>
        protected Spell mPrefab = null;
        public Spell Prefab
        {
            get { return mPrefab; }
            set { mPrefab =value; }
        }

        /// <summary>
        /// Owner of the spell at run-time
        /// </summary>
        protected GameObject mOwner = null;
        public GameObject Owner
        {
            get { return mOwner; }
            set { mOwner = value; }
        }

        /// <summary>
        /// Inventory that the spell was cast from
        /// </summary>
        protected SpellInventory mSpellInventory = null;
        public SpellInventory SpellInventory
        {
            get { return mSpellInventory; }
            set { mSpellInventory = value; }
        }

        /// <summary>
        /// Friendly name of the spell
        /// </summary>
        public string _Name = "";
        public virtual string Name
        {
            get { return _Name; }
            set { _Name = value; }
        }

        /// <summary>
        /// Description of the spell
        /// </summary>
        public string _Description = "";
        public virtual string Description
        {
            get { return _Description; }
            set { _Description = value; }
        }

        /// <summary>
        /// Image representing the spell in the GUI
        /// </summary>
        public Sprite _Icon = null;
        public Sprite Icon
        {
            get { return _Icon; }
            set { _Icon = value; }
        }

        /// <summary>
        /// Casting style is used with the MotionController to determine which
        /// animation will play when the spell is cast. This is associated with the
        /// PMP_BasicSpellCasting motion.
        /// </summary>
        public int _CastingStyle = 0;
        public virtual int CastingStyle
        {
            get { return _CastingStyle; }
            set { _CastingStyle = value; }
        }

        /// <summary>
        /// Casting pause is used with the MotionController to determine if the casting
        /// animation will be paused in order for the user to control the casting time. 
        /// This is associated with the PMP_BasicSpellCasting motion.
        /// </summary>
        public bool _CastingPause = false;
        public virtual bool CastingPause
        {
            get { return _CastingPause; }
            set { _CastingPause = value; }
        }

        /// <summary>
        /// Determines if we show debug information for this spell
        /// </summary>
        public bool _ShowDebug = false;
        public bool ShowDebug
        {
            get
            {
                if (!_ShowDebug) { return false; }
                if (mSpellInventory != null && !mSpellInventory.ShowDebug) { return false; }

                return true;
            }

            set { _ShowDebug = value; }
        }

        /// <summary>
        /// Nodes that the spell uses to start the spell. Each node releates to a SpellAction. So,
        /// we can have multiple actions starting at the same time.
        /// </summary>
        public List<Node> StartNodes = new List<Node>();

        /// <summary>
        /// Nodes that the spell uses to end the spell. Each node releates to a SpellAction. So,
        /// we can have multiple actions starting at the same time.
        /// </summary>
        public List<Node> EndNodes = new List<Node>();

        /// <summary>
        /// Gets and sets the spell's state. Actions may be updated based on the
        /// state change.
        /// </summary>
        protected int _State = EnumSpellState.INACTIVE;
        public int State
        {
            get { return _State; }

            set
            {
                if (_State == value) { return; }

                // Reinitialize the spell and have it ready for casting
                if (value == EnumSpellState.READY)
                {
                    Reset();
                }

                _State = value;
            }
        }

        /// <summary>
        /// Data associated with the instance of the spell. This allows us to pass the spell around
        /// </summary>
        protected SpellData mData = null;
        public SpellData Data
        {
            get { return mData; }
            set { mData = value; }
        }

        /// <summary>
        /// Determines if the spell is released from the camera
        /// </summary>
        protected bool mReleaseFromCamera = false;
        public bool ReleaseFromCamera
        {
            get { return mReleaseFromCamera; }
        }

        /// <summary>
        /// Determines the release distance
        /// </summary>
        protected float mReleaseDistance = 2f;
        public float ReleaseDistance
        {
            get { return mReleaseDistance; }
        }

        /// <summary>
        /// Nodes that are currently running
        /// </summary>
        protected List<Node> mActiveNodes = new List<Node>();

        /// <summary>
        /// List of actions that are being shut down
        /// </summary>
        protected List<SpellAction> mExpiringActions = new List<SpellAction>();

        // Detemines if we are cancelling the spell
        protected bool mIsCancelling = false;

        // Determines if we have already processed the end nodes
        protected bool mEndNodesLoaded = false;

        /// <summary>
        /// This function is called when the ScriptableObject script is started
        /// </summary>
        public virtual void Awake()
        {
            if (ShowDebug) { Utilities.Debug.Log.FileWrite(string.Format("Spell[{0}].Awake", Name)); }

            Reset();
        }

        /// <summary>
        /// Reset the contents of the spell and get it ready for casting again
        /// </summary>
        public virtual void Reset()
        {
            _State = EnumSpellState.READY;

            // Reset the canvas nodes
            for (int i = 0; i < StartNodes.Count; i++)
            {
                StartNodes[i].Reset();
            }

            // Reset the canvas nodes
            for (int i = 0; i < EndNodes.Count; i++)
            {
                EndNodes[i].Reset();
            }
            
            // Clear our lists
            mActiveNodes.Clear();
            mExpiringActions.Clear();

            // Clear any existing data
            if (mData == null)
            {
                mData = new SpellData();
            }
            else
            {
                mData.Clear();
            }

            mIsCancelling = false;
            mEndNodesLoaded = false;
        }

        /// <summary>
        /// Called to determine if casting can actually begin
        /// </summary>
        /// <returns></returns>
        public virtual bool TestCasting()
        {
            return false;
        }

        /// <summary>
        /// Begins casting
        /// </summary>
        public virtual void Start()
        {
            if (ShowDebug) { Utilities.Debug.Log.FileWrite(string.Format("Spell[{0}].Start", Name)); }

            // Flag the spell as casting
            mIsCancelling = false;
            mEndNodesLoaded = false;
            State = EnumSpellState.CASTING_STARTED;

            // Refresh the active nodes. We shouldn't nead this as 
            mActiveNodes.Clear();

            // Start all the start nodes
            for (int i = 0; i < StartNodes.Count; i++)
            {
                ActivateNode(StartNodes[i]);
            }
        }

        /// <summary>
        /// Allows the spell to be cancelled gracefully. The spell graph is responsible for
        /// cleaning up after itself.
        /// </summary>
        public virtual void Cancel()
        {
            if (!mEndNodesLoaded)
            {
                mIsCancelling = true;
            }
        }

        /// <summary>
        /// Spell is actually cast
        /// </summary>
        public virtual void Cast(bool rReleaseFromCamera = false, float rReleaseDistance = 0f)
        {
            mReleaseFromCamera = rReleaseFromCamera;
            mReleaseDistance = rReleaseDistance;

            State = EnumSpellState.SPELL_CAST;
        }

        /// <summary>
        /// Ends casting
        /// </summary>
        public virtual void End()
        {
            State = EnumSpellState.CASTING_ENDED;
        }

        /// <summary>
        /// Interrupts casting if the cast hasn't happened yet
        /// </summary>
        public virtual void InterruptCasting()
        {
        }

        /// <summary>
        /// Run as needed to process the "instance" data. This spell itself should only
        /// contain template level data.
        /// </summary>
        /// <param name="rDeltaTime">Time since the last frame update</param>
        /// <param name="rData">Instance level data for the spell.</param>
        public virtual void Update()
        {
            int lActiveNodes = mActiveNodes.Count;

            // Allow any expiring actions to finish shutting down. Then,
            // remove them when they are done.
            for (int i = mExpiringActions.Count - 1; i >= 0; i--)
            {
                SpellAction lAction = mExpiringActions[i];

                lAction.Update();
                if (!lAction.IsShuttingDown)
                {
                    mExpiringActions.RemoveAt(i);

                    // Since the node was probably listening for the action
                    // to finish, now we can stop
                    //if (!lAction.Node.IsImmediate)
                    {
                        Node lNode = lAction.Node;
                        ScriptableObject.Destroy(lNode.Content);
                        ScriptableObject.Destroy(lNode);
                    }
                }
            }

            // Go through all the active nodes and update. Since we
            // may append new nodes, we don't want to go past the original
            // active node count
            for (int i = 0; i < lActiveNodes; i++)
            {
                Node lNode = mActiveNodes[i];

                // Process the node content
                SpellAction lAction = lNode.Content as SpellAction;
                if (lAction != null)
                {
                    lAction.Update();
                }

                // If we're not shutting down, process the node links to see if they should be activated
                if (!mIsCancelling)
                {
                    for (int j = 0; j < lNode.Links.Count; j++)
                    {
                        bool lActivate = lNode.Links[j].TestActivate();
                        if (lActivate)
                        {
                            ActivateLink(lNode.Links[j], lNode.Data);
                        }
                    }
                }
            }

            // If we've cancelled, clear any active nodes
            if (mIsCancelling && !mEndNodesLoaded)
            {
                mActiveNodes.Clear();
            }
            // Deactivate any active nodes that have completed. This
            // may simply move them to the expiring queue
            else
            {
                for (int i = mActiveNodes.Count - 1; i >= 0; i--)
                {
                    Node lNode = mActiveNodes[i];

                    if (lNode.State == EnumNodeState.SUCCEEDED || lNode.State == EnumNodeState.FAILED)
                    {
                        DeactivateNode(lNode);
                    }
                }
            }

            // Flag the spell as completed if no actions are active
            if (State != EnumSpellState.READY && mExpiringActions.Count == 0 && mActiveNodes.Count == 0)
            {
                // Since no end nodes required, end
                if (mEndNodesLoaded || EndNodes.Count == 0)
                {
                    State = EnumSpellState.COMPLETED;
                }
                // Start all the end nodes
                else
                {
                    for (int i = 0; i < EndNodes.Count; i++)
                    {
                        ActivateNode(EndNodes[i]);
                    }

                    mIsCancelling = false;
                    mEndNodesLoaded = true;
                }
            }
        }

        /// <summary>
        /// Releases the spell back to the pool if we have a prefab
        /// </summary>
        public void Release()
        {
            if (ShowDebug) { Utilities.Debug.Log.FileWrite("Spell[" + Name + "].Release()"); }

            mOwner = null;
            mSpellInventory = null;

            Reset();

            if (mPrefab != null)
            {
                ScriptableObjectPool.Release(mPrefab, this);

                mPrefab = null;
            }
        }

        /// <summary>
        /// Activate the end node contents and flag the node as working
        /// </summary>
        /// <param name="rNode">Node to activate</param>
        public void ActivateLink(NodeLink rLink, object rData = null)
        {
            rLink.Activate();

            ActivateNode(rLink.EndNode, rData);
        }

        /// <summary>
        /// Activate the node contents and flag the node as working
        /// </summary>
        /// <param name="rNode">Node to activate</param>
        public void ActivateNode(Node rNode, object rData = null)
        {
            Node lNode = rNode;

            // If the node isn't immediate, we'll create an instance of it so we can 
            // loop over and over as needed. This instance is a shallow copy. So, we're not 
            // creating new instances of children.
            //if (!lNode.IsImmediate)
            {
                // Create an instance of the node.
                lNode = ScriptableObject.Instantiate<Node>(rNode);
                lNode.ID = rNode.ID;

                // Tell all the node links that this is the start node. We shallow copy them
                // as well so the StartNode can be different each instance
                for (int i = 0; i < lNode.Links.Count; i++)
                {
                    NodeLink lLink = ScriptableObject.Instantiate<NodeLink>(lNode.Links[i]);
                    lLink.StartNode = lNode;

                    // Creates instances for each of the actions
                    if (lLink.Actions != null && lLink.Actions.Count > 0)
                    {
                        for (int j = 0; j < lLink.Actions.Count; j++)
                        {
                            NodeLinkAction lLinkAction = ScriptableObject.Instantiate<NodeLinkAction>(lLink.Actions[j]);
                            lLinkAction._Link = lLink;

                            lLink.Actions[j] = lLinkAction;
                        }
                    }

                    lNode.Links[i] = lLink;
                }

                // We do want a deep copy of the content.
                lNode.Content = ScriptableObjectPool.DeepCopy(lNode.Content, true);

                if (ShowDebug) { Utilities.Debug.Log.FileWrite(string.Format("Spell[{0}].ActivateNode() - Instance created for", Name, lNode.Content.GetType().Name)); }
            }

            // If we're dealing with a spell action, activate
            SpellAction lAction = lNode.Content as SpellAction;
            if (lAction != null)
            {
                lNode.State = EnumNodeState.WORKING;

                lAction.Spell = this;
                lAction.Node = lNode;
                lAction.Activate(0, rData);

                if (ShowDebug) { Utilities.Debug.Log.FileWrite(string.Format("Spell[{0}].ActivateNode() - Activated: {1}", Name, lAction.GetType().Name)); }

                // If the action takes time, add it to your queue
                if (lNode.State == EnumNodeState.WORKING)
                {
                    if (!mActiveNodes.Contains(lNode))
                    {
                        mActiveNodes.Add(lNode);

                        if (ShowDebug) { Utilities.Debug.Log.FileWrite(string.Format("Spell[{0}].ActivateNode() - Added to active nodes: {1}", Name, lAction.GetType().Name)); }
                    }
                }
                // if it's an instant action, we want to move to the next node
                else
                {
                    if (ShowDebug) { Utilities.Debug.Log.FileWrite(string.Format("Spell[{0}].ActivateNode() - Testing links: {1}", Name, lAction.GetType().Name)); }

                    // Process the node links to see if they should be activated
                    for (int i = 0; i < lNode.Links.Count; i++)
                    {
                        bool lActivate = lNode.Links[i].TestActivate();
                        if (lActivate)
                        {
                            ActivateLink(lNode.Links[i], lNode.Data);
                        }
                    }
                }
            }
            // We may still have links that need to process
            else
            {
                lNode.State = EnumNodeState.SUCCEEDED;

                if (ShowDebug) { Utilities.Debug.Log.FileWrite(string.Format("Spell[{0}].ActivateNode() - No action, testing links: {1}", Name, lAction.GetType().Name)); }

                // Process the node links to see if they should be activated
                for (int i = 0; i < lNode.Links.Count; i++)
                {
                    bool lActivate = lNode.Links[i].TestActivate();
                    if (lActivate)
                    {
                        ActivateLink(lNode.Links[i], lNode.Data);
                    }
                }
            }
        }

        /// <summary>
        /// Deactivate the node contents and remove the node from the active list
        /// </summary>
        /// <param name="rNode">Node to activate</param>
        public void DeactivateNode(Node rNode)
        {
            if (ShowDebug) { Utilities.Debug.Log.FileWrite(string.Format("Spell[{0}].DeactivateNode() - Node: {1}", Name, rNode.Content.GetType().Name)); }

            // If we're dealing with a spell action, activate
            SpellAction lAction = rNode.Content as SpellAction;
            if (lAction != null)
            {
                if (lAction.State == EnumSpellActionState.ACTIVE)
                {
                    lAction.Deactivate();
                }
            }

            // Remove the node from the active list
            mActiveNodes.Remove(rNode);

            // Add the action to the shuttind down list. This way it can
            // run and we can remove it when needed
            if (lAction.IsShuttingDown)
            {
                if (ShowDebug) { Utilities.Debug.Log.FileWrite(string.Format("Spell[{0}].DeactivateNode() - Added to expiring nodes: {1}", Name, rNode.Content.GetType().Name)); }

                mExpiringActions.Add(lAction);
            }
            // Since we are instantiating the nodes, we now destroy them
            else // if (!rNode.IsImmediate)
            {
                if (ShowDebug) { Utilities.Debug.Log.FileWrite(string.Format("Spell[{0}].DeactivateNode() - Destroyed: {1}", Name, rNode.Content.GetType().Name)); }

                ScriptableObject.Destroy(rNode.Content);
                ScriptableObject.Destroy(rNode);
            }
        }

        #region Editor Functions

#if UNITY_EDITOR

        /// <summary>
        /// Allows us to re-open the last selected item
        /// </summary>
        public int EditorActionIndex = -1;

        /// <summary>
        /// Called when the inspector needs to draw
        /// </summary>
        public virtual bool OnInspectorGUI()
        {
            return false;
        }

#endif

        #endregion
    }
}
