using FileCraft.Models;
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
        private readonly ISharedStateService _sharedStateService;

        public FolderTreeManager FolderTreeManager { get; }

        public string SourcePath
        {
            get => _sharedStateService.SourcePath;
            set
            {
                if (_sharedStateService.SourcePath != value)
                {
                    _sharedStateService.SourcePath = value;
                    OnPropertyChanged();
                    FolderTreeManager.LoadTreeForPath(value);
                    SaveSettings();
                }
            }
        }

        public string DestinationPath
        {
            get => _sharedStateService.DestinationPath;
            set
            {
                if (_sharedStateService.DestinationPath != value)
                {
                    _sharedStateService.DestinationPath = value;
                    OnPropertyChanged();
                    SaveSettings();
                }
            }
        }

        public FileContentExportViewModel FileContentExportVM { get; }
        public TreeGeneratorViewModel TreeGeneratorVM { get; }
        public FolderContentExportViewModel FolderContentExportVM { get; }
        public FileRenamerViewModel FileRenamerVM { get; }

        public ICommand ClearPathsCommand { get; }

        public MainViewModel(
            ISettingsService settingsService,
            ISharedStateService sharedStateService,
            FolderTreeManager folderTreeManager,
            FileContentExportViewModel fileContentExportVM,
            TreeGeneratorViewModel treeGeneratorVM,
            FolderContentExportViewModel folderContentExportVM,
            FileRenamerViewModel fileRenamerVM)
        {
            _settingsService = settingsService;
            _sharedStateService = sharedStateService;
            FolderTreeManager = folderTreeManager;
            FileContentExportVM = fileContentExportVM;
            TreeGeneratorVM = treeGeneratorVM;
            FolderContentExportVM = folderContentExportVM;
            FileRenamerVM = fileRenamerVM;

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
            DestinationPath = settings.DestinationPath;
            SourcePath = settings.SourcePath;
        }

        public void SaveSettings()
        {
            var settings = new Settings
            {
                SourcePath = this.SourcePath,
                DestinationPath = this.DestinationPath,
                FolderTreeState = FolderTreeManager.GetFolderStates()
            };
            _settingsService.SaveSettings(settings);
        }
    }
}
