using FileCraft.Models;
using FileCraft.Services.Interfaces;
using FileCraft.Shared.Commands;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Windows.Input;

namespace FileCraft.ViewModels.Functional
{
    public class FileRenamerViewModel : BaseViewModel
    {
        private readonly ISharedStateService _sharedStateService;
        private readonly IFileOperationService _fileOperationService;
        private readonly IDialogService _dialogService;

        private string _outputFileName = "RenameResult";
        public string OutputFileName
        {
            get => _outputFileName;
            set { _outputFileName = value; OnPropertyChanged(); }
        }

        private bool _appendTimestamp;
        public bool AppendTimestamp
        {
            get => _appendTimestamp;
            set { _appendTimestamp = value; OnPropertyChanged(); }
        }

        private bool _includeFolders;
        public bool IncludeFolders
        {
            get => _includeFolders;
            set
            {
                if (_includeFolders != value)
                {
                    _includeFolders = value;
                    OnPropertyChanged();
                    UpdatePreview();
                }
            }
        }

        public ObservableCollection<string> RenamePreview { get; } = new();

        public ICommand RenameFilesCommand { get; }

        public FileRenamerViewModel(
            ISharedStateService sharedStateService,
            IFileOperationService fileOperationService,
            IDialogService dialogService)
        {
            _sharedStateService = sharedStateService;
            _fileOperationService = fileOperationService;
            _dialogService = dialogService;

            _sharedStateService.PropertyChanged += OnSharedStatePropertyChanged;

            RenameFilesCommand = new RelayCommand(async _ => await ExecuteRenameAsync(), _ => CanExecuteRename());

            UpdatePreview();
        }

        private void OnSharedStatePropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ISharedStateService.SourcePath))
            {
                UpdatePreview();
            }
        }

        private bool CanExecuteRename()
        {
            return !string.IsNullOrWhiteSpace(_sharedStateService.SourcePath)
                && Directory.Exists(_sharedStateService.SourcePath)
                && !string.IsNullOrWhiteSpace(_sharedStateService.DestinationPath)
                && Directory.Exists(_sharedStateService.DestinationPath)
                && !IsBusy;
        }

        private void UpdatePreview()
        {
            RenamePreview.Clear();

            string sourcePath = _sharedStateService.SourcePath;
            if (string.IsNullOrWhiteSpace(sourcePath) || !Directory.Exists(sourcePath))
            {
                return;
            }

            try
            {
                var itemsToPreview = new List<string>();
                itemsToPreview.AddRange(Directory.GetFiles(sourcePath, "*.*", SearchOption.TopDirectoryOnly));
                if (IncludeFolders)
                {
                    itemsToPreview.AddRange(Directory.GetDirectories(sourcePath, "*", SearchOption.TopDirectoryOnly));
                }

                int currentNumber = 1;
                foreach (var itemPath in itemsToPreview.OrderBy(f => f).Take(100))
                {
                    string originalName = Path.GetFileName(itemPath);
                    bool isDirectory = File.GetAttributes(itemPath).HasFlag(FileAttributes.Directory);
                    string extension = isDirectory ? "" : Path.GetExtension(itemPath);
                    string newName = $"{currentNumber:D4}{extension}";
                    RenamePreview.Add($"{originalName}  ->  {newName}");
                    currentNumber++;
                }
            }
            catch
            {
            }
        }

        private async Task ExecuteRenameAsync()
        {
            IsBusy = true;
            try
            {
                var itemsToRename = new List<string>();
                itemsToRename.AddRange(Directory.GetFiles(_sharedStateService.SourcePath, "*.*", SearchOption.TopDirectoryOnly));
                if (IncludeFolders)
                {
                    itemsToRename.AddRange(Directory.GetDirectories(_sharedStateService.SourcePath, "*", SearchOption.TopDirectoryOnly));
                }

                if (!itemsToRename.Any())
                {
                    _dialogService.ShowNotification("Information", "No items to rename in the source folder.");
                    return;
                }

                bool confirmed = _dialogService.ShowConfirmation(
                    actionName: "Rename Files",
                    destinationPath: _sharedStateService.SourcePath,
                    filesAffected: itemsToRename.Count);

                if (!confirmed)
                {
                    return;
                }

                string logFilePath = await _fileOperationService.RenameFilesAsync(
                    _sharedStateService.SourcePath,
                    _sharedStateService.DestinationPath,
                    OutputFileName,
                    AppendTimestamp,
                    IncludeFolders);

                _dialogService.ShowNotification("Operation Finished", $"Rename process complete.\n\nLog file created at:\n{logFilePath}");
            }
            catch (System.Exception ex)
            {
                _dialogService.ShowNotification("Error", $"An unexpected error occurred:\n\n{ex.Message}");
            }
            finally
            {
                IsBusy = false;
                UpdatePreview();
            }
        }

        public void ApplySettings(FileRenamerSettings settings)
        {
            OutputFileName = settings.OutputFileName;
            AppendTimestamp = settings.AppendTimestamp;
            IncludeFolders = settings.IncludeFolders;
        }

        public FileRenamerSettings GetSettings()
        {
            return new FileRenamerSettings
            {
                OutputFileName = this.OutputFileName,
                AppendTimestamp = this.AppendTimestamp,
                IncludeFolders = this.IncludeFolders
            };
        }
    }
}
