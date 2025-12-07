using FileCraft.Models;
using FileCraft.Services.Interfaces;
using FileCraft.Shared.Commands;
using FileCraft.Shared.Helpers;
using FileCraft.ViewModels.Shared;
using System.ComponentModel;
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
        private int _includedFoldersCount;

        public FullscreenManager<TreeGeneratorFullscreenState> FullscreenManager { get; }

        public int IncludedFoldersCount
        {
            get => _includedFoldersCount;
            set
            {
                _includedFoldersCount = value;
                OnPropertyChanged();
            }
        }

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

            GenerateTreeStructureCommand = new RelayCommand(async (_) => await GenerateTreeStructure(), (_) => CanExecuteOperation(this.OutputFileName));
            UpdateIncludedFoldersCount();
        }

        protected override void OnFolderTreeManagerPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            base.OnFolderTreeManagerPropertyChanged(sender, e);
            if (e.PropertyName == nameof(FolderTreeManager.RootFolders))
            {
                UpdateIncludedFoldersCount();
            }
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
                string savedToMsg = string.Format(ResourceHelper.GetString("Common_SavedTo"), outputFilePath);

                _dialogService.ShowNotification(
                    ResourceHelper.GetString("Common_SuccessTitle"),
                    $"{successMsg}\n\n{savedToMsg}",
                    DialogIconType.Success);
            }
            catch (Exception ex)
            {
                string errorMsg = string.Format(ResourceHelper.GetString("TreeGen_ErrorMessage"), ex.Message);
                _dialogService.ShowNotification(
                    ResourceHelper.GetString("Common_ErrorTitle"),
                    errorMsg,
                    DialogIconType.Error);
            }
            finally
            {
                IsBusy = false;
            }
        }
    }
}