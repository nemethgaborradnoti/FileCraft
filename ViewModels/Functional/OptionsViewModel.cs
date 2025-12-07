using FileCraft.Models;
using FileCraft.Services.Interfaces;
using FileCraft.Shared.Commands;
using FileCraft.Shared.Helpers;
using FileCraft.ViewModels.Shared;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Windows.Input;

namespace FileCraft.ViewModels.Functional
{
    public enum OptionsFullscreenState
    {
        None,
        Presets,
        CopyTree,
        IgnoredFolders
    }

    public class TabItemViewModel : BaseViewModel
    {
        public string Name { get; }
        public string? Icon { get; }
        public Brush? IconBrush { get; }
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

        public TabItemViewModel(string name, string? icon, Brush? iconBrush, FolderTreeManager? folderTreeManager)
        {
            Name = name;
            Icon = icon;
            IconBrush = iconBrush;
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

        private DateTime? _lastModified;
        public DateTime? LastModified
        {
            get => _lastModified;
            set
            {
                if (_lastModified != value)
                {
                    _lastModified = value;
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

        public string Version => ResourceHelper.GetString("App_Version");

        public FullscreenManager<OptionsFullscreenState> FullscreenManager { get; }

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

        private string _ignoredFoldersText = ResourceHelper.GetString("Options_IgnoredFoldersNone");
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

            FullscreenManager = new FullscreenManager<OptionsFullscreenState>(OptionsFullscreenState.None);

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

            // Setup Icons using AppIcons
            AllTabs.Add(new TabItemViewModel(ResourceHelper.GetString("Options_TabNone"), null, null, null));
            AllTabs.Add(new TabItemViewModel(ResourceHelper.GetString("TabFileContentExport"), AppIcons.FileContentExport.Glyph, AppIcons.FileContentExport.Brush, fileContentExportVM.FolderTreeManager));
            AllTabs.Add(new TabItemViewModel(ResourceHelper.GetString("TabTreeGenerator"), AppIcons.TreeGenerator.Glyph, AppIcons.TreeGenerator.Brush, treeGeneratorVM.FolderTreeManager));
            AllTabs.Add(new TabItemViewModel(ResourceHelper.GetString("TabFolderContentExport"), AppIcons.FolderContentExport.Glyph, AppIcons.FolderContentExport.Brush, folderContentExportVM.FolderTreeManager));

            SelectedSourceTab = AllTabs[0];
            SelectedDestinationTab = AllTabs[0];

            UpdateIgnoredFoldersText();
        }

        private void UpdateIgnoredFoldersText()
        {
            var ignoredFolders = _sharedStateService.IgnoredFolders;
            if (ignoredFolders == null || !ignoredFolders.Any())
            {
                IgnoredFoldersText = ResourceHelper.GetString("Options_IgnoredFoldersNone");
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

            var contentViewModel = new CopyTreeConfirmationViewModel
            {
                SourceTabName = SelectedSourceTab.Name,
                SourceTabIcon = SelectedSourceTab.Icon,
                SourceTabIconBrush = SelectedSourceTab.IconBrush,
                SourceFolderCount = sourceFolderCount,
                DestinationTabName = SelectedDestinationTab.Name,
                DestinationTabIcon = SelectedDestinationTab.Icon,
                DestinationTabIconBrush = SelectedDestinationTab.IconBrush,
                DestinationFolderCount = destFolderCount
            };

            bool confirmed = _dialogService.ShowCopyTreeConfirmation(
                ResourceHelper.GetString("Options_CopyTreeTitle"),
                DialogIconType.Warning,
                contentViewModel);

            if (confirmed)
            {
                OnStateChanging();
                var sourceState = sourceManager.GetFolderStates();
                destManager.LoadTreeForPath(_sharedStateService.SourcePath, sourceState);
                SelectedSourceTab = AllTabs[0];
                SelectedDestinationTab = AllTabs[0];
                _dialogService.ShowNotification(
                    ResourceHelper.GetString("Common_SuccessTitle"),
                    ResourceHelper.GetString("Options_CopyTreeSuccess"),
                    DialogIconType.Success);
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
                    _dialogService.ShowNotification(
                        ResourceHelper.GetString("Common_ErrorTitle"),
                        ResourceHelper.GetString("Options_OpenSaveError_NotFound"),
                        DialogIconType.Error);
                }
            }
            catch (Exception ex)
            {
                _dialogService.ShowNotification(
                    ResourceHelper.GetString("Common_ErrorTitle"),
                    string.Format(ResourceHelper.GetString("Options_OpenSaveError_Exception"), ex.Message),
                    DialogIconType.Error);
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
                    slot.LastModified = _saveService.GetPresetLastModifiedDate(slot.PresetNumber);
                    if (string.IsNullOrWhiteSpace(slot.PresetName))
                    {
                        slot.PresetName = ResourceHelper.GetString("Options_UnnamedPreset");
                    }
                }
                else
                {
                    slot.PresetName = ResourceHelper.GetString("Options_EmptyPreset");
                    slot.LastModified = null;
                }
            }
        }
    }
}