using FileCraft.Models;
using FileCraft.Services.Interfaces;
using FileCraft.Shared.Commands;
using FileCraft.ViewModels.Functional;
using FileCraft.ViewModels.Shared;
using System;
using System.Windows.Input;

namespace FileCraft.ViewModels
{
    public class MainViewModel : BaseViewModel
    {
        private readonly ISettingsService _settingsService;
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
        public SettingsViewModel SettingsVM { get; }

        public ICommand ClearPathsCommand { get; }
        public ICommand SelectSourcePathCommand { get; }
        public ICommand SelectDestinationPathCommand { get; }

        public MainViewModel(
            ISettingsService settingsService,
            ISharedStateService sharedStateService,
            IDialogService dialogService,
            FolderTreeManager folderTreeManager,
            FileContentExportViewModel fileContentExportVM,
            TreeGeneratorViewModel treeGeneratorVM,
            FolderContentExportViewModel folderContentExportVM,
            FileRenamerViewModel fileRenamerVM,
            SettingsViewModel settingsVM)
        {
            _settingsService = settingsService;
            _sharedStateService = sharedStateService;
            _dialogService = dialogService;
            FolderTreeManager = folderTreeManager;
            FileContentExportVM = fileContentExportVM;
            TreeGeneratorVM = treeGeneratorVM;
            FolderContentExportVM = folderContentExportVM;
            FileRenamerVM = fileRenamerVM;
            SettingsVM = settingsVM;
            SettingsVM.PresetSaveRequested += OnPresetSaveRequested;
            SettingsVM.PresetLoadRequested += OnPresetLoadRequested;

            ClearPathsCommand = new RelayCommand(_ => ClearPaths());
            SelectSourcePathCommand = new RelayCommand(_ => SelectPath(isSource: true));
            SelectDestinationPathCommand = new RelayCommand(_ => SelectPath(isSource: false));

            LoadSettings();
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

        private void LoadSettings()
        {
            Settings settings = _settingsService.LoadSettings();
            ApplyAllSettings(settings);
        }

        public void SaveSettings()
        {
            var settings = GetCurrentSettings();
            _settingsService.SaveSettings(settings);
        }

        private Settings GetCurrentSettings()
        {
            return new Settings
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
                FileRenamer = FileRenamerVM.GetSettings(),
                SettingsPage = new SettingsPageSettings()
            };
        }

        private void ApplyAllSettings(Settings settings)
        {
            _isLoading = true;
            try
            {
                SourcePath = settings.SourcePath;
                DestinationPath = settings.DestinationPath;
                SelectedTabIndex = settings.SelectedTabIndex;

                FolderTreeManager.LoadTreeForPath(SourcePath, settings.FolderTreeState);

                FileContentExportVM.ApplySettings(settings.FileContentExport);
                FolderContentExportVM.ApplySettings(settings.FolderContentExport);
                FileRenamerVM.ApplySettings(settings.FileRenamer);

                TreeGeneratorVM.OutputFileName = settings.TreeGenerator.OutputFileName;
                TreeGeneratorVM.AppendTimestamp = settings.TreeGenerator.AppendTimestamp;
            }
            finally
            {
                _isLoading = false;
            }
        }

        private void OnPresetSaveRequested(int presetNumber)
        {
            try
            {
                var currentSettings = GetCurrentSettings();
                _settingsService.SaveSettingsAsPreset(currentSettings, presetNumber);
                _dialogService.ShowNotification("Success", $"Preset {presetNumber} saved successfully.");
                SettingsVM.CheckForExistingPresets();
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
                var presetSettings = _settingsService.LoadSettingsFromPreset(presetNumber);
                if (presetSettings == null)
                {
                    _dialogService.ShowNotification("Information", $"Preset {presetNumber} does not exist yet.");
                    return;
                }

                ApplyAllSettings(presetSettings);
                SaveSettings();

                _dialogService.ShowNotification("Success", $"Preset {presetNumber} loaded successfully.");
            }
            catch (Exception ex)
            {
                _dialogService.ShowNotification("Error", $"Failed to load preset {presetNumber}.\n\n{ex.Message}");
            }
        }
    }
}
