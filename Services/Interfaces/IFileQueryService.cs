using FileCraft.Models;
using System.IO;

namespace FileCraft.Services.Interfaces
{
    public interface IFileQueryService
    {
        HashSet<string> GetAvailableExtensions(IEnumerable<(string Path, bool Recursive)> folderConfigs, ISet<string> ignoredFolderNames);

        IEnumerable<SelectableFile> GetFilesByExtensions(string basePath, IEnumerable<(string Path, bool Recursive)> folderConfigs, ISet<string> selectedExtensions, ISet<string> ignoredFolderNames);

        IEnumerable<FileInfo> GetAllFiles(IEnumerable<string> folderPaths, ISet<string> ignoredFolderNames);
    }
}