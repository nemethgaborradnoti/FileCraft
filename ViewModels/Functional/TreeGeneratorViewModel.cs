using FileCraft.Models;
using FileCraft.Services.Interfaces;
using FileCraft.Shared.Commands;
using FileCraft.ViewModels.Shared;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;

namespace FileCraft.ViewModels.Functional
{
    public class TreeGeneratorViewModel : ExportViewModelBase
    {
        private string _outputFileName = string.Empty;
        private bool _appendTimestamp;
        private int _includedFoldersCount;

        public string OutputFileName
        {
            get => _outputFileName;
            set
            {
                if (_outputFileName != value)
                {
                    OnStateChanging();
                    _outputFileName = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool AppendTimestamp
        {
            get => _appendTimestamp;
            set
            {
                if (_appendTimestamp != value)
                {
                    OnStateChanging();
                    _appendTimestamp = value;
                    OnPropertyChanged();
                }
            }
        }

        public int IncludedFoldersCount
        {
            get => _includedFoldersCount;
            set
            {
                _includedFoldersCount = value;
                OnPropertyChanged();
            }
        }

        public ObservableCollection<FolderViewModel> RootFolders => FolderTreeManager.RootFolders;
        public ICommand GenerateTreeStructureCommand { get; }

        public TreeGeneratorViewModel(
            ISharedStateService sharedStateService,
            IFileOperationService fileOperationService,
            IDialogService dialogService,
            FolderTreeManager folderTreeManager)
            : base(sharedStateService, fileOperationService, dialogService, folderTreeManager)
        {
            FolderTreeManager.FolderSelectionChanged += UpdateIncludedFoldersCount;
            FolderTreeManager.StateChanging += OnStateChanging;
            FolderTreeManager.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(FolderTreeManager.RootFolders))
                {
                    OnPropertyChanged(nameof(RootFolders));
                    UpdateIncludedFoldersCount();
                }
            };

            GenerateTreeStructureCommand = new RelayCommand(async (_) => await GenerateTreeStructure(), (_) => CanExecuteOperation(this.OutputFileName));
            UpdateIncludedFoldersCount();
        }

        private void UpdateIncludedFoldersCount()
        {
            var allNodes = RootFolders.Any() ? RootFolders[0].GetAllNodes() : Enumerable.Empty<FolderViewModel>();
            IncludedFoldersCount = allNodes.Count(n => n.IsSelected != false);
        }

        private async Task GenerateTreeStructure()
        {
            IsBusy = true;
            try
            {
                var allNodes = RootFolders.Any() ? RootFolders[0].GetAllNodes() : Enumerable.Empty<FolderViewModel>();

                var excludedFolderPaths = new HashSet<string>(
                    allNodes.Where(n => n.IsSelected == false).Select(n => n.FullPath),
                    StringComparer.OrdinalIgnoreCase);

                string message = $"Are you sure you want to generate the tree structure to the following path?\n{_sharedStateService.DestinationPath}";
                bool confirmed = _dialogService.ShowConfirmation(
                    title: "Generate Tree Structure",
                    message: message,
                    iconType: DialogIconType.Info,
                    filesAffected: IncludedFoldersCount);

                if (!confirmed)
                {
                    return;
                }

                string finalFileName = GetFinalFileName(OutputFileName, AppendTimestamp);
                string outputFilePath = await _fileOperationService.GenerateTreeStructureAsync(_sharedStateService.SourcePath, _sharedStateService.DestinationPath, excludedFolderPaths, finalFileName);
                _dialogService.ShowNotification("Success", $"Tree structure file was created successfully!\n\nSaved to: {outputFilePath}", DialogIconType.Success);
            }
            catch (Exception ex)
            {
                _dialogService.ShowNotification("Error", $"An unexpected error occurred:\n\n{ex.Message}", DialogIconType.Error);
            }
            finally
            {
                IsBusy = false;
            }
        }
    }
}
