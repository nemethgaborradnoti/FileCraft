using FileCraft.Models;
using FileCraft.Services.Interfaces;
using FileCraft.Shared.Commands;
using FileCraft.Shared.Helpers;
using FileCraft.ViewModels.Shared;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows.Input;
using Timer = System.Threading.Timer;

namespace FileCraft.ViewModels.Functional
{
    public class FileContentExportViewModel : ExportViewModelBase
    {
        private readonly IFileQueryService _fileQueryService;
        private readonly Timer _debounceTimer;
        private string _searchFilter = string.Empty;
        private bool _isShowingOnlySelected;
        private readonly ObservableCollection<SelectableFile> _allSelectableFiles = new();
        public ObservableCollection<SelectableFile> FilteredSelectableFiles { get; } = new();
        private int _totalFilesCount;
        private int _availableFilesCount;
        private int _selectedFilesCount;
        private bool? _areAllFilesSelected;
        private bool? _areAllExtensionsSelected;
        private string _outputFileName = string.Empty;
        private bool _appendTimestamp;

        public int TotalFilesCount
        {
            get => _totalFilesCount;
            set { _totalFilesCount = value; OnPropertyChanged(); }
        }

        public int AvailableFilesCount
        {
            get => _availableFilesCount;
            set { _availableFilesCount = value; OnPropertyChanged(); }
        }

        public int SelectedFilesCount
        {
            get => _selectedFilesCount;
            set { _selectedFilesCount = value; OnPropertyChanged(); }
        }

        public string SearchFilter
        {
            get => _searchFilter;
            set
            {
                if (_searchFilter != value)
                {
                    _searchFilter = value;
                    OnPropertyChanged();
                    _debounceTimer.Change(300, Timeout.Infinite);
                    OnPropertyChanged(nameof(CanClearFilter));
                }
            }
        }

        public bool IsShowingOnlySelected
        {
            get => _isShowingOnlySelected;
            set
            {
                if (_isShowingOnlySelected != value)
                {
                    _isShowingOnlySelected = value;
                    OnPropertyChanged();
                    ApplyFileFilter();
                    OnPropertyChanged(nameof(CanClearFilter));
                }
            }
        }

        public bool CanClearFilter => IsShowingOnlySelected || !string.IsNullOrWhiteSpace(SearchFilter);

        public bool? AreAllFilesSelected
        {
            get => _areAllFilesSelected;
            set
            {
                OnStateChanging();
                bool selectAll = _areAllFilesSelected != true;
                SetFilteredFilesSelectionState(selectAll);
            }
        }

        public bool? AreAllExtensionsSelected
        {
            get => _areAllExtensionsSelected;
            set
            {
                OnStateChanging();
                bool selectAll = _areAllExtensionsSelected != true;
                SetExtensionsSelectionState(selectAll);
            }
        }

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

        public ObservableCollection<SelectableItemViewModel> AvailableExtensions { get; } = new ObservableCollection<SelectableItemViewModel>();
        public ObservableCollection<FolderViewModel> RootFolders => FolderTreeManager.RootFolders;

        public ICommand ExportFileContentCommand { get; }
        public ICommand ClearFilterCommand { get; }

        public FileContentExportViewModel(
            ISharedStateService sharedStateService,
            IFileOperationService fileOperationService,
            IDialogService dialogService,
            FolderTreeManager folderTreeManager,
            IFileQueryService fileQueryService)
            : base(sharedStateService, fileOperationService, dialogService, folderTreeManager)
        {
            _fileQueryService = fileQueryService;
            _debounceTimer = new Timer(OnDebounceTimerElapsed, null, Timeout.Infinite, Timeout.Infinite);

            FolderTreeManager.PropertyChanged += OnFolderTreeManagerPropertyChanged;
            FolderTreeManager.FolderSelectionChanged += OnFolderSelectionChanged;
            FolderTreeManager.StateChanging += OnStateChanging;

            ExportFileContentCommand = new RelayCommand(async (_) => await ExportFileContentAsync(), (_) => CanExecuteOperation(this.OutputFileName) && _allSelectableFiles.Any(f => f.IsSelected));
            ClearFilterCommand = new RelayCommand(_ => ClearFilter());

            OnFolderSelectionChanged();
            UpdateFileCounts();
            UpdateExtensionMasterState();
        }

        private void ClearFilter()
        {
            SearchFilter = string.Empty;
            IsShowingOnlySelected = false;
        }

        public void ApplySettings(FileContentExportSettings settings)
        {
            OutputFileName = settings.OutputFileName;
            AppendTimestamp = settings.AppendTimestamp;

            var loadedSelectedExtensions = new HashSet<string>(settings.SelectedExtensions ?? new List<string>());
            foreach (var extVM in AvailableExtensions)
            {
                extVM.IsSelected = loadedSelectedExtensions.Contains(extVM.Name);
            }

            UpdateSelectableFiles();

            var loadedSelectedFilePaths = new HashSet<string>(settings.SelectedFilePaths ?? new List<string>());

            foreach (var fileVM in _allSelectableFiles)
            {
                fileVM.IsSelected = loadedSelectedFilePaths.Contains(fileVM.FullPath);
            }

            UpdateFileCounts();
            UpdateExtensionMasterState();
            ApplyFileFilter();
        }

        public List<string> GetSelectedFilePaths()
        {
            return _allSelectableFiles.Where(f => f.IsSelected).Select(f => f.FullPath).ToList();
        }

        public List<string> GetSelectedExtensions()
        {
            return AvailableExtensions.Where(e => e.IsSelected).Select(e => e.Name).ToList();
        }

        private void SetFilteredFilesSelectionState(bool isSelected)
        {
            foreach (var file in FilteredSelectableFiles)
                file.PropertyChanged -= OnFileSelectionChanged;

            foreach (var file in FilteredSelectableFiles)
            {
                file.SetIsSelectedInternal(isSelected);
            }

            foreach (var file in FilteredSelectableFiles)
                file.PropertyChanged += OnFileSelectionChanged;

            UpdateFileCounts();
        }

        private void SetExtensionsSelectionState(bool isSelected)
        {
            foreach (var ext in AvailableExtensions)
                ext.PropertyChanged -= OnExtensionSelectionChanged;

            foreach (var ext in AvailableExtensions)
            {
                ext.SetIsSelectedInternal(isSelected);
            }

            foreach (var ext in AvailableExtensions)
                ext.PropertyChanged += OnExtensionSelectionChanged;

            UpdateSelectableFiles();
            UpdateExtensionMasterState();
        }

        private void OnDebounceTimerElapsed(object? state) => ApplyFileFilter();

        private void ApplyFileFilter()
        {
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                FilteredSelectableFiles.Clear();
                IEnumerable<SelectableFile> filtered = _allSelectableFiles;

                if (IsShowingOnlySelected)
                {
                    filtered = filtered.Where(f => f.IsSelected);
                }

                if (!string.IsNullOrWhiteSpace(SearchFilter))
                {
                    filtered = filtered.Where(f => f.FullPath.IndexOf(SearchFilter, StringComparison.OrdinalIgnoreCase) >= 0);
                }

                foreach (var file in filtered)
                    FilteredSelectableFiles.Add(file);
                UpdateFileCounts();
            });
        }

        private void OnFolderTreeManagerPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(FolderTreeManager.RootFolders))
            {
                OnFolderSelectionChanged();
                OnPropertyChanged(nameof(RootFolders));
            }
        }

        private void OnFolderSelectionChanged()
        {
            UpdateAvailableExtensions();
            UpdateSelectableFiles();
        }

        private void UpdateAvailableExtensions()
        {
            var selectedFolders = GetSelectedFoldersForFileListing();
            var extensions = _fileQueryService.GetAvailableExtensions(selectedFolders);
            var previouslySelected = new HashSet<string>(GetSelectedExtensions());

            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                foreach (var ext in AvailableExtensions)
                    ext.PropertyChanged -= OnExtensionSelectionChanged;

                AvailableExtensions.Clear();
                foreach (var ext in extensions.Where(e => !string.IsNullOrEmpty(e)).OrderBy(e => e))
                {
                    var item = new SelectableItemViewModel(ext, previouslySelected.Contains(ext), this.OnStateChanging);
                    item.PropertyChanged += OnExtensionSelectionChanged;
                    AvailableExtensions.Add(item);
                }
                UpdateExtensionMasterState();
            });
        }

        private void UpdateSelectableFiles()
        {
            var selectedFolders = GetSelectedFoldersForFileListing();
            var selectedExtensions = new HashSet<string>(GetSelectedExtensions(), StringComparer.OrdinalIgnoreCase);
            var files = _fileQueryService.GetFilesByExtensions(_sharedStateService.SourcePath, selectedFolders, selectedExtensions);
            var previouslySelectedFiles = new HashSet<string>(_allSelectableFiles.Where(f => f.IsSelected).Select(f => f.FullPath));

            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                foreach (var file in _allSelectableFiles)
                    file.PropertyChanged -= OnFileSelectionChanged;
                _allSelectableFiles.Clear();

                foreach (var file in files.OrderBy(f => f.FullPath))
                {
                    if (previouslySelectedFiles.Contains(file.FullPath))
                    {
                        file.IsSelected = true;
                    }
                    file.SetStateChangingAction(this.OnStateChanging);
                    file.PropertyChanged += OnFileSelectionChanged;
                    _allSelectableFiles.Add(file);
                }

                ApplyFileFilter();
            });
        }

        private void OnFileSelectionChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(SelectableFile.IsSelected))
            {
                UpdateFileCounts();
                if (IsShowingOnlySelected)
                {
                    ApplyFileFilter();
                }
            }
        }

        private void OnExtensionSelectionChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(SelectableItemViewModel.IsSelected))
            {
                UpdateSelectableFiles();
                UpdateExtensionMasterState();
            }
        }

        private void UpdateFileCounts()
        {
            TotalFilesCount = _allSelectableFiles.Count;
            AvailableFilesCount = FilteredSelectableFiles.Count;
            SelectedFilesCount = _allSelectableFiles.Count(f => f.IsSelected);

            bool? newSelectionState = SelectionHelper.GetMasterSelectionState(FilteredSelectableFiles);
            if (_areAllFilesSelected != newSelectionState)
            {
                _areAllFilesSelected = newSelectionState;
                OnPropertyChanged(nameof(AreAllFilesSelected));
            }
        }

        private void UpdateExtensionMasterState()
        {
            bool? newSelectionState = SelectionHelper.GetMasterSelectionState(AvailableExtensions);
            if (_areAllExtensionsSelected != newSelectionState)
            {
                _areAllExtensionsSelected = newSelectionState;
                OnPropertyChanged(nameof(AreAllExtensionsSelected));
            }
        }

        private List<FolderViewModel> GetSelectedFoldersForFileListing()
        {
            if (!RootFolders.Any()) return new List<FolderViewModel>();
            return RootFolders[0].GetAllNodes().Where(n => n.IsSelected != false).ToList();
        }

        private async Task ExportFileContentAsync()
        {
            IsBusy = true;
            try
            {
                var selectedFiles = _allSelectableFiles.Where(f => f.IsSelected).ToList();
                if (!selectedFiles.Any())
                {
                    _dialogService.ShowNotification("Information", "No files were selected. Please select at least one file to export.", DialogIconType.Info);
                    return;
                }

                string message = $"Are you sure you want to export file contents to the following path?\n{_sharedStateService.DestinationPath}";
                bool confirmed = _dialogService.ShowConfirmation(
                    title: "Export File Contents",
                    message: message,
                    iconType: DialogIconType.Info,
                    filesAffected: selectedFiles.Count);

                if (!confirmed)
                {
                    return;
                }

                string finalFileName = GetFinalFileName(OutputFileName, AppendTimestamp);
                string outputFilePath = await _fileOperationService.ExportSelectedFileContentsAsync(_sharedStateService.DestinationPath, selectedFiles, finalFileName);
                _dialogService.ShowNotification("Success", $"File contents exported successfully!\n\n{selectedFiles.Count} files were processed.\nSaved to: {outputFilePath}", DialogIconType.Success);
            }
            catch (Exception ex)
            {
                _dialogService.ShowNotification("Error", $"An unexpected error occurred during export:\n\n{ex.Message}", DialogIconType.Error);
            }
            finally
            {
                IsBusy = false;
            }
        }
    }
}

