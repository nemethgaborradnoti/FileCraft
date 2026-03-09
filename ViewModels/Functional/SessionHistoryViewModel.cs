using FileCraft.Models;
using FileCraft.Services.Interfaces;
using FileCraft.Shared.Commands;
using FileCraft.Shared.Helpers;
using System;
using System.Threading.Tasks;
using System.Windows.Input;

namespace FileCraft.ViewModels.Functional
{
    public class SessionHistoryViewModel : BaseViewModel
    {
        private readonly ISaveService _saveService;
        private readonly IUndoService _undoService;
        private readonly IDialogService _dialogService;

        public Func<SaveData>? GetCurrentStateFunc { get; set; }
        public Func<SaveData, Task>? ApplyStateFuncAsync { get; set; }

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

        public ICommand UndoCommand { get; }
        public ICommand RedoCommand { get; }
        public ICommand SaveCommand { get; }

        public SessionHistoryViewModel(ISaveService saveService, IUndoService undoService, IDialogService dialogService)
        {
            _saveService = saveService;
            _undoService = undoService;
            _dialogService = dialogService;

            _undoService.HistoryChanged += () =>
            {
                OnPropertyChanged(nameof(CanUndo));
                OnPropertyChanged(nameof(CanRedo));
            };

            UndoCommand = new RelayCommand(async _ => await UndoAsync(), _ => CanUndo);
            RedoCommand = new RelayCommand(async _ => await RedoAsync(), _ => CanRedo);
            SaveCommand = new RelayCommand(_ => Save(), _ => HasUnsavedChanges);
        }

        public void RecordState()
        {
            if (GetCurrentStateFunc != null)
            {
                var state = GetCurrentStateFunc();
                _undoService.RecordState(state);
                HasUnsavedChanges = true;
            }
        }

        public void ClearHistory()
        {
            _undoService.Clear();
        }

        private async Task UndoAsync()
        {
            if (!CanUndo || GetCurrentStateFunc == null || ApplyStateFuncAsync == null) return;

            IsBusy = true;
            var currentState = GetCurrentStateFunc();
            var previousState = _undoService.Undo(currentState);
            await ApplyStateFuncAsync(previousState);
            HasUnsavedChanges = true;
            IsBusy = false;

            CommandManager.InvalidateRequerySuggested();
        }

        private async Task RedoAsync()
        {
            if (!CanRedo || GetCurrentStateFunc == null || ApplyStateFuncAsync == null) return;

            IsBusy = true;
            var currentState = GetCurrentStateFunc();
            var nextState = _undoService.Redo(currentState);
            await ApplyStateFuncAsync(nextState);
            HasUnsavedChanges = true;
            IsBusy = false;

            CommandManager.InvalidateRequerySuggested();
        }

        public void Save()
        {
            if (GetCurrentStateFunc == null) return;

            var saveData = GetCurrentStateFunc();
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
    }
}