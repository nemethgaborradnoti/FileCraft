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
                bool selectAll = (_areAllFilesSelected != true);
                if (selectAll)
                    SelectAllFilesCommand.Execute(null);
                else
                    DeselectAllFilesCommand.Execute(null);
            }
        }

        public bool? AreAllExtensionsSelected
        {
            get => _areAllExtensionsSelected;
            set
            {
                bool selectAll = (_areAllExtensionsSelected != true);
                if (selectAll)
                    SelectAllExtensionsCommand.Execute(null);
                else
                    DeselectAllExtensionsCommand.Execute(null);
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
        public ICommand SelectAllFilesCommand { get; }
        public ICommand DeselectAllFilesCommand { get; }
        public ICommand SelectAllExtensionsCommand { get; }
        public ICommand DeselectAllExtensionsCommand { get; }

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
            SelectAllFilesCommand = new RelayCommand(_ => SetFilteredFilesSelectionState(true), _ => FilteredSelectableFiles.Any());
            DeselectAllFilesCommand = new RelayCommand(_ => SetFilteredFilesSelectionState(false), _ => FilteredSelectableFiles.Any());
            SelectAllExtensionsCommand = new RelayCommand(_ => SetExtensionsSelectionState(true), _ => AvailableExtensions.Any());
            DeselectAllExtensionsCommand = new RelayCommand(_ => SetExtensionsSelectionState(false), _ => AvailableExtensions.Any());

            OnFolderSelectionChanged();
            UpdateFileCounts();
            UpdateExtensionCounts();
        }

        public void ApplySettings(FileContentExportSettings settings)
        {
            OutputFileName = settings.OutputFileName;
            AppendTimestamp = settings.AppendTimestamp;
            _loadedSelectedExtensions = settings.SelectedExtensions ?? new List<string>();
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
            UpdateExtensionCounts();
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
                UpdateExtensionCounts();
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
                    file.PropertyChanged += OnFileSelectionChanged;
                    _allSelectableFiles.Add(file);
                }
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
                UpdateExtensionCounts();
            }
        }

        private void UpdateFileCounts()
        {
            TotalFilesCount = _allSelectableFiles.Count;
            AvailableFilesCount = FilteredSelectableFiles.Count;
            SelectedFilesCount = _allSelectableFiles.Count(f => f.IsSelected);
            int selectedInFilter = FilteredSelectableFiles.Count(f => f.IsSelected);
            int totalInFilter = FilteredSelectableFiles.Count;
            bool? newSelectionState;
            if (totalInFilter == 0) newSelectionState = false;
            else if (selectedInFilter == 0) newSelectionState = false;
            else if (selectedInFilter == totalInFilter) newSelectionState = true;
            else newSelectionState = null;
            if (_areAllFilesSelected != newSelectionState)
            {
                _areAllFilesSelected = newSelectionState;
                OnPropertyChanged(nameof(AreAllFilesSelected));
            }
        }

        private void UpdateExtensionCounts()
        {
            int selectedCount = AvailableExtensions.Count(e => e.IsSelected);
            int totalCount = AvailableExtensions.Count;
            bool? newSelectionState;
            if (selectedCount == 0 && totalCount > 0) newSelectionState = false;
            else if (selectedCount == totalCount && totalCount > 0) newSelectionState = true;
            else newSelectionState = null;
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
