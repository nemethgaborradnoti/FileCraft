using FileCraft.Models;
using FileCraft.Services.Interfaces;
using System;
using System.Collections.ObjectModel;
using System.IO;

namespace FileCraft.ViewModels.Shared
{
    public class FolderTreeManager : BaseViewModel
    {
        private readonly IFolderTreeService _folderTreeService;
        private string _currentSourcePath = string.Empty;

        public event Action? FolderSelectionChanged;

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

        public FolderTreeManager(IFolderTreeService folderTreeService)
        {
            _folderTreeService = folderTreeService;
        }

        public void LoadTreeForPath(string sourcePath, List<FolderState>? folderState = null)
        {
            if (string.IsNullOrWhiteSpace(sourcePath) || !Directory.Exists(sourcePath))
            {
                RootFolders = new ObservableCollection<FolderViewModel>();
                _currentSourcePath = string.Empty;
                return;
            }

            if (sourcePath == _currentSourcePath && folderState == null) return;
            _currentSourcePath = sourcePath;

            var newTree = _folderTreeService.BuildFolderTree(sourcePath, HandleFolderStateChange, OnStateChanging);

            if (folderState != null && newTree.Any())
            {
                ApplyStateToNode(newTree[0], folderState);
            }
            RootFolders = newTree;
        }

        private void HandleFolderStateChange()
        {
            FolderSelectionChanged?.Invoke();
        }

        public List<FolderState> GetFolderStates()
        {
            if (!RootFolders.Any())
            {
                return new List<FolderState>();
            }

            var states = new List<FolderState>();
            ExtractStateFromNode(RootFolders[0], states);
            return states;
        }

        private void ExtractStateFromNode(FolderViewModel node, List<FolderState> states)
        {
            bool isDefaultState = node.IsSelected == false && node.IsExpanded == true;
            if (!isDefaultState)
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
