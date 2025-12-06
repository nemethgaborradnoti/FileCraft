using FileCraft.Models;
using FileCraft.Services.Interfaces;
using FileCraft.Shared.Commands;
using FileCraft.Shared.Helpers;
using FileCraft.ViewModels.Shared;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace FileCraft.ViewModels.Functional
{
    public enum TreeGeneratorFullscreenState
    {
        None,
        Folders
    }

    public class TreeGeneratorViewModel : ExportViewModelBase
    {
        private string _outputFileName = string.Empty;
        private bool _appendTimestamp;
        private int _includedFoldersCount;

        public FullscreenManager<TreeGeneratorFullscreenState> FullscreenManager { get; }

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
            FullscreenManager = new FullscreenManager<TreeGeneratorFullscreenState>(TreeGeneratorFullscreenState.None);

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

                string messageFormat = ResourceHelper.GetString("TreeGen_ConfirmGenerateMessage");
                string message = $"{messageFormat}\n{_sharedStateService.DestinationPath}";

                bool confirmed = _dialogService.ShowConfirmation(
                    title: ResourceHelper.GetString("TreeGen_GenerateTitle"),
                    message: message,
                    iconType: DialogIconType.Info,
                    filesAffected: IncludedFoldersCount);

                if (!confirmed)
                {
                    return;
                }

                string finalFileName = GetFinalFileName(OutputFileName, AppendTimestamp);
                string outputFilePath = await _fileOperationService.GenerateTreeStructureAsync(_sharedStateService.SourcePath, _sharedStateService.DestinationPath, excludedFolderPaths, finalFileName);

                string successMsg = ResourceHelper.GetString("TreeGen_SuccessMessage");
                string savedToMsg = ResourceHelper.GetString("Common_SavedTo");

                _dialogService.ShowNotification(
                    ResourceHelper.GetString("Common_SuccessTitle"),
                    $"{successMsg}\n\n{savedToMsg} {outputFilePath}",
                    DialogIconType.Success);
            }
            catch (Exception ex)
            {
                string errorMsg = ResourceHelper.GetString("TreeGen_ErrorMessage");
                _dialogService.ShowNotification(
                    ResourceHelper.GetString("Common_ErrorTitle"),
                    $"{errorMsg}\n\n{ex.Message}",
                    DialogIconType.Error);
            }
            finally
            {
                IsBusy = false;
            }
        }
    }
}