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
    public class TabItemViewModel : BaseViewModel
    {
        public string Name { get; }
        public string? IconPath { get; }
        public FolderTreeManager? FolderTreeManager { get; }

        private bool _isSelectable = true;
        public bool IsSelectable
        {
            get => _isSelectable;
            set
            {
                if (_isSelectable != value)
                {
                    _isSelectable = value;
                    OnPropertyChanged();
                }
            }
        }

        public TabItemViewModel(string name, string? iconPath, FolderTreeManager? folderTreeManager)
        {
            Name = name;
            IconPath = iconPath;
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

        public event Action? IgnoredFoldersChanged;
        public event Action<int>? PresetSaveRequested;
        public event Action<int, string>? PresetRenameRequested;
        public event Action<int>? PresetLoadRequested;
        public event Action<int>? PresetDeleteRequested;
        public event Action? CurrentSaveDeleteRequested;

        public ICommand SavePresetCommand { get; }
        public ICommand EditPresetCommand { get; }
        public ICommand DeletePresetCommand { get; }
        public ICommand LoadPresetCommand { get; }
        public ICommand DeleteCurrentSaveCommand { get; }
        public ICommand OpenSaveFolderCommand { get; }
        public ICommand CopyFolderTreeCommand { get; }
        public ICommand EditIgnoredFoldersCommand { get; }

        public ObservableCollection<PresetSlotViewModel> PresetSlots { get; } = new();
        public ObservableCollection<TabItemViewModel> AllTabs { get; } = new();

        private string _ignoredFoldersText = "No folders are ignored.";
        public string IgnoredFoldersText
        {
            get => _ignoredFoldersText;
            private set
            {
                if (_ignoredFoldersText != value)
                {
                    _ignoredFoldersText = value;
                    OnPropertyChanged();
                }
            }
        }

        private TabItemViewModel? _selectedSourceTab;
        public TabItemViewModel? SelectedSourceTab
        {
            get => _selectedSourceTab;
            set
            {
                if (_selectedSourceTab != value)
                {
                    _selectedSourceTab = value;
                    OnPropertyChanged();

                    foreach (var tab in AllTabs)
                    {
                        tab.IsSelectable = true;
                    }

                    if (_selectedSourceTab != null && _selectedSourceTab.FolderTreeManager != null)
                    {
                        var tabToDisable = AllTabs.FirstOrDefault(t => t.Name == _selectedSourceTab.Name);
                        if (tabToDisable != null)
                        {
                            tabToDisable.IsSelectable = false;
                        }
                    }

                    if (SelectedDestinationTab != null && !SelectedDestinationTab.IsSelectable)
                    {
                        SelectedDestinationTab = AllTabs.First(t => t.FolderTreeManager == null);
                    }
                }
            }
        }

        private TabItemViewModel? _selectedDestinationTab;
        public TabItemViewModel? SelectedDestinationTab
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
            _sharedStateService.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(ISharedStateService.IgnoredFolders))
                {
                    UpdateIgnoredFoldersText();
                }
            };

            SavePresetCommand = new RelayCommand(
                execute: slot =>
                {
                    if (slot is PresetSlotViewModel vm)
                    {
                        PresetSaveRequested?.Invoke(vm.PresetNumber);
                    }
                });

            EditPresetCommand = new RelayCommand(
                execute: slot =>
                {
                    if (slot is PresetSlotViewModel vm)
                    {
                        string? newName = _dialogService.ShowRenamePresetDialog(vm.PresetName, vm.PresetNumber);
                        if (!string.IsNullOrWhiteSpace(newName))
                        {
                            PresetRenameRequested?.Invoke(vm.PresetNumber, newName);
                        }
                    }
                },
                canExecute: slot => slot is PresetSlotViewModel vm && vm.Exists);

            DeletePresetCommand = new RelayCommand(
                execute: slot =>
                {
                    if (slot is PresetSlotViewModel vm)
                    {
                        PresetDeleteRequested?.Invoke(vm.PresetNumber);
                    }
                },
                canExecute: slot => slot is PresetSlotViewModel vm && vm.Exists);

            LoadPresetCommand = new RelayCommand(
                execute: slot =>
                {
                    if (slot is PresetSlotViewModel vm)
                    {
                        PresetLoadRequested?.Invoke(vm.PresetNumber);
                    }
                },
                canExecute: slot => slot is PresetSlotViewModel vm && vm.Exists);


            DeleteCurrentSaveCommand = new RelayCommand(
                _ => CurrentSaveDeleteRequested?.Invoke());

            OpenSaveFolderCommand = new RelayCommand(_ => OpenSaveFolder());
            CopyFolderTreeCommand = new RelayCommand(_ => CopyFolderTree(), _ => CanCopyFolderTree());
            EditIgnoredFoldersCommand = new RelayCommand(_ => EditIgnoredFolders());

            for (int i = 1; i <= 5; i++)
            {
                PresetSlots.Add(new PresetSlotViewModel(i));
            }
            CheckForExistingPresets();

            AllTabs.Add(new TabItemViewModel("-- None --", null, null));
            AllTabs.Add(new TabItemViewModel("File Content Export", "/Resources/filecontent01.png", fileContentExportVM.FolderTreeManager));
            AllTabs.Add(new TabItemViewModel("Tree Generator", "/Resources/treestructure01.png", treeGeneratorVM.FolderTreeManager));
            AllTabs.Add(new TabItemViewModel("Folder Content Export", "/Resources/foldercontent01.png", folderContentExportVM.FolderTreeManager));

            SelectedSourceTab = AllTabs[0];
            SelectedDestinationTab = AllTabs[0];

            UpdateIgnoredFoldersText();
        }

        private void UpdateIgnoredFoldersText()
        {
            var ignoredFolders = _sharedStateService.IgnoredFolders;
            if (ignoredFolders == null || !ignoredFolders.Any())
            {
                IgnoredFoldersText = "No folders are ignored.";
            }
            else
            {
                IgnoredFoldersText = string.Join(", ", ignoredFolders);
            }
        }

        private void EditIgnoredFolders()
        {
            OnStateChanging();
            string currentFoldersText = string.Join(", ", _sharedStateService.IgnoredFolders);
            string? newFoldersText = _dialogService.ShowEditIgnoredFoldersDialog(currentFoldersText);

            if (newFoldersText != null)
            {
                _sharedStateService.IgnoredFolders = newFoldersText
                    .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(line => line.Trim())
                    .Where(line => !string.IsNullOrWhiteSpace(line))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(name => name)
                    .ToList();

                IgnoredFoldersChanged?.Invoke();
            }
        }

        private bool CanCopyFolderTree()
        {
            return SelectedSourceTab?.FolderTreeManager != null &&
                   SelectedDestinationTab?.FolderTreeManager != null &&
                   SelectedSourceTab != SelectedDestinationTab &&
                   !string.IsNullOrWhiteSpace(_sharedStateService.SourcePath);
        }

        private void CopyFolderTree()
        {
            if (!CanCopyFolderTree()) return;

            var sourceManager = SelectedSourceTab!.FolderTreeManager!;
            var destManager = SelectedDestinationTab!.FolderTreeManager!;

            int sourceFolderCount = sourceManager.GetSelectedNodeCount();
            int destFolderCount = destManager.GetSelectedNodeCount();

            bool confirmed = _dialogService.ShowCopyTreeConfirmation(
                title: "Copy Folder Tree",
                iconType: DialogIconType.Warning,
                sourceName: SelectedSourceTab.Name,
                sourceIcon: SelectedSourceTab.IconPath,
                sourceCount: sourceFolderCount,
                destName: SelectedDestinationTab.Name,
                destIcon: SelectedDestinationTab.IconPath,
                destCount: destFolderCount);

            if (confirmed)
            {
                OnStateChanging();
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
                    slot.PresetName = "-- Empty --";
                }
            }
        }
    }
}

