using FileCraft.Models;
using FileCraft.Services.Interfaces;
using System.Collections.ObjectModel;
using System.IO;

namespace FileCraft.ViewModels.Shared
{
    public class FolderTreeManager : BaseViewModel
    {
        private readonly IFolderTreeService _folderTreeService;
        private readonly ISettingsService _settingsService;
        private string _currentSourcePath = string.Empty;

        private ObservableCollection<FolderViewModel> _rootFolders = new();
        public ObservableCollection<FolderViewModel> RootFolders
        {
            get => _rootFolders;
            private set
            {
                _rootFolders = value;
                OnPropertyChanged();
            }
        }

        public FolderTreeManager(IFolderTreeService folderTreeService, ISettingsService settingsService)
        {
            _folderTreeService = folderTreeService;
            _settingsService = settingsService;
        }

        public void LoadTreeForPath(string sourcePath)
        {
            if (string.IsNullOrWhiteSpace(sourcePath) || !Directory.Exists(sourcePath))
            {
                RootFolders = new ObservableCollection<FolderViewModel>();
                if (!string.IsNullOrWhiteSpace(_currentSourcePath))
                {
                    SaveState(true);
                }
                _currentSourcePath = string.Empty;
                return;
            }

            _currentSourcePath = sourcePath;

            var newTree = _folderTreeService.BuildFolderTree(sourcePath, () => SaveState());

            var settings = _settingsService.LoadSettings();
            if (settings.SourcePath == sourcePath && settings.FolderTreeState.Any())
            {
                if (newTree.Any())
                {
                    ApplyStateToNode(newTree[0], settings.FolderTreeState);
                }
            }
            else
            {
                if (newTree.Any())
                {
                    RootFolders = newTree;
                    SaveState();
                }
            }

            RootFolders = newTree;
        }

        public void SaveState(bool clear = false)
        {
            var settings = _settingsService.LoadSettings();

            if (clear || !RootFolders.Any() || string.IsNullOrWhiteSpace(_currentSourcePath))
            {
                settings.SourcePath = _currentSourcePath;
                settings.FolderTreeState = new List<FolderState>();
            }
            else
            {
                var states = new List<FolderState>();
                ExtractStateFromNode(RootFolders[0], states);

                settings.SourcePath = _currentSourcePath;
                settings.FolderTreeState = states;
            }

            _settingsService.SaveSettings(settings);
        }

        private void ExtractStateFromNode(FolderViewModel node, List<FolderState> states)
        {
            states.Add(new FolderState
            {
                FullPath = node.FullPath,
                IsSelected = node.IsSelected,
                IsExpanded = node.IsExpanded
            });

            foreach (var child in node.Children)
            {
                ExtractStateFromNode(child, states);
            }
        }

        private void ApplyStateToNode(FolderViewModel node, List<FolderState> savedStates)
        {
            var state = savedStates.FirstOrDefault(s => s.FullPath == node.FullPath);
            if (state != null)
            {
                node.ApplyState(state);
            }

            foreach (var child in node.Children)
            {
                ApplyStateToNode(child, savedStates);
            }
        }
    }
}
