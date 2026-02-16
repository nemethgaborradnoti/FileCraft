using FileCraft.Models;
using FileCraft.Services.Interfaces;
using FileCraft.ViewModels;
using System.IO;

namespace FileCraft.Services
{
    public class FileQueryService : IFileQueryService
    {
        public IEnumerable<FileInfo> GetAllFiles(IEnumerable<string> folderPaths, ISet<string> ignoredFolderNames)
        {
            var files = new List<FileInfo>();
            foreach (var path in folderPaths)
            {
                if (Directory.Exists(path))
                {
                    files.AddRange(GetFilesRecursive(path, ignoredFolderNames));
                }
            }
            return files;
        }

        public HashSet<string> GetAvailableExtensions(IEnumerable<string> folderPaths, ISet<string> ignoredFolderNames)
        {
            var extensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var allFiles = GetAllFiles(folderPaths, ignoredFolderNames);

            foreach (var file in allFiles)
            {
                extensions.Add(file.Extension);
            }
            return extensions;
        }

        public List<SelectableFile> GetFilesByExtensions(string basePath, IEnumerable<string> folderPaths, ISet<string> selectedExtensions, ISet<string> ignoredFolderNames)
        {
            var allFiles = GetAllFiles(folderPaths, ignoredFolderNames);

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

        private IEnumerable<FileInfo> GetFilesRecursive(string path, ISet<string> ignoredFolderNames)
        {
            var files = new List<FileInfo>();
            try
            {
                var dirInfo = new DirectoryInfo(path);

                if (ignoredFolderNames.Contains(dirInfo.Name))
                {
                    return files;
                }

                files.AddRange(dirInfo.GetFiles("*.*", SearchOption.TopDirectoryOnly));

                foreach (var directory in dirInfo.GetDirectories())
                {
                    if (!ignoredFolderNames.Contains(directory.Name))
                    {
                        files.AddRange(GetFilesRecursive(directory.FullName, ignoredFolderNames));
                    }
                }
            }
            catch (UnauthorizedAccessException)
            {
            }
            catch (Exception)
            {
            }
            return files;
        }
    }
}