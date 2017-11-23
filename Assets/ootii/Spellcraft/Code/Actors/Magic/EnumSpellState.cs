namespace com.ootii.Actors.Magic
{
    public class EnumSpellState
    {
        /// <summary>
        /// Enum values
        /// </summary>
        public const int INACTIVE = 0;              // Spell is not active
        public const int READY = 1;                 // Spell is instantiated, but not casting
        public const int CASTING_STARTED = 2;       // Casting has just begin
        public const int SPELL_CAST = 3;            // Casting is done and spell has been triggered
        public const int CASTING_ENDED = 4;         // Spell has completed
        public const int COMPLETED = 5;             // Spell has completed

        /// <summary>
        /// Contains a mapping from ID to names
        /// </summary>
        public static string[] Names = new string[]
        {
            "Inactive",
            "Ready",
            "Casting Started",
            "Spell Cast",
            "Casting Ended",
            "Completed"
        };

        /// <summary>
        /// Retrieve the index of the specified name
        /// </summary>
        /// <param name="rName">Name of the enumeration</param>
        /// <returns>ID of the enumeration or 0 if it's not found</returns>
        public static int GetEnum(string rName)
        {
            for (int i = 0; i < Names.Length; i++)
            {
                if (Names[i].ToLower() == rName.ToLower()) { return i; }
            }

            return 0;
        }
    }
}
