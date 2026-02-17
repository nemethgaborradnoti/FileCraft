using FileCraft.Models;
using FileCraft.Services.Interfaces;
using FileCraft.ViewModels;
using System.Collections.ObjectModel;
using System.IO;

namespace FileCraft.ViewModels.Shared
{
    public class FolderTreeManager : BaseViewModel
    {
        private readonly IFolderTreeService _folderTreeService;
        private readonly ISharedStateService _sharedStateService;
        private string _currentSourcePath = string.Empty;

        public event Action? FolderSelectionChanged;

        private ObservableCollection<FolderViewModel> _rootFolders = new();
        public ObservableCollection<FolderViewModel> RootFolders
        {
            get => _rootFolders;
            private set
            {
                if (_rootFolders != value)
                {
                    _rootFolders = value;
                    OnPropertyChanged();
                }
            }
        }

        public string Id { get; set; } = string.Empty;

        public string CurrentSourcePath => _currentSourcePath;

        public FolderTreeManager(IFolderTreeService folderTreeService, ISharedStateService sharedStateService)
        {
            _folderTreeService = folderTreeService;
            _sharedStateService = sharedStateService;
        }

        public async void RefreshTree()
        {
            if (!string.IsNullOrWhiteSpace(_currentSourcePath))
            {
                var currentState = GetFolderStates();
                await LoadTreeForPathAsync(_currentSourcePath, currentState);
            }
        }

        public void LoadTreeForPath(string sourcePath, List<FolderState>? folderState = null)
        {
            _ = LoadTreeForPathAsync(sourcePath, folderState);
        }

        public async Task LoadTreeForPathAsync(string sourcePath, List<FolderState>? folderState = null)
        {
            if (string.IsNullOrWhiteSpace(sourcePath))
            {
                RootFolders = new ObservableCollection<FolderViewModel>();
                _currentSourcePath = string.Empty;
                return;
            }

            if (!Directory.Exists(sourcePath))
            {
                RootFolders = new ObservableCollection<FolderViewModel>();
                _currentSourcePath = string.Empty;
                return;
            }

            if (sourcePath == _currentSourcePath && folderState == null)
            {
                return;
            }

            _currentSourcePath = sourcePath;

            var ignoredFoldersSet = new HashSet<string>(_sharedStateService.IgnoredFolders, StringComparer.OrdinalIgnoreCase);

            var newTree = _folderTreeService.BuildFolderTree(sourcePath, ignoredFoldersSet, HandleFolderStateChange, OnStateChanging);

            if (folderState != null && newTree.Any())
            {
                await ApplyStateToNodeAsync(newTree[0], folderState);
            }

            RootFolders = newTree;
        }

        public void SetSharedRootFolders(ObservableCollection<FolderViewModel> sharedFolders, string sourcePath)
        {
            _currentSourcePath = sourcePath;
            RootFolders = sharedFolders;
            HandleFolderStateChange();
        }

        public void UnlinkAndCloneState()
        {
            var clonedStates = GetFolderStates();
            var currentPath = _currentSourcePath;

            RootFolders = new ObservableCollection<FolderViewModel>();

            _ = LoadTreeForPathAsync(currentPath, clonedStates);
        }

        private void HandleFolderStateChange()
        {
            FolderSelectionChanged?.Invoke();
        }

        public int GetSelectedNodeCount()
        {
            var root = RootFolders.FirstOrDefault();
            if (root is null)
            {
                return 0;
            }
            return root.GetAllNodes().Count(n => n.IsSelected != false);
        }

        public List<FolderState> GetFolderStates()
        {
            var root = RootFolders.FirstOrDefault();
            if (root is null)
            {
                return new List<FolderState>();
            }

            var states = new List<FolderState>();
            ExtractStateFromNode(root, states);
            return states;
        }

        private void ExtractStateFromNode(FolderViewModel node, List<FolderState> states)
        {
            if (node.IsDummy) return;

            bool shouldSaveState = node.IsSelected != false || node.IsExpanded;

            if (shouldSaveState)
            {
                states.Add(new FolderState
                {
                    FullPath = node.FullPath,
                    IsSelected = node.IsSelected,
                    IsExpanded = node.IsExpanded
                });
            }

            foreach (var child in node.Children)
            {
                ExtractStateFromNode(child, states);
            }
        }

        private async Task ApplyStateToNodeAsync(FolderViewModel node, List<FolderState> savedStates)
        {
            var state = savedStates.FirstOrDefault(s => s.FullPath == node.FullPath);
            if (state != null)
            {
                await node.ApplyStateAsync(state);
            }

            foreach (var child in node.Children.ToList())
            {
                await ApplyStateToNodeAsync(child, savedStates);
            }
        }
    }
}