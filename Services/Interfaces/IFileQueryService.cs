using FileCraft.Models;
using System.IO;

namespace FileCraft.Services.Interfaces
{
    public interface IFileQueryService
    {
        HashSet<string> GetAvailableExtensions(IEnumerable<string> folderPaths);

        List<SelectableFile> GetFilesByExtensions(string basePath, IEnumerable<string> folderPaths, ISet<string> selectedExtensions);

        IEnumerable<FileInfo> GetAllFiles(IEnumerable<string> folderPaths);
    }
}