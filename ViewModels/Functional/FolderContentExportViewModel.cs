using FileCraft.Models;
using FileCraft.Services.Interfaces;
using FileCraft.Shared.Commands;
using FileCraft.Shared.Helpers;
using FileCraft.ViewModels.Shared;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Windows.Input;

namespace FileCraft.ViewModels.Functional
{
    public enum FolderContentFullscreenState
    {
        None,
        Folders,
        Details
    }

    public class FolderContentExportViewModel : ExportViewModelBase
    {
        private bool? _areAllColumnsSelected;
        private int _affectedFilesCount;

        public FullscreenManager<FolderContentFullscreenState> FullscreenManager { get; }

        public bool? AreAllColumnsSelected
        {
            get => _areAllColumnsSelected;
            set
            {
                OnStateChanging();
                bool selectAll = _areAllColumnsSelected != true;
                SetColumnsSelectionState(selectAll);
            }
        }

        public int AffectedFilesCount
        {
            get => _affectedFilesCount;
            set
            {
                _affectedFilesCount = value;
                OnPropertyChanged();
            }
        }

        public ObservableCollection<SelectableItemViewModel> AvailableColumns { get; } = new();
        public ObservableCollection<FolderViewModel> RootFolders => FolderTreeManager.RootFolders;
        public ICommand ExportFolderContentsCommand { get; }

        public FolderContentExportViewModel(
            ISharedStateService sharedStateService,
            IFileOperationService fileOperationService,
            IDialogService dialogService,
            FolderTreeManager folderTreeManager)
            : base(sharedStateService, fileOperationService, dialogService, folderTreeManager)
        {
            FullscreenManager = new FullscreenManager<FolderContentFullscreenState>(FolderContentFullscreenState.None);

            FolderTreeManager.FolderSelectionChanged += UpdateAffectedFilesCount;
            FolderTreeManager.StateChanging += OnStateChanging;
            FolderTreeManager.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(FolderTreeManager.RootFolders))
                {
                    OnPropertyChanged(nameof(RootFolders));
                    UpdateAffectedFilesCount();
                }
            };

            ExportFolderContentsCommand = new RelayCommand(async (_) => await ExportFolderContents(), (_) => CanExecuteOperation(this.OutputFileName) && AvailableColumns.Any(c => c.IsSelected));

            var columns = new List<string> {
                "Name", "Size (byte)", "CreationTime", "LastWriteTime", "LastAccessTime",
                "IsReadOnly", "Attributes", "FullPath", "Parent", "Format"
            };
            foreach (var col in columns)
            {
                var item = new SelectableItemViewModel(col, true, this.OnStateChanging);
                item.PropertyChanged += OnColumnSelectionChanged;
                AvailableColumns.Add(item);
            }
            UpdateSelectAllColumnsState();
            UpdateAffectedFilesCount();
        }

        private void OnColumnSelectionChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(SelectableItemViewModel.IsSelected))
            {
                UpdateSelectAllColumnsState();
            }
        }

        private void SetColumnsSelectionState(bool isSelected)
        {
            foreach (var column in AvailableColumns)
                column.PropertyChanged -= OnColumnSelectionChanged;

            foreach (var column in AvailableColumns)
            {
                column.SetIsSelectedInternal(isSelected);
            }

            foreach (var column in AvailableColumns)
                column.PropertyChanged += OnColumnSelectionChanged;

            UpdateSelectAllColumnsState();
        }

        private void UpdateSelectAllColumnsState()
        {
            bool? newSelectionState = SelectionHelper.GetMasterSelectionState(AvailableColumns);

            if (_areAllColumnsSelected != newSelectionState)
            {
                _areAllColumnsSelected = newSelectionState;
                OnPropertyChanged(nameof(AreAllColumnsSelected));
            }
        }

        public void ApplySettings(FolderContentExportSettings settings)
        {
            OutputFileName = settings.OutputFileName;
            AppendTimestamp = settings.AppendTimestamp;
            var loadedSelectedColumns = new HashSet<string>(settings.SelectedColumns ?? new List<string>());

            foreach (var column in AvailableColumns)
            {
                column.IsSelected = loadedSelectedColumns.Contains(column.Name);
            }

            UpdateSelectAllColumnsState();
        }

        public List<string> GetSelectedColumns()
        {
            return AvailableColumns.Where(c => c.IsSelected).Select(c => c.Name).ToList();
        }

        private void UpdateAffectedFilesCount()
        {
            var allNodes = RootFolders.Any() ? RootFolders[0].GetAllNodes() : Enumerable.Empty<FolderViewModel>();
            var includedFolderPaths = allNodes
                .Where(n => n.IsSelected != false)
                .Select(n => n.FullPath)
                .ToList();

            int fileCount = 0;
            foreach (var folderPath in includedFolderPaths)
            {
                if (Directory.Exists(folderPath))
                {
                    try
                    {
                        fileCount += Directory.GetFiles(folderPath, "*.*", SearchOption.TopDirectoryOnly).Length;
                    }
                    catch (UnauthorizedAccessException) { }
                }
            }
            AffectedFilesCount = fileCount;
        }

        private async Task ExportFolderContents()
        {
            IsBusy = true;
            try
            {
                var allNodes = RootFolders.Any() ? RootFolders[0].GetAllNodes() : Enumerable.Empty<FolderViewModel>();
                var includedFolderPaths = allNodes
                    .Where(n => n.IsSelected != false)
                    .Select(n => n.FullPath)
                    .ToList();

                if (!includedFolderPaths.Any())
                {
                    _dialogService.ShowNotification(
                        ResourceHelper.GetString("Common_InfoTitle"),
                        ResourceHelper.GetString("FolderContent_NoFoldersSelected"),
                        DialogIconType.Info);
                    return;
                }

                var selectedColumns = GetSelectedColumns();
                if (!selectedColumns.Any())
                {
                    _dialogService.ShowNotification(
                        ResourceHelper.GetString("Common_InfoTitle"),
                        ResourceHelper.GetString("FolderContent_NoColumnsSelected"),
                        DialogIconType.Info);
                    return;
                }

                string messageFormat = ResourceHelper.GetString("FolderContent_ConfirmExportMessage");
                string message = $"{messageFormat}\n{_sharedStateService.DestinationPath}";

                bool confirmed = _dialogService.ShowConfirmation(
                    title: ResourceHelper.GetString("FolderContent_ExportTitle"),
                    message: message,
                    iconType: DialogIconType.Info,
                    filesAffected: AffectedFilesCount);

                if (!confirmed)
                {
                    return;
                }

                string finalFileName = GetFinalFileName(OutputFileName, AppendTimestamp);
                string outputFilePath = await _fileOperationService.ExportFolderContentsAsync(_sharedStateService.DestinationPath, includedFolderPaths, finalFileName, selectedColumns);

                string successMsg = ResourceHelper.GetString("FolderContent_SuccessMessage");
                string savedToMsg = string.Format(ResourceHelper.GetString("Common_SavedTo"), outputFilePath);

                _dialogService.ShowNotification(
                    ResourceHelper.GetString("Common_SuccessTitle"),
                    $"{successMsg}\n\n{savedToMsg}",
                    DialogIconType.Success);
            }
            catch (Exception ex)
            {
                string errorMsg = string.Format(ResourceHelper.GetString("FolderContent_ErrorMessage"), ex.Message);
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