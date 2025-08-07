using FileCraft.Models;
using FileCraft.Services.Interfaces;
using FileCraft.ViewModels;
using System.IO;

namespace FileCraft.Services
{
    public class FileQueryService : IFileQueryService
    {
        public HashSet<string> GetAvailableExtensions(IEnumerable<FolderViewModel> folders)
        {
            var extensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var folder in folders)
            {
                try
                {
                    var files = Directory.GetFiles(folder.FullPath, "*.*", SearchOption.TopDirectoryOnly);
                    foreach (var file in files)
                    {
                        extensions.Add(Path.GetExtension(file));
                    }
                }
                catch (UnauthorizedAccessException)
                {
                    Console.WriteLine($"Access denied to folder: {folder.FullPath}");
                }
            }
            return extensions;
        }

        public List<SelectableFile> GetFilesByExtensions(string basePath, IEnumerable<FolderViewModel> folders, ISet<string> selectedExtensions)
        {
            var files = new List<SelectableFile>();
            foreach (var folder in folders)
            {
                try
                {
                    var filesInFolder = Directory.GetFiles(folder.FullPath, "*.*", SearchOption.TopDirectoryOnly)
                        .Where(f => selectedExtensions.Contains(Path.GetExtension(f), StringComparer.OrdinalIgnoreCase))
                        .Select(f => new SelectableFile
                        {
                            FileName = Path.GetFileName(f),
                            FullPath = f,
                            RelativePath = Path.GetRelativePath(basePath, f),
                            IsSelected = false
                        });
                    files.AddRange(filesInFolder);
                }
                catch (UnauthorizedAccessException)
                {
                    Console.WriteLine($"Access denied to folder: {folder.FullPath}");
                }
            }
            return files;
        }
    }
}
