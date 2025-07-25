namespace FileCraft.Services.Interfaces
{
    public interface IDialogService
    {
        string? SelectFolder(string description);

        void ShowNotification(string title, string message);
    }
}
