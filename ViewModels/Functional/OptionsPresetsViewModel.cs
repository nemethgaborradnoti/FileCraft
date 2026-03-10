using FileCraft.Models;
using FileCraft.Services.Interfaces;
using FileCraft.Shared.Helpers;
using FileCraft.ViewModels.Shared;
using System;
using System.Linq;

namespace FileCraft.ViewModels.Functional
{
    public class OptionsPresetsViewModel : BaseViewModel
    {
        private readonly ISaveService _saveService;
        private readonly IDialogService _dialogService;

        public event Action? PresetCreateRequested;
        public event Action<int>? PresetLoadRequested;
        public event Action<int, string>? PresetOverwriteRequested;

        public PresetListViewModel PresetListViewModel { get; }

        public OptionsPresetsViewModel(ISaveService saveService, IDialogService dialogService)
        {
            _saveService = saveService;
            _dialogService = dialogService;

            PresetListViewModel = new PresetListViewModel();

            var saveData = _saveService.LoadSaveData();
            PresetListViewModel.InitializeSort(saveData.SavePresetSortBy, saveData.SavePresetIsDescending);

            PresetListViewModel.SaveNewRequested += () => PresetCreateRequested?.Invoke();
            PresetListViewModel.LoadItemRequested += OnPresetLoadItemRequested;
            PresetListViewModel.OverwriteItemRequested += OnPresetOverwriteItemRequested;
            PresetListViewModel.DeleteItemRequested += OnPresetDeleteItemRequested;
            PresetListViewModel.RenameItemRequested += OnPresetRenameItemRequested;
            PresetListViewModel.ViewItemDetailsRequested += OnPresetViewItemDetailsRequested;
            PresetListViewModel.SortChanged += OnSortChanged;

            RefreshPresetList();
        }

        private void OnSortChanged()
        {
            var saveData = _saveService.LoadSaveData();
            saveData.SavePresetSortBy = PresetListViewModel.SortBy;
            saveData.SavePresetIsDescending = PresetListViewModel.IsDescending;
            _saveService.Save(saveData);
        }

        public void RefreshPresetList()
        {
            var presets = _saveService.LoadPresets();
            var viewModels = presets.Select(p => new PresetItemViewModel(
                p.Id,
                p.Name,
                p.LastModified,
                p.Description,
                p
            ));
            PresetListViewModel.SetItems(viewModels);
        }

        private void OnPresetViewItemDetailsRequested(PresetItemViewModel item)
        {
            _dialogService.ShowPresetDetails(item);
        }

        private void OnPresetLoadItemRequested(PresetItemViewModel item)
        {
            if (item.Id is int id)
            {
                PresetLoadRequested?.Invoke(id);
            }
        }

        private void OnPresetOverwriteItemRequested(PresetItemViewModel item)
        {
            if (item.Id is int id)
            {
                PresetOverwriteRequested?.Invoke(id, item.Name);
            }
        }

        private void OnPresetDeleteItemRequested(PresetItemViewModel item)
        {
            if (item.Id is int id)
            {
                bool confirm = _dialogService.ShowConfirmation(
                    ResourceHelper.GetString("Preset_DeleteConfirmTitle"),
                    string.Format(ResourceHelper.GetString("Preset_DeleteConfirmMessage"), id, item.Name),
                    DialogIconType.Warning);

                if (confirm)
                {
                    _saveService.DeletePreset(id);
                    PresetListViewModel.RemoveItem(item);

                    _dialogService.ShowNotification(
                         ResourceHelper.GetString("Common_SuccessTitle"),
                         string.Format(ResourceHelper.GetString("Preset_DeletedSuccess"), id, item.Name),
                         DialogIconType.Success);
                }
            }
        }

        private void OnPresetRenameItemRequested(PresetItemViewModel item)
        {
            if (item.Id is int id)
            {
                string? newName = _dialogService.ShowInputStringDialog(
                    ResourceHelper.GetString("RenamePreset_Title"),
                    string.Format(ResourceHelper.GetString("RenamePreset_Prompt"), item.Name),
                    item.Name);

                if (!string.IsNullOrWhiteSpace(newName) && newName != item.Name)
                {
                    if (_saveService.PresetNameExists(newName))
                    {
                        _dialogService.ShowNotification(
                           ResourceHelper.GetString("Common_ErrorTitle"),
                           "A preset with this name already exists.",
                           DialogIconType.Error);
                        return;
                    }

                    _saveService.UpdatePreset(id, newName, item.Description);
                    RefreshPresetList();
                }
            }
        }
    }
}