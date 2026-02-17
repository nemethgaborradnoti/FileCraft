using FileCraft.Models;
using FileCraft.Services.Interfaces;
using System.IO;

namespace FileCraft.Services
{
    public class FileQueryService : IFileQueryService
    {
        public IEnumerable<FileInfo> GetAllFiles(IEnumerable<string> folderPaths, ISet<string> ignoredFolderNames)
        {
            foreach (var path in folderPaths)
            {
                if (Directory.Exists(path))
                {
                    foreach (var filePath in EnumerateFilesInternal(path, ignoredFolderNames))
                    {
                        yield return new FileInfo(filePath);
                    }
                }
            }
        }

        public HashSet<string> GetAvailableExtensions(IEnumerable<string> folderPaths, ISet<string> ignoredFolderNames)
        {
            var extensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var path in folderPaths)
            {
                if (Directory.Exists(path))
                {
                    foreach (var filePath in EnumerateFilesInternal(path, ignoredFolderNames))
                    {
                        var extension = Path.GetExtension(filePath);
                        if (!string.IsNullOrEmpty(extension))
                        {
                            extensions.Add(extension);
                        }
                    }
                }
            }
            return extensions;
        }

        public IEnumerable<SelectableFile> GetFilesByExtensions(string basePath, IEnumerable<string> folderPaths, ISet<string> selectedExtensions, ISet<string> ignoredFolderNames)
        {
            foreach (var path in folderPaths)
            {
                if (Directory.Exists(path))
                {
                    foreach (var filePath in EnumerateFilesInternal(path, ignoredFolderNames))
                    {
                        var extension = Path.GetExtension(filePath);
                        if (selectedExtensions.Contains(extension))
                        {
                            yield return new SelectableFile
                            {
                                FileName = Path.GetFileName(filePath),
                                FullPath = filePath,
                                RelativePath = Path.GetRelativePath(basePath, filePath),
                                IsSelected = false
                            };
                        }
                    }
                }
            }
        }

        private IEnumerable<string> EnumerateFilesInternal(string path, ISet<string> ignoredFolderNames)
        {
            var stack = new Stack<string>();
            stack.Push(path);

            while (stack.Count > 0)
            {
                string currentDir = stack.Pop();
                string dirName = Path.GetFileName(currentDir);

                if (ignoredFolderNames.Contains(dirName))
                {
                    continue;
                }

                IEnumerable<string> files;
                try
                {
                    files = Directory.EnumerateFiles(currentDir);
                }
                catch (UnauthorizedAccessException)
                {
                    continue;
                }
                catch (DirectoryNotFoundException)
                {
                    continue;
                }

                foreach (var file in files)
                {
                    yield return file;
                }

                IEnumerable<string> subDirs;
                try
                {
                    subDirs = Directory.EnumerateDirectories(currentDir);
                }
                catch (UnauthorizedAccessException)
                {
                    continue;
                }
                catch (DirectoryNotFoundException)
                {
                    continue;
                }

                foreach (var subDir in subDirs)
                {
                    stack.Push(subDir);
                }
            }
        }
    }
}