using FileCraft.Services;
using FileCraft.Services.Interfaces;
using FileCraft.Shared.Commands;
using FileCraft.Shared.Validation;
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

        public event PropertyChangedEventHandler? PropertyChanged;

        public string SourcePath
        {
            get => _sourcePath;
            set { _sourcePath = value; OnPropertyChanged(); }
        }

        public string DestinationPath
        {
            get => _destinationPath;
            set { _destinationPath = value; OnPropertyChanged(); }
        }

        public bool IsBusy
        {
            get => _isBusy;
            set { _isBusy = value; OnPropertyChanged(); }
        }

        public ICommand ExportFolderContentsCommand { get; }
        public ICommand GenerateTreeStructureCommand { get; }

        public MainViewModel(IFileOperationService fileOperationService, IDialogService dialogService)
        {
            _fileOperationService = fileOperationService;
            _dialogService = dialogService;

            ExportFolderContentsCommand = new RelayCommand(async (p) => await ExportFolderContents(p), CanExecuteOperation);
            GenerateTreeStructureCommand = new RelayCommand(async (p) => await GenerateTreeStructure(p), CanExecuteOperation);
        }

        private bool CanExecuteOperation(object parameter)
        {
            return !string.IsNullOrWhiteSpace(SourcePath) &&
                   !string.IsNullOrWhiteSpace(DestinationPath) &&
                   !IsBusy;
        }

        private async Task ExportFolderContents(object parameter)
        {
            var options = parameter as Dictionary<string, object>;
            Guard.AgainstNull(options, nameof(options), "Internal application error: Command parameter is missing.");

            bool includeSubfolders = (bool)options["IncludeSubfolders"];

            IsBusy = true;
            try
            {
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
            var options = parameter as Dictionary<string, object>;
            Guard.AgainstNull(options, nameof(options), "Internal application error: Command parameter is missing.");

            string excludeFoldersText = (string)options["ExcludeFoldersText"];
            var excludedFolders = new HashSet<string>(
                excludeFoldersText.Split(';').Select(f => f.Trim()).Where(f => !string.IsNullOrWhiteSpace(f)),
                StringComparer.OrdinalIgnoreCase);

            IsBusy = true;
            try
            {
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

        protected void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
