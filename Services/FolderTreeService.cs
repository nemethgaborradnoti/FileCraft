using FileCraft.Services.Interfaces;
using FileCraft.ViewModels;
using System;
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
            var rootViewModel = new FolderViewModel(rootDirInfo.Name, rootDirInfo.FullName, null, onSelectionChanged, onStateChanging);

            PopulateChildren(rootViewModel, ignoredFolderNames, onSelectionChanged, onStateChanging);

            rootFolders.Add(rootViewModel);
            return rootFolders;
        }

        private void PopulateChildren(FolderViewModel parent, ISet<string> ignoredFolderNames, Action onSelectionChanged, Action onStateChanging)
        {
            try
            {
                var subDirs = Directory.GetDirectories(parent.FullPath)
                    .Where(d => !ignoredFolderNames.Contains(new DirectoryInfo(d).Name, StringComparer.OrdinalIgnoreCase));

                foreach (var dirPath in subDirs)
                {
                    var dirInfo = new DirectoryInfo(dirPath);
                    var childViewModel = new FolderViewModel(dirInfo.Name, dirInfo.FullName, parent, onSelectionChanged, onStateChanging);
                    parent.Children.Add(childViewModel);

                    PopulateChildren(childViewModel, ignoredFolderNames, onSelectionChanged, onStateChanging);
                }
            }
            catch (UnauthorizedAccessException)
            {
            }
        }
    }
}
