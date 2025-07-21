using FileCraft.Models;
using FileCraft.Services;
using FileCraft.Services.Interfaces;
using FileCraft.Shared.Commands;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace FileCraft.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private string _sourcePath = string.Empty;
        private string _destinationPath = string.Empty;
        private bool _isBusy;
        private readonly IFileOperationService _fileOperationService;
        private readonly IDialogService _dialogService;
        private readonly ISettingsService _settingsService;

        public event PropertyChangedEventHandler? PropertyChanged;

        public string SourcePath
        {
            get => _sourcePath;
            set
            {
                if (_sourcePath != value)
                {
                    _sourcePath = value;
                    OnPropertyChanged();
                    OnSourcePathChanged();
                    SaveSettings();
                }
            }
        }

        public string DestinationPath
        {
            get => _destinationPath;
            set
            {
                if (_destinationPath != value)
                {
                    _destinationPath = value;
                    OnPropertyChanged();
                    SaveSettings();
                }
            }
        }

        public bool IsBusy
        {
            get => _isBusy;
            set { _isBusy = value; OnPropertyChanged(); }
        }

        public ObservableCollection<SelectableFile> SelectableFiles { get; } = new ObservableCollection<SelectableFile>();
        public ObservableCollection<SelectableItemViewModel> AvailableExtensions { get; } = new ObservableCollection<SelectableItemViewModel>();
        public ObservableCollection<FolderViewModel> RootFolders { get; } = new ObservableCollection<FolderViewModel>();

        public ICommand ClearPathsCommand { get; }
        public ICommand ExportFileContentCommand { get; }
        public ICommand SelectAllFilesCommand { get; }
        public ICommand DeselectAllFilesCommand { get; }
        public ICommand SelectAllFoldersCommand { get; }
        public ICommand DeselectAllFoldersCommand { get; }
        public ICommand SelectAllExtensionsCommand { get; }
        public ICommand DeselectAllExtensionsCommand { get; }

        public ICommand ExportFolderContentsCommand { get; }
        public ICommand GenerateTreeStructureCommand { get; }


        public MainViewModel(IFileOperationService fileOperationService, IDialogService dialogService)
        {
            _fileOperationService = fileOperationService;
            _dialogService = dialogService;
            _settingsService = new SettingsService();

            ClearPathsCommand = new RelayCommand(_ => ClearPaths(), _ => !string.IsNullOrEmpty(SourcePath) || !string.IsNullOrEmpty(DestinationPath));
            ExportFileContentCommand = new RelayCommand(async (_) => await ExportFileContentAsync(), (_) => CanExecuteOperation(_) && SelectableFiles.Any(f => f.IsSelected));

            SelectAllFilesCommand = new RelayCommand(SelectAllFiles, (_) => SelectableFiles.Any());
            DeselectAllFilesCommand = new RelayCommand(DeselectAllFiles, (_) => SelectableFiles.Any());

            SelectAllFoldersCommand = new RelayCommand(SelectAllFolders, _ => RootFolders.Any());
            DeselectAllFoldersCommand = new RelayCommand(DeselectAllFolders, _ => RootFolders.Any());

            SelectAllExtensionsCommand = new RelayCommand(SelectAllExtensions, _ => AvailableExtensions.Any());
            DeselectAllExtensionsCommand = new RelayCommand(DeselectAllExtensions, _ => AvailableExtensions.Any());

            ExportFolderContentsCommand = new RelayCommand(async (_) => await ExportFolderContents(), CanExecuteOperation);
            GenerateTreeStructureCommand = new RelayCommand(async (_) => await GenerateTreeStructure(), CanExecuteOperation);

            LoadSettings();
        }

        private void ClearPaths()
        {
            SourcePath = string.Empty;
            DestinationPath = string.Empty;
        }

        private void LoadSettings()
        {
            Settings settings = _settingsService.LoadSettings();
            _sourcePath = settings.SourcePath;
            _destinationPath = settings.DestinationPath;
            OnPropertyChanged(nameof(SourcePath));
            OnPropertyChanged(nameof(DestinationPath));
            OnSourcePathChanged();
        }

        private void SaveSettings()
        {
            var settings = new Settings { SourcePath = this.SourcePath, DestinationPath = this.DestinationPath };
            _settingsService.SaveSettings(settings);
        }

        private bool CanExecuteOperation(object? parameter)
        {
            return !string.IsNullOrWhiteSpace(SourcePath) && !string.IsNullOrWhiteSpace(DestinationPath) && !IsBusy;
        }

        private void OnSourcePathChanged()
        {
            if (string.IsNullOrWhiteSpace(SourcePath) || !Directory.Exists(SourcePath))
            {
                RootFolders.Clear();
                AvailableExtensions.Clear();
                SelectableFiles.Clear();
                return;
            }

            IsBusy = true;
            try
            {
                BuildFolderTree();
                OnFolderSelectionChanged();
            }
            catch (Exception ex)
            {
                _dialogService.ShowNotification("Error", $"Could not access or process the source directory:\n\n{ex.Message}");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private void OnFolderSelectionChanged()
        {
            UpdateAvailableExtensions();
            UpdateSelectableFiles();
        }


        private void BuildFolderTree()
        {
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                RootFolders.Clear();
                var rootDirInfo = new DirectoryInfo(SourcePath);
                var rootViewModel = new FolderViewModel(rootDirInfo.Name, rootDirInfo.FullName, null, OnFolderSelectionChanged);
                PopulateChildren(rootViewModel);
                RootFolders.Add(rootViewModel);
            });
        }

        private void PopulateChildren(FolderViewModel parent)
        {
            try
            {
                var subDirs = Directory.GetDirectories(parent.FullPath);
                foreach (var dirPath in subDirs)
                {
                    var dirInfo = new DirectoryInfo(dirPath);
                    var childViewModel = new FolderViewModel(dirInfo.Name, dirInfo.FullName, parent, OnFolderSelectionChanged);
                    parent.Children.Add(childViewModel);
                    PopulateChildren(childViewModel);
                }
            }
            catch (UnauthorizedAccessException) { }
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
                    var item = new SelectableItemViewModel(ext, previouslySelected.Contains(ext) || previouslySelected.Count == 0);
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
            if (!RootFolders.Any())
            {
                return new List<FolderViewModel>();
            }
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
                    IsBusy = false;
                    return;
                }

                string outputFilePath = await _fileOperationService.ExportSelectedFileContentsAsync(DestinationPath, selectedPaths);
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

        #region Selection Commands
        private void SelectAllFiles(object? parameter) => SetAllFilesSelection(true);
        private void DeselectAllFiles(object? parameter) => SetAllFilesSelection(false);
        private void SetAllFilesSelection(bool isSelected)
        {
            foreach (var file in SelectableFiles) { file.IsSelected = isSelected; }
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

        private void SelectAllExtensions(object? parameter) => SetAllExtensionsSelection(true);
        private void DeselectAllExtensions(object? parameter) => SetAllExtensionsSelection(false);
        private void SetAllExtensionsSelection(bool isSelected)
        {
            foreach (var ext in AvailableExtensions) { ext.IsSelected = isSelected; }
        }
        #endregion

        #region Feature Methods

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

                string outputFilePath = await _fileOperationService.ExportFolderContentsAsync(DestinationPath, includedFolderPaths);
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

        private async Task GenerateTreeStructure()
        {
            IsBusy = true;
            try
            {
                var allNodes = RootFolders.Any() ? RootFolders[0].GetAllNodes() : Enumerable.Empty<FolderViewModel>();

                var excludedFolderPaths = new HashSet<string>(
                    allNodes.Where(n => n.IsSelected == false).Select(n => n.FullPath),
                    StringComparer.OrdinalIgnoreCase);

                string outputFilePath = await _fileOperationService.GenerateTreeStructureAsync(SourcePath, DestinationPath, excludedFolderPaths);
                _dialogService.ShowNotification("Success", $"Tree structure file was created successfully!\n\nSaved to: {outputFilePath}");
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
        #endregion

        protected void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
