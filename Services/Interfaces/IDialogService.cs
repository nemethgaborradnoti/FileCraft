namespace FileCraft.Services.Interfaces
{
    public interface IDialogService
    {
        string? SelectFolder(string description);
        void ShowNotification(string title, string message);
        bool ShowConfirmation(string title, string message, int? filesAffected = null);
    }
}
