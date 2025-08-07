using FileCraft.Models;
using FileCraft.Services.Interfaces;
using FileCraft.Shared.Commands;
using FileCraft.ViewModels.Functional;
using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
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
        public FileRenamerViewModel FileRenamerVM { get; }
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
            FileRenamerViewModel fileRenamerVM,
            OptionsViewModel optionsVM)
        {
            _saveService = saveService;
            _sharedStateService = sharedStateService;
            _dialogService = dialogService;
            _undoService = undoService;

            FileContentExportVM = fileContentExportVM;
            TreeGeneratorVM = treeGeneratorVM;
            FolderContentExportVM = folderContentExportVM;
            FileRenamerVM = fileRenamerVM;
            OptionsVM = optionsVM;

            SubscribeToChanges();

            OptionsVM.PresetSaveRequested += OnPresetSaveRequested;
            OptionsVM.PresetRenameRequested += OnPresetRenameRequested;
            OptionsVM.PresetLoadRequested += OnPresetLoadRequested;
            OptionsVM.PresetDeleteRequested += OnPresetDeleteRequested;
            OptionsVM.CurrentSaveDeleteRequested += OnCurrentSaveDeleteRequested;

            ClearPathsCommand = new RelayCommand(_ => ClearPaths());
            SelectSourcePathCommand = new RelayCommand(_ => SelectPath(isSource: true));
            SelectDestinationPathCommand = new RelayCommand(_ => SelectPath(isSource: false));
            SaveCommand = new RelayCommand(_ => Save(), _ => HasUnsavedChanges);
            UndoCommand = new RelayCommand(_ => Undo(), _ => CanUndo);
            RedoCommand = new RelayCommand(_ => Redo(), _ => CanRedo);

            LoadData();
            HasUnsavedChanges = false;
        }

        private void SubscribeToChanges()
        {
            FileContentExportVM.StateChanging += OnStateChanging;
            TreeGeneratorVM.StateChanging += OnStateChanging;
            FolderContentExportVM.StateChanging += OnStateChanging;
            FileRenamerVM.StateChanging += OnStateChanging;
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
            var title = isSource ? "Select the common source folder" : "Select the common destination folder";
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
            }
        }

        private void ClearPaths()
        {
            OnStateChanging();
            _sharedStateService.SourcePath = string.Empty;
            _sharedStateService.DestinationPath = string.Empty;
            OnPropertyChanged(nameof(SourcePath));
            OnPropertyChanged(nameof(DestinationPath));
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
                FileRenamer = saveData.FileRenamer,
                SettingsPage = saveData.SettingsPage
            };

            _saveService.Save(dataToPersist);
            HasUnsavedChanges = false;
            _dialogService.ShowNotification("Success", "Settings saved successfully.", DialogIconType.Success);
        }

        public void RequestClose(CancelEventArgs e)
        {
            if (!HasUnsavedChanges)
            {
                return;
            }

            var result = _dialogService.ShowExitConfirmation(
                "Unsaved Changes",
                "You have unsaved changes. Do you want to save before quitting?");

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
                    FolderTreeState = TreeGeneratorVM.FolderTreeManager.GetFolderStates()
                },
                FileRenamer = FileRenamerVM.GetSettings()
            };
        }

        private void ApplyAllData(SaveData saveData)
        {
            _isLoading = true;
            _sharedStateService.SourcePath = saveData.SourcePath;
            _sharedStateService.DestinationPath = saveData.DestinationPath;
            OnPropertyChanged(nameof(SourcePath));
            OnPropertyChanged(nameof(DestinationPath));

            FileContentExportVM.FolderTreeManager.LoadTreeForPath(SourcePath, saveData.FileContentExport.FolderTreeState);
            FileContentExportVM.ApplySettings(saveData.FileContentExport);

            FolderContentExportVM.FolderTreeManager.LoadTreeForPath(SourcePath, saveData.FolderContentExport.FolderTreeState);
            FolderContentExportVM.ApplySettings(saveData.FolderContentExport);

            TreeGeneratorVM.FolderTreeManager.LoadTreeForPath(SourcePath, saveData.TreeGenerator.FolderTreeState);
            TreeGeneratorVM.OutputFileName = saveData.TreeGenerator.OutputFileName;
            TreeGeneratorVM.AppendTimestamp = saveData.TreeGenerator.AppendTimestamp;

            FileRenamerVM.ApplySettings(saveData.FileRenamer);

            SelectedTabIndex = saveData.SelectedTabIndex;
            _isLoading = false;
        }

        private void OnPresetSaveRequested(int presetNumber)
        {
            if (string.IsNullOrWhiteSpace(SourcePath) || !Directory.Exists(SourcePath))
            {
                _dialogService.ShowNotification("Action Required", "Please select a valid source folder before saving a preset.", DialogIconType.Warning);
                return;
            }

            bool exists = _saveService.CheckPresetExists(presetNumber);
            string presetName = exists ? _saveService.GetPresetName(presetNumber) : $"Preset{presetNumber:00}";

            string message;
            string title;

            if (exists)
            {
                title = $"Overwrite Preset ({presetNumber})";
                message = $"Are you sure you want to overwrite Preset ({presetNumber}): '{presetName}' with the current settings?";
            }
            else
            {
                title = $"Save New Preset ({presetNumber})";
                message = $"Are you sure you want to save the current settings as a new preset named '{presetName}'?";
            }

            bool confirmed = _dialogService.ShowConfirmation(
                title: title,
                message: message,
                iconType: DialogIconType.Info);

            if (!confirmed) return;

            try
            {
                OnStateChanging();
                var currentSaveData = GetCurrentSaveData();
                currentSaveData.PresetName = presetName;

                var relativeSaveData = MakePathsRelative(currentSaveData, SourcePath);
                _saveService.SaveAsPreset(relativeSaveData, presetNumber);
                _dialogService.ShowNotification("Success", $"Preset '{presetName}' saved successfully to slot {presetNumber}.", DialogIconType.Success);
                OptionsVM.CheckForExistingPresets();
            }
            catch (System.Exception ex)
            {
                _dialogService.ShowNotification("Error", $"Failed to save preset {presetNumber}.\n\n{ex.Message}", DialogIconType.Error);
            }
        }

        private void OnPresetRenameRequested(int presetNumber, string newName)
        {
            try
            {
                OnStateChanging();
                _saveService.UpdatePresetName(presetNumber, newName);
                OptionsVM.CheckForExistingPresets();
                _dialogService.ShowNotification("Success", $"Preset ({presetNumber}) renamed to '{newName}'.", DialogIconType.Success);
            }
            catch (Exception ex)
            {
                _dialogService.ShowNotification("Error", $"Failed to rename preset {presetNumber}.\n\n{ex.Message}", DialogIconType.Error);
            }
        }

        private void OnPresetLoadRequested(int presetNumber)
        {
            if (string.IsNullOrWhiteSpace(SourcePath) || !Directory.Exists(SourcePath))
            {
                _dialogService.ShowNotification("Action Required", "To load a preset, please first select the root source folder for your project.", DialogIconType.Warning);
                return;
            }

            var relativePresetData = _saveService.LoadFromPreset(presetNumber);
            if (relativePresetData == null)
            {
                _dialogService.ShowNotification("Information", $"Preset ({presetNumber}) does not exist yet.", DialogIconType.Info);
                return;
            }

            string message = $"Are you sure you want to load ALL settings from Preset ({presetNumber}): '{relativePresetData.PresetName}'?\nThis will overwrite your entire current configuration.";
            bool confirmed = _dialogService.ShowConfirmation(
                title: $"Load from Preset ({presetNumber})",
                message: message,
                iconType: DialogIconType.Info);

            if (!confirmed) return;

            OnStateChanging();
            try
            {
                var absolutePresetData = MakePathsAbsolute(relativePresetData, SourcePath);
                ApplyAllData(absolutePresetData);
                HasUnsavedChanges = true;
                _dialogService.ShowNotification("Success", $"Preset ({presetNumber}): '{relativePresetData.PresetName}' loaded successfully.", DialogIconType.Success);
            }
            catch (System.Exception ex)
            {
                _dialogService.ShowNotification("Error", $"Failed to load preset {presetNumber}.\n\n{ex.Message}", DialogIconType.Error);
            }
        }

        private void OnPresetDeleteRequested(int presetNumber)
        {
            string presetName = _saveService.GetPresetName(presetNumber);
            if (string.IsNullOrWhiteSpace(presetName))
            {
                presetName = $"Preset ({presetNumber})";
            }

            string message = $"You are about to delete Preset ({presetNumber}): '{presetName}'.\nThis action cannot be undone.\n\nAre you sure you want to continue?";
            bool confirmed = _dialogService.ShowConfirmation(
                title: $"Delete Preset ({presetNumber})",
                message: message,
                iconType: DialogIconType.Warning);

            if (confirmed)
            {
                OnStateChanging();
                try
                {
                    _saveService.DeletePreset(presetNumber);
                    OptionsVM.CheckForExistingPresets();
                    _dialogService.ShowNotification("Success", $"Preset ({presetNumber}): '{presetName}' has been deleted.", DialogIconType.Success);
                }
                catch (System.Exception ex)
                {
                    _dialogService.ShowNotification("Error", $"Failed to delete preset {presetNumber}.\n\n{ex.Message}", DialogIconType.Error);
                }
            }
        }

        private void OnCurrentSaveDeleteRequested()
        {
            bool confirmed = _dialogService.ShowConfirmation(
                title: "Delete Current Save",
                message: "Are you sure you want to delete all current settings and reset to default? This action cannot be undone.",
                iconType: DialogIconType.Warning);

            if (confirmed)
            {
                OnStateChanging();
                try
                {
                    _saveService.DeleteSaveData();
                    ApplyAllData(new SaveData());
                    HasUnsavedChanges = false;
                    _dialogService.ShowNotification("Success", "Current save data has been deleted. Application has been reset to default settings.", DialogIconType.Success);
                }
                catch (System.Exception ex)
                {
                    _dialogService.ShowNotification("Error", $"Failed to delete save data.\n\n{ex.Message}", DialogIconType.Error);
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

            absoluteData.FolderContentExport.FolderTreeState = relativeData.FolderContentExport.FolderTreeState.Select(s => new FolderState { FullPath = Path.GetFullPath(Path.Combine(basePath, s.FullPath)), IsSelected = s.IsSelected, IsExpanded = s.IsExpanded }).ToList();
            absoluteData.TreeGenerator.FolderTreeState = relativeData.TreeGenerator.FolderTreeState.Select(s => new FolderState { FullPath = Path.GetFullPath(Path.Combine(basePath, s.FullPath)), IsSelected = s.IsSelected, IsExpanded = s.IsExpanded }).ToList();

            return absoluteData;
        }
    }
}
