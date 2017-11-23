namespace com.ootii.Actors.Magic
{
    public class EnumSpellActionActivation
    {
        public const int MANAGED = 0;               // Activates when called
        public const int CASTING_STARTED = 1;       // When casting starts
        public const int SPELL_CAST = 2;            // When the spell is cast
        public const int CASTING_ENDED = 3;         // When casting ends
        public const int PREVIOUS_COMPLETED = 4;    // When previous action completes
        public const int PREVIOUS_SUCCESS = 5;      // When previous action completes successufully
        public const int PREVIOUS_FAILURE = 6;      // When previous action completes failure

        public static string[] Names = new string[]
        {
            "Managed",
            "Casting Started",
            "Spell Cast",
            "Casting Ended",
            "Previous Completed",
            "Previous Succeeded",
            "Previous Failed"
        };
    }
}
