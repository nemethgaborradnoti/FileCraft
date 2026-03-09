using FileCraft.Models;
using FileCraft.Services.Interfaces;
using FileCraft.Shared.Commands;
using FileCraft.Shared.Helpers;
using FileCraft.ViewModels.Shared;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Media;

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
        public string Id { get; }
        public string Name { get; }
        public string? Icon { get; }
        public Brush? IconBrush { get; }
        public FolderTreeManager? FolderTreeManager { get; }

        private bool _isLinked;
        public bool IsLinked
        {
            get => _isLinked;
            set
            {
                if (_isLinked != value)
                {
                    _isLinked = value;
                    OnPropertyChanged();
                }
            }
        }

        public TabItemViewModel(string id, string name, string? icon, Brush? iconBrush, FolderTreeManager? folderTreeManager)
        {
            Id = id;
            Name = name;
            Icon = icon;
            IconBrush = iconBrush;
            FolderTreeManager = folderTreeManager;
        }
    }

    public class LinkGroupViewModel : BaseViewModel
    {
        public string GroupId { get; }
        public ObservableCollection<TabItemViewModel> LinkedTabs { get; } = new();
        public IEnumerable<TabItemViewModel> RestOfTabs => LinkedTabs.Skip(1);

        public LinkGroupViewModel(string groupId)
        {
            GroupId = groupId;
        }
    }

    public class OptionsViewModel : BaseViewModel
    {
        private readonly ISaveService _saveService;
        private readonly IDialogService _dialogService;
        private readonly ISharedStateService _sharedStateService;
        private readonly IFolderTreeLinkService _folderTreeLinkService;

        public string Version
        {
            get
            {
                var version = System.Reflection.Assembly.GetEntryAssembly()?.GetName().Version;
                return version != null ? $"v{version.Major}.{version.Minor}.{version.Build}" : "Unknown Version";
            }
        }

        public FullscreenManager<OptionsFullscreenState> FullscreenManager { get; }

        public event Action? IgnoredFoldersChanged;
        public event Action? PresetCreateRequested;
        public event Action<int>? PresetLoadRequested;
        public event Action? CurrentSaveDeleteRequested;

        public ICommand DeleteCurrentSaveCommand { get; }
        public ICommand OpenSaveFolderCommand { get; }
        public ICommand CopyFolderTreeCommand { get; }
        public ICommand EditIgnoredFoldersCommand { get; }
        public ICommand LinkFolderTreesCommand { get; }
        public ICommand UnlinkFolderTreeGroupCommand { get; }

        public PresetListViewModel PresetListViewModel { get; }
        public ObservableCollection<TabItemViewModel> AllTabs { get; } = new();
        public ObservableCollection<LinkGroupViewModel> ActiveLinkGroups { get; } = new();

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
                    UpdateLinkRelatedState();
                    CommandManager.InvalidateRequerySuggested();
                }
            }
        }

        private TabItemViewModel? _selectedDestinationTab;
        public TabItemViewModel? SelectedDestinationTab
        {
            get => _selectedDestinationTab;
            set
            {
                if (_selectedDestinationTab != value)
                {
                    _selectedDestinationTab = value;
                    OnPropertyChanged();
                    CommandManager.InvalidateRequerySuggested();
                }
            }
        }

        public OptionsViewModel(
            ISaveService saveService,
            IDialogService dialogService,
            ISharedStateService sharedStateService,
            IFolderTreeLinkService folderTreeLinkService,
            FileContentExportViewModel fileContentExportVM,
            TreeGeneratorViewModel treeGeneratorVM,
            FolderContentExportViewModel folderContentExportVM)
        {
            _saveService = saveService;
            _dialogService = dialogService;
            _sharedStateService = sharedStateService;
            _folderTreeLinkService = folderTreeLinkService;

            _sharedStateService.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(ISharedStateService.IgnoredFolders))
                {
                    UpdateIgnoredFoldersText();
                }
            };

            FullscreenManager = new FullscreenManager<OptionsFullscreenState>(OptionsFullscreenState.None);

            PresetListViewModel = new PresetListViewModel();

            var saveData = _saveService.LoadSaveData();
            PresetListViewModel.InitializeSort(saveData.SavePresetSortBy, saveData.SavePresetIsDescending);

            PresetListViewModel.SaveNewRequested += () => PresetCreateRequested?.Invoke();
            PresetListViewModel.LoadItemRequested += OnPresetLoadItemRequested;
            PresetListViewModel.DeleteItemRequested += OnPresetDeleteItemRequested;
            PresetListViewModel.RenameItemRequested += OnPresetRenameItemRequested;
            PresetListViewModel.ViewItemDetailsRequested += OnPresetViewItemDetailsRequested;
            PresetListViewModel.SortChanged += OnSortChanged;

            RefreshPresetList();

            DeleteCurrentSaveCommand = new RelayCommand(_ => CurrentSaveDeleteRequested?.Invoke());
            OpenSaveFolderCommand = new RelayCommand(_ => OpenSaveFolder());
            CopyFolderTreeCommand = new RelayCommand(async _ => await CopyFolderTree(), _ => CanCopyFolderTree());
            EditIgnoredFoldersCommand = new RelayCommand(_ => EditIgnoredFolders());
            LinkFolderTreesCommand = new RelayCommand(_ => LinkFolderTrees(), _ => CanLinkFolderTrees());
            UnlinkFolderTreeGroupCommand = new RelayCommand(p => UnlinkFolderTreeGroup(p));

            _folderTreeLinkService.OnLinksChanged += UpdateLinkRelatedState;

            AllTabs.Add(new TabItemViewModel("None", ResourceHelper.GetString("Options_TabNone"), null, null, null));
            AllTabs.Add(new TabItemViewModel("FileContentExport", ResourceHelper.GetString("TabFileContentExport"), AppIcons.FileContentExport.Glyph, AppIcons.FileContentExport.Brush, fileContentExportVM.FolderTreeManager));
            AllTabs.Add(new TabItemViewModel("TreeGenerator", ResourceHelper.GetString("TabTreeGenerator"), AppIcons.TreeGenerator.Glyph, AppIcons.TreeGenerator.Brush, treeGeneratorVM.FolderTreeManager));
            AllTabs.Add(new TabItemViewModel("FolderContentExport", ResourceHelper.GetString("TabFolderContentExport"), AppIcons.FolderContentExport.Glyph, AppIcons.FolderContentExport.Brush, folderContentExportVM.FolderTreeManager));

            SelectedSourceTab = AllTabs[0];
            SelectedDestinationTab = AllTabs[0];

            UpdateIgnoredFoldersText();
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
                   SelectedSourceTab.Id != SelectedDestinationTab.Id &&
                   !string.IsNullOrWhiteSpace(_sharedStateService.SourcePath) &&
                   !SelectedSourceTab.IsLinked;
        }

        private async Task CopyFolderTree()
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
                await destManager.LoadTreeForPathAsync(_sharedStateService.SourcePath, sourceState);

                _dialogService.ShowNotification(
                    ResourceHelper.GetString("Common_SuccessTitle"),
                    ResourceHelper.GetString("Options_CopyTreeSuccess"),
                    DialogIconType.Success);

                SelectedSourceTab = AllTabs.First(t => t.Id == "None");
                SelectedDestinationTab = AllTabs.First(t => t.Id == "None");
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

        private void UpdateLinkRelatedState()
        {
            var allLinkGroups = _folderTreeLinkService.GetLinkGroups();
            var linkedIds = allLinkGroups.SelectMany(g => g).ToHashSet();

            foreach (var tab in AllTabs)
            {
                if (tab.FolderTreeManager != null)
                {
                    tab.IsLinked = linkedIds.Contains(tab.Id);
                }
            }

            ActiveLinkGroups.Clear();

            foreach (var group in allLinkGroups)
            {
                if (!group.Any()) continue;

                var groupVm = new LinkGroupViewModel(group.First());
                foreach (var id in group)
                {
                    var tabVm = AllTabs.FirstOrDefault(t => t.Id == id);
                    if (tabVm != null)
                    {
                        groupVm.LinkedTabs.Add(tabVm);
                    }
                }
                ActiveLinkGroups.Add(groupVm);
            }
        }

        private bool CanLinkFolderTrees()
        {
            if (SelectedSourceTab?.FolderTreeManager == null ||
                SelectedDestinationTab?.FolderTreeManager == null ||
                SelectedSourceTab.Id == SelectedDestinationTab.Id)
            {
                return false;
            }

            var peers = _folderTreeLinkService.GetLinkedPeers(SelectedSourceTab.Id);
            if (peers.Contains(SelectedDestinationTab.Id))
            {
                return false;
            }

            return true;
        }

        private void LinkFolderTrees()
        {
            if (CanLinkFolderTrees())
            {
                OnStateChanging();
                _folderTreeLinkService.CreateLink(SelectedSourceTab!.Id, SelectedDestinationTab!.Id);
                SelectedSourceTab = AllTabs.First(t => t.Id == "None");
                SelectedDestinationTab = AllTabs.First(t => t.Id == "None");
            }
        }

        private void UnlinkFolderTreeGroup(object? parameter)
        {
            if (parameter is string groupId)
            {
                OnStateChanging();
                _folderTreeLinkService.RemoveLink(groupId);
            }
        }
    }
}