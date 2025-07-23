using FileCraft.ViewModels;
using System.Collections.ObjectModel;

namespace FileCraft.Services.Interfaces
{
    public interface IFolderTreeService
    {
        ObservableCollection<FolderViewModel> BuildFolderTree(string rootPath, Action onSelectionChanged);
    }
}
