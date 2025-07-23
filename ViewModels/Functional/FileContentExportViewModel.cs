using FileCraft.Models;
using FileCraft.Services;
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
    public class FileContentExportViewModel : BaseViewModel
    {
        private readonly MainViewModel _mainViewModel;
        private readonly IFileOperationService _fileOperationService;
        private readonly IDialogService _dialogService;

        public FolderTreeManager FolderTreeManager { get; }

        public ObservableCollection<SelectableFile> SelectableFiles { get; } = new ObservableCollection<SelectableFile>();
        public ObservableCollection<SelectableItemViewModel> AvailableExtensions { get; } = new ObservableCollection<SelectableItemViewModel>();

        public ObservableCollection<FolderViewModel> RootFolders => FolderTreeManager.RootFolders;

        public ICommand ExportFileContentCommand { get; }
        public ICommand SelectAllFilesCommand { get; }
        public ICommand DeselectAllFilesCommand { get; }
        public ICommand SelectAllFoldersCommand { get; }
        public ICommand DeselectAllFoldersCommand { get; }
        public ICommand SelectAllExtensionsCommand { get; }
        public ICommand DeselectAllExtensionsCommand { get; }

        public FileContentExportViewModel(MainViewModel mainViewModel, IFileOperationService fileOperationService, IDialogService dialogService, FolderTreeManager folderTreeManager)
        {
            _mainViewModel = mainViewModel;
            _fileOperationService = fileOperationService;
            _dialogService = dialogService;
            FolderTreeManager = folderTreeManager;

            FolderTreeManager.PropertyChanged += OnFolderTreeManagerPropertyChanged;

            ExportFileContentCommand = new RelayCommand(async (_) => await ExportFileContentAsync(), (_) => CanExecuteOperation() && SelectableFiles.Any(f => f.IsSelected));
            SelectAllFilesCommand = new RelayCommand(_ => SelectionHelper.SetSelectionState(SelectableFiles, true), _ => SelectableFiles.Any());
            DeselectAllFilesCommand = new RelayCommand(_ => SelectionHelper.SetSelectionState(SelectableFiles, false), _ => SelectableFiles.Any());
            SelectAllFoldersCommand = new RelayCommand(SelectAllFolders, _ => RootFolders.Any());
            DeselectAllFoldersCommand = new RelayCommand(DeselectAllFolders, _ => RootFolders.Any());
            SelectAllExtensionsCommand = new RelayCommand(_ => SelectionHelper.SetSelectionState(AvailableExtensions, true), _ => AvailableExtensions.Any());
            DeselectAllExtensionsCommand = new RelayCommand(_ => SelectionHelper.SetSelectionState(AvailableExtensions, false), _ => AvailableExtensions.Any());

            OnFolderSelectionChanged();
        }

        private void OnFolderTreeManagerPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(FolderTreeManager.RootFolders))
            {
                OnFolderSelectionChanged();
                OnPropertyChanged(nameof(RootFolders));
            }
        }

        private bool CanExecuteOperation()
        {
            return !string.IsNullOrWhiteSpace(_mainViewModel.SourcePath) &&
                   !string.IsNullOrWhiteSpace(_mainViewModel.DestinationPath) &&
                   !IsBusy;
        }

        private void OnFolderSelectionChanged()
        {
            UpdateAvailableExtensions();
            UpdateSelectableFiles();
        }
        private void UpdateAvailableExtensions()
        {
            var selectedFolders = GetSelectedFoldersForFileListing();
            var extensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var folder in selectedFolders)
            {
                try
                {
                    var files = Directory.GetFiles(folder.FullPath, "*.*", SearchOption.TopDirectoryOnly);
                    foreach (var file in files)
                    {
                        extensions.Add(Path.GetExtension(file));
                    }
                }
                catch (UnauthorizedAccessException) { }
            }

            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                var previouslySelected = new HashSet<string>(AvailableExtensions.Where(e => e.IsSelected).Select(e => e.Name));
                AvailableExtensions.Clear();
                foreach (var ext in extensions.Where(e => !string.IsNullOrEmpty(e)).OrderBy(e => e))
                {
                    var item = new SelectableItemViewModel(ext, previouslySelected.Contains(ext) || !previouslySelected.Any());
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

            var files = new List<SelectableFile>();
            foreach (var folder in selectedFolders)
            {
                try
                {
                    var filesInFolder = Directory.GetFiles(folder.FullPath, "*.*", SearchOption.TopDirectoryOnly)
                        .Where(f => selectedExtensions.Contains(Path.GetExtension(f)))
                        .Select(f => new SelectableFile { FileName = Path.GetFileName(f), FullPath = f, IsSelected = false });
                    files.AddRange(filesInFolder);
                }
                catch (UnauthorizedAccessException) { }
            }

            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                SelectableFiles.Clear();
                foreach (var file in files.OrderBy(f => f.FullPath))
                {
                    SelectableFiles.Add(file);
                }
            });
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

                string outputFilePath = await _fileOperationService.ExportSelectedFileContentsAsync(_mainViewModel.DestinationPath, selectedPaths);
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

        private void SelectAllFolders(object? parameter) => SetAllFoldersSelection(true);
        private void DeselectAllFolders(object? parameter) => SetAllFoldersSelection(false);
        private void SetAllFoldersSelection(bool isSelected)
        {
            if (!RootFolders.Any()) return;

            var root = RootFolders[0];
            if (isSelected)
            {
                root.IsSelected = true;
                root.SetIsExpandedRecursively(true);
            }
            else
            {
                foreach (var child in root.Children)
                {
                    child.IsSelected = false;
                }
            }
        }
    }
}
