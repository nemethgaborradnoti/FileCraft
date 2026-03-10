using FileCraft.Models;
using FileCraft.Services.Interfaces;
using FileCraft.Shared.Commands;
using FileCraft.Shared.Helpers;
using System.IO;
using System.Windows.Input;

namespace FileCraft.ViewModels.Shared
{
    public class PresetLoadSummaryViewModel : BaseViewModel
    {
        private bool _isNotFoundVisible;
        private bool _isLoadedVisible;

        public PathPresetLoadResult Result { get; }

        public string Title => ResourceHelper.GetString("PresetSummary_Title");

        public string NotFoundPathsText { get; }
        public string LoadedPathsText { get; }

        public bool HasNotFoundPaths => Result.NotFoundPaths.Count > 0;
        public bool HasLoadedPaths => Result.LoadedPaths.Count > 0;

        public bool IsNotFoundVisible
        {
            get => _isNotFoundVisible;
            set
            {
                if (_isNotFoundVisible != value)
                {
                    _isNotFoundVisible = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool IsLoadedVisible
        {
            get => _isLoadedVisible;
            set
            {
                if (_isLoadedVisible != value)
                {
                    _isLoadedVisible = value;
                    OnPropertyChanged();
                }
            }
        }

        public ICommand ToggleNotFoundCommand { get; }
        public ICommand ToggleLoadedCommand { get; }
        public ICommand CloseCommand { get; }

        public event Action? RequestClose;

        public PresetLoadSummaryViewModel(PathPresetLoadResult result, ISharedStateService sharedStateService)
        {
            Result = result;

            string sourcePath = sharedStateService.SourcePath;
            List<string> displayNotFoundPaths;
            List<string> displayLoadedPaths;

            if (!string.IsNullOrWhiteSpace(sourcePath))
            {
                displayNotFoundPaths = result.NotFoundPaths
                    .Select(p => Path.GetRelativePath(sourcePath, p))
                    .ToList();

                displayLoadedPaths = result.LoadedPaths;
            }
            else
            {
                displayNotFoundPaths = result.NotFoundPaths;
                displayLoadedPaths = result.LoadedPaths;
            }

            NotFoundPathsText = string.Join(Environment.NewLine, displayNotFoundPaths);
            LoadedPathsText = string.Join(Environment.NewLine, displayLoadedPaths);

            ToggleNotFoundCommand = new RelayCommand(_ => { if (HasNotFoundPaths) IsNotFoundVisible = !IsNotFoundVisible; });
            ToggleLoadedCommand = new RelayCommand(_ => { if (HasLoadedPaths) IsLoadedVisible = !IsLoadedVisible; });
            CloseCommand = new RelayCommand(_ => RequestClose?.Invoke());
        }
    }
}