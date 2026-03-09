namespace FileCraft.Services.Interfaces
{
    public interface IAppPathProvider
    {
        string GetAppDirectory();
        string GetBackupDirectory();
    }
}