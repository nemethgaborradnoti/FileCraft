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
                _exists = value;
                OnPropertyChanged();
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

        public event Action<int>? PresetSaveRequested;
        public event Action<int>? PresetLoadRequested;

        public ICommand SavePresetCommand { get; }
        public ICommand LoadPresetCommand { get; }

        public ObservableCollection<PresetSlotViewModel> LoadPresetSlots { get; } = new();

        public OptionsViewModel(ISaveService saveService)
        {
            _saveService = saveService;

            SavePresetCommand = new RelayCommand(presetNumber => PresetSaveRequested?.Invoke(Convert.ToInt32(presetNumber)));
            LoadPresetCommand = new RelayCommand(presetNumber => PresetLoadRequested?.Invoke(Convert.ToInt32(presetNumber)));

            for (int i = 1; i <= 5; i++)
            {
                LoadPresetSlots.Add(new PresetSlotViewModel(i));
            }
            CheckForExistingPresets();
        }

        public void CheckForExistingPresets()
        {
            foreach (var slot in LoadPresetSlots)
            {
                slot.Exists = _saveService.CheckPresetExists(slot.PresetNumber);
            }
        }
    }
}
