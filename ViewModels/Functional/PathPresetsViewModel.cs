using FileCraft.Models;
using FileCraft.Services.Interfaces;
using FileCraft.Shared.Helpers;
using FileCraft.ViewModels.Shared;
using System.IO;

namespace FileCraft.ViewModels.Functional
{
    public class PathPresetsViewModel : BaseViewModel
    {
        private readonly IPathPresetService _presetService;
        private readonly ISharedStateService _sharedStateService;
        private readonly IDialogService _dialogService;
        private readonly FileContentExportViewModel _fileContentExportVM;

        public PresetListViewModel ListViewModel { get; }

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

            ListViewModel = new PresetListViewModel();

            ListViewModel.SaveNewRequested += OnSaveNewRequested;
            ListViewModel.LoadItemRequested += OnLoadRequested;
            ListViewModel.DeleteItemRequested += OnDeleteRequested;
            ListViewModel.RenameItemRequested += OnRenameRequested;
            ListViewModel.ViewItemDetailsRequested += OnViewDetailsRequested; // Replaced

            LoadPresets();
        }

        private void LoadPresets()
        {
            var presets = _presetService.LoadPresets();
            var viewModels = presets.Select(p => new PresetItemViewModel(
                p.Id,
                p.Name,
                p.LastModified,
                $"{p.FilePaths.Count} files",
                p
            ));

            ListViewModel.SetItems(viewModels);
        }

        private void OnSaveNewRequested()
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
                    FilePaths = selectedPaths
                };

                _presetService.SavePreset(preset);
                LoadPresets();

                _dialogService.ShowNotification(
                    ResourceHelper.GetString("Common_SuccessTitle"),
                    string.Format(ResourceHelper.GetString("Common_SavedTo"), name),
                    DialogIconType.Success);
            }
        }

        private void OnLoadRequested(PresetItemViewModel item)
        {
            if (item.RawData is PathPreset preset)
            {
                var result = _fileContentExportVM.LoadPathPreset(preset.FilePaths);
                _dialogService.ShowPresetLoadSummary(result);
            }
        }

        private void OnViewDetailsRequested(PresetItemViewModel item)
        {
            // Now using the unified nice Details window
            _dialogService.ShowPresetDetails(item);
        }

        private void OnRenameRequested(PresetItemViewModel item)
        {
            string? newName = _dialogService.ShowInputStringDialog(
                ResourceHelper.GetString("PathPreset_RenameTitle"),
                string.Format(ResourceHelper.GetString("PathPreset_RenamePrompt"), item.Name),
                item.Name);

            if (!string.IsNullOrWhiteSpace(newName) && newName != item.Name)
            {
                _presetService.RenamePreset(item.Name, newName);
                LoadPresets();
            }
        }

        private void OnDeleteRequested(PresetItemViewModel item)
        {
            bool confirm = _dialogService.ShowConfirmation(
                ResourceHelper.GetString("PathPreset_DeleteConfirmTitle"),
                string.Format(ResourceHelper.GetString("PathPreset_DeleteConfirmMessage"), item.Name),
                DialogIconType.Warning);

            if (confirm)
            {
                _presetService.DeletePreset(item.Name);
                ListViewModel.RemoveItem(item);
            }
        }
    }
}