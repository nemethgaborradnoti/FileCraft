using FileCraft.Models;
using FileCraft.Services.Interfaces;
using FileCraft.Shared.Commands;
using FileCraft.Shared.Helpers;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows.Input;

namespace FileCraft.ViewModels.Functional
{
    public class OptionsGeneralViewModel : BaseViewModel
    {
        private readonly ISaveService _saveService;
        private readonly IDialogService _dialogService;
        private readonly ISharedStateService _sharedStateService;

        public event Action? IgnoredFoldersChanged;
        public event Action? CurrentSaveDeleteRequested;

        public string Version
        {
            get
            {
                var version = System.Reflection.Assembly.GetEntryAssembly()?.GetName().Version;
                return version != null ? $"v{version.Major}.{version.Minor}.{version.Build}" : "Unknown Version";
            }
        }

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

        public ICommand DeleteCurrentSaveCommand { get; }
        public ICommand OpenSaveFolderCommand { get; }
        public ICommand EditIgnoredFoldersCommand { get; }

        public OptionsGeneralViewModel(
            ISaveService saveService,
            IDialogService dialogService,
            ISharedStateService sharedStateService)
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

            DeleteCurrentSaveCommand = new RelayCommand(_ => CurrentSaveDeleteRequested?.Invoke());
            OpenSaveFolderCommand = new RelayCommand(_ => OpenSaveFolder());
            EditIgnoredFoldersCommand = new RelayCommand(_ => EditIgnoredFolders());

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
    }
}