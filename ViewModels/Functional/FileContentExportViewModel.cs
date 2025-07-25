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
    public class FileContentExportViewModel : ExportViewModelBase
    {
        private readonly IFileQueryService _fileQueryService;
        private int _availableFilesCount;
        private int _selectedFilesCount;

        #region Properties
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

        public ObservableCollection<SelectableFile> SelectableFiles { get; } = new ObservableCollection<SelectableFile>();
        public ObservableCollection<SelectableItemViewModel> AvailableExtensions { get; } = new ObservableCollection<SelectableItemViewModel>();
        public ObservableCollection<FolderViewModel> RootFolders => FolderTreeManager.RootFolders;
        #endregion

        #region Commands
        public ICommand ExportFileContentCommand { get; }
        public ICommand SelectAllFilesCommand { get; }
        public ICommand DeselectAllFilesCommand { get; }
        public ICommand SelectAllExtensionsCommand { get; }
        public ICommand DeselectAllExtensionsCommand { get; }
        #endregion

        public FileContentExportViewModel(
            ISharedStateService sharedStateService,
            IFileOperationService fileOperationService,
            IDialogService dialogService,
            ISettingsService settingsService,
            FolderTreeManager folderTreeManager,
            IFileQueryService fileQueryService)
            : base(sharedStateService, fileOperationService, dialogService, settingsService, folderTreeManager)
        {
            _fileQueryService = fileQueryService;
            OutputFileName = "FileContentsExport";

            FolderTreeManager.PropertyChanged += OnFolderTreeManagerPropertyChanged;
            FolderTreeManager.FolderSelectionChanged += OnFolderSelectionChanged;

            ExportFileContentCommand = new RelayCommand(async (_) => await ExportFileContentAsync(), (_) => CanExecuteOperation() && SelectableFiles.Any(f => f.IsSelected));
            SelectAllFilesCommand = new RelayCommand(_ => SelectionHelper.SetSelectionState(SelectableFiles, true), _ => SelectableFiles.Any());
            DeselectAllFilesCommand = new RelayCommand(_ => SelectionHelper.SetSelectionState(SelectableFiles, false), _ => SelectableFiles.Any());
            SelectAllExtensionsCommand = new RelayCommand(_ => SelectionHelper.SetSelectionState(AvailableExtensions, true), _ => AvailableExtensions.Any());
            DeselectAllExtensionsCommand = new RelayCommand(_ => SelectionHelper.SetSelectionState(AvailableExtensions, false), _ => AvailableExtensions.Any());

            OnFolderSelectionChanged();
            UpdateCounts();
        }

        protected override bool CanExecuteOperation()
        {
            return base.CanExecuteOperation() && SelectableFiles.Any(f => f.IsSelected);
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

            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                var previouslySelected = new HashSet<string>(AvailableExtensions.Where(e => e.IsSelected).Select(e => e.Name));
                AvailableExtensions.Clear();
                foreach (var ext in extensions.Where(e => !string.IsNullOrEmpty(e)).OrderBy(e => e))
                {
                    var item = new SelectableItemViewModel(ext, previouslySelected.Contains(ext));
                    item.PropertyChanged += (s, e) => { if (e.PropertyName == nameof(SelectableItemViewModel.IsSelected)) UpdateSelectableFiles(); };
                    AvailableExtensions.Add(item);
                }
            });
        }

        private void UpdateSelectableFiles()
        {
            var selectedFolders = GetSelectedFoldersForFileListing();
            var selectedExtensions = new HashSet<string>(
                AvailableExtensions.Where(e => e.IsSelected).Select(e => e.Name),
                StringComparer.OrdinalIgnoreCase);

            var files = _fileQueryService.GetFilesByExtensions(selectedFolders, selectedExtensions);

            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                foreach (var file in SelectableFiles)
                {
                    file.PropertyChanged -= SelectableFile_PropertyChanged;
                }
                SelectableFiles.Clear();

                foreach (var file in files.OrderBy(f => f.FullPath))
                {
                    file.PropertyChanged += SelectableFile_PropertyChanged;
                    SelectableFiles.Add(file);
                }
                UpdateCounts();
            });
        }

        private void SelectableFile_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(SelectableFile.IsSelected))
            {
                UpdateCounts();
            }
        }

        private void UpdateCounts()
        {
            AvailableFilesCount = SelectableFiles.Count;
            SelectedFilesCount = SelectableFiles.Count(f => f.IsSelected);
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
                var selectedPaths = SelectableFiles.Where(f => f.IsSelected).Select(f => f.FullPath).ToList();
                if (!selectedPaths.Any())
                {
                    _dialogService.ShowNotification("Information", "No files were selected. Please select at least one file to export.");
                    return;
                }

                string finalFileName = GetFinalFileName();

                string outputFilePath = await _fileOperationService.ExportSelectedFileContentsAsync(_sharedStateService.DestinationPath, selectedPaths, finalFileName);
                _dialogService.ShowNotification("Success", $"File contents exported successfully!\n\n{selectedPaths.Count} files were processed.\nSaved to: {outputFilePath}");

                SaveSettings();
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