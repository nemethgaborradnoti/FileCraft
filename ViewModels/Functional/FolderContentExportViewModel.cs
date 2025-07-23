using FileCraft.Services;
using FileCraft.Services.Interfaces;
using FileCraft.Shared.Commands;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace FileCraft.ViewModels.Functional
{
    public class FolderContentExportViewModel : BaseViewModel
    {
        private readonly MainViewModel _mainViewModel;
        private readonly IFileOperationService _fileOperationService;
        private readonly IDialogService _dialogService;
        private readonly IFolderTreeService _folderTreeService;

        public ObservableCollection<FolderViewModel> RootFolders { get; private set; } = new ObservableCollection<FolderViewModel>();

        public ICommand ExportFolderContentsCommand { get; }
        public ICommand SelectAllFoldersCommand { get; }
        public ICommand DeselectAllFoldersCommand { get; }

        public FolderContentExportViewModel(MainViewModel mainViewModel, IFileOperationService fileOperationService, IDialogService dialogService, IFolderTreeService folderTreeService)
        {
            _mainViewModel = mainViewModel;
            _fileOperationService = fileOperationService;
            _dialogService = dialogService;
            _folderTreeService = folderTreeService;

            _mainViewModel.SourcePathChanged += OnSourcePathChanged;

            ExportFolderContentsCommand = new RelayCommand(async (_) => await ExportFolderContents(), (_) => CanExecuteOperation());
            SelectAllFoldersCommand = new RelayCommand(SelectAllFolders, _ => RootFolders.Any());
            DeselectAllFoldersCommand = new RelayCommand(DeselectAllFolders, _ => RootFolders.Any());

            if (!string.IsNullOrWhiteSpace(_mainViewModel.SourcePath))
            {
                OnSourcePathChanged(_mainViewModel.SourcePath);
            }
        }

        private bool CanExecuteOperation()
        {
            return !string.IsNullOrWhiteSpace(_mainViewModel.SourcePath) &&
                   !string.IsNullOrWhiteSpace(_mainViewModel.DestinationPath) &&
                   !IsBusy;
        }

        private void OnSourcePathChanged(string newPath)
        {
            IsBusy = true;
            try
            {
                RootFolders = _folderTreeService.BuildFolderTree(newPath, () => { });
                OnPropertyChanged(nameof(RootFolders));
            }
            catch (Exception ex)
            {
                _dialogService.ShowNotification("Error", $"Could not access or process the source directory:\n\n{ex.Message}");
                RootFolders = new ObservableCollection<FolderViewModel>();
                OnPropertyChanged(nameof(RootFolders));
            }
            finally
            {
                IsBusy = false;
            }
        }

        private void SelectAllFolders(object? parameter)
        {
            if (!RootFolders.Any()) return;
            RootFolders[0].IsSelected = true;
            RootFolders[0].SetIsExpandedRecursively(true);
        }

        private void DeselectAllFolders(object? parameter)
        {
            if (!RootFolders.Any()) return;
            foreach (var child in RootFolders[0].Children)
            {
                child.IsSelected = false;
            }
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

                string outputFilePath = await _fileOperationService.ExportFolderContentsAsync(_mainViewModel.DestinationPath, includedFolderPaths);
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
