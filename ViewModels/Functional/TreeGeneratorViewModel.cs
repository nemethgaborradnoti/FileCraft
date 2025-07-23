using FileCraft.Services;
using FileCraft.Services.Interfaces;
using FileCraft.Shared.Commands;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace FileCraft.ViewModels.Functional
{
    public class TreeGeneratorViewModel : BaseViewModel
    {
        private readonly MainViewModel _mainViewModel;
        private readonly IFileOperationService _fileOperationService;
        private readonly IDialogService _dialogService;
        private readonly IFolderTreeService _folderTreeService;

        public ObservableCollection<FolderViewModel> RootFolders { get; private set; } = new ObservableCollection<FolderViewModel>();

        public ICommand GenerateTreeStructureCommand { get; }
        public ICommand SelectAllFoldersCommand { get; }
        public ICommand DeselectAllFoldersCommand { get; }

        public TreeGeneratorViewModel(MainViewModel mainViewModel, IFileOperationService fileOperationService, IDialogService dialogService, IFolderTreeService folderTreeService)
        {
            _mainViewModel = mainViewModel;
            _fileOperationService = fileOperationService;
            _dialogService = dialogService;
            _folderTreeService = folderTreeService;

            _mainViewModel.SourcePathChanged += OnSourcePathChanged;

            GenerateTreeStructureCommand = new RelayCommand(async (_) => await GenerateTreeStructure(), (_) => CanExecuteOperation());
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

        private async Task GenerateTreeStructure()
        {
            IsBusy = true;
            try
            {
                var allNodes = RootFolders.Any() ? RootFolders[0].GetAllNodes() : Enumerable.Empty<FolderViewModel>();

                var excludedFolderPaths = new HashSet<string>(
                    allNodes.Where(n => n.IsSelected == false).Select(n => n.FullPath),
                    StringComparer.OrdinalIgnoreCase);

                string outputFilePath = await _fileOperationService.GenerateTreeStructureAsync(_mainViewModel.SourcePath, _mainViewModel.DestinationPath, excludedFolderPaths);
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
    }
}
