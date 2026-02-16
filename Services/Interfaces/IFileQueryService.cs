using FileCraft.Models;
using System.IO;

namespace FileCraft.Services.Interfaces
{
    public interface IFileQueryService
    {
        HashSet<string> GetAvailableExtensions(IEnumerable<string> folderPaths, ISet<string> ignoredFolderNames);

        List<SelectableFile> GetFilesByExtensions(string basePath, IEnumerable<string> folderPaths, ISet<string> selectedExtensions, ISet<string> ignoredFolderNames);

        IEnumerable<FileInfo> GetAllFiles(IEnumerable<string> folderPaths, ISet<string> ignoredFolderNames);
    }
}