using FileCraft.Services.Interfaces;
using FileCraft.ViewModels;
using System;
using System.Collections.ObjectModel;
using System.IO;

namespace FileCraft.Services
{
    public class FolderTreeService : IFolderTreeService
    {
        public ObservableCollection<FolderViewModel> BuildFolderTree(string rootPath, Action onSelectionChanged, Action onStateChanging)
        {
            var rootFolders = new ObservableCollection<FolderViewModel>();
            if (string.IsNullOrWhiteSpace(rootPath) || !Directory.Exists(rootPath))
            {
                return rootFolders;
            }

            var rootDirInfo = new DirectoryInfo(rootPath);
            var rootViewModel = new FolderViewModel(rootDirInfo.Name, rootDirInfo.FullName, null, onSelectionChanged, onStateChanging);

            PopulateChildren(rootViewModel, onSelectionChanged, onStateChanging);

            rootFolders.Add(rootViewModel);
            return rootFolders;
        }

        private void PopulateChildren(FolderViewModel parent, Action onSelectionChanged, Action onStateChanging)
        {
            try
            {
                var subDirs = Directory.GetDirectories(parent.FullPath);
                foreach (var dirPath in subDirs)
                {
                    var dirInfo = new DirectoryInfo(dirPath);
                    var childViewModel = new FolderViewModel(dirInfo.Name, dirInfo.FullName, parent, onSelectionChanged, onStateChanging);
                    parent.Children.Add(childViewModel);

                    PopulateChildren(childViewModel, onSelectionChanged, onStateChanging);
                }
            }
            catch (UnauthorizedAccessException)
            {
            }
        }
    }
}
