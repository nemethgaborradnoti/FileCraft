using FileCraft.Services.Interfaces;
using FileCraft.ViewModels.Shared;

namespace FileCraft.ViewModels.Functional
{
    public abstract class ExportViewModelBase : BaseViewModel
    {
        protected readonly ISharedStateService _sharedStateService;
        protected readonly IFileOperationService _fileOperationService;
        protected readonly IDialogService _dialogService;
        public FolderTreeManager FolderTreeManager { get; }

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
