namespace FileCraft.Services
{
    public interface IDialogService
    {
        string? SelectFolder(string description);

        void ShowNotification(string title, string message);
    }
}
