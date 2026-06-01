namespace SideQuest.Authorization
{
    public static class SideQuestRoles
    {
        public const string Admin = "Admin";
        public const string Employer = "Employer";
        public const string Worker = "Worker";

        public static readonly string[] All =
        [
            Admin,
            Employer,
            Worker
        ];
    }
}
