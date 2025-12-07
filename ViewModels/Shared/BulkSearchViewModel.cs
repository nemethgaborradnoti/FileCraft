using FileCraft.Models;
using FileCraft.Services.Interfaces;
using FileCraft.Shared.Commands;
using FileCraft.Shared.Helpers;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Input;
using System.Windows.Media;

namespace FileCraft.ViewModels.Shared
{
    public class BulkSearchViewModel : BaseViewModel
    {
        private readonly Dictionary<string, SelectableFile> _allAvailableFilesLookup;
        private readonly List<FoundFileViewModel> _allFoundFileViewModelsCache = new();
        private readonly IDialogService _dialogService;

        public ObservableCollection<FoundFileViewModel> FilteredFoundFiles { get; } = new();

        private string _inputText = string.Empty;
        private string _lineNumbersText = string.Empty;
        private string _inputLinesCountText = string.Empty;
        private string _foundTotalText = string.Empty;
        private bool? _areAllFoundFilesSelected = false;
        private bool _isUpdatingSelectAll = false;
        private bool _isBreakBySpace = true;

        public string Title => ResourceHelper.GetString("BulkSearch_Title");
        public string InputPathsLabel => ResourceHelper.GetString("BulkSearch_InputPathsLabel");
        public string FoundPathsLabel => ResourceHelper.GetString("BulkSearch_FoundPathsLabel");
        public string IconGlyph { get; }
        public Brush IconBrush { get; }

        public event Action<bool?>? RequestClose;

        public string InputText
        {
            get => _inputText;
            set
            {
                if (_inputText != value)
                {
                    _inputText = value;
                    OnPropertyChanged();
                    OnInputTextChanged();
                }
            }
        }

        public string LineNumbersText
        {
            get => _lineNumbersText;
            set
            {
                if (_lineNumbersText != value)
                {
                    _lineNumbersText = value;
                    OnPropertyChanged();
                }
            }
        }

        public string InputLinesCountText
        {
            get => _inputLinesCountText;
            set
            {
                if (_inputLinesCountText != value)
                {
                    _inputLinesCountText = value;
                    OnPropertyChanged();
                }
            }
        }

        public string FoundTotalText
        {
            get => _foundTotalText;
            set
            {
                if (_foundTotalText != value)
                {
                    _foundTotalText = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool? AreAllFoundFilesSelected
        {
            get => _areAllFoundFilesSelected;
            set
            {
                if (_areAllFoundFilesSelected != value)
                {
                    _areAllFoundFilesSelected = value;
                    OnPropertyChanged();

                    if (_areAllFoundFilesSelected != null)
                    {
                        bool selectAll = _areAllFoundFilesSelected == true;
                        _isUpdatingSelectAll = true;
                        foreach (var file in FilteredFoundFiles)
                        {
                            file.IsSelected = selectAll;
                        }
                        _isUpdatingSelectAll = false;
                        UpdateTotals();
                    }
                }
            }
        }

        public bool IsBreakBySpace
        {
            get => _isBreakBySpace;
            set
            {
                if (_isBreakBySpace != value)
                {
                    _isBreakBySpace = value;
                    OnPropertyChanged();
                }
            }
        }

        public ICommand RemoveWhitespacesCommand { get; }
        public ICommand BreakIntoLinesCommand { get; }
        public ICommand ApplyCommand { get; }
        public ICommand CancelCommand { get; }

        public BulkSearchViewModel(
            IEnumerable<SelectableFile> allFiles,
            string iconGlyph,
            Brush iconBrush,
            IDialogService dialogService)
        {
            _allAvailableFilesLookup = allFiles.ToDictionary(
                f => f.RelativePath,
                f => f,
                StringComparer.OrdinalIgnoreCase);

            IconGlyph = iconGlyph;
            IconBrush = iconBrush;
            _dialogService = dialogService;

            InputLinesCountText = $"{ResourceHelper.GetString("BulkSearch_TotalLinesLabel")} 0";
            UpdateTotals();

            RemoveWhitespacesCommand = new RelayCommand(_ => RemoveWhitespaces());
            BreakIntoLinesCommand = new RelayCommand(_ => BreakIntoLines());
            ApplyCommand = new RelayCommand(_ => Apply());
            CancelCommand = new RelayCommand(_ => Cancel());
        }

        private void OnInputTextChanged()
        {
            var matchedFiles = new HashSet<SelectableFile>();
            var countBuilder = new StringBuilder();
            int inputLinesWithMatches = 0;

            string[] lines = InputText.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);

            foreach (string line in lines)
            {
                string term = line.Trim();
                string normalizedTerm = term.Replace('/', '\\');
                int count = 0;

                if (!string.IsNullOrEmpty(normalizedTerm))
                {
                    foreach (var file in _allAvailableFilesLookup.Values)
                    {
                        if (file.RelativePath.IndexOf(normalizedTerm, StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            count++;
                            matchedFiles.Add(file);
                        }
                    }
                }

                if (count > 0)
                {
                    inputLinesWithMatches++;
                }

                countBuilder.AppendLine(count > 0 ? count.ToString() : string.Empty);
            }

            LineNumbersText = countBuilder.ToString();
            InputLinesCountText = $"{ResourceHelper.GetString("BulkSearch_TotalLinesLabel")} {inputLinesWithMatches}";

            FilteredFoundFiles.Clear();
            foreach (var file in matchedFiles.OrderBy(f => f.RelativePath))
            {
                var vm = _allFoundFileViewModelsCache.FirstOrDefault(v => v.BackingFile == file);
                if (vm == null)
                {
                    vm = new FoundFileViewModel(file, UpdateSelectAllState);
                    _allFoundFileViewModelsCache.Add(vm);
                }
                FilteredFoundFiles.Add(vm);
            }

            UpdateSelectAllState();
        }

        private void UpdateSelectAllState()
        {
            if (_isUpdatingSelectAll) return;

            int selectedCount = FilteredFoundFiles.Count(f => f.IsSelected);
            if (selectedCount == 0)
            {
                _areAllFoundFilesSelected = false;
            }
            else if (selectedCount == FilteredFoundFiles.Count && FilteredFoundFiles.Count > 0)
            {
                _areAllFoundFilesSelected = true;
            }
            else
            {
                _areAllFoundFilesSelected = null;
            }
            OnPropertyChanged(nameof(AreAllFoundFilesSelected));
            UpdateTotals();
        }

        private void UpdateTotals()
        {
            int totalFound = FilteredFoundFiles.Count;
            int totalChanged = FilteredFoundFiles.Count(f => f.IsSelected != f.OriginalIsSelected);

            FoundTotalText = $"{ResourceHelper.GetString("BulkSearch_TotalFoundLabel")} {totalFound} | {ResourceHelper.GetString("BulkSearch_TotalChangedLabel")} {totalChanged}";
        }

        private void RemoveWhitespaces()
        {
            if (string.IsNullOrEmpty(InputText)) return;

            var lines = InputText.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

            var processedLines = lines
                .Select(line => Regex.Replace(line, @"\s+", ""))
                .Where(line => !string.IsNullOrEmpty(line));

            InputText = string.Join(Environment.NewLine, processedLines);
        }

        private void BreakIntoLines()
        {
            if (string.IsNullOrEmpty(InputText)) return;

            char delimiter = IsBreakBySpace ? ' ' : ';';

            var lines = InputText.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

            var newLines = new List<string>();
            foreach (var line in lines)
            {
                var parts = line.Split(new[] { delimiter }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var part in parts)
                {
                    string trimmedPart = part.Trim();
                    if (!string.IsNullOrEmpty(trimmedPart))
                    {
                        newLines.Add(trimmedPart);
                    }
                }
            }
            InputText = string.Join(Environment.NewLine, newLines);
        }

        private void Apply()
        {
            bool confirmed = _dialogService.ShowConfirmation(
                ResourceHelper.GetString("BulkSearch_ApplyTitle"),
                ResourceHelper.GetString("BulkSearch_ApplyMessage"),
                Models.DialogIconType.Info);

            if (confirmed)
            {
                foreach (var vm in _allFoundFileViewModelsCache)
                {
                    if (vm.IsSelected != vm.OriginalIsSelected)
                    {
                        vm.BackingFile.IsSelected = vm.IsSelected;
                    }
                }
                RequestClose?.Invoke(true);
            }
        }

        private void Cancel()
        {
            RequestClose?.Invoke(false);
        }
    }

    public class FoundFileViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        private readonly Action _onSelectionChanged;
        private bool _isSelected;
        public SelectableFile BackingFile { get; }
        public bool OriginalIsSelected { get; }

        public string RelativePath => BackingFile.RelativePath;

        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (_isSelected != value)
                {
                    _isSelected = value;
                    OnPropertyChanged();
                    _onSelectionChanged?.Invoke();
                }
            }
        }

        public FoundFileViewModel(SelectableFile backingFile, Action onSelectionChanged)
        {
            BackingFile = backingFile;
            OriginalIsSelected = backingFile.IsSelected;
            _isSelected = backingFile.IsSelected;
            _onSelectionChanged = onSelectionChanged;
        }

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}