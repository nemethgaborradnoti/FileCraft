using FileCraft.Services.Interfaces;
using FileCraft.ViewModels.Shared;

namespace FileCraft.ViewModels.Functional
{
    public abstract class ExportViewModelBase : BaseViewModel
    {

        protected readonly ISharedStateService _sharedStateService;
        protected readonly IFileOperationService _fileOperationService;
        protected readonly IDialogService _dialogService;
        protected readonly ISettingsService _settingsService;

        private string _outputFileName = "ExportedFile";
        public string OutputFileName
        {
            get => _outputFileName;
            set
            {
                if (_outputFileName != value)
                {
                    _outputFileName = value;
                    OnPropertyChanged();
                }
            }
        }

        private bool _appendTimestamp = false;
        public bool AppendTimestamp
        {
            get => _appendTimestamp;
            set
            {
                if (_appendTimestamp != value)
                {
                    _appendTimestamp = value;
                    OnPropertyChanged();
                }
            }
        }

        public FolderTreeManager FolderTreeManager { get; }

        protected ExportViewModelBase(
            ISharedStateService sharedStateService,
            IFileOperationService fileOperationService,
            IDialogService dialogService,
            ISettingsService settingsService,
            FolderTreeManager folderTreeManager)
        {
            _sharedStateService = sharedStateService;
            _fileOperationService = fileOperationService;
            _dialogService = dialogService;
            _settingsService = settingsService;
            FolderTreeManager = folderTreeManager;
        }

        protected virtual bool CanExecuteOperation()
        {
            return !string.IsNullOrWhiteSpace(_sharedStateService.SourcePath) &&
                   !string.IsNullOrWhiteSpace(_sharedStateService.DestinationPath) &&
                   !string.IsNullOrWhiteSpace(OutputFileName) &&
                   !IsBusy;
        }

        protected string GetFinalFileName()
        {
            return AppendTimestamp
                ? $"{OutputFileName}_{DateTime.Now:yyyy_MM_dd_HH_mm_ss}"
                : OutputFileName;
        }

        protected void SaveSettings()
        {
            var settings = new Models.Settings
            {
                SourcePath = _sharedStateService.SourcePath,
                DestinationPath = _sharedStateService.DestinationPath,
                FolderTreeState = FolderTreeManager.GetFolderStates()
            };
            _settingsService.SaveSettings(settings);
        }
    }
}
