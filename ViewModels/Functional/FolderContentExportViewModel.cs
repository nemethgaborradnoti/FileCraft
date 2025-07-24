using FileCraft.Services;
using FileCraft.Services.Interfaces;
using FileCraft.Shared.Commands;
using FileCraft.ViewModels.Shared;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace FileCraft.ViewModels.Functional
{
    public class FolderContentExportViewModel : BaseViewModel
    {
        private readonly MainViewModel _mainViewModel;
        private readonly IFileOperationService _fileOperationService;
        private readonly IDialogService _dialogService;

        private string _outputFileName = "FolderContentsExport";
        public string OutputFileName
        {
            get => _outputFileName;
            set
            {
                _outputFileName = value;
                OnPropertyChanged();
            }
        }

        private bool _appendTimestamp = true;
        public bool AppendTimestamp
        {
            get => _appendTimestamp;
            set
            {
                _appendTimestamp = value;
                OnPropertyChanged();
            }
        }

        public FolderTreeManager FolderTreeManager { get; }
        public ObservableCollection<FolderViewModel> RootFolders => FolderTreeManager.RootFolders;

        public ICommand ExportFolderContentsCommand { get; }

        public FolderContentExportViewModel(MainViewModel mainViewModel, IFileOperationService fileOperationService, IDialogService dialogService, FolderTreeManager folderTreeManager)
        {
            _mainViewModel = mainViewModel;
            _fileOperationService = fileOperationService;
            _dialogService = dialogService;
            FolderTreeManager = folderTreeManager;

            FolderTreeManager.PropertyChanged += (s, e) => {
                if (e.PropertyName == nameof(FolderTreeManager.RootFolders))
                {
                    OnPropertyChanged(nameof(RootFolders));
                }
            };

            ExportFolderContentsCommand = new RelayCommand(async (_) => await ExportFolderContents(), (_) => CanExecuteOperation());
        }

        private bool CanExecuteOperation()
        {
            return !string.IsNullOrWhiteSpace(_mainViewModel.SourcePath) &&
                   !string.IsNullOrWhiteSpace(_mainViewModel.DestinationPath) &&
                   !string.IsNullOrWhiteSpace(OutputFileName) &&
                   !IsBusy;
        }

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

                string finalFileName = AppendTimestamp
                    ? $"{OutputFileName}_{DateTime.Now:yyyy_MM_dd_HH_mm_ss}"
                    : OutputFileName;

                string outputFilePath = await _fileOperationService.ExportFolderContentsAsync(_mainViewModel.DestinationPath, includedFolderPaths, finalFileName);
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
    }
}
