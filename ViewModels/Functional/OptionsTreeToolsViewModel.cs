using FileCraft.Models;
using FileCraft.Services.Interfaces;
using FileCraft.Shared.Commands;
using FileCraft.Shared.Helpers;
using FileCraft.ViewModels.Shared;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Media;

namespace FileCraft.ViewModels.Functional
{
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
        public System.Collections.Generic.IEnumerable<TabItemViewModel> RestOfTabs => LinkedTabs.Skip(1);

        public LinkGroupViewModel(string groupId)
        {
            GroupId = groupId;
        }
    }

    public class OptionsTreeToolsViewModel : BaseViewModel
    {
        private readonly IDialogService _dialogService;
        private readonly ISharedStateService _sharedStateService;
        private readonly IFolderTreeLinkService _folderTreeLinkService;

        public ObservableCollection<TabItemViewModel> AllTabs { get; } = new();
        public ObservableCollection<LinkGroupViewModel> ActiveLinkGroups { get; } = new();

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

        public ICommand CopyFolderTreeCommand { get; }
        public ICommand LinkFolderTreesCommand { get; }
        public ICommand UnlinkFolderTreeGroupCommand { get; }

        public OptionsTreeToolsViewModel(
            IDialogService dialogService,
            ISharedStateService sharedStateService,
            IFolderTreeLinkService folderTreeLinkService,
            FileContentExportViewModel fileContentExportVM,
            TreeGeneratorViewModel treeGeneratorVM,
            FolderContentExportViewModel folderContentExportVM)
        {
            _dialogService = dialogService;
            _sharedStateService = sharedStateService;
            _folderTreeLinkService = folderTreeLinkService;

            CopyFolderTreeCommand = new RelayCommand(async _ => await CopyFolderTree(), _ => CanCopyFolderTree());
            LinkFolderTreesCommand = new RelayCommand(_ => LinkFolderTrees(), _ => CanLinkFolderTrees());
            UnlinkFolderTreeGroupCommand = new RelayCommand(p => UnlinkFolderTreeGroup(p));

            _folderTreeLinkService.OnLinksChanged += UpdateLinkRelatedState;

            AllTabs.Add(new TabItemViewModel("None", ResourceHelper.GetString("Options_TabNone"), null, null, null));
            AllTabs.Add(new TabItemViewModel("FileContentExport", ResourceHelper.GetString("TabFileContentExport"), AppIcons.FileContentExport.Glyph, AppIcons.FileContentExport.Brush, fileContentExportVM.FolderTreeManager));
            AllTabs.Add(new TabItemViewModel("TreeGenerator", ResourceHelper.GetString("TabTreeGenerator"), AppIcons.TreeGenerator.Glyph, AppIcons.TreeGenerator.Brush, treeGeneratorVM.FolderTreeManager));
            AllTabs.Add(new TabItemViewModel("FolderContentExport", ResourceHelper.GetString("TabFolderContentExport"), AppIcons.FolderContentExport.Glyph, AppIcons.FolderContentExport.Brush, folderContentExportVM.FolderTreeManager));

            SelectedSourceTab = AllTabs[0];
            SelectedDestinationTab = AllTabs[0];
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