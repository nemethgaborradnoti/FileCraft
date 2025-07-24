using FileCraft.Services;
using FileCraft.Services.Interfaces;
using FileCraft.Shared.Commands;
using FileCraft.ViewModels.Shared;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace FileCraft.ViewModels.Functional
{
    public class TreeGeneratorViewModel : BaseViewModel
    {
        private readonly MainViewModel _mainViewModel;
        private readonly IFileOperationService _fileOperationService;
        private readonly IDialogService _dialogService;

        private string _outputFileName = "TreeStructure";
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

        public ICommand GenerateTreeStructureCommand { get; }
        public ICommand SelectAllFoldersCommand { get; }
        public ICommand DeselectAllFoldersCommand { get; }

        public TreeGeneratorViewModel(MainViewModel mainViewModel, IFileOperationService fileOperationService, IDialogService dialogService, FolderTreeManager folderTreeManager)
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

            GenerateTreeStructureCommand = new RelayCommand(async (_) => await GenerateTreeStructure(), (_) => CanExecuteOperation());
            SelectAllFoldersCommand = new RelayCommand(SelectAllFolders, _ => RootFolders.Any());
            DeselectAllFoldersCommand = new RelayCommand(DeselectAllFolders, _ => RootFolders.Any());
        }

        private bool CanExecuteOperation()
        {
            return !string.IsNullOrWhiteSpace(_mainViewModel.SourcePath) &&
                   !string.IsNullOrWhiteSpace(_mainViewModel.DestinationPath) &&
                   !string.IsNullOrWhiteSpace(OutputFileName) &&
                   !IsBusy;
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

                string finalFileName = AppendTimestamp
                    ? $"{OutputFileName}_{DateTime.Now:yyyy_MM_dd_HH_mm_ss}"
                    : OutputFileName;

                string outputFilePath = await _fileOperationService.GenerateTreeStructureAsync(_mainViewModel.SourcePath, _mainViewModel.DestinationPath, excludedFolderPaths, finalFileName);
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
