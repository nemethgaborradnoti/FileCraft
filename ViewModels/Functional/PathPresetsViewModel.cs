using FileCraft.Models;
using FileCraft.Services.Interfaces;
using FileCraft.Shared.Commands;
using FileCraft.Shared.Helpers;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows.Input;

namespace FileCraft.ViewModels.Functional
{
    public class PathPresetsViewModel : BaseViewModel
    {
        private readonly IPathPresetService _presetService;
        private readonly ISharedStateService _sharedStateService;
        private readonly IDialogService _dialogService;
        private readonly FileContentExportViewModel _fileContentExportVM;

        public ObservableCollection<PathPreset> Presets { get; } = new();

        private PathPreset? _selectedPreset;
        public PathPreset? SelectedPreset
        {
            get => _selectedPreset;
            set
            {
                if (_selectedPreset != value)
                {
                    _selectedPreset = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(IsPresetSelected));
                }
            }
        }

        public bool IsPresetSelected => SelectedPreset != null;

        public ICommand SaveCurrentCommand { get; }
        public ICommand LoadSelectedCommand { get; }
        public ICommand ViewContentCommand { get; }
        public ICommand RenameCommand { get; }
        public ICommand DeleteCommand { get; }
        public ICommand CloseCommand { get; }

        public event Action? RequestClose;

        public PathPresetsViewModel(
            IPathPresetService presetService,
            ISharedStateService sharedStateService,
            IDialogService dialogService,
            FileContentExportViewModel fileContentExportVM)
        {
            _presetService = presetService;
            _sharedStateService = sharedStateService;
            _dialogService = dialogService;
            _fileContentExportVM = fileContentExportVM;

            SaveCurrentCommand = new RelayCommand(_ => SaveCurrent(), _ => true);
            LoadSelectedCommand = new RelayCommand(_ => LoadSelected(), _ => IsPresetSelected);
            ViewContentCommand = new RelayCommand(_ => ViewContent(), _ => IsPresetSelected);
            RenameCommand = new RelayCommand(_ => RenameSelected(), _ => IsPresetSelected);
            DeleteCommand = new RelayCommand(_ => DeleteSelected(), _ => IsPresetSelected);
            CloseCommand = new RelayCommand(_ => RequestClose?.Invoke());

            LoadPresets();
        }

        private void LoadPresets()
        {
            Presets.Clear();
            var loaded = _presetService.LoadPresets().OrderBy(p => p.Name);
            foreach (var preset in loaded)
            {
                Presets.Add(preset);
            }
        }

        private void SaveCurrent()
        {
            var selectedPaths = _fileContentExportVM.GetSelectedFilePaths();
            if (!selectedPaths.Any())
            {
                _dialogService.ShowNotification(
                    ResourceHelper.GetString("Common_WarningTitle"),
                    ResourceHelper.GetString("PathPreset_NoSelectionError"),
                    DialogIconType.Warning);
                return;
            }

            string? name = _dialogService.ShowInputStringDialog(
                ResourceHelper.GetString("PathPreset_NewPresetTitle"),
                ResourceHelper.GetString("PathPreset_NamePrompt"));

            if (!string.IsNullOrWhiteSpace(name))
            {
                if (_presetService.PresetExists(name))
                {
                    bool overwrite = _dialogService.ShowConfirmation(
                        ResourceHelper.GetString("Common_WarningTitle"),
                        string.Format(ResourceHelper.GetString("Preset_OverwriteMessage"), "-", name),
                        DialogIconType.Warning);

                    if (!overwrite) return;
                }

                var preset = new PathPreset
                {
                    Name = name,
                    FilePaths = selectedPaths,
                    LastModified = DateTime.Now
                };

                _presetService.SavePreset(preset);
                LoadPresets();
                SelectedPreset = Presets.FirstOrDefault(p => p.Name == name);

                _dialogService.ShowNotification(
                    ResourceHelper.GetString("Common_SuccessTitle"),
                    string.Format(ResourceHelper.GetString("Common_SavedTo"), name),
                    DialogIconType.Success);
            }
        }

        private void LoadSelected()
        {
            if (SelectedPreset != null)
            {
                var result = _fileContentExportVM.LoadPathPreset(SelectedPreset.FilePaths);
                _dialogService.ShowPresetLoadSummary(result);
            }
        }

        private void ViewContent()
        {
            if (SelectedPreset != null)
            {
                string sourcePath = _sharedStateService.SourcePath;
                List<string> displayPaths;

                if (!string.IsNullOrWhiteSpace(sourcePath))
                {
                    displayPaths = SelectedPreset.FilePaths
                        .Select(p => Path.GetRelativePath(sourcePath, p))
                        .ToList();
                }
                else
                {
                    displayPaths = SelectedPreset.FilePaths.ToList();
                }

                string content = string.Join(Environment.NewLine, displayPaths);
                string title = string.Format(ResourceHelper.GetString("PathPreset_ViewTitle"), SelectedPreset.Name);
                _dialogService.ShowTextContentDialog(title, content);
            }
        }

        private void RenameSelected()
        {
            if (SelectedPreset != null)
            {
                string? newName = _dialogService.ShowInputStringDialog(
                    ResourceHelper.GetString("PathPreset_RenameTitle"),
                    string.Format(ResourceHelper.GetString("PathPreset_RenamePrompt"), SelectedPreset.Name),
                    SelectedPreset.Name);

                if (!string.IsNullOrWhiteSpace(newName) && newName != SelectedPreset.Name)
                {
                    _presetService.RenamePreset(SelectedPreset.Name, newName);
                    LoadPresets();
                    SelectedPreset = Presets.FirstOrDefault(p => p.Name == newName);
                }
            }
        }

        private void DeleteSelected()
        {
            if (SelectedPreset != null)
            {
                bool confirm = _dialogService.ShowConfirmation(
                    ResourceHelper.GetString("PathPreset_DeleteConfirmTitle"),
                    string.Format(ResourceHelper.GetString("PathPreset_DeleteConfirmMessage"), SelectedPreset.Name),
                    DialogIconType.Warning);

                if (confirm)
                {
                    _presetService.DeletePreset(SelectedPreset.Name);
                    LoadPresets();
                    SelectedPreset = null;
                }
            }
        }
    }
}