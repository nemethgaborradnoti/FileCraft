using FileCraft.Services.Interfaces;
using FileCraft.ViewModels.Shared;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace FileCraft.ViewModels.Functional
{
    public abstract class ExportViewModelBase : BaseViewModel
    {
        protected readonly ISharedStateService _sharedStateService;
        protected readonly IFileOperationService _fileOperationService;
        protected readonly IDialogService _dialogService;

        private string _outputFileName = string.Empty;
        private bool _appendTimestamp;

        public FolderTreeManager FolderTreeManager { get; }
        public ObservableCollection<TabIconViewModel> LinkedTabs { get; } = new();

        public ObservableCollection<FolderViewModel> RootFolders => FolderTreeManager.RootFolders;

        public string OutputFileName
        {
            get => _outputFileName;
            set
            {
                if (_outputFileName != value)
                {
                    OnStateChanging();
                    _outputFileName = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool AppendTimestamp
        {
            get => _appendTimestamp;
            set
            {
                if (_appendTimestamp != value)
                {
                    OnStateChanging();
                    _appendTimestamp = value;
                    OnPropertyChanged();
                }
            }
        }

        protected ExportViewModelBase(
            ISharedStateService sharedStateService,
            IFileOperationService fileOperationService,
            IDialogService dialogService,
            FolderTreeManager folderTreeManager)
        {
            _sharedStateService = sharedStateService;
            _fileOperationService = fileOperationService;
            _dialogService = dialogService;
            FolderTreeManager = folderTreeManager;

            FolderTreeManager.PropertyChanged += OnFolderTreeManagerPropertyChanged;
        }

        protected virtual void OnFolderTreeManagerPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(FolderTreeManager.RootFolders))
            {
                OnPropertyChanged(nameof(RootFolders));
            }
        }

        protected virtual bool CanExecuteOperation(string outputFileName)
        {
            return !string.IsNullOrWhiteSpace(_sharedStateService.SourcePath) &&
                   !string.IsNullOrWhiteSpace(_sharedStateService.DestinationPath) &&
                   !string.IsNullOrWhiteSpace(outputFileName) &&
                   !IsBusy;
        }

        protected string GetFinalFileName(string outputFileName, bool appendTimestamp)
        {
            return appendTimestamp
                ? $"{outputFileName}_{DateTime.Now:yyyy_MM_dd_HH_mm_ss}"
                : outputFileName;
        }
    }
}