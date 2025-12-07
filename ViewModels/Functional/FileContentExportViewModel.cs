using FileCraft.Models;
using FileCraft.Services.Interfaces;
using FileCraft.Shared.Commands;
using FileCraft.Shared.Helpers;
using FileCraft.ViewModels.Shared;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Text;
using System.Windows.Input;

namespace FileCraft.ViewModels.Functional
{
    public enum ExportFullscreenState
    {
        None,
        Folders,
        Files,
        Extensions
    }

    public class FileContentExportViewModel : ExportViewModelBase
    {
        private readonly IFileQueryService _fileQueryService;
        private readonly Timer _debounceTimer;
        private string _searchFilter = string.Empty;
        private bool _isShowingOnlySelected;
        private bool _isShowingOnlyUnselected;
        private readonly ObservableCollection<SelectableFile> _allSelectableFiles = new();
        public ObservableCollection<SelectableFile> FilteredSelectableFiles { get; } = new();
        private int _totalFilesCount;
        private int _availableFilesCount;
        private int _selectedFilesCount;
        private bool? _areAllFilesSelected;
        private bool? _areAllExtensionsSelected;
        private string _selectedExtensionsText = string.Empty;
        private string _ignoredFilesText = string.Empty;
        private HashSet<string> _ignoredCommentFilePaths = new(StringComparer.OrdinalIgnoreCase);

        public FullscreenManager<ExportFullscreenState> FullscreenManager { get; }

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
                    if (_isShowingOnlySelected)
                    {
                        IsShowingOnlyUnselected = false;
                    }
                    ApplyFileFilter();
                    OnPropertyChanged(nameof(CanClearFilter));
                }
            }
        }

        public bool IsShowingOnlyUnselected
        {
            get => _isShowingOnlyUnselected;
            set
            {
                if (_isShowingOnlyUnselected != value)
                {
                    _isShowingOnlyUnselected = value;
                    OnPropertyChanged();
                    if (_isShowingOnlyUnselected)
                    {
                        IsShowingOnlySelected = false;
                    }
                    ApplyFileFilter();
                    OnPropertyChanged(nameof(CanClearFilter));
                }
            }
        }

        public bool CanClearFilter => IsShowingOnlySelected || IsShowingOnlyUnselected || !string.IsNullOrWhiteSpace(SearchFilter);

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

        public string SelectedExtensionsText
        {
            get => _selectedExtensionsText;
            set
            {
                if (_selectedExtensionsText != value)
                {
                    _selectedExtensionsText = value;
                    OnPropertyChanged();
                }
            }
        }

        public string IgnoredFilesText
        {
            get => _ignoredFilesText;
            set
            {
                if (_ignoredFilesText != value)
                {
                    _ignoredFilesText = value;
                    OnPropertyChanged();
                }
            }
        }

        public ObservableCollection<SelectableItemViewModel> AvailableExtensions { get; } = new ObservableCollection<SelectableItemViewModel>();

        public ICommand ExportFileContentCommand { get; }
        public ICommand ClearFilterCommand { get; }
        public ICommand BulkSearchCommand { get; }
        public ICommand ConfigureIgnoredCommentsCommand { get; }

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

            FullscreenManager = new FullscreenManager<ExportFullscreenState>(ExportFullscreenState.None);
            FullscreenManager.PropertyChanged += OnFullscreenStateChanged;

            FolderTreeManager.FolderSelectionChanged += OnFolderSelectionChanged;
            FolderTreeManager.StateChanging += OnStateChanging;

            ExportFileContentCommand = new RelayCommand(async (_) => await ExportFileContentAsync(), (_) => CanExecuteOperation(this.OutputFileName) && _allSelectableFiles.Any(f => f.IsSelected));
            ClearFilterCommand = new RelayCommand(_ => ClearFilter());
            BulkSearchCommand = new RelayCommand(_ => BulkSearch(), _ => _allSelectableFiles.Any());
            ConfigureIgnoredCommentsCommand = new RelayCommand(_ => ConfigureIgnoredComments(), _ => _allSelectableFiles.Any(f => f.IsSelected));

            _selectedExtensionsText = ResourceHelper.GetString("FileContent_NoExtensionsSelected");
            _ignoredFilesText = ResourceHelper.GetString("FileContent_NoIgnoredFiles");

            OnFolderSelectionChanged();
            UpdateFileCounts();
            UpdateExtensionMasterState();
        }

        private void OnFullscreenStateChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(FullscreenManager.CurrentState))
            {
                UpdateIgnoredFilesText();
            }
        }

        private void ClearFilter()
        {
            SearchFilter = string.Empty;
            IsShowingOnlySelected = false;
            IsShowingOnlyUnselected = false;
        }

        private void BulkSearch()
        {
            bool confirmed = _dialogService.ShowBulkSearchDialog(_allSelectableFiles);
            if (confirmed)
            {
                OnStateChanging();
                ApplyFileFilter();
            }
        }

        private void ConfigureIgnoredComments()
        {
            var selectedFiles = _allSelectableFiles.Where(f => f.IsSelected).ToList();
            if (!selectedFiles.Any())
            {
                _dialogService.ShowNotification(
                    ResourceHelper.GetString("Common_InfoTitle"),
                    ResourceHelper.GetString("FileContent_SelectFilesFirst"),
                    DialogIconType.Info);
                return;
            }

            var result = _dialogService.ShowIgnoredCommentsDialog(selectedFiles, _ignoredCommentFilePaths);
            if (result != null)
            {
                OnStateChanging();
                _ignoredCommentFilePaths = new HashSet<string>(result, StringComparer.OrdinalIgnoreCase);
                UpdateIgnoredFilesText();
            }
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

            _ignoredCommentFilePaths = new HashSet<string>(settings.IgnoredCommentFilePaths ?? new List<string>(), StringComparer.OrdinalIgnoreCase);

            UpdateFileCounts();
            UpdateExtensionMasterState();
            ApplyFileFilter();
            UpdateIgnoredFilesText();
        }

        public List<string> GetSelectedFilePaths()
        {
            return _allSelectableFiles.Where(f => f.IsSelected).Select(f => f.FullPath).ToList();
        }

        public List<string> GetIgnoredCommentFilePaths()
        {
            return _ignoredCommentFilePaths.ToList();
        }

        public List<SelectableFile> GetSelectedFiles()
        {
            return _allSelectableFiles.Where(f => f.IsSelected).ToList();
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
            Application.Current.Dispatcher.Invoke(() =>
            {
                FilteredSelectableFiles.Clear();
                IEnumerable<SelectableFile> filtered = _allSelectableFiles;

                if (IsShowingOnlySelected)
                {
                    filtered = filtered.Where(f => f.IsSelected);
                }
                else if (IsShowingOnlyUnselected)
                {
                    filtered = filtered.Where(f => !f.IsSelected);
                }

                if (!string.IsNullOrWhiteSpace(SearchFilter))
                {
                    string normalizedFilter = SearchFilter.Replace('/', '\\');
                    filtered = filtered.Where(f => f.FullPath.IndexOf(normalizedFilter, StringComparison.OrdinalIgnoreCase) >= 0);
                }

                foreach (var file in filtered)
                    FilteredSelectableFiles.Add(file);
                UpdateFileCounts();
            });
        }

        protected override void OnFolderTreeManagerPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            base.OnFolderTreeManagerPropertyChanged(sender, e);
            if (e.PropertyName == nameof(FolderTreeManager.RootFolders))
            {
                OnFolderSelectionChanged();
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

            Application.Current.Dispatcher.Invoke(() =>
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

            Application.Current.Dispatcher.Invoke(() =>
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
                if (IsShowingOnlySelected || IsShowingOnlyUnselected)
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
            UpdateSelectedExtensionsText();
        }

        private void UpdateSelectedExtensionsText()
        {
            var selected = AvailableExtensions.Where(e => e.IsSelected).Select(e => e.Name).ToList();
            if (selected.Count == 0)
            {
                SelectedExtensionsText = ResourceHelper.GetString("FileContent_NoExtensionsSelected");
            }
            else
            {
                SelectedExtensionsText = string.Join(", ", selected);
            }
        }

        private void UpdateIgnoredFilesText()
        {
            if (_ignoredCommentFilePaths.Count == 0)
            {
                IgnoredFilesText = ResourceHelper.GetString("FileContent_NoIgnoredFiles");
            }
            else
            {
                var sb = new StringBuilder();
                bool isFullscreen = FullscreenManager.CurrentState == ExportFullscreenState.Extensions;

                foreach (var path in _ignoredCommentFilePaths.OrderBy(p => p))
                {
                    sb.AppendLine(isFullscreen ? path : Path.GetFileName(path));
                }
                IgnoredFilesText = sb.ToString().TrimEnd();
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
                    _dialogService.ShowNotification(
                        ResourceHelper.GetString("Common_InfoTitle"),
                        ResourceHelper.GetString("FileContent_NoFilesToExport"),
                        DialogIconType.Info);
                    return;
                }

                string messageFormat = ResourceHelper.GetString("FileContent_ConfirmExportMessage");
                string message = $"{messageFormat}\n{_sharedStateService.DestinationPath}";

                bool confirmed = _dialogService.ShowConfirmation(
                    title: ResourceHelper.GetString("FileContent_ExportTitle"),
                    message: message,
                    iconType: DialogIconType.Info,
                    filesAffected: selectedFiles.Count);

                if (!confirmed)
                {
                    return;
                }

                string finalFileName = GetFinalFileName(OutputFileName, AppendTimestamp);

                var (outputFilePath, xmlLines, xmlChars) = await _fileOperationService.ExportSelectedFileContentsAsync(
                    _sharedStateService.DestinationPath, selectedFiles, finalFileName, _ignoredCommentFilePaths);

                var notificationMessage = new StringBuilder();
                string successMsg = string.Format(ResourceHelper.GetString("FileContent_ExportSuccessMessage"), selectedFiles.Count);
                notificationMessage.AppendLine(successMsg);

                string savedToMsg = string.Format(ResourceHelper.GetString("Common_SavedTo"), outputFilePath);
                notificationMessage.AppendLine(savedToMsg);

                if (xmlLines > 0)
                {
                    notificationMessage.AppendLine();
                    notificationMessage.AppendLine(ResourceHelper.GetString("FileContent_IgnoredParts"));
                    string stats = string.Format(ResourceHelper.GetString("FileContent_IgnoredStats"), xmlLines, xmlChars);
                    notificationMessage.AppendLine(stats);
                }

                _dialogService.ShowNotification(
                    ResourceHelper.GetString("Common_SuccessTitle"),
                    notificationMessage.ToString(),
                    DialogIconType.Success);
            }
            catch (Exception ex)
            {
                string errorMsg = string.Format(ResourceHelper.GetString("FileContent_ExportErrorMessage"), ex.Message);
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