using FileCraft.Services.Interfaces;
using FileCraft.ViewModels;
using System.Collections.ObjectModel;
using System.IO;

namespace FileCraft.Services
{
    public class FolderTreeService : IFolderTreeService
    {
        public ObservableCollection<FolderViewModel> BuildFolderTree(string rootPath, ISet<string> ignoredFolderNames, Action onSelectionChanged, Action onStateChanging)
        {
            var rootFolders = new ObservableCollection<FolderViewModel>();
            if (string.IsNullOrWhiteSpace(rootPath) || !Directory.Exists(rootPath))
            {
                return rootFolders;
            }

            var rootDirInfo = new DirectoryInfo(rootPath);

            Task<IEnumerable<FolderViewModel>> LoadChildrenDelegate(string path, FolderViewModel parent) =>
                GetChildrenAsync(path, parent, ignoredFolderNames, onSelectionChanged, onStateChanging);

            var rootViewModel = CreateFolderViewModel(rootDirInfo.Name, rootDirInfo.FullName, null, onSelectionChanged, onStateChanging, LoadChildrenDelegate);

            rootViewModel.IsExpanded = true;

            rootFolders.Add(rootViewModel);
            return rootFolders;
        }

        private FolderViewModel CreateFolderViewModel(string name, string fullPath, FolderViewModel? parent, Action onSelectionChanged, Action onStateChanging, Func<string, FolderViewModel, Task<IEnumerable<FolderViewModel>>> loadChildren)
        {
            var viewModel = new FolderViewModel(name, fullPath, parent, onSelectionChanged, onStateChanging, loadChildren);

            if (HasSubdirectories(fullPath))
            {
                viewModel.Children.Add(FolderViewModel.CreateDummy());
            }

            return viewModel;
        }

        private async Task<IEnumerable<FolderViewModel>> GetChildrenAsync(string path, FolderViewModel parent, ISet<string> ignoredFolderNames, Action onSelectionChanged, Action onStateChanging)
        {
            return await Task.Run(() =>
            {
                var children = new List<FolderViewModel>();
                try
                {
                    var subDirs = Directory.EnumerateDirectories(path)
                        .Where(d => !ignoredFolderNames.Contains(new DirectoryInfo(d).Name, StringComparer.OrdinalIgnoreCase));

                    Task<IEnumerable<FolderViewModel>> LoadChildrenDelegate(string p, FolderViewModel par) =>
                        GetChildrenAsync(p, par, ignoredFolderNames, onSelectionChanged, onStateChanging);

                    foreach (var dirPath in subDirs)
                    {
                        var dirInfo = new DirectoryInfo(dirPath);
                        var childViewModel = CreateFolderViewModel(dirInfo.Name, dirInfo.FullName, parent, onSelectionChanged, onStateChanging, LoadChildrenDelegate);
                        children.Add(childViewModel);
                    }
                }
                catch (UnauthorizedAccessException)
                {
                }
                return children;
            });
        }

        private bool HasSubdirectories(string path)
        {
            try
            {
                return Directory.EnumerateDirectories(path).Any();
            }
            catch
            {
                return false;
            }
        }
    }
}