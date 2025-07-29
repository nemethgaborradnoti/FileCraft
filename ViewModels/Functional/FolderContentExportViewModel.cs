using FileCraft.Models;
using FileCraft.Services.Interfaces;
using FileCraft.Shared.Commands;
using FileCraft.Shared.Helpers;
using FileCraft.ViewModels.Shared;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Input;

namespace FileCraft.ViewModels.Functional
{
    public class FolderContentExportViewModel : ExportViewModelBase
    {
        private string _outputFileName = string.Empty;
        private bool _appendTimestamp;
        private bool? _areAllColumnsSelected;

        public string OutputFileName
        {
            get => _outputFileName;
            set
            {
                if (_outputFileName != value)
                {
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
                    _appendTimestamp = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool? AreAllColumnsSelected
        {
            get => _areAllColumnsSelected;
            set
            {
                bool selectAll = _areAllColumnsSelected != true;
                SetColumnsSelectionState(selectAll);
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
            FolderTreeManager.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(FolderTreeManager.RootFolders))
                {
                    OnPropertyChanged(nameof(RootFolders));
                }
            };

            ExportFolderContentsCommand = new RelayCommand(async (_) => await ExportFolderContents(), (_) => CanExecuteOperation(this.OutputFileName) && AvailableColumns.Any(c => c.IsSelected));

            var columns = new List<string> {
                "Name",
                "Size (byte)",
                "CreationTime",
                "LastWriteTime",
                "LastAccessTime",
                "IsReadOnly",
                "Attributes",
                "FullPath",
                "Parent",
                "Format"
            };
            foreach (var col in columns)
            {
                var item = new SelectableItemViewModel(col, true);
                item.PropertyChanged += OnColumnSelectionChanged;
                AvailableColumns.Add(item);
            }
            UpdateSelectAllColumnsState();
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
            {
                column.PropertyChanged -= OnColumnSelectionChanged;
            }

            SelectionHelper.SetSelectionState(AvailableColumns, isSelected);

            foreach (var column in AvailableColumns)
            {
                column.PropertyChanged += OnColumnSelectionChanged;
            }
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
            var loadedSelectedColumns = settings.SelectedColumns ?? new List<string>();

            if (loadedSelectedColumns.Any())
            {
                foreach (var column in AvailableColumns)
                {
                    column.IsSelected = loadedSelectedColumns.Contains(column.Name);
                }
            }
        }

        public List<string> GetSelectedColumns()
        {
            return AvailableColumns.Where(c => c.IsSelected).Select(c => c.Name).ToList();
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
                    _dialogService.ShowNotification("Information", "No folders were selected. Please select at least one folder.");
                    return;
                }

                var selectedColumns = GetSelectedColumns();
                if (!selectedColumns.Any())
                {
                    _dialogService.ShowNotification("Information", "No columns were selected. Please select at least one column to export.");
                    return;
                }

                string finalFileName = GetFinalFileName(OutputFileName, AppendTimestamp);
                string outputFilePath = await _fileOperationService.ExportFolderContentsAsync(_sharedStateService.DestinationPath, includedFolderPaths, finalFileName, selectedColumns);
                _dialogService.ShowNotification("Success", $"Folder contents exported successfully!\n\nSaved to: {outputFilePath}");
            }
            catch (Exception ex)
            {
                _dialogService.ShowNotification("Error", $"An unexpected error occurred:\n\n{ex.Message}");
            }
            finally
            {
                IsBusy = false;
            }
        }
    }
}
