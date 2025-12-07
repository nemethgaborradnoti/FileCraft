using FileCraft.Models;
using FileCraft.Services.Interfaces;
using FileCraft.ViewModels;
using System.IO;

namespace FileCraft.Services
{
    public class FileQueryService : IFileQueryService
    {
        public IEnumerable<FileInfo> GetAllFiles(IEnumerable<string> folderPaths)
        {
            var files = new List<FileInfo>();
            foreach (var path in folderPaths)
            {
                if (Directory.Exists(path))
                {
                    try
                    {
                        var dirInfo = new DirectoryInfo(path);
                        files.AddRange(dirInfo.GetFiles("*.*", SearchOption.TopDirectoryOnly));
                    }
                    catch (UnauthorizedAccessException)
                    {
                        Console.WriteLine($"Access denied to folder: {path}");
                    }
                }
            }
            return files;
        }

        public HashSet<string> GetAvailableExtensions(IEnumerable<FolderViewModel> folders)
        {
            var extensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var folderPaths = folders.Select(f => f.FullPath);
            var allFiles = GetAllFiles(folderPaths);

            foreach (var file in allFiles)
            {
                extensions.Add(file.Extension);
            }
            return extensions;
        }

        public List<SelectableFile> GetFilesByExtensions(string basePath, IEnumerable<FolderViewModel> folders, ISet<string> selectedExtensions)
        {
            var folderPaths = folders.Select(f => f.FullPath);
            var allFiles = GetAllFiles(folderPaths);

            return allFiles
                .Where(f => selectedExtensions.Contains(f.Extension, StringComparer.OrdinalIgnoreCase))
                .Select(f => new SelectableFile
                {
                    FileName = f.Name,
                    FullPath = f.FullName,
                    RelativePath = Path.GetRelativePath(basePath, f.FullName),
                    IsSelected = false
                })
                .ToList();
        }
    }
}