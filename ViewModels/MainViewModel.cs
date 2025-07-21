using FileCraft.Models;
using FileCraft.Services;
using FileCraft.Services.Interfaces;
using FileCraft.Shared.Commands;
using FileCraft.Shared.Validation;
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

        private string _fileExtensions = ".cs;.html;.css;.js;.txt";
        private bool _showLoadFilesButton;
        private bool _contentExport_IncludeSubfolders;
        private string _contentExport_ExcludeFoldersText = "obj;bin;.git;.vs;node_modules";

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
                    OnSourcePathOrOptionsChanged();
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

        public string FileExtensions
        {
            get => _fileExtensions;
            set
            {
                _fileExtensions = value;
                OnPropertyChanged();
                OnSourcePathOrOptionsChanged();
            }
        }

        public bool ShowLoadFilesButton
        {
            get => _showLoadFilesButton;
            set { _showLoadFilesButton = value; OnPropertyChanged(); }
        }

        public bool ContentExport_IncludeSubfolders
        {
            get => _contentExport_IncludeSubfolders;
            set
            {
                _contentExport_IncludeSubfolders = value;
                OnPropertyChanged();
                OnSourcePathOrOptionsChanged();
            }
        }

        public string ContentExport_ExcludeFoldersText
        {
            get => _contentExport_ExcludeFoldersText;
            set
            {
                _contentExport_ExcludeFoldersText = value;
                OnPropertyChanged();
                OnSourcePathOrOptionsChanged();
            }
        }

        public ICommand ClearPathsCommand { get; }
        public ICommand ExportFolderContentsCommand { get; }
        public ICommand GenerateTreeStructureCommand { get; }
        public ICommand LoadFilesCommand { get; }
        public ICommand ExportFileContentCommand { get; }
        public ICommand SelectAllFilesCommand { get; }
        public ICommand DeselectAllFilesCommand { get; }


        public MainViewModel(IFileOperationService fileOperationService, IDialogService dialogService)
        {
            _fileOperationService = fileOperationService;
            _dialogService = dialogService;
            _settingsService = new SettingsService();

            ClearPathsCommand = new RelayCommand(
                _ => ClearPaths(),
                _ => !string.IsNullOrEmpty(SourcePath) || !string.IsNullOrEmpty(DestinationPath)
            );

            ExportFolderContentsCommand = new RelayCommand(async (p) => await ExportFolderContents(p), CanExecuteOperation);
            GenerateTreeStructureCommand = new RelayCommand(async (p) => await GenerateTreeStructure(p), CanExecuteOperation);
            LoadFilesCommand = new RelayCommand(async (p) => await LoadFilesAsync(), (p) => !string.IsNullOrWhiteSpace(SourcePath) && !IsBusy);
            ExportFileContentCommand = new RelayCommand(async (p) => await ExportFileContentAsync(), (p) => CanExecuteOperation(p) && SelectableFiles.Any(f => f.IsSelected));
            SelectAllFilesCommand = new RelayCommand(SelectAllFiles, (p) => SelectableFiles.Any());
            DeselectAllFilesCommand = new RelayCommand(DeselectAllFiles, (p) => SelectableFiles.Any());

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
        }

        private void SaveSettings()
        {
            var settings = new Settings
            {
                SourcePath = this.SourcePath,
                DestinationPath = this.DestinationPath
            };
            _settingsService.SaveSettings(settings);
        }

        private bool CanExecuteOperation(object parameter)
        {
            return !string.IsNullOrWhiteSpace(SourcePath) &&
                   !string.IsNullOrWhiteSpace(DestinationPath) &&
                   !IsBusy;
        }

        private async Task ExportFolderContents(object parameter)
        {
            IsBusy = true;
            try
            {
                var options = parameter as Dictionary<string, object>;
                Guard.AgainstNull(options, nameof(options), "Internal application error: Command parameter is missing.");

                bool includeSubfolders = (bool)options["IncludeSubfolders"];

                string outputFilePath = await _fileOperationService.ExportFolderContentsAsync(SourcePath, DestinationPath, includeSubfolders);
                var fileInfo = new FileInfo(outputFilePath);
                var fileCount = File.ReadLines(fileInfo.FullName).Count() - 1;
                _dialogService.ShowNotification("Success", $"Folder contents exported successfully!\n\n{fileCount} files were processed.\nSaved to: {outputFilePath}");
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

        private async Task GenerateTreeStructure(object parameter)
        {
            IsBusy = true;
            try
            {
                var options = parameter as Dictionary<string, object>;
                Guard.AgainstNull(options, nameof(options), "Internal application error: Command parameter is missing.");

                string excludeFoldersText = (string)options["ExcludeFoldersText"];
                var excludedFolders = new HashSet<string>(
                    excludeFoldersText.Split(';').Select(f => f.Trim()).Where(f => !string.IsNullOrWhiteSpace(f)),
                    StringComparer.OrdinalIgnoreCase);

                string outputFilePath = await _fileOperationService.GenerateTreeStructureAsync(SourcePath, DestinationPath, excludedFolders);
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

        private void OnSourcePathOrOptionsChanged()
        {
            SelectableFiles.Clear();
            ShowLoadFilesButton = false;

            if (string.IsNullOrWhiteSpace(SourcePath) || !Directory.Exists(SourcePath))
            {
                return;
            }

            try
            {
                var files = FindFiles();
                if (files.Count > 100)
                {
                    ShowLoadFilesButton = true;
                    _dialogService.ShowNotification("Information", $"{files.Count} files found. Please click 'Load Files' to populate the list.");
                }
                else if (files.Any())
                {
                    LoadFiles(files);
                }
            }
            catch (Exception ex)
            {
                _dialogService.ShowNotification("Error", $"Could not access the folder or read files:\n\n{ex.Message}");
            }
        }

        private async Task LoadFilesAsync()
        {
            IsBusy = true;
            ShowLoadFilesButton = false;
            await Task.Run(() =>
            {
                try
                {
                    var files = FindFiles();
                    LoadFiles(files);
                }
                catch (Exception ex)
                {
                    _dialogService.ShowNotification("Error", $"Could not load files:\n\n{ex.Message}");
                }
            });
            IsBusy = false;
        }

        private List<string> FindFiles()
        {
            var extensions = GetParsedExtensions();
            if (!extensions.Any()) return new List<string>();

            var excludedFolders = GetParsedExcludedFolders();
            var searchOption = ContentExport_IncludeSubfolders ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;

            return Directory.EnumerateFiles(SourcePath, "*.*", searchOption)
                .Where(filePath =>
                {
                    bool hasValidExtension = extensions.Contains(Path.GetExtension(filePath).ToLowerInvariant());
                    if (!hasValidExtension) return false;

                    if (!ContentExport_IncludeSubfolders) return true;

                    bool isInExcludedFolder = excludedFolders.Any(exFolder =>
                    {
                        string relativeDir = Path.GetDirectoryName(filePath.Substring(SourcePath.Length)) ?? "";
                        return relativeDir.Split(Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries)
                                          .Contains(exFolder, StringComparer.OrdinalIgnoreCase);
                    });

                    return !isInExcludedFolder;
                })
                .ToList();
        }

        private void LoadFiles(List<string> filePaths)
        {
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                SelectableFiles.Clear();
                foreach (var path in filePaths.OrderBy(p => p))
                {
                    SelectableFiles.Add(new SelectableFile
                    {
                        FileName = Path.GetFileName(path),
                        FullPath = path,
                        IsSelected = false
                    });
                }
            });
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

        private void SelectAllFiles(object parameter)
        {
            foreach (var file in SelectableFiles)
            {
                file.IsSelected = true;
            }
        }

        private void DeselectAllFiles(object parameter)
        {
            foreach (var file in SelectableFiles)
            {
                file.IsSelected = false;
            }
        }

        private HashSet<string> GetParsedExtensions()
        {
            return new HashSet<string>(
                FileExtensions.Split(';')
                              .Select(ext => ext.Trim().ToLowerInvariant())
                              .Where(ext => !string.IsNullOrWhiteSpace(ext) && ext.StartsWith(".")),
                StringComparer.OrdinalIgnoreCase
            );
        }

        private HashSet<string> GetParsedExcludedFolders()
        {
            return new HashSet<string>(
                ContentExport_ExcludeFoldersText.Split(';')
                                  .Select(f => f.Trim())
                                  .Where(f => !string.IsNullOrWhiteSpace(f)),
                StringComparer.OrdinalIgnoreCase);
        }

        protected void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}