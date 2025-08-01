namespace FileCraft.Services.Interfaces
{
    using FileCraft.Models;
    public interface IDialogService
    {
        string? SelectFolder(string description);
        void ShowNotification(string title, string message, DialogIconType iconType);
        bool ShowConfirmation(string title, string message, DialogIconType iconType, int? filesAffected = null);
    }
}