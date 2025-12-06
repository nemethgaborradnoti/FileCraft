namespace FileCraft.Shared.Helpers
{
    public static class ResourceHelper
    {
        public static string GetString(string key)
        {
            return Application.Current.TryFindResource(key) as string ?? key;
        }
    }
}