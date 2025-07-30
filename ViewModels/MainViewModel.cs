using FileCraft.Models;
using FileCraft.Services.Interfaces;
using FileCraft.Shared.Commands;
using FileCraft.ViewModels.Functional;
using FileCraft.ViewModels.Shared;
using System.Windows.Input;

namespace FileCraft.ViewModels
{
    public class MainViewModel : BaseViewModel
    {
        private readonly ISaveService _saveService;
        private readonly ISharedStateService _sharedStateService;
        private readonly IDialogService _dialogService;
        private bool _isLoading = false;

        public FolderTreeManager FolderTreeManager { get; }

        public string SourcePath
        {
            get => _sharedStateService.SourcePath;
            set
            {
                if (_sharedStateService.SourcePath != value)
                {
                    _sharedStateService.SourcePath = value;
                    OnPropertyChanged();
                    if (!_isLoading)
                    {
                        FolderTreeManager.LoadTreeForPath(value);
                    }
                }
            }
        }

        public string DestinationPath
        {
            get => _sharedStateService.DestinationPath;
            set
            {
                if (_sharedStateService.DestinationPath != value)
                {
                    _sharedStateService.DestinationPath = value;
                    OnPropertyChanged();
                }
            }
        }

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

        public MainViewModel(
            ISaveService saveService,
            ISharedStateService sharedStateService,
            IDialogService dialogService,
            FolderTreeManager folderTreeManager,
            FileContentExportViewModel fileContentExportVM,
            TreeGeneratorViewModel treeGeneratorVM,
            FolderContentExportViewModel folderContentExportVM,
            FileRenamerViewModel fileRenamerVM,
            OptionsViewModel optionsVM)
        {
            _saveService = saveService;
            _sharedStateService = sharedStateService;
            _dialogService = dialogService;
            FolderTreeManager = folderTreeManager;
            FileContentExportVM = fileContentExportVM;
            TreeGeneratorVM = treeGeneratorVM;
            FolderContentExportVM = folderContentExportVM;
            FileRenamerVM = fileRenamerVM;
            OptionsVM = optionsVM;
            OptionsVM.PresetSaveRequested += OnPresetSaveRequested;
            OptionsVM.PresetLoadRequested += OnPresetLoadRequested;
            OptionsVM.PresetDeleteRequested += OnPresetDeleteRequested;
            OptionsVM.CurrentSaveDeleteRequested += OnCurrentSaveDeleteRequested;

            ClearPathsCommand = new RelayCommand(_ => ClearPaths());
            SelectSourcePathCommand = new RelayCommand(_ => SelectPath(isSource: true));
            SelectDestinationPathCommand = new RelayCommand(_ => SelectPath(isSource: false));

            LoadData();
        }

        private void SelectPath(bool isSource)
        {
            var title = isSource ? "Select the common source folder" : "Select the common destination folder";
            var selectedPath = _dialogService.SelectFolder(title);
            if (!string.IsNullOrEmpty(selectedPath))
            {
                if (isSource)
                    SourcePath = selectedPath;
                else
                    DestinationPath = selectedPath;
            }
        }

        private void ClearPaths()
        {
            SourcePath = string.Empty;
            DestinationPath = string.Empty;
        }

        private void LoadData()
        {
            SaveData saveData = _saveService.LoadSaveData();
            ApplyAllData(saveData);
        }

        public void Save()
        {
            var saveData = GetCurrentSaveData();
            _saveService.Save(saveData);
        }

        private SaveData GetCurrentSaveData()
        {
            return new SaveData
            {
                SourcePath = this.SourcePath,
                DestinationPath = this.DestinationPath,
                SelectedTabIndex = this.SelectedTabIndex,
                FolderTreeState = FolderTreeManager.GetFolderStates(),
                FileContentExport = new FileContentExportSettings
                {
                    OutputFileName = FileContentExportVM.OutputFileName,
                    AppendTimestamp = FileContentExportVM.AppendTimestamp,
                    SelectedExtensions = FileContentExportVM.GetSelectedExtensions(),
                    SelectedFilePaths = FileContentExportVM.GetSelectedFilePaths()
                },
                FolderContentExport = new FolderContentExportSettings
                {
                    OutputFileName = FolderContentExportVM.OutputFileName,
                    AppendTimestamp = FolderContentExportVM.AppendTimestamp,
                    SelectedColumns = FolderContentExportVM.GetSelectedColumns()
                },
                TreeGenerator = new TreeGeneratorSettings
                {
                    OutputFileName = TreeGeneratorVM.OutputFileName,
                    AppendTimestamp = TreeGeneratorVM.AppendTimestamp
                },
                FileRenamer = FileRenamerVM.GetSettings()
            };
        }

        private void ApplyAllData(SaveData saveData)
        {
            _isLoading = true;
            try
            {
                SourcePath = saveData.SourcePath;
                DestinationPath = saveData.DestinationPath;
                SelectedTabIndex = saveData.SelectedTabIndex;

                FolderTreeManager.LoadTreeForPath(SourcePath, saveData.FolderTreeState);

                FileContentExportVM.ApplySettings(saveData.FileContentExport);
                FolderContentExportVM.ApplySettings(saveData.FolderContentExport);
                FileRenamerVM.ApplySettings(saveData.FileRenamer);

                TreeGeneratorVM.OutputFileName = saveData.TreeGenerator.OutputFileName;
                TreeGeneratorVM.AppendTimestamp = saveData.TreeGenerator.AppendTimestamp;
            }
            finally
            {
                _isLoading = false;
            }
        }

        private void OnPresetSaveRequested(int presetNumber, string presetName)
        {
            bool confirmed = _dialogService.ShowConfirmation(
                title: $"Save to Preset {presetNumber}",
                message: $"This will save the current configuration as '{presetName}'.\nIf a preset already exists, it will be overwritten.\n\nAre you sure you want to continue?");

            if (!confirmed) return;

            try
            {
                var currentSaveData = GetCurrentSaveData();
                currentSaveData.PresetName = presetName;
                _saveService.SaveAsPreset(currentSaveData, presetNumber);
                _dialogService.ShowNotification("Success", $"Preset '{presetName}' saved successfully to slot {presetNumber}.");
                OptionsVM.CheckForExistingPresets();
            }
            catch (Exception ex)
            {
                _dialogService.ShowNotification("Error", $"Failed to save preset {presetNumber}.\n\n{ex.Message}");
            }
        }

        private void OnPresetLoadRequested(int presetNumber)
        {
            try
            {
                var presetData = _saveService.LoadFromPreset(presetNumber);
                if (presetData == null)
                {
                    _dialogService.ShowNotification("Information", $"Preset {presetNumber} does not exist yet.");
                    return;
                }

                ApplyAllData(presetData);
                Save();

                _dialogService.ShowNotification("Success", $"Preset {presetNumber} loaded successfully.");
            }
            catch (Exception ex)
            {
                _dialogService.ShowNotification("Error", $"Failed to load preset {presetNumber}.\n\n{ex.Message}");
            }
        }

        private void OnPresetDeleteRequested(int presetNumber)
        {
            bool confirmed = _dialogService.ShowConfirmation(
                title: $"Delete Preset {presetNumber}",
                message: "This action cannot be undone.");

            if (confirmed)
            {
                try
                {
                    _saveService.DeletePreset(presetNumber);
                    OptionsVM.CheckForExistingPresets();
                    _dialogService.ShowNotification("Success", $"Preset {presetNumber} has been deleted.");
                }
                catch (Exception ex)
                {
                    _dialogService.ShowNotification("Error", $"Failed to delete preset {presetNumber}.\n\n{ex.Message}");
                }
            }
        }

        private void OnCurrentSaveDeleteRequested()
        {
            bool confirmed = _dialogService.ShowConfirmation(
                title: "Delete Current Save",
                message: "All current settings will be reset to default. This action cannot be undone.");

            if (confirmed)
            {
                try
                {
                    _saveService.DeleteSaveData();
                    ApplyAllData(new SaveData());
                    _dialogService.ShowNotification("Success", "Current save data has been deleted. Application has been reset to default settings.");
                }
                catch (Exception ex)
                {
                    _dialogService.ShowNotification("Error", $"Failed to delete save data.\n\n{ex.Message}");
                }
            }
        }
    }
}
