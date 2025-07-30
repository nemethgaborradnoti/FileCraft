namespace FileCraft.Services.Interfaces
{
    public interface IDialogService
    {
        string? SelectFolder(string description);

        void ShowNotification(string title, string message);

        bool ShowConfirmation(string actionName, string destinationPath, int filesAffected);
    }
}
