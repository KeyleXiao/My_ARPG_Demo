namespace com.ootii.Actors.Magic
{
    public class EnumSpellActionDeactivation
    {
        public const int IMMEDIATELY = 0;           // As soon as it starts
        public const int MANAGED = 1;               // Ends when the action determines it is done
        public const int TIMER = 2;                 // Ends when the timer finishes
        public const int CASTING_STARTED = 3;       // When casting starts
        public const int SPELL_CAST = 4;            // When the spell is cast
        public const int CASTING_ENDED = 5;         // When casting ends

        public static string[] Names = new string[] 
        {
            "Immediately",
            "Managed",
            "Timer",
            "Casting Started",
            "Spell Cast",
            "Casting Ended"
        };
    }
}
