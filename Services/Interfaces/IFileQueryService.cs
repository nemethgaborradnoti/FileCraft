using FileCraft.Models;
using FileCraft.ViewModels;

namespace FileCraft.Services.Interfaces
{
    public interface IFileQueryService
    {
        HashSet<string> GetAvailableExtensions(IEnumerable<FolderViewModel> folders);

        List<SelectableFile> GetFilesByExtensions(string basePath, IEnumerable<FolderViewModel> folders, ISet<string> selectedExtensions);
    }
}
