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
        private readonly IDialogService _dialogService;
        private bool _isLoading = false;

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
                    if (!_isLoading)
                    {
                        FolderTreeManager.LoadTreeForPath(value);
                    }
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
                }
            }
        }

        public FileContentExportViewModel FileContentExportVM { get; }
        public TreeGeneratorViewModel TreeGeneratorVM { get; }
        public FolderContentExportViewModel FolderContentExportVM { get; }
        public FileRenamerViewModel FileRenamerVM { get; }

        public ICommand ClearPathsCommand { get; }
        public ICommand SelectSourcePathCommand { get; }
        public ICommand SelectDestinationPathCommand { get; }

        public MainViewModel(
            ISettingsService settingsService,
            ISharedStateService sharedStateService,
            IDialogService dialogService,
            FolderTreeManager folderTreeManager,
            FileContentExportViewModel fileContentExportVM,
            TreeGeneratorViewModel treeGeneratorVM,
            FolderContentExportViewModel folderContentExportVM,
            FileRenamerViewModel fileRenamerVM)
        {
            _settingsService = settingsService;
            _sharedStateService = sharedStateService;
            _dialogService = dialogService;
            FolderTreeManager = folderTreeManager;
            FileContentExportVM = fileContentExportVM;
            TreeGeneratorVM = treeGeneratorVM;
            FolderContentExportVM = folderContentExportVM;
            FileRenamerVM = fileRenamerVM;

            ClearPathsCommand = new RelayCommand(_ => ClearPaths());
            SelectSourcePathCommand = new RelayCommand(_ => SelectPath(isSource: true));
            SelectDestinationPathCommand = new RelayCommand(_ => SelectPath(isSource: false));

            LoadSettings();
        }

        private void SelectPath(bool isSource)
        {
            var title = isSource ? "Select the common source folder" : "Select the common destination folder";
            var selectedPath = _dialogService.SelectFolder(title);
            if (!string.IsNullOrEmpty(selectedPath))
            {
                if (isSource)
                    SourcePath = selectedPath;
                else
                    DestinationPath = selectedPath;
            }
        }

        private void ClearPaths()
        {
            SourcePath = string.Empty;
            DestinationPath = string.Empty;
        }

        private void LoadSettings()
        {
            _isLoading = true;
            Settings settings = _settingsService.LoadSettings();
            SourcePath = settings.SourcePath;
            DestinationPath = settings.DestinationPath;

            FileContentExportVM.ApplySettings(settings.FileContentExport);

            FolderContentExportVM.OutputFileName = settings.FolderContentExport.OutputFileName;
            FolderContentExportVM.AppendTimestamp = settings.FolderContentExport.AppendTimestamp;

            TreeGeneratorVM.OutputFileName = settings.TreeGenerator.OutputFileName;
            TreeGeneratorVM.AppendTimestamp = settings.TreeGenerator.AppendTimestamp;

            FolderTreeManager.LoadTreeForPath(SourcePath, settings.FolderTreeState);
            _isLoading = false;
        }

        public void SaveSettings()
        {
            var settings = new Settings
            {
                SourcePath = this.SourcePath,
                DestinationPath = this.DestinationPath,
                FolderTreeState = FolderTreeManager.GetFolderStates(),
                FileContentExport = new FileContentExportSettings
                {
                    OutputFileName = FileContentExportVM.OutputFileName,
                    AppendTimestamp = FileContentExportVM.AppendTimestamp,
                    SelectedExtensions = FileContentExportVM.GetSelectedExtensions(),
                    SelectedFilePaths = FileContentExportVM.GetSelectedFilePaths()
                },
                FolderContentExport = new FolderContentExportSettings
                {
                    OutputFileName = FolderContentExportVM.OutputFileName,
                    AppendTimestamp = FolderContentExportVM.AppendTimestamp
                },
                TreeGenerator = new TreeGeneratorSettings
                {
                    OutputFileName = TreeGeneratorVM.OutputFileName,
                    AppendTimestamp = TreeGeneratorVM.AppendTimestamp
                }
            };
            _settingsService.SaveSettings(settings);
        }
    }
}
