using FileCraft.Models;
using FileCraft.Services;
using FileCraft.Services.Interfaces;
using FileCraft.Shared.Commands;
using FileCraft.ViewModels.Functional;
using FileCraft.ViewModels.Shared;
using System.Windows.Input;

namespace FileCraft.ViewModels
{
    public class MainViewModel : BaseViewModel
    {
        private readonly ISettingsService _settingsService;
        private string _sourcePath = string.Empty;
        private string _destinationPath = string.Empty;

        public FolderTreeManager FolderTreeManager { get; }

        public string SourcePath
        {
            get => _sourcePath;
            set
            {
                if (_sourcePath != value)
                {
                    _sourcePath = value;
                    OnPropertyChanged();
                    FolderTreeManager.LoadTreeForPath(value);
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
                    SaveDestinationPath();
                }
            }
        }

        public FileContentExportViewModel FileContentExportVM { get; }
        public TreeGeneratorViewModel TreeGeneratorVM { get; }
        public FolderContentExportViewModel FolderContentExportVM { get; }
        public FileRenamerViewModel FileRenamerVM { get; }
        public ICommand ClearPathsCommand { get; }

        public MainViewModel(IFileOperationService fileOperationService, IDialogService dialogService)
        {
            _settingsService = new SettingsService();
            IFolderTreeService folderTreeService = new FolderTreeService();

            FolderTreeManager = new FolderTreeManager(folderTreeService, _settingsService);

            FileContentExportVM = new FileContentExportViewModel(this, fileOperationService, dialogService, FolderTreeManager);
            TreeGeneratorVM = new TreeGeneratorViewModel(this, fileOperationService, dialogService, FolderTreeManager);
            FolderContentExportVM = new FolderContentExportViewModel(this, fileOperationService, dialogService, FolderTreeManager);
            FileRenamerVM = new FileRenamerViewModel();

            ClearPathsCommand = new RelayCommand(_ => ClearPaths());

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
            _destinationPath = settings.DestinationPath;
            OnPropertyChanged(nameof(DestinationPath));

            SourcePath = settings.SourcePath;
        }

        private void SaveDestinationPath()
        {
            var settings = _settingsService.LoadSettings();
            settings.DestinationPath = this.DestinationPath;
            _settingsService.SaveSettings(settings);
        }
    }
}
