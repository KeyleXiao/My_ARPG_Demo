using System.Collections.Generic;
using UnityEngine;
using com.ootii.Actors.AnimationControllers;
using com.ootii.Actors.Magic;
using com.ootii.MotionControllerPacks;

namespace com.ootii.Demos
{
    public class DemoSpellBar_v2 : MonoBehaviour
    {
        /// <summary>
        /// Motion Controller that will cast the spells
        /// </summary>
        public MotionController MotionController = null;

        /// <summary>
        /// List of spells to cast
        /// </summary>
        public SpellInventory SpellInventory = null;

        /// <summary>
        /// Indexes that controll the UI
        /// </summary>
        public List<int> SpellIndexes = new List<int>();

        private void Start()
        {
            if (MotionController == null)
            {
                GameObject lChallenger = GameObject.Find("Challenger");
                if (lChallenger != null) { MotionController = lChallenger.GetComponent<MotionController>(); }
            }

            if (SpellInventory == null)
            {
                GameObject lChallenger = GameObject.Find("Challenger");
                if (lChallenger != null) { SpellInventory = lChallenger.GetComponent<SpellInventory>(); }
            }
        }

        private void OnGUI()
        {
            if (MotionController == null || SpellInventory == null)
            {
                GUI.Label(new Rect(10, 10, 300, 20), "No Motion Controller or Spell Inventory Set!");
                return;
            }

            float lWidth = 60f;
            float lHeight = 45f;
            float lSpacer = 10f;

            int lSpellCount = Mathf.Min(SpellInventory._Spells.Count, SpellIndexes.Count);

            float lBarWidth = (lSpellCount * lWidth) + ((lSpellCount - 1) * lSpacer);
            float lBarX = (Screen.width - lBarWidth) * 0.5f;
            float lBarY = (Screen.height - lHeight - lSpacer);

            for (int i = 0; i < lSpellCount; i++)
            {
                int lIndex = SpellIndexes[i];

                string lName = SpellInventory._Spells[lIndex].Name.Replace(" ", "\n");

                if (GUI.Button(new Rect(lBarX + ((lWidth + lSpacer) * i), lBarY, lWidth, lHeight), lName))
                {
                    BasicSpellCasting lCastMotion = MotionController.GetMotion<BasicSpellCasting>();
                    if (!lCastMotion.IsActive && (!lCastMotion.RequiresStance || MotionController.ActorController.State.Stance == EnumControllerStance.SPELL_CASTING))
                    {
                    MotionController.ActivateMotion(lCastMotion, lIndex);
                    }
                }
            } 
            
            //if (GUI.Button(new Rect(10f, lHeight + lSpacer, lWidth, lHeight), "Interrupt"))
            //{
            //    MotionControllerMotion lMotion = MotionController.GetActiveMotion(0);
            //    lMotion.Interrupt(null);
            //}
        }
    }
}