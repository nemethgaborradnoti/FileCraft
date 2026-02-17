using FileCraft.Models;
using FileCraft.Services.Interfaces;
using FileCraft.Shared.Commands;
using FileCraft.Shared.Helpers;
using FileCraft.ViewModels.Shared;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
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
        private readonly Debouncer _debouncer;
        private CancellationTokenSource? _filterCts;

        private string _searchFilter = string.Empty;
        private bool _isShowingOnlySelected;
        private bool _isShowingOnlyUnselected;

        private List<SelectableFile> _allSelectableFiles = new();

        public RangeObservableCollection<SelectableFile> FilteredSelectableFiles { get; } = new();

        private int _totalFilesCount;
        private int _availableFilesCount;
        private int _selectedFilesCount;
        private bool? _areAllFilesSelected;
        private bool? _areAllExtensionsSelected;
        private string _selectedExtensionsText = string.Empty;
        private string _ignoredFilesText = string.Empty;
        private HashSet<string> _ignoredCommentFilePaths = new(StringComparer.OrdinalIgnoreCase);

        private FileContentExportSettings? _pendingSettings;

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
                    _debouncer.Debounce();
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

        public RangeObservableCollection<SelectableItemViewModel> AvailableExtensions { get; } = new();

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
            _debouncer = new Debouncer(ApplyFileFilter);

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
            _pendingSettings = settings;

            OutputFileName = settings.OutputFileName;
            AppendTimestamp = settings.AppendTimestamp;

            OnFolderSelectionChanged();
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

        private void ApplyFileFilter()
        {
            _filterCts?.Cancel();
            _filterCts = new CancellationTokenSource();
            var token = _filterCts.Token;

            var currentSearchFilter = SearchFilter;
            var onlySelected = IsShowingOnlySelected;
            var onlyUnselected = IsShowingOnlyUnselected;
            var sourceFiles = _allSelectableFiles.ToList();

            Task.Run(() =>
            {
                if (token.IsCancellationRequested) return;

                IEnumerable<SelectableFile> filtered = sourceFiles;

                if (onlySelected)
                {
                    filtered = filtered.Where(f => f.IsSelected);
                }
                else if (onlyUnselected)
                {
                    filtered = filtered.Where(f => !f.IsSelected);
                }

                if (!string.IsNullOrWhiteSpace(currentSearchFilter))
                {
                    var searchTerms = currentSearchFilter.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                    foreach (var term in searchTerms)
                    {
                        if (term.StartsWith("-") && term.Length > 1)
                        {
                            string excludeTerm = term.Substring(1).Replace('/', '\\');
                            filtered = filtered.Where(f => f.FullPath.IndexOf(excludeTerm, StringComparison.OrdinalIgnoreCase) < 0);
                        }
                        else
                        {
                            string includeTerm = term.Replace('/', '\\');
                            filtered = filtered.Where(f => f.FullPath.IndexOf(includeTerm, StringComparison.OrdinalIgnoreCase) >= 0);
                        }
                    }
                }

                var resultList = filtered.ToList();

                if (token.IsCancellationRequested) return;

                Application.Current.Dispatcher.Invoke(() =>
                {
                    if (token.IsCancellationRequested) return;

                    FilteredSelectableFiles.ReplaceAll(resultList);
                    UpdateFileCounts();
                });
            }, token);
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
        }

        private void UpdateAvailableExtensions()
        {
            var selectedFolderConfigs = GetSelectedFolderConfigs();
            var ignoredFolders = new HashSet<string>(_sharedStateService.IgnoredFolders, StringComparer.OrdinalIgnoreCase);

            IsBusy = true;
            Task.Run(() =>
            {
                var extensions = _fileQueryService.GetAvailableExtensions(selectedFolderConfigs, ignoredFolders);

                Application.Current.Dispatcher.Invoke(() =>
                {
                    HashSet<string> extensionsToSelect;

                    if (_pendingSettings != null)
                    {
                        extensionsToSelect = new HashSet<string>(_pendingSettings.SelectedExtensions ?? new List<string>(), StringComparer.OrdinalIgnoreCase);
                    }
                    else
                    {
                        extensionsToSelect = new HashSet<string>(GetSelectedExtensions(), StringComparer.OrdinalIgnoreCase);
                    }

                    foreach (var ext in AvailableExtensions)
                        ext.PropertyChanged -= OnExtensionSelectionChanged;

                    var newExtensionsList = new List<SelectableItemViewModel>();
                    foreach (var ext in extensions.Where(e => !string.IsNullOrEmpty(e)).OrderBy(e => e))
                    {
                        var item = new SelectableItemViewModel(ext, extensionsToSelect.Contains(ext), this.OnStateChanging);
                        item.PropertyChanged += OnExtensionSelectionChanged;
                        newExtensionsList.Add(item);
                    }

                    AvailableExtensions.ReplaceAll(newExtensionsList);
                    UpdateExtensionMasterState();
                    UpdateSelectableFiles();
                });
            });
        }

        private void UpdateSelectableFiles()
        {
            var selectedFolderConfigs = GetSelectedFolderConfigs();
            var selectedExtensions = new HashSet<string>(GetSelectedExtensions(), StringComparer.OrdinalIgnoreCase);
            var ignoredFolders = new HashSet<string>(_sharedStateService.IgnoredFolders, StringComparer.OrdinalIgnoreCase);
            var basePath = _sharedStateService.SourcePath;

            IsBusy = true;
            Task.Run(() =>
            {
                var files = _fileQueryService.GetFilesByExtensions(basePath, selectedFolderConfigs, selectedExtensions, ignoredFolders);

                var newFileList = new List<SelectableFile>();
                foreach (var file in files)
                {
                    newFileList.Add(file);
                }

                Application.Current.Dispatcher.Invoke(() =>
                {
                    HashSet<string>? pendingFilesToSelect = null;

                    if (_pendingSettings != null)
                    {
                        pendingFilesToSelect = new HashSet<string>(_pendingSettings.SelectedFilePaths ?? new List<string>(), StringComparer.OrdinalIgnoreCase);

                        _ignoredCommentFilePaths = new HashSet<string>(_pendingSettings.IgnoredCommentFilePaths ?? new List<string>(), StringComparer.OrdinalIgnoreCase);
                        UpdateIgnoredFilesText();
                    }

                    var previouslySelectedFiles = new HashSet<string>(_allSelectableFiles.Where(f => f.IsSelected).Select(f => f.FullPath));

                    foreach (var file in _allSelectableFiles)
                        file.PropertyChanged -= OnFileSelectionChanged;

                    _allSelectableFiles.Clear();

                    foreach (var file in newFileList.OrderBy(f => f.FullPath))
                    {
                        bool isSelected = false;

                        if (pendingFilesToSelect != null)
                        {
                            isSelected = pendingFilesToSelect.Contains(file.FullPath);
                        }
                        else
                        {
                            isSelected = previouslySelectedFiles.Contains(file.FullPath);
                        }

                        if (isSelected)
                        {
                            file.IsSelected = true;
                        }

                        file.SetStateChangingAction(this.OnStateChanging);
                        file.PropertyChanged += OnFileSelectionChanged;
                        _allSelectableFiles.Add(file);
                    }

                    _pendingSettings = null;

                    ApplyFileFilter();
                    IsBusy = false;
                });
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

        private List<(string Path, bool Recursive)> GetSelectedFolderConfigs()
        {
            var configs = new List<(string Path, bool Recursive)>();
            if (RootFolders.Any())
            {
                CollectFolderConfigs(RootFolders[0], configs);
            }
            return configs;
        }

        private void CollectFolderConfigs(FolderViewModel node, List<(string Path, bool Recursive)> configs)
        {
            if (node.IsSelected == true)
            {
                configs.Add((node.FullPath, true));
            }
            else if (node.IsSelected == null)
            {
                configs.Add((node.FullPath, false));

                foreach (var child in node.Children)
                {
                    CollectFolderConfigs(child, configs);
                }
            }
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

                var result = await _fileOperationService.ExportSelectedFileContentsAsync(
                    _sharedStateService.DestinationPath, selectedFiles, finalFileName, _ignoredCommentFilePaths);

                long totalChars = result.ExportedCharacters + result.IgnoredCharacters;
                double savingsPercent = totalChars > 0 ? (double)result.IgnoredCharacters / totalChars * 100.0 : 0;

                var nfi = new NumberFormatInfo { NumberGroupSeparator = " ", NumberDecimalDigits = 0 };

                var notificationMessage = new StringBuilder();

                notificationMessage.AppendLine(string.Format(ResourceHelper.GetString("FileContent_Result_Header"), selectedFiles.Count));
                notificationMessage.AppendLine(string.Format(ResourceHelper.GetString("FileContent_Result_Path"), result.OutputFilePath));
                notificationMessage.AppendLine();
                notificationMessage.AppendLine(string.Format(ResourceHelper.GetString("FileContent_Result_Chars"), result.ExportedCharacters.ToString("N0", nfi)));
                notificationMessage.AppendLine(string.Format(ResourceHelper.GetString("FileContent_Result_Lines"), result.ExportedLines.ToString("N0", nfi)));
                notificationMessage.AppendLine();
                notificationMessage.AppendLine(string.Format(ResourceHelper.GetString("FileContent_Result_IgnoredHeader"), result.FilesWithIgnoredCommentsCount));
                notificationMessage.AppendLine(string.Format(ResourceHelper.GetString("FileContent_Result_IgnoredChars"), result.IgnoredCharacters.ToString("N0", nfi)));
                notificationMessage.AppendLine(string.Format(ResourceHelper.GetString("FileContent_Result_IgnoredLines"), result.IgnoredLines.ToString("N0", nfi)));
                notificationMessage.AppendLine();
                notificationMessage.AppendLine(string.Format(ResourceHelper.GetString("FileContent_Result_Savings"), savingsPercent.ToString("0.##", CultureInfo.InvariantCulture)));

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