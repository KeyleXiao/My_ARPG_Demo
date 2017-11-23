namespace com.ootii.Actors.Magic
{
    public class EnumSpellActionState
    {
        /// <summary>
        /// Enum values
        /// </summary>
        public const int INACTIVE = 0;              // Spell action is not active or ready to be used
        public const int READY = 1;                 // Spell action is ready, but not active
        public const int ACTIVE = 2;                // Spell action is running
        public const int SUCCEEDED = 5;               // Spell action has finished
        public const int FAILED = 6;

        /// <summary>
        /// Contains a mapping from ID to names
        /// </summary>
        public static string[] Names = new string[]
        {
            "Inactive",
            "Ready",
            "Active",
            "Succeeded",
            "Failed"
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
