namespace FileCraft.Services.Interfaces
{
    public interface IFileOperationService
    {
        Task<string> GenerateTreeStructureAsync(string sourcePath, string destinationPath, ISet<string> excludedFolders);

        Task<string> ExportFolderContentsAsync(string sourcePath, string destinationPath, bool includeSubfolders);

        Task<string> ExportSelectedFileContentsAsync(string destinationPath, IEnumerable<string> selectedFilePaths);
    }
}
