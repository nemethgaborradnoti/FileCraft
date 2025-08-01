using FileCraft.Services.Interfaces;
using FileCraft.Shared.Commands;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
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
        private readonly IDialogService _dialogService;
        public string Version => "v1.0.0";

        public event Action<int, string>? PresetSaveRequested;
        public event Action<int>? PresetLoadRequested;
        public event Action<int>? PresetDeleteRequested;
        public event Action? CurrentSaveDeleteRequested;

        public ICommand SavePresetCommand { get; }
        public ICommand LoadPresetCommand { get; }
        public ICommand DeletePresetCommand { get; }
        public ICommand DeleteCurrentSaveCommand { get; }
        public ICommand OpenSaveFolderCommand { get; }

        public ObservableCollection<PresetSlotViewModel> PresetSlots { get; } = new();

        public OptionsViewModel(ISaveService saveService, IDialogService dialogService)
        {
            _saveService = saveService;
            _dialogService = dialogService;

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

            DeletePresetCommand = new RelayCommand(
                presetNumber => PresetDeleteRequested?.Invoke(Convert.ToInt32(presetNumber)));

            DeleteCurrentSaveCommand = new RelayCommand(
                _ => CurrentSaveDeleteRequested?.Invoke());

            OpenSaveFolderCommand = new RelayCommand(_ => OpenSaveFolder());


            for (int i = 1; i <= 5; i++)
            {
                PresetSlots.Add(new PresetSlotViewModel(i));
            }
            CheckForExistingPresets();
        }

        private void OpenSaveFolder()
        {
            try
            {
                string path = _saveService.GetSaveDirectory();
                if (Directory.Exists(path))
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = path,
                        UseShellExecute = true,
                        Verb = "open"
                    });
                }
                else
                {
                    _dialogService.ShowNotification("Error", "The save folder could not be found.", Models.DialogIconType.Error);
                }
            }
            catch (Exception ex)
            {
                _dialogService.ShowNotification("Error", $"An error occurred while trying to open the folder:\n{ex.Message}", Models.DialogIconType.Error);
            }
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
