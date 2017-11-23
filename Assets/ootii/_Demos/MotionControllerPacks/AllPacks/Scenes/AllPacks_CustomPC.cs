using UnityEngine;
using com.ootii.Actors.Combat;
using com.ootii.Reactors;

namespace com.ootii.Demos
{
    public class AllPacks_CustomPC : MonoBehaviour
    {
        /// <summary>
        /// Allows us to choose the attack style we'll attack with
        /// </summary>
        /// <param name="rAction"></param>
        public void SelectAttackStyle(ReactorAction rAction)
        {
            if (rAction == null || rAction.Message == null) { return; }

            CombatMessage lCombatMessage = rAction.Message as CombatMessage;
            if (lCombatMessage == null) { return; }

            //lCombatMessage.StyleIndex = 2;
        }
    }
}
