using FileCraft.Models;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using FileCraft.ViewModels.Shared;
using FileCraft.Shared.Helpers;

namespace FileCraft.Views.Shared
{
    public partial class BulkSearchWindow : Window, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        private readonly Dictionary<string, SelectableFile> _allAvailableFilesLookup;
        public ObservableCollection<FoundFileViewModel> FilteredFoundFiles { get; } = new();
        private readonly List<FoundFileViewModel> _allFoundFileViewModelsCache = new();

        private readonly string _iconGlyph;
        private readonly Brush _iconBrush;

        private bool? _areAllFoundFilesSelected = false;
        private bool _isUpdatingSelectAll = false;

        public bool? AreAllFoundFilesSelected
        {
            get => _areAllFoundFilesSelected;
            set
            {
                bool selectAll = _areAllFoundFilesSelected != true;

                _isUpdatingSelectAll = true;
                foreach (var file in FilteredFoundFiles)
                {
                    file.IsSelected = selectAll;
                }
                _isUpdatingSelectAll = false;
                UpdateSelectAllState();
            }
        }

        public BulkSearchWindow(IEnumerable<SelectableFile> allFiles, string iconGlyph, Brush iconBrush)
        {
            InitializeComponent();
            Owner = Application.Current.MainWindow;
            DataContext = this;
            _allAvailableFilesLookup = allFiles.ToDictionary(
                f => f.RelativePath,
                f => f,
                StringComparer.OrdinalIgnoreCase);

            _iconGlyph = iconGlyph;
            _iconBrush = iconBrush;

            InputTotalTextBlock.Text = $"{ResourceHelper.GetString("BulkSearch_TotalLinesLabel")} 0";
            UpdateTotals();
        }

        private void InputTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var matchedFiles = new HashSet<SelectableFile>();
            var countBuilder = new StringBuilder();
            int inputLinesWithMatches = 0;

            int lineCount = InputTextBox.LineCount;
            for (int i = 0; i < lineCount; i++)
            {
                string line = InputTextBox.GetLineText(i);
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

            CountTextBlock.Text = countBuilder.ToString();
            InputTotalTextBlock.Text = $"{ResourceHelper.GetString("BulkSearch_TotalLinesLabel")} {inputLinesWithMatches}";

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

        private void InputTextBox_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            if (e.VerticalChange != 0)
            {
                CountScrollViewer.ScrollToVerticalOffset(e.VerticalOffset);
            }
            if (e.HorizontalChange != 0)
            {
                CountScrollViewer.ScrollToHorizontalOffset(e.HorizontalOffset);
            }
        }

        private void UpdateTotals()
        {
            int totalFound = FilteredFoundFiles.Count;
            int totalChanged = FilteredFoundFiles.Count(f => f.IsSelected != f.OriginalIsSelected);

            FoundTotalTextBlock.Text = $"{ResourceHelper.GetString("BulkSearch_TotalFoundLabel")} {totalFound} | {ResourceHelper.GetString("BulkSearch_TotalChangedLabel")} {totalChanged}";
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

        private void ApplyButton_Click(object sender, RoutedEventArgs e)
        {
            var viewModel = new GenericDialogViewModel
            {
                Title = ResourceHelper.GetString("BulkSearch_ApplyTitle"),
                Message = ResourceHelper.GetString("BulkSearch_ApplyMessage"),
                IconGlyph = _iconGlyph,
                IconBrush = _iconBrush,
                PrimaryButtonText = ResourceHelper.GetString("Common_YesButton"),
                PrimaryButtonStyle = "PrimaryButton",
                SecondaryButtonText = ResourceHelper.GetString("Common_CancelButton")
            };

            var confirmationWindow = new GenericDialogWindow(viewModel);

            if (confirmationWindow.ShowDialog() == true)
            {
                foreach (var vm in _allFoundFileViewModelsCache)
                {
                    if (vm.IsSelected != vm.OriginalIsSelected)
                    {
                        vm.BackingFile.IsSelected = vm.IsSelected;
                    }
                }
                this.DialogResult = true;
                this.Close();
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }

        private void RemoveWhitespacesButton_Click(object sender, RoutedEventArgs e)
        {
            string currentText = InputTextBox.Text;
            if (string.IsNullOrEmpty(currentText))
            {
                return;
            }

            var lines = currentText.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

            var processedLines = lines
                .Select(line => Regex.Replace(line, @"\s+", ""))
                .Where(line => !string.IsNullOrEmpty(line));

            InputTextBox.Text = string.Join(Environment.NewLine, processedLines);
        }

        private void BreakIntoLinesButton_Click(object sender, RoutedEventArgs e)
        {
            string currentText = InputTextBox.Text;
            if (string.IsNullOrEmpty(currentText))
            {
                return;
            }

            char delimiter = BySemicolonRadioButton.IsChecked == true ? ';' : ' ';

            var lines = currentText.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

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
            InputTextBox.Text = string.Join(Environment.NewLine, newLines);
        }

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
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