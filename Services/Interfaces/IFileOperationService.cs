using FileCraft.Models;

namespace FileCraft.Services.Interfaces
{
    public interface IFileOperationService
    {
        Task<string> GenerateTreeStructureAsync(string sourcePath, string destinationPath, ISet<string> excludedFolders, string outputFileName);

        Task<string> ExportFolderContentsAsync(string destinationPath, IEnumerable<string> includedFolderPaths, string outputFileName, IEnumerable<string> selectedColumns);

        Task<(string FilePath, int IgnoredLines, int IgnoredChars)> ExportSelectedFileContentsAsync(string destinationPath, IEnumerable<SelectableFile> selectedFiles, string outputFileName);
    }
}
