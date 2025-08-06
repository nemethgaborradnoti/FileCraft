using FileCraft.Models;
using FileCraft.Services.Interfaces;
using FileCraft.Shared.Commands;
using FileCraft.ViewModels.Shared;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows.Input;

namespace FileCraft.ViewModels.Functional
{
    public class TabInfo
    {
        public string Name { get; set; }
        public FolderTreeManager FolderTreeManager { get; set; }

        public TabInfo(string name, FolderTreeManager folderTreeManager)
        {
            Name = name;
            FolderTreeManager = folderTreeManager;
        }
    }

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
        private readonly ISharedStateService _sharedStateService;
        public string Version => "v2.0.0";

        public event Action<int, string>? PresetSaveRequested;
        public event Action<int>? PresetLoadRequested;
        public event Action<int>? PresetDeleteRequested;
        public event Action? CurrentSaveDeleteRequested;

        public ICommand SavePresetCommand { get; }
        public ICommand LoadPresetCommand { get; }
        public ICommand DeletePresetCommand { get; }
        public ICommand DeleteCurrentSaveCommand { get; }
        public ICommand OpenSaveFolderCommand { get; }
        public ICommand CopyFolderTreeCommand { get; }

        public ObservableCollection<PresetSlotViewModel> PresetSlots { get; } = new();
        public ObservableCollection<TabInfo> AvailableTabs { get; } = new();
        public ObservableCollection<TabInfo> AvailableDestinationTabs { get; } = new();

        private TabInfo? _selectedSourceTab;
        public TabInfo? SelectedSourceTab
        {
            get => _selectedSourceTab;
            set
            {
                if (_selectedSourceTab != value)
                {
                    _selectedSourceTab = value;
                    OnPropertyChanged();
                    UpdateAvailableDestinationTabs();
                }
            }
        }

        private TabInfo? _selectedDestinationTab;
        public TabInfo? SelectedDestinationTab
        {
            get => _selectedDestinationTab;
            set
            {
                _selectedDestinationTab = value;
                OnPropertyChanged();
            }
        }

        public OptionsViewModel(
            ISaveService saveService,
            IDialogService dialogService,
            ISharedStateService sharedStateService,
            FileContentExportViewModel fileContentExportVM,
            TreeGeneratorViewModel treeGeneratorVM,
            FolderContentExportViewModel folderContentExportVM)
        {
            _saveService = saveService;
            _dialogService = dialogService;
            _sharedStateService = sharedStateService;

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
            CopyFolderTreeCommand = new RelayCommand(_ => CopyFolderTree(), _ => CanCopyFolderTree());

            for (int i = 1; i <= 5; i++)
            {
                PresetSlots.Add(new PresetSlotViewModel(i));
            }
            CheckForExistingPresets();

            AvailableTabs.Add(new TabInfo("File Content Export", fileContentExportVM.FolderTreeManager));
            AvailableTabs.Add(new TabInfo("Tree Generator", treeGeneratorVM.FolderTreeManager));
            AvailableTabs.Add(new TabInfo("Folder Content Export", folderContentExportVM.FolderTreeManager));

            UpdateAvailableDestinationTabs();
        }

        private void UpdateAvailableDestinationTabs()
        {
            var currentDestination = SelectedDestinationTab;
            AvailableDestinationTabs.Clear();

            var filteredTabs = SelectedSourceTab == null
                ? AvailableTabs
                : AvailableTabs.Where(t => t != SelectedSourceTab);

            foreach (var tab in filteredTabs)
            {
                AvailableDestinationTabs.Add(tab);
            }

            if (SelectedDestinationTab != null && SelectedDestinationTab == SelectedSourceTab)
            {
                SelectedDestinationTab = null;
            }
        }

        private bool CanCopyFolderTree()
        {
            return SelectedSourceTab != null &&
                   SelectedDestinationTab != null &&
                   SelectedSourceTab != SelectedDestinationTab &&
                   !string.IsNullOrWhiteSpace(_sharedStateService.SourcePath);
        }

        private void CopyFolderTree()
        {
            if (!CanCopyFolderTree()) return;

            OnStateChanging();

            var sourceManager = SelectedSourceTab!.FolderTreeManager;
            var destManager = SelectedDestinationTab!.FolderTreeManager;

            int sourceFolderCount = sourceManager.GetSelectedNodeCount();
            int destFolderCount = destManager.GetSelectedNodeCount();

            string message = $"Are you sure you want to copy {SelectedSourceTab.Name} tab's folder tree ({sourceFolderCount} folders) to {SelectedDestinationTab.Name} tab's folder tree ({destFolderCount} folders)?";

            bool confirmed = _dialogService.ShowConfirmation(
                title: "Copy Folder Tree",
                message: message,
                iconType: DialogIconType.Warning);

            if (confirmed)
            {
                var sourceState = sourceManager.GetFolderStates();
                destManager.LoadTreeForPath(_sharedStateService.SourcePath, sourceState);
                _dialogService.ShowNotification("Success", "Folder tree copied successfully.", DialogIconType.Success);
            }
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
