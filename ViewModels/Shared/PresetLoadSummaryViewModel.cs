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

        public PathPresetLoadResult Result { get; }

        public string Title => ResourceHelper.GetString("PresetSummary_Title");

        public string NotFoundPathsText { get; }

        public bool HasNotFoundPaths => Result.NotFoundPaths.Count > 0;

        public bool IsNotFoundVisible
        {
            get => _isNotFoundVisible;
            set
            {
                if (_isNotFoundVisible != value)
                {
                    _isNotFoundVisible = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(ToggleNotFoundButtonText));
                }
            }
        }

        public string ToggleNotFoundButtonText => IsNotFoundVisible
            ? ResourceHelper.GetString("PresetSummary_HideMissingButton")
            : ResourceHelper.GetString("PresetSummary_ShowMissingButton");

        public ICommand ToggleNotFoundCommand { get; }
        public ICommand CloseCommand { get; }

        public event Action? RequestClose;

        public PresetLoadSummaryViewModel(PathPresetLoadResult result, ISharedStateService sharedStateService)
        {
            Result = result;

            string sourcePath = sharedStateService.SourcePath;
            List<string> displayPaths;

            if (!string.IsNullOrWhiteSpace(sourcePath))
            {
                displayPaths = result.NotFoundPaths
                    .Select(p => Path.GetRelativePath(sourcePath, p))
                    .ToList();
            }
            else
            {
                displayPaths = result.NotFoundPaths;
            }

            NotFoundPathsText = string.Join(Environment.NewLine, displayPaths);

            ToggleNotFoundCommand = new RelayCommand(_ => IsNotFoundVisible = !IsNotFoundVisible);
            CloseCommand = new RelayCommand(_ => RequestClose?.Invoke());
        }
    }
}