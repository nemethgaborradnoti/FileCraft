using FileCraft.Models;
using FileCraft.Services;
using FileCraft.Services.Interfaces;
using FileCraft.Shared.Commands;
using FileCraft.ViewModels.Functional;
using System.Windows.Input;

namespace FileCraft.ViewModels
{
    public class MainViewModel : BaseViewModel
    {
        private readonly ISettingsService _settingsService;
        private string _sourcePath = string.Empty;
        private string _destinationPath = string.Empty;
        public event Action<string>? SourcePathChanged;
        public string SourcePath
        {
            get => _sourcePath;
            set
            {
                if (_sourcePath != value)
                {
                    _sourcePath = value;
                    OnPropertyChanged();
                    SourcePathChanged?.Invoke(value);
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
        public FileContentExportViewModel FileContentExportVM { get; }
        public TreeGeneratorViewModel TreeGeneratorVM { get; }
        public FolderContentExportViewModel FolderContentExportVM { get; }
        public FileRenamerViewModel FileRenamerVM { get; }
        public ICommand ClearPathsCommand { get; }

        public MainViewModel(IFileOperationService fileOperationService, IDialogService dialogService)
        {
            _settingsService = new SettingsService();
            IFolderTreeService folderTreeService = new FolderTreeService();

            FileContentExportVM = new FileContentExportViewModel(this, fileOperationService, dialogService, folderTreeService);
            TreeGeneratorVM = new TreeGeneratorViewModel(this, fileOperationService, dialogService, folderTreeService);
            FolderContentExportVM = new FolderContentExportViewModel(this, fileOperationService, dialogService, folderTreeService);
            FileRenamerVM = new FileRenamerViewModel();

            ClearPathsCommand = new RelayCommand(_ => ClearPaths(), _ => !string.IsNullOrEmpty(SourcePath) || !string.IsNullOrEmpty(DestinationPath));

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
            SourcePath = settings.SourcePath;
            DestinationPath = settings.DestinationPath;
        }

        private void SaveSettings()
        {
            var settings = new Settings { SourcePath = this.SourcePath, DestinationPath = this.DestinationPath };
            _settingsService.SaveSettings(settings);
        }
    }
}
