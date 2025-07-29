using FileCraft.Models;
using FileCraft.Services.Interfaces;
using FileCraft.Shared.Commands;
using FileCraft.Shared.Helpers;
using FileCraft.ViewModels.Shared;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Input;
using Timer = System.Threading.Timer;

namespace FileCraft.ViewModels.Functional
{
    public class FileContentExportViewModel : ExportViewModelBase
    {
        private readonly IFileQueryService _fileQueryService;
        private readonly Timer _debounceTimer;
        private string _searchFilter = string.Empty;
        private readonly ObservableCollection<SelectableFile> _allSelectableFiles = new();
        public ObservableCollection<SelectableFile> FilteredSelectableFiles { get; } = new();
        private int _totalFilesCount;
        private int _availableFilesCount;
        private int _selectedFilesCount;
        private bool? _areAllFilesSelected;
        private bool? _areAllExtensionsSelected;
        private string _outputFileName = string.Empty;
        private bool _appendTimestamp;

        private List<string> _loadedSelectedExtensions = new();
        private List<string> _loadedSelectedFilePaths = new();

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
                }
            }
        }

        public bool? AreAllFilesSelected
        {
            get => _areAllFilesSelected;
            set
            {
                bool selectAll = _areAllFilesSelected != true;
                SetFilteredFilesSelectionState(selectAll);
            }
        }

        public bool? AreAllExtensionsSelected
        {
            get => _areAllExtensionsSelected;
            set
            {
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

        public ObservableCollection<SelectableItemViewModel> AvailableExtensions { get; } = new ObservableCollection<SelectableItemViewModel>();
        public ObservableCollection<FolderViewModel> RootFolders => FolderTreeManager.RootFolders;

        public ICommand ExportFileContentCommand { get; }

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

            ExportFileContentCommand = new RelayCommand(async (_) => await ExportFileContentAsync(), (_) => CanExecuteOperation(this.OutputFileName) && _allSelectableFiles.Any(f => f.IsSelected));

            OnFolderSelectionChanged();
            UpdateFileCounts();
            UpdateExtensionMasterState();
        }

        public void ApplySettings(FileContentExportSettings settings)
        {
            OutputFileName = settings.OutputFileName;
            AppendTimestamp = settings.AppendTimestamp;
            _loadedSelectedExtensions = settings.SelectedExtensions ?? new List<string>();
            _loadedSelectedFilePaths = settings.SelectedFilePaths ?? new List<string>();
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

            SelectionHelper.SetSelectionState(FilteredSelectableFiles, isSelected);

            foreach (var file in FilteredSelectableFiles)
                file.PropertyChanged += OnFileSelectionChanged;

            UpdateFileCounts();
        }

        private void SetExtensionsSelectionState(bool isSelected)
        {
            foreach (var ext in AvailableExtensions)
                ext.PropertyChanged -= OnExtensionSelectionChanged;

            SelectionHelper.SetSelectionState(AvailableExtensions, isSelected);

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
                var filtered = string.IsNullOrWhiteSpace(SearchFilter)
                    ? _allSelectableFiles
                    : _allSelectableFiles.Where(f => f.FullPath.IndexOf(SearchFilter, StringComparison.OrdinalIgnoreCase) >= 0);
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
                    bool isSelected = previouslySelected.Contains(ext) || _loadedSelectedExtensions.Contains(ext);
                    var item = new SelectableItemViewModel(ext, isSelected);
                    item.PropertyChanged += OnExtensionSelectionChanged;
                    AvailableExtensions.Add(item);
                }
                _loadedSelectedExtensions.Clear();
                UpdateExtensionMasterState();
            });
        }

        private void UpdateSelectableFiles()
        {
            var selectedFolders = GetSelectedFoldersForFileListing();
            var selectedExtensions = new HashSet<string>(GetSelectedExtensions(), StringComparer.OrdinalIgnoreCase);
            var files = _fileQueryService.GetFilesByExtensions(selectedFolders, selectedExtensions);

            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                foreach (var file in _allSelectableFiles)
                    file.PropertyChanged -= OnFileSelectionChanged;
                _allSelectableFiles.Clear();

                foreach (var file in files.OrderBy(f => f.FullPath))
                {
                    if (_loadedSelectedFilePaths.Contains(file.FullPath))
                    {
                        file.IsSelected = true;
                    }
                    file.PropertyChanged += OnFileSelectionChanged;
                    _allSelectableFiles.Add(file);
                }

                _loadedSelectedFilePaths.Clear();

                ApplyFileFilter();
            });
        }

        private void OnFileSelectionChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(SelectableFile.IsSelected))
                UpdateFileCounts();
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
                var selectedPaths = _allSelectableFiles.Where(f => f.IsSelected).Select(f => f.FullPath).ToList();
                if (!selectedPaths.Any())
                {
                    _dialogService.ShowNotification("Information", "No files were selected. Please select at least one file to export.");
                    return;
                }
                string finalFileName = GetFinalFileName(OutputFileName, AppendTimestamp);
                string outputFilePath = await _fileOperationService.ExportSelectedFileContentsAsync(_sharedStateService.DestinationPath, selectedPaths, finalFileName);
                _dialogService.ShowNotification("Success", $"File contents exported successfully!\n\n{selectedPaths.Count} files were processed.\nSaved to: {outputFilePath}");
            }
            catch (Exception ex)
            {
                _dialogService.ShowNotification("Error", $"An unexpected error occurred during export:\n\n{ex.Message}");
            }
            finally
            {
                IsBusy = false;
            }
        }
    }
}
