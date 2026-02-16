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
                    files.AddRange(GetFilesRecursive(path));
                }
            }
            return files;
        }

        public HashSet<string> GetAvailableExtensions(IEnumerable<string> folderPaths)
        {
            var extensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var allFiles = GetAllFiles(folderPaths);

            foreach (var file in allFiles)
            {
                extensions.Add(file.Extension);
            }
            return extensions;
        }

        public List<SelectableFile> GetFilesByExtensions(string basePath, IEnumerable<string> folderPaths, ISet<string> selectedExtensions)
        {
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

        private IEnumerable<FileInfo> GetFilesRecursive(string path)
        {
            var files = new List<FileInfo>();
            try
            {
                var dirInfo = new DirectoryInfo(path);
                files.AddRange(dirInfo.GetFiles("*.*", SearchOption.TopDirectoryOnly));

                foreach (var directory in dirInfo.GetDirectories())
                {
                    files.AddRange(GetFilesRecursive(directory.FullName));
                }
            }
            catch (UnauthorizedAccessException)
            {
                // Skip folders we don't have permission to access
            }
            catch (Exception)
            {
                // Handle other potential IO errors gracefully by skipping
            }
            return files;
        }
    }
}