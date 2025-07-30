using FileCraft.Services.Interfaces;
using FileCraft.Shared.Commands;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace FileCraft.ViewModels.Functional
{
    public class PresetSlotViewModel : BaseViewModel
    {
        public int PresetNumber { get; }

        private bool _exists;
        public bool Exists
        {
            get => _exists;
            set
            {
                if (_exists != value)
                {
                    _exists = value;
                    OnPropertyChanged();
                }
            }
        }

        private string _presetName = string.Empty;
        public string PresetName
        {
            get => _presetName;
            set
            {
                if (_presetName != value)
                {
                    _presetName = value;
                    OnPropertyChanged();
                }
            }
        }

        private string _inputName = string.Empty;
        public string InputName
        {
            get => _inputName;
            set
            {
                if (_inputName != value)
                {
                    _inputName = value;
                    OnPropertyChanged();
                }
            }
        }

        public PresetSlotViewModel(int number)
        {
            PresetNumber = number;
        }
    }


    public class OptionsViewModel : BaseViewModel
    {
        private readonly ISaveService _saveService;
        public string Version => "v1.0.0";

        public event Action<int, string>? PresetSaveRequested;
        public event Action<int>? PresetLoadRequested;

        public ICommand SavePresetCommand { get; }
        public ICommand LoadPresetCommand { get; }

        public ObservableCollection<PresetSlotViewModel> PresetSlots { get; } = new();

        public OptionsViewModel(ISaveService saveService)
        {
            _saveService = saveService;

            SavePresetCommand = new RelayCommand(
                execute: slot =>
                {
                    if (slot is PresetSlotViewModel vm)
                    {
                        PresetSaveRequested?.Invoke(vm.PresetNumber, vm.InputName);
                        vm.InputName = string.Empty;
                    }
                },
                canExecute: slot =>
                {
                    return slot is PresetSlotViewModel vm && !string.IsNullOrWhiteSpace(vm.InputName);
                });


            LoadPresetCommand = new RelayCommand(
                presetNumber => PresetLoadRequested?.Invoke(Convert.ToInt32(presetNumber)));

            for (int i = 1; i <= 5; i++)
            {
                PresetSlots.Add(new PresetSlotViewModel(i));
            }
            CheckForExistingPresets();
        }

        public void CheckForExistingPresets()
        {
            foreach (var slot in PresetSlots)
            {
                slot.Exists = _saveService.CheckPresetExists(slot.PresetNumber);
                if (slot.Exists)
                {
                    slot.PresetName = _saveService.GetPresetName(slot.PresetNumber);
                    if (string.IsNullOrWhiteSpace(slot.PresetName))
                    {
                        slot.PresetName = "Unnamed Preset";
                    }
                }
                else
                {
                    slot.PresetName = "Empty Slot";
                }
            }
        }
    }
}
