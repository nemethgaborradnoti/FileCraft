using FileCraft.Shared.Commands;
using System;
using System.Windows.Input;

namespace FileCraft.ViewModels.Functional
{
    public class SettingsViewModel : BaseViewModel
    {
        public string Version => "v1.0.0";

        public event Action<int> PresetSaveRequested;
        public event Action<int> PresetLoadRequested;

        public ICommand SavePresetCommand { get; }
        public ICommand LoadPresetCommand { get; }

        public SettingsViewModel()
        {
            SavePresetCommand = new RelayCommand(presetNumber => PresetSaveRequested?.Invoke(Convert.ToInt32(presetNumber)));
            LoadPresetCommand = new RelayCommand(presetNumber => PresetLoadRequested?.Invoke(Convert.ToInt32(presetNumber)));
        }
    }
}
