using FileCraft.Models;
using FileCraft.Services.Interfaces;
using FileCraft.Shared.Commands;
using FileCraft.Shared.Helpers;
using FileCraft.ViewModels.Functional;
using System.ComponentModel;
using System.IO;
using System.Windows.Input;

namespace FileCraft.ViewModels
{
    public class MainViewModel : BaseViewModel
    {
        private readonly ISaveService _saveService;
        private readonly ISharedStateService _sharedStateService;
        private readonly IDialogService _dialogService;
        private readonly IUndoService _undoService;
        private bool _isLoading = false;

        private bool _hasUnsavedChanges;
        public bool HasUnsavedChanges
        {
            get => _hasUnsavedChanges;
            set
            {
                if (_hasUnsavedChanges != value)
                {
                    _hasUnsavedChanges = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool CanUndo => _undoService.CanUndo;
        public bool CanRedo => _undoService.CanRedo;
        public bool CanClearPaths => !string.IsNullOrWhiteSpace(SourcePath) || !string.IsNullOrWhiteSpace(DestinationPath);

        public string SourcePath => _sharedStateService.SourcePath;
        public string DestinationPath => _sharedStateService.DestinationPath;

        private int _selectedTabIndex;
        public int SelectedTabIndex
        {
            get => _selectedTabIndex;
            set
            {
                if (_selectedTabIndex != value)
                {
                    _selectedTabIndex = value;
                    OnPropertyChanged();
                }
            }
        }

        public FileContentExportViewModel FileContentExportVM { get; }
        public TreeGeneratorViewModel TreeGeneratorVM { get; }
        public FolderContentExportViewModel FolderContentExportVM { get; }
        public OptionsViewModel OptionsVM { get; }

        public ICommand ClearPathsCommand { get; }
        public ICommand SelectSourcePathCommand { get; }
        public ICommand SelectDestinationPathCommand { get; }
        public ICommand SaveCommand { get; }
        public ICommand UndoCommand { get; }
        public ICommand RedoCommand { get; }

        public MainViewModel(
            ISaveService saveService,
            ISharedStateService sharedStateService,
            IDialogService dialogService,
            IUndoService undoService,
            FileContentExportViewModel fileContentExportVM,
            TreeGeneratorViewModel treeGeneratorVM,
            FolderContentExportViewModel folderContentExportVM,
            OptionsViewModel optionsVM)
        {
            _saveService = saveService;
            _sharedStateService = sharedStateService;
            _dialogService = dialogService;
            _undoService = undoService;

            FileContentExportVM = fileContentExportVM;
            TreeGeneratorVM = treeGeneratorVM;
            FolderContentExportVM = folderContentExportVM;
            OptionsVM = optionsVM;

            SubscribeToChanges();

            OptionsVM.IgnoredFoldersChanged += OnIgnoredFoldersChanged;
            OptionsVM.PresetSaveRequested += OnPresetSaveRequested;
            OptionsVM.PresetRenameRequested += OnPresetRenameRequested;
            OptionsVM.PresetLoadRequested += OnPresetLoadRequested;
            OptionsVM.PresetDeleteRequested += OnPresetDeleteRequested;
            OptionsVM.CurrentSaveDeleteRequested += OnCurrentSaveDeleteRequested;

            ClearPathsCommand = new RelayCommand(_ => ClearPaths(), _ => CanClearPaths);
            SelectSourcePathCommand = new RelayCommand(_ => SelectPath(isSource: true));
            SelectDestinationPathCommand = new RelayCommand(_ => SelectPath(isSource: false));
            SaveCommand = new RelayCommand(_ => Save(), _ => HasUnsavedChanges);
            UndoCommand = new RelayCommand(_ => Undo(), _ => CanUndo);
            RedoCommand = new RelayCommand(_ => Redo(), _ => CanRedo);

            LoadData();
            HasUnsavedChanges = false;
        }

        private void OnIgnoredFoldersChanged()
        {
            FileContentExportVM.FolderTreeManager.RefreshTree();
            TreeGeneratorVM.FolderTreeManager.RefreshTree();
            FolderContentExportVM.FolderTreeManager.RefreshTree();
        }

        private void SubscribeToChanges()
        {
            FileContentExportVM.StateChanging += OnStateChanging;
            TreeGeneratorVM.StateChanging += OnStateChanging;
            FolderContentExportVM.StateChanging += OnStateChanging;
            OptionsVM.StateChanging += OnStateChanging;

            _undoService.HistoryChanged += OnHistoryChanged;
        }

        private void OnHistoryChanged()
        {
            OnPropertyChanged(nameof(CanUndo));
            OnPropertyChanged(nameof(CanRedo));
        }

        private void Undo()
        {
            if (!CanUndo) return;
            _isLoading = true;
            var currentState = GetCurrentSaveData();
            var previousState = _undoService.Undo(currentState);
            ApplyAllData(previousState);
            HasUnsavedChanges = true;
            _isLoading = false;
        }

        private void Redo()
        {
            if (!CanRedo) return;
            _isLoading = true;
            var currentState = GetCurrentSaveData();
            var nextState = _undoService.Redo(currentState);
            ApplyAllData(nextState);
            HasUnsavedChanges = true;
            _isLoading = false;
        }

        private void SelectPath(bool isSource)
        {
            OnStateChanging();
            var title = isSource ? ResourceHelper.GetString("MainVM_SelectSourceFolder") : ResourceHelper.GetString("MainVM_SelectDestFolder");
            var selectedPath = _dialogService.SelectFolder(title);
            if (!string.IsNullOrEmpty(selectedPath))
            {
                if (isSource)
                {
                    _sharedStateService.SourcePath = selectedPath;
                    FileContentExportVM.FolderTreeManager.LoadTreeForPath(selectedPath);
                    TreeGeneratorVM.FolderTreeManager.LoadTreeForPath(selectedPath);
                    FolderContentExportVM.FolderTreeManager.LoadTreeForPath(selectedPath);
                }
                else
                {
                    _sharedStateService.DestinationPath = selectedPath;
                }
                OnPropertyChanged(isSource ? nameof(SourcePath) : nameof(DestinationPath));
                OnPropertyChanged(nameof(CanClearPaths));
            }
        }

        private void ClearPaths()
        {
            OnStateChanging();
            _sharedStateService.SourcePath = string.Empty;
            _sharedStateService.DestinationPath = string.Empty;

            FileContentExportVM.FolderTreeManager.LoadTreeForPath(string.Empty);
            TreeGeneratorVM.FolderTreeManager.LoadTreeForPath(string.Empty);
            FolderContentExportVM.FolderTreeManager.LoadTreeForPath(string.Empty);

            OnPropertyChanged(nameof(SourcePath));
            OnPropertyChanged(nameof(DestinationPath));
            OnPropertyChanged(nameof(CanClearPaths));
        }

        private void LoadData()
        {
            _isLoading = true;
            _undoService.Clear();
            SaveData saveData = _saveService.LoadSaveData();
            ApplyAllData(saveData);
            SelectedTabIndex = saveData.SelectedTabIndex;
            _isLoading = false;
        }

        public void Save()
        {
            var saveData = GetCurrentSaveData();

            var dataToPersist = new SaveData
            {
                SourcePath = saveData.SourcePath,
                DestinationPath = saveData.DestinationPath,
                SelectedTabIndex = saveData.SelectedTabIndex,
                FileContentExport = saveData.FileContentExport,
                FolderContentExport = saveData.FolderContentExport,
                TreeGenerator = saveData.TreeGenerator,
                SettingsPage = saveData.SettingsPage
            };

            _saveService.Save(dataToPersist);
            HasUnsavedChanges = false;
            _dialogService.ShowNotification(
                ResourceHelper.GetString("Common_SuccessTitle"),
                ResourceHelper.GetString("MainVM_SettingsSaved"),
                DialogIconType.Success);
        }

        public void RequestClose(CancelEventArgs e)
        {
            if (!HasUnsavedChanges)
            {
                return;
            }

            var result = _dialogService.ShowExitConfirmation(
                ResourceHelper.GetString("MainVM_UnsavedChangesTitle"),
                ResourceHelper.GetString("MainVM_UnsavedChangesMessage"));

            switch (result)
            {
                case ExitConfirmationResult.Save:
                    Save();
                    break;
                case ExitConfirmationResult.DontSave:
                    break;
                case ExitConfirmationResult.Cancel:
                    e.Cancel = true;
                    break;
            }
        }

        private SaveData GetCurrentSaveData()
        {
            return new SaveData
            {
                SourcePath = this.SourcePath,
                DestinationPath = this.DestinationPath,
                SelectedTabIndex = this.SelectedTabIndex,
                FileContentExport = new FileContentExportSettings
                {
                    OutputFileName = FileContentExportVM.OutputFileName,
                    AppendTimestamp = FileContentExportVM.AppendTimestamp,
                    SelectedExtensions = FileContentExportVM.GetSelectedExtensions(),
                    SelectedFilePaths = FileContentExportVM.GetSelectedFilePaths(),
                    IgnoredCommentFilePaths = FileContentExportVM.GetIgnoredCommentFilePaths(),
                    FolderTreeState = FileContentExportVM.FolderTreeManager.GetFolderStates()
                },
                FolderContentExport = new FolderContentExportSettings
                {
                    OutputFileName = FolderContentExportVM.OutputFileName,
                    AppendTimestamp = FolderContentExportVM.AppendTimestamp,
                    SelectedColumns = FolderContentExportVM.GetSelectedColumns(),
                    FolderTreeState = FolderContentExportVM.FolderTreeManager.GetFolderStates()
                },
                TreeGenerator = new TreeGeneratorSettings
                {
                    OutputFileName = TreeGeneratorVM.OutputFileName,
                    AppendTimestamp = TreeGeneratorVM.AppendTimestamp,
                    FolderTreeState = TreeGeneratorVM.FolderTreeManager.GetFolderStates(),
                    GenerationMode = TreeGeneratorVM.IsStructuredTree ? TreeGenerationMode.Structured : TreeGenerationMode.PathsOnly
                },
                SettingsPage = new SettingsPageSettings
                {
                    IgnoredFolders = _sharedStateService.IgnoredFolders
                }
            };
        }

        private void ApplyAllData(SaveData saveData)
        {
            _isLoading = true;
            _sharedStateService.SourcePath = saveData.SourcePath;
            _sharedStateService.DestinationPath = saveData.DestinationPath;
            _sharedStateService.IgnoredFolders = saveData.SettingsPage.IgnoredFolders ?? new();

            OnPropertyChanged(nameof(SourcePath));
            OnPropertyChanged(nameof(DestinationPath));
            OnPropertyChanged(nameof(CanClearPaths));

            FileContentExportVM.FolderTreeManager.LoadTreeForPath(SourcePath, saveData.FileContentExport.FolderTreeState);
            FileContentExportVM.ApplySettings(saveData.FileContentExport);

            FolderContentExportVM.FolderTreeManager.LoadTreeForPath(SourcePath, saveData.FolderContentExport.FolderTreeState);
            FolderContentExportVM.ApplySettings(saveData.FolderContentExport);

            TreeGeneratorVM.FolderTreeManager.LoadTreeForPath(SourcePath, saveData.TreeGenerator.FolderTreeState);
            TreeGeneratorVM.OutputFileName = saveData.TreeGenerator.OutputFileName;
            TreeGeneratorVM.AppendTimestamp = saveData.TreeGenerator.AppendTimestamp;

            if (saveData.TreeGenerator.GenerationMode == TreeGenerationMode.PathsOnly)
            {
                TreeGeneratorVM.IsPathsOnly = true;
            }
            else
            {
                TreeGeneratorVM.IsStructuredTree = true;
            }

            SelectedTabIndex = saveData.SelectedTabIndex;
            _isLoading = false;
        }

        private void OnPresetSaveRequested(int presetNumber)
        {
            if (string.IsNullOrWhiteSpace(SourcePath) || !Directory.Exists(SourcePath))
            {
                _dialogService.ShowNotification(
                    ResourceHelper.GetString("Common_WarningTitle"),
                    ResourceHelper.GetString("Preset_SelectSourceFirst"),
                    DialogIconType.Warning);
                return;
            }

            bool exists = _saveService.CheckPresetExists(presetNumber);
            string presetName = exists ? _saveService.GetPresetName(presetNumber) : $"{ResourceHelper.GetString("Preset_DefaultNamePrefix")}{presetNumber:00}";

            if (exists)
            {
                string title = string.Format(ResourceHelper.GetString("Preset_OverwriteTitle"), presetNumber);
                string message = string.Format(ResourceHelper.GetString("Preset_OverwriteMessage"), presetNumber, presetName);

                bool confirmed = _dialogService.ShowConfirmation(
                    title: title,
                    message: message,
                    iconType: DialogIconType.Info);

                if (!confirmed) return;
            }

            try
            {
                var currentSaveData = GetCurrentSaveData();
                currentSaveData.PresetName = presetName;

                var relativeSaveData = MakePathsRelative(currentSaveData, SourcePath);
                _saveService.SaveAsPreset(relativeSaveData, presetNumber);

                OptionsVM.CheckForExistingPresets();

                if (!exists)
                {
                    string? newName = _dialogService.ShowRenamePresetDialog(presetName, presetNumber);
                    if (!string.IsNullOrWhiteSpace(newName) && newName != presetName)
                    {
                        _saveService.UpdatePresetName(presetNumber, newName);
                        OptionsVM.CheckForExistingPresets();
                        _dialogService.ShowNotification(
                            ResourceHelper.GetString("Common_SuccessTitle"),
                            string.Format(ResourceHelper.GetString("Preset_SavedAndRenamed"), newName),
                            DialogIconType.Success);
                        return;
                    }
                }

                _dialogService.ShowNotification(
                    ResourceHelper.GetString("Common_SuccessTitle"),
                    string.Format(ResourceHelper.GetString("Preset_SavedSuccess"), presetName, presetNumber),
                    DialogIconType.Success);
            }
            catch (System.Exception ex)
            {
                _dialogService.ShowNotification(
                    ResourceHelper.GetString("Common_ErrorTitle"),
                    string.Format(ResourceHelper.GetString("Preset_SaveError"), presetNumber, ex.Message),
                    DialogIconType.Error);
            }
        }

        private void OnPresetRenameRequested(int presetNumber, string newName)
        {
            try
            {
                _saveService.UpdatePresetName(presetNumber, newName);
                OptionsVM.CheckForExistingPresets();
                _dialogService.ShowNotification(
                    ResourceHelper.GetString("Common_SuccessTitle"),
                    string.Format(ResourceHelper.GetString("Preset_RenamedSuccess"), presetNumber, newName),
                    DialogIconType.Success);
            }
            catch (Exception ex)
            {
                _dialogService.ShowNotification(
                    ResourceHelper.GetString("Common_ErrorTitle"),
                    string.Format(ResourceHelper.GetString("Preset_RenameError"), presetNumber, ex.Message),
                    DialogIconType.Error);
            }
        }

        private void OnPresetLoadRequested(int presetNumber)
        {
            if (string.IsNullOrWhiteSpace(SourcePath) || !Directory.Exists(SourcePath))
            {
                _dialogService.ShowNotification(
                    ResourceHelper.GetString("Common_WarningTitle"),
                    ResourceHelper.GetString("Preset_LoadSelectSourceFirst"),
                    DialogIconType.Warning);
                return;
            }

            var relativePresetData = _saveService.LoadFromPreset(presetNumber);
            if (relativePresetData == null)
            {
                _dialogService.ShowNotification(
                    ResourceHelper.GetString("Common_InfoTitle"),
                    string.Format(ResourceHelper.GetString("Preset_NotExist"), presetNumber),
                    DialogIconType.Info);
                return;
            }

            string message = string.Format(ResourceHelper.GetString("Preset_LoadConfirmMessage"), presetNumber, relativePresetData.PresetName);
            bool confirmed = _dialogService.ShowConfirmation(
                title: string.Format(ResourceHelper.GetString("Preset_LoadConfirmTitle"), presetNumber),
                message: message,
                iconType: DialogIconType.Info);

            if (!confirmed) return;

            OnStateChanging();
            try
            {
                var absolutePresetData = MakePathsAbsolute(relativePresetData, SourcePath);
                ApplyAllData(absolutePresetData);
                HasUnsavedChanges = true;
                _dialogService.ShowNotification(
                    ResourceHelper.GetString("Common_SuccessTitle"),
                    string.Format(ResourceHelper.GetString("Preset_LoadedSuccess"), presetNumber, relativePresetData.PresetName),
                    DialogIconType.Success);
            }
            catch (System.Exception ex)
            {
                _dialogService.ShowNotification(
                    ResourceHelper.GetString("Common_ErrorTitle"),
                    string.Format(ResourceHelper.GetString("Preset_LoadError"), presetNumber, ex.Message),
                    DialogIconType.Error);
            }
        }

        private void OnPresetDeleteRequested(int presetNumber)
        {
            string presetName = _saveService.GetPresetName(presetNumber);
            if (string.IsNullOrWhiteSpace(presetName))
            {
                presetName = string.Format(ResourceHelper.GetString("Preset_DefaultNamePrefix") + " ({0})", presetNumber);
            }

            string message = string.Format(ResourceHelper.GetString("Preset_DeleteConfirmMessage"), presetNumber, presetName);
            bool confirmed = _dialogService.ShowConfirmation(
                title: string.Format(ResourceHelper.GetString("Preset_DeleteConfirmTitle"), presetNumber),
                message: message,
                iconType: DialogIconType.Warning);

            if (confirmed)
            {
                try
                {
                    _saveService.DeletePreset(presetNumber);
                    OptionsVM.CheckForExistingPresets();
                    _dialogService.ShowNotification(
                        ResourceHelper.GetString("Common_SuccessTitle"),
                        string.Format(ResourceHelper.GetString("Preset_DeletedSuccess"), presetNumber, presetName),
                        DialogIconType.Success);
                }
                catch (System.Exception ex)
                {
                    _dialogService.ShowNotification(
                        ResourceHelper.GetString("Common_ErrorTitle"),
                        string.Format(ResourceHelper.GetString("Preset_DeleteError"), presetNumber, ex.Message),
                        DialogIconType.Error);
                }
            }
        }

        private void OnCurrentSaveDeleteRequested()
        {
            bool confirmed = _dialogService.ShowConfirmation(
                title: ResourceHelper.GetString("Reset_ConfirmTitle"),
                message: ResourceHelper.GetString("Reset_ConfirmMessage"),
                iconType: DialogIconType.Warning);

            if (confirmed)
            {
                OnStateChanging();
                try
                {
                    _saveService.DeleteSaveData();
                    ApplyAllData(new SaveData());
                    HasUnsavedChanges = false;
                    _dialogService.ShowNotification(
                        ResourceHelper.GetString("Common_SuccessTitle"),
                        ResourceHelper.GetString("Reset_SuccessMessage"),
                        DialogIconType.Success);
                }
                catch (System.Exception ex)
                {
                    _dialogService.ShowNotification(
                        ResourceHelper.GetString("Common_ErrorTitle"),
                        string.Format(ResourceHelper.GetString("Reset_ErrorMessage"), ex.Message),
                        DialogIconType.Error);
                }
            }
        }

        private void OnStateChanging()
        {
            if (_isLoading) return;
            var currentState = GetCurrentSaveData();
            _undoService.RecordState(currentState);
            HasUnsavedChanges = true;
        }

        private SaveData MakePathsRelative(SaveData absoluteData, string basePath)
        {
            var relativeData = GetCurrentSaveData();
            relativeData.PresetName = absoluteData.PresetName;

            relativeData.SourcePath = ".";
            relativeData.DestinationPath = string.IsNullOrWhiteSpace(absoluteData.DestinationPath) ? "" : Path.GetRelativePath(basePath, absoluteData.DestinationPath);

            relativeData.FileContentExport.FolderTreeState = absoluteData.FileContentExport.FolderTreeState.Select(s => new FolderState { FullPath = Path.GetRelativePath(basePath, s.FullPath), IsSelected = s.IsSelected, IsExpanded = s.IsExpanded }).ToList();
            relativeData.FileContentExport.SelectedFilePaths = absoluteData.FileContentExport.SelectedFilePaths.Select(p => Path.GetRelativePath(basePath, p)).ToList();
            relativeData.FileContentExport.IgnoredCommentFilePaths = absoluteData.FileContentExport.IgnoredCommentFilePaths.Select(p => Path.GetRelativePath(basePath, p)).ToList();

            relativeData.FolderContentExport.FolderTreeState = absoluteData.FolderContentExport.FolderTreeState.Select(s => new FolderState { FullPath = Path.GetRelativePath(basePath, s.FullPath), IsSelected = s.IsSelected, IsExpanded = s.IsExpanded }).ToList();
            relativeData.TreeGenerator.FolderTreeState = absoluteData.TreeGenerator.FolderTreeState.Select(s => new FolderState { FullPath = Path.GetRelativePath(basePath, s.FullPath), IsSelected = s.IsSelected, IsExpanded = s.IsExpanded }).ToList();

            return relativeData;
        }

        private SaveData MakePathsAbsolute(SaveData relativeData, string basePath)
        {
            var absoluteData = relativeData;
            absoluteData.SourcePath = basePath;
            absoluteData.DestinationPath = string.IsNullOrWhiteSpace(relativeData.DestinationPath) ? "" : Path.GetFullPath(Path.Combine(basePath, relativeData.DestinationPath));

            absoluteData.FileContentExport.FolderTreeState = relativeData.FileContentExport.FolderTreeState.Select(s => new FolderState { FullPath = Path.GetFullPath(Path.Combine(basePath, s.FullPath)), IsSelected = s.IsSelected, IsExpanded = s.IsExpanded }).ToList();
            absoluteData.FileContentExport.SelectedFilePaths = relativeData.FileContentExport.SelectedFilePaths.Select(p => Path.GetFullPath(Path.Combine(basePath, p))).ToList();
            absoluteData.FileContentExport.IgnoredCommentFilePaths = relativeData.FileContentExport.IgnoredCommentFilePaths.Select(p => Path.GetFullPath(Path.Combine(basePath, p))).ToList();

            absoluteData.FolderContentExport.FolderTreeState = relativeData.FolderContentExport.FolderTreeState.Select(s => new FolderState { FullPath = Path.GetFullPath(Path.Combine(basePath, s.FullPath)), IsSelected = s.IsSelected, IsExpanded = s.IsExpanded }).ToList();
            absoluteData.TreeGenerator.FolderTreeState = relativeData.TreeGenerator.FolderTreeState.Select(s => new FolderState { FullPath = Path.GetFullPath(Path.Combine(basePath, s.FullPath)), IsSelected = s.IsSelected, IsExpanded = s.IsExpanded }).ToList();

            return absoluteData;
        }
    }
}