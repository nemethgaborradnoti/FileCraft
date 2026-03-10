using FileCraft.ViewModels.Shared;
using System;

namespace FileCraft.ViewModels.Functional
{
    public enum OptionsFullscreenState
    {
        None,
        Presets,
        CopyTree,
        IgnoredFolders
    }

    public class OptionsViewModel : BaseViewModel
    {
        public OptionsPresetsViewModel PresetsVM { get; }
        public OptionsTreeToolsViewModel TreeToolsVM { get; }
        public OptionsGeneralViewModel GeneralVM { get; }
        public FullscreenManager<OptionsFullscreenState> FullscreenManager { get; }

        public event Action? IgnoredFoldersChanged;
        public event Action? PresetCreateRequested;
        public event Action<int>? PresetLoadRequested;
        public event Action<int, string>? PresetOverwriteRequested;
        public event Action? CurrentSaveDeleteRequested;

        public OptionsViewModel(
            OptionsPresetsViewModel presetsVM,
            OptionsTreeToolsViewModel treeToolsVM,
            OptionsGeneralViewModel generalVM)
        {
            PresetsVM = presetsVM;
            TreeToolsVM = treeToolsVM;
            GeneralVM = generalVM;
            FullscreenManager = new FullscreenManager<OptionsFullscreenState>(OptionsFullscreenState.None);

            GeneralVM.IgnoredFoldersChanged += () => IgnoredFoldersChanged?.Invoke();
            GeneralVM.CurrentSaveDeleteRequested += () => CurrentSaveDeleteRequested?.Invoke();
            PresetsVM.PresetCreateRequested += () => PresetCreateRequested?.Invoke();
            PresetsVM.PresetLoadRequested += (id) => PresetLoadRequested?.Invoke(id);
            PresetsVM.PresetOverwriteRequested += (id, name) => PresetOverwriteRequested?.Invoke(id, name);

            PresetsVM.StateChanging += OnStateChangingDelegate;
            TreeToolsVM.StateChanging += OnStateChangingDelegate;
            GeneralVM.StateChanging += OnStateChangingDelegate;
        }

        public void RefreshPresetList()
        {
            PresetsVM.RefreshPresetList();
        }

        private void OnStateChangingDelegate()
        {
            OnStateChanging();
        }
    }
}