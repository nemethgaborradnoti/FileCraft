using FileCraft.Services.Interfaces;
using FileCraft.Shared.Commands;
using FileCraft.Shared.Helpers;
using System.Windows.Input;

namespace FileCraft.ViewModels.Functional
{
    public class PathSelectionViewModel : BaseViewModel
    {
        private readonly ISharedStateService _sharedStateService;
        private readonly IDialogService _dialogService;

        public string SourcePath => _sharedStateService.SourcePath;
        public string DestinationPath => _sharedStateService.DestinationPath;

        public bool CanClearPaths => !string.IsNullOrWhiteSpace(SourcePath) || !string.IsNullOrWhiteSpace(DestinationPath);

        public ICommand SelectSourcePathCommand { get; }
        public ICommand SelectDestinationPathCommand { get; }
        public ICommand ClearPathsCommand { get; }

        public event Action<string>? SourcePathUpdated;
        public event Action? PathsCleared;

        public PathSelectionViewModel(ISharedStateService sharedStateService, IDialogService dialogService)
        {
            _sharedStateService = sharedStateService;
            _dialogService = dialogService;

            _sharedStateService.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(ISharedStateService.SourcePath))
                {
                    OnPropertyChanged(nameof(SourcePath));
                    OnPropertyChanged(nameof(CanClearPaths));
                    CommandManager.InvalidateRequerySuggested();
                }
                else if (e.PropertyName == nameof(ISharedStateService.DestinationPath))
                {
                    OnPropertyChanged(nameof(DestinationPath));
                    OnPropertyChanged(nameof(CanClearPaths));
                    CommandManager.InvalidateRequerySuggested();
                }
            };

            SelectSourcePathCommand = new RelayCommand(_ => SelectPath(isSource: true));
            SelectDestinationPathCommand = new RelayCommand(_ => SelectPath(isSource: false));
            ClearPathsCommand = new RelayCommand(_ => ClearPaths(), _ => CanClearPaths);
        }

        private void SelectPath(bool isSource)
        {
            OnStateChanging();
            var title = isSource ? ResourceHelper.GetString("MainVM_SelectSourceFolder") : ResourceHelper.GetString("MainVM_SelectDestFolder");
            var selectedPath = _dialogService.SelectFolder(title);
            if (!string.IsNullOrEmpty(selectedPath))
            {
                if (isSource)
                {
                    _sharedStateService.SourcePath = selectedPath;
                    SourcePathUpdated?.Invoke(selectedPath);
                }
                else
                {
                    _sharedStateService.DestinationPath = selectedPath;
                }
            }
        }

        private void ClearPaths()
        {
            OnStateChanging();
            _sharedStateService.SourcePath = string.Empty;
            _sharedStateService.DestinationPath = string.Empty;
            PathsCleared?.Invoke();
        }
    }
}